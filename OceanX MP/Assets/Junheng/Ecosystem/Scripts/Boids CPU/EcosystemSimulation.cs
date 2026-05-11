using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Manages the full multi-species ecosystem simulation.
// Attach to a scene GameObject and assign an EcosystemDefinition asset.
// All species are spawned at Start, share a single spatial partition,
// and interact with each other via predator-prey relationships.
public class EcosystemSimulation : MonoBehaviour
{
    [Header("Ecosystem")]
    [Tooltip("The ScriptableObject that defines which species exist and their relationships.")]
    public EcosystemDefinition Ecosystem;

    [Header("Spatial Partition")]
    [Tooltip("Cell size for the spatial partition grid. Should cover at least the largest vision/detection range.")]
    public float CellSize = 15f;
    [Range(1, 3)]
    [Tooltip("How many cells outward to search for neighbours. 1 = immediate neighbours only.")]
    public int NeighbourSearchRange = 1;

    [Header("Population Tick")]
    [Tooltip("Seconds between each population growth/death calculation.")]
    public float PopulationTickInterval   = 5f;
    [Tooltip("Uncheck to freeze populations at their spawned counts (useful for debugging movement).")]
    public bool  EnablePopulationDynamics = true;

    [Header("Global Affecters (optional)")]
    [Tooltip("Targets and obstacles that affect all species equally.")]
    public BoidAffecter[] GlobalAffecters;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private List<Boid>               _allBoids = new List<Boid>();
    private SpatialPartition3D<Boid> _grid;
    private Bounds                   _bounds;

    // Per-frame scratch lists — reused to avoid per-frame allocations.
    private readonly List<Boid> _nearbyBuffer   = new List<Boid>();
    private readonly List<int>  _deadIndices    = new List<int>();
    private readonly List<Boid> _killCandidates = new List<Boid>();

    // Parent transforms for each species hierarchy, populated during spawn.
    private readonly Dictionary<int, Transform> _speciesParents = new Dictionary<int, Transform>();

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        if (Ecosystem == null)
        {
            Debug.LogError("[EcosystemSimulation] No EcosystemDefinition assigned.", this);
            enabled = false;
            return;
        }

        _bounds = new Bounds(Ecosystem.SimulationCenter, Ecosystem.SimulationSize);

        AssignRuntimeIds();
        SpawnAllSpecies();
        BuildSpatialPartition();

