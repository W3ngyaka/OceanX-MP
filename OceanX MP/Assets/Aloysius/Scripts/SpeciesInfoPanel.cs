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

    // current selection (for the View Details button)
    private Sprite _cardSprite;
    private int _speciesIndex = -1;

    void Awake()
    {
        Instance = this;
        if (viewDetailsButton != null)
            viewDetailsButton.onClick.AddListener(OpenModal);
        Clear();
    }

    public void Show(SpeciesData data, Sprite cardSprite, int speciesIndex)
    {
        if (data == null) { Clear(); return; }

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
    }

    // Locked/mystery display: tapped a locked bubble. Hides real data behind '???'.
    public void ShowLocked(SpeciesData data)
    {
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
        if (emptyState != null) emptyState.SetActive(true);
        if (detailRoot != null) detailRoot.SetActive(false);
        _cardSprite = null;
        _speciesIndex = -1;
    }

    void OpenModal()
    {
        if (ModalController.Instance == null || _cardSprite == null) return;
        ModalController.Instance.Open(_cardSprite, _speciesIndex);
    }
}
