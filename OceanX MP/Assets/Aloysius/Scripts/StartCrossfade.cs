using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Quick, robust crossfade from the start screen to the Food Web UI on experience start.
// Fades the Tap overlay out while the UI pieces fade in together. Waits a couple frames after
// activating the pieces so the food web finishes building BEFORE fading (prevents the hitch).
// No slide, no stagger, no dim. Pairs with HideUntilStarted (deferRevealToTransition = true).
public class StartCrossfade : MonoBehaviour
{
    [Header("Refs")]
    public CanvasGroup tapOverlay;      // start screen (title + deco) to fade out
    public GameObject tapOverlayRoot;   // deactivated after fade (usually same object as tapOverlay)
    public HideUntilStarted hideGate;   // provides the UI pieces to fade in

    [Header("Timing")]
    public float fadeOut = 0.4f;
    public float fadeIn = 0.5f;
    public int settleFrames = 2;        // let the UI build before fading in (anti-clank)

    private bool _played;

    void Awake()
    {
        if (hideGate == null) hideGate = GetComponent<HideUntilStarted>();
        if (hideGate != null) hideGate.deferRevealToTransition = true;
    }

    void Update()
    {
        if (_played) return;
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net == null) return;
        if (net.HasStarted) { Trigger(); return; }
        net.OnStarted += Trigger;
        enabled = false;
    }

    void Trigger()
    {
        if (_played) return;
        _played = true;
        StartCoroutine(Play());
    }

    IEnumerator Play()
    {
        // Fade the start screen out (runs alongside the UI fade-in).
        if (tapOverlay != null) StartCoroutine(FadeCG(tapOverlay, tapOverlay.alpha, 0f, fadeOut, tapOverlayRoot));

        // Activate UI pieces hidden, let them build, then fade in together.
        var pieces = new List<CanvasGroup>();
        if (hideGate != null)
            foreach (var go in hideGate.HideTargets)
            {
                if (go == null) continue;
                var cg = EnsureCG(go);
                cg.alpha = 0f;
                go.SetActive(true);
                pieces.Add(cg);
            }
        for (int i = 0; i < settleFrames; i++) yield return null;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, fadeIn);
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f); // ease-out
            foreach (var cg in pieces) if (cg != null) cg.alpha = e;
            yield return null;
        }
        foreach (var cg in pieces) if (cg != null) cg.alpha = 1f;
    }

    IEnumerator FadeCG(CanvasGroup cg, float from, float to, float dur, GameObject deactivateAfter)
    {
        float t = 0f;
        while (t < 1f) { t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, dur); cg.alpha = Mathf.Lerp(from, to, t); yield return null; }
        cg.alpha = to;
        if (deactivateAfter != null && to == 0f) deactivateAfter.SetActive(false);
    }

    static CanvasGroup EnsureCG(GameObject go){ var c=go.GetComponent<CanvasGroup>(); if(c==null)c=go.AddComponent<CanvasGroup>(); return c; }

    void OnDestroy()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net != null) net.OnStarted -= Trigger;
    }

    public void DebugReplay()
    {
        StopAllCoroutines(); _played = true;
        if (tapOverlay != null) { tapOverlay.alpha = 1f; if (tapOverlayRoot != null) tapOverlayRoot.SetActive(true); }
        if (hideGate != null) foreach (var go in hideGate.HideTargets) if (go != null) { EnsureCG(go).alpha = 0f; go.SetActive(false); }
        StartCoroutine(Play());
    }

    // Fresh-start reset: re-arm the one-shot so the crossfade PLAYS AGAIN on the next start (otherwise
    // _played stays true and the second visitor's start never reveals the UI), and return the start
    // overlay + UI pieces to their pre-start state. Does NOT re-enable this component (it stays subscribed
    // to OnStarted from its first setup — re-enabling would double-subscribe).
    public void ResetForNewSession()
    {
        StopAllCoroutines();
        _played = false;
        if (tapOverlay != null) tapOverlay.alpha = 1f;
        if (tapOverlayRoot != null) tapOverlayRoot.SetActive(true);
        if (hideGate != null)
            foreach (var go in hideGate.HideTargets)
                if (go != null) { EnsureCG(go).alpha = 0f; go.SetActive(false); }
    }
}
