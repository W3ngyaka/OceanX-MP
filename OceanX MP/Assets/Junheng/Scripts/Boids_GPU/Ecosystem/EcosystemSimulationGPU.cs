using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OceanX.BoidsGPU.SpatialPartitionInstancedRendering;

namespace OceanX.BoidsGPU.Ecosystem
{
    public class EcosystemSimulationGPU : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EcosystemDefinitionGPU _ecosystem;

        [Tooltip("The BoidSimulationGPU (03_Spatial_Partition_Instanced_Rendering) in the scene.")]
        [SerializeField] private BoidSimulationGPU _simulation;

        public EcosystemDefinitionGPU Ecosystem => _ecosystem;

        [Header("Population Tick")]
        [Tooltip("Seconds between each population tick.")]
        [SerializeField] private float _tickInterval = 5f;

        // -------------------------------------------------------------------------
        // Runtime state — every species starts at zero schools (empty ocean). The
        // player builds the ecosystem up from nothing; schools are added/removed one
        // at a time and a species is excluded from the simulation entirely at N == 0.
        // -------------------------------------------------------------------------

        // Current number of schools per species — the one piece of genuine runtime state.
        // ALL species start at 0. A species is in this dictionary iff it has a matching spawner.
        // FishPerSchool / MaxSchools / carrying capacity are constant authoring data and are read
        // straight off the SpeciesDataGPU asset (see FishPerSchool / MaxSchoolsOf helpers).
        private readonly Dictionary<SpeciesDataGPU, int> _schoolCount = new Dictionary<SpeciesDataGPU, int>();
        // Animated roaming targets created at runtime, one per school, owned by this manager so they
        // can be destroyed on removal/extinction with no leaks.
        private readonly Dictionary<SpeciesDataGPU, List<SimulationAffecterComponent>> _managedTargets
            = new Dictionary<SpeciesDataGPU, List<SimulationAffecterComponent>>();

        // Constant per-species authoring values, clamped to a sane minimum. Read directly from the
        // asset so there is a single source of truth (no cached copies to drift out of sync).
        private static int FishPerSchool(SpeciesDataGPU species) => Mathf.Max(1, species.FishPerSchool);
        private static int MaxSchoolsOf(SpeciesDataGPU species) => Mathf.Max(1, species.MaxSchools);

        // -------------------------------------------------------------------------
        // Unity lifecycle — Awake runs before any Start, so spawners are set inactive
        // (N = 0) before BoidSimulationGPU.Start initializes the GPU buffers. The sim
        // therefore boots into a valid "empty ocean" state.
        // -------------------------------------------------------------------------

        private void Awake()
        {
            if (_ecosystem == null)
            {
                Debug.LogError("[EcosystemSimulationGPU] EcosystemDefinitionGPU not assigned.", this);
                enabled = false;
                return;
            }

            if (_simulation == null)
            {
                Debug.LogError("[EcosystemSimulationGPU] BoidSimulationGPU not assigned.", this);
                enabled = false;
                return;
            }

            SetupAllSpecies();
        }

