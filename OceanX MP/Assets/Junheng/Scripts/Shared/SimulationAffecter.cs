using System;
using UnityEngine;

namespace OceanX
{
    /// <summary>
    /// Enumeration specifying currently supported simulation affecters.
    /// Target  — fish steer toward it (schools follow their wandering target).
    /// Obstacle — casual avoidance: fish curve around it, no panic, no speed-up.
    /// Predator — fleeing: fish steer straight away AND accelerate to escape.
    /// </summary>
    [Serializable]
    public enum SimulationAffecterType : byte
    {
        Target = 0,
        Obstacle = 1,
        Predator = 2
    }

    /// <summary>
    /// Shape of an affecter's influence volume. Sphere uses <see cref="SimulationAffecter.Radius"/>;
    /// Box uses <see cref="SimulationAffecter.HalfExtents"/> + <see cref="SimulationAffecter.Rotation"/>
    /// (an oriented box). A sphere is rotationally symmetric, so rotation is ignored for it.
    /// </summary>
    [Serializable]
    public enum SimulationAffecterShape : byte
    {
        Sphere = 0,
        Box = 1
    }

    /// <summary>
    /// Structure defining properties of an affecter that the fish need to avoid or go towards.
    /// </summary>
    [Serializable]
    public struct SimulationAffecter
    {
        public const int ALL_BOIDS_AFFECTER_ID = 255;

        /// <summary>
        /// World position of the affecter.
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// Radius of the affecter (Sphere shape). If the fish gets closer than this distance from the
        /// target, the target will stop affecting it.
        /// </summary>
        public float Radius;
        /// <summary>
        /// Influence half-extents for the Box shape (per-axis, in the box's local space).
        /// Ignored when <see cref="Shape"/> is Sphere.
        /// </summary>
        public Vector3 HalfExtents;
        /// <summary>
        /// Orientation of the Box shape. Ignored when <see cref="Shape"/> is Sphere.
        /// </summary>
        public Quaternion Rotation;
        /// <summary>
        /// Whether this affecter's influence volume is a Sphere or an (oriented) Box.
        /// </summary>
        public SimulationAffecterShape Shape;
        /// <summary>
        /// Type of the affecter.
        /// </summary>
        public SimulationAffecterType Type;
        /// <summary>
        /// ID of the boid group that this affecter affects.
        /// </summary>
        public int BoidGroupId;
        /// <summary>
        /// ID of the sub-group, inside one boid group (fish species) that this affecter affects.
        /// This ID is used to specify affecters that only affect specific sub-groups in larger simulations,
        /// to add additional diversity and look to the whole thing.
        /// </summary>
        public int BoidSubGroupId;

        public bool AffectsAllGroups { get => BoidGroupId == ALL_BOIDS_AFFECTER_ID; }

        /// <summary>
        /// Property specifying if this affecter affects all boid sub groups or not. The value 255 is used for comparison
        /// since values 0-254 are used for specific group IDs, while this represents a flag that affects all sub-groups.
        /// </summary>
        public bool AffectsAllSubGroups { get => BoidSubGroupId == ALL_BOIDS_AFFECTER_ID; }
    }
}