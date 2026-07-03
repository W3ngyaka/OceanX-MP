using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OceanX.BoidsGPU.Ecosystem;

/// <summary>
/// Drives the tablet's Add / Remove buttons for the currently-selected species,
/// fully decoupled from the UI-team scripts (SpeciesInfoPanel / ModalController).
///
/// Wiring (all in the Inspector — no UI-team script is touched):
///   • Put this on an always-active object (e.g. Ecosystem Panel or the Info panel).
///   • Assign addButton / removeButton.
///   • On EACH species bubble's Button.onClick, add a call to Select(...) and drag
///     that species' SpeciesDataGPU asset (its "_Data" file) as the argument.
///
/// The species index is the same contract every RPC uses: the position of the
/// species in EcosystemDefinitionGPU.Species, resolved via TabletEcosystemUIGPU.
///
/// OPTIMISTIC UI: a tap updates the shown count immediately (authoritative synced
/// count + the user's not-yet-confirmed intent), then reconciles as the host's real
/// count catches up. Because a removed school swims out before the host commits the
/// count, this hides that delay — the number drops the instant you tap, and the host
/// honours rapid taps in order (see EcosystemSimulationGPU's op queue).
/// </summary>
public class TabletAddRemoveUIGPU : MonoBehaviour
{
    public static TabletAddRemoveUIGPU Instance { get; private set; }

    [Header("Buttons (on the Info screen)")]
    public Button addButton;
    public Button removeButton;

    [Tooltip("Optional: shows the selected species' school count on the info screen, " +
             "next to +/- so the player sees feedback at the point of adding.")]
    public TMP_Text populationLabel;

    // Netcode index of the currently-selected species, or -1 when nothing is selected.
    private int _index = -1;

    // Optimistic UI state, keyed by species netcode index:
    //   _pendingDelta — the user's tapped-but-not-yet-confirmed change (+adds / -removes).
    //   _lastAuth     — the authoritative count last time we reconciled, to measure host movement.
    private readonly Dictionary<int, int> _pendingDelta = new Dictionary<int, int>();
    private readonly Dictionary<int, int> _lastAuth = new Dictionary<int, int>();
    private readonly List<int> _reconcileKeys = new List<int>();

    private void Awake()
    {
        Instance = this;
        if (addButton != null)    addButton.onClick.AddListener(OnAdd);
        if (removeButton != null) removeButton.onClick.AddListener(OnRemove);
        RefreshButtons();
    }

    /// <summary>Called from each bubble's Button.onClick (pass that bubble's SpeciesDataGPU asset).</summary>
    public void Select(SpeciesDataGPU species)
    {
        _index = TabletEcosystemUIGPU.Instance != null
            ? TabletEcosystemUIGPU.Instance.GetSpeciesIndex(species)
            : -1;
        RefreshButtons();
    }

    private void OnAdd()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (_index < 0 || net == null) return;
        int max = net.GetMaxSchools(_index);
        if (max > 0 && DisplayCount(_index) >= max) return; // already at cap optimistically

        AdjustPending(_index, +1);
        net.RequestAddSpeciesRpc(_index);
        RefreshButtons();
    }

    private void OnRemove()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (_index < 0 || net == null) return;
        if (DisplayCount(_index) <= 0) return; // already at zero optimistically

        AdjustPending(_index, -1);
        net.RequestRemoveSpeciesRpc(_index);
        RefreshButtons();
    }

    // Records a tapped change and seeds the reconcile baseline the first time we track this species.
    private void AdjustPending(int index, int delta)
    {
        if (!_lastAuth.ContainsKey(index))
        {
            var net = EcosystemNetworkManagerGPU.Instance;
            _lastAuth[index] = net != null ? net.GetPopulation(index) : 0;
        }
        _pendingDelta[index] = (_pendingDelta.TryGetValue(index, out int d) ? d : 0) + delta;
    }

    // The count to show: authoritative synced count + the user's not-yet-confirmed intent, clamped.
    private int DisplayCount(int index)
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net == null || index < 0) return 0;
        int auth = net.GetPopulation(index);
        int pending = _pendingDelta.TryGetValue(index, out int d) ? d : 0;
        int max = net.GetMaxSchools(index);
        int hi = max > 0 ? max : int.MaxValue;
        return Mathf.Clamp(auth + pending, 0, hi);
    }

    private void Update()
    {
        Reconcile();
        RefreshButtons();
    }

    // As the host's authoritative count moves in the DIRECTION the user asked for, consume that much
    // pending intent so the displayed target stays put and finally lands exactly on the real count.
    // Movement in the opposite direction (e.g. the auto population tick growing a species while the
    // user is removing it) is NOT counted against the user's intent — display just tracks it.
    private void Reconcile()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net == null) return;

        _reconcileKeys.Clear();
        foreach (var kv in _pendingDelta) _reconcileKeys.Add(kv.Key);

        for (int k = 0; k < _reconcileKeys.Count; k++)
        {
            int index = _reconcileKeys[k];
            int auth = net.GetPopulation(index);
            int last = _lastAuth.TryGetValue(index, out int la) ? la : auth;
            int pending = _pendingDelta[index];

            int move = auth - last;
            if (move != 0)
            {
                if (pending > 0 && move > 0)      pending -= Mathf.Min(pending, move);
                else if (pending < 0 && move < 0) pending -= Mathf.Max(pending, move);
                _lastAuth[index] = auth;
            }

            // Snap-back guard: never show a value outside [0, max] if the host can't satisfy a request.
            int max = net.GetMaxSchools(index);
            int hi = max > 0 ? max : int.MaxValue;
            int disp = auth + pending;
            if (disp > hi)      pending = hi - auth;
            else if (disp < 0)  pending = -auth;

            if (pending == 0)
            {
                _pendingDelta.Remove(index);
                _lastAuth.Remove(index);
            }
            else
            {
                _pendingDelta[index] = pending;
            }
        }
    }

    private void RefreshButtons()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (_index < 0 || net == null)
        {
            if (addButton != null)    addButton.interactable    = false;
            if (removeButton != null) removeButton.interactable = false;
            if (populationLabel != null) populationLabel.text = _index < 0 ? "" : "0";
            return;
        }

        int disp = DisplayCount(_index);
        int max = net.GetMaxSchools(_index);
        if (addButton != null)    addButton.interactable    = !(max > 0 && disp >= max); // off at cap
        if (removeButton != null) removeButton.interactable = disp > 0;                   // off at zero
        if (populationLabel != null) populationLabel.text = disp.ToString();
    }
}
