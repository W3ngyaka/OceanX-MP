using OceanX.BoidsGPU.SpatialPartitionInstancedRendering;
using UnityEngine;

namespace OceanX.BoidsGPU
{
    /// <summary>
    /// Component that creates target animators for each of the initial boid groups to
    /// provide additional diversity and versatility of the simulation. Each target is
    /// animated inside the simulation area and is moving based on the cruising speed of the
    /// fish species.
    /// </summary>
    public class BoidSimulationTargetAnimatorsSpawner : MonoBehaviour
    {
        [Header("References: ")]
        [SerializeField] private BoidSimulationBase _boidSimulation = null;
        [SerializeField] private BoidSpawnerBase _boidSpawner = null;
        [SerializeField] private Transform _targetsHolder = null;
        [SerializeField] private Transform _targetsAnimatorsHolder = null;

        [Header("Options: ")]
        [SerializeField] private float _targetRadius = 3f;
        [SerializeField] private float _targetMovementSpeed = 2f;
        [Tooltip("How many meters from the edge of the simulation zone won't be used " +
            "for animating targets. Increasing safe zone reduces the available area for animations but " +
            "increases safety of fish going outside of simulation area.")]
        [SerializeField] private float _boundsSafeZoneSize = 2f;

        /// <summary>
        /// Function deletes all spawned targets and target animators.
        /// </summary>
        public void DeleteTargets()
        {
            for(int i = 0; i < _targetsHolder.childCount; i++)
            {
                DestroyImmediate(_targetsHolder.GetChild(i).gameObject);
                i--;
            }

            for (int i = 0; i < _targetsAnimatorsHolder.childCount; i++)
            {
                DestroyImmediate(_targetsAnimatorsHolder.GetChild(i).gameObject);
                i--;
            }

            _boidSpawner.ClearTargets();
        }

        /// <summary>
        /// Function creates new targets for each group of the spawned boids in the simulation.
        /// Each target is also animated inside the simulation area with a mixture of circular,
        /// rectangular and line movements.
        /// </summary>
        public void CreateTargets()
        {
            int totalTargets = _boidSpawner.InitialGroupsCount;
            Bounds simulationAreaBounds = _boidSimulation.SimulationAreaBounds;

            for(int targetIndex = 0; targetIndex < totalTargets; targetIndex++)
            {
                // Initialize the target and the animator for the target.
                SimulationAffecterComponent target = CreateTarget(targetIndex);
                TransformAnimator targetTransformAnimator = CreateTargetAnimator(target, targetIndex);                

                // Setup appropriate random animation.
                int animationType = targetIndex % 3;
                switch (animationType)
                {
                    case 0:
                        SetupLineAnimation(targetTransformAnimator, simulationAreaBounds);
                        break;
                    case 1:
                        SetupCircleAnimation(targetTransformAnimator, simulationAreaBounds);
                        break;
                    default:
                        SetupRectangleAnimation(targetTransformAnimator, simulationAreaBounds);
                        break;
                }

                // Based on animation type, initialize the starting position of the target.
                targetTransformAnimator.SetupInitialTargetPosition();

                // Make sure that the target is recorded for the simulation.
                _boidSpawner.AddTarget(target);
            }
        }

        private SimulationAffecterComponent CreateTarget(int boidSubGroupId)
        {
            // Create a new game object that will represent a specific target in the simulation.
            GameObject targetGameObject = new GameObject($"Target ({boidSubGroupId})");
            Transform targetTransform = targetGameObject.transform;
            targetTransform.SetParent(_targetsHolder);

            // Assign the simulation affecter component to it.
            SimulationAffecterComponent targetComponent = targetGameObject.AddComponent<SimulationAffecterComponent>();

            // Specify the ID of the sub-group of boids that this target will affect.
            targetComponent.SetSubGroupID(boidSubGroupId);

            // Make sure that the affecter is marked as target.
            targetComponent.SetAffecterType(SimulationAffecterType.Target);

            // Make sure that the target updates its position to the GPU every frame.
            targetComponent.SetUpdatePositionEveryFrame(true);

            // Update the size of the target.
            targetTransform.localScale = Vector3.one * _targetRadius;

            return targetComponent;
        }

