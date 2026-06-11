using Unity.Netcode;
using UnityEngine;
using OceanX.BoidsGPU.Ecosystem;

// Make this a prefab and register it in NetworkManager's Network Prefabs list.
// The host spawns it at runtime — the client receives it automatically.
// On the host it finds EcosystemSimulationGPU and drives the sync.
// On the client it just exposes GetPopulation() for the tablet UI to read.
[RequireComponent(typeof(NetworkObject))]
public class EcosystemNetworkManagerGPU : NetworkBehaviour
{
    public static EcosystemNetworkManagerGPU Instance { get; private set; }

    [SerializeField] private float _populationSyncInterval = 1f;

    private EcosystemSimulationGPU _simulation;
    private NetworkList<int> _populationCounts;
    private float _syncTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _populationCounts = new NetworkList<int>();
    }

    public override void OnNetworkSpawn()
    {
        Instance = this;

        if (!IsServer) return;

        _simulation = FindFirstObjectByType<EcosystemSimulationGPU>();
        if (_simulation == null)
        {
            Debug.LogError("[EcosystemNetworkManagerGPU] No EcosystemSimulationGPU found in scene.");
            return;
        }

        Debug.Log($"[EcosystemNetworkManagerGPU] Found simulation with {_simulation.Ecosystem.Species.Count} species.");

        int speciesCount = _simulation.Ecosystem.Species.Count;
        for (int i = 0; i < speciesCount; i++)
            _populationCounts.Add(0);
    }

    private void Update()
    {
        if (!IsServer || _simulation == null) return;

        _syncTimer += Time.deltaTime;
        if (_syncTimer < _populationSyncInterval) return;
        _syncTimer = 0f;

        SyncPopulations();
    }

    // Recount every species and write into the synced list. Called on the periodic
    // tick AND immediately after an add/remove so the tablet reflects user actions
    // without waiting for the next tick.
    private void SyncPopulations()
    {
        for (int i = 0; i < _simulation.Ecosystem.Species.Count; i++)
        {
            SpeciesDataGPU s = _simulation.Ecosystem.Species[i];
            if (s != null && i < _populationCounts.Count)
                _populationCounts[i] = _simulation.CountGroups(s);
        }
        _syncTimer = 0f;   // reset so the periodic tick doesn't immediately re-fire
    }

    // Called from the tablet client — executes on the host/server.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestAddSpeciesRpc(int speciesIndex)
    {
        if (_simulation == null) return;
        if ((uint)speciesIndex >= (uint)_simulation.Ecosystem.Species.Count) return;
        _simulation.AddSpecies(_simulation.Ecosystem.Species[speciesIndex]);
        SyncPopulations();   // reflect the change on the tablet right away
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestRemoveSpeciesRpc(int speciesIndex)
    {
        if (_simulation == null) return;
        if ((uint)speciesIndex >= (uint)_simulation.Ecosystem.Species.Count) return;
        _simulation.RemoveSpecies(_simulation.Ecosystem.Species[speciesIndex]);
        SyncPopulations();   // reflect the change on the tablet right away
    }

    // Read by the tablet UI (ModalController) every frame.
    public int GetPopulation(int speciesIndex)
    {
        if (speciesIndex < 0 || speciesIndex >= _populationCounts.Count) return 0;
        return _populationCounts[speciesIndex];
    }
}
