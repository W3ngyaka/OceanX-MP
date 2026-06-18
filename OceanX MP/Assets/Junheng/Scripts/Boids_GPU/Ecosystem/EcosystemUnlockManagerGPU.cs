using System;
using System.Collections.Generic;
using UnityEngine;
using OceanX.BoidsGPU.Ecosystem;

/// <summary>
/// Drives the species unlock / progression system, ported from the HTML prototype's gate model.
/// A locked species unlocks the moment BOTH conditions hold:
///   1. every entry in its <see cref="SpeciesData.requires"/> is met — the required species has
///      at least that many schools live in the simulation, and
///   2. live eco-health (0–100%) ≥ its <see cref="SpeciesData.minHealth"/>.
///
/// Unlocking is one-way (latching) — once unlocked a species stays unlocked, like the prototype.
///
/// The config lives on Aloysius' <see cref="SpeciesData"/> assets. Each one links to a
/// <see cref="SpeciesDataGPU"/> via <c>gpuSpecies</c>; this manager follows that link to read the
/// authoritative, network-synced population (and eco-health) from <see cref="EcosystemNetworkManagerGPU"/>,
/// so it runs the same on host and tablet client.
///
/// UI usage:
///   - <see cref="IsUnlocked(SpeciesData)"/> to grey/silhouette a node
///   - subscribe to <see cref="OnSpeciesUnlocked"/> for the "New Species!" toast
///   - <see cref="GetLockInfo"/> to render the locked modal's requirement checklist
/// </summary>
public class EcosystemUnlockManagerGPU : MonoBehaviour
{
    public static EcosystemUnlockManagerGPU Instance { get; private set; }

    [Header("Species")]
    [Tooltip("All UI species in the game. Order doesn't matter — unlock config is read off each asset.")]
    [SerializeField] private List<SpeciesData> _allSpecies = new List<SpeciesData>();

    [Header("Data source")]
    [Tooltip("Optional. If set (or auto-found), the manager reads population + eco-health straight " +
             "from the simulation — lets you test on the host/standalone with the debug harness, no " +
             "netcode needed. When absent it falls back to the synced netcode layer (tablet client).")]
    [SerializeField] private EcosystemSimulationGPU _simulation;

    [Header("Tuning")]
    [Tooltip("How often (seconds) to re-evaluate unlocks against live sim data.")]
    [SerializeField] private float _checkInterval = 0.5f;

    [Tooltip("Log each unlock to the Console — handy for testing without the UI wired up.")]
    [SerializeField] private bool _logUnlocks = true;

    /// <summary>Fired once per species the first time it unlocks. UI shows the reveal/toast.</summary>
    public event Action<SpeciesData> OnSpeciesUnlocked;
    /// <summary>Fired after any pass that changed unlock state (UI can refresh all nodes).</summary>
    public event Action OnUnlockStateChanged;

    private readonly Dictionary<SpeciesData, bool> _unlocked = new Dictionary<SpeciesData, bool>();
    // Hint progression per locked species (replaces GameState.tapCounts).
    private readonly Dictionary<SpeciesData, int> _tapCounts = new Dictionary<SpeciesData, int>();
    private float _timer;
    private bool _initialised;

    /// <summary>Live eco-health as a 0–100 percentage (rounded), or 0 if no data source is up yet.</summary>
    public int CurrentHealthPercent
    {
        get
        {
            if (_simulation != null)
                return Mathf.RoundToInt(_simulation.EcoHealth01 * 100f);
            var net = EcosystemNetworkManagerGPU.Instance;
            return net != null ? Mathf.RoundToInt(net.GetEcoHealth() * 100f) : 0;
        }
    }

    /// <summary>True when some data source (direct sim or synced netcode) is available to read.</summary>
    private bool HasDataSource => _simulation != null || EcosystemNetworkManagerGPU.Instance != null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Init()
    {
        if (_simulation == null)
            _simulation = FindFirstObjectByType<EcosystemSimulationGPU>();

        _unlocked.Clear();
        foreach (var sd in _allSpecies)
        {
            if (sd == null) continue;
            _unlocked[sd] = sd.startUnlocked;
        }
        _initialised = true;
        OnUnlockStateChanged?.Invoke();
        RefreshAllBubbles();
    }

    private void Update()
    {
        if (!_initialised) Init();

        _timer += Time.deltaTime;
        if (_timer < _checkInterval) return;
        _timer = 0f;

        if (!HasDataSource) return;  // no sim / netcode to read yet
        CheckUnlocks();
    }

