using System.Collections;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

namespace OceanX.BoidsGPU.Ecosystem
{
    /// <summary>
    /// Cinematic "introduction" camera. When a species is added for the FIRST time (its school count goes
    /// 0 -> 1), this catches the new school at its off-screen entry gate and follows the real fish as they
    /// swim into the scene, then releases so the CinemachineBrain blends back to the overview shot.
    ///
    /// Host-side only — it listens to <see cref="EcosystemSimulationGPU.OnSpeciesFirstIntroduced"/>, which is
    /// fired inside AddSchool once the fish exist at the gate, and reads their live centre-of-mass back from
    /// the GPU via <see cref="EcosystemSimulationGPU.TryGetSchoolCentroid"/>.
    ///
    /// SMOOTH FOLLOW: the raw GPU readback is throttled (a synchronous stall — don't do it every frame), so
    /// between reads the director has no fresh position. It must NOT simply damp the proxy toward the last
    /// read to cover that gap: chasing a value that is frozen three frames out of four left the proxy
    /// permanently behind the fish AND lurching every time a read landed.
    ///
    /// So each read returns the school's centre AND its live velocity (one readback, see
    /// EcosystemSimulationGPU.TryGetSchoolCentroidAndVelocity), and the target is EXTRAPOLATED forward from
    /// the last read every frame: centre + velocity * timeSinceRead. That target moves continuously — no
    /// steps to lurch over — and stays on the fish instead of trailing them.
    ///
    /// The proxy IS still smoothed, but aimed so the smoothing costs little lag. A school's centroid jitters
    /// (fish mill about, and it is an average over many of them), so feeding it raw to the camera looks
    /// nervous. The trick is WHERE the smoothing aims: the proxy is SmoothDamped toward a point led along the
    /// school's heading — target = centre + velocity * (timeSinceRead + smoothTime * leadStrength). A damped
    /// chase settles one smoothing time BEHIND whatever it aims at, so leading by that much cancels it.
    ///
    /// That cancellation is only EXACT at a steady speed, which entering fish are not: they sprint in at up
    /// to MaxSpeed and then decelerate hard to cruising. The velocity reading is smoothed, so while they slow
    /// it still reads high and a full-strength lead throws the proxy AHEAD of them (the camera aims at empty
    /// water); while they accelerate it reads low and the proxy falls behind. Hence _leadStrength defaults
    /// below 1: under-compensating slightly keeps the fish framed through both phases, because sitting a
    /// little behind reads far better than looking ahead of them. It is the knob to turn if the framing is
    /// consistently off in one direction.
    ///
    /// FRAMING MODE: <see cref="_framingMode"/> picks where the camera sits relative to the school —
    /// follow-behind (over their backs), a full side profile, or a 3/4 angle. The offset is authored in the
    /// SCHOOL'S frame, then at shot start it's rotated by the school's heading and by which side faces the
    /// camera, and baked into a fixed WORLD offset. So the framing is fish-relative (a side view is a real
    /// broadside from whichever gate they use, on the near side), but the camera then HOLDS that spot and
    /// simply TURNS to keep the fish framed as they swim in (aim damping) — rather than riding locked in
    /// their frame, which read as static. All set from code, so every scene gets it at runtime.
    ///
    /// That one baked offset is why the heading must be the direction the school is GOING, not the direction
    /// it happens to point as the shot opens — see <see cref="ResolveHeading"/>. New fish spawn facing the
    /// simulation centre and then steer onto a per-school randomised path, so a broadside built from their
    /// opening heading skews off by a different amount every shot.
    ///
    /// SCENE WIRING (full checklist in the comment block at the bottom of this file): a CinemachineBrain on
    /// the Main Camera, an overview CinemachineCamera at the idle priority, an intro CinemachineCamera with a
    /// CinemachineFollow + CinemachineRotationComposer whose Tracking Target is the proxy, and an empty proxy
    /// GameObject. Drop this component on any host object and wire the references.
    /// </summary>
    [DisallowMultipleComponent]
    public class IntroductionCameraDirectorGPU : MonoBehaviour
    {
        /// <summary>Where the intro camera sits relative to the entering school.</summary>
        public enum FramingMode
        {
            /// <summary>Behind the school, looking the way they swim — you see them over their backs against
            /// the scene ahead ("facing the scene").</summary>
            FollowBehind,
            /// <summary>Directly to the side — a full broadside profile as the school crosses the frame.</summary>
            SideView,
            /// <summary>A 3/4 angle — part side profile, part looking into the scene. A blend of the two.</summary>
            ThreeQuarter
        }

