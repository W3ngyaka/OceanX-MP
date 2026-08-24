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

        // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
        // When set, the NEXT rebuild spawns this spawner's newly added school (the last sub-group)
        // clustered at this off-screen world position facing _pendingSpawnInwardDirection, instead of
        // behind its in-bounds target. Consumed (cleared) by the rebuild that follows. Existing fish
        // are untouched — position preservation only reuses spawn positions for brand-new fish.
        private bool    _hasPendingSpawnOrigin = false;
        private Vector3 _pendingSpawnOrigin = Vector3.zero;
        private Vector3 _pendingSpawnInwardDirection = Vector3.forward;

        // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
        /// <summary>
        /// Tells the spawner to place the school added on the next rebuild at <paramref name="worldPosition"/>
        /// (an off-screen entry point outside the bounds), oriented toward <paramref name="inwardDirection"/>
        /// so the fish immediately swim into the simulation. Applies to the newly added school only.
        /// </summary>
        public void SetPendingSpawnOrigin(Vector3 worldPosition, Vector3 inwardDirection)
        {
            _hasPendingSpawnOrigin = true;
            _pendingSpawnOrigin = worldPosition;
            _pendingSpawnInwardDirection = inwardDirection.sqrMagnitude > 1e-6f
                ? inwardDirection.normalized
                : Vector3.forward;
        }

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
                _hasPendingSpawnOrigin = false; // don't let a pending origin leak into a later rebuild
                base.InitializeBoidsSpawnData(simulationAreaBounds);
                return;
            }

            int totalBoidsCount = _boidSpawnData.BoidsCount;
            _boids = new BoidInfoGPU[totalBoidsCount];

            // Spawn boids inside the simulation area, at random positions.
            GroupOfBoidsSpawnData[] boidGroupsSpawnData = CalculateBoidsSpawnData(totalBoidsCount, simulationAreaBounds, ResolvedMinSpawnDistance, _initialGroupsCount);
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
                    // Slight per-individual swim-speed variation (±12%) so no two fish tail-beat at exactly the
                    // same rate — this keeps a school from re-synchronising into one identical wave over time.
                    float playbackSpeedVariation = UnityEngine.Random.Range(0.88f, 1.12f);
                    _boids[boidIndex] = new BoidInfoGPU
                    {
                        Position = groupOfBoidsSpawnData.SpawnPositions[boidIndexInGroup] + originalCenterToTargetVector,
                        Direction = originalCenterToTargetDirection,
                        Acceleration = 0f,
                        Speed            = movementProps != null ? movementProps.CruisingSpeed : 0f,
                        AngularAcceleration = 0f,
                        AngularVelocity = 0f,
                        // Bits 8-15 are the GLOBAL flock id (see BoidSpawnerBase.FlockIdForSchool), bits 0-7
                        // the species id, which SetId/UpdateBoidGroupId ORs in afterwards.
                        BoidID = BitConverter.Int32BitsToSingle((FlockIdForSchool(currentBoidSubGroup) & 0xFF) << 8),
                        // Randomize each fish's starting animation phase so individuals are desynced the moment
                        // they spawn instead of all tail-beating in lockstep. One full tail cycle == a
                        // CurrentSwimTime of 1, so a [0,1) start covers the entire phase. NOTE: this per-boid
                        // swim time only drives the visual once the fish material's _AutomaticSwimVisualization
                        // is OFF — otherwise the shader animates every fish off a single shared global clock.
                        CurrentSwimTime = UnityEngine.Random.value,
                        MaxPlaybackSpeed = (renderProps != null ? renderProps.MaxSwimPlaybackSpeed : 0f) * playbackSpeedVariation,
                        MinPlaybackSpeed = (renderProps != null ? renderProps.MinSwimPlaybackSpeed : 0f) * playbackSpeedVariation,
                        SwimMotionIntensity = 0f,
                        OriginalIndex = boidIndex
                    };
                    totalBoidsSpawned++;
                }
                currentBoidSubGroup++;
            }

            // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
            // If an off-screen entry point was requested, move the just-added school (the last
            // sub-group, i.e. the last contiguous block of fish) out to that point facing inward.
            // Only these brand-new fish are relocated; existing fish keep their preserved positions.
            if (_hasPendingSpawnOrigin && boidGroupsSpawnData.Length > 0)
            {
                int lastGroupCount = boidGroupsSpawnData[boidGroupsSpawnData.Length - 1].SpawnPositions.Length;
                int lastGroupStart = totalBoidsCount - lastGroupCount;
                RelocateGroupToEntryPoint(lastGroupStart, lastGroupCount);
            }
            _hasPendingSpawnOrigin = false;
        }

        // Recenters the fish in the index range [start, start + count) onto the pending entry point,
        // preserving their tight in-school offsets, and points them all toward the simulation center so
        // they swim straight in. The compute shader's out-of-bounds sprint then rushes them into the box.
        private void RelocateGroupToEntryPoint(int start, int count)
        {
            if (count <= 0) return;

            Vector3 groupCenter = Vector3.zero;
            for (int i = start; i < start + count; i++) groupCenter += _boids[i].Position;
            groupCenter /= count;

            for (int i = start; i < start + count; i++)
            {
                BoidInfoGPU b = _boids[i];
                b.Position  = _pendingSpawnOrigin + (b.Position - groupCenter);
                b.Direction = _pendingSpawnInwardDirection;
                _boids[i]   = b;
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