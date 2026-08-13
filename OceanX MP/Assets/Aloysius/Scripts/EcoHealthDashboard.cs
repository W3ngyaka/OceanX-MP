using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class EcoHealthDashboard : MonoBehaviour
{
    [Header("Refs")]
        public Image fillImage;
        public TMP_Text percentText;
    public TMP_Text statusText;
    public bool colorStatus = true;
        public bool colorFill = true;
        public TMP_Text speciesCountText;
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
        if (percentText != null) percentText.text = Mathf.RoundToInt(target * 100f) + "%";
        if (statusText != null)
        {
            statusText.text = StatusWord(target);
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
        if (h >= 0.60f) return new Color(0.35f, 0.9f, 0.5f);
        if (h >= 0.35f) return new Color(1f, 0.75f, 0.2f);
        return new Color(0.95f, 0.3f, 0.3f);
    }

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
            if (h > 1.001f) h /= 100f;
            return Mathf.Clamp01(h);
        }
        return Mathf.Clamp01(manualHealth01);
    }

    void OnEnable() { AutoWire(); }

    void AutoWire()
    {

        if (statusText == null)
        {
            var t = transform.Find("StatusText");
            if (t != null) statusText = t.GetComponent<TMP_Text>();
        }
        if (speciesCountText == null)
        {

            var p = transform.parent;
            if (p != null)
            {
                var t = p.Find("SpeciesCountText");
                if (t != null) speciesCountText = t.GetComponent<TMP_Text>();
            }
        }
    }
}
