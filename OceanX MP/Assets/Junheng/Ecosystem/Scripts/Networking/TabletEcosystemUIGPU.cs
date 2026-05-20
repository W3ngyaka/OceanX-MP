using System.Collections.Generic;
using UnityEngine;
using OceanX.BoidsGPU.Ecosystem;

// GPU equivalent of TabletEcosystemUI.
// Reads EcosystemDefinitionGPU and spawns a TabletSpeciesCardUIGPU per species.
public class TabletEcosystemUIGPU : MonoBehaviour
{
    [Header("References")]
    public EcosystemDefinitionGPU      Ecosystem;
    public TabletSpeciesCardUIGPU      CardPrefab;
    public Transform                   CardContainer;

    private readonly List<TabletSpeciesCardUIGPU> _cards = new List<TabletSpeciesCardUIGPU>();

    private void Start()
    {
        if (Ecosystem == null || CardPrefab == null || CardContainer == null)
        {
            Debug.LogError("[TabletEcosystemUIGPU] Missing references.", this);
            return;
        }
        BuildCards();
    }

    private void BuildCards()
    {
        for (int i = 0; i < Ecosystem.Species.Count; i++)
        {
            SpeciesDataGPU species = Ecosystem.Species[i];
            if (species == null) continue;
            TabletSpeciesCardUIGPU card = Instantiate(CardPrefab, CardContainer);
            card.Initialize(species, i);
            _cards.Add(card);
        }
    }
}