        private void Start()
        {
            StartCoroutine(PopulationTickRoutine());
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>Adds one school to this species (up to its MaxSchools cap) and rebuilds GPU buffers. Called by UI.</summary>
        public void AddSpecies(SpeciesDataGPU species)
        {
            if (!ValidateSpecies(species, out BoidSpawnerGPUMultiTargets spawner)) return;
            if (AddSchool(species, spawner))
            {
                _simulation.ReinitializeBuffers();
            }
        }

        /// <summary>Removes one school from this species (down to extinction at 0) and rebuilds GPU buffers. Called by UI.</summary>
        public void RemoveSpecies(SpeciesDataGPU species)
        {
            if (!ValidateSpecies(species, out BoidSpawnerGPUMultiTargets spawner)) return;
            if (RemoveSchool(species, spawner))
            {
                _simulation.ReinitializeBuffers();
            }
        }

        /// <summary>
        /// Returns the current number of schools for this species (one unit = one school).
        /// 0 means the species is extinct / not yet added.
        /// </summary>
        public int CountGroups(SpeciesDataGPU species)
        {
            if (species == null) return 0;
            return _schoolCount.TryGetValue(species, out int n) ? n : 0;
        }

        /// <summary>Static per-species cap on the number of schools. 0 if the species is null.</summary>
        public int GetMaxSchools(SpeciesDataGPU species)
        {
            if (species == null) return 0;
            return MaxSchoolsOf(species);
        }

        // -------------------------------------------------------------------------
        // Population tick — manual add/remove plus the emergent starvation cascade.
        // Natural births and natural deaths are intentionally NOT modelled; population
        // only changes via the UI and the prey-ratio starvation below.
        // -------------------------------------------------------------------------

        private IEnumerator PopulationTickRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(_tickInterval);
                RunPopulationTick();
            }
        }

        private void RunPopulationTick()
        {
            bool anyChanged = false;

            foreach (SpeciesDataGPU species in _ecosystem.Species)
            {
                if (species == null) continue;
                if (!_schoolCount.TryGetValue(species, out int n) || n <= 0) continue; // already extinct / not present

                BoidSpawnerGPUMultiTargets spawner = FindSpawner(species);
                if (spawner == null) continue;

                // Starvation — fires if any prey species is below its starvation threshold (ratio-based).
                // Ratio = prey's live fish (N * FishPerSchool) over its carrying capacity (MaxSchools * FishPerSchool).
                bool starved = false;
                foreach (SpeciesDataGPU prey in species.PreySpecies)
                {
                    if (prey == null) continue;
                    if (!_schoolCount.TryGetValue(prey, out int preyN)) continue;
                    int preyFish = preyN * FishPerSchool(prey);
                    int preyCap  = MaxSchoolsOf(prey) * FishPerSchool(prey); // carrying capacity in fish
                    float preyRatio = (float)preyFish / Mathf.Max(1, preyCap);
                    if (preyRatio < species.StarvationThreshold)
                    {
                        if (Random.value < species.StarvationDeathRate) starved = true;
                        break;
                    }
                }

                // Starvation can remove the LAST school (extinction) — the only guard is N > 0,
                // which we already checked above.
                if (starved && RemoveSchool(species, spawner)) anyChanged = true;
            }

            if (anyChanged) _simulation.ReinitializeBuffers();
        }

        // -------------------------------------------------------------------------
        // Internal school mutation — no GPU rebuild here; the caller rebuilds once.
        // Returns true if the school count actually changed.
        // -------------------------------------------------------------------------

        private bool AddSchool(SpeciesDataGPU species, BoidSpawnerGPUMultiTargets spawner)
        {
            int n = _schoolCount.TryGetValue(species, out int current) ? current : 0;
            if (n >= MaxSchoolsOf(species)) return false; // at cap — no-op

            int fishPerSchool = FishPerSchool(species);

            // Create the target for the new school FIRST so the spawner has >= N targets before the
            // rebuild (BoidSpawnerGPUMultiTargets needs one target per sub-group). The new school's
            // sub-group index is the old count n (0-based).
            CreateTarget(species, spawner, n);

            int newN = n + 1;
            _schoolCount[species] = newN;
            spawner.SetSchoolConfiguration(newN, fishPerSchool);
            return true;
        }

        private bool RemoveSchool(SpeciesDataGPU species, BoidSpawnerGPUMultiTargets spawner)
        {
            int n = _schoolCount.TryGetValue(species, out int current) ? current : 0;
            if (n <= 0) return false; // already extinct — no-op

            int fishPerSchool = FishPerSchool(species);

            int newN = n - 1;
            _schoolCount[species] = newN;

            // Destroy the last school's target (the one beyond the new count) so targets and schools
            // stay in lockstep and no GameObjects leak.
            DestroyLastTarget(species, spawner);

            // newN == 0 deactivates the spawner (excluded from the simulation entirely).
            spawner.SetSchoolConfiguration(newN, fishPerSchool);
            return true;
        }

        // -------------------------------------------------------------------------
        // Runtime target lifecycle — one animated roaming target per school, for ALL
        // species. This unifies the previously apex-only WanderingAffecterGPU path so
        // every role behaves the same: groups, targets, animators and N move together.
        // -------------------------------------------------------------------------

        private void CreateTarget(SpeciesDataGPU species, BoidSpawnerGPUMultiTargets spawner, int subGroupIndex)
        {
            WanderingAffecterGPU wanderer = CreateAndInitWanderer(species, subGroupIndex);
            spawner.AddTarget(wanderer);

            if (!_managedTargets.TryGetValue(species, out List<SimulationAffecterComponent> targets))
            {
                targets = new List<SimulationAffecterComponent>();
                _managedTargets[species] = targets;
            }
            targets.Add(wanderer);
        }

        private void DestroyLastTarget(SpeciesDataGPU species, BoidSpawnerGPUMultiTargets spawner)
        {
            if (!_managedTargets.TryGetValue(species, out List<SimulationAffecterComponent> targets) || targets.Count == 0)
                return;

            int lastIndex = targets.Count - 1;
            SimulationAffecterComponent target = targets[lastIndex];
            targets.RemoveAt(lastIndex);

            if (target != null)
            {
                spawner.RemoveTarget(target);
                Destroy(target.gameObject);
            }
        }

        // Creates a WanderingAffecterGPU on a new child GameObject and configures its sub-group ID.
        private WanderingAffecterGPU CreateAndInitWanderer(SpeciesDataGPU species, int subGroupIndex)
        {
            GameObject go = new GameObject($"{species.SpeciesName}_Target_SubGroup{subGroupIndex}");
            go.transform.SetParent(transform);

            WanderingAffecterGPU wanderer = go.AddComponent<WanderingAffecterGPU>();
            wanderer.SetSubGroupID(subGroupIndex);
            wanderer.SetAffecterType(SimulationAffecterType.Target);
            wanderer.Initialize(_ecosystem.SimulationBounds);
            return wanderer;
        }

        // -------------------------------------------------------------------------
        // Setup helpers
        // -------------------------------------------------------------------------

        private void SetupAllSpecies()
        {
            for (int i = 0; i < _ecosystem.Species.Count; i++)
            {
                SpeciesDataGPU species = _ecosystem.Species[i];
                if (species == null) continue;

                BoidSpawnerGPUMultiTargets spawner = FindSpawner(species);
                if (spawner == null)
                {
                    Debug.LogWarning($"[EcosystemSimulationGPU] '{species.SpeciesName}' has no matching spawner in the simulation — skipping.");
                    continue;
                }

                species.RuntimeId = i;

                _schoolCount[species]    = 0;
                _managedTargets[species] = new List<SimulationAffecterComponent>();

                // Start every species at zero schools: clear any inspector-assigned targets and mark the
                // spawner inactive so it is excluded from the GPU simulation until the player adds a school.
                spawner.ClearTargets();
                spawner.SetSchoolConfiguration(0, FishPerSchool(species));
            }
        }

        private BoidSpawnerGPUMultiTargets FindSpawner(SpeciesDataGPU species)
        {
            if (species == null) return null;
            BoidSpawnerBase[] spawners = _simulation.GetBoidSpawners();
            if (spawners == null) return null;
            foreach (BoidSpawnerBase s in spawners)
            {
                if (s is BoidSpawnerGPUMultiTargets mt && mt.SpeciesData == species)
                    return mt;
            }
            return null;
        }

        private bool ValidateSpecies(SpeciesDataGPU species, out BoidSpawnerGPUMultiTargets spawner)
        {
            spawner = null;
            if (species == null)
            {
                Debug.LogWarning("[EcosystemSimulationGPU] Null species passed.");
                return false;
            }
            spawner = FindSpawner(species);
            if (spawner == null)
            {
                Debug.LogWarning($"[EcosystemSimulationGPU] No spawner in the simulation has SpeciesData '{species.SpeciesName}' assigned.");
                return false;
            }
            return true;
        }

        // -------------------------------------------------------------------------
        // Editor visualisation
        // -------------------------------------------------------------------------

        private void OnDrawGizmosSelected()
        {
            if (_ecosystem == null) return;
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.12f);
            Gizmos.DrawCube(_ecosystem.SimulationCenter, _ecosystem.SimulationSize);
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.5f);
            Gizmos.DrawWireCube(_ecosystem.SimulationCenter, _ecosystem.SimulationSize);
        }
    }
}
