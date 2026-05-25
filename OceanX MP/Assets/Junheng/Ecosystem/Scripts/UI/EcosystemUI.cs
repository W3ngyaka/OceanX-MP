using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Main ecosystem UI panel.
// Attach to a Canvas GameObject in the scene.
// On Start it reads the EcosystemDefinition and spawns one SpeciesCardUI per species.
//
// Minimum scene setup:
//   Canvas
//     └─ EcosystemUI (this script)
//           ├─ CardContainer  (Vertical/Grid Layout Group)
//           └─ (optional) EcoHealthBar, EcoHealthLabel
//
// Assign CardPrefab — a prefab with SpeciesCardUI + the Button/Text/Image children.
public class EcosystemUI : MonoBehaviour
{
    [Header("Simulation reference")]
    [Tooltip("If left empty, auto-found in the scene.")]
    public EcosystemSimulation Simulation;

    [Header("Card prefab & container")]
    [Tooltip("Prefab with a SpeciesCardUI component on its root.")]
    public SpeciesCardUI CardPrefab;

    [Tooltip("Parent transform that holds the spawned cards (use a Layout Group).")]
    public Transform CardContainer;

    [Header("Eco-Health display (optional)")]
    [Tooltip("Slider or Image fill that shows ecosystem health 0-1.")]
    public Slider    EcoHealthBar;
    [Tooltip("Label showing the health value as a percentage.")]
    public TMP_Text  EcoHealthLabel;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private readonly List<SpeciesCardUI> _cards = new List<SpeciesCardUI>();

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        // Auto-find simulation if not assigned
        if (Simulation == null)
            Simulation = FindFirstObjectByType<EcosystemSimulation>();

        if (Simulation == null)
        {
            Debug.LogError("[EcosystemUI] No EcosystemSimulation found in the scene.", this);
            enabled = false;
            return;
        }

        if (CardPrefab == null)
        {
            Debug.LogError("[EcosystemUI] CardPrefab not assigned.", this);
            enabled = false;
            return;
        }

        if (CardContainer == null)
            CardContainer = transform;

        BuildCards();
    }

    private void Update()
    {
        UpdateEcoHealth();
    }

    // -------------------------------------------------------------------------
    // Card construction
    // -------------------------------------------------------------------------

    private void BuildCards()
    {
        // Clear any existing cards (e.g. editor preview)
        foreach (SpeciesCardUI existing in _cards)
            if (existing != null) Destroy(existing.gameObject);
        _cards.Clear();

        if (Simulation.Ecosystem == null) return;

        foreach (SpeciesDefinition species in Simulation.Ecosystem.Species)
        {
            if (species == null) continue;

            SpeciesCardUI card = Instantiate(CardPrefab, CardContainer);
            card.name = $"Card_{species.SpeciesName}";
            card.Initialize(species, Simulation);
            _cards.Add(card);
        }
    }

    // -------------------------------------------------------------------------
    // Eco-health bar
    // -------------------------------------------------------------------------

    // Simple health score: average of (currentPop / carryingCapacity) per species,
    // clamped to [0, 1]. Replace with your own formula once the health system is built.
    private void UpdateEcoHealth()
    {
        if (Simulation == null || Simulation.Ecosystem == null) return;
        if (EcoHealthBar == null && EcoHealthLabel == null)      return;

        float total  = 0f;
        int   count  = 0;

        foreach (SpeciesDefinition species in Simulation.Ecosystem.Species)
        {
            if (species == null || species.CarryingCapacity <= 0) continue;
            float ratio = Mathf.Clamp01((float)Simulation.CountLiving(species) / species.CarryingCapacity);
            total += ratio;
            count++;
        }

        float health = count == 0 ? 0f : total / count;

        if (EcoHealthBar   != null) EcoHealthBar.value    = health;
        if (EcoHealthLabel != null) EcoHealthLabel.text   = $"{Mathf.RoundToInt(health * 100f)}%";
    }
}
