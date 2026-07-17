using System.Collections;
using Unity.Cinemachine;
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
    /// follow-behind (looking into the scene over their backs), a full side profile, or a 3/4 angle. Each
    /// mode has its own Follow Offset below; the director writes the chosen one onto the intro camera's
    /// CinemachineFollow at startup and whenever the mode changes in the Inspector.
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

        [Tooltip("FollowBehind offset: camera behind the school looking into the scene. Tuned for this scene " +
                 "(fish swim +X toward the reef), so the big number is on -X.")]
        [SerializeField] private Vector3 _followBehindOffset = new Vector3(-8f, 2.5f, -4f);

        [Tooltip("SideView offset: camera to the side for a full broadside profile. Big number on -Z so the " +
                 "camera looks across the +X-travelling fish, reef (+Z) behind them.")]
        [SerializeField] private Vector3 _sideViewOffset = new Vector3(0f, 2f, -9f);

        [Tooltip("ThreeQuarter offset: a blend — angled behind AND to the side.")]
        [SerializeField] private Vector3 _threeQuarterOffset = new Vector3(-5f, 2.5f, -7f);

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

        // The intro camera's position stage — cached so we can push the framing offset onto it.
        private CinemachineFollow _introFollow;

        // The species whose shot is currently playing, or null when idle. Guards against overlapping shots
        // (a second introduction while one is running is ignored rather than fighting over the proxy/camera).
        private SpeciesDataGPU _playing;

        // Smoothing state: the raw target we chase, the smoothed position, and SmoothDamp's velocity memory.
        private Vector3 _targetPos;
        private Vector3 _smoothVel;

        private void OnEnable()
        {
            if (_simulation == null)
                _simulation = FindAnyObjectByType<EcosystemSimulationGPU>();

            if (_simulation != null)
                _simulation.OnSpeciesFirstIntroduced += HandleFirstIntroduction;

            CacheFollow();
            ApplyFramingOffset();

            if (_introCamera != null)
                _introCamera.Priority = _idlePriority;
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
            if (_introFollow == null && _introCamera != null)
                _introFollow = _introCamera.GetComponent<CinemachineFollow>();
        }

        // Writes the offset for the current framing mode onto the intro camera's CinemachineFollow.
        private void ApplyFramingOffset()
        {
            if (_introFollow == null) return;
            _introFollow.FollowOffset = SelectedOffset();
        }

        private Vector3 SelectedOffset()
        {
            switch (_framingMode)
            {
                case FramingMode.FollowBehind: return _followBehindOffset;
                case FramingMode.SideView:     return _sideViewOffset;
                default:                       return _threeQuarterOffset;
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

            // 1) Seed the proxy at the school's entry position. The buffers were just rebuilt, so the first
            //    readback can miss by a frame or two — retry briefly before giving up.
            bool seeded = false;
            for (int i = 0; i < _maxSeedWaitFrames; i++)
            {
                if (_simulation.TryGetSchoolCentroid(species, FirstSchoolIndex, out Vector3 seedPos))
                {
                    _targetPos = seedPos;
                    _followProxy.position = seedPos; // snap to the gate — no smoothing on the very first frame
                    _smoothVel = Vector3.zero;
                    seeded = true;
                    break;
                }
                yield return null;
            }

            // If we never got a position (buffers not ready / species vanished), abort without hijacking the
            // camera — the Brain stays on the overview shot.
            if (!seeded)
            {
                _playing = null;
                yield break;
            }

            // 2) Cut priority up — the Brain blends the real camera over to the intro shot (the framing
            //    offset above is what reads as the chosen angle).
            _introCamera.Priority = _activePriority;

            // 3) Follow the live fish centre in. The GPU readback is throttled (a stall), but the proxy is
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

                _followProxy.position = Vector3.SmoothDamp(
                    _followProxy.position, _targetPos, ref _smoothVel, _positionSmoothTime);

                frame++;
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 4) Release — Priority back down, Brain blends home to the overview shot.
            _introCamera.Priority = _idlePriority;
            _playing = null;
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
//  4. Create a CinemachineCamera "CM_Introduce". Priority = 0. Add CinemachineFollow (Binding Mode =
//     World Space) + CinemachineRotationComposer. The director overwrites Follow Offset from the framing mode.
//  5. Create an empty GameObject "IntroFollowProxy". Set CM_Introduce's Tracking Target to it.
//  6. Drop this IntroductionCameraDirectorGPU on any host object; wire _simulation, _introCamera (CM_Introduce),
//     and _followProxy (IntroFollowProxy). Pick a Framing Mode. Leave _simulation empty to auto-find.
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────
