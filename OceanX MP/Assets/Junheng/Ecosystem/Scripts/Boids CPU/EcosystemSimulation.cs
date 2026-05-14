using System.Collections.Generic;
using UnityEngine;

// Manages the full multi-species ecosystem simulation.
// Attach to a scene GameObject and assign an EcosystemDefinition asset.
//
// Runtime API (call from UI or other scripts):
//   AddSpecies(species, count)    — spawns fish from outside the boundary swimming in
//   RemoveSpecies(species, count) — selected fish swim out and disappear
public class EcosystemSimulation : MonoBehaviour
{
    [Header("Ecosystem")]
    [Tooltip("The ScriptableObject that defines which species exist and their relationships.")]
    public EcosystemDefinition Ecosystem;

    [Header("Spatial Partition")]
    [Tooltip("Cell size for the spatial partition grid.")]
    public float CellSize = 15f;
    [Range(1, 3)]
    public int NeighbourSearchRange = 1;
    [Header("Spawn Entry")]
    [Tooltip("How far outside the boundary fish spawn before swimming in.")]
    public float SpawnOffsetDistance = 8f;

    [Header("Global Affecters (optional)")]
    public BoidAffecter[] GlobalAffecters;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private List<Boid>               _allBoids       = new List<Boid>();
    private SpatialPartition3D<Boid> _grid;
    private Bounds                   _bounds;

