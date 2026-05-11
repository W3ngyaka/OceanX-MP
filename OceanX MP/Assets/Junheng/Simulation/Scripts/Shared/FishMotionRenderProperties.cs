using UnityEngine;

namespace OceanX
{
    /// <summary>
    /// Scriptable object containing properties that guide the visual look of the motion of the fish.
    /// Each different property has a min and a max value that determine the range of possible values
    /// that the fish can have based on its current acceleration. The min values are applied at the 
    /// cruising acceleration of the fish (normal swimming), while the max values are applied when the
    /// fish is fully accelerating.
    /// </summary>
    [CreateAssetMenu(fileName = nameof(FishMotionRenderProperties), menuName = "ScriptableObjects/" + nameof(FishMotionRenderProperties))]
    public class FishMotionRenderProperties : ScriptableObject
    {
        public const float MIN_PLAYBACK_SPEED = 0f;
        public const float MAX_PLAYBACK_SPEED = 6f;
        public const float MIN_SIDE_TO_SIDE_AMPLITUDE = 0f;
        public const float MAX_SIDE_TO_SIDE_AMPLITUDE = 10f;
        public const float MIN_YAW_ROTATION_AMPLITUDE = 0f;
        public const float MAX_YAW_ROTATION_AMPLITUDE = 0.025f;
        public const float MIN_ROLL_ROTATION_AMPLITUDE = 0f;
        public const float MAX_ROLL_ROTATION_AMPLITUDE = 0.15f;
        public const float MIN_PANNING_YAW_AMPLITUDE = 0f;
        public const float MAX_PANNING_YAW_AMPLITUDE = 0.1f;

        /// <summary>
        /// Playback speed of the fish swimming animation at the cruise acceleration.
        /// </summary>
        [Range(MIN_PLAYBACK_SPEED, MAX_PLAYBACK_SPEED)] public float MinSwimPlaybackSpeed = 0.75f;
        /// <summary>
        /// Playback speed of the fish swimming animation at the max acceleration.
        /// </summary>
        [Range(MIN_PLAYBACK_SPEED, MAX_PLAYBACK_SPEED)] public float MaxSwimPlaybackSpeed = 4.25f;
        [Space]
        /// <summary>
        /// Side to side amplitude of the fish swimming animation at the cruise acceleration.
        /// </summary>
        [Range(MIN_SIDE_TO_SIDE_AMPLITUDE, MAX_SIDE_TO_SIDE_AMPLITUDE)] public float MinSideToSideAmplitude = 1f;
        /// <summary>
        /// Side to side amplitude of the fish swimming animation at the max acceleration.
        /// </summary>
        [Range(MIN_SIDE_TO_SIDE_AMPLITUDE, MAX_SIDE_TO_SIDE_AMPLITUDE)] public float MaxSideToSideAmplitude = 3f;
        [Space]
        /// <summary>
        /// Yaw rotation amplitude of the fish swimming animation at the cruise acceleration.
        /// </summary>
        [Range(MIN_YAW_ROTATION_AMPLITUDE, MAX_YAW_ROTATION_AMPLITUDE)] public float MinYawRotationAmplitude = 0.004f;
        /// <summary>
        /// Yaw rotation amplitude of the fish swimming animation at the max acceleration.
        /// </summary>
        [Range(MIN_YAW_ROTATION_AMPLITUDE, MAX_YAW_ROTATION_AMPLITUDE)] public float MaxYawRotationAmplitude = 0.008f;
        [Space]
        /// <summary>
        /// Roll rotation amplitude of the fish swimming animation at the cruise acceleration.
        /// </summary>
        [Range(MIN_ROLL_ROTATION_AMPLITUDE, MAX_ROLL_ROTATION_AMPLITUDE)] public float MinRollRotationAmplitude = 0.014f;
        /// <summary>
        /// Roll rotation amplitude of the fish swimming animation at the max acceleration.
        /// </summary>
        [Range(MIN_ROLL_ROTATION_AMPLITUDE, MAX_ROLL_ROTATION_AMPLITUDE)] public float MaxRollRotationAmplitude = 0.082f;
        [Space]
        /// <summary>
        /// Panning Yaw rotation amplitude of the fish swimming animation at the cruise acceleration.
        /// </summary>
        [Range(MIN_PANNING_YAW_AMPLITUDE, MAX_PANNING_YAW_AMPLITUDE)] public float MinPanningYawAmplitude = 0.01f;
        /// <summary>
        /// Panning Yaw rotation amplitude of the fish swimming animation at the max acceleration.
        /// </summary>
        [Range(MIN_PANNING_YAW_AMPLITUDE, MAX_PANNING_YAW_AMPLITUDE)] public float MaxPanningYawAmplitude = 0.05f;
    }
}