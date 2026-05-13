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

        /// <inheritdoc/>
        public override BoidSpawnerBase[] GetBoidSpawners()
        {
            return _gpuBoidSpawners;
        }

        /// <inheritdoc/>
        protected override void SpawnBoids()
        {
            // Spawn all boids inside the simulation area.            
            foreach (BoidSpawnerGPU gpuBoidSpawner in _gpuBoidSpawners)
            {
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
            // Fetch all fish school properties and cache them. Also, fetch all boid school render properties.
            _boidSchoolsRenderInfos = new BoidRenderInfoGPU[_gpuBoidSpawners.Length];
            foreach (BoidSpawnerGPU gpuBoidSpawner in _gpuBoidSpawners)
            {
                int index = Mathf.Min(gpuBoidSpawner.BoidGroupId, _boidsSchools.Count);
                _boidsSchools.Insert(index, gpuBoidSpawner.SpawnData.FishSchoolProperties);
                _boidSchoolsRenderInfos[gpuBoidSpawner.BoidGroupId] = ExtractBoidSchoolRenderInfo(gpuBoidSpawner.SpawnData.FishSchoolProperties.MotionRenderProperties);
            }

            // Initialize the compute buffer that will hold the render information for each boid school in a
            // GPU-readable format, so that instanced materials can use it.
            _boidSchoolsRenderInfoBuffer = new ComputeBuffer(_boidSchoolsRenderInfos.Length, BoidRenderInfoGPU.Size);
            _boidSchoolsRenderInfoBuffer.SetData(_boidSchoolsRenderInfos);
        }

        /// <inheritdoc/>
        protected override void InitializeComputeShaderData()
        {
            base.InitializeComputeShaderData();

            _sortedBoidsComputeBuffer = new ComputeBuffer(_boidsCount, BoidInfoGPU.Size);
            _sortedBoidsComputeBuffer.SetData(_boidsInfos);
        }

        /// <inheritdoc/>
        protected override void UpdateSimulation(float timeDelta)
        {
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
            _boidsComputeShader.SetBuffer(_boidsKernelId, "_Affecters", _affectersComputeBuffer);

            // Update the affecters data on the GPU to reflect their newly updated position.
            UpdateSimulationAffecters(_gpuBoidSpawners);

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
                gpuBoidSpawner.RenderBoids(boidsOutputDataBuffer, _boidSchoolsRenderInfoBuffer, _simulationAreaBounds);
            }

            _sortedBoidsBufferIsOutput = !_sortedBoidsBufferIsOutput;
        }      
    }
}