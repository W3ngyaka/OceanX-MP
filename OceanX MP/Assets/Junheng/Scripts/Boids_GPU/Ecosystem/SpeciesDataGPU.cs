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
    /// Shape of the path a school's Target Animator (TransformAnimator) moves its target along —
    /// the same three shapes as the original Boids_Demo. RandomShape (the default, so existing
    /// assets pick it up without editing) rolls one of the three per school. Whatever the shape,
    /// every school gets its OWN randomized centre, swim height, size, speed and direction — no
    /// two schools trace the same path — and the whole path always fits inside the simulation bounds.
    /// </summary>
    public enum SpeciesPathStyleGPU
    {
        RandomShape = 0,
        Circle,
        Rectangle,
        Line
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

        [Header("Movement Path")]
        [Tooltip("Path shape each school's roaming target follows. RandomShape picks Circle or Rectangle " +
                 "per school. Every school also gets its own randomized centre, height, size, speed and " +
                 "phase (inside the simulation bounds), so no two schools swim the same route.")]
        public SpeciesPathStyleGPU PathStyle = SpeciesPathStyleGPU.RandomShape;

        [Tooltip("Depth band this species prefers to roam in, as a FRACTION of the simulation area's " +
                 "height: x = bottom of the band, y = top, where 0 is the floor of the simulation bounds " +
                 "(the sand) and 1 is the ceiling. Each school's roaming target is placed at a random " +
                 "height inside this band, so the species settles into that slice of the water column: " +
                 "(0, 0.1) pins a ray to the seabed, (0.6, 0.95) keeps scad up near the pillar tops.\n\n" +
                 "Stored as a fraction, not a world Y, so the bands still mean the same thing if the " +
                 "simulation bounds are resized. Leave at (0, 1) to roam the whole water column.")]
        public Vector2 PreferredDepthBand = new Vector2(0f, 1f);

        [Header("Schooling Dynamics")]
        [Tooltip("Allow two schools OF THIS SPECIES that drift near each other to merge into one bigger " +
                 "school, and for a merged school to later break back apart. Both are rolled on the " +
                 "ecosystem tick. Off = schools of this species never combine (the original behaviour).")]
        public bool AllowSchoolMerging = false;

        [Tooltip("Chance per ecosystem tick that two schools of this species already within Merge Distance " +
                 "of each other combine. Rolled once per candidate pair.")]
        [Range(0f, 1f)] public float MergeChancePerTick = 0.25f;

        [Tooltip("Chance per ecosystem tick that a merged school of this species breaks back apart into " +
                 "the schools it was made of. Keep BELOW Merge Chance or shoals will never hold together.")]
        [Range(0f, 1f)] public float SplitChancePerTick = 0.08f;

        [Tooltip("How close two schools' centres must be (metres) before they are considered for merging. " +
                 "Also used for mixed-species shoaling below.")]
        [Min(0f)] public float MergeDistance = 8f;

        [Space(6)]
        [Tooltip("Allow schools of this species to form a MIXED shoal with a different species listed in " +
                 "Shoals With. Each fish keeps its own speed, size and behaviour - they just swim as one " +
                 "group. Independent of Allow School Merging: you can have either, both, or neither.")]
        public bool AllowMixedSpeciesSchooling = false;

        [Tooltip("Chance per ecosystem tick that this species forms a mixed shoal with a nearby eligible " +
                 "species. Deliberately rarer than the same-species Merge Chance - mixed shoals are the " +
                 "exception on a reef, not the norm.")]
        [Range(0f, 1f)] public float MixedMergeChancePerTick = 0.05f;

        [Tooltip("Chance per ecosystem tick that a mixed shoal breaks apart. Usually HIGHER than the " +
                 "same-species split chance: a mixed shoal is a looser, less stable association.")]
        [Range(0f, 1f)] public float MixedSplitChancePerTick = 0.15f;

        [Tooltip("Species this one is willing to form a mixed shoal with. Empty = none, so nothing mixes " +
                 "until you say what mixes with what. Only consulted when Allow Mixed Species Schooling " +
                 "is on. The pairing is checked from BOTH sides, so add each species to the other's list.")]
        public List<SpeciesDataGPU> ShoalsWith = new List<SpeciesDataGPU>();

        [Header("Predator-Prey Relationships")]
        public List<SpeciesDataGPU> PreySpecies     = new List<SpeciesDataGPU>();
        public List<SpeciesDataGPU> PredatorSpecies = new List<SpeciesDataGPU>();

        [Header("Rendering")]
        [Tooltip("Render this species with the serpentine path-following body (moray eel) instead of the " +
                 "standard rigid instanced fish shader. When ON, BoidSimulationGPU maintains a per-instance " +
                 "head-path trail buffer for this species and its spawner's material must use " +
                 "OceanX/Moray_Lit_Instanced. Only ONE species should enable this (buffers are sized for it).")]
        public bool UseSpineDeformation = false;

        // Set at runtime by EcosystemSimulationGPU.
        [HideInInspector] public int RuntimeId = -1;
    }
}
