using UnityEngine;
using TMPro;
using System.Collections;

// Spawns a floating "+1" that rises and fades — feedback for adding a species.
// Singleton so any script can call AddFeedback.Instance.PlusOne(worldOrScreenPos).
public class AddFeedback : MonoBehaviour
{
    public static AddFeedback Instance { get; private set; }

    [Header("Refs")]
    [Tooltip("Canvas (screen-space) to parent the popups under. Defaults to this object's canvas.")]
    public RectTransform canvasRect;

    [Header("Look")]
    public string label = "+1";
    public float fontSize = 48f;
    public TMP_FontAsset font;   // Rajdhani-Bold
    public Color color = new Color(0.4f, 1f, 0.55f, 1f);  // green, matches 'prey/food' language
    public float riseDistance = 120f;
    public float duration = 0.9f;

    void Awake()
    {
        Instance = this;
        if (canvasRect == null)
        {
            var cv = GetComponentInParent<Canvas>();
            if (cv != null) canvasRect = cv.transform as RectTransform;
        }
    }

    // Show a +1 at a screen position (e.g. a button's position converted to screen space).
    public void PlusOneAtScreen(Vector2 screenPos)
    {
        if (canvasRect == null) return;
        StartCoroutine(Float(screenPos));
    }

    // Convenience: show a +1 over a UI element.
    public void PlusOneAt(RectTransform target)
    {
        if (target == null || canvasRect == null) return;
        Vector3[] c = new Vector3[4];
        target.GetWorldCorners(c);
        Vector3 top = (c[1] + c[2]) * 0.5f;   // top-center of the element
        var cam = GetCanvasCamera();
        Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, top);
        PlusOneAtScreen(sp);
    }

    Camera GetCanvasCamera()
    {
        var cv = canvasRect != null ? canvasRect.GetComponent<Canvas>() : null;
        if (cv == null) cv = GetComponentInParent<Canvas>();
        if (cv != null && cv.renderMode != RenderMode.ScreenSpaceOverlay) return cv.worldCamera;
        return null;
    }

    IEnumerator Float(Vector2 screenPos)
    {
        var go = new GameObject("PlusOne", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(canvasRect, false);
        rt.SetAsLastSibling();

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        if (font != null) tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.raycastTarget = false;

        // Convert screen point to the canvas's local space.
        var cam = GetCanvasCamera();
        Vector2 startLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, cam, out startLocal);
        rt.anchoredPosition = startLocal;
        rt.localScale = Vector3.one;

        float t = 0f;
        Vector2 from = startLocal;
        Vector2 to = startLocal + new Vector2(0f, riseDistance);
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 2f);   // ease-out
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to, e);
            // pop in quickly, then fade out over the second half
            float scale = t < 0.15f ? Mathf.Lerp(0.6f, 1.1f, t / 0.15f) : 1.1f;
            rt.localScale = Vector3.one * scale;
            tmp.alpha = t < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.5f) / 0.5f);
            yield return null;
        }
        Destroy(go);
    }
}
