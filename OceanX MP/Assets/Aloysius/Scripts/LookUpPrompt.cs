using UnityEngine;
using System.Collections;

// Transient "look up at the big screen" toast, fired every time the visitor adds a fish.
//
// Deliberately NOT a ContextNudge. A nudge is a one-shot onboarding hint that dismisses
// permanently once its action is performed; this is a recurring event toast that re-fires
// on every add for the whole session. Same CanvasGroup fade and the same BG+Text layout so
// it reads as part of the same family, but the lifecycle is its own.
public class LookUpPrompt : MonoBehaviour
{
    public static LookUpPrompt Instance { get; private set; }

    [Header("Refs")]
    public CanvasGroup group;

    [Header("Timing")]
    [Tooltip("Seconds the prompt stays up after the most recent add. Adds while it is already " +
             "visible restart this window rather than re-fading from zero.")]
    public float holdDuration = 2.5f;
    public float fadeDuration = 0.35f;

    [Tooltip("Stay hidden while the HOW TO PLAY panel is open, so it can't draw over it.")]
    public bool suppressWhileTutorialOpen = true;

    private Coroutine _fade;
    private Coroutine _hide;
    private bool _visible;

    void Awake()
    {
        Instance = this;
        if (group == null) group = GetComponent<CanvasGroup>();
        if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Static entry point so call sites don't need their own null check.
    public static void Trigger()
    {
        if (Instance != null) Instance.Show();
    }

    public void Show()
    {
        if (group == null) return;
        if (suppressWhileTutorialOpen && TutorialPanel.Instance != null && TutorialPanel.Instance.IsOpenOrPending) return;

        // Already up: restart the hold window instead of fading in again. Re-fading on every
        // press reads as a flicker when the visitor taps Add several times in a row.
        if (_hide != null) { StopCoroutine(_hide); _hide = null; }
        if (!_visible) { _visible = true; Fade(1f); }
        _hide = StartCoroutine(HideAfterHold());
    }

    IEnumerator HideAfterHold()
    {
        yield return new WaitForSecondsRealtime(holdDuration);
        _hide = null;
        _visible = false;
        Fade(0f);
    }

    // Fresh-start reset: drop the toast instantly so a late add can't leave it hanging
    // over the attract screen for the next visitor.
    public void ResetForNewSession()
    {
        StopAllCoroutines();
        _fade = null;
        _hide = null;
        _visible = false;
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
