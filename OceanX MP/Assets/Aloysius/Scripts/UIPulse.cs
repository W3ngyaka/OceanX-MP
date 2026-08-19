using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPulse : MonoBehaviour
{
    public enum Mode { Scale, Glow }
    public Mode mode = Mode.Scale;

    [Header("Scale mode")]
    public float pulseScale = 1.08f;

    [Header("Glow mode (brightness + alpha pulse)")]
        public float minAlpha = 0.55f;
        [Range(0f,1f)] public float brighten = 0.35f;

    [Header("Common")]
    public float speed = 1.2f;

    private Vector3 _baseScale;
    private Graphic[] _graphics;
    private Color[] _baseColors;

    void OnEnable()
    {
        _baseScale = transform.localScale;
        _graphics = GetComponentsInChildren<Graphic>(true);
        _baseColors = new Color[_graphics.Length];
        for (int i = 0; i < _graphics.Length; i++) _baseColors[i] = _graphics[i].color;
    }

    void OnDisable()
    {
        transform.localScale = _baseScale;
        if (_graphics != null)
            for (int i = 0; i < _graphics.Length; i++)
                if (_graphics[i] != null) _graphics[i].color = _baseColors[i];
    }

    void Update()
    {
        float s = (Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f) + 1f) * 0.5f;

        if (mode == Mode.Scale)
        {
            transform.localScale = _baseScale * Mathf.Lerp(1f, pulseScale, s);
        }
        else
        {
            for (int i = 0; i < _graphics.Length; i++)
            {
                if (_graphics[i] == null) continue;
                Color b = _baseColors[i];

                Color bright = Color.Lerp(b, Color.white, brighten);
                Color c = Color.Lerp(b, bright, s);
                c.a = Mathf.Lerp(minAlpha, b.a, s);
                _graphics[i].color = c;
            }
        }
    }
}
