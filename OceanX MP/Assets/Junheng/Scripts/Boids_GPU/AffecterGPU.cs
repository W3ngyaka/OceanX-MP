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
        /// MUST match the HLSL 'Affecter' struct in BoidSimulationData.hlsl field-for-field, in order.
        /// Layout (16 floats = 64 bytes): position(3) radius(1) halfExtents(3) shape(1)
        /// rotation(4) affecterType(1) boidGroupId(1) boidSubGroupId(1) emptyFiller(1).
        /// Each float3 is followed by a scalar and the float4 sits at a 16-byte boundary so
        /// C# sequential layout and HLSL structured-buffer packing line up.
        /// </summary>
        public const int Size = sizeof(float) * 16;

        /// <summary>
        /// World position of the affecter.
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// Sphere radius — distance from the affecter where its impact is non-existent (Sphere shape).
        /// </summary>
        public float Radius;

        /// <summary>
        /// Box influence half-extents in local space (Box shape). Unused for Sphere.
        /// </summary>
        public Vector3 HalfExtents;
        /// <summary>
        /// Shape flag: 0 --> Sphere, 1 --> Box.
        /// </summary>
        public float Shape;

        /// <summary>
        /// Box orientation quaternion (x, y, z, w). Unused for Sphere.
        /// </summary>
        public Quaternion Rotation;

        /// <summary>
        /// Type of the affecter (0 --> Target, 1 --> Obstacle, 2 --> Predator).
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
        /// Currently not used, padding to a 16-float (64-byte) stride.
        /// </summary>
        public float EmptyFiller;
    }
}