using UnityEngine;

/// <summary>
/// Exhibit "fresh start" coordinator. Put ONE on each device's scene (host + tablet each run their own copy).
///
///  • Operator trigger: hold <see cref="resetKey"/> for <see cref="holdSeconds"/>. A hold (not a tap) avoids
///    accidents. The tablet has no keyboard, so in practice the operator resets from the host PC; either way
///    the request routes to the host.
///
///  • The HOST (big screen) plays a bubble wipe (<see cref="BubbleTransition"/>). At the covered PEAK — screen
///    hidden — it performs the host-authoritative reset (empty ocean + drop to attract + sync 0s) and signals
///    "reset applied". BOTH screens then run their LOCAL cleanup: re-lock species, clear the tablet's counts,
///    dismiss the win overlay, clear leftover cards, and return every start-flow UI to the title / "Tap to
///    Start". The tablet shows no bubbles — it just flips back to the title with everything reset.
///
/// Trigger flow: hold key → RequestResetRpc() → host fires OnHostResetRequested → host plays the bubble wipe
/// → at the covered peak: PerformResetCore() + SignalResetApplied() → OnReset fires on both → DoLocalReset().
/// </summary>
public class ExhibitReset : MonoBehaviour
{
    [Header("Operator reset (hold on the host keyboard)")]
    [Tooltip("Hold this key for Hold Seconds to reset the whole exhibit for the next visitor.")]
    public KeyCode resetKey = KeyCode.F9;

    [Tooltip("How long the reset key must be held before the reset fires (a hold, not a tap, avoids accidents).")]
    [Min(0.1f)]
    public float holdSeconds = 1.5f;

    private bool _subscribed;
    private float _held;
    private bool _latched;   // true once a hold has fired; released when the key comes back up

    private void Update()
    {
        // Subscribe once the networked manager exists (netcode spawns it on connect).
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net != null && !_subscribed)
        {
            _subscribed = true;
            net.OnReset += DoLocalReset;                  // reset APPLIED (covered peak) -> local cleanup (both)
            net.OnHostResetRequested += StartHostSequence; // server-side only -> host plays the bubble wipe
            Debug.Log($"[ExhibitReset] subscribed to OnReset (IsServer={net.IsServer}).", this);
        }

