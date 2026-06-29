using UnityEngine;
using UnityEngine.UI;

public class ModalController : MonoBehaviour
{
    public static ModalController Instance;

    [Header("Dim Overlay")]
    public CanvasGroup DimOverlay;
    public float dimFadeDuration = 0.25f;

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

    public void Open(Sprite card) => Open(card, -1);

    // speciesIndex is ignored — add/remove/population now live on the info screen
    // (SpeciesInfoPanel + TabletAddRemoveUIGPU). Kept so existing call sites still compile.
    public void Open(Sprite card, int speciesIndex)
    {
        if (img != null) img.sprite = card;

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
