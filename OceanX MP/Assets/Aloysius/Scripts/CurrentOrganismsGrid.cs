using UnityEngine;
using System.Collections.Generic;

public class CurrentOrganismsGrid : MonoBehaviour
{
    [Header("References")]
    public Transform gridContent;
    public GameObject organismCardPrefab;
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

            int pop = EcosystemNetworkManagerGPU.Instance != null
                ? OptimisticPopulationStore.Display(index)
                : 0;

            if (pop <= 0) continue;

            Sprite icon = bubble.cardImage;
            string contentKey = !string.IsNullOrEmpty(bubble.data.contentId)
                ? bubble.data.contentId : bubble.data.speciesName;
            var content = SpeciesContentDB.Get(contentKey);
            if (content != null)
            {
                Sprite csvIcon = SpeciesContentDB.GetImage(content.revealImageFile)
                                 ?? SpeciesContentDB.GetImage(content.imageFile);
                if (csvIcon != null) icon = csvIcon;
            }

            var cardGO = Instantiate(organismCardPrefab, gridContent);
            var cardData = cardGO.GetComponent<OrganismCardData>();
            if (cardData != null)
                cardData.Setup(icon, bubble.data.speciesName, pop, index);

            spawnedCards.Add(cardGO);
        }

        if (emptyState == null)
        {
            var t = transform.Find("EmptyState");
            if (t != null) emptyState = t.gameObject;
        }
        if (emptyState != null) emptyState.SetActive(spawnedCards.Count == 0);
    }
}
