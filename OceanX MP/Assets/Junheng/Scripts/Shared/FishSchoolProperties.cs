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

        // Hidden from Inspector — injected at runtime by BoidSpawnerGPUMultiTargets from SpeciesDataGPU.
        // Set these directly on SpeciesDataGPU; do NOT assign them here in the Inspector.
        [HideInInspector] public FishMovementProperties    MovementProperties    = null;
        [HideInInspector] public FishMotionRenderProperties MotionRenderProperties = null;
    }
}