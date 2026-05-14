using Unity.Netcode;
using UnityEngine;

// Sits on the same GameObject as EcosystemSimulation.
// The tablet (client) calls ServerRpcs here — the laptop (host/server)
// receives them and forwards to EcosystemSimulation using the real
// SpeciesDefinition ScriptableObjects.
//
// Species are identified by their index in EcosystemDefinition.Species
// so only a plain int travels over the wire — no ScriptableObject serialisation needed.
//
// Scene setup:
//   EcosystemManager (GameObject)
//     ├─ NetworkObject          (add this component)
//     ├─ EcosystemSimulation    (existing)
//     └─ EcosystemNetworkBridge (this script)
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(EcosystemSimulation))]
public class EcosystemNetworkBridge : NetworkBehaviour
{
    private EcosystemSimulation _sim;

    private void Awake()
    {
        _sim = GetComponent<EcosystemSimulation>();
    }

    // -------------------------------------------------------------------------
    // Tablet → Server commands
    // -------------------------------------------------------------------------

    // Call this from the tablet UI instead of EcosystemSimulation.AddSpecies directly.
    [ServerRpc(RequireOwnership = false)]
    public void AddSpeciesServerRpc(int speciesIndex, int count = 1)
    {
        if (!ValidateIndex(speciesIndex)) return;
        _sim.AddSpecies(_sim.Ecosystem.Species[speciesIndex], count);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveSpeciesServerRpc(int speciesIndex, int count = 1)
    {
        if (!ValidateIndex(speciesIndex)) return;
        _sim.RemoveSpecies(_sim.Ecosystem.Species[speciesIndex], count);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ClearAllServerRpc()
    {
        if (_sim.Ecosystem == null) return;

        foreach (SpeciesDefinition species in _sim.Ecosystem.Species)
        {
            if (species == null) continue;
            int living = _sim.CountLiving(species);
            if (living > 0)
                _sim.RemoveSpecies(species, living);
        }
    }

    // -------------------------------------------------------------------------
    // Server → All clients (optional feedback — e.g. update tablet counters)
    // -------------------------------------------------------------------------

    // Call this from EcosystemSimulation whenever population changes if you want
    // the tablet to reflect live counts without polling.
    [ClientRpc]
    public void SyncPopulationClientRpc(int speciesIndex, int newCount)
    {
        // Handled by TabletUINetworkController if present on the client.
        TabletUINetworkController tabletUI = FindFirstObjectByType<TabletUINetworkController>();
        tabletUI?.OnPopulationSync(speciesIndex, newCount);
    }

    // -------------------------------------------------------------------------
    // Helper
    // -------------------------------------------------------------------------

    private bool ValidateIndex(int index)
    {
        if (_sim.Ecosystem == null)
        {
            Debug.LogWarning("[EcosystemNetworkBridge] No EcosystemDefinition assigned.");
            return false;
        }

        if (index < 0 || index >= _sim.Ecosystem.Species.Count)
        {
            Debug.LogWarning($"[EcosystemNetworkBridge] Species index {index} out of range.");
            return false;
        }

        return _sim.Ecosystem.Species[index] != null;
    }
}
