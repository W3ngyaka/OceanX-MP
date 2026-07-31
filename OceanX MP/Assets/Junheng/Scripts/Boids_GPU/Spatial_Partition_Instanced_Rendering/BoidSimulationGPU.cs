using System;
using OceanX.BoidsCPU;
using UnityEngine;

namespace OceanX.BoidsGPU.SpatialPartitionInstancedRendering
{
    /// <summary>
    /// Script that controls the boids simulation that is executed on the GPU. This version
    /// of the simulation uses a spatial partition method to sort the boids into cells first and then
    /// only compare boids from neighboring cells, with instanced rendering of static meshes.
    /// This version is the most optimized version of the simulation.
    /// 
    /// Small clarification of terms:
    /// Spatial Partition --> Splitting the simulation area into a grid of cells and placing each boid into an 
    /// appropriate cell based on its position inside the grid. This is an optimization technique that limits the number
    /// of distance comparisons that need to be done during simulation since boid only checks its neighboring boids now.
    /// Instanced Rendering --> The fish are not instantiated in the scene. Instead, they're just static meshes that are being rendered 
    /// on the appropriate positions using static mesh instancing. The simulation compute shader updates the properties of each boid and the
    /// render shader uses that same data to correctly render the boids at the correct world position, rotation, etc. This removes the
    /// need of fetching the data from the GPU back to the CPU just to issue render calls and update objects in the scene.
    /// </summary>
    [ExecuteAlways]
    public class BoidSimulationGPU : BoidSimulationBaseGPU
    {
        [SerializeField] private SpatialPartitionGPU _spatialPartitionGPU = null;
        [SerializeField] private BoidSpawnerGPU[] _gpuBoidSpawners = null;

        [Tooltip("Baked distance field of the real reef geometry (OceanX > Bake Reef SDF). When present, " +
                 "fish steer off the actual rock and coral surface instead of the box obstacle affecters, " +
                 "which is both more accurate and cheaper. Leave empty — or leave it unbaked — to fall " +
                 "back to the affecters.")]
        [SerializeField] private ReefSDFVolume _reefSDF = null;

        [Header("Options: ")]
        [Tooltip("Should the fish school settings be updated every frame or not. This is " +
            "useful when tweaking the behavior settings of the fish species.")]
        [SerializeField] private bool _updateSchoolSettingsEveryFrame = false;

        [Tooltip("How long (seconds) a newly added fish keeps sprinting AFTER it swims into the " +
            "simulation from its off-screen spawn point, before settling to cruising speed. Fish " +
            "always sprint while still outside the bounds; this only controls the extra momentum " +
            "carried once they cross in. 0 = drop to cruising speed the instant they enter.")]
        [Min(0f)]
        [SerializeField] private float _entryBoostDuration = 1.5f;

        [Tooltip("How close (metres) an exiting fish must get to its parked exit point before it STOPS " +
            "there instead of overshooting and circling the point. Keep this at or below the ecosystem's " +
            "Exit Arrival Radius so a stopped fish still counts as arrived and its school gets culled.")]
        [Min(0.1f)]
        [SerializeField] private float _exitStopDistance = 1.5f;

        [Tooltip("How quickly the ray wing shader's tail sway eases toward the boid's current turn each " +
            "second (frame-rate independent). Lower = a slower, floatier tail that trails further behind " +
            "the turn; higher = a snappier, more immediate tail. Only affects OceanX/Ray_Wing_Lit_Instanced.")]
        [Min(0.01f)]
        [SerializeField] private float _tailSwayResponsiveness = 4f;

        [Tooltip("How fast the reef penetration backstop may turn a fish's heading when it gets pushed " +
            "back out of rock/coral, as a MULTIPLE of that species' max angular velocity. This is the " +
            "emergency turn that used to be instant — the up/down 'snap' seen near flat rock tops and the " +
            "seabed. A finite value eases it out over a few frames. Lower = smoother but a fish may scrape " +
            "along the surface a little longer; higher = snappier. ~2-4 is the usable range. Tunable live " +
            "in Play mode.")]
        [Min(0.1f)]
        [SerializeField] private float _reefBackstopTurnMultiplier = 3f;

        [Header("Moray serpentine body")]
        [Tooltip("Number of head-path samples kept per moray (the species with UseSpineDeformation). The trail " +
                 "auto-spaces itself to cover the whole body (see below), so more samples = a SMOOTHER body, " +
                 "not a longer one. Changing this only takes effect after a buffer rebuild (add/remove).")]
        [Min(2)]
        [SerializeField] private int _moraySpineTrailSampleCount = 48;

        [Tooltip("Metres of head travel between recorded trail samples. 0 = AUTO: derive it from the body " +
                 "length so the trail always spans the whole eel (bodyLength × 1.15 ÷ (samples-1)). Only set a " +
                 "value to override. Fixed for a buffer's lifetime (rebuild to change).")]
        [Min(0f)]
        [SerializeField] private float _moraySpineTrailSpacingOverride = 0f;

        [Tooltip("Object-space head→tail length of the moray mesh. 0 = auto from the mesh bounds (size along " +
                 "local Z). Set explicitly only if the auto value looks wrong.")]
        [SerializeField] private float _moraySpineBodyLengthOverride = 0f;

        [Tooltip("Object-space Z of the moray's head tip. 0 = auto from the mesh bounds (max Z; assumes the " +
                 "model faces +Z like the other fish). Set explicitly if the head is not at +Z.")]
        [SerializeField] private float _moraySpineHeadLocalZOverride = 0f;

