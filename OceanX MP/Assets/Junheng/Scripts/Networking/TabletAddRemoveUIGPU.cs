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
/// </summary>
public class TabletAddRemoveUIGPU : MonoBehaviour
{
    public static TabletAddRemoveUIGPU Instance { get; private set; }

    [Header("Buttons (on the Info screen)")]
    public Button addButton;
    public Button removeButton;

    [Tooltip("Optional: shows the selected species' live school count on the info screen, " +
             "next to +/- so the player sees feedback at the point of adding.")]
    public TMP_Text populationLabel;

    // Netcode index of the currently-selected species, or -1 when nothing is selected.
    private int _index = -1;

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
        if (_index >= 0) EcosystemNetworkManagerGPU.Instance?.RequestAddSpeciesRpc(_index);
    }

    private void OnRemove()
    {
        if (_index >= 0) EcosystemNetworkManagerGPU.Instance?.RequestRemoveSpeciesRpc(_index);
    }

    private void Update()
    {
        // Keep the buttons greyed in step with the live (synced) population.
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

        int pop = net.GetPopulation(_index);
        int max = net.GetMaxSchools(_index);
        if (addButton != null)    addButton.interactable    = !(max > 0 && pop >= max); // off at cap
        if (removeButton != null) removeButton.interactable = pop > 0;                  // off at zero
        if (populationLabel != null) populationLabel.text = pop.ToString();
    }
}
