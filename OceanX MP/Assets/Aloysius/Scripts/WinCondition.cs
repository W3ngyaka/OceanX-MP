using UnityEngine;

// Local win detector (one per scene). Reads the networked eco-health via the manager singleton
// and latches Won once health holds at/above the threshold for holdSeconds. Both host and tablet
// run their own copy; since they read the same networked health, they trigger within a frame or
// two of each other. WinScreen watches Won.
public class WinCondition : MonoBehaviour
{
    [Tooltip("Eco-health (0..1) at or above which the ecosystem counts as 'won'.")]
    public float winThreshold01 = 0.99f;

    [Tooltip("Seconds health must stay at/above the threshold before winning (ignores one-frame spikes).")]
    public float holdSeconds = 2f;

    public bool Won { get; private set; }

    private float _heldFor;

    void Update()
    {
        if (Won) return;
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net == null) return;

        float h = net.GetEcoHealth(); // 0..1, networked value
        if (h >= winThreshold01)
        {
            _heldFor += Time.deltaTime;
            if (_heldFor >= holdSeconds) Won = true;
        }
        else _heldFor = 0f;
    }

    public void Reset()
    {
        _heldFor = 0f;
        Won = false;
    }

    public static WinCondition Instance { get; private set; }
    void Awake() { Instance = this; }
}
