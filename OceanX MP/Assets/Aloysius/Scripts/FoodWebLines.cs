using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class FoodWebLines : MonoBehaviour
{
    public static FoodWebLines Instance;
    public GameObject linePrefab;

    public List<GameObject> activeLines = new List<GameObject>();
    public List<Image> glowRings = new List<Image>();

    void Awake()
    {
        Instance = this;
    }

    public void ShowConnections(SpeciesBubble source)
    {
        Debug.Log($"ShowConnections: {source.name}, prey={source.prey.Count}, predators={source.predators.Count}");

        foreach (var predator in source.predators)
            DrawLine(source.transform, predator.transform, new Color(0f, 0.85f, 1f, 1f));

        foreach (var p in source.prey)
            DrawLine(source.transform, p.transform, new Color(0f, 0.85f, 1f, 0.4f));

        Debug.Log($"activeLines count after draw: {activeLines.Count}");
    }

    public void HideConnections()
    {
        Debug.Log($"HideConnections: destroying {activeLines.Count} lines");
        for (int i = activeLines.Count - 1; i >= 0; i--)
        {
            if (activeLines[i] != null)
                DestroyImmediate(activeLines[i]);
        }
        activeLines.Clear();

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
        rt.sizeDelta = new Vector2(dist, 6f);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.rotation = Quaternion.Euler(0, 0, angle);

        Debug.Log($"Drew line from {from.name} to {to.name}, activeLines now: {activeLines.Count}");
    }
}
