using OceanX.BoidsCPU;
using UnityEngine;
using static OceanX.FishSwimmingUtility;

namespace OceanX.AutomaticFishSwim
{
    /// <summary>
    /// Component that controls the swimming movement of the fish based on the target it needs to achieve.
    /// It's used as a transition from manual towards autonomous movement of the fish so that the implementation
    /// of the boids system could be less painful.
    /// </summary>
    public class AutomaticFishSwimming : MonoBehaviour
    {
        [Header("Simulation: ")]
        [SerializeField] private AutomaticFishSwimSimulation _simulation = null;
        [SerializeField] private bool _switchTargetUponReachingIt = true;
        [SerializeField] private float _reachingDistanceFromTarget = 1f;

        [Header("Movement Settings: ")]
        [SerializeField] private FishMovementProperties _fishMovementProperties = null;
        [SerializeField] private FishSchoolProperties _fishSchoolProperties = null;
        [SerializeField] private FishSwimmingMaterialUpdate _fishMaterialUpdate = null;

        [Header("Visualization Only: ")]
        [SerializeField] private BoidInformation _boidInfo = default;

        private Vector3 _currentTargetPosition = Vector3.zero;

        private void Start()
        {
            // Initialize the first target we need to reach.
            _currentTargetPosition = _simulation.GetCurrentTargetPosition();

            // Initialize the movement properties to the cruise speed and acceleration.
            _boidInfo = InitializeBoid(transform, _fishMovementProperties);
        }

        private void Update()
        {
            // Check if we have reached the current target. If we have, ask for a new target from the 
            // automatic fish swimming simulation.
            UpdateTarget();

            // Cache the current time delta so it could be re-used in the simulation.
            float timeDelta = Time.deltaTime;

            // Calculate the new movement direction toward the target.
            Vector3 currentPosition = _boidInfo.Position;
            Vector3 directionTowardsTarget = (_currentTargetPosition - currentPosition).normalized;

            // Get the closest obstacle and check if it is in the radius of obstacle aversion.
            SimulationAffecter nearestObstacle = GetClosestObstacle();
            Vector3 nearestObstaclePosition = nearestObstacle.Position;
            float obstacleRadius = nearestObstacle.Radius;
            float distanceToObstacle = (nearestObstaclePosition - currentPosition).magnitude - obstacleRadius;

            // Select the new desired movement direction based on the priority of obstacle aversion.
            float fishVisionRange = _fishSchoolProperties.VisionRange;
            float fishObstacleAversionDistance = _fishSchoolProperties.ObstacleAvoidanceRange;
            bool shouldAvoidObstacle = distanceToObstacle <= fishVisionRange && distanceToObstacle <= fishObstacleAversionDistance;
            if (!SimulationHasObstacles())
            {
                shouldAvoidObstacle = false;
            }
            Vector3 newDesiredDirection = shouldAvoidObstacle ? (currentPosition - nearestObstaclePosition).normalized : directionTowardsTarget;

            // Check if the desired direction will lead to leaving the simulation bounds. If so, offset the movement direction towards the center of the simulation area.
            float currentMovementSpeed = _boidInfo.Speed;
            if (WillLeaveSimulationArea(currentPosition, newDesiredDirection, currentMovementSpeed, _simulation.SimulationAreaBounds))
            {
                newDesiredDirection = (_simulation.SimulationAreaBounds.center - currentPosition).normalized;
                shouldAvoidObstacle = false;
            }
            UpdateMovementDirection(newDesiredDirection, timeDelta, _fishMovementProperties, ref _boidInfo);

            // Update the current movement properties of the fish.
            UpdateMovementSpeed(shouldAccelerate: shouldAvoidObstacle, timeDelta, _fishMovementProperties, ref _boidInfo);

            // Update the position and the rotation of the fish.
            UpdatePositionAndRotation(timeDelta, transform, ref _boidInfo);

            // Update render settings to reflect the current swimming intensity of the fish.
            float movementIntensity = Mathf.InverseLerp(0f, _fishMovementProperties.MaxAcceleration, _boidInfo.Acceleration);
            float rotationIntensity = Mathf.InverseLerp(0f, _fishMovementProperties.MaxAngularAcceleration, _boidInfo.AngularAcceleration);
            _fishMaterialUpdate.UpdateSwimIntensityPercentage(Mathf.Max(movementIntensity, rotationIntensity));
        }

        private void UpdateTarget()
        {
            if(!_switchTargetUponReachingIt)
            {
                _currentTargetPosition = _simulation.GetCurrentTargetPosition();
                return;
            }

            // Check if the target has been reached and update the target position if it has.
            float distanceFromTarget = (_boidInfo.Position - _currentTargetPosition).magnitude;
            if (distanceFromTarget <= _reachingDistanceFromTarget)
            {
                _currentTargetPosition = _simulation.GetNewTargetPosition();
            }
        }

        private bool SimulationHasObstacles()
        {
            SimulationAffecter[] obstacles = _simulation.Obstacles;
            if (obstacles == null || obstacles.Length == 0)
            {
                return false;
            }

            return true;
        }

        private SimulationAffecter GetClosestObstacle()
        {
            SimulationAffecter[] obstacles = _simulation.Obstacles;
            if (obstacles == null || obstacles.Length == 0)
            {
                return default;
            }

            Vector3 currentPosition = _boidInfo.Position;
            SimulationAffecter nearestObstacle = obstacles[0];
            float nearestObstacleDistanceSquared = (nearestObstacle.Position - currentPosition).sqrMagnitude;
            nearestObstacleDistanceSquared -= (nearestObstacle.Radius * nearestObstacle.Radius);

            for (int i = 1; i < obstacles.Length; i++)
            {
                SimulationAffecter obstacle = obstacles[i];
                float distanceFromObstacleSquared = (obstacle.Position - currentPosition).sqrMagnitude;
                distanceFromObstacleSquared -= (obstacle.Radius * obstacle.Radius);

                if (distanceFromObstacleSquared < nearestObstacleDistanceSquared)
                {
                    nearestObstacle = obstacle;
                    nearestObstacleDistanceSquared = distanceFromObstacleSquared;
                }
            }

            return nearestObstacle;
        }
    }
}