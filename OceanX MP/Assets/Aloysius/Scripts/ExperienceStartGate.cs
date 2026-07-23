using UnityEngine;
using UnityEngine.UI;

// Cross-device "tap the tablet to start" gate. Fits the existing netcode: the host (large
// screen) and tablet are already connected via EcosystemNetworkManagerGPU. The tablet's tap
// flips one shared networked flag (HasStarted) that BOTH screens react to — no cross-device
// scene loading, no change to JunHeng's connect flow.
//
//   • LargeScreen: shows a title/attract overlay; hides it (revealing the reef) once started.
//   • Tablet:      shows a "Tap to Start" prompt once connected; the tap fires RequestStartRpc,
//                  then the tablet UI is revealed when the shared flag turns true.
public class ExperienceStartGate : MonoBehaviour
{
    public enum Mode { LargeScreen, Tablet }

    [Header("Role")]
    public Mode mode = Mode.LargeScreen;

    [Header("Large screen")]
    [Tooltip("Title / attract overlay shown until the experience starts, then hidden.")]
    public GameObject titleOverlay;

    [Header("Tablet")]
    [Tooltip("'Tap to Start' prompt (contains the full-screen button). Shown once connected, hidden after the tap.")]
    public GameObject tapToStart;
    [Tooltip("Full-screen button that fires the start (wired automatically).")]
    public Button startButton;
    [Tooltip("Tablet UI to reveal once the experience starts. Leave null if something else reveals it.")]
    public GameObject tabletUIRoot;

    private bool _started;
    private bool _subscribed;
    private bool _requested;

    void Start()
    {
        if (mode == Mode.LargeScreen && titleOverlay != null) titleOverlay.SetActive(true);
        if (mode == Mode.Tablet)
        {
            if (tapToStart != null) tapToStart.SetActive(false);     // shown once connected (Update)
            if (tabletUIRoot != null) tabletUIRoot.SetActive(false);
        }
        if (startButton != null) startButton.onClick.AddListener(RequestStart);
    }

    void Update()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net == null) return;   // not connected / manager not spawned yet

        // Subscribe once the networked manager exists (host spawns it; client receives on connect).
        if (!_subscribed)
        {
            _subscribed = true;
            net.OnStarted += OnStarted;
            if (net.HasStarted) OnStarted();   // already started (e.g. tablet joined late)
        }

        // Tablet: reveal the "Tap to Start" prompt now that we're connected.
        if (mode == Mode.Tablet && !_started && !_requested && tapToStart != null && !tapToStart.activeSelf)
            tapToStart.SetActive(true);
    }

    // Wired to the tablet's full-screen button.
    public void RequestStart()
    {
        if (mode != Mode.Tablet || _requested || _started) return;
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net == null) return;   // not connected yet — ignore taps
        _requested = true;
        if (tapToStart != null) tapToStart.SetActive(false);
        net.RequestStartRpc();     // -> host flips HasStarted -> OnStarted fires on both screens
    }

    void OnStarted()
    {
        if (_started) return;
        _started = true;
        if (titleOverlay != null) titleOverlay.SetActive(false);   // large screen: reveal the reef
        if (tapToStart != null) tapToStart.SetActive(false);
        if (tabletUIRoot != null) tabletUIRoot.SetActive(true);
    }

    // Fresh-start reset: reverses OnStarted so this screen returns to its pre-start
    // (attract) state. The "Tap to Start" prompt re-appears on its own via Update once
    // connected, so we don't force tapToStart back on here.
    public void ReturnToAttract()
    {
        _started = false;
        _requested = false;
        if (mode == Mode.LargeScreen && titleOverlay != null) titleOverlay.SetActive(true);
        if (mode == Mode.Tablet && tabletUIRoot != null) tabletUIRoot.SetActive(false);
    }

    void OnDestroy()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net != null) net.OnStarted -= OnStarted;
    }
}
