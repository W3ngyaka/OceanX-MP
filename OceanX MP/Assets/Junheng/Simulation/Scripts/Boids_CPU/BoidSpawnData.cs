using System;
using UnityEngine;

namespace OceanX.BoidsCPU
{
    /// <summary>
    /// Structure containing data required for simulating one fish school. It contains information
    /// about the fish species, obstacles and targets that will be affected in the simulation.
    /// </summary>
    [Serializable]
    public struct BoidSpawnData 
    {
        [Header("Spawn Info: ")]
        [Tooltip("How many boids will be spawned inside the simulation.")]
        [Range(10, 1000000)] public int BoidsCount;
        [Tooltip("Minimal distance around a newly spawned boid where there are no other boids.")]
        [Range(0, 10)] public float MinSpawnDistanceBetweenBoids;

        [Header("Fish Info: ")]
        [Tooltip("Mesh asset that will be assigned to each newly spawned boid to visually represent it.")]
        public Mesh BoidMesh;
        [Tooltip("Material that will be assigned to each newly spawned boid.")]
        public Material BoidMaterial;
        [Tooltip("Reference to the scriptable object containing movement properties of all boids in" +
            " relation to each other. How much distance should they keep between them, how strongly do they " +
            "want to follow the target, etc.")]
        public FishSchoolProperties FishSchoolProperties;

        [Header("Simulation Affecters: ")]
        [Tooltip("Collection of targets that boids will try to follow.")]
        public SimulationAffecterComponent[] Targets;
        [Tooltip("Obstacles that the boids should avoid during the simulation. Whenever they get close to " +
            "the obstacle, they will accelerate and try to get away as soon as possible.")]
        public SimulationAffecterComponent[] Obstacles;
    }
}