using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Drives a dashboard arc gauge (fill Image + percent text) from live eco-health.
// Reads EcosystemNetworkManagerGPU.GetEcoHealth() (0-1), falls back to a manual
// value when no network manager is present. Drives the Image fillAmount + text
// directly (bypasses the prefab's own slider component).
[ExecuteAlways]
public class EcoHealthDashboard : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("The arc fill Image (Dashboard01_Progress). Must be Image type = Filled.")]
    public Image fillImage;
    [Tooltip("The percentage text (e.g. shows 75%).")]
    public TMP_Text percentText;
    public TMP_Text statusText;
    public bool colorStatus = true;


    [Header("Behaviour")]
    public bool smooth = true;
    public float smoothSpeed = 2f;

    [Header("Fallback (no network manager)")]
    [Range(0f, 1f)] public float manualHealth01 = 0.5f;

    private float _displayed01 = -1f;

    void Update()
    {
        if (statusText == null) AutoWire();
        float target = GetHealth01();
        if (_displayed01 < 0f) _displayed01 = target;
        else if (smooth && Application.isPlaying)
            _displayed01 = Mathf.MoveTowards(_displayed01, target, smoothSpeed * Time.deltaTime);
        else
            _displayed01 = target;
        if (fillImage != null) fillImage.fillAmount = _displayed01;
        if (percentText != null) percentText.text = Mathf.RoundToInt(_displayed01 * 100f) + "%";
        if (statusText != null)
        {
            statusText.text = StatusWord(_displayed01);
            if (colorStatus) statusText.color = StatusColor(_displayed01);
        }
    }

    string StatusWord(float h)
    {
        if (h >= 0.85f) return "THRIVING";
        if (h >= 0.60f) return "HEALTHY";
        if (h >= 0.35f) return "UNSTABLE";
        if (h > 0.001f) return "CRITICAL";
        return "COLLAPSED";
    }

    Color StatusColor(float h)
    {
        if (h >= 0.60f) return new Color(0.35f, 0.9f, 0.5f);   // green
        if (h >= 0.35f) return new Color(1f, 0.75f, 0.2f);     // amber
        return new Color(0.95f, 0.3f, 0.3f);                   // red
    }

    float GetHealth01()
    {
        var mgr = EcosystemNetworkManagerGPU.Instance;
        if (mgr != null)
        {
            float h = mgr.GetEcoHealth();
            if (h > 1.001f) h /= 100f; // accept 0-100 too
            return Mathf.Clamp01(h);
        }
        return Mathf.Clamp01(manualHealth01);
    }


    void OnEnable() { AutoWire(); }

    void AutoWire()
    {
        // Self-heal references so a cleared Inspector slot can't break the widget.
        if (statusText == null)
        {
            var t = transform.Find("StatusText");
            if (t != null) statusText = t.GetComponent<TMP_Text>();
        }
    }
}