        [Header("References")]
        [Tooltip("The EcosystemSimulationGPU whose first-introduction event drives the shot. Auto-found in " +
                 "the scene if left empty.")]
        [SerializeField] private EcosystemSimulationGPU _simulation;

        [Tooltip("The intro CinemachineCamera. Its Priority is raised while a school swims in so the Brain " +
                 "blends to it, then lowered again. Its Follow / LookAt should point at the proxy below.")]
        [SerializeField] private CinemachineCamera _introCamera;

        [Tooltip("Empty Transform the intro camera Follows / LookAts. This director moves it to the new " +
                 "school's live centre (smoothed) each frame so the camera rides in with the fish.")]
        [SerializeField] private Transform _followProxy;

        [Header("Framing")]
        [Tooltip("Which angle the intro camera takes on the entering school. Changing this in the Inspector " +
                 "updates the camera's Follow Offset live (use CM_Introduce's Solo button to preview).")]
        [SerializeField] private FramingMode _framingMode = FramingMode.ThreeQuarter;

        // NOTE: offsets are now in the SCHOOL'S OWN frame (the proxy is rotated to face the way the fish
        // swim). +Z = ahead of them, -Z = behind, +X = their right, +Y = up. The X magnitude is the side
        // distance; its SIGN is chosen per-shot so the camera sits on the side facing where it came from
        // (no crossing over the school). So these are direction-agnostic — the same broadside no matter
        // which gate the fish enter from.
        // Renamed (…Local suffix) from the old world-space fields on purpose: these are now in the school's
        // OWN frame, and the rename orphans the stale world-space values saved in existing scenes so every
        // scene picks up these new defaults — while staying inspector-tunable going forward.
        [Tooltip("FollowBehind offset (school-local): mostly behind the school (-Z), a little to the side, up " +
                 "a touch — you ride in over their backs looking the way they swim.")]
        [SerializeField] private Vector3 _followBehindOffsetLocal = new Vector3(4f, 2.5f, -8f);

        [Tooltip("SideView offset (school-local): straight out to the side (X) for a full broadside profile " +
                 "as the school crosses the frame. No fore/aft component.")]
        [SerializeField] private Vector3 _sideViewOffsetLocal = new Vector3(9f, 2f, 0f);

        [Tooltip("ThreeQuarter offset (school-local): a blend — behind (-Z) AND out to the side (X).")]
        [SerializeField] private Vector3 _threeQuarterOffsetLocal = new Vector3(7f, 2.5f, -5f);

        [Header("Shot")]
        [Tooltip("Priority given to the intro camera while the shot plays. Must be ABOVE the overview " +
                 "camera's priority so the Brain blends to it.")]
        [SerializeField] private int _activePriority = 20;

        [Tooltip("Priority the intro camera rests at when idle. Must be BELOW the overview camera's priority.")]
        [SerializeField] private int _idlePriority = 0;

        [Tooltip("How long (seconds) to follow the school in before blending back to the overview shot.")]
        [Min(0.5f)]
        [SerializeField] private float _followDuration = 3.5f;

        [Header("Tracking")]
        // Renamed away from the old _positionSmoothTime on purpose: that value damped the proxy toward a
        // STALE target, which is what made it lag. _followSmoothTime below damps toward a LED target
        // instead, so it filters jitter without buying lag. The rename orphans the stale 0.25 saved in
        // existing scenes so every scene picks up the new defaults.
        [Tooltip("How hard to smooth out the school centroid's jitter (seconds). This does NOT make the " +
                 "camera lag: the target is led by this same amount, which cancels the damping's delay. " +
                 "Raise it if the shot looks nervous, lower it if the camera feels floaty. 0 = no filtering " +
                 "(the proxy sits exactly on the raw centroid, jitter included).")]
        [Min(0f)]
        [SerializeField] private float _followSmoothTime = 0.25f;

        [Tooltip("How much to smooth the school's measured velocity before extrapolating with it (seconds). " +
                 "Only steadies the SPEED reading, never the position, so it costs no tracking accuracy. " +
                 "A wobbling velocity swings the extrapolation around, so this is the OTHER jitter knob: " +
                 "raise it if the shot shimmers. But raise it too far and the reading is slow to notice the " +
                 "fish slowing down after their entry sprint, which throws the camera AHEAD of them. 0 = raw.")]
        [Min(0f)]
        [SerializeField] private float _velocitySmoothTime = 0.18f;