        private TransformAnimator CreateTargetAnimator(SimulationAffecterComponent target, int targetIndex)
        {
            // Create a new game object that will represent a specific target animator.
            GameObject targetAnimatorGameObject = new GameObject($"Target Animator ({targetIndex})");
            Transform targetAnimatorTransform = targetAnimatorGameObject.transform;
            targetAnimatorTransform.SetParent(_targetsAnimatorsHolder);

            // Assign the transform animator component that will animate the target during the simulation.
            TransformAnimator targetTransformAnimator = targetAnimatorGameObject.AddComponent<TransformAnimator>();

            // Set the reference to the target transform.
            targetTransformAnimator.TargetTransform = target.transform;
            targetTransformAnimator.MovementSpeed = _targetMovementSpeed;

            return targetTransformAnimator;
        }

        private void SetupLineAnimation(TransformAnimator transformAnimator, Bounds simulationAreaBounds)
        {
            // Height in world space at which the line will be placed.
            float lineHeight = simulationAreaBounds.center.y + Random.Range(-simulationAreaBounds.extents.y, simulationAreaBounds.extents.y) * 0.8f;

            // Angle of the line, indicating the start and end point.
            float lineAngle = Random.Range(0f, 180f);

            // Calculate the size of the line on which the transform will be moved.
            float lineMaxLength = Mathf.Min(simulationAreaBounds.size.x, simulationAreaBounds.size.z) - _boundsSafeZoneSize;
            float lineLength = Random.Range(lineMaxLength * 0.5f, lineMaxLength);

            // Setup the transform animator based on the calculated properties.
            transformAnimator.transform.position = new Vector3(simulationAreaBounds.center.x, lineHeight, simulationAreaBounds.center.z);
            transformAnimator.transform.localEulerAngles = new Vector3(0f, lineAngle, 0f);
            transformAnimator.LineLength = lineLength;
            transformAnimator.AnimationShape = TransformAnimator.PositionAnimationShape.Line;
            transformAnimator.MovementSpeed = _targetMovementSpeed;
        }

        private void SetupCircleAnimation(TransformAnimator transformAnimator, Bounds simulationAreaBounds)
        {
            // Height in world space at which the circle will be placed.
            float circleHeight = simulationAreaBounds.center.y + Random.Range(-simulationAreaBounds.extents.y, simulationAreaBounds.extents.y) * 0.8f;

            // Calculate the radius of the circle on which the transform will be moved.
            float circleMaxRadius = Mathf.Min(simulationAreaBounds.extents.x, simulationAreaBounds.extents.z) - _boundsSafeZoneSize;
            float circleRadius = Random.Range(circleMaxRadius * 0.5f, circleMaxRadius);

            // Initial angle of the circle, used for additional randomization.
            float initialAngle = Random.Range(0f, 180f);

            // Setup the transform animator based on the calculated properties.
            transformAnimator.transform.position = new Vector3(simulationAreaBounds.center.x, circleHeight, simulationAreaBounds.center.z);
            transformAnimator.transform.localEulerAngles = new Vector3(0f, initialAngle, 0f);
            transformAnimator.CircleRadius = circleRadius;
            transformAnimator.MovingClockwise = Random.Range(0f, 1f) > 0.5f;
            transformAnimator.AnimationShape = TransformAnimator.PositionAnimationShape.Circle;
            transformAnimator.MovementSpeed = _targetMovementSpeed;
        }

        private void SetupRectangleAnimation(TransformAnimator transformAnimator, Bounds simulationAreaBounds)
        {
            // Height in world space at which the rectangle will be placed.
            float rectangleHeight = simulationAreaBounds.center.y + Random.Range(-simulationAreaBounds.extents.y, simulationAreaBounds.extents.y) * 0.8f;

            // Calculate the size of the rectangle.
            float sizeMultiplier = Random.Range(0.5f, 1.0f);
            float rectangleWidth = sizeMultiplier * simulationAreaBounds.size.x;
            float rectangleLength = sizeMultiplier * simulationAreaBounds.size.z;

            // Setup the transform animator based on the calculated properties.
            transformAnimator.transform.position = new Vector3(simulationAreaBounds.center.x, rectangleHeight, simulationAreaBounds.center.z);
            transformAnimator.transform.localEulerAngles = new Vector3(0f, Random.Range(0f, 1f) <= 0.5f ? 0f : 180f, 0f);
            transformAnimator.RectangleWidth = rectangleWidth;
            transformAnimator.RectangleLength = rectangleLength;
            transformAnimator.MovingClockwise = Random.Range(0f, 1f) > 0.5f;
            transformAnimator.AnimationShape = TransformAnimator.PositionAnimationShape.Rectangle;
            transformAnimator.MovementSpeed = _targetMovementSpeed;
        }
    }
}