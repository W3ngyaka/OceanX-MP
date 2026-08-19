using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ContextNudge : MonoBehaviour
{

    private static readonly List<ContextNudge> _all = new List<ContextNudge>();

    [Header("Identity")]
        public string id = "tap";
        public string showAfterId = "";
        public bool waitForTutorialClose = true;

    [Header("Refs")]
    public CanvasGroup group;

    [Header("Timing")]
    public float appearDelay = 1.5f;
        public float autoHideAfter = 12f;
    public float fadeDuration = 0.35f;

    private bool _dismissed;
    private bool _started;
    private bool _subscribed;
    private bool _rearmPending;
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

        if (!_subscribed) { _subscribed = true; net.OnStarted += Begin; }

        if (_rearmPending)
        {
            if (!net.HasStarted) _rearmPending = false;
            return;
        }

        if (!_started && !_dismissed && string.IsNullOrEmpty(showAfterId) && net.HasStarted) Begin();
    }

    void Begin()
    {

        if (_dismissed || _started || !string.IsNullOrEmpty(showAfterId)) return;
        _started = true;
        StartCoroutine(ShowAfterDelay());
    }

    IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSecondsRealtime(appearDelay);
        if (_dismissed) yield break;

        if (waitForTutorialClose)
        {
            while (TutorialPanel.Instance != null && TutorialPanel.Instance.IsOpen)
                yield return null;
            if (_dismissed) yield break;
            yield return new WaitForSecondsRealtime(0.4f);
            if (_dismissed) yield break;
        }

        while (SlotBlocked()) yield return null;
        if (_dismissed) yield break;

        Fade(1f);
        bool visible = true;
        if (autoHideAfter > 0f)
        {

            float shown = 0f;
            while (shown < autoHideAfter && !_dismissed)
            {
                if (SlotBlocked())
                {
                    if (visible) { visible = false; HideInstant(); }

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

        Advance(id);
    }

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

    public static void DismissId(string nudgeId)
    {
        foreach (var n in _all.ToArray())
            if (n != null && n.id == nudgeId) n.Dismiss();
    }

    public static void DismissAll()
    {
        foreach (var n in _all.ToArray()) if (n != null) n.Dismiss();
    }

    public void ResetForNewSession()
    {
        StopAllCoroutines();
        _fade = null;
        _dismissed = false;
        _started = false;
        _rearmPending = true;
        if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
    }

    static bool SlotBlocked()
    {
        if (LookUpPrompt.IsShowing) return true;
        var tut = TutorialPanel.Instance;
        return tut != null && tut.IsOpenOrPending;
    }

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