        [Tooltip("How much of the smoothing delay to cancel by leading the fish. THE FRAMING KNOB:\n" +
                 "  camera looks AHEAD of the fish  -> lower it\n" +
                 "  camera trails BEHIND the fish   -> raise it\n" +
                 "1 = fully cancel the delay, which is exact only while the fish hold a steady speed; " +
                 "entering fish sprint then slow down, so a full lead overshoots ahead as they settle. " +
                 "0 = no lead at all (the proxy simply trails by the smoothing time).")]
        [Range(0f, 1f)]
        [SerializeField] private float _leadStrength = 0.6f;

        [Tooltip("Extra seconds to LEAD the fish by, on top of catching up to now. 0 keeps the proxy exactly " +
                 "on the school, which frames best — the camera AIMS at this proxy, so leading pushes the " +
                 "fish off-centre backwards. Raise it only to pull the camera body in closer behind fast " +
                 "fish, and expect to trade a little centring for it.")]
        [Min(0f)]
        [SerializeField] private float _velocityLeadTime = 0f;

        [Tooltip("Safety cap on how STALE a reading may be counted as (seconds). Stops the proxy flying off " +
                 "on an old velocity if readbacks fail for a while. Keep a little above the read interval. " +
                 "The smoothing/lead terms are added on top of this and are not capped by it.")]
        [Min(0.05f)]
        [SerializeField] private float _maxExtrapolationTime = 0.4f;

        [Tooltip("Read the fish position back from the GPU every N frames (1 = every frame). The readback is " +
                 "a synchronous GPU stall, so keep this at 3-5 — the extrapolation covers the gap between " +
                 "reads exactly, so reading MORE often buys accuracy you already have, at the cost of stalls.")]
        [Min(1)]
        [SerializeField] private int _readbackEveryNFrames = 4;

        [Tooltip("Max frames to wait for the freshly spawned school's positions to become readable before " +
                 "starting the shot anyway (buffers are usually ready within a frame of the spawn).")]
        [Min(1)]
        [SerializeField] private int _maxSeedWaitFrames = 10;

        [Tooltip("Frames spent watching the school right after it spawns, BEFORE cutting the camera in: used " +
                 "to read a stable entry heading and to keep the proxy glued to the fish so the shot doesn't " +
                 "lurch to catch up when it starts. A handful (~0.1s); too many and the shot starts late.")]
        [Min(1)]
        [SerializeField] private int _entrySettleFrames = 5;

        [Header("Framing feel")]
        [Tooltip("How gently the camera TURNS to keep the fish framed as they swim (Cinemachine aim damping, " +
                 "seconds). The camera holds the spot it entered on and turns to follow — higher = a lazier, " +
                 "more cinematic turn; 0 = rigidly glued to the fish. Applied in code so every scene matches.")]
        [Min(0f)]
        [SerializeField] private float _aimDamping = 0.5f;

        [Tooltip("How long the camera BODY takes to reach its framing spot (Cinemachine Follow's Position " +
                 "Damping, seconds). Pushed from code for the same reason as the binding mode and the aim " +
                 "damping above — so every scene matches instead of carrying whatever was typed into it.\n" +
                 "This is a lag on the camera itself, on top of any smoothing of the proxy, and it is what " +
                 "makes the shot feel like it is forever ARRIVING rather than settled: at 1.5s the camera is " +
                 "still most of a tank-length short of its mark while the fish are sprinting. 0.4-0.6 keeps " +
                 "the move soft without the shot ending before it lands. Raise it for a lazier, more floated " +
                 "camera; lower it toward 0.2 to lock on hard.")]
        [Min(0f)]
        [SerializeField] private float _cameraPositionDamping = 0.5f;

