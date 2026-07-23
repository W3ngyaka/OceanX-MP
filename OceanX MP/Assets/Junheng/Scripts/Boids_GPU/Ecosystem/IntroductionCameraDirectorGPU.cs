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
    /// SMOOTH FOLLOW: the raw GPU readback is throttled (a synchronous stall — don't do it every frame) and
    /// the school centroid also jitters as fish mill about. So instead of snapping the proxy to each read,
    /// the director SmoothDamps the proxy toward the latest read EVERY frame. That gives continuous, non-
    /// stepped motion regardless of readback rate — the CinemachineCamera then Follows / LookAts the proxy.
    ///
    /// FRAMING MODE: <see cref="_framingMode"/> picks where the camera sits relative to the school —
    /// follow-behind (over their backs), a full side profile, or a 3/4 angle. The offset is authored in the
    /// SCHOOL'S frame, then at shot start it's rotated by the direction the fish ENTER on and by which side
    /// faces the camera, and baked into a fixed WORLD offset. So the framing is fish-relative (a side view is
    /// a real broadside from whichever gate they use, on the near side), but the camera then HOLDS that spot
    /// and simply TURNS to keep the fish framed as they swim in (aim damping) — rather than riding locked in
    /// their frame, which read as static. All set from code, so every scene gets it at runtime.
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

        [Header("Smoothing")]
        [Tooltip("How gently the proxy chases the fish. Higher = smoother but laggier. ~0.15-0.35 feels good. " +
                 "This is what removes the stutter from the stepped GPU readback and the school's own jitter.")]
        [Min(0f)]
        [SerializeField] private float _positionSmoothTime = 0.25f;

        [Tooltip("Read the fish position back from the GPU every N frames (1 = every frame). The readback is " +
                 "a synchronous GPU stall, so keep this at 3-5; the per-frame SmoothDamp hides the stepping.")]
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

        [Tooltip("Below this speed (m/s) the freshly spawned school counts as 'milling', and the shot falls " +
                 "back to the camera's own facing to choose the entry angle instead of a noisy heading.")]
        [Min(0f)]
        [SerializeField] private float _minHeadingSpeed = 0.4f;

        // The intro camera's position + aim stages — cached so we can push the framing offset / aim damping on.
        private CinemachineFollow _introFollow;
        private CinemachineRotationComposer _introComposer;

        // The species whose shot is currently playing, or null when idle. Guards against overlapping shots
        // (a second introduction while one is running is ignored rather than fighting over the proxy/camera).
        private SpeciesDataGPU _playing;

        // Smoothing state: the raw target we chase, the smoothed position, and SmoothDamp's velocity memory.
        private Vector3 _targetPos;
        private Vector3 _smoothVel;

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
            ApplyFramingOffset();

            if (_introCamera != null)
                _introCamera.Priority = _idlePriority;
        }

        // The offset is written in WORLD space, but it's COMPUTED each shot from the school's entry heading
        // (see FollowInRoutine), so the framing is fish-relative at entry yet the camera then holds that spot
        // and turns to the fish as they swim — instead of riding locked in their frame (which felt static).
        // Also push the aim damping so that "turn to the fish" is smooth. Done in code so every scene matches.
        private void ConfigureFollowBinding()
        {
            if (_introFollow != null)
                _introFollow.TrackerSettings.BindingMode = BindingMode.WorldSpace;
            if (_introComposer != null)
                _introComposer.Damping = new Vector2(_aimDamping, _aimDamping);
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
                if (_simulation.TryGetSchoolCentroid(species, FirstSchoolIndex, out Vector3 pos))
                {
                    _targetPos = pos;
                    _followProxy.position = pos;
                    _smoothVel = Vector3.zero;
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
            Vector3 headingAccum = Vector3.zero;
            Vector3 prevPos = _followProxy.position;
            for (int i = 0; i < _entrySettleFrames; i++)
            {
                yield return null;
                if (_simulation.TryGetSchoolCentroid(species, FirstSchoolIndex, out Vector3 pos))
                {
                    headingAccum += pos - prevPos;
                    prevPos = pos;
                    _targetPos = pos;
                    _followProxy.position = pos; // stay on the fish so there's no catch-up when the shot starts
                    _smoothVel = Vector3.zero;
                }
            }

            // Averaged heading; fall back to the camera's own facing if the school barely moved (milling), so
            // a noisy near-zero heading doesn't throw the entry angle off.
            float minTravel = _minHeadingSpeed * _entrySettleFrames * Mathf.Max(Time.deltaTime, 1e-4f);
            _heading = headingAccum.magnitude >= minTravel && headingAccum.sqrMagnitude > 1e-6f
                ? headingAccum.normalized
                : CameraForwardFlattened();

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

            // 4) Follow the live fish centre in. The GPU readback is throttled (a stall), but the proxy is
            //    SmoothDamped toward the latest read EVERY frame, so the motion is continuous, not stepped.
            float elapsed = 0f;
            int frame = 0;
            while (elapsed < _followDuration)
            {
                if (frame % _readbackEveryNFrames == 0 &&
                    _simulation.TryGetSchoolCentroid(species, FirstSchoolIndex, out Vector3 pos))
                {
                    _targetPos = pos; // keep last target on a failed readback
                }

                // The camera holds its entry spot (fixed world offset) and follows the school's POSITION; the
                // composer's aim damping turns it to keep the fish framed as they swim. No per-frame reframing —
                // that's what made it feel locked/static before.
                _followProxy.position = Vector3.SmoothDamp(
                    _followProxy.position, _targetPos, ref _smoothVel, _positionSmoothTime);

                frame++;
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 5) Release — Priority back down, Brain blends home to the overview shot.
            _introCamera.Priority = _idlePriority;
            _playing = null;
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
