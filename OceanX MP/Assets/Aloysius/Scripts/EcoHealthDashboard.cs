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

    [Header("Behaviour")]
    public bool smooth = true;
    public float smoothSpeed = 2f;

    [Header("Fallback (no network manager)")]
    [Range(0f, 1f)] public float manualHealth01 = 0.5f;

    private float _displayed01 = -1f;

    void Update()
    {
        float target = GetHealth01();
        if (_displayed01 < 0f) _displayed01 = target; // first frame: snap
        else if (smooth && Application.isPlaying)
            _displayed01 = Mathf.MoveTowards(_displayed01, target, smoothSpeed * Time.deltaTime);
        else
            _displayed01 = target;

        if (fillImage != null) fillImage.fillAmount = _displayed01;
        if (percentText != null) percentText.text = Mathf.RoundToInt(_displayed01 * 100f) + "%";
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
}
