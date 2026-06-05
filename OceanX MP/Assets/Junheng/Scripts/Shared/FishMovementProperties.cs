using UnityEngine;

namespace OceanX
{
    /// <summary>
    /// Scriptable object describing movement properties of one fish species.
    /// </summary>
    [CreateAssetMenu(fileName = nameof(FishMovementProperties), menuName = "ScriptableObjects/" + nameof(FishMovementProperties))]
    public class FishMovementProperties : ScriptableObject
    {
        [Header("Speed Settings: ")]
        /// <summary>
        /// Speed [m/s] of the normal swimming for this fish species.
        /// After acceleration, this is the speed that the fish will try to maintain.
        /// </summary>
        public float CruisingSpeed = 1.75f;
        /// <summary>
        /// Maximum possible speed [m/s] for this fish species.
        /// </summary>
        public float MaxSpeed = 12f;
        /// <summary>
        /// Percentage of velocity lost every second due to the water friction.
        /// </summary>
        [Range(0f, 1f)] public float WaterFriction = 0.45f;

        [Header("Acceleration Settings: ")]
        /// <summary>
        /// How fast does the movement speed reduces over the period of one second
        /// after the fish stops accelerating.
        /// </summary>
        public float Deceleration = 15f;
        /// <summary>
        /// Max possible acceleration of this fish species.
        /// </summary>
        public float MaxAcceleration = 20f;
        /// <summary>
        /// Maximum change in acceleration over one second.
        /// </summary>
        public float MovementJerk = 1000f;

        [Header("Rotation Settings: ")]
        /// <summary>
        /// Maximum possible angular velocity that this fish species can have.
        /// How fast does the fish rotates over time (in degrees).
        /// </summary>
        public float MaxAngularVelocity = 180.0f;
        /// <summary>
        /// How fast does the angular velocity reduces over time after the fish stops turning.
        /// </summary>
        public float AngularVelocityReduction = 200.0f;
        /// <summary>
        /// Maximum possible angular acceleration that this fish species can have.
        /// Specifies the change of angular velocity over time.
        /// </summary>
        public float MaxAngularAcceleration = 85.0f;
        /// <summary>
        /// How fast does the angular velocity changes over time when the fish
        /// isn't accelerating. Usually happens on straight periods of movement.
        /// </summary>
        public float AngularDeceleration = 30f;
        /// <summary>
        /// How fast does the angular acceleration increases once the fish starts
        /// to rotate and change direction.
        /// </summary>
        public float AngularJerk = 1600.0f;
        /// <summary>
        /// Percentage of angular acceleration that gets transferred to movement acceleration.
        /// </summary>
        [Range(0f, 0.5f)] public float RotationEffectOnSpeed = 0.05f;
    }
}