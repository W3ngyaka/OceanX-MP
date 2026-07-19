using UnityEngine;
using System.Collections.Generic;

// Hides a set of visual objects until the experience begins (the tablet's "tap to start" flips
// the shared networked HasStarted flag). Crucially this hides only the assigned VISUAL objects,
// NOT the logic-bearing panel root (TabController / unlock manager must keep running), and it
// only RE-SHOWS the ones that were visible to begin with — it never force-enables tab pages that
// the tab system keeps hidden.
public class HideUntilStarted : MonoBehaviour
{
    [Tooltip("Visual objects to hide until start. Leave logic roots OUT of this list.")]
    public List<GameObject> hideTargets = new List<GameObject>();

    private bool _subscribed;
    private bool _hidden;

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
        net.OnStarted += Reveal;
        if (net.HasStarted) Reveal();
    }

    void OnDestroy()
    {
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net != null) net.OnStarted -= Reveal;
    }

    void Reveal()
    {
        if (!_hidden) return;
        _hidden = false;
        foreach (var t in hideTargets) if (t != null) t.SetActive(true);
    }
}
