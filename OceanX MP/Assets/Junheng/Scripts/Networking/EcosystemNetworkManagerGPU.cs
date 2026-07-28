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

    [SerializeField] private float _populationSyncInterval = 0.15f;

    private EcosystemSimulationGPU _simulation;
    private NetworkList<int> _populationCounts;
    // Static per-species school cap, synced once on spawn so clients can grey the Add button at the cap.
    private NetworkList<int> _maxSchools;
    // Live per-species classification (EcosystemSimulationGPU.SpeciesStatus stored as int, because
    // NetworkList needs an unmanaged serialisable type). Written by the server each sync so the
    // tablet's Overpopulated badge uses the simulation's own rule instead of re-deriving one.
    private NetworkList<int> _speciesStatus;
    // BALANCE DELTA: signed per-species count change that would clear its own unhealthy state.
    // Computed host-side because the thresholds are private to the simulation.
    private NetworkList<int> _speciesDelta;
    // Live ecosystem health in 0..1, written by the server each sync, read by the tablet health bar.
    private readonly NetworkVariable<float> _ecoHealth = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private float _syncTimer;

    // Server-authoritative "the experience has begun" flag for the cross-device attract flow:
    // the large screen sits on its title/attract state and the tablet shows "Tap to Start"; the
    // tablet's tap calls RequestStartRpc(), the host flips this, and BOTH screens react.
    private readonly NetworkVariable<bool> _hasStarted = new NetworkVariable<bool>(false);
    public bool HasStarted => _hasStarted.Value;

    // ---- Tablet 'currently viewing' mirror (UI) ----
    // Tablet reports which species its info modal shows so the LARGE SCREEN can mirror a
    // summarised card for bystanders. -1 = nothing being viewed.
    private readonly NetworkVariable<int> _viewedSpecies = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public int ViewedSpecies => _viewedSpecies.Value;
    public event System.Action<int> OnViewedSpeciesChanged;
    public event System.Action OnStarted;   // fires on host + client when the flag turns true

    // Exhibit "fresh start": bumped by the server (SignalResetApplied) at the bubble transition's COVERED
    // peak. Every device watches it and, on any change past 0, runs its local cleanup via OnReset (re-lock,
    // clear tablet counts, return to attract, …). Bumped only after the host has emptied + synced 0 health,
    // so the tablet re-locks with health already 0 (no re-unlock race). A monotonic counter so back-to-back
    // resets each register, and a late-joining client that reads a non-zero value simply starts clean.
    private readonly NetworkVariable<int> _resetGeneration = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public event System.Action OnReset;              // fires on host + client when a reset is APPLIED
    public event System.Action OnHostResetRequested; // server-side only: the host starts its reset sequence

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _populationCounts = new NetworkList<int>();
        _maxSchools = new NetworkList<int>();
        _speciesStatus = new NetworkList<int>();
        _speciesDelta = new NetworkList<int>();   // BALANCE DELTA
    }

    public override void OnNetworkSpawn()
    {
        Instance = this;

        // Both host and client watch the shared start flag so their screens leave the attract state together.
        _hasStarted.OnValueChanged += (_, now) => { if (now) OnStarted?.Invoke(); };
        _viewedSpecies.OnValueChanged += (_, now) => OnViewedSpeciesChanged?.Invoke(now);
        // Fresh-start broadcast: fires on host + client the moment the server bumps the generation.
        _resetGeneration.OnValueChanged += (_, now) =>
        {
            Debug.Log($"[NetMgr] _resetGeneration -> {now} (IsServer={IsServer}, OnReset subs={(OnReset?.GetInvocationList().Length ?? 0)}).");
            if (now > 0) OnReset?.Invoke();
        };

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
        {
            _populationCounts.Add(0);
            _speciesStatus.Add((int)EcosystemSimulationGPU.SpeciesStatus.Absent);
            _speciesDelta.Add(0);   // BALANCE DELTA
            SpeciesDataGPU s = _simulation.Ecosystem.Species[i];
            _maxSchools.Add(s != null ? _simulation.GetMaxSchools(s) : 0);
        }
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
            if (s == null) continue;
            if (i < _populationCounts.Count)
                // Committed count (excludes schools mid swim-out) so the tablet number drops the
                // instant Remove is pressed, instead of waiting for the fish to finish leaving.
                _populationCounts[i] = _simulation.CountCommittedGroups(s);
            if (i < _speciesStatus.Count)
                _speciesStatus[i] = (int)_simulation.GetSpeciesStatus(s);
            if (i < _speciesDelta.Count)
                _speciesDelta[i] = _simulation.GetSpeciesDelta(s);   // BALANCE DELTA
        }
        _ecoHealth.Value = _simulation.EcoHealth01;   // push the live health score to clients
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

    // Called from the tablet's "Tap to Start" — runs on the host and flips the shared start flag
    // that both the large screen and tablet watch (via OnStarted / HasStarted).
    // Tablet tells the host which species its info modal is showing (-1 = closed).
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetViewedSpeciesRpc(int speciesIndex)
    {
        _viewedSpecies.Value = speciesIndex;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestStartRpc()
    {
        if (!_hasStarted.Value) _hasStarted.Value = true;
    }

    // Exhibit "fresh start". Split so the reset is HIDDEN behind the big screen's bubble wipe:
    //   RequestResetRpc()    — anyone -> server: "begin a reset" (host's ExhibitReset plays the bubble wipe).
    //   PerformResetCore()   — host: empty the ocean + drop to attract + sync 0s, run at the covered PEAK.
    //   SignalResetApplied() — host: bump the generation so BOTH screens run their local cleanup via OnReset.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestResetRpc()
    {
        OnHostResetRequested?.Invoke();   // host: ExhibitReset plays the bubble wipe, resets at the peak
    }

    // Host-authoritative reset, run at the transition's covered peak (screen hidden). No-op off the server.
    public void PerformResetCore()
    {
        if (!IsServer) return;
        if (_simulation != null) _simulation.ResetToEmpty();  // remove every school of every species now
        _hasStarted.Value = false;                            // back to the title / "Tap to Start" attract
        SyncPopulations();                                    // push 0 counts + 0 health to the tablet
    }

    // Host: tell BOTH screens to run their local cleanup. Fired right AFTER PerformResetCore so the tablet
    // re-locks with health already 0 (no re-unlock race). No-op off the server.
    public void SignalResetApplied()
    {
        if (IsServer) _resetGeneration.Value += 1;   // -> OnReset on host + client
    }

    // Read by the tablet UI (ModalController) every frame.
    public int GetPopulation(int speciesIndex)
    {
        if (speciesIndex < 0 || speciesIndex >= _populationCounts.Count) return 0;
        return _populationCounts[speciesIndex];
    }

    // Read by the tablet UI (ModalController) to grey the Add button at the cap.
    // Returns 0 when the cap hasn't synced yet — the UI treats 0 as "unknown" and the host
    // enforces the real cap regardless.
    public int GetMaxSchools(int speciesIndex)
    {
        if (speciesIndex < 0 || speciesIndex >= _maxSchools.Count) return 0;
        return _maxSchools[speciesIndex];
    }

    // Read by the tablet eco-health bar (Health.cs). 0..1, replicated from the host.
    public float GetEcoHealth() => _ecoHealth.Value;

    // The species' live classification as the SIMULATION sees it (not a UI-side guess).
    // Absent until the first sync arrives.
    public EcosystemSimulationGPU.SpeciesStatus GetSpeciesStatus(int speciesIndex)
    {
        if (speciesIndex < 0 || speciesIndex >= _speciesStatus.Count)
            return EcosystemSimulationGPU.SpeciesStatus.Absent;
        return (EcosystemSimulationGPU.SpeciesStatus)_speciesStatus[speciesIndex];
    }

    // BALANCE DELTA: how many of this species to add (+) or remove (-) to make it healthy.
    // 0 means it is fine, or the fix belongs to another species (check GetSpeciesStatus).
    public int GetSpeciesDelta(int speciesIndex)
    {
        if (_speciesDelta == null || speciesIndex < 0 || speciesIndex >= _speciesDelta.Count) return 0;
        return _speciesDelta[speciesIndex];
    }

    // Convenience for the tablet badges: is the sim currently calling this species overpopulated?
    public bool IsOverpopulated(int speciesIndex) =>
        GetSpeciesStatus(speciesIndex) == EcosystemSimulationGPU.SpeciesStatus.Overpopulated;
}