        [Tooltip("Force the CinemachineBrain to evaluate in LateUpdate while this director is active.\n" +
                 "The proxy is moved from a script (a coroutine), and the Brain's default Smart Update picks " +
                 "between the physics rate and the render rate by WATCHING how the tracking target moves. A " +
                 "script-driven target that sits frozen between shots is exactly the case it reads wrong, and " +
                 "a wrong pick evaluates the camera at the physics rate (50Hz) while the fish are drawn at " +
                 "frame rate — a small mismatch every frame, which looks like the camera constantly catching " +
                 "up. LateUpdate is the correct setting for a target driven from script.\n" +
                 "Only overrides Smart Update; a Brain deliberately set to Fixed or Manual is left alone.")]
        [SerializeField] private bool _forceBrainLateUpdate = true;

        [Tooltip("Take the framing heading from the direction of the school's swim TARGET rather than from " +
                 "how the fish happen to be pointing as the shot opens. THIS IS WHY A SIDE VIEW SOMETIMES " +
                 "ARRIVES AS A 3/4.\n" +
                 "New fish are spawned pointing at the simulation centre (EcosystemSimulationGPU." +
                 "ApplyEntrySpawnOrigin), but they immediately steer onto their school's own path, whose " +
                 "centre, size, yaw and height are all randomised per school. So their opening heading is " +
                 "NOT the direction they settle into, and the camera — which bakes one fixed world offset " +
                 "from that heading and then only turns — watches its broadside skew away by a different " +
                 "amount every shot. Aiming at the target instead frames the direction they are actually " +
                 "going. Turn off to go back to reading the heading off their live velocity.")]
        [SerializeField] private bool _aimHeadingAtTarget = true;

        [Tooltip("Below this speed (m/s) the freshly spawned school counts as 'milling', and the shot falls " +
                 "back to the camera's own facing to choose the entry angle instead of a noisy heading.")]
        [Min(0f)]
        [SerializeField] private float _minHeadingSpeed = 0.4f;

        // Closer than this to its target, a school's school->target vector is too short to be a trustworthy
        // heading (and it is about to turn away anyway), so the shot falls back to the measured velocity.
        // Entry shots are never near this — the gate is off-screen and the target is inside the bounds.
        private const float MinTargetHeadingDistance = 3f;

        // The intro camera's position + aim stages — cached so we can push the framing offset / aim damping on.
        private CinemachineFollow _introFollow;
        private CinemachineRotationComposer _introComposer;

        // The Brain, cached so the director can set its update method. Its blend timing is left exactly as
        // the scene authored it: that blend is the camera's real travel from the overview spot to the shot,
        // often 20m+, so it needs its time — shortening it turns the approach into a whip-pan.
        private CinemachineBrain _brain;

        // The species whose shot is currently playing, or null when idle. Guards against overlapping shots
        // (a second introduction while one is running is ignored rather than fighting over the proxy/camera).
        private SpeciesDataGPU _playing;

        // Tracking state. Each frame the proxy is SmoothDamped toward
        // (_lastReadCentroid + _schoolVelocity * (timeSinceRead + _followSmoothTime)) — i.e. toward where the
        // fish will be one smoothing time from now, so the damping's own delay lands it back on where they
        // are right now. _smoothVel is SmoothDamp's momentum; it is seeded with the school's real velocity at
        // the start of a shot so the proxy never has to accelerate from a standstill while the fish sprint.
        private Vector3 _lastReadCentroid;
        private Vector3 _schoolVelocity;
        private Vector3 _smoothVel;
        private float _timeSinceRead;

        // Entry framing, both captured ONCE per shot: the direction the school swims in on, and which side
        // (+1 = the fish's right, -1 = left) to sit on. From these the camera takes a FIXED world spot and
        // then just turns (aim damping) to keep the fish framed — instead of riding locked in the school's
        // frame, which read as static.
        private Vector3 _heading = Vector3.forward;
        private float _sideSign = 1f;

        private void OnEnable()
        {
            if (_simulation == null)
                _simulation = FindAnyObjectByType<EcosystemSimulationGPU>();

            if (_simulation != null)
                _simulation.OnSpeciesFirstIntroduced += HandleFirstIntroduction;

            CacheFollow();
            ConfigureFollowBinding();
            ApplyBrainUpdateMethod();
            ApplyFramingOffset();

            if (_introCamera != null)
                _introCamera.Priority = _idlePriority;
        }

