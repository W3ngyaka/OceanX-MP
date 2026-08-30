using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ExperienceStartGate : MonoBehaviour
{
    public enum Mode { LargeScreen, Tablet }

    public Mode mode = Mode.LargeScreen;

        public GameObject titleOverlay;
        [Min(0f)]
    public float titleFadeSeconds = 0.6f;

        public GameObject tapToStart;
        public Button startButton;
        public GameObject tabletUIRoot;

    private bool _started;
    private bool _subscribed;
    private bool _requested;
    private Coroutine _titleFade;

    void Start()
    {
        if (mode == Mode.LargeScreen) ShowTitle();
        if (mode == Mode.Tablet)
        {
            if (tapToStart != null) tapToStart.SetActive(false);
            if (tabletUIRoot != null) tabletUIRoot.SetActive(false);
        }
        if (startButton != null) startButton.onClick.AddListener(RequestStart);
    }

    void Update()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net == null) return;

        if (!_subscribed)
        {
            _subscribed = true;
            net.OnStarted += OnStarted;
            if (net.HasStarted) OnStarted();
        }

        if (mode == Mode.Tablet && !_started && !_requested && tapToStart != null && !tapToStart.activeSelf)
            tapToStart.SetActive(true);
    }

    public void RequestStart()
    {
        if (mode != Mode.Tablet || _requested || _started) return;
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net == null) return;
        _requested = true;
        // Past the guards, so this fires once on the real start — not on repeat taps.
        // Host/Trifold return early on the mode check above, which is just as well:
        // UISoundManager only exists in the Tablet scene.
        if (UISoundManager.Instance != null) UISoundManager.Instance.Play(UISound.Start);
        if (tapToStart != null) tapToStart.SetActive(false);
        net.RequestStartRpc();
    }

    void OnStarted()
    {
        if (_started) return;
        _started = true;
        if (_titleFade != null) { StopCoroutine(_titleFade); _titleFade = null; }
        if (titleOverlay != null) titleOverlay.SetActive(false);
        if (tapToStart != null) tapToStart.SetActive(false);
        if (tabletUIRoot != null) tabletUIRoot.SetActive(true);
    }

    void ShowTitle()
    {
        if (titleOverlay == null) return;
        titleOverlay.SetActive(true);

        var cg = titleOverlay.GetComponent<CanvasGroup>();
        if (cg == null) cg = titleOverlay.AddComponent<CanvasGroup>();

        if (_titleFade != null) StopCoroutine(_titleFade);
        if (titleFadeSeconds <= 0f) { cg.alpha = 1f; return; }
        _titleFade = StartCoroutine(FadeTitleIn(cg));
    }

    IEnumerator FadeTitleIn(CanvasGroup cg)
    {
        cg.alpha = 0f;
        float t = 0f;
        while (t < titleFadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(t / titleFadeSeconds);
            yield return null;
        }
        cg.alpha = 1f;
        _titleFade = null;
    }

    public void ReturnToAttract()
    {
        _started = false;
        _requested = false;
        if (mode == Mode.LargeScreen) ShowTitle();
        if (mode == Mode.Tablet && tapToStart != null)
        {
            if (tabletUIRoot != null) tabletUIRoot.SetActive(false);

            tapToStart.SetActive(true);
            var cg = tapToStart.GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
        }
    }

    void OnDestroy()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net != null) net.OnStarted -= OnStarted;
    }
}
