using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Species detail modal. YOU design the panel (background, icons, static labels, layout);
// this just fills the value slots you drag in from the CSV (SpeciesContentDB). Any slot
// left unassigned is simply skipped, so you only wire the ones your design uses.
public class ModalController : MonoBehaviour
{
    // Resolve lazily (including inactive) so callers work even if the modal starts inactive
    // or gets deactivated before its Awake runs.
    static ModalController _instance;
    public static ModalController Instance
    {
        get
        {
            if (_instance == null) _instance = FindAnyObjectByType<ModalController>(FindObjectsInactive.Include);
            return _instance;
        }
        private set { _instance = value; }
    }

    [Header("Dim Overlay")]
    public CanvasGroup DimOverlay;
    public float dimFadeDuration = 0.25f;

    
    public Image photo;
    public Image IUCNPhoto;
    public TMP_Text titleText;
    public TMP_Text sciNameText;
    public TMP_Text dietText;
    public TMP_Text iucnStatusText;
    public TMP_Text descriptionText;

    private Image img;                  
    private DimFader dimFader;

    void Awake()
    {
        Instance = this;
        img = GetComponent<Image>();

        if (DimOverlay != null)
        {
            dimFader = DimOverlay.GetComponent<DimFader>();
            if (dimFader == null) dimFader = DimOverlay.gameObject.AddComponent<DimFader>();
        }
    }

    // Panel is authored inactive in the scene, so Start() is deferred to the first Open().
    // Previously Start() called SetActive(false), which slammed the panel shut right after
    // that first Open (the 'first tap does nothing' bug). Removed; Hide() closes it instead.

    void Start()
    {
        if (DimOverlay != null)
        {
            DimOverlay.alpha = 0f;
            DimOverlay.blocksRaycasts = false;
            DimOverlay.gameObject.SetActive(false);
        }
    }

    // Primary, data-driven entry point.
    public void Open(SpeciesData data)
    {
        Populate(data);
        Show();
    }

    // Back-compat: legacy callers that only had a card sprite. Shows it in the photo slot.
    public void Open(Sprite card) => Open(card, -1);
    public void Open(Sprite card, int speciesIndex)
    {
        if (photo != null && card != null) { photo.sprite = card; photo.enabled = true; photo.preserveAspect = true; }
        Show();
    }

    void Populate(SpeciesData data)
    {
        // Prefer the stable contentId (rename/translation-safe); fall back to the display name.
        // SpeciesContentDB is indexed by both, so either resolves the same row.
        string lookupKey = data == null ? null
            : (!string.IsNullOrEmpty(data.contentId) ? data.contentId : data.speciesName);
        var e = lookupKey != null ? SpeciesContentDB.Get(lookupKey) : null;

        SetText(titleText, data != null ? data.speciesName : "");
        SetText(sciNameText, e != null && !string.IsNullOrWhiteSpace(e.sciName) ? e.sciName : (data != null ? data.sciName : ""));
        SetText(dietText, e?.diet);
        SetText(iucnStatusText, e?.iucnStatus);
        if (iucnStatusText != null)
        {
            var col = IucnColor(e?.iucnStatus);
            if (col.HasValue) iucnStatusText.color = col.Value;   // else keep its authored colour
        }
        SetText(descriptionText, e?.description);

        // Main species photo.
        Sprite pic = e != null ? SpeciesContentDB.GetImage(e.imageFile) : null;
        if (photo != null)
        {
            photo.sprite = pic;
            photo.enabled = pic != null;
            photo.preserveAspect = true;
        }

        // Extra image (e.g. IUCN badge) — its own 'iucnImage' CSV column + its own slot.
        Sprite iucnPic = e != null ? SpeciesContentDB.GetImage(e.iucnImage) : null;
        if (IUCNPhoto != null)
        {
            IUCNPhoto.sprite = iucnPic;
            IUCNPhoto.enabled = iucnPic != null;
            IUCNPhoto.preserveAspect = true;
        }
    }

    // Fill a value slot (no label prefix — your design supplies the labels).
    static void SetText(TMP_Text t, string val)
    {
        if (t != null) t.text = val ?? "";
    }

    // IUCN status -> label colour. Returns null to leave the text's own colour untouched
    // (so any status not listed here keeps whatever colour you set in the design).
    static Color? IucnColor(string status)
    {
        switch ((status ?? "").Trim().ToLowerInvariant())
        {
            case "least concern":   return Hex("008E6A");
            case "vulnerable":      return Hex("cc9900");
            case "near threatened": return Hex("006666");
            default: return null;
        }
    }

    static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }

    void Show()
    {
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
}
