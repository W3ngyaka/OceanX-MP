using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OceanX.BoidsGPU.Ecosystem;

// GPU equivalent of TabletSpeciesCardUI.
// Sends RPCs through EcosystemNetworkManagerGPU and reads the synced population count.
public class TabletSpeciesCardUIGPU : MonoBehaviour
{
    [Header("References")]
    public Image    SpeciesIcon;
    public TMP_Text SpeciesNameLabel;
    public TMP_Text PopulationLabel;
    public Button   AddButton;
    public Button   RemoveButton;

    private int _speciesIndex;

    public void Initialize(SpeciesDataGPU species, int speciesIndex)
    {
        _speciesIndex = speciesIndex;

        if (SpeciesNameLabel != null)
            SpeciesNameLabel.text = species.SpeciesName;

        if (AddButton    != null) AddButton.onClick.AddListener(OnAdd);
        if (RemoveButton != null) RemoveButton.onClick.AddListener(OnRemove);
    }

    private void Update()
    {
        if (EcosystemNetworkManagerGPU.Instance == null) return;

        int pop = EcosystemNetworkManagerGPU.Instance.GetPopulation(_speciesIndex);

        if (PopulationLabel != null)
            PopulationLabel.text = pop.ToString();

        if (RemoveButton != null)
            RemoveButton.interactable = pop > 0;
    }

    private void OnAdd()    => EcosystemNetworkManagerGPU.Instance?.RequestAddSpeciesRpc(_speciesIndex);
    private void OnRemove() => EcosystemNetworkManagerGPU.Instance?.RequestRemoveSpeciesRpc(_speciesIndex);
}