        [Tooltip("Lateral serpentine swim wave, in METRES of sway at the tail (0 at the head). This is the " +
                 "moray's actual swimming undulation when going straight. For a ~12-unit eel try 1.5-3. " +
                 "0 = pure path following (no swim wag). Live-tunable.")]
        [Min(0f)]
        [SerializeField] private float _moraySpineUndulationAmplitude = 2f;

        [Tooltip("Number of full wavelengths along the body. A moray shows roughly ONE wave - keep this near " +
                 "1 (2+ looks buzzy/framey). Live-tunable.")]
        [Min(0f)]
        [SerializeField] private float _moraySpineUndulationWaves = 1f;

        [Tooltip("Beat speed (cycles/sec) of the swim undulation. Live-tunable.")]
        [Min(0f)]
        [SerializeField] private float _moraySpineUndulationSpeed = 1.2f;

        [Tooltip("Fraction of the body (from the head) kept still, where the swim sway begins ramping in. " +
                 "Smaller = the wave starts closer to the head; larger = a longer still 'neck'. ~0.03 starts " +
                 "it just behind the head. Live-tunable.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _moraySpineUndulationHeadHold = 0.03f;

        [Tooltip("Extra arclength (metres) each side used to estimate a body slice's facing direction. " +
                 "0 = TIGHT: the body follows the local curve and genuinely bends along the path (crisp, but " +
                 "can jitter when the head is shoved off a rock). Raise a LITTLE (try 0.3-0.8 on a ~12-unit " +
                 "eel) to low-pass that jitter. Too high decouples orientation from the curve and the body " +
                 "flattens to a stiff plank. Live-tunable.")]
        [Min(0f)]
        [SerializeField] private float _moraySpineSmoothingWindow = 0f;

        // Trail coverage margin: the auto-spaced trail spans this multiple of the body length, so the tail
        // always has recorded path to sit on even with a little slack.
        private const float MorayTrailCoverageMargin = 1.15f;

        [Tooltip("Flip the moray's normals. The giant-moray FBX looks like a mirrored / reverse-wound import " +
                 "(it also needs the shader's Cull Off), which leaves its lit surface facing INWARD - dark, " +
                 "'absorbing the scene'. ON corrects the lighting. Turn OFF if the eel instead looks correctly " +
                 "lit without it. Live-tunable.")]
        [SerializeField] private bool _moraySpineFlipNormals = true;

        [Tooltip("DEBUG: render the moray as the OLD rigid straight eel (LookRotation about the centre, no " +
                 "spine) so you can A/B compare against the path-following body. Live-tunable.")]
        [SerializeField] private bool _moraySpineDebugStraight = false;

        [Header("Moray Cave Rest Pose + Mouth (MorayCaveDirector drives the anchors)")]
        [Tooltip("MATCH radius (m): how near the head must be to a cave anchor to bind to it. Make it larger " +
                 "than the eel's loiter/mill radius so the pose doesn't flicker while it idles at the mouth. " +
                 "The pose intensity is driven by the director's ramp, NOT by this distance.")]
        [Min(0.1f)]
        [SerializeField] private float _moraySpineRestRadius = 10f;

        [Tooltip("Radius (m) of the resting body's horizontal coil. Smaller = tighter curl tucked in the " +
                 "cave; larger = a looser, straighter body. Tune in Play mode.")]
        [Min(0.05f)]
        [SerializeField] private float _moraySpineRestCurlRadius = 3f;

        [Tooltip("Fraction of the swim sway kept while fully resting (0 = dead still, ~0.15 = a faint breath).")]
        [Range(0f, 1f)]
        [SerializeField] private float _moraySpineRestUndulationScale = 0.15f;

        [Tooltip("Max jaw-open angle in DEGREES while resting (buccal breathing). 0 = mouth stays shut. " +
                 "Geometric jaw (no mesh mask): opens verts near the head tip and below Hinge Y. Defaults are " +
                 "scaled to the giant-moray mesh (body ~12 units, head tip Z 5.07, head Y range -0.28..1.19).")]
        [Range(0f, 60f)]
        [SerializeField] private float _moraySpineMouthMaxAngle = 18f;

        [Tooltip("Breaths per second (jaw open/close cycles).")]
        [Min(0f)]
        [SerializeField] private float _moraySpineMouthRate = 0.5f;

        [Tooltip("Object-space length back from the head tip (Z) that counts as jaw, in MESH units. The moray " +
                 "body is ~12 units long, so the jaw is a couple of units - not a fraction. Tune so only the " +
                 "mouth opens, not the throat.")]
        [Min(0f)]
        [SerializeField] private float _moraySpineMouthLength = 2f;

        [Tooltip("Object-space Y below which vertices are the lower jaw, in MESH units. The head spans Y " +
                 "-0.28..1.19; ~0.45 is the midline (lower half opens). Raise to catch more of the jaw, lower " +
                 "to open only the very bottom. Tune in Play mode.")]
        [SerializeField] private float _moraySpineMouthHingeY = 0.45f;

        // Cave rest anchors, refreshed each frame by MorayCaveDirector via SetMorayRestAnchors. Fixed-length
        // (matches the shader's MORAY_MAX_REST_ANCHORS); only the first _morayRestAnchorCount are live.
        private const int MorayMaxRestAnchors = 8;
        private readonly Vector4[] _morayRestAnchorPos = new Vector4[MorayMaxRestAnchors];
        private readonly Vector4[] _morayRestAnchorDir = new Vector4[MorayMaxRestAnchors];
        private int _morayRestAnchorCount = 0;

        /// <summary>
        /// Publishes the active moray cave rest anchors for this frame (called by MorayCaveDirector). Each
        /// anchor is a cave mouth: <paramref name="anchorPos"/>.xyz = where the head sits, <paramref
        /// name="anchorDir"/>.xyz = the head-out direction (body trails along -dir into the rock). A moray
        /// whose head is near an anchor lays into the rock and gapes; count 0 = no resting this frame.
        /// </summary>
        public void SetMorayRestAnchors(Vector4[] anchorPos, Vector4[] anchorDir, int count)
        {
            int n = Mathf.Clamp(count, 0, MorayMaxRestAnchors);
            for (int i = 0; i < MorayMaxRestAnchors; i++)
            {
                _morayRestAnchorPos[i] = (anchorPos != null && i < anchorPos.Length) ? anchorPos[i] : Vector4.zero;
                _morayRestAnchorDir[i] = (anchorDir != null && i < anchorDir.Length) ? anchorDir[i] : Vector4.zero;
            }
            _morayRestAnchorCount = n;
        }

        // Runtime override of the moray's obstacle-avoidance range (< 0 = no override, use the species value).
        // MorayCaveDirector drops this to 0 while the moray is path-following into a cave so it hugs the
        // authored route and clips slightly into the reef, and clears it (normal avoidance) while roaming.
        private float _morayAvoidanceOverride = -1f;

        /// <summary>Override the moray's obstacle-avoidance range for this frame; pass a negative value to
        /// clear the override and use the species' authored range. Called by MorayCaveDirector.</summary>
        public void SetMorayAvoidanceOverride(float range) => _morayAvoidanceOverride = range;

        private ComputeBuffer _sortedBoidsComputeBuffer = null;
        private ComputeBuffer _boidSchoolsRenderInfoBuffer = null;

        // ---- Moray serpentine trail buffers (path-following body) ----------------------------------
        // Persistent (NOT ping-ponged) ring of recent head positions the kernel appends to and the moray
        // render shader reads. Created/seeded in InitializeComputeShaderData, released on rebuild/destroy.
        private ComputeBuffer _morayTrailBuffer       = null;
        private ComputeBuffer _morayTrailCursorBuffer = null;
        private BoidSpawnerGPU _moraySpawner = null; // the active UseSpineDeformation spawner, or null
        private int _morayGroupId      = -1;         // -1 => no moray active this build
        private int _morayBufferOffset = 0;
        private int _morayCount        = 0;
        // Derived once per rebuild and pushed to BOTH the kernel and the material so they never disagree.
        private float _morayTrailSpacing = 0.1f;
        private float _morayHeadLocalZ   = 0f;
        private float _morayBodyLength    = 1f;

        private bool _sortedBoidsBufferIsOutput = false;
        private BoidRenderInfoGPU[] _boidSchoolsRenderInfos = null;

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            CleanUpComputeBuffer(ref _sortedBoidsComputeBuffer);
            CleanUpComputeBuffer(ref _boidSchoolsRenderInfoBuffer);
            CleanUpComputeBuffer(ref _morayTrailBuffer);
            CleanUpComputeBuffer(ref _morayTrailCursorBuffer);
        }

        // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
        /// <summary>
        /// Tears down all GPU compute buffers (derived class, base class, and spatial partition),
        /// clears the cached boid data on every spawner, then re-runs the full initialization
        /// chain. Call this after changing a spawner's InitialGroupsCount at runtime so the GPU
        /// reflects the new group configuration.
        /// </summary>
        public void ReinitializeBuffers()
        {
            // Deferred on purpose: the actual teardown/rebuild runs at the START of the next Update (see
            // FlushPendingReinitialize), never mid-frame. If we disposed the compute buffers here, a
            // RenderMeshIndirect draw already submitted this frame would still reference them via its
            // MaterialPropertyBlock, and the GPU would throw "Fish_Lit_Instanced requires a buffer (SRV)
            // _Boids ... none provided" when it executes at end of frame. Callers just request a rebuild.
            _reinitializePending = true;
        }

        // True while a rebuild has been requested but not yet applied. Coalesces multiple requests made in
        // one frame (e.g. several adds draining the op queue) into a single rebuild.
        private bool _reinitializePending = false;

        // Applies a pending ReinitializeBuffers request. Called at the top of Update, before any draw is
        // submitted this frame, so buffers are only ever torn down/recreated between frames.
        private void FlushPendingReinitialize()
        {
            if (!_reinitializePending) return;
            _reinitializePending = false;
            PerformReinitializeBuffers();
        }

        /// <inheritdoc/>
        protected override void Update()
        {
            // Rebuild BEFORE base.Update() runs the simulation and submits this frame's draws, so the sim
            // and render always use freshly created buffers and no in-flight draw references a disposed one.
            FlushPendingReinitialize();
            base.Update();
        }

        // Performs the actual buffer teardown + full re-init. Do not call directly — request a rebuild via
        // ReinitializeBuffers() so it happens at a frame-safe point.
        private void PerformReinitializeBuffers()
        {
            // Before tearing anything down, read the live boid positions back from the GPU
            // so each spawner can restore its fish to where they currently are instead of
            // teleporting them to fresh spawn positions.
            //
            // Two subtleties:
            //   1. Read from the buffer that was the LAST render output, which alternates each
            //      frame. After each UpdateSimulation the flag is toggled, so the last output is
            //      in _boidsComputeBuffer when the flag is now false, and in
            //      _sortedBoidsComputeBuffer when it is now true.
            //   2. Use spawner.Boids.Length (the live local array) for the slice size — NOT
            //      SpawnData.BoidsCount, which may already reflect the new target count because
            //      EcosystemSimulationGPU calls SetBoidsCount BEFORE ReinitializeBuffers.
            //   3. Only read back when there are real boids. Coming out of the empty-ocean state
            //      _boidsInfos is a zero-length array and ComputeBuffer.GetData rejects empty arrays
            //      (the buffers themselves are size-1 placeholders). Nothing to preserve anyway.
            if (_boidsCount > 0 && _boidsComputeBuffer != null && _boidsInfos != null)
            {
                ComputeBuffer readBuffer = _sortedBoidsBufferIsOutput
                    ? _sortedBoidsComputeBuffer
                    : _boidsComputeBuffer;

                if (readBuffer != null)
                {
                    readBuffer.GetData(_boidsInfos);
                    foreach (BoidSpawnerGPU spawner in _gpuBoidSpawners)
                    {
                        // Skip spawners going inactive this rebuild (extinct): they won't SpawnBoids,
                        // so preserving their positions would only leave stale data to be wrongly
                        // restored if the species is re-added later.
                        if (!spawner.IsActive) continue;
                        BoidInfoGPU[] oldBoids = spawner.Boids;
                        if (oldBoids == null) continue;
                        int offset = spawner.RenderingOffset;
                        int count  = oldBoids.Length;
                        if (offset + count > _boidsInfos.Length) continue;
                        BoidInfoGPU[] slice = new BoidInfoGPU[count];
                        Array.Copy(_boidsInfos, offset, slice, 0, count);
                        spawner.StorePreservedBoids(slice);
                    }
                }
            }

            // Release this class's buffers.
            CleanUpComputeBuffer(ref _sortedBoidsComputeBuffer);
            CleanUpComputeBuffer(ref _boidSchoolsRenderInfoBuffer);
            // NOTE: the moray trail buffers are deliberately NOT released here. SetupMoraySpineBuffers reuses
            // them when the moray count is unchanged, so the eel's curled body SURVIVES a rebuild (e.g. a
            // population tick adding/removing some OTHER species) instead of snapping straight every time.
            _moraySpawner              = null;
            _morayGroupId              = -1;
            _boidSchoolsRenderInfos    = null;
            _sortedBoidsBufferIsOutput = false;

            // Release the base-class compute buffers and reset cached collections.
            CleanupBaseGPUBuffers();

            // Release spatial partition buffers so InitializeGrid() can run again.
            _spatialPartitionGPU.CleanupGrid();

            // Release each spawner's draw-argument buffer and boid array so SpawnBoids() can recreate them.
            foreach (BoidSpawnerGPU spawner in _gpuBoidSpawners)
                spawner.CleanupSpawnData();

            // Re-run the full initialization chain: SpawnBoids → sort by size → assign IDs →
            // InitializeRenderProperties → InitializeComputeShaderData → InitializeGrid.
            InitializeBoidsSimulation();
        }

        /// <inheritdoc/>
        public override BoidSpawnerBase[] GetBoidSpawners()
        {
            return _gpuBoidSpawners;
        }

        // Hands the baked reef distance field to the kernel. _ReefSDFEnabled is the switch the shader
        // reads: 0 means "no field", and it falls back to steering off the box obstacle affecters, so a
        // scene that was never baked still behaves exactly as it did before.
        //
        // A Texture3D must always be bound even when disabled — an unbound texture in a compute shader is
        // a hard error on some platforms, whether or not the sampling code actually runs.
        private void BindReefSDF()
        {
            bool usable = _reefSDF != null && _reefSDF.IsBaked;
            _boidsComputeShader.SetInt("_ReefSDFEnabled", usable ? 1 : 0);

            if (usable)
            {
                _boidsComputeShader.SetTexture(_boidsKernelId, "_ReefSDF", _reefSDF.Field);
                _boidsComputeShader.SetVector("_ReefSDFMin", _reefSDF.BakedMin);
                _boidsComputeShader.SetVector("_ReefSDFSize", _reefSDF.BakedSize);
                _boidsComputeShader.SetFloat("_ReefSDFVoxelSize", _reefSDF.BakedVoxelSize);
            }
            else
            {
                _boidsComputeShader.SetTexture(_boidsKernelId, "_ReefSDF", GetPlaceholderSDF());
                _boidsComputeShader.SetVector("_ReefSDFMin", Vector3.zero);
                _boidsComputeShader.SetVector("_ReefSDFSize", Vector3.one);
                _boidsComputeShader.SetFloat("_ReefSDFVoxelSize", 1f);
            }
        }

        // A 1x1x1 stand-in bound when there is no baked field. There is no built-in Texture3D equivalent of
        // Texture2D.whiteTexture, and leaving the binding empty is a hard error on some platforms even
        // though the sampling path is switched off. Reports a huge distance, so if it ever WERE sampled it
        // would read as "no reef anywhere near" rather than steering fish into something.
        private Texture3D _placeholderSDF = null;
        private Texture3D GetPlaceholderSDF()
        {
            if (_placeholderSDF != null) return _placeholderSDF;
            _placeholderSDF = new Texture3D(1, 1, 1, TextureFormat.RHalf, false)
            {
                name = "ReefSDF_Placeholder",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _placeholderSDF.SetPixels(new[] { new Color(1e6f, 0f, 0f, 0f) });
            _placeholderSDF.Apply(false, false);
            return _placeholderSDF;
        }

        // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
        /// <summary>
        /// Reads the current GPU positions of the boids in the global buffer range
        /// [<paramref name="startIndex"/>, startIndex + count) and outputs their average (centroid).
        /// Reads from whichever ping-pong buffer holds the latest simulation output. Returns false if
        /// the buffers are not ready or the range is out of bounds. Used by EcosystemSimulationGPU to
        /// detect when a removed school has reached its off-screen exit point.
        /// </summary>
        public bool TryGetBoidsCentroid(int startIndex, int count, out Vector3 centroid)
        {
            centroid = Vector3.zero;
            if (count <= 0 || _boidsCount == 0) return false;
            if (startIndex < 0 || startIndex + count > _boidsCount) return false;

            ComputeBuffer readBuffer = _sortedBoidsBufferIsOutput
                ? _sortedBoidsComputeBuffer
                : _boidsComputeBuffer;
            if (readBuffer == null) return false;

            BoidInfoGPU[] slice = new BoidInfoGPU[count];
            // GetData(dest, destOffset, sourceOffset, count) — read only this school's slice.
            readBuffer.GetData(slice, 0, startIndex, count);

            Vector3 sum = Vector3.zero;
            for (int i = 0; i < count; i++) sum += slice[i].Position;
            centroid = sum / count;
            return true;
        }

        // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
        /// <summary>
        /// Reads the GPU positions of the boids in the global buffer range [<paramref name="startIndex"/>,
        /// startIndex + count) and counts how many are within <paramref name="radius"/> of
        /// <paramref name="point"/>. Used by the removal flow to cull an exiting school the moment every one
        /// of its fish has reached its off-screen exit point (reachedCount == count), with no fixed timer.
        /// Returns false (and reachedCount 0) if the buffers are not ready or the range is out of bounds.
        /// </summary>
        public bool TryCountBoidsWithinRadius(int startIndex, int count, Vector3 point, float radius, out int reachedCount)
        {
            reachedCount = 0;
            if (count <= 0 || _boidsCount == 0) return false;
            if (startIndex < 0 || startIndex + count > _boidsCount) return false;

            ComputeBuffer readBuffer = _sortedBoidsBufferIsOutput
                ? _sortedBoidsComputeBuffer
                : _boidsComputeBuffer;
            if (readBuffer == null) return false;

            BoidInfoGPU[] slice = new BoidInfoGPU[count];
            readBuffer.GetData(slice, 0, startIndex, count);

            float radiusSqr = radius * radius;
            for (int i = 0; i < count; i++)
            {
                // A fish counts as arrived if it is within the radius OR it has stopped dead. The compute
                // shader freezes an exiting fish (speed 0) once it is inside its own capture radius, which
                // for big fast species (shark) is its turning circle — possibly larger than `radius`. No
                // normal fish ever has speed 0 (clamped to cruising speed), so stopped == arrived-at-exit.
                if ((slice[i].Position - point).sqrMagnitude <= radiusSqr || slice[i].Speed <= 0f) reachedCount++;
            }
            return true;
        }

        /// <inheritdoc/>
        protected override void SpawnBoids()
        {
            // Spawn boids only for active species. An inactive (zero-school) spawner is skipped so it
            // produces no boid array and no draw-arguments buffer — it is fully excluded this rebuild.
            foreach (BoidSpawnerGPU gpuBoidSpawner in _gpuBoidSpawners)
            {
                if (!gpuBoidSpawner.IsActive) continue;
                gpuBoidSpawner.SpawnBoids(_simulationAreaBounds);
            }
        }

        /// <inheritdoc/>
        protected override void InitializeBoidsSimulation()
        {
            base.InitializeBoidsSimulation();

            // Initialize the spatial partition of the simulation area.
            _spatialPartitionGPU.InitializeGrid(_simulationAreaBounds, _boidsCount);
        }

        /// <inheritdoc/>
        protected override void InitializeRenderProperties()
        {
            // Build the per-group school + render info only for the active spawners, in BoidGroupId
            // order. _activeSortedSpawners is already ordered by BoidGroupId (0..N-1, dense), so the
            // arrays line up with the group ID packed into each boid and sampled by the shaders.
            int activeCount = _activeSortedSpawners.Count;
            _boidsSchools.Clear();
            _boidSchoolsRenderInfos = new BoidRenderInfoGPU[Mathf.Max(1, activeCount)];
            for (int i = 0; i < activeCount; i++)
            {
                BoidSpawnerGPU gpuBoidSpawner = _activeSortedSpawners[i];
                _boidsSchools.Add(gpuBoidSpawner.SpawnData.FishSchoolProperties);
                _boidSchoolsRenderInfos[i] = ExtractBoidSchoolRenderInfo(gpuBoidSpawner.SpawnData.FishSchoolProperties.MotionRenderProperties);
            }

            // Initialize the compute buffer that will hold the render information for each boid school in a
            // GPU-readable format, so that instanced materials can use it. Sized Mathf.Max(1, ...) so it is
            // never zero-sized when the ocean is empty (the placeholder element is never sampled).
            _boidSchoolsRenderInfoBuffer = new ComputeBuffer(Mathf.Max(1, activeCount), BoidRenderInfoGPU.Size);
            if (activeCount > 0)
            {
                _boidSchoolsRenderInfoBuffer.SetData(_boidSchoolsRenderInfos);
            }
        }

        /// <inheritdoc/>
        protected override void InitializeComputeShaderData()
        {
            base.InitializeComputeShaderData();

            // Mathf.Max(1, ...) so the ping-pong buffer is never zero-sized at empty-ocean start.
            _sortedBoidsComputeBuffer = new ComputeBuffer(Mathf.Max(1, _boidsCount), BoidInfoGPU.Size);
            if (_boidsCount > 0)
            {
                _sortedBoidsComputeBuffer.SetData(_boidsInfos);
            }

            SetupMoraySpineBuffers();
        }

        /// <summary>
        /// Locates the active serpentine species (the moray, flagged UseSpineDeformation), then creates,
        /// seeds and binds its head-path trail ring buffers. Always binds valid buffers even when no moray
        /// is active (_MorayGroupId = -1 disables the kernel append) because an unbound RWStructuredBuffer
        /// is a hard error on some platforms. Called from InitializeComputeShaderData on every (re)build.
        /// </summary>
        private void SetupMoraySpineBuffers()
        {
            _moraySpawner = null;
            _morayGroupId = -1;
            _morayBufferOffset = 0;
            _morayCount = 0;

            foreach (BoidSpawnerGPU spawner in _activeSortedSpawners)
            {
                if (spawner is BoidSpawnerGPUMultiTargets mt && mt.SpeciesData != null && mt.SpeciesData.UseSpineDeformation)
                {
                    _moraySpawner      = spawner;
                    _morayGroupId      = spawner.BoidGroupId;
                    _morayBufferOffset = spawner.RenderingOffset;
                    _morayCount        = spawner.Boids != null ? spawner.Boids.Length : 0;
                    break;
                }
            }

            // Body metrics + auto-spaced trail: span the whole eel across the samples so the tail always has
            // recorded path to sit on. A body-length-derived spacing is what stops a long eel's body
            // collapsing onto its head (a fixed small spacing only covered the front of the body).
            ComputeMorayBodyMetrics(out _morayHeadLocalZ, out _morayBodyLength);
            int K = Mathf.Max(2, _moraySpineTrailSampleCount);
            _morayTrailSpacing = _moraySpineTrailSpacingOverride > 0f
                ? _moraySpineTrailSpacingOverride
                : Mathf.Max(1e-3f, _morayBodyLength * MorayTrailCoverageMargin / (K - 1));

            int trailLen  = Mathf.Max(1, _morayCount * K);
            int cursorLen = Mathf.Max(1, _morayCount);

            // Reuse the existing buffers when they are already the right size (i.e. the moray count did not
            // change across this rebuild). Reusing them PRESERVES the recorded path, so a rebuild triggered
            // by some other species' population tick no longer snaps the eel's body straight. The trail is
            // indexed by moray-local index, which is stable while the count is unchanged (position
            // preservation keeps each eel at the same slot). Only (re)create + straight-seed when the size
            // actually changes: first build, empty<->active, or the moray itself was added/removed.
            bool reuse = _morayTrailBuffer != null && _morayTrailBuffer.count == trailLen
                      && _morayTrailCursorBuffer != null && _morayTrailCursorBuffer.count == cursorLen;

            if (!reuse)
            {
                // PRESERVE the existing head-path across a buffer RECREATE (not just the reuse-in-place case):
                // read the old ring back first. A resting eel's head doesn't move, so if we straight-seed on a
                // recreate its coiled body snaps to a straight T-pose (and never recovers because no new samples
                // are appended). By copying the old path into the surviving moray slots we keep the coil no
                // matter WHY the buffer had to be recreated (count change, group re-sort, etc.). Only genuinely
                // new slots fall back to the straight seed. GetData here is cheap and only runs on a recreate.
                Vector4[] oldTrail = null; int[] oldCursor = null; int oldK = 0, oldCount = 0;
                if (_morayTrailBuffer != null && _morayTrailCursorBuffer != null
                    && _morayTrailBuffer.count > 0 && _morayTrailCursorBuffer.count > 0)
                {
                    oldCount  = _morayTrailCursorBuffer.count;
                    oldK      = _morayTrailBuffer.count / oldCount;
                    oldTrail  = new Vector4[_morayTrailBuffer.count]; _morayTrailBuffer.GetData(oldTrail);
                    oldCursor = new int[oldCount];                    _morayTrailCursorBuffer.GetData(oldCursor);
                }

                CleanUpComputeBuffer(ref _morayTrailBuffer);
                CleanUpComputeBuffer(ref _morayTrailCursorBuffer);
                _morayTrailBuffer       = new ComputeBuffer(trailLen, sizeof(float) * 4);
                _morayTrailCursorBuffer = new ComputeBuffer(cursorLen, sizeof(int));

                // Straight seed: each moray's ring as a STRAIGHT tail receding behind the head along its heading,
                // so a NEW eel renders straight from frame one and relaxes into trailing as the kernel appends.
                // Ring semantics (cursor = 0): the n-th oldest sample lives at ring index (1 - n) mod K and sits
                // n * spacing behind the head, i.e. the straight line pos - dir * (n * spacing).
                if (_morayCount > 0 && _morayGroupId >= 0 && _boidsInfos != null)
                {
                    Vector4[] trailSeed  = new Vector4[trailLen];
                    int[]     cursorSeed = new int[cursorLen];
                    for (int i = 0; i < _boidsInfos.Length; i++)
                    {
                        int gid = BitConverter.SingleToInt32Bits(_boidsInfos[i].BoidID) & 0xFF;
                        if (gid != _morayGroupId) continue;
                        int local = (int)_boidsInfos[i].OriginalIndex - _morayBufferOffset;
                        if (local < 0 || local >= _morayCount) continue;

                        Vector3 p = _boidsInfos[i].Position;
                        Vector3 d = _boidsInfos[i].Direction;
                        d = d.sqrMagnitude < 1e-6f ? Vector3.forward : d.normalized;

                        cursorSeed[local] = 0;
                        for (int n = 1; n <= K; n++)
                        {
                            int idx = (((1 - n) % K) + K) % K;
                            Vector3 sp = p - d * (_morayTrailSpacing * n);
                            trailSeed[local * K + idx] = new Vector4(sp.x, sp.y, sp.z, 1f);
                        }
                    }

                    // Overwrite surviving slots with the PRESERVED path (same local index, same ring length K),
                    // so an existing eel keeps its exact coiled body across the recreate.
                    if (oldTrail != null && oldK == K)
                    {
                        int survive = Mathf.Min(oldCount, _morayCount);
                        for (int local = 0; local < survive; local++)
                        {
                            cursorSeed[local] = oldCursor[local];
                            for (int j = 0; j < K; j++) trailSeed[local * K + j] = oldTrail[local * K + j];
                        }
                    }

                    _morayTrailBuffer.SetData(trailSeed);
                    _morayTrailCursorBuffer.SetData(cursorSeed);
                }
            }

            // Bind + push the params the kernel needs. Spacing/count are fixed for the buffer's lifetime.
            _boidsComputeShader.SetBuffer(_boidsKernelId, "_MorayTrail", _morayTrailBuffer);
            _boidsComputeShader.SetBuffer(_boidsKernelId, "_MorayTrailCursor", _morayTrailCursorBuffer);
            _boidsComputeShader.SetInt("_MorayGroupId", _morayGroupId);
            _boidsComputeShader.SetInt("_MorayBufferOffset", _morayBufferOffset);
            _boidsComputeShader.SetInt("_MorayCount", _morayCount);
            _boidsComputeShader.SetInt("_MorayTrailCount", K);
            _boidsComputeShader.SetFloat("_MorayTrailSpacing", _morayTrailSpacing);
        }

        /// <summary>
        /// Computes the moray body's head-tip local Z and head→tail length, from the inspector overrides if
        /// set (non-zero) otherwise from the mesh bounds (length = bounds size Z, head = bounds max Z, which
        /// assumes the model faces +Z like the other fish).
        /// </summary>
        private void ComputeMorayBodyMetrics(out float headLocalZ, out float bodyLength)
        {
            Mesh mesh = _moraySpawner != null ? _moraySpawner.SpawnData.BoidMesh : null;
            headLocalZ = _moraySpineHeadLocalZOverride != 0f
                ? _moraySpineHeadLocalZOverride
                : (mesh != null ? mesh.bounds.max.z : 0f);
            bodyLength = _moraySpineBodyLengthOverride > 0f
                ? _moraySpineBodyLengthOverride
                : (mesh != null ? mesh.bounds.size.z : 1f);
        }

        /// <inheritdoc/>
        protected override void UpdateSimulation(float timeDelta)
        {
            // Empty ocean (all species extinct / none added yet): nothing to dispatch or draw.
            // The GPU buffers exist (sized to 1) but must never be dispatched or rendered when there
            // are no real boids — dispatching 0 threads / drawing 0 instances is what we avoid here.
            if (_boidsCount == 0) return;

            if (_updateSchoolSettingsEveryFrame)
            {
                // Update the fish school properties (cohesion, separation, alignment and target weight) every frame
                // for each fish species. This is useful when tweaking fish school behavior settings in editor.
                int boidSchoolsCount = _boidsSchools.Count;
                for (int i = 0; i < boidSchoolsCount; i++)
                {
                    _boidSchoolsInfos[i] = ExtractBoidSchoolInfo(_boidsSchools[i]);
                }
                _boidsSchoolsComputeBuffer.SetData(_boidSchoolsInfos);
            }

            // Moray cave AI: apply the director's obstacle-avoidance override on top of the (per-group)
            // school info, whether or not _updateSchoolSettingsEveryFrame rebuilt it this frame. Only the
            // single moray group element is re-uploaded, and only when its value actually changes.
            if (_moraySpawner != null && _morayGroupId >= 0 && _boidSchoolsInfos != null
                && _morayGroupId < _boidSchoolsInfos.Length && _boidsSchoolsComputeBuffer != null)
            {
                float baseRange = _moraySpawner.SpawnData.FishSchoolProperties.ObstacleAvoidanceRange;
                float range     = _morayAvoidanceOverride >= 0f ? _morayAvoidanceOverride : baseRange;
                float rangeSq   = range * range;
                if (_boidSchoolsInfos[_morayGroupId].ObstacleAvoidanceRangeSquared != rangeSq)
                {
                    BoidSchoolInfoGPU info = _boidSchoolsInfos[_morayGroupId];
                    info.ObstacleAvoidanceRangeSquared = rangeSq;
                    _boidSchoolsInfos[_morayGroupId] = info;
                    _boidsSchoolsComputeBuffer.SetData(_boidSchoolsInfos, _morayGroupId, _morayGroupId, 1);
                }
            }

            // Update properties that change every frame to the compute shader.
            _boidsComputeShader.SetFloat("_TimeDelta", timeDelta);
            // Push the entry-sprint duration every frame so it can be tuned live in the Inspector.
            _boidsComputeShader.SetFloat("_EntryBoostDuration", _entryBoostDuration);
            // Push the exit-stop distance too (where a leaving fish halts at its exit point).
            _boidsComputeShader.SetFloat("_ExitStopDistance", _exitStopDistance);
            // Push the ray tail-sway responsiveness so it can be tuned live in the Inspector.
            _boidsComputeShader.SetFloat("_TailSwayResponsiveness", _tailSwayResponsiveness);
            // Push the reef-penetration backstop turn rate so the snap-out speed can be tuned live.
            _boidsComputeShader.SetFloat("_ReefBackstopTurnMultiplier", _reefBackstopTurnMultiplier);
            _boidsComputeShader.SetBuffer(_boidsKernelId, "_Affecters", _affectersComputeBuffer);
            BindReefSDF();

            // Update the affecters data on the GPU to reflect their newly updated position.
            UpdateSimulationAffecters();

            // Sort the boids based on their position inside the simulation area.
            _spatialPartitionGPU.UpdateGridOccupancy(_boidsComputeBuffer, _sortedBoidsComputeBuffer);

            // Dispatch the compute shader to execute another update of the GPU boid simulation.
            _spatialPartitionGPU.SetSpatialPartitionProperties(_boidsComputeShader, _boidsKernelId);
            ComputeBuffer boidsInputDataBuffer = _sortedBoidsBufferIsOutput ? _sortedBoidsComputeBuffer : _boidsComputeBuffer;
            ComputeBuffer boidsOutputDataBuffer = _sortedBoidsBufferIsOutput ? _boidsComputeBuffer : _sortedBoidsComputeBuffer;
            _boidsComputeShader.SetBuffer(_boidsKernelId, "_Boids", boidsInputDataBuffer);
            _boidsComputeShader.SetBuffer(_boidsKernelId, "_BoidsOutput", boidsOutputDataBuffer);

            // Moray cave freeze: hand the kernel the same rest anchors the render pose uses, so a settling
            // moray is eased onto its cave anchor and held there (a boid can't otherwise stop). Bound every
            // frame because the weights ramp; inert when no anchors / no moray (guarded in the kernel).
            _boidsComputeShader.SetVectorArray("_MorayRestAnchorPos", _morayRestAnchorPos);
            _boidsComputeShader.SetInt("_MorayRestAnchorCount", _morayRestAnchorCount);
            _boidsComputeShader.SetFloat("_MorayRestMatchRadius", _moraySpineRestRadius);

            _boidsComputeShader.DispatchThreads(_boidsKernelId, _boidsCount);

            // Moray only: refresh the spine render tuning on the moray spawner before it draws, so the
            // undulation/debug sliders and the mesh-derived body length/head take effect live in Play mode.
            if (_moraySpawner != null)
            {
                // Use the metrics + spacing derived at buffer-setup time (NOT recomputed here) so the material
                // and the kernel always agree on the ring geometry. Only the undulation/debug tunables are live.
                _moraySpawner.SetSpineRenderData(
                    _morayTrailBuffer, _morayTrailCursorBuffer, Mathf.Max(2, _moraySpineTrailSampleCount),
                    _morayTrailSpacing, _morayHeadLocalZ, _morayBodyLength,
                    _moraySpineUndulationAmplitude, _moraySpineUndulationWaves, _moraySpineUndulationSpeed,
                    _moraySpineDebugStraight, _moraySpineFlipNormals, _moraySpineSmoothingWindow,
                    _moraySpineUndulationHeadHold);

                // Cave rest pose + mouth gape: forward the anchors MorayCaveDirector set this frame plus the
                // live tuning, so a caved eel lays into the rock and breathes.
                _moraySpawner.SetSpineRestData(
                    _morayRestAnchorPos, _morayRestAnchorDir, _morayRestAnchorCount,
                    _moraySpineRestRadius, _moraySpineRestCurlRadius, _moraySpineRestUndulationScale,
                    _moraySpineMouthMaxAngle, _moraySpineMouthRate, _moraySpineMouthLength, _moraySpineMouthHingeY);
            }

            // Issue an instanced static mesh rendering call to render all boids in the scene.
            // The amount of boids for each group should be known. In order for all boids to be rendered
            // correctly, the final array containing the boids data must be sorted by ID.
            foreach (BoidSpawnerGPU gpuBoidSpawner in _gpuBoidSpawners)
            {
                if (!gpuBoidSpawner.IsActive) continue; // inactive species have no draw-args buffer
                gpuBoidSpawner.RenderBoids(boidsOutputDataBuffer, _boidSchoolsRenderInfoBuffer, _simulationAreaBounds);
            }

            _sortedBoidsBufferIsOutput = !_sortedBoidsBufferIsOutput;
        }      
    }
}