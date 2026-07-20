using UnityEngine;
using System.Collections.Generic;

public class CurrentOrganismsGrid : MonoBehaviour
{
    [Header("References")]
    public Transform gridContent;       
    public GameObject organismCardPrefab;
    [Tooltip("Shown when no species are in the ecosystem (e.g. 'NO ORGANISMS IN ECOSYSTEM').")]
    public GameObject emptyState;   
    public List<SpeciesBubble> allBubbles = new List<SpeciesBubble>(); 

    private List<GameObject> spawnedCards = new List<GameObject>();

    public void Refresh()
    {
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

            // Use the optimistic count (host count + not-yet-confirmed taps) so a species the
            // player just removed to zero doesn't get a card recreated on panel switch while its
            // fish are still swimming out, and an optimistically-added one shows up right away.
            int pop = EcosystemNetworkManagerGPU.Instance != null
                ? OptimisticPopulationStore.Display(index)
                : 0;

            if (pop <= 0) continue; // only show species actually added

            // Card icon comes from SpeciesContent.csv's 'unlockImageFile' column (online-
            // editable, warmed on Android) — DELIBERATELY a different column from the modal,
            // which uses 'imageFile'. Falls back to the bubble's inspector cardImage when the
            // sheet has no unlock image for this species, so a card never goes blank.
            Sprite icon = bubble.cardImage;
            string contentKey = !string.IsNullOrEmpty(bubble.data.contentId)
                ? bubble.data.contentId : bubble.data.speciesName;
            var content = SpeciesContentDB.Get(contentKey);
            if (content != null)
            {
                Sprite csvIcon = SpeciesContentDB.GetImage(content.unlockImageFile);
                if (csvIcon != null) icon = csvIcon;
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
