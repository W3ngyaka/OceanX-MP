using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// A small contextual nudge (e.g. "Tap any fish to learn about it"). Fades in after the experience
// starts — optionally only AFTER another nudge's action was performed, so hints can chain — then
// disappears permanently once the visitor performs its own action.
public class ContextNudge : MonoBehaviour
{
    // Nudges register themselves so other scripts can dismiss/unlock them by id.
    private static readonly List<ContextNudge> _all = new List<ContextNudge>();

    [Header("Identity")]
    [Tooltip("Id used to dismiss this nudge (e.g. 'tap', 'hold').")]
    public string id = "tap";
    [Tooltip("Optional gate. Blank = show as soon as the experience starts. Otherwise wait for either the nudge with THIS id to be dismissed, or a matching ContextNudge.Advance(id) milestone (e.g. 'details' = a species panel was opened and closed).")]
    public string showAfterId = "";
    [Tooltip("Don't appear while the HOW TO PLAY panel is open (avoids overlapping it).")]
    public bool waitForTutorialClose = true;

    [Header("Refs")]
    public CanvasGroup group;

    [Header("Timing")]
    public float appearDelay = 1.5f;
    [Tooltip("Auto-hide after this many seconds even if ignored (0 = stay until dismissed).")]
    public float autoHideAfter = 12f;
    public float fadeDuration = 0.35f;

    private bool _dismissed;
    private bool _started;
    private bool _subscribed;
    private bool _rearmPending;   // set by ResetForNewSession; cleared in Update once HasStarted is false
    private Coroutine _fade;

    void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
        if (!_all.Contains(this)) _all.Add(this);
    }

    void OnDestroy()
    {
        _all.Remove(this);
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net != null) net.OnStarted -= Begin;
    }

    void Update()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net == null) return;

        // ALWAYS subscribe (not just in the not-started branch). A tablet that joins a session that is
        // already running receives _hasStarted inside the spawn payload, which lands BEFORE
        // OnNetworkSpawn wires up OnValueChanged, so OnStarted never fires for it. The HasStarted poll
        // below is what catches that case.
        if (!_subscribed) { _subscribed = true; net.OnStarted += Begin; }

        // After a reset, only re-arm once HasStarted is actually back to false.
        if (_rearmPending)
        {
            if (!net.HasStarted) _rearmPending = false;
            return;
        }

        if (!_started && !_dismissed && string.IsNullOrEmpty(showAfterId) && net.HasStarted) Begin();
    }

    void Begin()
    {
        // Gated nudges wait until their prerequisite is dismissed (see Dismiss()).
        if (_dismissed || _started || !string.IsNullOrEmpty(showAfterId)) return;
        _started = true;
        StartCoroutine(ShowAfterDelay());
    }

    IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSecondsRealtime(appearDelay);
        if (_dismissed) yield break;

        // Hold off while the HOW TO PLAY panel is up so we don't draw over it.
        if (waitForTutorialClose)
        {
            while (TutorialPanel.Instance != null && TutorialPanel.Instance.IsOpen)
                yield return null;
            if (_dismissed) yield break;
            yield return new WaitForSecondsRealtime(0.4f);   // small beat after it closes
            if (_dismissed) yield break;
        }
        // BANNER SLOT: this hint, the other nudge and LookUpPrompt all sit at the same position on
        // the canvas, so only one may be visible. The toast wins -- it is tied to an action the
        // visitor just took and is only useful while the fish is actually arriving, whereas an
        // onboarding hint is happy to wait a couple of seconds.
        while (LookUpPrompt.IsShowing) yield return null;
        if (_dismissed) yield break;

        Fade(1f);
        bool visible = true;
        if (autoHideAfter > 0f)
        {
            // Time spent yielding to the toast does NOT count against the hint's visible time,
            // otherwise a burst of adds could silently eat the whole window and the visitor would
            // never actually get to read it.
            float shown = 0f;
            while (shown < autoHideAfter && !_dismissed)
            {
                if (LookUpPrompt.IsShowing)
                {
                    if (visible) { visible = false; HideInstant(); }   // vacate at once: a 0.35s
                                                                        // cross-fade with the toast at
                                                                        // the same position reads as a
                                                                        // garbled double-exposure.
                }
                else
                {
                    if (!visible) { visible = true; Fade(1f); }
                    shown += Time.unscaledDeltaTime;
                }
                yield return null;
            }
            if (!_dismissed) Fade(0f);
        }
    }

    public void Dismiss()
    {
        if (_dismissed) return;
        _dismissed = true;
        Fade(0f);

        // Unlock any nudge waiting on this one.
        Advance(id);
    }

    // Release the nudges gated on `gateId` via showAfterId. `gateId` is either another nudge's
    // id (Dismiss calls this automatically) or a plain milestone the game reports itself, for
    // cases where "the previous hint faded" is not the right moment. ModalController.Close calls
    // Advance("details") so the hold hint only appears once a species panel has been read AND closed.
    public static void Advance(string gateId)
    {
        if (string.IsNullOrEmpty(gateId)) return;
        foreach (var n in _all.ToArray())
            if (n != null && !n._started && !n._dismissed && !n._rearmPending && n.showAfterId == gateId)
            {
                n._started = true;
                n.StartCoroutine(n.ShowAfterDelay());
            }
    }

    // Dismiss a specific nudge by id (e.g. ContextNudge.DismissId("tap")).
    public static void DismissId(string nudgeId)
    {
        foreach (var n in _all.ToArray())
            if (n != null && n.id == nudgeId) n.Dismiss();
    }

    public static void DismissAll()
    {
        foreach (var n in _all.ToArray()) if (n != null) n.Dismiss();
    }

    // Fresh-start reset: cancel any active/pending nudge and clear state/timers so it
    // behaves as freshly started and re-arms on the next start (gated nudges still
    // wait on their prerequisite via showAfterId). The re-arm is deferred in Update()
    // until HasStarted is observed false again, so the reset can't instantly re-trigger it.
    public void ResetForNewSession()
    {
        StopAllCoroutines();          // cancel a pending ShowAfterDelay / running fade
        _fade = null;
        _dismissed = false;
        _started = false;
        _rearmPending = true;
        if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
    }

    // Drop out of the shared banner slot with no fade at all.
    void HideInstant()
    {
        if (_fade != null) { StopCoroutine(_fade); _fade = null; }
        if (group != null) group.alpha = 0f;
    }

    void Fade(float target)
    {
        if (group == null) return;
        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(FadeTo(target));
    }

    IEnumerator FadeTo(float target)
    {
        float from = group.alpha, t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, fadeDuration);
            group.alpha = Mathf.Lerp(from, target, t);
            yield return null;
        }
        group.alpha = target;
    }
}
