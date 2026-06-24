using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Drives the DataChassis radial widget from the live eco-health value.
// - number text shows health as a percentage (e.g. "87%")
// - the radial Image fills proportionally to health (0-1)
// - reads from EcosystemNetworkManagerGPU.GetEcoHealth() (tablet/client),
//   falling back to a manual value if no network manager is present.
public class EcoHealthChassis : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("The radial fill Image (DataChassis ring). Set its Image type to Filled.")]
    public Image ringFill;
    [Tooltip("The big number text (percentage).")]
    public TMP_Text percentText;
    [Tooltip("Optional label text under the number.")]
    public TMP_Text labelText;

    [Header("Settings")]
    public string label = "ECO-HEALTH";
    [Tooltip("Smooth the ring/number toward the target instead of snapping.")]
    public bool smooth = true;
    public float smoothSpeed = 3f;

    [Header("Fallback (no network manager)")]
    [Range(0f, 1f)] public float manualHealth01 = 0.5f;

    private float _displayed01;

    void Start()
    {
        if (labelText != null) labelText.text = label;
        _displayed01 = GetHealth01();
        Apply(_displayed01);
    }

    void Update()
    {
        float target = GetHealth01();
        if (smooth)
            _displayed01 = Mathf.MoveTowards(_displayed01, target, smoothSpeed * Time.deltaTime);
        else
            _displayed01 = target;
        Apply(_displayed01);
    }

    float GetHealth01()
    {
        var mgr = EcosystemNetworkManagerGPU.Instance;
        if (mgr != null)
        {
            float h = mgr.GetEcoHealth();
            // Accept either 0-1 or 0-100 from the source.
            if (h > 1.001f) h /= 100f;
            return Mathf.Clamp01(h);
        }
        return Mathf.Clamp01(manualHealth01);
    }

    void Apply(float h01)
    {
        if (ringFill != null)
        {
            ringFill.fillAmount = h01;
            ringFill.color = HealthColor(h01);
        }
        if (percentText != null) percentText.text = Mathf.RoundToInt(h01 * 100f) + "%";
    }

    // red (low) -> amber (mid) -> green (high)
    Color HealthColor(float h01)
    {
        Color low = new Color(0.95f, 0.25f, 0.25f);   // red
        Color mid = new Color(1f, 0.75f, 0.2f);        // amber
        Color high = new Color(0.35f, 0.9f, 0.5f);     // green
        if (h01 < 0.5f)
            return Color.Lerp(low, mid, Mathf.Clamp01(h01 / 0.5f));
        return Color.Lerp(mid, high, Mathf.Clamp01((h01 - 0.5f) / 0.5f));
    }
}