        // The offset is written in WORLD space, but it's COMPUTED each shot from the school's entry heading
        // (see FollowInRoutine), so the framing is fish-relative at entry yet the camera then holds that spot
        // and turns to the fish as they swim — instead of riding locked in their frame (which felt static).
        // Also push the aim damping so that "turn to the fish" is smooth, and the body damping so the camera
        // actually reaches its mark inside the shot. Done in code so every scene matches.
        private void ConfigureFollowBinding()
        {
            if (_introFollow != null)
            {
                _introFollow.TrackerSettings.BindingMode = BindingMode.WorldSpace;
                _introFollow.TrackerSettings.PositionDamping = Vector3.one * _cameraPositionDamping;
            }
            if (_introComposer != null)
                _introComposer.Damping = new Vector2(_aimDamping, _aimDamping);
        }

        // Smart Update (the Brain's default) decides between the physics rate and the render rate by sampling
        // how the tracking target moves. Our proxy is script-driven and frozen between shots, which is the
        // case it misreads — and being evaluated at 50Hz while the fish draw at frame rate reads as a small,
        // relentless catch-up. LateUpdate is right for a script-driven target and costs nothing else.
        // A Brain deliberately set to FixedUpdate or ManualUpdate is respected and left as-is.
        private void ApplyBrainUpdateMethod()
        {
            if (!_forceBrainLateUpdate) return;

            Camera cam = Camera.main;
            _brain = cam != null ? cam.GetComponent<CinemachineBrain>() : null;
            if (_brain == null) _brain = FindAnyObjectByType<CinemachineBrain>();
            if (_brain == null) return;

            if (_brain.UpdateMethod == CinemachineBrain.UpdateMethods.SmartUpdate)
                _brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
        }

        private void OnDisable()
        {
            if (_simulation != null)
                _simulation.OnSpeciesFirstIntroduced -= HandleFirstIntroduction;
        }

        // Keep the live camera offset in sync with the chosen mode while editing (visible via Solo).
        private void OnValidate()
        {
            CacheFollow();
            ApplyFramingOffset();
        }

        private void CacheFollow()
        {
            if (_introCamera == null) return;
            if (_introFollow == null)   _introFollow   = _introCamera.GetComponent<CinemachineFollow>();
            if (_introComposer == null) _introComposer = _introCamera.GetComponent<CinemachineRotationComposer>();
        }

        // Idle/editor placeholder offset. A live shot overwrites FollowOffset with a world-space vector built
        // from the school's entry heading (see FollowInRoutine); this just gives the camera a sane resting
        // offset before any shot has run.
        private void ApplyFramingOffset()
        {
            if (_introFollow == null) return;
            _introFollow.FollowOffset = SelectedOffset();
        }

        private Vector3 SelectedOffset()
        {
            switch (_framingMode)
            {
                case FramingMode.FollowBehind: return _followBehindOffsetLocal;
                case FramingMode.SideView:     return _sideViewOffsetLocal;
                default:                       return _threeQuarterOffsetLocal;
            }
        }

        private void HandleFirstIntroduction(SpeciesDataGPU species)
        {
            if (species == null) return;
            if (_introCamera == null || _followProxy == null) return;
            if (_playing != null) return; // a shot is already playing — let it finish

            _playing = species;
            StartCoroutine(FollowInRoutine(species));
        }

        // A freshly introduced species has exactly one school, at sub-group index 0.
        private const int FirstSchoolIndex = 0;

