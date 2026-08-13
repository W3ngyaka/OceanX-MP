using UnityEngine;
using UnityEngine.UI;
using TMPro;using OceanX.BoidsGPU.Ecosystem;

[ExecuteAlways]
public class HealthBarBinder : MonoBehaviour
{
    [Header("Source (auto-found if left empty)")]
    [SerializeField] private EcosystemSimulationGPU sim;

    [Header("UI targets (auto-found from children if left empty)")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text percentText;

    [Header("Options")]
    [SerializeField] private bool smooth = true;
    [SerializeField] private float smoothSpeed = 4f;

    [Header("Debug / preview")]
        [SerializeField] private bool debugOverride = false;
    [Range(0f, 1f)]
    [SerializeField] private float debugHealth01 = 1f;

    [Header("Colour by health state")]
        [SerializeField] private bool colorFill = true;
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

    static readonly Color HealthRed   = new Color(0.95f, 0.30f, 0.30f);
    static readonly Color HealthAmber = new Color(1f,    0.75f, 0.20f);
    static readonly Color HealthGreen = new Color(0.35f, 0.90f, 0.50f);

    Color HealthColor(float h)
    {
        if (h <= 0.5f) return Color.Lerp(HealthRed, HealthAmber, Mathf.Clamp01(h / 0.5f));
        return Color.Lerp(HealthAmber, HealthGreen, Mathf.Clamp01((h - 0.5f) / 0.5f));
    }
}
