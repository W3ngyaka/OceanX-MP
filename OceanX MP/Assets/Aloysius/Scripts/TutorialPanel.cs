using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TutorialPanel : MonoBehaviour
{
    public static TutorialPanel Instance { get; private set; }

    [Header("Refs")]
    public CanvasGroup group;
    public Button closeButton;
    public Button helpButton;

    [Header("Behaviour")]
        public bool showOnStart = true;
        public float autoShowDelay = 0.6f;
    public float fadeDuration = 0.3f;

    private Coroutine _fade;
    private bool _open;

    public bool IsOpen => _open;

    public bool IsOpenOrPending => _open || _autoShowPending;

    private bool _autoShowPending;
    private bool _shownOnce;
    private bool _subscribed;
    private bool _rearmPending;

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

        if (!_subscribed) { _subscribed = true; net.OnStarted += TryAutoShow; }

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
        _autoShowPending = true;
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

    public void ResetForNewSession()
    {
        if (_fade != null) { StopCoroutine(_fade); _fade = null; }
        StopAllCoroutines();
        _fade = null;
        _rearmPending = true;
        _autoShowPending = false;
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