        if (EnablePopulationDynamics)
            StartCoroutine(PopulationTick());
    }

    private void Update()
    {
        if (_allBoids.Count == 0) return;

        float dt = Time.deltaTime;

        // Update each boid's cell in the grid before querying neighbours.
        for (int i = 0; i < _allBoids.Count; i++)
            _grid.UpdateObjectCell(_allBoids[i].Position, _allBoids[i]);

        // Run the simulation for every living boid.
        for (int i = 0; i < _allBoids.Count; i++)
        {
            Boid boid = _allBoids[i];
            if (!boid.IsAlive) continue;

            // GetNearby allocates a new List each call — replace with buffer version.
            List<Boid> nearby = _grid.GetNearby(boid.Position);
            boid.UpdateBoid(nearby, dt, _bounds, GlobalAffecters);
        }

        // Remove any boids that were killed this frame.
        CleanUpDeadBoids();
    }

    // -------------------------------------------------------------------------
    // Setup helpers
    // -------------------------------------------------------------------------

    // Assign a unique integer ID to every species before spawning,
    // so predator-prey ID caches built inside Boid.InitializeAsSpecies() are valid.
    private void AssignRuntimeIds()
    {
        for (int i = 0; i < Ecosystem.Species.Count; i++)
        {
            if (Ecosystem.Species[i] != null)
                Ecosystem.Species[i].RuntimeId = i;
        }
    }

    private void SpawnAllSpecies()
    {
        for (int s = 0; s < Ecosystem.Species.Count; s++)
        {
            SpeciesDefinition species = Ecosystem.Species[s];
            if (species == null)
            {
                Debug.LogWarning($"[EcosystemSimulation] Species slot {s} is null — skipping.");
                continue;
            }

            if (species.Prefab == null)
            {
                Debug.LogError($"[EcosystemSimulation] {species.SpeciesName} has no prefab assigned — skipping.");
                continue;
            }

            if (species.SchoolProperties == null)
            {
                Debug.LogError($"[EcosystemSimulation] {species.SpeciesName} has no SchoolProperties — skipping.");
                continue;
            }

            SpawnSpecies(species);
        }

        Debug.Log($"[EcosystemSimulation] Spawned {_allBoids.Count} total boids across {Ecosystem.Species.Count} species.");
    }

    private void SpawnSpecies(SpeciesDefinition species)
    {
        Transform parent = new GameObject($"[{species.SpeciesName}]").transform;
        parent.SetParent(transform);
        _speciesParents[species.RuntimeId] = parent;

        for (int i = 0; i < species.DefaultPopulation; i++)
        {
            Vector3    spawnPos = Ecosystem.SimulationCenter + Random.insideUnitSphere * species.SpawnRadius;
            Quaternion spawnRot = Random.rotation;
            GameObject go       = Instantiate(species.Prefab, spawnPos, spawnRot, parent);
            go.name = $"{species.SpeciesName}_{i}";

            Boid boid = go.GetComponent<Boid>();
            if (boid == null)
            {
                Debug.LogError($"[EcosystemSimulation] Prefab for {species.SpeciesName} is missing a Boid component.");
                Destroy(go);
                continue;
            }

            boid.InitializeAsSpecies(species);
            _allBoids.Add(boid);
        }
    }

    private void BuildSpatialPartition()
    {
        _grid = new SpatialPartition3D<Boid>(CellSize, NeighbourSearchRange);
        foreach (Boid b in _allBoids)
            _grid.Add(b.Position, b);
    }

    // -------------------------------------------------------------------------
    // Cleanup
    // -------------------------------------------------------------------------

    private void CleanUpDeadBoids()
    {
        _deadIndices.Clear();

        for (int i = 0; i < _allBoids.Count; i++)
        {
            Boid b = _allBoids[i];
            if (b == null || !b.IsAlive)
                _deadIndices.Add(i);
        }

        // Remove in reverse order so indices stay valid.
        for (int i = _deadIndices.Count - 1; i >= 0; i--)
        {
            int  idx  = _deadIndices[i];
            Boid dead = _allBoids[idx];
            _allBoids.RemoveAt(idx);

            if (dead != null)
            {
                _grid.Remove(dead);
                Destroy(dead.gameObject);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Population dynamics
    // -------------------------------------------------------------------------

    private IEnumerator PopulationTick()
    {
        while (true)
        {
            yield return new WaitForSeconds(PopulationTickInterval);

            foreach (SpeciesDefinition species in Ecosystem.Species)
            {
                if (species == null) continue;

                int currentPop = CountLiving(species);

                // Logistic growth — slows as population approaches carrying capacity.
                int births = currentPop > 0
                    ? Mathf.RoundToInt(currentPop * species.ReproductionRate
                      * Mathf.Max(0f, 1f - (float)currentPop / species.CarryingCapacity))
                    : 0;

                // Baseline mortality independent of predation.
                int naturalDeaths = Mathf.RoundToInt(currentPop * species.NaturalDeathRate);

                // Starvation — triggers when any prey species drops below the threshold ratio.
                int starvationDeaths = 0;
                if (species.PreySpecies != null)
                {
                    foreach (SpeciesDefinition prey in species.PreySpecies)
                    {
                        if (prey == null || prey.CarryingCapacity <= 0) continue;
                        float preyRatio = (float)CountLiving(prey) / prey.CarryingCapacity;
                        if (preyRatio < species.StarvationThreshold)
                        {
                            starvationDeaths = Mathf.RoundToInt(currentPop * species.StarvationDeathRate);
                            break;
                        }
                    }
                }

                int toSpawn  = births;
                int toRemove = Mathf.Min(currentPop, naturalDeaths + starvationDeaths);

                if (toSpawn  > 0) SpawnAdditional(species, toSpawn);
                if (toRemove > 0) KillRandom(species, toRemove);
            }
        }
    }

    private int CountLiving(SpeciesDefinition species)
    {
        int count = 0;
        for (int i = 0; i < _allBoids.Count; i++)
        {
            Boid b = _allBoids[i];
            if (b != null && b.IsAlive && b.GroupId == species.RuntimeId)
                count++;
        }
        return count;
    }

    private void SpawnAdditional(SpeciesDefinition species, int count)
    {
        if (species.Prefab == null || species.SchoolProperties == null) return;

        if (!_speciesParents.TryGetValue(species.RuntimeId, out Transform parent))
        {
            parent = new GameObject($"[{species.SpeciesName}]").transform;
            parent.SetParent(transform);
            _speciesParents[species.RuntimeId] = parent;
        }

        int room        = species.CarryingCapacity - CountLiving(species);
        int actualSpawn = Mathf.Min(count, Mathf.Max(0, room));

        for (int i = 0; i < actualSpawn; i++)
        {
            Vector3    spawnPos = Ecosystem.SimulationCenter + Random.insideUnitSphere * species.SpawnRadius;
            Quaternion spawnRot = Random.rotation;
            GameObject go       = Instantiate(species.Prefab, spawnPos, spawnRot, parent);
            go.name = $"{species.SpeciesName}_{parent.childCount}";

            Boid boid = go.GetComponent<Boid>();
            if (boid == null) { Destroy(go); continue; }

            boid.InitializeAsSpecies(species);
            _allBoids.Add(boid);
            _grid.Add(boid.Position, boid);
        }
    }

    private void KillRandom(SpeciesDefinition species, int count)
    {
        _killCandidates.Clear();
        for (int i = 0; i < _allBoids.Count; i++)
        {
            Boid b = _allBoids[i];
            if (b != null && b.IsAlive && b.GroupId == species.RuntimeId)
                _killCandidates.Add(b);
        }

        int killCount = Mathf.Min(count, _killCandidates.Count);
        for (int i = 0; i < killCount; i++)
        {
            int j = Random.Range(i, _killCandidates.Count);
            (_killCandidates[i], _killCandidates[j]) = (_killCandidates[j], _killCandidates[i]);
            _killCandidates[i].TryKill();
        }
    }

    // -------------------------------------------------------------------------
    // Editor visualisation
    // -------------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        if (Ecosystem == null) return;

        // Simulation bounds
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.12f);
        Gizmos.DrawCube(Ecosystem.SimulationCenter, Ecosystem.SimulationSize);
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.5f);
        Gizmos.DrawWireCube(Ecosystem.SimulationCenter, Ecosystem.SimulationSize);

        // Spatial partition grid (runtime only)
        if (_grid == null) return;
        Gizmos.color = new Color(1f, 1f, 0f, 0.06f);
        foreach (Vector3Int cell in _grid.Grid.Keys)
        {
            Vector3 center = new Vector3(
                (cell.x + 0.5f) * CellSize,
                (cell.y + 0.5f) * CellSize,
                (cell.z + 0.5f) * CellSize);
            Gizmos.DrawCube(center, Vector3.one * CellSize);
        }
    }
}
