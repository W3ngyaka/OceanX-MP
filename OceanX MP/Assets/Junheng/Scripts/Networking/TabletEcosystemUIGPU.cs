using System.Collections.Generic;
using UnityEngine;
using OceanX.BoidsGPU.Ecosystem;

// GPU equivalent of TabletEcosystemUI.
// Reads EcosystemDefinitionGPU and spawns a TabletSpeciesCardUIGPU per species.
public class TabletEcosystemUIGPU : MonoBehaviour
{
    // Lets the new SpeciesBubble UI resolve a species -> netcode index without
    // each bubble needing its own ecosystem reference.
    public static TabletEcosystemUIGPU Instance { get; private set; }

    [Header("References")]
    public EcosystemDefinitionGPU      Ecosystem;

    [Header("Legacy card UI (optional)")]
    [Tooltip("Old auto-generated cards. Leave CardPrefab/CardContainer empty to disable and use the SpeciesBubble UI instead.")]
    public TabletSpeciesCardUIGPU      CardPrefab;
    public Transform                   CardContainer;

    private readonly List<TabletSpeciesCardUIGPU> _cards = new List<TabletSpeciesCardUIGPU>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (Ecosystem == null)
        {
            Debug.LogError("[TabletEcosystemUIGPU] Missing Ecosystem reference.", this);
            return;
        }

        // The old card UI is optional now. Only build it if both refs are assigned;
        // otherwise this component just serves as the ecosystem/index lookup.
        if (CardPrefab != null && CardContainer != null)
            BuildCards();
    }

    // Returns the index of a species in the ecosystem list, or -1 if not present.
    // This index is the contract used by every EcosystemNetworkManagerGPU RPC.
    public int GetSpeciesIndex(SpeciesDataGPU species)
    {
        if (Ecosystem == null || species == null) return -1;
        return Ecosystem.Species.IndexOf(species);
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
