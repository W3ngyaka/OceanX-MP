using OceanX.BoidsCPU;
using System;
using SimBoid = OceanX.BoidsCPU.Boid;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OceanX.BoidsGPU.BruteForceNormalRendering
{
    /// <summary>
    /// Script that controls the boids simulation that is executed on the GPU. This version
    /// of the simulation uses a brute-force check between all boids, with normal rendering
    /// without static instancing. This version is not the most optimized version of the simulation, it's
    /// just presented as a comparison with other solutions.
    /// 
    /// Small clarification of terms:
    /// Brute-Force --> Every Boid checks its distance to every other boid in the simulation, no matter if they're close to each other.
    /// Normal Rendering --> After the simulation is updated on the GPU, the results are fetched to the CPU side and the actual Boid objects
    /// from the scene are updated to new positions, and their material properties are updated to reflect the swimming motion. There is no
    /// static instancing existing in the simulation.
    /// </summary>
    [ExecuteAlways]
    public class BoidSimulationGPU : BoidSimulationBaseGPU
    {
        [SerializeField] private BoidSpawner[] _boidSpawners = null;

        private List<SimBoid> _boids = new List<SimBoid>();

        /// <inheritdoc/>
        public override BoidSpawnerBase[] GetBoidSpawners()
        {
            return _boidSpawners;
        }

        /// <inheritdoc/>
        protected override void SpawnBoids()
        {
            // Spawn all boids and cache their reference.
            foreach (BoidSpawner boidSpawner in _boidSpawners)
            {
                boidSpawner.SpawnBoids(_simulationAreaBounds);
                List<SimBoid> spawnedBoids = boidSpawner.Boids;
                _boids.AddRange(spawnedBoids);
            }
        }

        /// <inheritdoc/>
        protected override void InitializeRenderProperties()
        {
            // This version of the GPU boid simulation doesn't use instanced rendering
            // of a static mesh to render boids, so this function doesn't need to do anything.
        }

        /// <inheritdoc/>
        protected override void InitializeBoidsSimulation()
        {
            SpawnBoids();

            // Collect all simulation affecters into one collection. They differentiate on their boid group ID.
            BoidSpawnerBase[] boidSpawners = GetBoidSpawners();
            int boidSpawnersCount = boidSpawners.Length;

            // Assign an ID to each boid group that will differentiate it from other groups
            // and also let it compare its fish species' size compared to other boid groups.
            // Higher ID means that the fish species is larger than the other one.
            ComparableBounds[] boidFishSizesBounds = new ComparableBounds[boidSpawnersCount];
            for (int i = 0; i < boidSpawnersCount; i++)
            {
                int boundsID = i;
                boidFishSizesBounds[i] = new ComparableBounds { OriginalBoundsID = boundsID, Bounds = boidSpawners[i].FishSizeBounds };
            }
            for (int i = 0; i < boidSpawnersCount; i++)
            {
                int boidGroupIndex = i;
                boidSpawners[boidGroupIndex].SetId(CalculateBoidGroupId(boidGroupIndex, boidFishSizesBounds));
            }

            foreach (BoidSpawner boidSpawner in boidSpawners)
            {
                SimulationAffecter[] targets = boidSpawner.Targets;
                if (targets != null && targets.Length > 0)
                {
                    _affecters.AddRange(targets);
                }

                SimulationAffecter[] obstacles = boidSpawner.Obstacles;
                if (obstacles != null && obstacles.Length > 0)
                {
                    _affecters.AddRange(obstacles);
                }
            }

            // Include global affecters in the simulation.
            foreach (SimulationAffecterComponent globalAffecter in _globalAffecters)
            {
                _affecters.Add(globalAffecter.Affecter);
            }

            // Fetch all fish school properties and cache them.
            FishSchoolProperties[] orderedBoidSchools = new FishSchoolProperties[boidSpawners.Length];
            foreach (BoidSpawner boidSpawner in boidSpawners)
            {
                orderedBoidSchools[boidSpawner.BoidGroupId] = boidSpawner.SpawnData.FishSchoolProperties;
            }
            _boidsSchools = orderedBoidSchools.ToList();

            _boidsCount = _boids.Count;
            _boidsInfos = new BoidInfoGPU[_boidsCount];
            for (int i = 0; i < _boidsCount; i++)
            {
                int boidIndex = i;
                _boidsInfos[i] = ExtractBoidData(_boids[boidIndex], boidIndex);
            }

            // Initialize the compute shader and assign the required data for simulation on the GPU.
            InitializeComputeShaderData();
        }

        /// <inheritdoc/>
        protected override void UpdateSimulation(float timeDelta)
        {
            // Update properties that change every frame to the compute shader.
            _boidsComputeShader.SetFloat("_TimeDelta", timeDelta);
            _boidsComputeShader.SetBuffer(_boidsKernelId, "_Affecters", _affectersComputeBuffer);

            // Update the affecters data on the GPU to reflect their newly updated position.
            UpdateSimulationAffecters(_boidSpawners);

            // Dispatch the compute shader to execute another update of the GPU boid simulation.
            int boidsCount = _boids.Count;
            _boidsComputeShader.DispatchThreads(_boidsKernelId, boidsCount);

            // After the boids simulation has been updated, fetch the updated boid data.
            _boidsComputeBuffer.GetData(_boidsInfos);

            // Update the boids with the updated data.
            for (int i = 0; i < boidsCount; i++)
            {
                SimBoid boid = _boids[i];
                BoidInfoGPU boidInfoGPU = _boidsInfos[i];
                Transform boidTransform = boid.transform;
                boidTransform.position = boidInfoGPU.Position;
                boidTransform.rotation = Quaternion.LookRotation(boidInfoGPU.Direction, Vector3.up);
                boid.UpdateSwimMotionIntensity(boidInfoGPU.SwimMotionIntensity);
            }
        }

        private BoidInfoGPU ExtractBoidData(SimBoid boid, int boidIndex)
        {
            // Boid Group ID into lowest 8 bits, Boid sub-group ID between 8-16 bits.
            int subGroupAndGroupPackedTogether = ((boid.BoidSubGroupId << 8) & 0xFF00) + (boid.BoidGroupId & 0xFF);
            float finalBoidID = BitConverter.Int32BitsToSingle(subGroupAndGroupPackedTogether);

            return new BoidInfoGPU
            {
                Position = boid.Position,
                Acceleration = 0f,
                Direction = boid.MovementDirection,
                Speed = boid.MovementProperties.CruisingSpeed,
                AngularVelocity = 0f,
                AngularAcceleration = 0f,
                BoidID = finalBoidID,
                SwimMotionIntensity = 0f,
                CurrentSwimTime = 0f,
                MinPlaybackSpeed = boid.RenderProperties.MinSwimPlaybackSpeed,
                MaxPlaybackSpeed = boid.RenderProperties.MaxSwimPlaybackSpeed,
                OriginalIndex = boidIndex
            };
        }        
    }
}