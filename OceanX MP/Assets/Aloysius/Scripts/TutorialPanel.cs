using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// Tablet onboarding: shows a 'how to use this' panel the first time the experience starts, and
// again whenever the (?) help button is tapped. Fades in/out; blocks input while open so a
// visitor can't half-tap the UI behind it.
public class TutorialPanel : MonoBehaviour
{
    public static TutorialPanel Instance { get; private set; }

    [Header("Refs")]
    public CanvasGroup group;          // fade + block raycasts
    public Button closeButton;         // 'Got it' / X
    public Button helpButton;          // the (?) button that reopens this

    [Header("Behaviour")]
    [Tooltip("Show automatically the first time the experience starts.")]
    public bool showOnStart = true;
    [Tooltip("Seconds to wait after start before auto-showing (lets the UI settle).")]
    public float autoShowDelay = 0.6f;
    public float fadeDuration = 0.3f;

    private Coroutine _fade;
    private bool _open;

    // True while the panel is showing (used by ContextNudge so hints don't overlap it).
    public bool IsOpen => _open;

    // True while the panel is showing OR queued to appear on the auto-show delay. Other UI should
    // gate on THIS rather than IsOpen: between the session starting and the panel fading in there is
    // an autoShowDelay-long window where IsOpen is still false, and anything keying off IsOpen alone
    // flashes on screen for exactly that long before the tutorial covers it.
    public bool IsOpenOrPending => _open || _autoShowPending;

    private bool _autoShowPending;
    private bool _shownOnce;
    private bool _subscribed;
    private bool _rearmPending;   // set by ResetForNewSession; cleared in Update once HasStarted is false

    void Awake()
    {
        Instance = this;
        if (group == null) group = GetComponent<CanvasGroup>();
        SetVisible(false, instant: true);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (helpButton != null) helpButton.onClick.AddListener(Show);
    }

    void Update()
    {
        if (!showOnStart) return;
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net == null) return;

        // ALWAYS subscribe (not just in the not-started branch). A tablet that joins a session that is
        // already running receives _hasStarted inside the spawn payload, which lands BEFORE
        // OnNetworkSpawn wires up OnValueChanged, so OnStarted never fires for it. The HasStarted poll
        // below is what catches that case; the event covers the normal start-while-connected case.
        if (!_subscribed) { _subscribed = true; net.OnStarted += TryAutoShow; }

        // After a reset, only re-arm once HasStarted is actually back to false, otherwise the panel can
        // re-show instantly off the stale true that may still be in flight.
        if (_rearmPending)
        {
            if (!net.HasStarted) { _rearmPending = false; _shownOnce = false; }
            return;
        }

        if (!_shownOnce && net.HasStarted) TryAutoShow();
    }

    void OnDestroy()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net != null) net.OnStarted -= TryAutoShow;
    }

    void TryAutoShow()
    {
        if (_shownOnce) return;
        _shownOnce = true;
        _autoShowPending = true;      // claim the screen NOW, not in autoShowDelay seconds
        StartCoroutine(AutoShowAfterDelay());
    }

    IEnumerator AutoShowAfterDelay()
    {
        yield return new WaitForSecondsRealtime(autoShowDelay);
        _autoShowPending = false;
        Show();
    }

    public void Show() { SetVisible(true); }
    public void Hide() { SetVisible(false); }

    // Fresh-start reset: hide the panel and re-arm the "show once on start" latch so the onboarding
    // panel auto-shows again for the NEXT visitor (otherwise _shownOnce stays true forever and it never
    // reappears). The re-arm is DEFERRED to Update() until HasStarted is observed false again, so the
    // panel can't pop straight back up off a stale true while the reset is still propagating.
    public void ResetForNewSession()
    {
        if (_fade != null) { StopCoroutine(_fade); _fade = null; }
        StopAllCoroutines();          // cancel any pending AutoShowAfterDelay / fade
        _fade = null;
        _rearmPending = true;         // Update() clears _shownOnce once HasStarted is false again
        _autoShowPending = false;     // a queued auto-show is cancelled with the coroutines above
        SetVisible(false, instant: true);
    }

    void SetVisible(bool visible, bool instant = false)
    {
        _open = visible;
        if (group == null) return;
        group.blocksRaycasts = visible;
        group.interactable = visible;
        if (instant) { group.alpha = visible ? 1f : 0f; return; }
        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(FadeTo(visible ? 1f : 0f));
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
