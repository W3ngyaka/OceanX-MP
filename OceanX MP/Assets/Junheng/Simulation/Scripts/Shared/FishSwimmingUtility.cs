using OceanX.BoidsCPU;
using UnityEngine;

namespace OceanX
{
    /// <summary>
    /// Static class that implements shared fish movement functionality to be re-used
    /// across multiple different fish swimming set of controls.
    /// </summary>
    public static class FishSwimmingUtility
    {
        /// <summary>
        /// Function initializes the properties of the <see cref="BoidInformation"/> structure instance based on the provided
        /// <paramref name="boidTransform"/> and <paramref name="fishMovementProperties"/>. The current position of
        /// the fish is initialized to the current world position of the provided <paramref name="boidTransform"/> while
        /// the current movement speed is set to the cruising speed.
        /// </summary>
        /// <param name="boidTransform">Reference to the <see cref="Transform"/> component whose position and rotation
        /// are used to initialize the initial position and rotation of the fish.</param>
        /// <param name="fishMovementProperties">Reference to the <see cref="FishMovementProperties"/> scriptable object
        /// instance containing the movement properties of this fish species.</param>
        /// <returns>Reference to the <see cref="BoidInformation"/> structure containing initialized fish properties.</returns>
        public static BoidInformation InitializeBoid(Transform boidTransform, FishMovementProperties fishMovementProperties)
        {
            return new BoidInformation
            {
                Position = boidTransform.position,
                MovementDirection = boidTransform.forward,
                Speed = fishMovementProperties.CruisingSpeed,
                Acceleration = 0f,
                AngularAcceleration = 0f,
                AngularVelocity = 0f
            };
        }

        /// <summary>
        /// Function updates the current movement direction and rotation properties so that the fish tries to rotate towards the provided <paramref name="targetDirection"/>.
        /// Also, since a percentage of angular velocity is being transferred to the movement acceleration, the current movement acceleration is updated as well. The 
        /// updated values are then written into the <paramref name="BoidInformation"/> structure.
        /// </summary>
        /// <param name="targetDirection">Unit vector representing the direction towards which the fish should be looking.</param>
        /// <param name="timeDelta">Time passed since last movement direction update.</param>
        /// <param name="fishMovementProperties">Reference to the scriptable object containing the movement properties of this fish species.</param>
        /// <param name="BoidInformation">Reference to the structure containing the current properties of this fish instance.</param>
        public static void UpdateMovementDirection(Vector3 targetDirection, float timeDelta, FishMovementProperties fishMovementProperties, ref BoidInformation BoidInformation)
        {
            // Unpacking properties from the Fish struct into a more readable format.
            Vector3 currentMovementDirection = BoidInformation.MovementDirection;
            float currentAcceleration = BoidInformation.Acceleration;
            float currentAngularAcceleration = BoidInformation.AngularAcceleration;
            float currentAngularVelocity = BoidInformation.AngularVelocity;

            // Calculate quaternion rotations of the current fish rotation and the rotation towards the target.
            Quaternion rotationTowardTarget = Quaternion.LookRotation(targetDirection, Vector3.up);
            Quaternion currentRotation = Quaternion.LookRotation(currentMovementDirection, Vector3.up);
            float angleTowardTarget = Quaternion.Angle(currentRotation, rotationTowardTarget);

            // Update the current angular acceleration and velocity. Using 50 degrees as an angle threshold for starting to speed up at maximum acceleration.
            float currentAngularJerk = currentAngularVelocity < angleTowardTarget ? Mathf.Lerp(0f, fishMovementProperties.AngularJerk, (angleTowardTarget - currentAngularVelocity) / 50f) : 0f;
            float angularDeceleration = currentAngularJerk > 0f ? 0f : fishMovementProperties.AngularDeceleration;
            currentAngularAcceleration = Mathf.Clamp(currentAngularAcceleration + (currentAngularJerk - angularDeceleration) * timeDelta, 0f, fishMovementProperties.MaxAngularAcceleration);
            currentAngularVelocity = Mathf.Clamp(currentAngularVelocity + currentAngularAcceleration * timeDelta, 0f, fishMovementProperties.MaxAngularVelocity);

            // Calculate new rotation based on the current angular velocity and the direction toward target.
            Quaternion newRotation = Quaternion.RotateTowards(currentRotation, rotationTowardTarget, currentAngularVelocity * timeDelta);

            // Reduce angular velocity when there's acceleration that increases it.
            if (currentAngularJerk <= 0f)
            {
                currentAngularVelocity = Mathf.Clamp(currentAngularVelocity - fishMovementProperties.AngularVelocityReduction * timeDelta, 0f, fishMovementProperties.MaxAngularVelocity);
            }

            // Transfer some of the rotation energy toward the forward movement.
            currentAcceleration += currentAngularAcceleration * fishMovementProperties.RotationEffectOnSpeed * timeDelta;

            // Update current movement direction of the fish.
            currentMovementDirection = newRotation * Vector3.forward;

            // Pack the updated values back into the Fish struct.
            BoidInformation.Acceleration = currentAcceleration;
            BoidInformation.MovementDirection = currentMovementDirection;
            BoidInformation.AngularVelocity = currentAngularVelocity;
            BoidInformation.AngularAcceleration = currentAngularAcceleration;
        }