        private IEnumerator FollowInRoutine(SpeciesDataGPU species)
        {
            // Make sure the camera is framing with the currently-selected mode's offset.
            ApplyFramingOffset();

            // 1a) Wait for the freshly spawned school to become readable, then snap the proxy onto it. Buffers
            //     were just rebuilt, so the first read can miss by a frame or two — retry briefly.
            bool seeded = false;
            for (int i = 0; i < _maxSeedWaitFrames && !seeded; i++)
            {
                if (_simulation.TryGetSchoolCentroidAndVelocity(
                        species, FirstSchoolIndex, out Vector3 pos, out Vector3 vel))
                {
                    _lastReadCentroid = pos;
                    _schoolVelocity = vel; // seed raw — there is no earlier reading to smooth against yet
                    _smoothVel = vel;      // start with the fish's momentum, not from a standstill
                    _timeSinceRead = 0f;
                    _followProxy.position = pos;
                    seeded = true;
                }
                else yield return null;
            }

            // If we never got a position (buffers not ready / species vanished), abort without hijacking the
            // camera — the Brain stays on the overview shot.
            if (!seeded)
            {
                _playing = null;
                yield break;
            }

            // 1b) Watch the school for a few frames to read a STABLE (averaged) entry heading, and KEEP the
            //     proxy glued to the fish the whole time. That last part is the fix for the "move then change
            //     direction abruptly" glitch: if the proxy sat frozen at the gate while the fish swam off, the
            //     shot would cut in aimed at the empty gate and then lurch to catch up. Gluing it means the
            //     camera cuts in already on the fish, moving with them.
            for (int i = 0; i < _entrySettleFrames; i++)
            {
                yield return null;
                if (_simulation.TryGetSchoolCentroidAndVelocity(
                        species, FirstSchoolIndex, out Vector3 pos, out Vector3 vel))
                {
                    _lastReadCentroid = pos;
                    _schoolVelocity = BlendVelocity(_schoolVelocity, vel, Time.deltaTime);
                    _timeSinceRead = 0f;
                    _followProxy.position = pos; // stay on the fish so there's no catch-up when the shot starts
                }
            }

            // Entry heading. Three sources, best first — see ResolveHeading: where the school is HEADED (its
            // swim target), else how it is currently MOVING (measured velocity), else the camera's own facing
            // if it is barely moving at all (milling), so a noisy near-zero heading can't throw the framing.
            _heading = ResolveHeading(species);

            // 2) Pick the side to sit on: the side of the school facing where the camera is coming from, so the
            //    shot moves TOWARD the fish rather than swinging across to the far side. Then bake the framing
            //    into a FIXED world offset (school-local offset rotated by the entry heading). From here the
            //    camera holds this spot and only TURNS (aim damping) to keep the fish framed as they swim in.
            Vector3 camHome = CameraPosition();
            Vector3 schoolRight = Vector3.Cross(Vector3.up, _heading).normalized;
            _sideSign = Vector3.Dot(camHome - _followProxy.position, schoolRight) >= 0f ? 1f : -1f;

            Vector3 localOffset = SelectedOffset();
            localOffset.x *= _sideSign;
            if (_introFollow != null)
                _introFollow.FollowOffset = Quaternion.LookRotation(_heading, Vector3.up) * localOffset;

            // 3) Cut priority up — the Brain blends the real camera over to the intro shot (moving toward the
            //    fish and turning onto them, which reads as the chosen angle).
            _introCamera.Priority = _activePriority;

            // 4) Follow the live fish centre in. The GPU readback is throttled (a stall), so between reads the
            //    proxy is carried forward on the school's own velocity — continuous, and still on the fish.
            float elapsed = 0f;
            int frame = 0;
            while (elapsed < _followDuration)
            {
                _timeSinceRead += Time.deltaTime;

                if (frame % _readbackEveryNFrames == 0 &&
                    _simulation.TryGetSchoolCentroidAndVelocity(
                        species, FirstSchoolIndex, out Vector3 pos, out Vector3 vel))
                {
                    // On a failed readback the centre and the velocity both keep their last values and the
                    // extrapolation below simply carries on from them (capped), so a dropped read costs nothing.
                    _schoolVelocity = BlendVelocity(_schoolVelocity, vel, _timeSinceRead);
                    _lastReadCentroid = pos;
                    _timeSinceRead = 0f;
                }

                // Aim point: where the fish are NOW (last read + however far they have swum since, capped in
                // case reads dry up), pushed further along their heading to offset the damping below.
                // SmoothDamp settles one smoothing time behind whatever it chases, so leading by that much
                // would cancel the delay exactly — at a STEADY speed. Entering fish sprint and then slow, and
                // the velocity reading lags that change, so a full lead overshoots ahead of them as they
                // settle. _leadStrength scales it back to something that holds through both phases.
                float stale = Mathf.Min(_timeSinceRead, _maxExtrapolationTime);
                float lead  = _followSmoothTime * _leadStrength + _velocityLeadTime;
                Vector3 aimPoint = _lastReadCentroid + _schoolVelocity * (stale + lead);

                // Damping the POSITION is what filters the centroid's jitter (it wobbles — it is an average
                // over a school of milling fish, sampled in steps). Because the aim point above is led, this
                // costs no lag: it smooths without falling behind.
                _followProxy.position = _followSmoothTime > 0f
                    ? Vector3.SmoothDamp(_followProxy.position, aimPoint, ref _smoothVel, _followSmoothTime)
                    : aimPoint;

                frame++;
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 5) Release — Priority back down, Brain blends home to the overview shot.
            _introCamera.Priority = _idlePriority;
            _playing = null;
        }

