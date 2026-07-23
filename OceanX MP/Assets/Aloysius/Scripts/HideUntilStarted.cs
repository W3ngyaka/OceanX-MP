using UnityEngine;
using System.Collections.Generic;

// Hides a set of visual objects until the experience begins. Hides on Awake (root stays active
// so panel logic keeps running). Reveal is delegated: if a StartTransition is present it animates
// the reveal; otherwise this reveals instantly on OnStarted. Exposes HideTargets + RevealInstant
// so the transition can drive them.
public class HideUntilStarted : MonoBehaviour
{
    [Tooltip("Visual objects to hide until start. Leave logic roots OUT of this list.")]
    public List<GameObject> hideTargets = new List<GameObject>();

    [Tooltip("If a StartTransition will animate the reveal, set true so this doesn't instant-show.")]
    public bool deferRevealToTransition = false;

    private bool _subscribed;
    private bool _hidden;

    public IReadOnlyList<GameObject> HideTargets => hideTargets;

    void Awake()
    {
        foreach (var t in hideTargets) if (t != null) t.SetActive(false);
        _hidden = true;
    }

    void Update()
    {
        if (_subscribed) return;
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net == null) return;
        _subscribed = true;
        net.OnStarted += OnStarted;
        if (net.HasStarted) OnStarted();
    }

    void OnDestroy()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net != null) net.OnStarted -= OnStarted;
    }

    void OnStarted()
    {
        if (deferRevealToTransition) return;  // StartTransition will handle it
        RevealInstant();
    }

    public void RevealInstant()
    {
        if (!_hidden) return;
        _hidden = false;
        foreach (var t in hideTargets) if (t != null) t.SetActive(true);
    }

    // Fresh-start reset: return the targets to the hidden pre-start state and re-arm the
    // reveal latch, so this hides again now and reveals on the next OnStarted.
    public void ReHideForReset()
    {
        foreach (var t in hideTargets) if (t != null) t.SetActive(false);
        _hidden = true;
    }
}
