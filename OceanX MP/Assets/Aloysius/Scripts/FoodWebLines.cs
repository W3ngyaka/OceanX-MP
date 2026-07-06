using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class FoodWebLines : MonoBehaviour
{
    public static FoodWebLines Instance;
    public GameObject linePrefab;

    public List<GameObject> activeLines = new List<GameObject>();
    public List<Image> glowRings = new List<Image>();

    [Header("Energy flow animation")]
    [Tooltip("Animate dots drifting prey -> predator along each revealed line.")]
    public bool animateFlow = true;
    [Tooltip("How fast a dot travels the line, in fractions of the line per second.")]
    public float flowSpeed = 0.35f;
    [Tooltip("Dots per line (spaced evenly).")]
    public int dotsPerLine = 3;
    [Tooltip("Dot diameter in pixels.")]
    public float dotSize = 14f;

    private List<CanvasGroup> dimmedBubbles = new List<CanvasGroup>();

    // One flow per drawn line: the curve it rides plus its dot images.
    private class Flow { public Vector2 a, c, b; public Image[] dots; }
    private readonly List<Flow> _flows = new List<Flow>();
    private float _flowT;
    private Sprite _dotSprite;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (!animateFlow || _flows.Count == 0) return;

        _flowT += Time.unscaledDeltaTime * flowSpeed;
        foreach (var f in _flows)
        {
            if (f.dots == null) continue;
            for (int i = 0; i < f.dots.Length; i++)
            {
                var img = f.dots[i];
                if (img == null) continue;

                // Evenly-offset dots looping along the curve, prey (t=0) -> predator (t=1).
                float t = Mathf.Repeat(_flowT + i / (float)f.dots.Length, 1f);
                img.rectTransform.position = Bezier(f.a, f.c, f.b, t);

                // Fade in as it leaves the prey and out as it reaches the predator.
                const float edge = 0.18f;
                float a = Mathf.Clamp01(Mathf.Min(t, 1f - t) / edge);
                var col = img.color; col.a = a; img.color = col;
            }
        }
    }

    public void ShowConnections(SpeciesBubble source)
    {
        Debug.Log($"ShowConnections: {source.name}, prey={source.prey.Count}, predators={source.predators.Count}");

        // build connected set
        var connected = new HashSet<SpeciesBubble>();
        connected.Add(source);
        foreach (var p in source.prey) if (p != null) connected.Add(p);
        foreach (var p in source.predators) if (p != null) connected.Add(p);

        // dim all unconnected bubbles using CanvasGroup
        dimmedBubbles.Clear();
        var allBubbles = FindObjectsByType<SpeciesBubble>(FindObjectsSortMode.None);
        foreach (var b in allBubbles)
        {
            if (!connected.Contains(b))
            {
                CanvasGroup cg = b.GetComponent<CanvasGroup>();
                if (cg == null) cg = b.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0.15f;
                dimmedBubbles.Add(cg);
            }
        }

        // Draw each link in the direction energy flows — from the eaten toward the eater —
        // with an arrowhead at the tip. The source is eaten by its predators (source -> predator)
        // and eats its prey (prey -> source), so the arrow always points at the predator.
        Color lineColor = new Color(0f, 0.85f, 1f, 1f);

        foreach (var predator in source.predators)
            if (predator != null)
                DrawLine(source.transform, predator.transform, lineColor);

        foreach (var prey in source.prey)
            if (prey != null)
                DrawLine(prey.transform, source.transform, lineColor);

        Debug.Log($"activeLines after draw: {activeLines.Count}");
    }

    public void HideConnections()
    {
        Debug.Log($"HideConnections: {activeLines.Count} lines");

        // restore all dimmed bubbles
        foreach (var cg in dimmedBubbles)
            if (cg != null) cg.alpha = 1f;
        dimmedBubbles.Clear();

        // destroy lines (dots are registered in activeLines too, so they go with them)
        for (int i = activeLines.Count - 1; i >= 0; i--)
            if (activeLines[i] != null)
                DestroyImmediate(activeLines[i]);
        activeLines.Clear();
        _flows.Clear();

        // hide glow rings
        foreach (var ring in glowRings)
            if (ring != null) ring.enabled = false;
        glowRings.Clear();
    }

    void DrawLine(Transform from, Transform to, Color color)
    {
        if (linePrefab == null) { Debug.LogError("linePrefab is null!"); return; }

        Vector2 start = from.position;
        Vector2 end = to.position;
        Vector2 dir = end - start;
        float dist = dir.magnitude;
        if (dist < 0.001f) return;
        Vector2 unit = dir / dist;

        // Trim the endpoints back to each bubble's rim so the line touches the edge
        // instead of running under the fish art (only if there's room between them).
        float rFrom = BubbleRadius(from);
        float rTo = BubbleRadius(to);
        if (rFrom + rTo < dist)
        {
            start += unit * rFrom;
            end -= unit * rTo;
            dir = end - start;
            dist = dir.magnitude;
            if (dist < 0.001f) return;
            unit = dir / dist;
        }

        Vector2 mid = (start + end) / 2f;
        Vector2 perp = new Vector2(-unit.y, unit.x);

        // Gather the other bubbles (exclude the two endpoints) with their radii so we
        // can keep the line clear of them.
        var obstacles = new List<Vector2>();
        var radii = new List<float>();
        var allBubbles = FindObjectsByType<SpeciesBubble>(FindObjectsSortMode.None);
        foreach (var b in allBubbles)
        {
            if (b.transform == from || b.transform == to) continue;
            obstacles.Add(b.transform.position);
            radii.Add(BubbleRadius(b.transform));
        }

        // Keep the line straight whenever the direct path is already clear; only bow —
        // by the smallest amount that works — when a bubble is actually in the way.
        const float margin = 12f;
        Vector2 control = mid;                                   // bow = 0 -> straight line
        float bestPenalty = PathPenalty(start, control, end, obstacles, radii, margin);
        if (bestPenalty > 0f)
        {
            float maxBow = Mathf.Clamp(dist * 0.6f, 40f, 500f);
            for (float bow = 30f; bow <= maxBow; bow += 30f)
            {
                Vector2 cA = mid + perp * bow;
                Vector2 cB = mid - perp * bow;
                float pA = PathPenalty(start, cA, end, obstacles, radii, margin);
                float pB = PathPenalty(start, cB, end, obstacles, radii, margin);
                if (pA < bestPenalty) { bestPenalty = pA; control = cA; }
                if (pB < bestPenalty) { bestPenalty = pB; control = cB; }
                if (bestPenalty <= 0f) break;                    // fully clear — stop growing the bow
            }
        }

        int segments = 20;
        Vector2 prev = start;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector2 point = Bezier(start, control, end, t);
            DrawSegment(prev, point, color);
            prev = point;
        }

        // Arrowhead at the tip, aligned with the curve's tangent there (points at the predator).
        Vector2 tangent = end - control;                        // quadratic Bezier tangent at t = 1
        if (tangent.sqrMagnitude < 0.0001f) tangent = unit;
        AddArrowHead(end, tangent.normalized, color);

        // Energy dots riding this curve from prey (start) toward predator (end).
        if (animateFlow && dotsPerLine > 0)
            SpawnFlow(start, control, end, color);
    }

    // Create the dots for one line and register them for animation in Update.
    void SpawnFlow(Vector2 a, Vector2 c, Vector2 b, Color color)
    {
        var dots = new Image[dotsPerLine];
        for (int i = 0; i < dotsPerLine; i++)
        {
            var go = new GameObject("FlowDot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.sizeDelta = new Vector2(dotSize, dotSize);

            var img = go.GetComponent<Image>();
            img.sprite = DotSprite();
            img.color = new Color(color.r, color.g, color.b, 0f); // Update sets alpha
            img.raycastTarget = false;
            rt.position = Bezier(a, c, b, i / (float)dotsPerLine);

            activeLines.Add(go); // so HideConnections cleans it up
            dots[i] = img;
        }
        _flows.Add(new Flow { a = a, c = c, b = b, dots = dots });
    }

    // A soft round dot, generated once at runtime so there's no asset to manage.
    Sprite DotSprite()
    {
        if (_dotSprite != null) return _dotSprite;

        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float r = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a)); // soft falloff
            }
        tex.Apply();
        _dotSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _dotSprite;
    }

    // Two short barbs forming a "V" at the tip. dir = unit direction of travel toward the tip.
    void AddArrowHead(Vector2 tip, Vector2 dir, Color color)
    {
        const float len = 16f;
        const float ang = 28f * Mathf.Deg2Rad;
        Vector2 back = -dir;
        DrawSegment(tip, tip + Rotate(back, ang) * len, color);
        DrawSegment(tip, tip + Rotate(back, -ang) * len, color);
    }

    Vector2 Rotate(Vector2 v, float rad)
    {
        float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    // How far the curve intrudes into any bubble (sum of overlaps). 0 = the path is clear.
    float PathPenalty(Vector2 a, Vector2 c, Vector2 b, List<Vector2> obstacles, List<float> radii, float margin)
    {
        if (obstacles.Count == 0) return 0f;
        float penalty = 0f;
        int samples = 12;
        for (int i = 1; i < samples; i++)
        {
            float t = i / (float)samples;
            Vector2 p = Bezier(a, c, b, t);
            for (int o = 0; o < obstacles.Count; o++)
            {
                float need = radii[o] + margin;
                float d = Vector2.Distance(p, obstacles[o]);
                if (d < need) penalty += need - d;
            }
        }
        return penalty;
    }

    Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
    }

    void DrawSegment(Vector2 p1, Vector2 p2, Color color)
    {
        GameObject line = Instantiate(linePrefab, transform);
        line.SetActive(true);
        activeLines.Add(line);

        RectTransform rt = line.GetComponent<RectTransform>();
        Image img = line.GetComponent<Image>();
        img.color = color;

        Vector2 d = p2 - p1;
        float len = d.magnitude;
        rt.position = (p1 + p2) / 2f;
        rt.sizeDelta = new Vector2(len + 1f, 4f);
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        rt.rotation = Quaternion.Euler(0, 0, angle);
    }

    // Approximate a bubble's on-screen radius from its RectTransform.
    float BubbleRadius(Transform bubble)
    {
        var brt = bubble as RectTransform;
        if (brt == null) brt = bubble.GetComponent<RectTransform>();
        if (brt == null) return 0f;
        // half the smaller world-space dimension, with a little extra gap
        Vector3[] corners = new Vector3[4];
        brt.GetWorldCorners(corners);
        float w = Vector3.Distance(corners[0], corners[3]);
        float h = Vector3.Distance(corners[0], corners[1]);
        return Mathf.Min(w, h) * 0.5f + 8f;
    }

    void HighlightBubble(SpeciesBubble bubble, bool bright)
    {
        if (bubble.glowRing == null) return;
        Image ring = bubble.glowRing.GetComponent<Image>();
        if (ring == null) return;
        ring.enabled = true;
        ring.color = bright ? new Color(0f, 0.85f, 1f, 1f) : new Color(0f, 0.85f, 1f, 0.4f);
        glowRings.Add(ring);
    }
}
