using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ModalController : MonoBehaviour
{
    public static ModalController Instance;

    [Header("Netcode controls (optional — leave unset for a cosmetic-only card)")]
    public Button   AddButton;
    public Button   RemoveButton;
    public TMP_Text PopulationLabel;

    private Image img;

    // Species this modal currently controls; -1 = cosmetic only (no Add/Remove).
    private int _speciesIndex = -1;

    void Awake()
    {
        Instance = this;
        img = GetComponent<Image>();

        if (AddButton    != null) AddButton.onClick.AddListener(OnAdd);
        if (RemoveButton != null) RemoveButton.onClick.AddListener(OnRemove);
    }

    void Start()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (_speciesIndex < 0 || EcosystemNetworkManagerGPU.Instance == null) return;

        int pop = EcosystemNetworkManagerGPU.Instance.GetPopulation(_speciesIndex);

        if (PopulationLabel != null) PopulationLabel.text = pop.ToString();
        if (RemoveButton    != null) RemoveButton.interactable = pop > 0;
    }

    // Cosmetic-only card (no netcode target).
    public void Open(Sprite card) => Open(card, -1);

    // Netcode-aware: the Add/Remove buttons drive this species via the host.
    public void Open(Sprite card, int speciesIndex)
    {
        if (img != null) img.sprite = card;
        _speciesIndex = speciesIndex;

        // Buttons stay visible; they're just disabled when this bubble has no
        // netcode target (cosmetic-only, or species not found in the ecosystem list).
        bool hasTarget = speciesIndex >= 0;
        if (AddButton    != null) AddButton.interactable    = hasTarget;
        if (RemoveButton != null) RemoveButton.interactable = hasTarget;

        // Population is per-species: show it only for a real target, and set it
        // right away so we never show the previously-opened card's number.
        if (PopulationLabel != null)
        {
            PopulationLabel.gameObject.SetActive(hasTarget);
            if (hasTarget && EcosystemNetworkManagerGPU.Instance != null)
                PopulationLabel.text = EcosystemNetworkManagerGPU.Instance.GetPopulation(speciesIndex).ToString();
        }

        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void OnAdd()
    {
        if (_speciesIndex < 0) return;
        EcosystemNetworkManagerGPU.Instance?.RequestAddSpeciesRpc(_speciesIndex);
    }

    private void OnRemove()
    {
        if (_speciesIndex < 0) return;
        EcosystemNetworkManagerGPU.Instance?.RequestRemoveSpeciesRpc(_speciesIndex);
    }
}
