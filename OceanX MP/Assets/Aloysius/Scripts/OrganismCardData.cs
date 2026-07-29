using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OceanX.BoidsGPU.Ecosystem;   // SpeciesStatus (BALANCE DELTA)

public class OrganismCardData : MonoBehaviour
{
    [Header("References")]
    public Image iconImage;
    public TMP_Text countText;
    public TMP_Text nameText; // optional, leave unassigned if not used

    [Header("Overpopulation badge")]
    [Tooltip("Shown when the simulation reports this species as overpopulated (too few predators left " +
             "to keep it in check). Auto-found (child named 'OverpopBadge') if left empty. Matches the " +
             "food-web bubble's overpop logic.")]
    public GameObject overpopBadge;

    // ===== BALANCE DELTA (revert: delete this block, the two marked calls, and UpdateDelta) =====
    [Header("Balance hint")]
    [Tooltip("Shows how many to add (+) or remove (-) to make this species healthy, using the " +
             "simulation's own thresholds. Auto-found (child named 'DeltaLabel') if left empty. " +
             "Leave unassigned to disable entirely.")]
    public TMP_Text deltaText;

    [Tooltip("Text when this species is already fine.")]
        // Rajdhani has no U+2713 check mark and no fallback supplies one, so a tick renders as tofu.
    public string okLabel = "ok";

    [Tooltip("Text when the fix belongs to another species (e.g. it is starving and needs more prey).")]
    public string needsFoodLabel = "hungry";

    [Tooltip("Text when a species is being hunted out but is already at its MaxSchools cap, so the " +
             "only fix is removing some of its predators. Without this the row would read 'ok'.")]
    public string huntedOutLabel = "hunted";

    public Color addColor    = new Color(0.55f, 0.90f, 1f, 1f);
    public Color removeColor = new Color(1f, 0.62f, 0.55f, 1f);
    public Color okColor     = new Color(0.60f, 0.95f, 0.72f, 1f);
    // ===== END BALANCE DELTA ====================================================================

    [Header("Live update")]
    [Tooltip("Seconds between population polls. 0 = every frame.")]
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

        // Show the optimistic count so a card recreated after a panel switch reflects a pending
        // removal instead of snapping back to the host's not-yet-lowered count.
        if (deltaText == null)   // BALANCE DELTA
        {
            var d = transform.Find("DeltaLabel");
            if (d != null) deltaText = d.GetComponent<TMP_Text>();
        }
        lastDelta = int.MinValue;

        SetShown(OptimisticPopulationStore.Display(index));
        UpdateOverpop();
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
            // Species no longer in the ecosystem: remove this card from the list.
            gameObject.SetActive(false);
            return;
        }
        if (display != lastShown) SetShown(display);
        UpdateOverpop();
        UpdateDelta();   // BALANCE DELTA
    }

    void SetShown(int count)
    {
        lastShown = count;
        if (countText != null) countText.text = count.ToString();
    }

    // Toggle the overpopulation badge using the SAME check the food-web bubbles use: the
    // simulation's own status, synced from the host. Being at MaxSchools is NOT enough.
    void UpdateOverpop()
    {
        if (overpopBadge == null) return;
        bool over = false;
        var net = EcosystemNetworkManagerGPU.Instance;
        if (speciesIndex >= 0 && net != null)
            over = net.IsOverpopulated(speciesIndex);
        if (over != overpopShown)
        {
            overpopShown = over;
            overpopBadge.SetActive(over);
        }
    }

    // ===== BALANCE DELTA ========================================================================
    // Renders the host-computed advice for this species. The number is a live target: it moves
    // whenever this species' predators or prey change, because the simulation defines "healthy"
    // as a ratio to neighbours rather than a fixed count. Never recomputed here.
    void UpdateDelta()
    {
        if (deltaText == null) return;
        var net = EcosystemNetworkManagerGPU.Instance;
        if (speciesIndex < 0 || net == null) return;

        // Switch on the NUMBER ALONE. The status enum is deliberately not consulted: it and the
        // health formula disagree (a species can be OverPredated on a pure predator-ratio test yet
        // still count as healthy for eco-health), and mixing the two is what put "too many hunters"
        // on rows while the gauge read 100%. The host encodes everything it means in one int.
        int delta = net.GetSpeciesDelta(speciesIndex);
        if (delta == lastDelta) return;
        lastDelta = delta;

        if (delta == EcosystemSimulationGPU.DeltaNeedsPrey)
        { deltaText.text = needsFoodLabel; deltaText.color = removeColor; }
        else if (delta == EcosystemSimulationGPU.DeltaCapped)
        { deltaText.text = huntedOutLabel; deltaText.color = removeColor; }
        else if (delta > 0)
        { deltaText.text = "+" + delta;    deltaText.color = addColor; }
        else if (delta < 0)
        { deltaText.text = "-" + (-delta); deltaText.color = removeColor; }
        else
        { deltaText.text = okLabel;        deltaText.color = okColor; }
    }
    // ===== END BALANCE DELTA ====================================================================

    // Hook this to the card's "-1" Button.onClick (prototype spec: -1 or All).
    public void OnTapRemoveOne()
    {
        if (speciesIndex < 0 || EcosystemNetworkManagerGPU.Instance == null) return;
        if (OptimisticPopulationStore.Display(speciesIndex) <= 0) return; // already at zero optimistically

        // Record the intent in the shared store (persists across panel switches), then fire the RPC.
        OptimisticPopulationStore.RegisterDelta(speciesIndex, -1);
        EcosystemNetworkManagerGPU.Instance.RequestRemoveSpeciesRpc(speciesIndex);

        // Update the number the instant the player taps — don't wait for the next poll.
        int display = OptimisticPopulationStore.Display(speciesIndex);
        if (display <= 0) { SetShown(0); gameObject.SetActive(false); }
        else SetShown(display);
    }
}
