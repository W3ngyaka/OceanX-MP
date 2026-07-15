using UnityEngine;
using UnityEngine.UI;
using TMPro;using OceanX.BoidsGPU.Ecosystem;


/// <summary>
/// Drives the Boids_Demo health bar from the live ecosystem health value
/// (EcosystemSimulationGPU.EcoHealth01, normalized 0..1).
/// Updates a Filled Image's fillAmount and an optional percent TMP label.
/// </summary>
[ExecuteAlways]
public class HealthBarBinder : MonoBehaviour
{
    [Header("Source (auto-found if left empty)")]
    [SerializeField] private EcosystemSimulationGPU sim;

    [Header("UI targets (auto-found from children if left empty)")]
    [SerializeField] private Image fillImage;     // the Filled (Horizontal) image
    [SerializeField] private TMP_Text percentText; // e.g. "79%"

    [Header("Options")]
    [SerializeField] private bool smooth = true;
    [SerializeField] private float smoothSpeed = 4f;

    [Header("Debug / preview")]
    [Tooltip("Ignore the live sim and use the slider below. For testing colours/fill in the Inspector.")]
    [SerializeField] private bool debugOverride = false;
    [Range(0f, 1f)]
    [SerializeField] private float debugHealth01 = 1f;

    [Header("Colour by health state")]
    [Tooltip("Tint the fill green -> amber -> red as health drops. Needs a white/neutral fill sprite so the tint shows.")]
    [SerializeField] private bool colorFill = true;
    [Tooltip("Optional: also tint the percent label to match.")]
    [SerializeField] private bool colorPercentText = false;

    private float _displayed;

    void Awake()
    {
        if (sim == null) sim = FindFirstObjectByType<EcosystemSimulationGPU>();

        if (fillImage == null)
        {
            foreach (var img in GetComponentsInChildren<Image>(true))
            {
                if (img.type == Image.Type.Filled) { fillImage = img; break; }
            }
        }

        if (percentText == null)
        {
            // pick the TMP child whose text looks like a percentage, else last TMP
            TMP_Text fallback = null;
            foreach (var t in GetComponentsInChildren<TMP_Text>(true))
            {
                fallback = t;
                if (t.text != null && t.text.Contains("%")) { percentText = t; break; }
            }
            if (percentText == null) percentText = fallback;
        }

        _displayed = fillImage != null ? fillImage.fillAmount : 0f;
    }

    void Update()
    {
        if (sim == null && !debugOverride)
        {
            sim = FindFirstObjectByType<EcosystemSimulationGPU>();
            if (sim == null) return;
        }

        float target = debugOverride ? Mathf.Clamp01(debugHealth01) : Mathf.Clamp01(sim.EcoHealth01);

        if (smooth && Application.isPlaying)
        {
            // Exponential ease-out: frame-rate independent, decelerates near target.
            float k = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
            _displayed = Mathf.Lerp(_displayed, target, k);
            if (Mathf.Abs(_displayed - target) < 0.001f) _displayed = target;
        }
        else
        {
            _displayed = target;
        }

        if (fillImage != null)
        {
            fillImage.fillAmount = _displayed;
            if (colorFill) fillImage.color = HealthColor(_displayed);
        }
        if (percentText != null)
        {
            percentText.text = Mathf.RoundToInt(_displayed * 100f).ToString() + "%";
            if (colorPercentText) percentText.color = HealthColor(_displayed);
        }
    }

    // Smooth green -> amber -> red gradient (matches EcoHealthDashboard).
    static readonly Color HealthRed   = new Color(0.95f, 0.30f, 0.30f);
    static readonly Color HealthAmber = new Color(1f,    0.75f, 0.20f);
    static readonly Color HealthGreen = new Color(0.35f, 0.90f, 0.50f);

    Color HealthColor(float h)
    {
        if (h <= 0.5f) return Color.Lerp(HealthRed, HealthAmber, Mathf.Clamp01(h / 0.5f));
        return Color.Lerp(HealthAmber, HealthGreen, Mathf.Clamp01((h - 0.5f) / 0.5f));
    }
}
