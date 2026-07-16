using UnityEngine;

namespace OceanX
{
    /// <summary>
    /// Scriptable object describing behavioral properties of a single school of fish.
    /// </summary>
    [CreateAssetMenu(fileName = nameof(FishSchoolProperties), menuName = "ScriptableObjects/" + nameof(FishSchoolProperties))]
    public class FishSchoolProperties: ScriptableObject
    {
        [Header("Movement Pattern Settings: ")]
        /// <summary>
        /// Distance in meters from this fish inside which other fish, obstacles and attractors can affect
        /// its behaviour. Everything outside this distance doesn't have any influence on this fish.
        /// </summary>
        [Range(0f, 100f)] public float VisionRange = 10f;
        /// <summary>
        /// Distance from obstacle at which the fish will start prioritizing its avoidance.
        /// </summary>
        [Range(0f, 20f)] public float ObstacleAvoidanceRange = 5f; 
        /// <summary>
        /// Distance at which the fish will start to avoid other fishes. This is basically how introverted
        /// the fish is in relation to the rest of the flock.
        /// </summary>
        [Range(0f, 100f)] public float SeparationRange = 5f;
        /// <summary>
        /// How strongly should the fish try to get as close as possible to the center of the school.
        /// </summary>
        [Range(0f, 10f)] public float CohesionWeight = 0.5f;
        /// <summary>
        /// How strongly should the fish try to align its direction with the average movement direction of the school.
        /// </summary>
        [Range(0f, 10f)] public float AlignmentWeight = 0.7f;
        /// <summary>
        /// How strongly should the fish try to separate itself from the rest of the school and 
        /// maintain a safe distance from them.
        /// </summary>
        [Range(0f, 10f)] public float SeparationWeight = 0.9f;
        /// <summary>
        /// How strongly should the fish try to reach the closest target.
        /// </summary>
        [Range(0f, 10f)] public float TargetWeight = 1.75f;

        [Tooltip("How strongly this species prefers to steer AROUND obstacles rather than OVER them.\n\n" +
                 "0 = free 3D avoidance. Faced with a rock, the fish takes the shortest way clear, which is " +
                 "usually up and over — correct for most fish.\n\n" +
                 "1 = strictly horizontal avoidance. The fish banks around a rock and never climbs it. Use " +
                 "for bottom-dwellers: a ray or moray that goes 'over' the reef ends up drifting off the " +
                 "seabed and, over time, up to the surface, because every escape vector near the substrate " +
                 "has an upward component and they accumulate.\n\n" +
                 "Ignored when an obstacle is directly above or below with nothing to steer around — a " +
                 "head-on dive into the seabed is still avoided vertically, whatever this is set to.")]
        [Range(0f, 1f)] public float HorizontalAvoidBias = 0f;

        // Hidden from Inspector — injected at runtime by BoidSpawnerGPUMultiTargets from SpeciesDataGPU.
        // Set these directly on SpeciesDataGPU; do NOT assign them here in the Inspector.
        [HideInInspector] public FishMovementProperties    MovementProperties    = null;
        [HideInInspector] public FishMotionRenderProperties MotionRenderProperties = null;
    }
}