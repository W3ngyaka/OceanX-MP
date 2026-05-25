using UnityEngine;
using UnityEngine.UI;
using TMPro;

// One species card in the ecosystem UI panel.
// Drag a SpeciesCardUI prefab (or build one in the scene) and assign the references.
// EcosystemUI spawns one of these per species and calls Initialize().
public class SpeciesCardUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Displays the species icon/sprite.")]
    public Image         SpeciesIcon;

    [Tooltip("Displays the species name.")]
    public TMP_Text      SpeciesNameLabel;

    [Tooltip("Displays current live population count.")]
    public TMP_Text      PopulationLabel;

    [Tooltip("Button that adds one fish of this species.")]
    public Button        AddButton;

    [Tooltip("Button that removes one fish of this species.")]
    public Button        RemoveButton;

    [Tooltip("How many fish to add/remove per button press.")]
    public int           StepSize = 1;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private SpeciesDefinition  _species;
    private EcosystemSimulation _sim;

    // -------------------------------------------------------------------------
    // Public setup — called by EcosystemUI
    // -------------------------------------------------------------------------

    public void Initialize(SpeciesDefinition species, EcosystemSimulation sim)
    {
        _species = species;
        _sim     = sim;

        // Labels
        if (SpeciesNameLabel != null)
            SpeciesNameLabel.text = species.SpeciesName;

        // Icon — expects a SpriteRenderer or Sprite field on SpeciesDefinition.
        // If your SpeciesDefinition has a Sprite field, assign it here.
        // if (SpeciesIcon != null && species.Icon != null)
        //     SpeciesIcon.sprite = species.Icon;

        // Wire buttons
        if (AddButton    != null) AddButton.onClick.AddListener(OnAdd);
        if (RemoveButton != null) RemoveButton.onClick.AddListener(OnRemove);

        RefreshPopulationLabel();
    }

    // -------------------------------------------------------------------------
    // Per-frame refresh
    // -------------------------------------------------------------------------

    private void Update()
    {
        RefreshPopulationLabel();

        // Grey out Remove when no fish are alive
        if (RemoveButton != null)
            RemoveButton.interactable = _sim != null && _sim.CountLiving(_species) > 0;
    }

    // -------------------------------------------------------------------------
    // Button callbacks
    // -------------------------------------------------------------------------

    private void OnAdd()
    {
        if (_sim == null || _species == null) return;
        _sim.AddSpecies(_species, StepSize);
    }

    private void OnRemove()
    {
        if (_sim == null || _species == null) return;
        _sim.RemoveSpecies(_species, StepSize);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void RefreshPopulationLabel()
    {
        if (PopulationLabel == null || _sim == null || _species == null) return;
        PopulationLabel.text = _sim.CountLiving(_species).ToString();
    }
}
