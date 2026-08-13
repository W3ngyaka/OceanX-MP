using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ModalController : MonoBehaviour
{

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

            DimOverlay.alpha = 0f;
            DimOverlay.blocksRaycasts = false;
            DimOverlay.gameObject.SetActive(false);
        }
    }

    public void Open(SpeciesData data)
    {
        Populate(data);
        Show();
    }

    public void Open(Sprite card) => Open(card, -1);
    public void Open(Sprite card, int speciesIndex)
    {

        ViewedSpeciesReporter.Report(speciesIndex);
        if (photo != null && card != null) { photo.sprite = card; photo.enabled = true; photo.preserveAspect = true; }
        Show();
    }

    void Populate(SpeciesData data)
    {

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
            if (col.HasValue) iucnStatusText.color = col.Value;
        }
        SetText(descriptionText, e?.description);

        Sprite pic = e != null ? SpeciesContentDB.GetImage(e.imageFile) : null;
        if (photo != null)
        {
            photo.sprite = pic;
            photo.enabled = pic != null;
            photo.preserveAspect = true;
        }

        Sprite iucnPic = e != null ? SpeciesContentDB.GetImage(e.iucnImage) : null;
        if (IUCNPhoto != null)
        {
            IUCNPhoto.sprite = iucnPic;
            IUCNPhoto.enabled = iucnPic != null;
            IUCNPhoto.preserveAspect = true;
        }
    }

    static void SetText(TMP_Text t, string val)
    {
        if (t != null) t.text = val ?? "";
    }

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
        bool wasClosed = !gameObject.activeSelf;
        gameObject.SetActive(true);
        if (wasClosed && UISoundManager.Instance != null)
            UISoundManager.Instance.Play(UISound.ModalOpen);

        if (DimOverlay != null && dimFader != null)
        {
            DimOverlay.gameObject.SetActive(true);
            DimOverlay.blocksRaycasts = true;
            dimFader.FadeTo(1f, dimFadeDuration);
        }
    }

    public void Close()
    {

        if (gameObject.activeSelf && UISoundManager.Instance != null)
            UISoundManager.Instance.Play(UISound.ModalClose);

        ViewedSpeciesReporter.Clear();

        ContextNudge.Advance("details");

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
