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

    [Header("Spam guard")]
    [Tooltip("Minimum seconds between Add/Remove presses. Blocks accidental double-taps and " +
             "mashing without hurting deliberate tapping. 0 = off.")]
    public float addCooldown = 0.3f;
    private float _lastAddTime = -999f;

    [Header("Buttons (on the Info screen)")]
    public Button addButton;
    public Button removeButton;

    [Tooltip("Optional: shows the selected species' school count on the info screen, " +
             "next to +/- so the player sees feedback at the point of adding.")]
    public TMP_Text populationLabel;

    // Netcode index of the currently-selected species, or -1 when nothing is selected.
    private int _index = -1;
    // The selected species, kept so Add can be gated on its unlock state.
    private SpeciesDataGPU _selected;

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
        _selected = species;
        _index = TabletEcosystemUIGPU.Instance != null
            ? TabletEcosystemUIGPU.Instance.GetSpeciesIndex(species)
            : -1;
        RefreshButtons();
    }

    // A locked (not-yet-discovered) species can't be added. Permitted when no unlock manager exists.
    private bool SelectedUnlocked()
    {
        var mgr = EcosystemUnlockManagerGPU.Instance;
        return mgr == null || mgr.IsUnlocked(_selected);
    }

    private void OnAdd()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (_index < 0 || net == null) return;
        if (!SelectedUnlocked()) return; // locked species can't be added until discovered
        if (Time.unscaledTime - _lastAddTime < addCooldown) return; // spam cooldown
        _lastAddTime = Time.unscaledTime;
        int max = net.GetMaxSchools(_index);
        if (max > 0 && OptimisticPopulationStore.Display(_index) >= max) return; // already at cap optimistically

        OptimisticPopulationStore.RegisterDelta(_index, +1);
        net.RequestAddSpeciesRpc(_index);
        RefreshButtons();
    }

    private void OnRemove()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (_index < 0 || net == null) return;
        if (OptimisticPopulationStore.Display(_index) <= 0) return; // already at zero optimistically
        if (Time.unscaledTime - _lastAddTime < addCooldown) return; // spam cooldown (shared with Add)
        _lastAddTime = Time.unscaledTime;

        OptimisticPopulationStore.RegisterDelta(_index, -1);
        net.RequestRemoveSpeciesRpc(_index);
        RefreshButtons();
    }

    private void Update()
    {
        // The shared store reconciles pending intent onto the host's real count as Display() is read.
        RefreshButtons();
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

        int disp = OptimisticPopulationStore.Display(_index);
        int max = net.GetMaxSchools(_index);
        if (addButton != null)    addButton.interactable    = !(max > 0 && disp >= max) && SelectedUnlocked(); // off at cap or while locked
        if (removeButton != null) removeButton.interactable = disp > 0;                   // off at zero
        if (populationLabel != null) populationLabel.text = disp.ToString();
    }
}
