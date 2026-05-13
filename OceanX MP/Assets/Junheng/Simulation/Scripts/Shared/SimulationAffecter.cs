using System;
using UnityEngine;

namespace OceanX
{
    /// <summary>
    /// Enumeration specifying currently supported simulation affecters.
    /// </summary>
    [Serializable]
    public enum SimulationAffecterType : byte
    {
        Target = 0, 
        Obstacle = 1
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
        /// Radius of the affecter, if the fish gets closer than this distance from the target, the 
        /// target will stop affecting it.
        /// </summary>
        public float Radius;
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