using System;
using UnityEngine;
using OceanX.BoidsGPU.Ecosystem;
using static OceanX.BoidSpawnUtility;

namespace OceanX.BoidsGPU
{
    /// <summary>
    /// Special version of the <see cref="BoidSpawnerGPU"/> that spawns boids
    /// based on the pre-defined targets so that they're initially spawned at a
    /// certain offset from them and facing in their direction.
    /// </summary>
    public class BoidSpawnerGPUMultiTargets: BoidSpawnerGPU
    {
        [Space]
        [SerializeField] private float _initialOffsetFromTarget = 1.5f;

        // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
        [Space]
        [Tooltip("Assign a SpeciesDataGPU asset to drive all simulation properties from one place. " +
                 "When set, SchoolProperties is pulled from here instead of BoidSpawnData directly.")]
        [SerializeField] private SpeciesDataGPU _speciesData = null;

        // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
        /// <summary>The species data asset driving this spawner, if assigned.</summary>
        public SpeciesDataGPU SpeciesData => _speciesData;

        /// <inheritdoc/>
        protected override void InitializeBoidsSpawnData(Bounds simulationAreaBounds)
        {
            // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
            // When a SpeciesDataGPU asset is assigned, create a runtime copy of SchoolProperties
            // and inject MovementProperties + MotionRenderProperties from SpeciesDataGPU into it.
            // This way FishSchoolProperties only holds flocking weights in the Inspector —
            // movement and render props are set once on SpeciesDataGPU and flow through here automatically.
            if (_speciesData != null && _speciesData.SchoolProperties != null)
            {
                FishSchoolProperties runtimeSchool = Instantiate(_speciesData.SchoolProperties);
                runtimeSchool.MovementProperties    = _speciesData.MovementProperties;
                runtimeSchool.MotionRenderProperties = _speciesData.MotionRenderProperties;
                _boidSpawnData.FishSchoolProperties  = runtimeSchool;
            }

            FishMovementProperties     movementProps = _boidSpawnData.FishSchoolProperties?.MovementProperties;
            FishMotionRenderProperties renderProps   = _boidSpawnData.FishSchoolProperties?.MotionRenderProperties;

            // If we don't have enough targets for each boid sub-group, just revert to default boid spawning.
            SimulationAffecterComponent[] targets = _boidSpawnData.Targets;
            if(targets == null || targets.Length < _initialGroupsCount)
            {
                base.InitializeBoidsSpawnData(simulationAreaBounds);
                return;
            }

            int totalBoidsCount = _boidSpawnData.BoidsCount;
            _boids = new BoidInfoGPU[totalBoidsCount];

            // Spawn boids inside the simulation area, at random positions.
            GroupOfBoidsSpawnData[] boidGroupsSpawnData = CalculateBoidsSpawnData(totalBoidsCount, simulationAreaBounds, _boidSpawnData.MinSpawnDistanceBetweenBoids, _initialGroupsCount);
            int totalBoidsSpawned = 0;
            int currentBoidSubGroup = 0;

            // For each boid sub-group, offset and re-orientate each boid so that it's located
            // behind the corresponding target it will follow. This will initialize boids to correct initial position
            // so that they don't need to move across the whole simulation area to get to their target first.
            for(int i = 0; i < boidGroupsSpawnData.Length; i++)
            {
                GroupOfBoidsSpawnData groupOfBoidsSpawnData = boidGroupsSpawnData[i];

                // Based on the target position and the center of this sub-group, calculate the offset vector
                // that should be added to position of each boid.
                Vector3 target = targets[i].AffecterPosition;
                Vector3 groupOriginalCenter = GetCenterPosition(groupOfBoidsSpawnData.SpawnPositions);
                Vector3 originalCenterToTargetVector = (target - groupOriginalCenter);

                // Based on this offset direction, calculate the new rotation of each boid and reduce the
                // offset so that they're spawned a bit behind the target.
                Vector3 originalCenterToTargetDirection = originalCenterToTargetVector.normalized;
                originalCenterToTargetVector -= originalCenterToTargetDirection * _initialOffsetFromTarget;

                int boidsCountInThisGroup = groupOfBoidsSpawnData.SpawnPositions.Length;
                for (int boidIndexInGroup = 0; boidIndexInGroup < boidsCountInThisGroup; boidIndexInGroup++)
                {
                    int boidIndex = totalBoidsSpawned;
                    _boids[boidIndex] = new BoidInfoGPU
                    {
                        Position = groupOfBoidsSpawnData.SpawnPositions[boidIndexInGroup] + originalCenterToTargetVector,
                        Direction = originalCenterToTargetDirection,
                        Acceleration = 0f,
                        Speed            = movementProps != null ? movementProps.CruisingSpeed : 0f,
                        AngularAcceleration = 0f,
                        AngularVelocity = 0f,
                        BoidID = BitConverter.Int32BitsToSingle((currentBoidSubGroup & 0xFF) << 8),
                        CurrentSwimTime = 0f,
                        MaxPlaybackSpeed = renderProps != null ? renderProps.MaxSwimPlaybackSpeed : 0f,
                        MinPlaybackSpeed = renderProps != null ? renderProps.MinSwimPlaybackSpeed : 0f,
                        SwimMotionIntensity = 0f,
                        OriginalIndex = boidIndex
                    };
                    totalBoidsSpawned++;
                }
                currentBoidSubGroup++;
            }
        }

        private Vector3 GetCenterPosition(Vector3[] positions)
        {
            Vector3 centerPosition = Vector3.zero;
            int positionsCount = positions.Length;

            for(int i = 0; i < positionsCount; i++)
            {
                centerPosition += positions[i];
            }

            return centerPosition / (float)positionsCount;
        }
    }
}