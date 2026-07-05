using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class FoodWebLines : MonoBehaviour
{
    public static FoodWebLines Instance;
    public GameObject linePrefab;

    public List<GameObject> activeLines = new List<GameObject>();
    public List<Image> glowRings = new List<Image>();

    private List<CanvasGroup> dimmedBubbles = new List<CanvasGroup>();

    void Awake()
    {
        Instance = this;
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

        // destroy lines
        for (int i = activeLines.Count - 1; i >= 0; i--)
            if (activeLines[i] != null)
                DestroyImmediate(activeLines[i]);
        activeLines.Clear();

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
