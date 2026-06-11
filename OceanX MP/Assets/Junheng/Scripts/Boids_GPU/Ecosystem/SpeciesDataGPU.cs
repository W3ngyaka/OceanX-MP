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
    [CreateAssetMenu(fileName = "SpeciesData", menuName = "OceanX/Species Data")]
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

        [Header("Population Dynamics")]
        [Tooltip("Additional chance per tick to lose one school when prey is scarce.")]
        [Range(0f, 1f)] public float StarvationDeathRate = 0.25f;
        [Tooltip("Fraction of prey's carrying capacity below which this species starts starving.")]
        [Range(0f, 1f)] public float StarvationThreshold = 0.20f;

        [Header("Predator-Prey Relationships")]
        public List<SpeciesDataGPU> PreySpecies     = new List<SpeciesDataGPU>();
        public List<SpeciesDataGPU> PredatorSpecies = new List<SpeciesDataGPU>();

        // Set at runtime by EcosystemSimulationGPU.
        [HideInInspector] public int RuntimeId = -1;
    }
}
