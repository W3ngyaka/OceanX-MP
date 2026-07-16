using System;
using System.Runtime.InteropServices;

namespace OceanX.BoidsGPU
{
    /// <summary>
    /// Structure containing information about one boid school. Since we're simulating fish 
    /// behavior, this structure basically holds information about one fish species.
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct BoidSchoolInfoGPU
    {
        /// <summary>
        /// Total size of the struct, in bytes. Manually pre-calculated.
        /// </summary>
        public const int Size = sizeof(float) * 20;

        /// <summary>
        /// Sensing distance of other fish around this one, for moving in line with them.
        /// </summary>
        public float VisionRangeSquared;
        /// <summary>
        /// Sensing distance for surrounding obstacles.
        /// </summary>
        public float ObstacleAvoidanceRangeSquared;
        /// <summary>
        /// Sensing distance for other fish around this one, for moving away from them.
        /// </summary>
        public float SeparationRangeSquared;
        /// <summary>
        /// Multiplier representing how strongly we want to keep our distance from other fish.
        /// </summary>
        public float SeparationWeight;

        /// <summary>
        /// Multiplier representing how strongly we want to keep close to other fish.
        /// </summary>
        public float CohesionWeight;
        /// <summary>
        /// Multiplier representing how strongly we want to swim in the same direction as other fish.
        /// </summary>
        public float AlignmentWeight;
        /// <summary>
        /// Multiplier representing how strongly we want to follow the closest target.
        /// </summary>
        public float TargetFollowWeight;
        /// <summary>
        /// Normal swimming speed of this fish species.
        /// </summary>
        public float CruisingSpeed;

        /// <summary>
        /// Maximum swimming speed of this fish species.
        /// </summary>
        public float MaxSpeed;
        /// <summary>
        /// Water friction expressed as a percentage of the current movement speed.
        /// </summary>
        public float WaterFriction;
        /// <summary>
        /// How fast will the fish loose velocity when it starts slowing down.
        /// </summary>
        public float Deceleration;
        /// <summary>
        /// Maximum possible acceleration that the fish can have.
        /// </summary>
        public float MaxAcceleration;

        /// <summary>
        /// Basically determines how fast can acceleration increase.
        /// </summary>
        public float MovementJerk;
        /// <summary>
        /// Max angular velocity that the fish can reach.
        /// </summary>
        public float MaxAngularVelocity;
        /// <summary>
        /// Angular deceleration of the angular velocity when the fish is not trying to turn. Used for smooth movement after turning.
        /// </summary>
        public float AngularVelocityReduction;
        /// <summary>
        /// Max angular acceleration that the fish can reach.
        /// </summary>
        public float MaxAngularAcceleration;

        /// <summary>
        /// Angular deceleration of the acceleration when the fish is not trying to turn. Used for smooth movement after turning.
        /// </summary>
        public float AngularDeceleration;
        /// <summary>
        /// Basically determines how fast can angular acceleration increase.
        /// </summary>
        public float AngularJerk;
        /// <summary>
        /// How much of angular acceleration is transferred to movement acceleration.
        /// </summary>
        public float RotationEffectOnSpeed;
        /// <summary>
        /// 0 = avoid obstacles in full 3D (shortest way clear, usually over the top).
        /// 1 = avoid strictly horizontally, so a bottom-dweller banks around reef structure instead of
        /// climbing it and drifting off the seabed. See <see cref="FishSchoolProperties.HorizontalAvoidBias"/>.
        /// (This slot used to be EmptyFiller, so the struct size is unchanged.)
        /// </summary>
        public float HorizontalAvoidBias;
    }
}