        // The direction the shot frames against — the axis the school-local framing offset is rotated onto,
        // captured ONCE per shot. Three sources, best first:
        //
        //   1. The school's swim TARGET. This is where the fish are GOING, which is what the framing should
        //      be built on. Their heading as the shot opens is not that: every new fish is spawned pointing
        //      at the simulation centre, then steers onto a path whose centre / size / yaw / height are
        //      randomised per school — so a broadside baked from the opening heading skews away over the
        //      shot by an amount that differs every time. That is the "my side view came out 3/4" case.
        //   2. The measured velocity, when there is no target yet (or the school is already sitting on it,
        //      so the school->target vector is too short to mean anything).
        //   3. The camera's own flattened facing, when the school is barely moving — a near-zero velocity
        //      is direction noise, and assuming they swim roughly the way the camera looks beats framing
        //      against a random axis.
        private Vector3 ResolveHeading(SpeciesDataGPU species)
        {
            if (_aimHeadingAtTarget && _simulation != null)
            {
                EcosystemTargetGPU target = _simulation.GetSchoolTarget(species, FirstSchoolIndex);
                if (target != null)
                {
                    Vector3 toTarget = target.transform.position - _followProxy.position;
                    if (toTarget.magnitude >= MinTargetHeadingDistance)
                        return toTarget.normalized;
                }
            }

            if (_schoolVelocity.magnitude >= _minHeadingSpeed && _schoolVelocity.sqrMagnitude > 1e-6f)
                return _schoolVelocity.normalized;

            return CameraForwardFlattened();
        }

        // Exponential blend of the measured school velocity, framerate-independent. Steadies the reading (a
        // school's mean velocity wobbles as fish mill about) WITHOUT damping the position, so it costs no
        // tracking accuracy — the proxy still lands exactly on the centroid every time a read comes in.
        private Vector3 BlendVelocity(Vector3 current, Vector3 fresh, float deltaTime)
        {
            if (_velocitySmoothTime <= 0f || deltaTime <= 0f) return fresh;
            return Vector3.Lerp(current, fresh, 1f - Mathf.Exp(-deltaTime / _velocitySmoothTime));
        }

        // Where the camera is coming from when a shot starts — the live Brain output (Main Camera). Used to
        // decide which side of the school to sit on. Falls back to the intro camera's own transform.
        private Vector3 CameraPosition()
        {
            Camera cam = Camera.main;
            if (cam != null) return cam.transform.position;
            return _introCamera != null ? _introCamera.transform.position : transform.position;
        }

        // The camera's facing, flattened to horizontal — the fallback heading when a school is seeded but
        // hasn't started moving yet (assume it swims roughly into the scene, the way the camera looks).
        private Vector3 CameraForwardFlattened()
        {
            Camera cam = Camera.main;
            Vector3 fwd = cam != null ? cam.transform.forward : Vector3.forward;
            fwd.y = 0f;
            return fwd.sqrMagnitude > 1e-4f ? fwd.normalized : Vector3.forward;
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────
// SCENE WIRING CHECKLIST (do once, in the editor, in SCENE_MainScene):
//
//  1. Package Manager → Unity Registry → install "Cinemachine" (3.x).
//  2. Main Camera → Add Component → CinemachineBrain. Set Default Blend = EaseInOut, ~1.5–2s.
//  3. Create a CinemachineCamera "CM_Overview" framing the tank as it looks now. Priority = 10 (the idle
//     level; must sit BETWEEN this director's _idlePriority (0) and _activePriority (20)).
//  4. Create a CinemachineCamera "CM_Introduce". Priority = 0. Add CinemachineFollow + CinemachineRotationComposer.
//     Binding Mode / Follow Offset / aim Damping don't need setting in the editor — the director forces Binding
//     Mode = World Space and pushes the aim damping at runtime, and each shot computes the Follow Offset from
//     the school's entry heading + near side. So the framing is fish-relative but the camera turns to follow.
//  5. Create an empty GameObject "IntroFollowProxy". Set CM_Introduce's Tracking Target to it.
//  6. Drop this IntroductionCameraDirectorGPU on any host object; wire _simulation, _introCamera (CM_Introduce),
//     and _followProxy (IntroFollowProxy). Pick a Framing Mode. Leave _simulation empty to auto-find.
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────
