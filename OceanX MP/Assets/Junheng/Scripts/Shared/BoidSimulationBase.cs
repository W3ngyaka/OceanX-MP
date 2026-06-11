using OceanX.BoidsCPU;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace OceanX
{
    /// <summary>
    /// Class implementing shared behaviour for all versions of boid simulations (both CPU and GPU).
    /// </summary>
    [ExecuteAlways]
    public abstract class BoidSimulationBase : MonoBehaviour
    {
        [Serializable]
        protected struct ComparableBounds
        {
            public int OriginalBoundsID;
            public Bounds Bounds;
        }

        [Header("Simulation Settings: ")]
        [Tooltip("Area inside which the boid simulation will occur.")]
        [SerializeField] protected Bounds _simulationAreaBounds = default;
        [Tooltip("Multiplier for the time delta in the simulation. Higher values will increase " +
            "simulation speed while lower ones will make it appear as if it is in slow motion.")]
        [SerializeField, Range(1.0f, 10.0f)] protected float _simulationSpeedMultiplier = 1.0f;
        [Tooltip("Affecters that should affect every boid in the simulation.")]
        [SerializeField] protected List<SimulationAffecterComponent> _globalAffecters = null;

        /// <summary>
        /// Bounds specifying the simulation area of the simulation. Each boid will have to
        /// make sure he never leaves this area.
        /// </summary>
        public Bounds SimulationAreaBounds { get => _simulationAreaBounds; }
        /// <summary>
        /// Current speed of the simulation. Basically a multiplier for the time delta.
        /// </summary>
        public float SimulationSpeed { get => _simulationSpeedMultiplier; set => _simulationSpeedMultiplier = value; }

        /// <summary>
        /// Function returns reference to the array of boid spawners for this boid simulation.
        /// </summary>
        /// <returns>Reference to the array containing boid spawners for this boid simulation.</returns>
        public abstract BoidSpawnerBase[] GetBoidSpawners();
        /// <summary>
        /// Function spawns boids by invoking the spawn method of the referenced boids spawners.
        /// </summary>
        protected abstract void SpawnBoids();
        /// <summary>
        /// Function runs one tick of the boid simulation. This function will be called every frame.
        /// </summary>
        /// <param name="timeDelta">Time passed from the last simulation update.</param>
        protected abstract void UpdateSimulation(float timeDelta);

        protected virtual void OnDrawGizmosSelected()
        {
            // Bounds visualization in the scene.
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(_simulationAreaBounds.center, _simulationAreaBounds.size);
        }

        protected virtual void Start()
        {
            // Since script is active in the editor as well, make sure that the boids initialization
            // only happens in the play mode.
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif

            InitializeBoidsSimulation();
        }

        protected virtual void Update()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                UpdateSimulationAreaPosition();
                return;
            }
#endif

            float timeDelta = Time.deltaTime * _simulationSpeedMultiplier;
            UpdateSimulation(timeDelta);
        }

        /// <summary>
        /// Function adds the provided <paramref name="simulationAffecters"/> as global affecters that affect
        /// every boid in the simulation.
        /// </summary>
        /// <param name="simulationAffecters">Collection of <see cref="SimulationAffecterComponent"/> that will
        /// affect every boid in the simulation.</param>
        public virtual void AddGlobalAffecters(SimulationAffecterComponent[] simulationAffecters)
        {
            foreach(SimulationAffecterComponent affecterToAdd in simulationAffecters)
            {
                if (affecterToAdd != null && !_globalAffecters.Contains(affecterToAdd))
                {
                    _globalAffecters.Add(affecterToAdd);
                }
            }
        }

        /// <summary>
        /// Function initializes everything for successful execution of the simulation. It should spawn
        /// boids, initialize data structures, cache required references and ensure that the simulation
        /// can be started in the Update method.
        /// </summary>
        protected virtual void InitializeBoidsSimulation()
        {
            SpawnBoids();

            // Only active spawners participate in the simulation. An inactive (ecosystem-controlled,
            // zero-school) spawner is skipped so the group IDs assigned below stay dense — 0..N-1 across
            // the active set — which is what the render/compute shaders index by.
            BoidSpawnerBase[] allSpawners = GetBoidSpawners();
            List<BoidSpawnerBase> activeSpawners = new List<BoidSpawnerBase>(allSpawners.Length);
            foreach (BoidSpawnerBase spawner in allSpawners)
            {
                if (spawner != null && spawner.IsActive)
                {
                    activeSpawners.Add(spawner);
                }
            }
            int activeCount = activeSpawners.Count;

            // Assign an ID to each active boid group that will differentiate it from other groups
            // and also let it compare its fish species' size compared to other boid groups.
            // Higher ID means that the fish species is larger than the other one.
            ComparableBounds[] boidFishSizesBounds = new ComparableBounds[activeCount];
            for (int i = 0; i < activeCount; i++)
            {
                int boundsID = i;
                boidFishSizesBounds[i] = new ComparableBounds { OriginalBoundsID = boundsID, Bounds = activeSpawners[i].FishSizeBounds };
            }
            for (int i = 0; i < activeCount; i++)
            {
                int boidGroupIndex = i;
                activeSpawners[boidGroupIndex].SetId(CalculateBoidGroupId(boidGroupIndex, boidFishSizesBounds));
            }
        }

        /// <summary>
        /// Function calculates the ID of the boid group based on the size of the fish species.
        /// </summary>
        /// <param name="boidGroupIndex">Index of the boid group for which the ID should be calculated.</param>
        /// <param name="boidGroupsFishSizeBounds">Array containing size of all fish species in the simulation.</param>
        /// <returns>ID of the boid group represented by the <paramref name="boidGroupIndex"/>.</returns>
        protected virtual int CalculateBoidGroupId(int boidGroupIndex, ComparableBounds[] boidGroupsFishSizeBounds)
        {
            BoundsComparer boundsComparer = new BoundsComparer();
            List<ComparableBounds> sortedBounds = boidGroupsFishSizeBounds.OrderBy(x => x.Bounds, boundsComparer).ToList();

            for(int i = 0; i < sortedBounds.Count; i++)
            {
                if (sortedBounds[i].OriginalBoundsID == boidGroupIndex)
                {
                    return i;
                }
            }

            Debug.LogError("This should never happen, the ID of the bounds haven't been correctly compared, returning 0 instead.");
            return 0;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Function updates the center of the simulation area while in editor mode.
        /// </summary>
        private void UpdateSimulationAreaPosition()
        {
            _simulationAreaBounds.center = transform.position;
        }
#endif
    }
}