using UnityEngine;
using TMPro;

// Soft glow pulse for a TMP label.
//
// Works on the material INSTANCE (TMP_Text.fontMaterial), never the shared asset. The tablet's
// Rajdhani materials are shared by ~28 labels, so animating fontSharedMaterial would set the whole
// UI throbbing. Touching fontMaterial in edit mode leaks material instances into the scene, so this
// is deliberately play-mode only.
[RequireComponent(typeof(TMP_Text))]
public class TextGlowPulse : MonoBehaviour
{
    [Header("Glow")]
    public Color glowColor = new Color(0.35f, 0.80f, 1f, 1f);

    [Tooltip("Glow strength at the dimmest point of the pulse.")]
    [Range(0f, 1f)] public float minPower = 0.15f;

    [Tooltip("Glow strength at the brightest point.")]
    [Range(0f, 1f)] public float maxPower = 0.75f;

    [Tooltip("How far the glow spreads outward from the glyph edge.")]
    [Range(0f, 1f)] public float glowOuter = 0.35f;

    [Tooltip("Seconds for one full dim-bright-dim cycle.")]
    public float period = 2.2f;

    [Header("Optional scale breath")]
    [Tooltip("Adds a very slight size pulse in time with the glow. Subtle is the point: anything " +
             "over ~1.03 reads as the text wobbling rather than glowing.")]
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

        // fontMaterial returns a per-object instance, created on first access.
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
        // Park at the dim end rather than leaving it frozen mid-flash.
        if (_ready && _mat != null) _mat.SetFloat(ShaderUtilities.ID_GlowPower, minPower);
        if (pulseScale) transform.localScale = _baseScale;
    }

    void Update()
    {
        if (!_ready || _mat == null) return;

        // 0..1 triangle-ish via sine, unscaled so it keeps breathing if the exhibit ever pauses.
        float t = 0.5f - 0.5f * Mathf.Cos(Time.unscaledTime * (2f * Mathf.PI / Mathf.Max(0.01f, period)));

        _mat.SetFloat(ShaderUtilities.ID_GlowPower, Mathf.Lerp(minPower, maxPower, t));

        if (pulseScale)
            transform.localScale = _baseScale * Mathf.Lerp(1f, scalePeak, t);
    }
}
