using System;
using System.Runtime.InteropServices;

namespace OceanX.BoidsGPU
{
    /// <summary>
    /// Structure defining properties that affect the look of the swimming motion of a boid.
    /// Only required for the rendering part in the rendering shader.
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct BoidRenderInfoGPU
    {
        /// <summary>
        /// Total size of the struct, in bytes. Manually pre-calculated.
        /// </summary>
        public const int Size = sizeof(float) * 8;

        public float MinSideToSideAmplitude;
        public float MaxSideToSideAmplitude;
        public float MinYawRotationAmplitude;
        public float MaxYawRotationAmplitude;

        public float MinRollRotationAmplitude;
        public float MaxRollRotationAmplitude;
        public float MinPanningYawAmplitude;
        public float MaxPanningYawAmplitude;
    }
}