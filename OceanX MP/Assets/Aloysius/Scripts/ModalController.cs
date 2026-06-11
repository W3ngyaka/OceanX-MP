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
        int max = EcosystemNetworkManagerGPU.Instance.GetMaxSchools(_speciesIndex);

        if (PopulationLabel != null) PopulationLabel.text = pop.ToString();
        // Remove greys out at 0 (extinction); Add greys out once the cap is reached.
        // max <= 0 means the cap hasn't synced yet — leave Add enabled (the host enforces the cap).
        if (RemoveButton    != null) RemoveButton.interactable = pop > 0;
        if (AddButton       != null) AddButton.interactable    = !(max > 0 && pop >= max);
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
        // Compute the same cap/floor interactable state Update() uses, so a species already
        // at its cap (or at 0) doesn't show an enabled button for one frame before Update corrects it.
        bool hasTarget = speciesIndex >= 0;
        int pop = 0, max = 0;
        if (hasTarget && EcosystemNetworkManagerGPU.Instance != null)
        {
            pop = EcosystemNetworkManagerGPU.Instance.GetPopulation(speciesIndex);
            max = EcosystemNetworkManagerGPU.Instance.GetMaxSchools(speciesIndex);
        }
        if (AddButton    != null) AddButton.interactable    = hasTarget && !(max > 0 && pop >= max);
        if (RemoveButton != null) RemoveButton.interactable = hasTarget && pop > 0;

        // Population is per-species: show it only for a real target, and set it
        // right away so we never show the previously-opened card's number.
        if (PopulationLabel != null)
        {
            PopulationLabel.gameObject.SetActive(hasTarget);
            if (hasTarget) PopulationLabel.text = pop.ToString();
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
