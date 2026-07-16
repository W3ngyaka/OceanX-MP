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
    [Tooltip("Tint the arc fill itself green->amber->red by health. Requires a white/neutral " +
             "arc sprite so the tint shows (a pre-colored green sprite can't be tinted red).")]
    public bool colorFill = true;
    [Tooltip("Optional 'X / 12 species present' text.")]
    public TMP_Text speciesCountText;
    [Tooltip("Total species in the ecosystem (denominator).")]
    public int totalSpecies = 12;



    [Header("Behaviour")]
    public bool smooth = true;
    public float smoothSpeed = 8f;

    [Header("Fallback (no network manager)")]
    [Range(0f, 1f)] public float manualHealth01 = 0.5f;

    private float _displayed01 = -1f;

    void Update()
    {
        if (statusText == null) AutoWire();
        float target = GetHealth01();
        if (_displayed01 < 0f) _displayed01 = target;
        else if (smooth && Application.isPlaying)
            {
                // Exponential (frame-rate-independent) easing — matches the large-screen
                // HealthBarBinder so the tablet gauge feels identical: glides in and
                // decelerates toward the target instead of the abrupt linear MoveTowards.
                float k = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
                _displayed01 = Mathf.Lerp(_displayed01, target, k);
            }
        else
            _displayed01 = target;
        if (fillImage != null)
        {
            fillImage.fillAmount = _displayed01;
            if (colorFill) fillImage.color = HealthColor(_displayed01);
        }
        if (percentText != null) percentText.text = Mathf.RoundToInt(target * 100f) + "%";  // instant, unsmoothed
        if (statusText != null)
        {
            statusText.text = StatusWord(target);          // instant, matches the number
            if (colorStatus) statusText.color = StatusColor(target);
        }
        if (speciesCountText != null)
            speciesCountText.text = CountPresent() + " / " + totalSpecies + " species present";
    }

    int CountPresent()
    {
        var mgr = EcosystemNetworkManagerGPU.Instance;
        if (mgr == null) return 0;
        int present = 0;
        for (int i = 0; i < totalSpecies; i++)
            if (mgr.GetPopulation(i) > 0) present++;
        return present;
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

    // Smooth green -> amber -> red gradient for the arc fill, so it eases into red as health drops.
    static readonly Color HealthRed   = new Color(0.95f, 0.30f, 0.30f);
    static readonly Color HealthAmber = new Color(1f, 0.75f, 0.20f);
    static readonly Color HealthGreen = new Color(0.35f, 0.90f, 0.50f);
    Color HealthColor(float h)
    {
        if (h <= 0.5f) return Color.Lerp(HealthRed, HealthAmber, Mathf.Clamp01(h / 0.5f));
        return Color.Lerp(HealthAmber, HealthGreen, Mathf.Clamp01((h - 0.5f) / 0.5f));
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
        if (speciesCountText == null)
        {
            // count text lives on the Health parent, not under the dashboard
            var p = transform.parent;
            if (p != null)
            {
                var t = p.Find("SpeciesCountText");
                if (t != null) speciesCountText = t.GetComponent<TMP_Text>();
            }
        }
    }
}
