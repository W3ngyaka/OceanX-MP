using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Marine snow: faint particles drifting slowly through the water for depth/atmosphere.
// Attach to a full-size UI RectTransform behind the bubbles (stretched to fill the panel).
// Self-contained: generates its own soft-dot sprite at runtime, no assets needed.
[RequireComponent(typeof(RectTransform))]
public class Bubbles : MonoBehaviour
{
    [Header("Amount")]
    [Tooltip("How many particles exist at once.")]
    public int particleCount = 60;

    [Header("Drift")]
    [Tooltip("Downward fall speed (pixels/sec). Negative would rise.")]
    public float fallSpeed = 14f;
    [Tooltip("Sideways sway amount in pixels.")]
    public float swayAmount = 18f;
    [Tooltip("How fast the sway oscillates.")]
    public float swaySpeed = 0.5f;

    [Header("Size & Look")]
    public float minSize = 2f;
    public float maxSize = 7f;
    [Tooltip("Particle colour. Alpha sets max opacity (kept low for subtlety).")]
    public Color color = new Color(0.8f, 0.95f, 1f, 0.25f);

    private RectTransform _rt;
    private Sprite _dot;
    private readonly List<Flake> _flakes = new List<Flake>();
    private float _w, _h;

    private class Flake
    {
        public RectTransform rt;
        public Image img;
        public float baseX;      // horizontal anchor before sway
        public float swayPhase;  // offset so they don't sway in unison
        public float swayMul;    // per-flake sway scale
        public float speedMul;   // per-flake fall speed variation
        public float alpha;      // per-flake opacity
    }

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _dot = BuildDotSprite(64);
    }

    void Start()
    {
        var size = _rt.rect.size;
        _w = size.x; _h = size.y;
        for (int i = 0; i < particleCount; i++)
            _flakes.Add(MakeFlake(randomY: true));
    }

    void Update()
    {
        var size = _rt.rect.size;
        _w = size.x; _h = size.y;
        float halfW = _w * 0.5f;
        float halfH = _h * 0.5f;

        foreach (var f in _flakes)
        {
            var p = f.rt.anchoredPosition;
            p.y += fallSpeed * f.speedMul * Time.deltaTime;   // rise upward (fallSpeed reused as rise)
            float sway = Mathf.Sin(Time.time * swaySpeed + f.swayPhase) * swayAmount * f.swayMul;
            p.x = f.baseX + sway;

            // Recycle to the bottom once it rises off the top.
            if (p.y > halfH + 10f)
            {
                f.baseX = Random.Range(-halfW, halfW);
                p.x = f.baseX;
                p.y = -halfH - 10f;
            }
            f.rt.anchoredPosition = p;
        }
    }

    Flake MakeFlake(bool randomY)
    {
        var go = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        float s = Random.Range(minSize, maxSize);
        rt.sizeDelta = new Vector2(s, s);

        float halfW = _w * 0.5f;
        float halfH = _h * 0.5f;
        float x = Random.Range(-halfW, halfW);
        float y = randomY ? Random.Range(-halfH, halfH) : -halfH - 10f;
        rt.anchoredPosition = new Vector2(x, y);

        var img = go.GetComponent<Image>();
        img.sprite = _dot;
        img.raycastTarget = false;
        float a = color.a * Random.Range(0.5f, 1f);
        img.color = new Color(color.r, color.g, color.b, a);

        return new Flake
        {
            rt = rt,
            img = img,
            baseX = x,
            swayPhase = Random.Range(0f, Mathf.PI * 2f),
            swayMul = Random.Range(0.5f, 1.4f),
            speedMul = Random.Range(0.6f, 1.6f),
            alpha = a
        };
    }

    // Soft round dot: white, fading to transparent at the edge.
    Sprite BuildDotSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float c = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - c + 0.5f;
                float dy = y - c + 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / c; // 0 centre -> 1 edge
                if (d > 1f) { tex.SetPixel(x, y, new Color(1, 1, 1, 0)); continue; }
                float rim = Mathf.Clamp01(1f - Mathf.Abs(d - 0.88f) / 0.16f); // bright outer rim
                float fill = Mathf.Clamp01(1f - d) * 0.18f;                    // faint interior
                float a = Mathf.Clamp01(rim * 0.9f + fill);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
