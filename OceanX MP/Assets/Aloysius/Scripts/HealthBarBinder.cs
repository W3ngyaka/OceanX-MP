using UnityEngine;
using UnityEngine.UI;
using TMPro;using OceanX.BoidsGPU.Ecosystem;


/// <summary>
/// Drives the Boids_Demo health bar from the live ecosystem health value
/// (EcosystemSimulationGPU.EcoHealth01, normalized 0..1).
/// Updates a Filled Image's fillAmount and an optional percent TMP label.
/// </summary>
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
    [SerializeField] private string percentFormat = "0"; // "0" -> 79, "0.0" -> 78.6

    private float _displayed;

    void Awake()
    {
        if (sim == null) sim = FindObjectOfType<EcosystemSimulationGPU>();

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
        if (sim == null)
        {
            sim = FindObjectOfType<EcosystemSimulationGPU>();
            if (sim == null) return;
        }

        float target = Mathf.Clamp01(sim.EcoHealth01);

        if (smooth)
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

        if (fillImage != null) fillImage.fillAmount = _displayed;
        if (percentText != null)
            percentText.text = Mathf.RoundToInt(_displayed * 100f).ToString() + "%";
    }
}
