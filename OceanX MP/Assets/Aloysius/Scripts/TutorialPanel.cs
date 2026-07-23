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
    private bool _shownOnce;
    private bool _subscribed;

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
        if (_subscribed || !showOnStart) return;
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net == null) return;
        _subscribed = true;
        if (net.HasStarted) TryAutoShow();
        else net.OnStarted += TryAutoShow;
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
        StartCoroutine(AutoShowAfterDelay());
    }

    IEnumerator AutoShowAfterDelay()
    {
        yield return new WaitForSecondsRealtime(autoShowDelay);
        Show();
    }

    public void Show() { SetVisible(true); }
    public void Hide() { SetVisible(false); }

    void SetVisible(bool visible, bool instant = false)
    {
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
