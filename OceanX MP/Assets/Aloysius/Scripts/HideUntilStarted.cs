using UnityEngine;
using System.Collections.Generic;

public class HideUntilStarted : MonoBehaviour
{
        public List<GameObject> hideTargets = new List<GameObject>();

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
        if (deferRevealToTransition) return;
        RevealInstant();
    }

    public void RevealInstant()
    {
        if (!_hidden) return;
        _hidden = false;
        foreach (var t in hideTargets) if (t != null) t.SetActive(true);
    }

    public void ReHideForReset()
    {
        foreach (var t in hideTargets) if (t != null) t.SetActive(false);
        _hidden = true;
    }
}