    /// <summary>
    /// Re-evaluate every locked species. Mirrors the prototype's checkUnlocks():
    /// reqsMet (all requirement counts) AND health ≥ minHealth → unlock.
    /// </summary>
    public void CheckUnlocks()
    {
        if (!_initialised) return;

        int health = CurrentHealthPercent;
        bool changed = false;

        foreach (var sd in _allSpecies)
        {
            if (sd == null || _unlocked[sd]) continue;
            if (health < sd.minHealth) continue;
            if (!RequirementsMet(sd)) continue;

            _unlocked[sd] = true;
            changed = true;
            if (_logUnlocks)
                Debug.Log($"[Unlock] '{sd.speciesName}' unlocked  (health {health}% ≥ {sd.minHealth}%, requirements met).", this);
            NotificationManager.Instance?.ShowUnlocked(sd);   // "You've unlocked the …!" toast
            OnSpeciesUnlocked?.Invoke(sd);
        }

        if (changed)
        {
            OnUnlockStateChanged?.Invoke();
            RefreshAllBubbles();
        }
    }

    private bool RequirementsMet(SpeciesData sd)
    {
        if (sd.requires == null) return true;
        foreach (var req in sd.requires)
        {
            if (req == null || req.species == null) continue;     // ignore blank rows
            if (PopulationOf(req.species) < req.count) return false;
        }
        return true;
    }

    // Live school count for a species, via its gpuSpecies link.
    // Host/standalone: read the simulation directly. Tablet client: go through the synced netcode layer.
    private int PopulationOf(SpeciesData sd)
    {
        if (sd == null || sd.gpuSpecies == null) return 0;

        if (_simulation != null)
            return _simulation.CountGroups(sd.gpuSpecies);

        var tablet = TabletEcosystemUIGPU.Instance;
        var net = EcosystemNetworkManagerGPU.Instance;
        if (tablet == null || net == null) return 0;

        int idx = tablet.GetSpeciesIndex(sd.gpuSpecies);
        return idx >= 0 ? net.GetPopulation(idx) : 0;
    }

    // ─────────────────────────── UI query API ───────────────────────────

    /// <summary>Has this species unlocked? Locked-by-default for unknown species.</summary>
    public bool IsUnlocked(SpeciesData species)
    {
        return species != null && _unlocked.TryGetValue(species, out bool u) && u;
    }

    /// <summary>
    /// Records a tap on a locked species and returns the PRE-increment tap index (0,1,2,…),
    /// so the UI can show progressively clearer hints. Mirrors GameState.tapCounts.
    /// </summary>
    public int RegisterLockedTap(SpeciesData species)
    {
        if (species == null) return 0;
        int taps = _tapCounts.TryGetValue(species, out int v) ? v : 0;
        _tapCounts[species] = taps + 1;
        return taps;
    }

    /// <summary>Tell every SpeciesBubble in the scene to re-read its lock state. Mirrors GameState.RefreshAllBubbles.</summary>
    private void RefreshAllBubbles()
    {
        var bubbles = FindObjectsByType<SpeciesBubble>(FindObjectsSortMode.None);
        foreach (var b in bubbles)
            b.Refresh();
    }

    /// <summary>One requirement row, resolved against the live sim — for the locked modal checklist.</summary>
    public struct RequirementStatus
    {
        public SpeciesData Species;
        public int Required;
        public int Current;
        public bool Met;
    }

    /// <summary>
    /// Everything the locked modal needs: the health gate (met + threshold + current)
    /// and the per-prey requirement rows. Returns false if the species is unknown.
    /// </summary>
    public bool GetLockInfo(SpeciesData species,
                            out bool healthMet, out int minHealth, out int currentHealth,
                            out List<RequirementStatus> requirements)
    {
        currentHealth = CurrentHealthPercent;
        minHealth = species != null ? species.minHealth : 0;
        healthMet = currentHealth >= minHealth;
        requirements = new List<RequirementStatus>();

        if (species == null) return false;

        if (species.requires != null)
        {
            foreach (var req in species.requires)
            {
                if (req == null || req.species == null) continue;
                int cur = PopulationOf(req.species);
                requirements.Add(new RequirementStatus
                {
                    Species  = req.species,
                    Required = req.count,
                    Current  = cur,
                    Met      = cur >= req.count
                });
            }
        }
        return true;
    }
}
