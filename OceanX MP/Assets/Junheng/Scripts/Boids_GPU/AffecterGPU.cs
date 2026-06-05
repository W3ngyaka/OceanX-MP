using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace OceanX.BoidsGPU
{
    /// <summary>
    /// Structure defining information about the simulation affecter that affects the
    /// movement of the boids throughout the simulation. Affecters can be either considered
    /// as targets or as obstacles.
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct AffecterGPU
    {
        /// <summary>
        /// Total size of the struct, in bytes. Manually pre-calculated.
        /// </summary>
        public const int Size = sizeof(float) * 8;

        /// <summary>
        /// World position of the affecter.
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// Size of the affecter, determining the distance from affecter where its impact is non-existent.
        /// </summary>
        public float Radius;

        /// <summary>
        /// Type of the affecter (0 --> Target, 1 --> Obstacle).
        /// </summary>
        public float AffecterType;
        /// <summary>
        /// Which boid group does this affecter affect.
        /// </summary>
        public float BoidGroupId;
        /// <summary>
        /// Sub-group inside the fish species that this boid belongs to.
        /// </summary>
        public float BoidSubGroupId;
        /// <summary>
        /// Currently not used, but added to fill to the 16-byte memory slot.
        /// </summary>
        public float EmptyFiller;
    }
}