        /// <summary>
        /// Function updates the current movement speed and acceleration of the fish. The output values are then stored back into the <paramref name="BoidInformation"/> structure.
        /// </summary>
        /// <param name="shouldAccelerate">Should the fish increase its acceleration in this frame or not.</param>
        /// <param name="timeDelta">Time passed from the last movement speed update.</param>
        /// <param name="fishMovementProperties">Reference to the scriptable object containing the movement properties of this fish species.</param>
        /// <param name="BoidInformation">Reference to the structure containing the current properties of this fish instance.</param>
        public static void UpdateMovementSpeed(bool shouldAccelerate, float timeDelta, FishMovementProperties fishMovementProperties, ref BoidInformation BoidInformation)
        {
            // Unpacking properties from the Fish struct into a more readable format.
            float currentAcceleration = BoidInformation.Acceleration;
            float currentMovementSpeed = BoidInformation.Speed;

            // Update current movement acceleration based on user's input.
            float currentJerk = shouldAccelerate ? fishMovementProperties.MovementJerk : 0.0f;
            float deceleration = currentJerk > 0f ? 0f : fishMovementProperties.Deceleration;
            currentAcceleration = Mathf.Clamp(currentAcceleration + (currentJerk - deceleration) * timeDelta, 0f, fishMovementProperties.MaxAcceleration);

            // Check if we need to increase acceleration in order to maintain the cruise movement speed.
            if ((currentMovementSpeed + (currentAcceleration - fishMovementProperties.WaterFriction * currentMovementSpeed) * Time.deltaTime) < fishMovementProperties.CruisingSpeed)
            {
                // Set the acceleration to the one that will maintain the cruise velocity.
                currentAcceleration = fishMovementProperties.CruisingSpeed * fishMovementProperties.WaterFriction;
            }

            // Update current movement speed based on the acceleration.
            currentMovementSpeed += (currentAcceleration - fishMovementProperties.WaterFriction * currentMovementSpeed) * timeDelta;
            currentMovementSpeed = Mathf.Clamp(currentMovementSpeed, fishMovementProperties.CruisingSpeed, fishMovementProperties.MaxSpeed);

            // Pack the updated values back into the Fish struct.
            BoidInformation.Acceleration = currentAcceleration;
            BoidInformation.Speed = currentMovementSpeed;
        }

        /// <summary>
        /// Function updates the position and rotation of the provided <paramref name="transform"/> and caches the 
        /// updated position in the <paramref name="BoidInformation"/> structure.
        /// </summary>
        /// <param name="timeDelta">Time passed from last position and rotation update.</param>
        /// <param name="transform">Reference to the <see cref="Transform"/> component whose position and rotation will be updated.</param>
        /// <param name="BoidInformation">Reference to the structure containing the current properties of this fish instance.</param>
        public static void UpdatePositionAndRotation(float timeDelta, Transform transform, ref BoidInformation BoidInformation)
        {
            // Update the position and the rotation of the fish.
            Vector3 currentMovementDirection = BoidInformation.MovementDirection;
            Vector3 newPosition = BoidInformation.Position + currentMovementDirection * timeDelta * BoidInformation.Speed;
            Quaternion newRotation = Quaternion.LookRotation(currentMovementDirection, Vector3.up);
            transform.SetPositionAndRotation(newPosition, newRotation);

            // Pack the updated values back into the Fish struct.
            BoidInformation.Position = newPosition;
        }

        /// <summary>
        /// Function checks if the movement in current direction will result in leaving the simulation area bounds. Also, if we're already
        /// outside the simulation area bounds, the function will return true.
        /// </summary>
        /// <param name="currentPosition">Current position of the boid.</param>
        /// <param name="currentMovementDirection">Current movement direction of the boid (normalized).</param>
        /// <param name="currentMovementSpeed">Current movement speed of the boid [m/s].</param>
        /// <param name="simulationAreaBounds">Bounds structure representing the current simulation area for the boids.</param>
        /// <returns>True if the boid is currently outside the simulation area, or if the movement with the current speed will result leaving the area.</returns>
        public static bool WillLeaveSimulationArea(Vector3 currentPosition, Vector3 currentMovementDirection, float currentMovementSpeed, Bounds simulationAreaBounds)
        {
            // If we're already outside the area bounds, no need to check anything else.
            if (!simulationAreaBounds.Contains(currentPosition))
            {
                return true;
            }

            Vector3 newPosition = currentPosition + currentMovementDirection.normalized * currentMovementSpeed;
            return !simulationAreaBounds.Contains(newPosition);
        }
    }
}