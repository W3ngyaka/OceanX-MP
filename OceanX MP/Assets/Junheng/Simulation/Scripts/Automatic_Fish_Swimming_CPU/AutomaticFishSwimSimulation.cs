using UnityEngine;

namespace OceanX.AutomaticFishSwim
{
    /// <summary>
    /// Component that guides the simulation of the automatic fish swim movement.
    /// It randomly selects a target position inside the simulation area that the
    /// fish will try to get to. When the fish reaches the target position, it will
    /// select a new random position at a certain distance from the previous one.
    /// </summary>
    [ExecuteInEditMode]
    public class AutomaticFishSwimSimulation: MonoBehaviour
    {
        [Header("Simulation Settings: ")]
        [SerializeField] private Bounds _simulationAreaBounds = new Bounds();

        [Header("Target: ")]
        [SerializeField] private SimulationAffecterComponent _target = null;
        [SerializeField] private float _minDistanceBetweenTargets = 5f;
        [SerializeField] private float _maxDistanceBetweenTargets = 10f;

        [Header("Obstacles: ")]
        [SerializeField] private SimulationAffecterComponent[] _obstacles = null;

        public Bounds SimulationAreaBounds { get => _simulationAreaBounds; }

        public SimulationAffecter[] Obstacles
        {
            get
            {
                SimulationAffecter[] obstacles = new SimulationAffecter[_obstacles.Length];
                for(int i = 0; i < _obstacles.Length; i++) 
                {
                    obstacles[i] = _obstacles[i].Affecter;
                }
                return obstacles;
            }
        }

        private void OnDrawGizmos()
        {
            //Gizmos.color = Color.magenta;
            //Gizmos.DrawWireCube(_simulationAreaBounds.center, _simulationAreaBounds.size);
        }

        private void Update()
        {
            _simulationAreaBounds.center = transform.position;
        }

        /// <summary>
        /// Function returns the current position of the current target of the simulation.
        /// </summary>
        /// <returns>World position of the current target in the simulation.</returns>
        public Vector3 GetCurrentTargetPosition()
        {
            return _target.AffecterPosition;
        }

        /// <summary>
        /// Function calculates a new target position inside the simulation area bounds that
        /// will be at lease <see cref="_minDistanceBetweenTargets"/> away from the current 
        /// target position.
        /// </summary>
        /// <returns>New target position inside the simulation area.</returns>
        public Vector3 GetNewTargetPosition()
        {
            Vector3 previousTargetPosition = _target.AffecterPosition;

            // Calculate the new target position at the correct distance from the previous target.
            Vector3 newTargetPosition = GetRandomPositionInsideSimulationArea();
            float distanceBetweenTargets = Random.Range(_minDistanceBetweenTargets, _maxDistanceBetweenTargets);
            Vector3 directionTowardNewPosition = (newTargetPosition - previousTargetPosition).normalized;
            newTargetPosition = previousTargetPosition + directionTowardNewPosition * distanceBetweenTargets;

            // Update the target to the new position.
            _target.AffecterPosition = newTargetPosition;

            return newTargetPosition;
        }

        private Vector3 GetRandomPositionInsideSimulationArea()
        {
            Vector3 simulationAreaCenter = _simulationAreaBounds.center;
            Vector3 simulationAreaExtents = _simulationAreaBounds.extents;

            float positionX = Random.Range(simulationAreaCenter.x - simulationAreaExtents.x, simulationAreaCenter.x + simulationAreaExtents.x);
            float positionY = Random.Range(simulationAreaCenter.y - simulationAreaExtents.y, simulationAreaCenter.y + simulationAreaExtents.y);
            float positionZ = Random.Range(simulationAreaCenter.z - simulationAreaExtents.z, simulationAreaCenter.z + simulationAreaExtents.z);

            return new Vector3(positionX, positionY, positionZ);
        }
    }
}