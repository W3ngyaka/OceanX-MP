using UnityEngine;
using OceanX.BoidsGPU.Ecosystem;

// Holds the EcosystemDefinitionGPU and resolves a species -> netcode index for the
// tablet UI (SpeciesBubble). That index is the contract used by every
// EcosystemNetworkManagerGPU RPC. No visual UI of its own — just the lookup service.
public class TabletEcosystemUIGPU : MonoBehaviour
{
    public static TabletEcosystemUIGPU Instance { get; private set; }

    [Header("References")]
    public EcosystemDefinitionGPU Ecosystem;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (Ecosystem == null)
            Debug.LogError("[TabletEcosystemUIGPU] Missing Ecosystem reference.", this);
    }

    // Returns the index of a species in the ecosystem list, or -1 if not present.
    public int GetSpeciesIndex(SpeciesDataGPU species)
    {
        if (Ecosystem == null || species == null) return -1;
        return Ecosystem.Species.IndexOf(species);
    }
}
