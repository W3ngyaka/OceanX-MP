using System.Collections.Generic;
using UnityEngine;
using static OceanX.BoidSpawnUtility;

namespace OceanX.BoidsCPU
{
    /// <summary>
    /// Component that spawns boids inside the simulation area at random positions.
    /// </summary>
    public class BoidSpawner : BoidSpawnerBase
    {
        protected List<Boid> _boids = new List<Boid>();

        /// <summary>
        /// Reference to the collection of all <see cref="Boid"/> instances that were
        /// spawned by this boid spawner.
        /// </summary>
        public List<Boid> Boids { get => _boids; }
       
        /// <inheritdoc/>
        public override void SpawnBoids(Bounds simulationAreaBounds)
        {
            Mesh boidMesh = _boidSpawnData.BoidMesh;
            Material boidMaterial = _boidSpawnData.BoidMaterial;
            FishSchoolProperties schoolProperties = _boidSpawnData.FishSchoolProperties;

            int totalBoidsCount = _boidSpawnData.BoidsCount;
            int currentGroupId = 0;
            GroupOfBoidsSpawnData[] boidGroupsSpawnData = CalculateBoidsSpawnData(totalBoidsCount, simulationAreaBounds, _boidSpawnData.MinSpawnDistanceBetweenBoids, _initialGroupsCount);
            foreach (GroupOfBoidsSpawnData groupOfBoidsSpawnData in boidGroupsSpawnData)
            {
                Quaternion spawnRotation = groupOfBoidsSpawnData.RotationOfTheGroup;
                int boidsCountInThisGroup = groupOfBoidsSpawnData.SpawnPositions.Length;
                for (int i = 0; i < boidsCountInThisGroup; i++)
                {
                    Boid boid = SpawnNewBoid(groupOfBoidsSpawnData.SpawnPositions[i], spawnRotation, boidMesh, boidMaterial, _boids.Count);
                    boid.BoidSubGroupId = currentGroupId;
                    _boids.Add(boid);
                }
                currentGroupId++;
            }

            // After all boids have been spawned and cached, initialize them to start the simulation.
            foreach (Boid boid in _boids)
            {
                boid.Initialize(this, schoolProperties);
            }
        }

        /// <inheritdoc/>
        protected override void UpdateBoidGroupId(int boidGroupId)
        {
            foreach (Boid boid in _boids)
            {
                boid.SetId(boidGroupId);
            }
        }

        protected virtual Boid SpawnNewBoid(Vector3 position, Quaternion rotation, Mesh boidMesh, Material boidMaterial, int boidIndex)
        {
            // Spawn new boid and position it inside the simulation area at a random position.
            GameObject boidGameObject = new GameObject($"Boid_{boidIndex}");
            boidGameObject.transform.SetParent(transform);
            boidGameObject.transform.position = position;
            boidGameObject.transform.rotation = rotation;

            // Add mesh filter and assign the boid mesh.
            MeshFilter meshFilter = boidGameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = boidMesh;

            // Add mesh renderer and instantiate new material instance for this boid.
            MeshRenderer renderer = boidGameObject.AddComponent<MeshRenderer>();
            renderer.material = boidMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Assign boid component to it and cache the reference.
            Boid boid = boidGameObject.AddComponent<Boid>();
            return boid;
        }
    }
}