using Unity.Netcode;
using UnityEngine;

// Make this a prefab and register it in NetworkManager's Network Prefabs list.
// The host spawns it at runtime — the client receives it automatically.
// On the host it finds EcosystemSimulation and drives the sync.
// On the client it just exposes GetPopulation() for the tablet UI to read.
[RequireComponent(typeof(NetworkObject))]
public class EcosystemNetworkManager : NetworkBehaviour
{
    public static EcosystemNetworkManager Instance { get; private set; }

    [SerializeField] private float _populationSyncInterval = 1f;

    private EcosystemSimulation _simulation;
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
        Debug.Log($"[EcosystemNetworkManager] OnNetworkSpawn — IsServer: {IsServer}, IsClient: {IsClient}");

        if (!IsServer) return;

        // Auto-find — no manual wiring needed in the prefab
        _simulation = FindFirstObjectByType<EcosystemSimulation>();
        if (_simulation == null)
        {
            Debug.LogError("[EcosystemNetworkManager] No EcosystemSimulation found in scene.");
            return;
        }

        Debug.Log($"[EcosystemNetworkManager] Found simulation with {_simulation.Ecosystem.Species.Count} species.");

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

        for (int i = 0; i < _simulation.Ecosystem.Species.Count; i++)
        {
            SpeciesDefinition s = _simulation.Ecosystem.Species[i];
            if (s != null && i < _populationCounts.Count)
                _populationCounts[i] = _simulation.CountLiving(s);
        }
    }

    // Called from the tablet client — executes on the host/server.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestAddSpeciesRpc(int speciesIndex, int count)
    {
        if (_simulation == null) return;
        if ((uint)speciesIndex >= (uint)_simulation.Ecosystem.Species.Count) return;
        _simulation.AddSpecies(_simulation.Ecosystem.Species[speciesIndex], count);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestRemoveSpeciesRpc(int speciesIndex, int count)
    {
        if (_simulation == null) return;
        if ((uint)speciesIndex >= (uint)_simulation.Ecosystem.Species.Count) return;
        _simulation.RemoveSpecies(_simulation.Ecosystem.Species[speciesIndex], count);
    }

    // Read by TabletSpeciesCardUI every frame.
    public int GetPopulation(int speciesIndex)
    {
        if (speciesIndex < 0 || speciesIndex >= _populationCounts.Count) return 0;
        return _populationCounts[speciesIndex];
    }
}
