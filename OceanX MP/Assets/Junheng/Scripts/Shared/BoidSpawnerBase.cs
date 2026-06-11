using OceanX.BoidsCPU;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OceanX
{
    /// <summary>
    /// Base class for all boid spawners.
    /// </summary>
    public abstract class BoidSpawnerBase : MonoBehaviour
    {
        [Header("Boids Data: ")]
        [SerializeField] protected BoidSpawnData _boidSpawnData = default;
        [Tooltip("Into how many tight groups should the boids be grouped into " +
            "upon initialization. Each group is located in a different area inside the simulation zone.")]
        [SerializeField, Range(1, 100)] protected int _initialGroupsCount = 2;

        protected int _boidGroupId = 0;

        // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
        // Number of schools this spawner currently represents. 0 = excluded from the
        // simulation entirely (no boids, no targets, no draw call). Only meaningful once
        // EcosystemSimulationGPU takes control via SetSchoolConfiguration.
        protected int _schoolCount = 0;
        // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
        // True once EcosystemSimulationGPU drives this spawner's school count. Spawners that
        // are never ecosystem-controlled (e.g. standalone boid demos) keep their legacy
        // "always active" behaviour so existing scenes are unaffected.
        protected bool _ecosystemControlled = false;

        /// <summary>
        /// Collection of simulation affecters that the boids of this boid group
        /// will try to reach.
        /// </summary>
        public SimulationAffecter[] Targets { get => GetSimulationAffecters(_boidSpawnData.Targets); }
        /// <summary>
        /// Collection of simulation affecters that the boids of this boid group
        /// will try to avoid at all costs.
        /// </summary>
        public SimulationAffecter[] Obstacles { get => GetSimulationAffecters(_boidSpawnData.Obstacles); }
        /// <summary>
        /// Bounds of the fish species that will be spawned by this boid spawner.
        /// They represent the size of the fish.
        /// </summary>
        public Bounds FishSizeBounds { get => _boidSpawnData.BoidMesh.bounds; }
        /// <summary>
        /// Reference to the boid spawn data structure that contains spawn properties 
        /// that define the boids spawned by this spawner inside this simulation.
        /// </summary>
        public BoidSpawnData SpawnData { get => _boidSpawnData; }
        /// <summary>
        /// ID of the boid group that this spawner spawned.
        /// </summary>
        public int BoidGroupId { get => _boidGroupId; }
        /// <summary>
        /// Total number of sub-groups that this boid species will be initially divided into.
        /// </summary>
        public int InitialGroupsCount { get => _initialGroupsCount; }

        // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
        /// <summary>Current number of schools (sub-groups) this spawner represents.</summary>
        public int SchoolCount { get => _schoolCount; }

        // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
        /// <summary>
        /// Whether this spawner participates in the simulation. An ecosystem-controlled
        /// spawner with zero schools is inactive: it is excluded from the buffer concat,
        /// spatial grid, target collection and rendering. Spawners that were never placed
        /// under ecosystem control are always active (legacy behaviour).
        /// </summary>
        public virtual bool IsActive { get => !_ecosystemControlled || _schoolCount > 0; }

        /// <summary>
        /// Function spawns boid instances inside the provided <paramref name="simulationAreaBounds"/>.
        /// </summary>
        /// <param name="simulationAreaBounds">Bounds of the simulation inside which the boids will be instantiated.</param>
        public abstract void SpawnBoids(Bounds simulationAreaBounds);
        /// <summary>
        /// Function updates the boid group ID for every boid in the simulation.
        /// </summary>
        /// <param name="boidGroupId">ID of the boid group that should be updated on every boid.</param>
        protected abstract void UpdateBoidGroupId(int boidGroupId);

        /// <summary>
        /// Function sets the ID of the boid group to every boid created by this spawner.
        /// </summary>
        /// <param name="boidGroupId">ID of the boid group that uniquely identifies this boid group and
        /// can be used for comparing the size of the fish species with other boid groups.</param>
        public virtual void SetId(int boidGroupId)
        {
            _boidGroupId = boidGroupId;

            // Update ID for every boid in the simulation.
            UpdateBoidGroupId(boidGroupId);

            // Update the boid group ID for every simulation affecter that affects this simulation.
            foreach (SimulationAffecterComponent simulationAffecterComponent in _boidSpawnData.Targets)
            {
                simulationAffecterComponent.SetAffecterID(boidGroupId);
            }
            foreach (SimulationAffecterComponent simulationAffecterComponent in _boidSpawnData.Obstacles)
            {
                simulationAffecterComponent.SetAffecterID(boidGroupId);
            }
        }

        /// <summary>
        /// Function adds the provided <paramref name="target"/> to the collection of targets that
        /// affect the boids spawned by this boid spawner.
        /// </summary>
        /// <param name="target">Reference to the <see cref="SimulationAffecterComponent"/> that will
        /// be cached.</param>
        public virtual void AddTarget(SimulationAffecterComponent target)
        {
            if (_boidSpawnData.Targets == null)
            {
                _boidSpawnData.Targets = new SimulationAffecterComponent[1] { target };
                return;
            }

            List<SimulationAffecterComponent> targets = _boidSpawnData.Targets.ToList();
            targets.Add(target);
            _boidSpawnData.Targets = targets.ToArray();
        }

        /// <summary>
        /// Function removes references to all targets of the simulation.
        /// </summary>
        public virtual void ClearTargets()
        {
            _boidSpawnData.Targets = null;
        }

        // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
        /// <summary>Sets the number of initial boid sub-groups on this spawner. Minimum 1.</summary>
        public void SetInitialGroupsCount(int count) => _initialGroupsCount = Mathf.Max(1, count);

        // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
        /// <summary>Sets the total boid count on this spawner. Minimum 1.</summary>
        public void SetBoidsCount(int count) => _boidSpawnData.BoidsCount = Mathf.Max(1, count);

        // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
        /// <summary>
        /// Places this spawner under ecosystem control and configures it for <paramref name="schools"/>
        /// schools of <paramref name="fishPerSchool"/> fish each. Passing 0 schools deactivates the
        /// spawner (see <see cref="IsActive"/>). The underlying BoidsCount and InitialGroupsCount keep
        /// their minimum-1 clamps so no GPU buffer is ever sized from a zero here — IsActive is the real
        /// gate that excludes an empty species from the simulation.
        /// </summary>
        public void SetSchoolConfiguration(int schools, int fishPerSchool)
        {
            _ecosystemControlled = true;
            _schoolCount = Mathf.Max(0, schools);
            SetInitialGroupsCount(_schoolCount);                       // clamps to >= 1
            SetBoidsCount(_schoolCount * Mathf.Max(1, fishPerSchool)); // clamps to >= 1
        }

        // ECOSYSTEM HOOK — added for EcosystemSimulationGPU, do not remove
        /// <summary>Removes a specific target affecter from this spawner's target list.</summary>
        public virtual void RemoveTarget(SimulationAffecterComponent target)
        {
            if (_boidSpawnData.Targets == null || _boidSpawnData.Targets.Length == 0) return;
            List<SimulationAffecterComponent> targets = _boidSpawnData.Targets.ToList();
            targets.Remove(target);
            _boidSpawnData.Targets = targets.ToArray();
        }

        /// <summary>
        /// Function returns an array of all affecters affecting the boids spawned by this boid spawner.
        /// </summary>
        /// <param name="affecterComponents">Reference to all affecters components that wrap the simulation 
        /// affecter structures.</param>
        /// <returns>Array of all affecters affecting the boids spawned by this boid spawner.</returns>
        protected virtual SimulationAffecter[] GetSimulationAffecters(SimulationAffecterComponent[] affecterComponents)
        {
            if (affecterComponents == null || affecterComponents.Length == 0)
            {
                return null;
            }

            SimulationAffecter[] affecters = new SimulationAffecter[affecterComponents.Length];
            for (int i = 0; i < affecterComponents.Length; i++)
            {
                affecters[i] = affecterComponents[i].Affecter;
            }
            return affecters;
        }
    }
}