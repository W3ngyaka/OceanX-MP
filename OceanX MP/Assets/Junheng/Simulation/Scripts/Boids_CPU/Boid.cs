using System.Collections.Generic;
using UnityEngine;
using static OceanX.FishSwimmingUtility;

namespace OceanX.BoidsCPU
{
    /// <summary>
    /// CPU implementation of a single boid. It implements cohesion, alignment and separation rules.
    /// Additionally, it strives to reach specific targets placed in the world and avoid obstacles.
    /// Obstacles and targets are usually static but can be dynamic as well.
    /// </summary>
    public class Boid : MonoBehaviour
    {
        [Header("Visualization Properties: ")]
        [SerializeField] private BoidInformation _boidInfo = default;
        [SerializeField] private int _boidGroupId = -1;
        [SerializeField] private int _subGroupId = -1;

        private Transform _cachedTransform = null;
        private BoidSpawner _boidSpawner = null;
        private FishSchoolProperties _fishSchoolProperties = null;
        private FishMovementProperties _fishMovementProperties = null;
        private FishMotionRenderProperties _fishRenderProperties = null;
        private FishSwimmingMaterialUpdate _fishMaterialUpdate = null;

        public int BoidGroupId { get => _boidGroupId; }
        public int BoidSubGroupId { get => _subGroupId; set => _subGroupId = value; }
        public Vector3 Position { get => _boidInfo.Position; }
        public Vector3 MovementDirection { get => _boidInfo.MovementDirection; }
        public Transform Transform
        {
            get
            {
                if(_cachedTransform == null)
                {
                    _cachedTransform = transform;
                }
                return _cachedTransform;
            }
        }
        public FishMovementProperties MovementProperties { get => _fishMovementProperties; }
        public FishMotionRenderProperties RenderProperties { get => _fishRenderProperties; }

        /// <summary>
        /// Function sets up the ID of the boid group that can be used for comparing 
        /// with boids from other groups. Larger boid group ID represents a larger fish species.
        /// </summary>
        /// <param name="boidGroupId">ID of the boid group that this boid belongs to.</param>
        public void SetId(int boidGroupId)
        {
            _boidGroupId = boidGroupId;
        }

        /// <summary>
        /// Function updates the position, rotation, and acceleration of the boid based on the current simulation state.
        /// Each boid reacts to the presence of other boids, it tries to maintain their movement direction while keeping
        /// close to them. Also, it maintains a safe distance from other boids. Finally, it tries to reach specific targets
        /// placed inside the simulation area. The boid tries to remain inside the simulation area at all costs.
        /// </summary>
        /// <param name="boids">Reference to the collection of all boids inside the simulation.</param>
        /// <param name="thisBoidIndex">Index of this boid in the <paramref name="boids"/> collection.</param>
        /// <param name="timeDelta">Time passed since the last call of the function.</param>
        /// <param name="simulationAreaBounds"><see cref="Bounds"/> representing the simulation area.</param>
        /// <param name="globalAffecters">Collection of global affecters that affect every boid in the simulation.</param>
        public void UpdateBoid(List<Boid> boids, int thisBoidIndex, float timeDelta, Bounds simulationAreaBounds, List<SimulationAffecter> globalAffecters)
        {
            if(_boidSpawner == null)
            {
                return;
            }

            // Current position and rotation of the fish. They're required for calculating the new movement direction.
            Vector3 currentPosition = _boidInfo.Position;

            // Collect all obstacles that affect this boid.
            List<SimulationAffecter> obstacles = new List<SimulationAffecter>();
            foreach(SimulationAffecter globalAffecter in globalAffecters)
            {
                if(globalAffecter.Type == SimulationAffecterType.Obstacle)
                {
                    obstacles.Add(globalAffecter);
                }
            }
            if (_boidSpawner.Obstacles != null && _boidSpawner.Obstacles.Length > 0)
            {
                obstacles.AddRange(_boidSpawner.Obstacles);
            }

            // Find if there are any obstacles nearby that the boid needs to avoid.
            SimulationAffecter obstacle = GetClosestAffecter(currentPosition, obstacles.ToArray());
            Vector3 obstaclePosition = obstacle.Position;
            float obstacleRadius = obstacle.Radius;

            // When there is an obstacle we need to avoid, it takes priority over normal movement.
            bool needToAvoidObstacle = (currentPosition - obstaclePosition).magnitude <= obstacleRadius;
            Vector3 directionAwayFromObstacle = (currentPosition - obstaclePosition).normalized;

            // When there is no obstacle to avoid, move in the wanted direction.
            Vector3 currentMovementDirection = _boidInfo.MovementDirection;

            // Calculate the desired movement direction based on the neighboring fish and school movement properties.
            Vector3 desiredMovementDirection = needToAvoidObstacle ? 
                directionAwayFromObstacle : 
                CalculateSchoolBehaviorDirection(currentPosition, currentMovementDirection, boids, thisBoidIndex, globalAffecters);

            // Check if the desired direction will lead to leaving the simulation bounds. If so, offset the movement direction towards the center of the simulation area.
            float currentMovementSpeed = _boidInfo.Speed;
            if (WillLeaveSimulationArea(currentPosition, desiredMovementDirection, currentMovementSpeed, simulationAreaBounds))
            {
                desiredMovementDirection = (simulationAreaBounds.center - currentPosition).normalized;
                needToAvoidObstacle = false;
            }

            UpdateMovementDirection(desiredMovementDirection, timeDelta, _fishMovementProperties, ref _boidInfo);

            // Update the current movement properties of the fish. Only accelerate when running away from obstacles.
            UpdateMovementSpeed(shouldAccelerate: needToAvoidObstacle, timeDelta, _fishMovementProperties, ref _boidInfo);

            // Update the position and the rotation of the fish.
            UpdatePositionAndRotation(timeDelta, Transform, ref _boidInfo);

            // Update render settings to reflect the current swimming intensity of the fish.
            float movementIntensity = Mathf.InverseLerp(0f, _fishMovementProperties.MaxAcceleration, _boidInfo.Acceleration);
            float rotationIntensity = Mathf.InverseLerp(0f, _fishMovementProperties.MaxAngularAcceleration, _boidInfo.AngularAcceleration);
            UpdateSwimMotionIntensity(Mathf.Max(movementIntensity, rotationIntensity));
        }

