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

        // Wanderers created BY THIS MANAGER at runtime (Awake fallback + AddSpecies).
        // Does NOT include pre-existing targets from BoidSimulationTargetAnimatorsSpawner.
        private readonly Dictionary<SpeciesDataGPU, List<WanderingAffecterGPU>> _managedWanderers
            = new Dictionary<SpeciesDataGPU, List<WanderingAffecterGPU>>();

        // Derived from each spawner's initial state on Awake — used as the population ceiling.
        private readonly Dictionary<SpeciesDataGPU, int> _carryingCapacity = new Dictionary<SpeciesDataGPU, int>();
        // Boids per group at startup — kept fixed so each added/removed group scales total boid count by this amount.
        private readonly Dictionary<SpeciesDataGPU, int> _boidsPerGroup = new Dictionary<SpeciesDataGPU, int>();

        // -------------------------------------------------------------------------
        // Unity lifecycle — Awake runs before any Start, so spawner targets are
        // configured before BoidSimulationGPU.Start initializes the GPU buffers.
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

        /// <summary>Adds one boid school to this species and rebuilds GPU buffers. Called by UI.</summary>
        public void AddSpecies(SpeciesDataGPU species)
        {
            if (!ValidateSpecies(species, out BoidSpawnerGPUMultiTargets spawner)) return;
            if (_carryingCapacity.TryGetValue(species, out int cap) && spawner.SpawnData.BoidsCount >= cap) return;
            AddGroup(species, spawner);
            _simulation.ReinitializeBuffers();
        }

        /// <summary>Removes one boid school from this species and rebuilds GPU buffers. Called by UI.</summary>
        public void RemoveSpecies(SpeciesDataGPU species)
        {
            if (!ValidateSpecies(species, out BoidSpawnerGPUMultiTargets spawner)) return;
            _boidsPerGroup.TryGetValue(species, out int bpg);
            if (spawner.SpawnData.BoidsCount <= bpg)
            {
                Debug.LogWarning($"[EcosystemSimulationGPU] Cannot remove last school of '{species.SpeciesName}'.");
                return;
            }
            RemoveGroup(species, spawner);
            _simulation.ReinitializeBuffers();
        }

        /// <summary>
        /// Returns the current boid group (sub-group / school) count for this species.
        /// Each group represents a school of fish. One unit = one school.
        /// </summary>
        public int CountGroups(SpeciesDataGPU species)
        {
            BoidSpawnerGPUMultiTargets spawner = FindSpawner(species);
            if (spawner == null) return 0;
            _boidsPerGroup.TryGetValue(species, out int bpg);
            return bpg > 0 ? spawner.SpawnData.BoidsCount / bpg : 0;
        }

        // -------------------------------------------------------------------------
        // Population tick
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
                BoidSpawnerGPUMultiTargets spawner = FindSpawner(species);
                if (spawner == null) continue;

                int current = spawner.SpawnData.BoidsCount;
                _boidsPerGroup.TryGetValue(species, out int bpg);

                // Natural births (respawn) and natural deaths removed.
                // Population now only changes via the starvation cascade below
                // and the UI add/remove API.

                // Starvation — fires if any prey species is below its starvation threshold (ratio-based).
                bool starved = false;
                foreach (SpeciesDataGPU prey in species.PreySpecies)
                {
                    if (prey == null) continue;
                    BoidSpawnerGPUMultiTargets preySpawner = FindSpawner(prey);
                    if (preySpawner == null) continue;
                    _carryingCapacity.TryGetValue(prey, out int preyCap);
                    float preyRatio = (float)preySpawner.SpawnData.BoidsCount / Mathf.Max(1, preyCap);
                    if (preyRatio < species.StarvationThreshold)
                    {
                        if (current > bpg && Random.value < species.StarvationDeathRate) starved = true;
                        break;
                    }
                }

                if (starved && current > bpg) { RemoveGroup(species, spawner); anyChanged = true; }
            }

            if (anyChanged) _simulation.ReinitializeBuffers();
        }

        // -------------------------------------------------------------------------
        // Internal group mutation — no GPU rebuild, caller is responsible for that.
        // -------------------------------------------------------------------------

        private void AddGroup(SpeciesDataGPU species, BoidSpawnerGPUMultiTargets spawner)
        {
            if (_boidsPerGroup.TryGetValue(species, out int bpg))
                spawner.SetBoidsCount(spawner.SpawnData.BoidsCount + bpg);
        }

        private void RemoveGroup(SpeciesDataGPU species, BoidSpawnerGPUMultiTargets spawner)
        {
            if (_boidsPerGroup.TryGetValue(species, out int bpg))
                spawner.SetBoidsCount(Mathf.Max(bpg, spawner.SpawnData.BoidsCount - bpg));
        }

        // -------------------------------------------------------------------------
        // Setup helpers
        // -------------------------------------------------------------------------

        private void SetupAllSpecies()
        {
            Bounds bounds = _ecosystem.SimulationBounds;

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

                int initialGroups = spawner.InitialGroupsCount;
                _boidsPerGroup[species]    = Mathf.Max(1, spawner.SpawnData.BoidsCount / initialGroups);
                _carryingCapacity[species] = spawner.SpawnData.BoidsCount;

                if (species.Role == SpeciesRoleGPU.Apex)
                    SetupApexTargets(species, spawner, bounds);
                // Mesopredator, Prey, and Neutral keep their existing Inspector targets untouched.
            }
        }

        /// <summary>
        /// For Apex species: if the spawner already has targets (e.g. from BoidSimulationTargetAnimatorsSpawner),
        /// leave them in place. Only create WanderingAffecterGPU instances when the spawner has no targets at all.
        /// </summary>
        private void SetupApexTargets(SpeciesDataGPU species, BoidSpawnerGPUMultiTargets spawner, Bounds bounds)
        {
            SimulationAffecterComponent[] existingTargets = spawner.SpawnData.Targets;
            int groupCount = spawner.InitialGroupsCount;

            if (existingTargets != null && existingTargets.Length >= groupCount)
            {
                // Targets are already configured (BoidSimulationTargetAnimatorsSpawner or Inspector).
                // Leave them in place — no wanderers needed for the initial groups.
                Debug.Log($"[EcosystemSimulationGPU] '{species.SpeciesName}': {existingTargets.Length} existing targets found, keeping them.");
                return;
            }

            // No targets assigned — create a WanderingAffecterGPU for each group.
            spawner.ClearTargets();
            List<WanderingAffecterGPU> wanderers = new List<WanderingAffecterGPU>(groupCount);
            for (int g = 0; g < groupCount; g++)
            {
                WanderingAffecterGPU wanderer = CreateAndInitWanderer(g);
                wanderer.Initialize(bounds);
                spawner.AddTarget(wanderer);
                wanderers.Add(wanderer);
            }
            _managedWanderers[species] = wanderers;

            Debug.Log($"[EcosystemSimulationGPU] '{species.SpeciesName}': created {groupCount} WanderingAffecterGPU targets.");
        }

        // Creates a WanderingAffecterGPU on a new child GameObject and configures its sub-group ID.
        private WanderingAffecterGPU CreateAndInitWanderer(int subGroupIndex)
        {
            GameObject go = new GameObject($"WanderingAffecter_SubGroup{subGroupIndex}");
            go.transform.SetParent(transform);

            WanderingAffecterGPU wanderer = go.AddComponent<WanderingAffecterGPU>();
            wanderer.SetSubGroupID(subGroupIndex);
            wanderer.SetAffecterType(SimulationAffecterType.Target);
            wanderer.Initialize(_ecosystem.SimulationBounds);
            return wanderer;
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
