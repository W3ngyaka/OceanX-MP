using System.Collections.Generic;
using UnityEngine;

namespace OceanX.BoidsGPU.Ecosystem
{
    public enum SpeciesRoleGPU
    {
        Apex,
        Mesopredator,
        Prey,
        Neutral
    }

    /// <summary>
    /// Single source of truth for a GPU species.
    /// All four simulation scriptable objects live here so every species property
    /// can be configured from one asset. BoidSpawnerGPUMultiTargets reads from here
    /// instead of having FishSchoolProperties set directly on BoidSpawnData.
    /// </summary>
    [CreateAssetMenu(fileName = "SpeciesDataGPU", menuName = "OceanX/Species Data")]
    public class SpeciesDataGPU : ScriptableObject
    {
        [Header("Identity")]
        public string         SpeciesName = "Unknown Species";
        public string ScientificName = "Scientific Name";
        public SpeciesRoleGPU Role        = SpeciesRoleGPU.Prey;

        [Header("Simulation Properties")]
        [Tooltip("Flocking weights and ranges (vision, separation, cohesion, alignment, target weight).")]
        public FishSchoolProperties         SchoolProperties;

        [Tooltip("Speed, acceleration, deceleration and rotation settings.")]
        public FishMovementProperties       MovementProperties;

        [Tooltip("Swimming animation amplitude and playback speed ranges.")]
        public FishMotionRenderProperties   MotionRenderProperties;

        [Tooltip("Hunting and fleeing AI settings. Leave null for Prey and Neutral.")]
        public SpeciesBehaviorPropertiesGPU BehaviorProperties;

        [Header("School Scaling")]
        [Tooltip("Number of fish in a single school. Each Add spawns one more school of this many fish; " +
                 "schools always stay this size — adding makes MORE schools, not denser ones.")]
        [Range(1, 200)] public int FishPerSchool = 10;

        [Tooltip("Maximum number of schools the player can add for this species. " +
                 "Add greys out once the school count reaches this cap.")]
        [Range(1, 50)] public int MaxSchools = 5;

        [Header("Predator-Prey Relationships")]
        public List<SpeciesDataGPU> PreySpecies     = new List<SpeciesDataGPU>();
        public List<SpeciesDataGPU> PredatorSpecies = new List<SpeciesDataGPU>();

        // Set at runtime by EcosystemSimulationGPU.
        [HideInInspector] public int RuntimeId = -1;
    }
}
