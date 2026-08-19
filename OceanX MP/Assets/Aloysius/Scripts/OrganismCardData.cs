using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OceanX.BoidsGPU.Ecosystem;   // SpeciesStatus (BALANCE DELTA)

public class OrganismCardData : MonoBehaviour
{
    [Header("References")]
    public Image iconImage;
    public TMP_Text countText;
    public TMP_Text nameText;

    [Header("Overpopulation badge")]
        public GameObject overpopBadge;

    // ===== BALANCE DELTA (revert: delete this block, the two marked calls, and UpdateDelta) =====
    [Header("Balance hint")]
        public TMP_Text deltaText;

        public string stableLabel = "Stable";

        public string underLabel = "Under";

        public string overLabel = "Over";

        public TMP_Text badgeLabel;

        public UnityEngine.UI.Image badgeImage;

    public Color overBadgeColor  = new Color32(0xF2, 0x6B, 0x26, 0xFF);
    public Color underBadgeColor = new Color32(0x2E, 0x9B, 0xD6, 0xFF);
    public Color stableBadgeColor = new Color32(0x2E, 0x9B, 0x5B, 0xFF);

    public Color underColor  = new Color(0.55f, 0.90f, 1f, 1f);
    public Color overColor   = new Color(1f, 0.62f, 0.55f, 1f);
    public Color stableColor = new Color(0.60f, 0.95f, 0.72f, 1f);
    // ===== END BALANCE DELTA ====================================================================

    [Header("Live update")]
        public float pollInterval = 0.2f;

    private int speciesIndex = -1;
    private int lastShown = -1;
    private float pollTimer;
    private bool overpopShown;
    private int lastDelta = int.MinValue;   // BALANCE DELTA

    public void Setup(Sprite icon, string speciesName, int count, int index)
    {
        speciesIndex = index;

        if (iconImage != null) iconImage.sprite = icon;
        if (nameText != null) nameText.text = speciesName;

        if (overpopBadge == null)
        {
            var b = transform.Find("OverpopBadge");
            if (b != null) overpopBadge = b.gameObject;
        }
        if (overpopBadge != null) overpopBadge.SetActive(false);
        overpopShown = false;

        if (overpopBadge != null)   // BALANCE DELTA: badge parts
        {
            if (badgeImage == null) badgeImage = overpopBadge.GetComponent<UnityEngine.UI.Image>();
            if (badgeLabel == null) badgeLabel = overpopBadge.GetComponentInChildren<TMP_Text>(true);
        }
        if (deltaText == null)   // BALANCE DELTA
        {
            var d = transform.Find("DeltaLabel");
            if (d != null) deltaText = d.GetComponent<TMP_Text>();
        }
        lastDelta = int.MinValue;

        SetShown(OptimisticPopulationStore.Display(index));
        UpdateDelta();   // BALANCE DELTA
    }

    void Update()
    {
        if (speciesIndex < 0 || EcosystemNetworkManagerGPU.Instance == null) return;

        pollTimer -= Time.unscaledDeltaTime;
        if (pollTimer > 0f) return;
        pollTimer = pollInterval;

        int display = OptimisticPopulationStore.Display(speciesIndex);
        if (display <= 0)
        {

            gameObject.SetActive(false);
            return;
        }
        if (display != lastShown) SetShown(display);
        UpdateDelta();   // BALANCE DELTA
    }

    void SetShown(int count)
    {
        lastShown = count;
        if (countText != null) countText.text = count.ToString();
    }

    // ===== BALANCE DELTA ========================================================================

    void UpdateDelta()
    {

        var net = EcosystemNetworkManagerGPU.Instance;
        if (speciesIndex < 0 || net == null) return;

        int delta = net.GetSpeciesDelta(speciesIndex);
        if (delta == lastDelta) return;
        lastDelta = delta;

        int state;
        if (delta == EcosystemSimulationGPU.DeltaCapped)          state =  1;
        else if (delta == EcosystemSimulationGPU.DeltaNeedsPrey)   state = -1;
        else if (delta > 0)                                        state =  1;
        else if (delta < 0)                                        state = -1;
        else                                                       state =  0;

        if (overpopBadge != null) overpopBadge.SetActive(true);
        if (badgeLabel != null)
            badgeLabel.text = state == 0 ? stableLabel : (state > 0 ? underLabel : overLabel);
        if (badgeImage != null)
            badgeImage.color = state == 0 ? stableBadgeColor : (state > 0 ? underBadgeColor : overBadgeColor);

        if (deltaText != null)
        {
            deltaText.text  = state == 0 ? stableLabel : (state > 0 ? underLabel  : overLabel);
            deltaText.color = state == 0 ? stableColor : (state > 0 ? underColor  : overColor);
        }
    }
    // ===== END BALANCE DELTA ====================================================================

    public void OnTapRemoveOne()
    {
        if (speciesIndex < 0 || EcosystemNetworkManagerGPU.Instance == null) return;
        if (OptimisticPopulationStore.Display(speciesIndex) <= 0) return;

        OptimisticPopulationStore.RegisterDelta(speciesIndex, -1);
        EcosystemNetworkManagerGPU.Instance.RequestRemoveSpeciesRpc(speciesIndex);
        if (UISoundManager.Instance != null) UISoundManager.Instance.Play(UISound.Remove);

        int display = OptimisticPopulationStore.Display(speciesIndex);
        if (display <= 0) { SetShown(0); gameObject.SetActive(false); }
        else SetShown(display);
    }
}
