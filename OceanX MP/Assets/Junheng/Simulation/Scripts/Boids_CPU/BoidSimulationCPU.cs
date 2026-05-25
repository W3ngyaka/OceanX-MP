using System.Collections.Generic;
using UnityEngine;

namespace OceanX.BoidsCPU
{
    /// <summary>
    /// Script that controls the simulation of the boids on the CPU. It starts by 
    /// providing a simulation area bounds to each boid spawner and retrieving spawned
    /// boid instances from them. Note that each boid group gets an ID that allows them to 
    /// differentiate themselves from other boid groups. Also, larger ID means that this fish species
    /// is larger and that the boid with the smaller ID should avoid the larger fish.
    /// Then, every update, it triggers one simulation tick that updates the position 
    /// of each boid in the simulation.
    /// </summary>
    [ExecuteAlways]
    public class BoidSimulationCPU : BoidSimulationBase
    {
        [Header("Spatial Partition Settings: ")]
        [SerializeField] private bool _useSpatialPartition = true;
        [SerializeField] private float _spatialPartitionGridCellSize = 10f;
        [SerializeField, Range(1, 4)] private int _neighborCellSearchCount = 1;

        [Header("References: ")]
        [SerializeField] private BoidSpawner[] _boidSpawners = null;

        private List<Boid> _boids = new List<Boid>();
        private SpatialPartition3D<Boid> _spatialPartitionGrid = null;

        /// <inheritdoc/>
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            if (_spatialPartitionGrid == null)
            {
                return;
            }

            // Visualize spatial partition areas.
            Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 0.1f);
            Dictionary<Vector3Int, List<Boid>> grid = _spatialPartitionGrid.Grid;
            foreach (Vector3Int cell in grid.Keys)
            {
                Vector3 cellCenter = new Vector3(cell.x * _spatialPartitionGridCellSize, cell.y * _spatialPartitionGridCellSize, cell.z * _spatialPartitionGridCellSize) + Vector3.one * 0.5f * _spatialPartitionGridCellSize;
                Gizmos.DrawCube(cellCenter, _spatialPartitionGridCellSize * Vector3.one);
            }
        }

        /// <inheritdoc/>
        public override BoidSpawnerBase[] GetBoidSpawners()
        {
            return _boidSpawners;
        }

        /// <inheritdoc/>
        protected override void SpawnBoids()
        {
            // Spawn all boids and cache their reference.
            foreach (BoidSpawner boidSpawner in _boidSpawners)
            {
                boidSpawner.SpawnBoids(_simulationAreaBounds);
                List<Boid> spawnedBoids = boidSpawner.Boids;
                _boids.AddRange(spawnedBoids);
            }
        }

        /// <inheritdoc/>
        protected override void InitializeBoidsSimulation()
        {
            base.InitializeBoidsSimulation();

            if (!_useSpatialPartition)
            {
                return;
            }

            // Initialize a spatial partition grid and add all objects to it.
            _spatialPartitionGrid = new SpatialPartition3D<Boid>(_spatialPartitionGridCellSize, _neighborCellSearchCount);
            foreach (Boid boid in _boids)
            {
                _spatialPartitionGrid.Add(boid.Position, boid);
            }
        }

        /// <inheritdoc/>
        protected override void UpdateSimulation(float timeDelta)
        {
            if (_useSpatialPartition)
            {
                // For each boid, update its grid cell index if necessary.
                foreach (Boid boid in _boids)
                {
                    _spatialPartitionGrid.UpdateObjectCell(boid.Position, boid);
                }
            }

            // Update the position, rotation and movement properties of each boid in the simulation.
            int boidsCount = _boids.Count;
            for (int i = 0; i < boidsCount; i++)
            {
                int boidIndex = i;
                Boid boid = _boids[boidIndex];

                List<Boid> nearbyBoids = _useSpatialPartition ? _spatialPartitionGrid.GetNearby(boid.Position) : _boids;
                int boidIndexInNeighbors = nearbyBoids.IndexOf(boid);
                List<SimulationAffecter> globalAffecters = new List<SimulationAffecter>();
                foreach (SimulationAffecterComponent globalAffecterComponent in _globalAffecters)
                {
                    globalAffecters.Add(globalAffecterComponent.Affecter);
                }
                boid.UpdateBoid(nearbyBoids, boidIndexInNeighbors, timeDelta, _simulationAreaBounds, globalAffecters);
            }
        }
    }
}
