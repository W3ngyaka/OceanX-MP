using OceanX.BoidsCPU;
using UnityEngine;

namespace OceanX.BoidsGPU.BruteForceInstancedRendering
{
    /// <summary>
    /// Script that controls the boids simulation that is executed on the GPU. This version
    /// of the simulation uses a brute-force check between all boids, with instanced rendering
    /// of static meshes. This version is not the most optimized version of the simulation, it's
    /// just presented as a comparison with other solutions.
    /// 
    /// Small clarification of terms:
    /// Brute-Force --> Every Boid checks its distance to every other boid in the simulation, no matter if they're close to each other.
    /// Instanced Rendering --> The fish are not instantiated in the scene. Instead, they're just static meshes that are being rendered 
    /// on the appropriate positions using static mesh instancing. The simulation compute shader updates the properties of each boid and the
    /// render shader uses that same data to correctly render the boids at the correct world position, rotation, etc. This removes the
    /// need of fetching the data from the GPU back to the CPU just to issue render calls and update objects in the scene.
    /// </summary>
    [ExecuteAlways]
    public class BoidSimulationGPU : BoidSimulationBaseGPU
    {
        [SerializeField] private BoidSpawnerGPU[] _gpuBoidSpawners = null;

        private ComputeBuffer _boidSchoolsRenderInfoBuffer = null;

        private BoidRenderInfoGPU[] _boidSchoolsRenderInfos = null;

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
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
        protected override void UpdateSimulation(float timeDelta)
        {
            // Update properties that change every frame to the compute shader.
            _boidsComputeShader.SetFloat("_TimeDelta", timeDelta);
            _boidsComputeShader.SetBuffer(_boidsKernelId, "_Affecters", _affectersComputeBuffer);

            // Update the affecters data on the GPU to reflect their newly updated position.
            UpdateSimulationAffecters(_gpuBoidSpawners);

            // Dispatch the compute shader to execute another update of the GPU boid simulation.
            _boidsComputeShader.DispatchThreads(_boidsKernelId, _boidsCount);

            // Issue an instanced static mesh rendering call to render all boids in the scene.
            // The amount of boids for each group should be known. In order for all boids to be rendered
            // correctly, the final array containing the boids data must be sorted by ID.
            foreach (BoidSpawnerGPU gpuBoidSpawner in _gpuBoidSpawners)
            {
                gpuBoidSpawner.RenderBoids(_boidsComputeBuffer, _boidSchoolsRenderInfoBuffer, _simulationAreaBounds);
            }
        }
    }
}