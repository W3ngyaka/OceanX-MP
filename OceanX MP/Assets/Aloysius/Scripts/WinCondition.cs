using UnityEngine;

public class WinCondition : MonoBehaviour
{
        public float winThreshold01 = 0.99f;

        public float holdSeconds = 2f;

    public bool Won { get; private set; }

    private float _heldFor;

    void Update()
    {
        if (Won) return;
        var net = EcosystemNetworkManagerGPU.Instance;
        if (net == null) return;

        float h = net.GetEcoHealth();
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
