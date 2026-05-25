using System.Collections.Generic;
using UnityEngine;

// Tablet-side UI manager.
// Reads the same EcosystemDefinition ScriptableObject to know which species exist,
// then spawns one TabletSpeciesCardUI per species.
// No EcosystemSimulation needed in the tablet scene.
//
// Minimum scene setup (tablet):
//   NetworkManager (with UnityTransport + NetworkBootstrap role=Client)
//   Canvas
//     └─ TabletEcosystemUI (this script)
//           └─ CardContainer  (Vertical/Grid Layout Group)
public class TabletEcosystemUI : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("The same EcosystemDefinition asset used on the trifold.")]
    public EcosystemDefinition Ecosystem;

    [Header("Card prefab & container")]
    [Tooltip("Prefab with a TabletSpeciesCardUI component on its root.")]
    public TabletSpeciesCardUI CardPrefab;

    [Tooltip("Parent transform that holds the spawned cards.")]
    public Transform CardContainer;

    private readonly List<TabletSpeciesCardUI> _cards = new();

    private void Start()
    {
        if (Ecosystem == null)
        {
            Debug.LogError("[TabletEcosystemUI] No EcosystemDefinition assigned.", this);
            return;
        }

        if (CardPrefab == null)
        {
            Debug.LogError("[TabletEcosystemUI] CardPrefab not assigned.", this);
            return;
        }

        if (CardContainer == null)
            CardContainer = transform;

        BuildCards();
    }

    private void BuildCards()
    {
        for (int i = 0; i < Ecosystem.Species.Count; i++)
        {
            SpeciesDefinition species = Ecosystem.Species[i];
            if (species == null) continue;

            TabletSpeciesCardUI card = Instantiate(CardPrefab, CardContainer);
            card.name = $"Card_{species.SpeciesName}";
            card.Initialize(species, i);
            _cards.Add(card);
        }
    }
}
