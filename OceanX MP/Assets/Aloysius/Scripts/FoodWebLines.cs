using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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

    [Header("Ambient web (pulses ONE link at a time)")]
    [Tooltip("Softly cycle a single predator/prey connection at a time so the layout reads as a web without any clutter.")]
    public bool showAmbientWeb = true;
    [Tooltip("Colour + alpha of the pulsing link. Defaults to match the hold-reveal exactly.")]
    public Color ambientColor = new Color(0f, 0.85f, 1f, 1f);
    [Tooltip("Thickness (px) of the pulsing link. Matches the hold-reveal (4).")]
    public float ambientThickness = 4f;
    [Tooltip("Arrowhead (prey -> predator) on the pulsing link.")]
    public bool ambientArrows = true;
    [Tooltip("How much clear links bow into an arc, as a fraction of link length. 0 = straight.")]
    [Range(0f, 0.3f)] public float ambientArc = 0.15f;
    [Tooltip("Max arc bow in pixels (caps the curve on long links so they can't swoop).")]
    public float ambientArcMax = 120f;
    [Tooltip("Length (px) of each dash/segment. Keeps dashes the SAME size on short and long links.")]
    public float segmentLength = 18f;
    [Tooltip("Draw links as dashes (dash-gap-dash) instead of one solid line.")]
    public bool dashed = true;
    [Tooltip("Seconds to fade the link in.")]
    public float pulseFadeIn = 0.35f;
    [Tooltip("Seconds the link stays full while the energy dot travels.")]
    public float pulseHold = 0.8f;
    [Tooltip("Seconds to fade the link out.")]
    public float pulseFadeOut = 0.35f;
    [Tooltip("Seconds of blank space between links.")]
    public float pulseGap = 0.12f;

    private List<CanvasGroup> dimmedBubbles = new List<CanvasGroup>();

    // One flow per drawn line: the curve it rides plus its dot images.
    private class Flow { public Vector2 a, c, b; public Image[] dots; }
    private readonly List<Flow> _flows = new List<Flow>();
    private float _flowT;
    private Sprite _dotSprite;

    // Always-on ambient web (pulse mode).
    private Transform _ambientRoot;
    private readonly List<GameObject> _pulseObjs = new List<GameObject>();
    private GameObject _pulseContainer;
    private bool _revealActive;   // a bright hold-reveal is up -> pause the pulse

    void Awake()
    {
        Instance = this;
    }

    // NOTE: pulse startup moved to OnEnable so the ambient web survives tab switches.
    // Deactivating this layer kills its coroutines permanently; Unity does not resume
    // them on re-enable and Start() only runs once. OnEnable re-arms it every time.
    private bool _pulseRunning;
    private int _pulseIdx;   // persists across tab switches so the web resumes, not restarts

    void OnEnable()
    {
        if (showAmbientWeb && !_pulseRunning)
        {
            _pulseRunning = true;
            StartCoroutine(PulseLoop());
        }
    }

    void OnDisable()
    {
        // Coroutines are already dead here; just reset so OnEnable restarts cleanly
        // and no stale reveal/pulse state carries across the tab switch.
        _pulseRunning = false;
        _revealActive = false;
        ClearPulse();
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

    // ---------------------------------------------------------------------------
    // Interactive reveal — bright, on hold.
    // ---------------------------------------------------------------------------

    public void ShowConnections(SpeciesBubble source)
    {
        var connected = new HashSet<SpeciesBubble>();
        connected.Add(source);
        foreach (var p in source.prey) if (p != null) connected.Add(p);
        foreach (var p in source.predators) if (p != null) connected.Add(p);

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

        // Pause + clear the ambient pulse so the bright reveal is clean.
        _revealActive = true;
        ClearPulse();

        Color lineColor = new Color(0f, 0.85f, 1f, 1f);

        foreach (var predator in source.predators)
            if (predator != null)
                DrawLineInto(transform, activeLines, source.transform, predator.transform, lineColor, 4f, true, animateFlow);

        foreach (var prey in source.prey)
            if (prey != null)
                DrawLineInto(transform, activeLines, prey.transform, source.transform, lineColor, 4f, true, animateFlow);
    }

    public void HideConnections()
    {
        foreach (var cg in dimmedBubbles)
            if (cg != null) cg.alpha = 1f;
        dimmedBubbles.Clear();

        for (int i = activeLines.Count - 1; i >= 0; i--)
            if (activeLines[i] != null)
                DestroyImmediate(activeLines[i]);
        activeLines.Clear();
        _flows.Clear();

        foreach (var ring in glowRings)
            if (ring != null) ring.enabled = false;
        glowRings.Clear();

        _revealActive = false; // ambient pulse resumes
    }

    // ---------------------------------------------------------------------------
    // Ambient web — one link pulses at a time (clean, no tangle).
    // ---------------------------------------------------------------------------

    IEnumerator PulseLoop()
    {
        yield return null;
        yield return null; // let any runtime layout settle
        EnsureAmbientRoot();

        // Collect each unique prey -> predator link once.
        var links = new List<KeyValuePair<Transform, Transform>>();
        var bubbles = FindObjectsByType<SpeciesBubble>(FindObjectsSortMode.None);
        var seen = new HashSet<long>();
        foreach (var b in bubbles)
        {
            foreach (var prey in b.prey)
                if (prey != null && seen.Add(Key(prey, b)))
                    links.Add(new KeyValuePair<Transform, Transform>(prey.transform, b.transform));
            foreach (var pred in b.predators)
                if (pred != null && seen.Add(Key(b, pred)))
                    links.Add(new KeyValuePair<Transform, Transform>(b.transform, pred.transform));
        }
        if (links.Count == 0) { _pulseRunning = false; yield break; }

        while (true)
        {
            while (_revealActive) yield return null;   // paused during a hold reveal

            var link = links[_pulseIdx % links.Count];
            _pulseIdx++;
            Transform from = link.Key, to = link.Value;
            if (from == null || to == null) { yield return null; continue; }

            // Draw the link with the SAME renderer the hold-reveal uses (curved line +
            // arrow + animated energy dots), inside a CanvasGroup we fade in/out.
            var container = new GameObject("PulseLink", typeof(RectTransform), typeof(CanvasGroup));
            var crt = (RectTransform)container.transform;
            crt.SetParent(_ambientRoot, false);
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            _pulseContainer = container;
            var cg = container.GetComponent<CanvasGroup>();
            cg.alpha = 0f; cg.blocksRaycasts = false;

            DrawLineInto(container.transform, _pulseObjs, from, to,
                         new Color(ambientColor.r, ambientColor.g, ambientColor.b, 1f),
                         ambientThickness, ambientArrows, true);

            float total = pulseFadeIn + pulseHold + pulseFadeOut;
            float t = 0f;
            while (t < total && !_revealActive)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = t < pulseFadeIn ? t / pulseFadeIn
                         : t < pulseFadeIn + pulseHold ? 1f
                         : Mathf.Clamp01(1f - (t - pulseFadeIn - pulseHold) / pulseFadeOut);
                yield return null;
            }

            ClearPulse();
            if (!_revealActive) yield return new WaitForSeconds(pulseGap);
        }
    }

    void EnsureAmbientRoot()
    {
        if (_ambientRoot != null) return;
        Transform layer = transform.parent != null ? transform.parent : transform;
        Transform existing = layer.Find("AmbientWeb");
        if (existing != null) { _ambientRoot = existing; }
        else
        {
            var go = new GameObject("AmbientWeb", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(layer, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            _ambientRoot = rt;
        }
        _ambientRoot.SetAsFirstSibling(); // behind the bubbles
    }

    void ClearPulse()
    {
        _pulseObjs.Clear(); // these are children of the container, destroyed with it below
        if (_pulseContainer != null) { DestroyImmediate(_pulseContainer); _pulseContainer = null; }
        // Drop flow entries whose dots were destroyed with the container (avoids a slow leak).
        _flows.RemoveAll(f => f.dots == null || AllNull(f.dots));
    }

    static bool AllNull(Image[] dots)
    {
        foreach (var d in dots) if (d != null) return false;
        return true;
    }

    static long Key(Object from, Object to)
        => ((long)from.GetInstanceID() << 32) ^ (uint)to.GetInstanceID();

    // ---------------------------------------------------------------------------
    // Shared drawing primitives.
    // ---------------------------------------------------------------------------

    void DrawLineInto(Transform parent, List<GameObject> list, Transform from, Transform to,
                      Color color, float thickness, bool drawArrow, bool drawFlow)
    {
        if (linePrefab == null) { Debug.LogError("linePrefab is null!"); return; }

        Vector2 startC = from.position;   // bubble centres
        Vector2 endC = to.position;
        if (Vector2.Distance(startC, endC) < 0.001f) return;

        // Curve shape from the centres so it stays stable regardless of edge-trimming.
        Vector2 control = ComputeCurveControl(startC, endC, from, to);

        // Trim each end back to the bubble rim ALONG the curve's approach direction
        // (control -> centre), so the line meets the edge and the arrowhead points AT the
        // bubble's centre instead of grazing its side.
        Vector2 endTan = endC - control;
        if (endTan.sqrMagnitude < 0.0001f) endTan = endC - startC;
        endTan.Normalize();
        Vector2 startTan = startC - control;
        if (startTan.sqrMagnitude < 0.0001f) startTan = startC - endC;
        startTan.Normalize();

        Vector2 start = startC - startTan * BubbleRadius(from);
        Vector2 end = endC - endTan * BubbleRadius(to);

        // If the bubbles are so close the trims cross over, fall back to centre-to-centre.
        if (Vector2.Dot(end - start, endC - startC) <= 0f) { start = startC; end = endC; }
        // Segment count scales with the curve length so every dash is the same size,
        // whether the link is short or long.
        float approxLen = Vector2.Distance(start, control) + Vector2.Distance(control, end);
        int segments = Mathf.Clamp(Mathf.RoundToInt(approxLen / Mathf.Max(4f, segmentLength)), 1, 80);
        Vector2 prev = start;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector2 point = Bezier(start, control, end, t);
            if (!dashed || (i % 2) == 1)   // dashed -> draw every other segment (dash, gap, dash)
                DrawSegmentInto(parent, list, prev, point, color, thickness);
            prev = point;
        }

        if (drawArrow)
        {
            Vector2 tangent = end - control;
            if (tangent.sqrMagnitude < 0.0001f) tangent = endTan;
            AddArrowHeadInto(parent, list, end, tangent.normalized, color, thickness);
        }

        if (drawFlow && dotsPerLine > 0)
            SpawnFlow(parent, list, start, control, end, color);
    }

    void SpawnFlow(Transform parent, List<GameObject> list, Vector2 a, Vector2 c, Vector2 b, Color color)
    {
        var dots = new Image[dotsPerLine];
        for (int i = 0; i < dotsPerLine; i++)
        {
            var go = new GameObject("FlowDot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(dotSize, dotSize);

            var img = go.GetComponent<Image>();
            img.sprite = DotSprite();
            img.color = new Color(color.r, color.g, color.b, 0f);
            img.raycastTarget = false;
            rt.position = Bezier(a, c, b, i / (float)dotsPerLine);

            list.Add(go);
            dots[i] = img;
        }
        _flows.Add(new Flow { a = a, c = c, b = b, dots = dots });
    }

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
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        tex.Apply();
        _dotSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _dotSprite;
    }

    void AddArrowHeadInto(Transform parent, List<GameObject> list, Vector2 tip, Vector2 dir, Color color, float thickness)
    {
        const float len = 16f;
        const float ang = 28f * Mathf.Deg2Rad;
        Vector2 back = -dir;
        DrawSegmentInto(parent, list, tip, tip + Rotate(back, ang) * len, color, thickness);
        DrawSegmentInto(parent, list, tip, tip + Rotate(back, -ang) * len, color, thickness);
    }

    Vector2 Rotate(Vector2 v, float rad)
    {
        float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    // Pick a bezier control point for a nice curved link: bow around any bubbles in the
    // way, and — when the path is already clear — still apply a gentle arc bowing away from
    // the layout centre so no link is dead straight.
    Vector2 ComputeCurveControl(Vector2 start, Vector2 end, Transform from, Transform to)
    {
        Vector2 mid = (start + end) / 2f;
        Vector2 dir = end - start;
        float dist = dir.magnitude;
        if (dist < 0.001f) return mid;
        Vector2 unit = dir / dist;
        Vector2 perp = new Vector2(-unit.y, unit.x);

        var obstacles = new List<Vector2>();
        var radii = new List<float>();
        Vector2 centroid = Vector2.zero;
        int n = 0;
        foreach (var b in FindObjectsByType<SpeciesBubble>(FindObjectsSortMode.None))
        {
            centroid += (Vector2)b.transform.position; n++;
            if (b.transform == from || b.transform == to) continue;
            obstacles.Add(b.transform.position);
            radii.Add(BubbleRadius(b.transform));
        }
        if (n > 0) centroid /= n;

        const float margin = 12f;
        Vector2 control = mid;
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
                if (bestPenalty <= 0f) break;
            }
        }

        // Clear path -> apply a GENTLE arc bowing outward from the layout centre.
        if (control == mid && ambientArc > 0f)
        {
            float side = Vector2.Dot(mid - centroid, perp) >= 0f ? 1f : -1f;
            control = mid + perp * side * Mathf.Min(dist * ambientArc, ambientArcMax);
        }
        return control;
    }

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

    void DrawSegmentInto(Transform parent, List<GameObject> list, Vector2 p1, Vector2 p2, Color color, float thickness)
    {
        GameObject line = Instantiate(linePrefab, parent);
        line.SetActive(true);
        list.Add(line);

        RectTransform rt = line.GetComponent<RectTransform>();
        Image img = line.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        Vector2 d = p2 - p1;
        float len = d.magnitude;
        rt.position = (p1 + p2) / 2f;
        rt.sizeDelta = new Vector2(len + 1f, thickness);
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        rt.rotation = Quaternion.Euler(0, 0, angle);
    }

    float BubbleRadius(Transform bubble)
    {
        var brt = bubble as RectTransform;
        if (brt == null) brt = bubble.GetComponent<RectTransform>();
        if (brt == null) return 0f;
        Vector3[] corners = new Vector3[4];
        brt.GetWorldCorners(corners);
        float w = Vector3.Distance(corners[0], corners[3]);
        float h = Vector3.Distance(corners[0], corners[1]);
        return Mathf.Min(w, h) * 0.5f + 8f;
    }
}
