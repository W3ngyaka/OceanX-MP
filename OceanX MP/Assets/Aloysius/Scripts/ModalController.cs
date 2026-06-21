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

    [Header("Dim Overlay")]
    public CanvasGroup DimOverlay;
    public float dimFadeDuration = 0.25f;

    private Image img;
    private DimFader dimFader;
    private int _speciesIndex = -1;

    void Awake()
    {
        Instance = this;
        img = GetComponent<Image>();

        if (AddButton    != null) AddButton.onClick.AddListener(OnAdd);
        if (RemoveButton != null) RemoveButton.onClick.AddListener(OnRemove);

        if (DimOverlay != null)
        {
            dimFader = DimOverlay.GetComponent<DimFader>();
            if (dimFader == null) dimFader = DimOverlay.gameObject.AddComponent<DimFader>();
        }
    }

    void Start()
    {
        gameObject.SetActive(false);

        if (DimOverlay != null)
        {
            DimOverlay.alpha = 0f;
            DimOverlay.blocksRaycasts = false;
            DimOverlay.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (_speciesIndex < 0 || EcosystemNetworkManagerGPU.Instance == null) return;

        int pop = EcosystemNetworkManagerGPU.Instance.GetPopulation(_speciesIndex);
        int max = EcosystemNetworkManagerGPU.Instance.GetMaxSchools(_speciesIndex);

        if (PopulationLabel != null) PopulationLabel.text = pop.ToString();
        if (RemoveButton    != null) RemoveButton.interactable = pop > 0;
        if (AddButton       != null) AddButton.interactable    = !(max > 0 && pop >= max);
    }

    public void Open(Sprite card) => Open(card, -1);

    public void Open(Sprite card, int speciesIndex)
    {
        if (img != null) img.sprite = card;
        _speciesIndex = speciesIndex;

        bool hasTarget = speciesIndex >= 0;
        int pop = 0, max = 0;
        if (hasTarget && EcosystemNetworkManagerGPU.Instance != null)
        {
            pop = EcosystemNetworkManagerGPU.Instance.GetPopulation(speciesIndex);
            max = EcosystemNetworkManagerGPU.Instance.GetMaxSchools(speciesIndex);
        }
        if (AddButton    != null) AddButton.interactable    = hasTarget && !(max > 0 && pop >= max);
        if (RemoveButton != null) RemoveButton.interactable = hasTarget && pop > 0;

        if (PopulationLabel != null)
        {
            PopulationLabel.gameObject.SetActive(hasTarget);
            if (hasTarget) PopulationLabel.text = pop.ToString();
        }

        gameObject.SetActive(true);

        if (DimOverlay != null && dimFader != null)
        {
            DimOverlay.gameObject.SetActive(true);
            DimOverlay.blocksRaycasts = true;
            dimFader.FadeTo(1f, dimFadeDuration);
        }
    }

    public void Close()
    {
        if (DimOverlay != null && dimFader != null)
        {
            DimOverlay.blocksRaycasts = false;
            dimFader.FadeTo(0f, dimFadeDuration, () =>
            {
                DimOverlay.gameObject.SetActive(false);
            });
        }

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
