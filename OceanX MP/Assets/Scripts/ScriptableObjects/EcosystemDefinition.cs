using System.Collections.Generic;
using UnityEngine;

// Top-level asset that defines the full ecosystem — which species exist,
// how many there are, and the size of the simulation area.
// Create via Assets > Create > OceanX > Ecosystem Definition.
// Assign one to an EcosystemSimulation component to run the simulation.
[CreateAssetMenu(fileName = "EcosystemDefinition", menuName = "OceanX/Ecosystem Definition")]
public class EcosystemDefinition : ScriptableObject
{
    [Header("Identity")]
    public string EcosystemName = "Coral Reef";

    [Header("Species")]
    [Tooltip("All species in this ecosystem. Each is assigned a unique runtime ID based on its list index.")]
    public List<SpeciesDefinition> Species = new List<SpeciesDefinition>();

    [Header("Simulation Area")]
    public Vector3 SimulationCenter = Vector3.zero;
    public Vector3 SimulationSize   = new Vector3(100f, 40f, 100f);

#if UNITY_EDITOR
    [ContextMenu("Validate")]
    private void Validate()
    {
        if (Species == null || Species.Count == 0)
        {
            Debug.LogWarning($"[{EcosystemName}] No species defined.");
            return;
        }

        for (int i = 0; i < Species.Count; i++)
        {
            if (Species[i] == null)
            {
                Debug.LogWarning($"[{EcosystemName}] Species slot {i} is null.");
                continue;
            }

            SpeciesDefinition s = Species[i];
            string prefabStatus  = s.Prefab            != null ? "OK" : "MISSING PREFAB";
            string schoolStatus  = s.SchoolProperties  != null ? "OK" : "MISSING SCHOOL PROPS";
            string behaviorNote  = s.BehaviorProperties != null ? "has behavior" : "no behavior (schooling only)";

            Debug.Log($"  [{i}] {s.SpeciesName} ({s.Role}) — prefab:{prefabStatus} school:{schoolStatus} — {behaviorNote}");

            if (s.Role == SpeciesRole.Predator && s.PreySpecies.Count == 0)
                Debug.LogWarning($"  [{i}] {s.SpeciesName} is a Predator but has no prey assigned.");

            if ((s.Role == SpeciesRole.Prey || s.Role == SpeciesRole.Neutral) && s.PredatorSpecies.Count == 0)
                Debug.Log($"  [{i}] {s.SpeciesName} has no predators — it will never flee.");
        }

        Debug.Log($"[{EcosystemName}] Validation complete — {Species.Count} species.");
    }
#endif
}
