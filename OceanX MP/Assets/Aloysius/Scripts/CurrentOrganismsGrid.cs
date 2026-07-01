using UnityEngine;
using System.Collections.Generic;

public class CurrentOrganismsGrid : MonoBehaviour
{
    [Header("References")]
    public Transform gridContent;       // parent with Grid Layout Group
    public GameObject organismCardPrefab;
    [Tooltip("Shown when no species are in the ecosystem (e.g. 'NO ORGANISMS IN ECOSYSTEM').")]
    public GameObject emptyState;   // auto-found by name if left null
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

            // The bubble's own Image is just the bubble shape/background.
            // The actual fish photo lives on a nested child Image (e.g. "Sharkimage").
            // Find the first child Image (excluding the bubble's own) that isn't a
            // lock/overpop/glow overlay.
            Sprite icon = bubble.cardImage; // fallback
            var ownImage = bubble.GetComponent<UnityEngine.UI.Image>();
            var childImages = bubble.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            foreach (var childImg in childImages)
            {
                if (childImg == ownImage) continue;
                if (childImg.sprite == null) continue;
                string n = childImg.gameObject.name.ToLower();
                if (n.Contains("lock") || n.Contains("overpop") || n.Contains("glow") || n.Contains("ring")) continue;
                icon = childImg.sprite;
                break;
            }

            var cardGO = Instantiate(organismCardPrefab, gridContent);
            var cardData = cardGO.GetComponent<OrganismCardData>();
            if (cardData != null)
                cardData.Setup(icon, bubble.data.speciesName, pop, index);

            spawnedCards.Add(cardGO);
        }

        // Empty state: show message when nothing is in the ecosystem.
        if (emptyState == null)
        {
            var t = transform.Find("EmptyState");
            if (t != null) emptyState = t.gameObject;
        }
        if (emptyState != null) emptyState.SetActive(spawnedCards.Count == 0);
    }
}