    private readonly List<Boid> _deadIndices    = new List<Boid>();
    private readonly List<Boid> _killCandidates = new List<Boid>();

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
        BuildSpatialPartition();  // must exist before AddSpecies tries to add to it
        SpawnAllSpecies();
    }

    private void Update()
    {
        if (_allBoids.Count == 0) return;

        float dt = Time.deltaTime;

        for (int i = 0; i < _allBoids.Count; i++)
            _grid.UpdateObjectCell(_allBoids[i].Position, _allBoids[i]);

        for (int i = 0; i < _allBoids.Count; i++)
        {
            Boid boid = _allBoids[i];
            if (!boid.IsAlive) continue;

            List<Boid> nearby = _grid.GetNearby(boid.Position);
            boid.UpdateBoid(nearby, dt, _bounds, GlobalAffecters);
        }

        CleanUpDeadBoids();
    }

    // -------------------------------------------------------------------------
    // Public API — call from UI buttons
    // -------------------------------------------------------------------------

    /// <summary>
    /// Spawns <paramref name="count"/> fish of <paramref name="species"/> just outside
    /// the simulation boundary. They swim inward and join normal behaviour once inside.
    /// </summary>
    public void AddSpecies(SpeciesDefinition species, int count = 1)
    {
        if (species == null || species.Prefab == null || species.SchoolProperties == null)
        {
            Debug.LogWarning("[EcosystemSimulation] AddSpecies: invalid species data.");
            return;
        }

        // Make sure the species has a runtime ID assigned.
        if (species.RuntimeId < 0)
            species.RuntimeId = Ecosystem.Species.IndexOf(species);

        if (!_speciesParents.TryGetValue(species.RuntimeId, out Transform parent))
        {
            parent = new GameObject($"[{species.SpeciesName}]").transform;
            parent.SetParent(transform);
            _speciesParents[species.RuntimeId] = parent;
        }

        for (int i = 0; i < count; i++)
        {
            // Pick a random point on the boundary surface, then offset outward.
            Vector3 spawnPos = GetRandomBoundaryPoint(out Vector3 inwardDir);

            Quaternion spawnRot = Quaternion.LookRotation(inwardDir);
            GameObject go       = Instantiate(species.Prefab, spawnPos, spawnRot, parent);
            go.name = $"{species.SpeciesName}_{parent.childCount}";

            Boid boid = go.GetComponent<Boid>();
            if (boid == null) { Destroy(go); continue; }

            boid.InitializeAsSpecies(species);

            // Target: random point inside the simulation area.
            Vector3 target = _bounds.center + Random.insideUnitSphere * (_bounds.extents.magnitude * 0.5f);
            boid.SetEntering(target);

            _allBoids.Add(boid);
            _grid.Add(boid.Position, boid);
        }
    }

    /// <summary>
    /// Tells <paramref name="count"/> random fish of <paramref name="species"/> to
    /// swim out of the simulation boundary and disappear.
    /// </summary>
    public void RemoveSpecies(SpeciesDefinition species, int count = 1)
    {
        if (species == null) return;

        _killCandidates.Clear();
        for (int i = 0; i < _allBoids.Count; i++)
        {
            Boid b = _allBoids[i];
            if (b != null && b.IsAlive && !b.IsExiting && b.GroupId == species.RuntimeId)
                _killCandidates.Add(b);
        }

        int toRemove = Mathf.Min(count, _killCandidates.Count);
        for (int i = 0; i < toRemove; i++)
        {
            // Fisher-Yates shuffle pick
            int j = Random.Range(i, _killCandidates.Count);
            (_killCandidates[i], _killCandidates[j]) = (_killCandidates[j], _killCandidates[i]);
            _killCandidates[i].BeginExit(_bounds);
        }
    }

    /// <summary>Returns how many living fish of a species are currently in the simulation.</summary>
    public int CountLiving(SpeciesDefinition species)
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

    // -------------------------------------------------------------------------
    // Setup helpers
    // -------------------------------------------------------------------------

    private void AssignRuntimeIds()
    {
        for (int i = 0; i < Ecosystem.Species.Count; i++)
            if (Ecosystem.Species[i] != null)
                Ecosystem.Species[i].RuntimeId = i;
    }

    private void SpawnAllSpecies()
    {
        for (int s = 0; s < Ecosystem.Species.Count; s++)
        {
            SpeciesDefinition species = Ecosystem.Species[s];
            if (species == null) continue;
            if (species.DefaultPopulation <= 0) continue;
            AddSpecies(species, species.DefaultPopulation);
        }

        Debug.Log($"[EcosystemSimulation] Entering: {_allBoids.Count} boids across {Ecosystem.Species.Count} species.");
    }

    private void BuildSpatialPartition()
    {
        _grid = new SpatialPartition3D<Boid>(CellSize, NeighbourSearchRange);
        // Boids are added to the grid individually by AddSpecies as they spawn.
    }

    // Returns a point just outside a random face of the simulation bounds.
    // outInwardDir is the direction pointing into the simulation from that point.
    private Vector3 GetRandomBoundaryPoint(out Vector3 outInwardDir)
    {
        int face = Random.Range(0, 6);
        Vector3 point  = _bounds.center;
        Vector3 normal = Vector3.zero;

        switch (face)
        {
            case 0: point.x = _bounds.max.x; normal = Vector3.right;   break;
            case 1: point.x = _bounds.min.x; normal = Vector3.left;    break;
            case 2: point.y = _bounds.max.y; normal = Vector3.up;      break;
            case 3: point.y = _bounds.min.y; normal = Vector3.down;    break;
            case 4: point.z = _bounds.max.z; normal = Vector3.forward; break;
            default: point.z = _bounds.min.z; normal = Vector3.back;   break;
        }

        // Randomise position along the face.
        switch (face)
        {
            case 0: case 1:
                point.y = Random.Range(_bounds.min.y, _bounds.max.y);
                point.z = Random.Range(_bounds.min.z, _bounds.max.z);
                break;
            case 2: case 3:
                point.x = Random.Range(_bounds.min.x, _bounds.max.x);
                point.z = Random.Range(_bounds.min.z, _bounds.max.z);
                break;
            case 4: case 5:
                point.x = Random.Range(_bounds.min.x, _bounds.max.x);
                point.y = Random.Range(_bounds.min.y, _bounds.max.y);
                break;
        }

        // Push outside the boundary.
        point       += normal * SpawnOffsetDistance;
        outInwardDir = -normal;
        return point;
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
                _deadIndices.Add(b);
        }

        foreach (Boid dead in _deadIndices)
        {
            _allBoids.Remove(dead);
            if (dead != null)
            {
                _grid.Remove(dead);
                Destroy(dead.gameObject);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Editor visualisation
    // -------------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        if (Ecosystem == null) return;

        Gizmos.color = new Color(0f, 0.8f, 1f, 0.12f);
        Gizmos.DrawCube(Ecosystem.SimulationCenter, Ecosystem.SimulationSize);
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.5f);
        Gizmos.DrawWireCube(Ecosystem.SimulationCenter, Ecosystem.SimulationSize);
    }
}
