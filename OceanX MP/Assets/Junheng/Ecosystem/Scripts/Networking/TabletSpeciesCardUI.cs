using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Network-aware version of SpeciesCardUI for the tablet (client).
// Instead of calling EcosystemSimulation directly, it sends Server RPCs
// through EcosystemNetworkManager. Population counts are read from the
// synced NetworkList rather than the live simulation.
public class TabletSpeciesCardUI : MonoBehaviour
{
    [Header("References")]
    public Image    SpeciesIcon;
    public TMP_Text SpeciesNameLabel;
    public TMP_Text PopulationLabel;
    public Button   AddButton;
    public Button   RemoveButton;

    [Tooltip("How many fish to add/remove per button press.")]
    public int StepSize = 1;

    private int _speciesIndex;

    public void Initialize(SpeciesDefinition species, int speciesIndex)
    {
        _speciesIndex = speciesIndex;

        if (SpeciesNameLabel != null)
            SpeciesNameLabel.text = species.SpeciesName;

        if (AddButton    != null) AddButton.onClick.AddListener(OnAdd);
        if (RemoveButton != null) RemoveButton.onClick.AddListener(OnRemove);
    }

    private void Update()
    {
        if (EcosystemNetworkManager.Instance == null) return;

        int pop = EcosystemNetworkManager.Instance.GetPopulation(_speciesIndex);

        if (PopulationLabel != null)
            PopulationLabel.text = pop.ToString();

        if (RemoveButton != null)
            RemoveButton.interactable = pop > 0;
    }

    private void OnAdd()
    {
        EcosystemNetworkManager.Instance?.RequestAddSpeciesRpc(_speciesIndex, StepSize);
    }

    private void OnRemove()
    {
        EcosystemNetworkManager.Instance?.RequestRemoveSpeciesRpc(_speciesIndex, StepSize);
    }
}
