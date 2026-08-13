using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class TextGlowPulse : MonoBehaviour
{
    [Header("Glow")]
    public Color glowColor = new Color(0.35f, 0.80f, 1f, 1f);

        [Range(0f, 1f)] public float minPower = 0.15f;

        [Range(0f, 1f)] public float maxPower = 0.75f;

        [Range(0f, 1f)] public float glowOuter = 0.35f;

        public float period = 2.2f;

    [Header("Optional scale breath")]
        public bool pulseScale = false;
    [Range(1f, 1.1f)] public float scalePeak = 1.02f;

    private TMP_Text _text;
    private Material _mat;
    private Vector3 _baseScale;
    private bool _ready;

    void Awake()
    {
        _text = GetComponent<TMP_Text>();
        _baseScale = transform.localScale;
    }

    void OnEnable()
    {
        if (!Application.isPlaying) return;

        _mat = _text.fontMaterial;
        if (_mat == null) return;

        _mat.EnableKeyword("GLOW_ON");
        _mat.SetColor(ShaderUtilities.ID_GlowColor, glowColor);
        _mat.SetFloat(ShaderUtilities.ID_GlowOuter, glowOuter);
        _mat.SetFloat(ShaderUtilities.ID_GlowInner, 0.05f);
        _mat.SetFloat(ShaderUtilities.ID_GlowOffset, 0f);
        _ready = true;
    }

    void OnDisable()
    {

        if (_ready && _mat != null) _mat.SetFloat(ShaderUtilities.ID_GlowPower, minPower);
        if (pulseScale) transform.localScale = _baseScale;
    }

    void Update()
    {
        if (!_ready || _mat == null) return;

        float t = 0.5f - 0.5f * Mathf.Cos(Time.unscaledTime * (2f * Mathf.PI / Mathf.Max(0.01f, period)));

        _mat.SetFloat(ShaderUtilities.ID_GlowPower, Mathf.Lerp(minPower, maxPower, t));

        if (pulseScale)
            transform.localScale = _baseScale * Mathf.Lerp(1f, scalePeak, t);
    }
}
