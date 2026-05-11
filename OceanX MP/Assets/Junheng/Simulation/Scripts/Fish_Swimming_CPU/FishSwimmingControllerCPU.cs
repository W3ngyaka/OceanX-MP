using OceanX.BoidsCPU;
using UnityEngine;
using static OceanX.FishSwimmingUtility;

namespace OceanX
{
    /// <summary>
    /// Component that controls the movement of one fish based on the user's input.
    /// It updates the material values to reflect the changes in the speed of the fish. 
    /// </summary>
    public class FishSwimmingControllerCPU : MonoBehaviour
    {
        [Header("References: ")]
        [SerializeField] private Transform _fishTransform = null;
        [SerializeField] private FishMovementProperties _fishMovementProperties = null;
        [SerializeField] private FishSwimmingMaterialUpdate _fishMaterialUpdate = null;

        [Header("Visualization Only: ")]
        [SerializeField] private BoidInformation _boidInfo = default;

        private void OnEnable()
        {
            // Make sure that the fish swimming is controlled based on this script, and not automatically.
            if(_fishMaterialUpdate != null)
            {
                _fishMaterialUpdate.SetAutomaticSwim(false);
            }
        }

        private void Start()
        {
            // Initialize the movement properties to the cruise speed and acceleration.
            _boidInfo = InitializeBoid(_fishTransform, _fishMovementProperties);
        }

        private void Update()
        {
            // Cache the current time delta so it could be re-used in the simulation.
            float timeDelta = Time.deltaTime;

            // Calculate the new movement direction toward the target.
            Vector3 directionTowardsTarget = GetTargetDirectionBasedOnInput();
            UpdateMovementDirection(directionTowardsTarget, timeDelta, _fishMovementProperties, ref _boidInfo);

            // Update the current movement properties of the fish.
            UpdateMovementSpeed(shouldAccelerate: Input.GetMouseButton(0), timeDelta, _fishMovementProperties, ref _boidInfo);

            // Update the position and the rotation of the fish.
            UpdatePositionAndRotation(timeDelta, _fishTransform, ref _boidInfo);

            // Update render settings to reflect the current swimming intensity of the fish.
            UpdateMaterial();
        }

        private Vector3 GetTargetDirectionBasedOnInput()
        {
            Vector3 currentMovementDirection = _boidInfo.MovementDirection;

            // Get the current steering direction based on user's input.
            Vector3 targetDirection = Vector3.zero;
            if (Input.GetKey(KeyCode.W))
            {
                targetDirection += (currentMovementDirection + Vector3.down).normalized;
            }
            if (Input.GetKey(KeyCode.A))
            {
                targetDirection -= _fishTransform.right;
            }
            if (Input.GetKey(KeyCode.S))
            {
                targetDirection += (currentMovementDirection + Vector3.up).normalized;
            }
            if (Input.GetKey(KeyCode.D))
            {
                targetDirection += _fishTransform.right;
            }

            return targetDirection == Vector3.zero ? currentMovementDirection : targetDirection.normalized;
        }

        private void UpdateMaterial()
        {
            // Update render settings to reflect the current swimming intensity of the fish.
            float movementIntensity = Mathf.InverseLerp(0f, _fishMovementProperties.MaxAcceleration, _boidInfo.Acceleration);
            float rotationIntensity = Mathf.InverseLerp(0f, _fishMovementProperties.MaxAngularAcceleration, _boidInfo.AngularAcceleration);
            _fishMaterialUpdate.UpdateSwimIntensityPercentage(Mathf.Max(movementIntensity, rotationIntensity));
        }
    }
}