using UnityEngine;
using System.Collections.Generic;

public class CurrentOrganismsGrid : MonoBehaviour
{
    [Header("References")]
    public Transform gridContent;       // parent with Grid Layout Group
    public GameObject organismCardPrefab;
    public List<SpeciesBubble> allBubbles = new List<SpeciesBubble>(); // drag all 12 bubbles here

    private List<GameObject> spawnedCards = new List<GameObject>();

    // Call this every time the popup opens
    public void Refresh()
    {
        // clear old cards
        foreach (var card in spawnedCards)
            if (card != null) Destroy(card);
        spawnedCards.Clear();

        foreach (var bubble in allBubbles)
        {
            if (bubble == null || bubble.data == null) continue;
            if (bubble.data.gpuSpecies == null) continue;
            if (TabletEcosystemUIGPU.Instance == null) continue;

            int index = TabletEcosystemUIGPU.Instance.GetSpeciesIndex(bubble.data.gpuSpecies);
            if (index < 0) continue;

            int pop = EcosystemNetworkManagerGPU.Instance != null
                ? EcosystemNetworkManagerGPU.Instance.GetPopulation(index)
                : 0;

            if (pop <= 0) continue; // only show species actually added

            var cardGO = Instantiate(organismCardPrefab, gridContent);
            var cardData = cardGO.GetComponent<OrganismCardData>();
            if (cardData != null)
                cardData.Setup(bubble.cardImage, bubble.data.speciesName, pop, index);

            spawnedCards.Add(cardGO);
        }
    }
}
