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
    [Tooltip("Optional: only start once the nudge with THIS id has been dismissed. Blank = show immediately.")]
    public string showAfterId = "";

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
        if (_subscribed) return;
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net == null) return;
        _subscribed = true;
        if (net.HasStarted) Begin();
        else net.OnStarted += Begin;
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
        Fade(1f);
        if (autoHideAfter > 0f)
        {
            yield return new WaitForSecondsRealtime(autoHideAfter);
            if (!_dismissed) Fade(0f);
        }
    }

    public void Dismiss()
    {
        if (_dismissed) return;
        _dismissed = true;
        Fade(0f);

        // Unlock any nudge waiting on this one.
        foreach (var n in _all)
            if (n != null && !n._started && !n._dismissed && n.showAfterId == id)
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
    // behaves as freshly started and re-arms on the next OnStarted (gated nudges still
    // wait on their prerequisite via showAfterId).
    public void ResetForNewSession()
    {
        StopAllCoroutines();          // cancel a pending ShowAfterDelay / running fade
        _fade = null;
        _dismissed = false;
        _started = false;
        if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
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
