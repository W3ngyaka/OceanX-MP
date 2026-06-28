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

        // draw lines
        foreach (var predator in source.predators)
            if (predator != null)
                DrawLine(source.transform, predator.transform, new Color(0f, 0.85f, 1f, 1f));

        foreach (var prey in source.prey)
            if (prey != null)
                DrawLine(source.transform, prey.transform, new Color(0f, 0.85f, 1f, 0.6f));

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
        Vector2 mid = (start + end) / 2f;

        // Perpendicular directions to bow toward.
        Vector2 perp = new Vector2(-unit.y, unit.x);
        float bow = Mathf.Clamp(dist * 0.42f, 60f, 400f);

        // Gather other bubbles (exclude the two endpoints) to test clearance against.
        var obstacles = new List<Vector2>();
        var allBubbles = FindObjectsByType<SpeciesBubble>(FindObjectsSortMode.None);
        foreach (var b in allBubbles)
        {
            if (b.transform == from || b.transform == to) continue;
            obstacles.Add(b.transform.position);
        }

        // Try bowing each way; pick the side whose curve stays farthest from obstacles.
        Vector2 controlA = mid + perp * bow;
        Vector2 controlB = mid - perp * bow;
        float clearA = MinClearance(start, controlA, end, obstacles);
        float clearB = MinClearance(start, controlB, end, obstacles);
        Vector2 control = (clearA >= clearB) ? controlA : controlB;

        int segments = 16;
        Vector2 prev = start;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector2 point = Bezier(start, control, end, t);
            DrawSegment(prev, point, color);
            prev = point;
        }
    }

    // Smallest distance from any sampled point on the curve to the nearest obstacle.
    float MinClearance(Vector2 a, Vector2 c, Vector2 b, List<Vector2> obstacles)
    {
        if (obstacles.Count == 0) return float.MaxValue;
        float min = float.MaxValue;
        int samples = 8;
        for (int i = 1; i < samples; i++)
        {
            float t = i / (float)samples;
            Vector2 p = Bezier(a, c, b, t);
            foreach (var o in obstacles)
            {
                float d = Vector2.Distance(p, o);
                if (d < min) min = d;
            }
        }
        return min;
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
