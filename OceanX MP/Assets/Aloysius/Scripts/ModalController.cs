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

    [Header("Content slots — drag YOUR text/image elements here")]
    [Tooltip("Species photo.")]                       public Image photo;
    [Tooltip("Species (common) name.")]               public TMP_Text titleText;
    [Tooltip("Scientific name value.")]               public TMP_Text sciNameText;
    [Tooltip("Diet value.")]                          public TMP_Text dietText;
    [Tooltip("IUCN status value, e.g. 'Least Concern'.")] public TMP_Text iucnStatusText;
    [Tooltip("Description body.")]                     public TMP_Text descriptionText;

    private Image img;                  // the panel background
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
        var e = data != null ? SpeciesContentDB.Get(data.speciesName) : null;

        SetText(titleText, data != null ? data.speciesName : "");
        SetText(sciNameText, e != null && !string.IsNullOrWhiteSpace(e.sciName) ? e.sciName : (data != null ? data.sciName : ""));
        SetText(dietText, e?.diet);
        SetText(iucnStatusText, e?.iucnStatus);
        SetText(descriptionText, e?.description);

        Sprite pic = e != null ? SpeciesContentDB.GetImage(e.imageFile) : null;
        if (photo != null)
        {
            photo.sprite = pic;
            photo.enabled = pic != null;
            photo.preserveAspect = true;
        }
    }

    // Fill a value slot (no label prefix — your design supplies the labels).
    static void SetText(TMP_Text t, string val)
    {
        if (t != null) t.text = val ?? "";
    }

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
