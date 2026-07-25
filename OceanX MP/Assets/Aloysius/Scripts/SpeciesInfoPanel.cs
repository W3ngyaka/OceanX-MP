using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Right-side species detail panel. A bubble tap calls Show(...) to fill it in;
// the "View Details" button opens the full ModalController for that species.
// Shows an empty state when nothing is selected.
public class SpeciesInfoPanel : MonoBehaviour
{
    public static SpeciesInfoPanel Instance;

    [Header("Empty state")]
    [Tooltip("Shown when no organism is selected (e.g. the 'No Organism Selected' text).")]
    public GameObject emptyState;

    [Header("Detail elements (shown when a species is selected)")]
    public GameObject detailRoot;     // container for name/badge/desc/button
    public Image speciesImage;        // optional, leave null for text-first
    public TMP_Text nameText;
    public TMP_Text badgeText;        // role / trophic tier
    public TMP_Text descriptionText;
    public Button viewDetailsButton;

    [Header("Locked (mystery) display")]
    public string lockedName = "???";
    public string lockedBadge = "???";
    [TextArea] public string lockedDescription = "This species hasn't been discovered yet. Build the ecosystem to reveal it.";

    [Header("Hint fallback")]
    [Tooltip("If a species is selected but View Details is never opened, release the hold-for-food-web hint after this many seconds. 0 = require a real details open+close.")]
    public float detailsHintFallbackSeconds = 8f;

    // current selection (for the View Details button)
    private SpeciesData _data;
    private int _index = -1;   // sim species index (for the large-screen bystander mirror)
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

        // Always show the button when a species is selected; the click handles missing modal/card gracefully.
        if (viewDetailsButton != null)
            viewDetailsButton.gameObject.SetActive(true);

        EnsureHintFallback();   // start the grace period toward releasing the hold hint
    }

    // Locked/mystery display: tapped a locked bubble. Hides real data behind '???'.
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
        if (ModalController.Instance == null || _data == null) return;
        CancelHintFallback();                   // the real open+close path takes over from here
        ViewedSpeciesReporter.Report(_index);   // mirror to large screen for bystanders
        ModalController.Instance.Open(_data);   // data-driven: modal pulls text + image from the CSV
    }

    // Safety net for the onboarding chain. The hold-for-food-web hint is gated on a real
    // View Details open+close (ModalController.Close -> ContextNudge.Advance("details")), so a
    // visitor who selects a fish but never taps through would never discover the food web at
    // all. Once a species has been selected, release the gate after a grace period instead.
    // The clock starts on the FIRST selection and is not restarted by further bubble taps,
    // otherwise a visitor browsing bubbles would postpone the hint indefinitely.
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
