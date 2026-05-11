using UnityEngine;
using static OceanX.BoidSpawnUtility;

namespace OceanX.BoidsCPU
{
    /// <summary>
    /// Component that spawns boids inside the simulation area at random positions.
    /// </summary>
    public class BoidSpawnerRandom : BoidSpawner
    {
        /// <inheritdoc/>
        public override void SpawnBoids(Bounds simulationAreaBounds)
        {
            Mesh boidMesh = _boidSpawnData.BoidMesh;
            Material boidMaterial = _boidSpawnData.BoidMaterial;
            FishSchoolProperties schoolProperties = _boidSpawnData.FishSchoolProperties;

            int totalBoidsCount = _boidSpawnData.BoidsCount;
            int currentGroupId = 0;

            (Vector3[] spawnPositions, Quaternion[] spawnRotations) = CalculateRandomBoidsSpawnData(totalBoidsCount, simulationAreaBounds);
            for (int i = 0; i < totalBoidsCount; i++)
            {
                Boid boid = SpawnNewBoid(spawnPositions[i], spawnRotations[i], boidMesh, boidMaterial, _boids.Count);
                boid.BoidSubGroupId = currentGroupId;
                _boids.Add(boid);
            }

            // After all boids have been spawned and cached, initialize them to start the simulation.
            foreach (Boid boid in _boids)
            {
                boid.Initialize(this, schoolProperties);
            }
        }
    }
}