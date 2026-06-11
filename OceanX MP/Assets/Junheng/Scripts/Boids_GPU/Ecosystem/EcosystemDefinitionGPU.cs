using System.Collections.Generic;
using UnityEngine;

namespace OceanX.BoidsGPU.Ecosystem
{
    /// <summary>
    /// Top-level asset that defines the full GPU ecosystem — which species exist and
    /// the size of the simulation area. Mirrors EcosystemDefinition (CPU Ecosystem)
    /// but references GPU species assets.
    /// Create via Assets > Create > OceanX GPU > Ecosystem Definition GPU.
    /// Assign one to an EcosystemSimulationGPU component to run the GPU simulation.
    /// </summary>
    [CreateAssetMenu(fileName = "EcosystemDefinition", menuName = "OceanX/Ecosystem Definition")]
    public class EcosystemDefinitionGPU : ScriptableObject
    {
        [Header("Identity")]
        public string EcosystemName = "Coral Reef (GPU)";

        [Header("Species")]
        [Tooltip("All GPU species in this ecosystem. Order determines RuntimeId.")]
        public List<SpeciesDataGPU> Species = new List<SpeciesDataGPU>();

        [Header("Simulation Area (fallback only)")]
        [Tooltip("Fallback bounds. At runtime EcosystemSimulationGPU reads the simulation volume straight " +
                 "from the BoidSimulationGPU's Simulation Area Bounds, so these values are only used when " +
                 "no BoidSimulationGPU is assigned. You normally don't need to match them by hand anymore.")]
        public Vector3 SimulationCenter = Vector3.zero;
        public Vector3 SimulationSize   = new Vector3(100f, 40f, 100f);

        /// <summary>Unity Bounds built from SimulationCenter and SimulationSize.</summary>
        public Bounds SimulationBounds => new Bounds(SimulationCenter, SimulationSize);
    }
}
