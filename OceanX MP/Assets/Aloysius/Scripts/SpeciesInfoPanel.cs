using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpeciesInfoPanel : MonoBehaviour
{
    public static SpeciesInfoPanel Instance;

    [Header("Empty state")]
        public GameObject emptyState;

    [Header("Detail elements (shown when a species is selected)")]
    public GameObject detailRoot;
    public Image speciesImage;
    public TMP_Text nameText;
    public TMP_Text badgeText;
    public TMP_Text descriptionText;
    public Button viewDetailsButton;

    [Header("Locked (mystery) display")]
    public string lockedName = "???";
    public string lockedBadge = "???";
    [TextArea] public string lockedDescription = "This species hasn't been discovered yet. Build the ecosystem to reveal it.";

    [Header("Hint fallback")]
        public float detailsHintFallbackSeconds = 8f;

    private SpeciesData _data;
    private int _index = -1;
    private Sprite _cardSprite;
    private int _speciesIndex = -1;
    private Coroutine _hintFallback;

    void Awake()
    {
        Instance = this;
        if (viewDetailsButton != null)
            viewDetailsButton.onClick.AddListener(OpenModal);
        Clear();
    }

    public void Show(SpeciesData data, Sprite cardSprite, int speciesIndex)
    {
        _index = speciesIndex;
        if (data == null) { Clear(); return; }

        _data = data;
        _cardSprite = cardSprite;
        _speciesIndex = speciesIndex;

        if (emptyState != null) emptyState.SetActive(false);
        if (detailRoot != null) detailRoot.SetActive(true);

        if (nameText != null) nameText.text = data.speciesName;
        if (badgeText != null) badgeText.text = string.IsNullOrEmpty(data.tier) ? "" : data.tier.ToUpper();
        if (descriptionText != null) descriptionText.text = data.addedMessage;

        if (speciesImage != null)
        {
            speciesImage.sprite = cardSprite;
            speciesImage.enabled = (cardSprite != null);
        }

        if (viewDetailsButton != null)
            viewDetailsButton.gameObject.SetActive(true);

        EnsureHintFallback();
    }

    public void ShowLocked(SpeciesData data)
    {
        _index = -1;
        _data = null;
        _cardSprite = null;
        _speciesIndex = -1;
        if (emptyState != null) emptyState.SetActive(false);
        if (detailRoot != null) detailRoot.SetActive(true);
        if (nameText != null) nameText.text = lockedName;
        if (badgeText != null) badgeText.text = lockedBadge;
        if (descriptionText != null) descriptionText.text = lockedDescription;
        if (speciesImage != null) speciesImage.enabled = false;
        if (viewDetailsButton != null) viewDetailsButton.gameObject.SetActive(false);
    }

    public void Clear()
    {
        _index = -1;
        if (emptyState != null) emptyState.SetActive(true);
        if (detailRoot != null) detailRoot.SetActive(false);
        _data = null;
        _cardSprite = null;
        _speciesIndex = -1;
    }

    void OpenModal()
    {
        GuidedTutorial.NotifyViewDetails();
        if (ModalController.Instance == null || _data == null) return;
        CancelHintFallback();
        ViewedSpeciesReporter.Report(_index);
        ModalController.Instance.Open(_data);
    }

    void EnsureHintFallback()
    {
        if (_hintFallback != null) return;
        if (detailsHintFallbackSeconds > 0f && gameObject.activeInHierarchy)
            _hintFallback = StartCoroutine(DetailsHintFallback());
    }

    void CancelHintFallback()
    {
        if (_hintFallback != null) { StopCoroutine(_hintFallback); _hintFallback = null; }
    }

    System.Collections.IEnumerator DetailsHintFallback()
    {
        yield return new WaitForSecondsRealtime(detailsHintFallbackSeconds);
        _hintFallback = null;
        ContextNudge.Advance("details");
    }
}