        /// <summary>
        /// Set up boid movement and render settings based on the current global settings of this fish species.
        /// </summary>
        /// <param name="boidSpawner">Reference to the <see cref="BoidSpawner"/> object that contains references
        /// to all other boids in this simulation.</param>
        /// <param name="fishSchoolProperties">Reference to the scriptable object containing movement and render
        /// properties of this fish species.</param>
        public void Initialize(BoidSpawner boidSpawner, FishSchoolProperties fishSchoolProperties)
        {
            // Cache required references.
            _boidSpawner = boidSpawner;
            _fishSchoolProperties = fishSchoolProperties;
            _fishMovementProperties = fishSchoolProperties.MovementProperties;
            _fishRenderProperties = fishSchoolProperties.MotionRenderProperties;

            // Initialize fish material update component that will animate the movement of the fish motion.
            _fishMaterialUpdate = gameObject.AddComponent<FishSwimmingMaterialUpdate>();
            _fishMaterialUpdate.FishRenderProperties = _fishRenderProperties;

            // Initialize the start properties of the fish.
            _boidInfo = InitializeBoid(Transform, _fishMovementProperties);
        }

        /// <summary>
        /// Function updates the swim motion intensity of the boid swim movement.
        /// </summary>
        /// <param name="swimMotionIntensity">Float value in range [0, 1] representing
        /// the swimming intensity of the boid.</param>
        public void UpdateSwimMotionIntensity(float swimMotionIntensity)
        {
            _fishMaterialUpdate.UpdateSwimIntensityPercentage(swimMotionIntensity);
        }

