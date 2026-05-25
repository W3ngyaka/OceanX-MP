using UnityEngine;

namespace OceanX.BoidsGPU.Ecosystem
{
    /// <summary>
    /// Thin adapter that lets UI scripts talk to EcosystemSimulationGPU using
    /// SpeciesDataGPU directly — no CPU SpeciesDefinition mapping needed.
    ///
    /// Attach to any GameObject. UI buttons call AddSpecies / RemoveSpecies / CountGroups
    /// on this component, passing the SpeciesDataGPU asset for the species they control.
    ///
    /// Example wiring (UI button OnClick):
    ///   adapter.AddSpecies(speciesData_GiantTrevally)
    ///   adapter.RemoveSpecies(speciesData_Clownfish)
    ///   adapter.CountGroups(speciesData_GoldenTrevally)
    /// </summary>
    public class EcosystemUIAdapterGPU : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EcosystemSimulationGPU _ecosystemGPU;

        private void Start()
        {
            if (_ecosystemGPU == null)
            {
                Debug.LogError("[EcosystemUIAdapterGPU] EcosystemSimulationGPU not assigned.", this);
                enabled = false;
            }
        }

        /// <summary>Adds one boid school of the given GPU species.</summary>
        public void AddSpecies(SpeciesDataGPU species) => _ecosystemGPU.AddSpecies(species);

        /// <summary>Removes one boid school of the given GPU species.</summary>
        public void RemoveSpecies(SpeciesDataGPU species) => _ecosystemGPU.RemoveSpecies(species);

        /// <summary>Returns the current number of active boid schools for the given GPU species.</summary>
        public int CountGroups(SpeciesDataGPU species) => _ecosystemGPU.CountGroups(species);
    }
}
