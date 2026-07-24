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

        private ComputeBuffer _sortedBoidsComputeBuffer = null;
        private ComputeBuffer _boidSchoolsRenderInfoBuffer = null;

        private bool _sortedBoidsBufferIsOutput = false;
        private BoidRenderInfoGPU[] _boidSchoolsRenderInfos = null;

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            CleanUpComputeBuffer(ref _sortedBoidsComputeBuffer);
            CleanUpComputeBuffer(ref _boidSchoolsRenderInfoBuffer);
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
            _boidsComputeShader.DispatchThreads(_boidsKernelId, _boidsCount);

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