        private Vector3 CalculateSchoolBehaviorDirection(Vector3 currentPosition, Vector3 currentMovementDirection, List<Boid> boids, int thisBoidIndex, List<SimulationAffecter> globalAffecters)
        {
            // Initial direction vectors.
            Vector3 separation = Vector3.zero;
            Vector3 alignmentDirection = currentMovementDirection;
            Vector3 cohesionPosition = currentPosition;

            // Collect all targets that affect this boid.
            List<SimulationAffecter> targets = new List<SimulationAffecter>();
            foreach (SimulationAffecter globalAffecter in globalAffecters)
            {
                if (globalAffecter.Type == SimulationAffecterType.Target)
                {
                    targets.Add(globalAffecter);
                }
            }
            targets.AddRange(_boidSpawner.Targets);

            // Target position that the boids are trying to reach.
            SimulationAffecter target = GetClosestAffecter(currentPosition, targets.ToArray());
            Vector3 targetPosition = target.Position;
            float targetRadius = target.Radius;

            // Ranges of influence on the behavior of the fish.
            float visibilityRangeSquared = Mathf.Pow(_fishSchoolProperties.VisionRange, 2.0f);
            float separationRangeSquared = Mathf.Pow(_fishSchoolProperties.SeparationRange, 2.0f);
            float inverseSeparationRangeSquared = 1.0f / separationRangeSquared;

            // Calculating separation, alignment and cohesion vectors based on neighboring boids.
            int neighborsCount = 0;
            int separationNeighborsCount = 0;
            int boidsCount = boids.Count;
            for(int i = 0; i < boidsCount; i++)
            {
                if(i == thisBoidIndex)
                {
                    continue;
                }

                Boid otherBoid = boids[i];

                // If the other fish is a smaller species, we don't care for it. It needs to 
                // make sure it avoids us, not the other way around.
                if(otherBoid.BoidGroupId < _boidGroupId)
                {
                    continue;
                }

                Vector3 otherBoidPosition = otherBoid.Position;

                // If the boid is outside the vision range, discard it.
                float distanceSquared = (otherBoidPosition - currentPosition).sqrMagnitude;
                if (distanceSquared > visibilityRangeSquared)
                {
                    continue;
                }

                // Other boid is either our species or a fish species larger than ours, so we
                // definitely need to keep our separation from it.
                if (distanceSquared < separationRangeSquared)
                {
                    separation += GetSeparationVector(currentPosition, otherBoidPosition, inverseSeparationRangeSquared);
                    separationNeighborsCount++;
                }

                // Only count alignment and cohesion for the same fish species. 
                if(_boidGroupId != otherBoid.BoidGroupId)
                {
                    continue;
                }

                alignmentDirection += otherBoid.MovementDirection;
                cohesionPosition += otherBoidPosition;
                neighborsCount++;
            }

            // Average vectors based on number of neighbors.
            float averagingFactor = neighborsCount == 0 ? 1.0f : 1.0f / neighborsCount;
            alignmentDirection *= averagingFactor;
            cohesionPosition *= averagingFactor;

            // Average the separation as well.
            if (separationNeighborsCount > 0)
            {
                separation /= separationNeighborsCount;
            }

            // Direction from the boid towards the cohesive position of school.
            Vector3 cohesionDirection = neighborsCount == 0 ? currentMovementDirection : (cohesionPosition - currentPosition).normalized;

            // Vector3 from the boid away from other boids of the school.
            Vector3 separationDirection = separationNeighborsCount > 0 ? separation.normalized : currentMovementDirection;

            // Direction towards target. Only use if the target hasn't already been reached.
            Vector3 directionTowardsTarget = (targetPosition - currentPosition).normalized;
            float targetWeight = (targetPosition - currentPosition).magnitude > targetRadius ? _fishSchoolProperties.TargetWeight : 0.0f;

            // Calculate the final movement direction as a sum of all directions.
            Vector3 newDesiredDirection = 
                cohesionDirection * _fishSchoolProperties.CohesionWeight + 
                separationDirection * _fishSchoolProperties.SeparationWeight + 
                alignmentDirection * _fishSchoolProperties.AlignmentWeight +
                directionTowardsTarget * targetWeight;
            return newDesiredDirection.normalized;
        }

        private SimulationAffecter GetClosestAffecter(Vector3 currentPosition, SimulationAffecter[] simulationAffecters)
        {
            if(simulationAffecters == null || simulationAffecters.Length == 0)
            {
                return default;
            }

            SimulationAffecter closestAffecter = default;
            float closestAffecterDistance = -1f;

            for(int i = 0; i < simulationAffecters.Length; i++)
            {
                SimulationAffecter affecter = simulationAffecters[i];
                // Only looking at affecters that affect this boid group.
                if((!affecter.AffectsAllGroups && affecter.BoidGroupId != _boidGroupId) || 
                   (!affecter.AffectsAllSubGroups && affecter.BoidSubGroupId != _subGroupId))
                {
                    continue;
                }

                Vector3 affecterPosition = affecter.Position;
                float affecterRadius = affecter.Radius;
                float distanceToAffecter = Mathf.Max(0f, (currentPosition - affecterPosition).magnitude - affecterRadius);

                // If this is the first affecter or if the distance to affecter is less than the previously closest one,
                // cache the affecter as the closest one and cache its distance as the closest distance.
                if(closestAffecterDistance < 0.0f || distanceToAffecter < closestAffecterDistance)
                {
                    closestAffecter = affecter;
                    closestAffecterDistance = distanceToAffecter;
                }
            }

            return closestAffecter;
        }

        private Vector3 GetSeparationVector(Vector3 boidPosition, Vector3 neighborPosition, float inverseSeparationRangeSquared)
        {
            Vector3 vectorAwayFromNeighbor = boidPosition - neighborPosition;
            float distanceSquared = vectorAwayFromNeighbor.sqrMagnitude;

            // The smaller separation distance is, the stronger the effect of the separation vector needs to be.
            return vectorAwayFromNeighbor * Mathf.Clamp01(1.0f - distanceSquared * inverseSeparationRangeSquared);
        }
    }
}