using System.Collections.Generic;
using UnityEngine;

// Central manager for a CPU boid simulation.
// Attach to a GameObject; assign a fish prefab that has the Boid component.
// Create BoidSchoolProperties and BoidMovementProperties assets via the
// Assets > Create > OceanX menus and link them here.
public class BoidSimulation : MonoBehaviour
{
    [Header("Spawning")]
    public GameObject          fishPrefab;
    public int                 fishCount    = 30;
    public float               spawnRadius  = 5f;
    public int                 boidGroupId  = 0;
    public BoidSchoolProperties schoolProperties;

    [Header("Simulation Area")]
    public Vector3 simulationCenter = Vector3.zero;
    public Vector3 simulationSize   = new Vector3(50f, 20f, 50f);

    [Header("Spatial Partition")]
    public bool  useSpatialPartition    = true;
    public float cellSize               = 10f;
    [Range(1, 4)] public int neighborCellSearchCount = 1;

    [Header("Affecters (targets & obstacles)")]
    public BoidAffecter[] affecters;

    private List<Boid>              _boids = new List<Boid>();
    private SpatialPartition3D<Boid> _grid;
    private Bounds                   _bounds;

    private void Start()
    {
        _bounds = new Bounds(simulationCenter, simulationSize);
        SpawnBoids();

        if (useSpatialPartition)
        {
            _grid = new SpatialPartition3D<Boid>(cellSize, neighborCellSearchCount);
            foreach (Boid b in _boids)
                _grid.Add(b.Position, b);
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (useSpatialPartition)
        {
            foreach (Boid b in _boids)
                _grid.UpdateObjectCell(b.Position, b);
        }

        int count = _boids.Count;
        for (int i = 0; i < count; i++)
        {
            Boid       boid        = _boids[i];
            List<Boid> nearbyBoids = useSpatialPartition
                ? _grid.GetNearby(boid.Position)
                : _boids;

            boid.UpdateBoid(nearbyBoids, dt, _bounds, affecters);
        }
    }

    private void SpawnBoids()
    {
        if (fishPrefab == null || schoolProperties == null)
        {
            Debug.LogError("BoidSimulation: fishPrefab or schoolProperties is not assigned.");
            return;
        }

        for (int i = 0; i < fishCount; i++)
        {
            Vector3    spawnPos = transform.position + Random.insideUnitSphere * spawnRadius;
            Quaternion spawnRot = Random.rotation;
            GameObject go       = Instantiate(fishPrefab, spawnPos, spawnRot, transform);

            Boid boid = go.GetComponent<Boid>();
            if (boid == null)
            {
                Debug.LogError("BoidSimulation: fishPrefab does not have a Boid component.");
                Destroy(go);
                continue;
            }

            boid.Initialize(schoolProperties, boidGroupId);
            _boids.Add(boid);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.6f, 1f, 0.15f);
        Gizmos.DrawCube(simulationCenter, simulationSize);

        Gizmos.color = new Color(0f, 0.6f, 1f, 0.5f);
        Gizmos.DrawWireCube(simulationCenter, simulationSize);

        if (_grid == null) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.08f);
        foreach (Vector3Int cell in _grid.Grid.Keys)
        {
            Vector3 center = new Vector3(
                (cell.x + 0.5f) * cellSize,
                (cell.y + 0.5f) * cellSize,
                (cell.z + 0.5f) * cellSize);
            Gizmos.DrawCube(center, Vector3.one * cellSize);
        }
    }
}
