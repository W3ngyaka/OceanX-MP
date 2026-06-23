using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// God rays: soft light shafts fanning down from the surface (top of the panel).
// Attach to a full-size UI RectTransform behind the fish. Self-contained: builds its
// own soft vertical-gradient sprite at runtime, no assets needed.
[RequireComponent(typeof(RectTransform))]
public class GodRays : MonoBehaviour
{
    [Header("Rays")]
    [Tooltip("How many light shafts.")]
    public int rayCount = 5;
    [Tooltip("Width of each shaft in pixels (at the top).")]
    public float rayWidth = 120f;
    [Tooltip("How tall the shafts are, as a fraction of panel height (0-1).")]
    [Range(0.3f, 1f)] public float rayHeightFraction = 0.85f;
    [Tooltip("Tilt of the shafts in degrees (they fan diagonally).")]
    public float tilt = 12f;

    [Header("Look")]
    [Tooltip("Ray colour. Alpha is the peak opacity (keep low and subtle).")]
    public Color color = new Color(0.85f, 0.95f, 1f, 0.12f);

    [Header("Motion")]
    [Tooltip("How far each shaft sways horizontally (pixels).")]
    public float swayAmount = 30f;
    [Tooltip("Sway speed.")]
    public float swaySpeed = 0.25f;
    [Tooltip("How much the opacity shimmers (0 = none).")]
    [Range(0f, 1f)] public float shimmer = 0.4f;

    private RectTransform _rt;
    private Sprite _shaftSprite;
    private readonly List<Ray> _rays = new List<Ray>();

    private class Ray
    {
        public RectTransform rt;
        public Image img;
        public float baseX;
        public float phase;
        public float baseAlpha;
    }

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _shaftSprite = BuildShaftSprite(32, 256);
    }

    void Start()
    {
        var size = _rt.rect.size;
        float w = size.x, h = size.y;
        float spacing = w / (rayCount + 1);
        for (int i = 0; i < rayCount; i++)
        {
            float x = -w * 0.5f + spacing * (i + 1);
            _rays.Add(MakeRay(x, w, h, i));
        }
    }

    void Update()
    {
        foreach (var r in _rays)
        {
            float sway = Mathf.Sin(Time.time * swaySpeed + r.phase) * swayAmount;
            var p = r.rt.anchoredPosition;
            p.x = r.baseX + sway;
            r.rt.anchoredPosition = p;

            if (shimmer > 0f)
            {
                float s = 1f - shimmer * 0.5f * (1f + Mathf.Sin(Time.time * swaySpeed * 1.7f + r.phase));
                var c = r.img.color;
                c.a = r.baseAlpha * Mathf.Clamp01(s);
                r.img.color = c;
            }
        }
    }

    Ray MakeRay(float x, float w, float h, int i)
    {
        var go = new GameObject("Ray", typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(transform, false);
        // Anchor to top center; pivot at top so it hangs down from the surface.
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        float rh = h * rayHeightFraction;
        rt.sizeDelta = new Vector2(rayWidth * Random.Range(0.7f, 1.4f), rh);
        rt.anchoredPosition = new Vector2(x, 20f);
        rt.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0 ? tilt : -tilt) * Random.Range(0.6f, 1.2f));

        var img = go.GetComponent<Image>();
        img.sprite = _shaftSprite;
        img.raycastTarget = false;
        float a = color.a * Random.Range(0.6f, 1.1f);
        img.color = new Color(color.r, color.g, color.b, a);

        return new Ray { rt = rt, img = img, baseX = x, phase = Random.Range(0f, 6.28f), baseAlpha = a };
    }

    // Vertical shaft: brightest at top, fading to nothing at the bottom, and soft on the sides.
    Sprite BuildShaftSprite(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float cx = w / 2f;
        for (int y = 0; y < h; y++)
        {
            // y=0 is bottom in texture space; we want bright at top (y = h-1).
            float vTop = (float)y / (h - 1);          // 0 bottom -> 1 top
            float vertical = vTop * vTop;             // fade out toward bottom
            for (int x = 0; x < w; x++)
            {
                float hx = 1f - Mathf.Abs(x - cx + 0.5f) / cx; // 1 center -> 0 edge
                hx = Mathf.Clamp01(hx);
                hx = hx * hx;                          // soft sides
                float a = vertical * hx;
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1f), 100f);
    }
}
