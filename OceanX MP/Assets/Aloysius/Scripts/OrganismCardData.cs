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
    [Tooltip("Shows this species' population state relative to what supports it. Auto-found " +
             "(child named 'DeltaLabel') if left empty. Leave unassigned to disable entirely.")]
    public TMP_Text deltaText;

    // Three descriptive states only, deliberately WITHOUT a magnitude. The row tells the visitor
    // which direction is wrong, not how far, so the ratio still has to be discovered by trying --
    // a printed "+2" is an instruction, "Under" is an observation.
    [Tooltip("Population is where it needs to be.")]
    public string stableLabel = "Stable";

    [Tooltip("Too few for what preys on it \u2014 outnumbered by its predators.")]
    public string underLabel = "Under";

    [Tooltip("Too many for what supports it \u2014 either runaway numbers, or too many mouths for " +
             "the prey available.")]
    public string overLabel = "Over";

    [Tooltip("Label inside the badge (child 'OverpopBadge/Label'). Auto-found if left empty.")]
    public TMP_Text badgeLabel;

    [Tooltip("Badge background Image (on 'OverpopBadge'). Auto-found if left empty. Recoloured per state.")]
    public UnityEngine.UI.Image badgeImage;

    public Color overBadgeColor  = new Color32(0xF2, 0x6B, 0x26, 0xFF);   // the card's existing orange
    public Color underBadgeColor = new Color32(0x2E, 0x9B, 0xD6, 0xFF);
    public Color stableBadgeColor = new Color32(0x2E, 0x9B, 0x5B, 0xFF);   // white text needs a deep green

    public Color underColor  = new Color(0.55f, 0.90f, 1f, 1f);
    public Color overColor   = new Color(1f, 0.62f, 0.55f, 1f);
    public Color stableColor = new Color(0.60f, 0.95f, 0.72f, 1f);
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
            // Species no longer in the ecosystem: remove this card from the list.
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

    // Toggle the overpopulation badge using the SAME check the food-web bubbles use: the
    // simulation's own status, synced from the host. Being at MaxSchools is NOT enough.
    // ===== BALANCE DELTA ========================================================================
    // Renders the host-computed advice for this species. The number is a live target: it moves
    // whenever this species' predators or prey change, because the simulation defines "healthy"
    // as a ratio to neighbours rather than a fixed count. Never recomputed here.
    void UpdateDelta()
    {
        // NO early-out on deltaText: the BADGE is the readout now and deltaText is optional.
        var net = EcosystemNetworkManagerGPU.Instance;
        if (speciesIndex < 0 || net == null) return;

        // Switch on the NUMBER ALONE. The status enum is deliberately not consulted: it and the
        // health formula disagree (a species can be OverPredated on a pure predator-ratio test yet
        // still count as healthy for eco-health), and mixing the two is what put "too many hunters"
        // on rows while the gauge read 100%. The host encodes everything it means in one int.
        int delta = net.GetSpeciesDelta(speciesIndex);
        if (delta == lastDelta) return;
        lastDelta = delta;

        // -1 = Over, 0 = Stable, +1 = Under.
        // The sentinels are tested FIRST: both are negative (int.MinValue + n), so a plain
        // `delta < 0` test would swallow DeltaCapped and mislabel it.
        int state;
        if (delta == EcosystemSimulationGPU.DeltaCapped)          state =  1;  // outnumbered, at its cap
        else if (delta == EcosystemSimulationGPU.DeltaNeedsPrey)   state = -1;  // too many mouths for its prey
        else if (delta > 0)                                        state =  1;
        else if (delta < 0)                                        state = -1;
        else                                                       state =  0;

        // The badge is the single readout for all three states and is always visible. It replaces
        // the old net.IsOverpopulated() path, which was a second opinion that could disagree with
        // the health bar.
        if (overpopBadge != null) overpopBadge.SetActive(true);
        if (badgeLabel != null)
            badgeLabel.text = state == 0 ? stableLabel : (state > 0 ? underLabel : overLabel);
        if (badgeImage != null)
            badgeImage.color = state == 0 ? stableBadgeColor : (state > 0 ? underBadgeColor : overBadgeColor);

        // Optional secondary text readout; normally left unassigned now the badge carries it.
        if (deltaText != null)
        {
            deltaText.text  = state == 0 ? stableLabel : (state > 0 ? underLabel  : overLabel);
            deltaText.color = state == 0 ? stableColor : (state > 0 ? underColor  : overColor);
        }
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
