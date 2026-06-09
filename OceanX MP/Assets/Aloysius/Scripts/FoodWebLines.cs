using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class FoodWebLines : MonoBehaviour
{
    public static FoodWebLines Instance;

    public GameObject linePrefab;

    private List<GameObject> activeLines = new List<GameObject>();
    private List<Image> glowRings = new List<Image>();

    void Awake()
    {
        Instance = this;
    }

    public void ShowConnections(SpeciesBubble source)
    {
        HideConnections();

        foreach (var predator in source.predators)
        {
            DrawLine(source.transform, predator.transform, new Color(0f, 0.85f, 1f, 1f));
            HighlightBubble(predator, true);
        }

        foreach (var prey in source.prey)
        {
            DrawLine(source.transform, prey.transform, new Color(0f, 0.85f, 1f, 0.4f));
            HighlightBubble(prey, false);
        }
    }

    public void HideConnections()
    {
        foreach (var line in activeLines)
        {
            if (line != null)
                Destroy(line);
        }
        activeLines.Clear();

        foreach (var ring in glowRings)
        {
            if (ring != null)
                ring.enabled = false;
        }
        glowRings.Clear();
    }

    void DrawLine(Transform from, Transform to, Color color)
    {
        GameObject line = Instantiate(linePrefab, transform);
        RectTransform rt = line.GetComponent<RectTransform>();
        Image img = line.GetComponent<Image>();

        img.color = color;

        Vector2 fromPos = from.position;
        Vector2 toPos = to.position;

        Vector2 dir = toPos - fromPos;
        float dist = dir.magnitude;

        rt.position = (fromPos + toPos) / 2f;
        rt.sizeDelta = new Vector2(dist, 2f);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.rotation = Quaternion.Euler(0, 0, angle);

        activeLines.Add(line);
    }

    void HighlightBubble(SpeciesBubble bubble, bool bright)
    {
        Transform ring = bubble.transform.Find("GlowRing");
        if (ring == null) return;

        Image ringImg = ring.GetComponent<Image>();
        if (ringImg == null) return;

        ringImg.enabled = true;

        ringImg.color = bright
            ? new Color(0f, 0.85f, 1f, 1f)
            : new Color(0f, 0.85f, 1f, 0.4f);

        glowRings.Add(ringImg);
    }
}