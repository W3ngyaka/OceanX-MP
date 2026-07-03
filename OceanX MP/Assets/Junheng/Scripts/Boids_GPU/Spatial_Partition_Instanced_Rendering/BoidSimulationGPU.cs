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
            _boidsComputeShader.SetBuffer(_boidsKernelId, "_Affecters", _affectersComputeBuffer);

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