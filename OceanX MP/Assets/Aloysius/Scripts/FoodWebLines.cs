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

        GameObject line = Instantiate(linePrefab, transform);
        line.SetActive(true);
        activeLines.Add(line);

        RectTransform rt = line.GetComponent<RectTransform>();
        Image img = line.GetComponent<Image>();
        img.color = color;

        Vector2 fromPos = from.position;
        Vector2 toPos = to.position;
        Vector2 dir = toPos - fromPos;
        float dist = dir.magnitude;

        rt.position = (fromPos + toPos) / 2f;
        rt.sizeDelta = new Vector2(dist, 4f);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.rotation = Quaternion.Euler(0, 0, angle);

        Debug.Log($"Drew line: {from.name} → {to.name}");
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