        // Operator hold-to-reset. unscaledDeltaTime so it still works if the game is paused/timescaled.
        if (Input.GetKey(resetKey))
        {
            if (!_latched)
            {
                _held += Time.unscaledDeltaTime;
                if (_held >= holdSeconds) { _latched = true; TriggerReset(); }
            }
        }
        else
        {
            _held = 0f;
            _latched = false;
        }
    }

    /// <summary>Ask the host to perform a fresh start. Public so it can also be wired to a hidden button.</summary>
    public void TriggerReset()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net == null) return;   // not connected yet — nothing to reset
        Debug.Log("[ExhibitReset] Fresh start requested.", this);
        net.RequestResetRpc();     // -> server fires OnHostResetRequested -> StartHostSequence on the host
    }

    // HOST ONLY (OnHostResetRequested is server-side). Plays the big-screen bubble wipe; at the covered peak
    // performs the host reset and signals both screens. Falls back to an instant reset if no wipe is present.
    private void StartHostSequence()
    {
        // Stop ALL host-side visuals the moment the reset is REQUESTED (not at the covered peak), so Alucia's
        // speech and any reveal/unlock cards vanish immediately as the bubbles rise, instead of lingering for
        // the cover duration. The full state reset still applies at the peak via DoLocalReset (idempotent).
        const FindObjectsInactive INC = FindObjectsInactive.Include;
        const FindObjectsSortMode NONE = FindObjectsSortMode.None;
        foreach (var a in FindObjectsByType<AluciaController>(INC, NONE))     a.ResetForNewSession();  // stop talking
        foreach (var r in FindObjectsByType<RevealQueue>(INC, NONE))          r.ClearAll();            // reveal + unlock cards
        foreach (var s in FindObjectsByType<SpeciesAddedReveal>(INC, NONE))   s.ResetShownHistory();   // pending arrival card
        foreach (var n in FindObjectsByType<NotificationManager>(INC, NONE))  n.ClearAll();            // unlock toasts

        var bubbles = FindFirstObjectByType<BubbleTransition>(FindObjectsInactive.Include);
        if (bubbles != null && !bubbles.IsPlaying)
            bubbles.Play(onCovered: ApplyHostReset, onComplete: null);
        else if (bubbles == null)
            ApplyHostReset();   // no wipe wired -> reset immediately (still correct, just no cover)
    }

    // At the covered peak: empty the ocean + drop to attract + sync 0s, then tell both screens to clean up.
    private void ApplyHostReset()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net == null) return;
        Debug.Log($"[ExhibitReset] ApplyHostReset (IsServer={net.IsServer}) -> PerformResetCore + SignalResetApplied.");
        net.PerformResetCore();    // host: empty + attract + sync (hidden behind the bubbles)
        net.SignalResetApplied();  // -> OnReset on host + client -> DoLocalReset everywhere
    }

    // Runs on THIS device when the reset is applied (host + tablet, in lockstep). Health/counts are already 0
    // (the host synced them just before signalling), so re-locking sticks. On the host this happens behind the
    // bubbles; on the tablet it flips the UI back to the title.
    private void DoLocalReset()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        int gates = FindObjectsByType<ExperienceStartGate>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        Debug.Log($"[ExhibitReset] DoLocalReset FIRED (IsServer={(net != null && net.IsServer)}, " +
                  $"unlockMgr={(EcosystemUnlockManagerGPU.Instance != null)}, startGates={gates}).", this);

        OptimisticPopulationStore.Clear();                                  // tablet: drop not-yet-confirmed taps

        if (EcosystemUnlockManagerGPU.Instance != null)
            EcosystemUnlockManagerGPU.Instance.ResetToStart();             // re-lock everything to the 5 starters

        // Return every start-flow / card component to its attract state. FindObjects (not singletons) so this
        // works whichever components a given device actually has, without any scene wiring.
        const FindObjectsInactive INC = FindObjectsInactive.Include;
        const FindObjectsSortMode NONE = FindObjectsSortMode.None;

        foreach (var g in FindObjectsByType<ExperienceStartGate>(INC, NONE)) g.ReturnToAttract();
        foreach (var a in FindObjectsByType<AluciaController>(INC, NONE))    a.ResetForNewSession();
        foreach (var h in FindObjectsByType<HideUntilStarted>(INC, NONE))    h.ReHideForReset();
        foreach (var s in FindObjectsByType<StartCrossfade>(INC, NONE))      s.ResetForNewSession();  // re-arm the reveal crossfade
        foreach (var t in FindObjectsByType<TutorialPanel>(INC, NONE))       t.ResetForNewSession();  // re-arm the onboarding panel
        foreach (var tc in FindObjectsByType<TabController>(INC, NONE))      tc.ResetForNewSession();  // snap back to the default tab
        foreach (var c in FindObjectsByType<ContextNudge>(INC, NONE))        c.ResetForNewSession();
        foreach (var p in FindObjectsByType<LookUpPrompt>(INC, NONE))        p.ResetForNewSession();  // drop the look-up toast
        foreach (var s in FindObjectsByType<SpeciesAddedReveal>(INC, NONE))  s.ResetShownHistory();
        foreach (var r in FindObjectsByType<RevealQueue>(INC, NONE))         r.ClearAll();
        foreach (var n in FindObjectsByType<NotificationManager>(INC, NONE)) n.ClearAll();
        foreach (var w in FindObjectsByType<WinCondition>(INC, NONE))        w.Reset();
    }

    private void OnDestroy()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net != null)
        {
            net.OnReset -= DoLocalReset;
            net.OnHostResetRequested -= StartHostSequence;
        }
    }
}
