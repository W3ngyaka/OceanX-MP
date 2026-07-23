using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Big-screen "SpongeBob" bubble wipe (matches the prototype's resetGame flood). A stream of soap bubbles of
/// mixed sizes rises CONTINUOUSLY from the bottom of the screen to past the top — they never stop — while a
/// water veil briefly peaks opaque to hide the reset, then clears. Self-contained: builds its own full-screen
/// overlay canvas and generates a soft bubble sprite, so dropping this on an empty GameObject just works.
/// Assign <see cref="bubbleSprite"/> to override the look.
///
/// <see cref="Play"/>(onCovered, onComplete): <c>onCovered</c> fires when the veil reaches full cover — do the
/// reset there so it happens behind the bubbles; <c>onComplete</c> fires when the whole flood finishes.
/// </summary>
public class BubbleTransition : MonoBehaviour
{
    [Header("Timing (seconds)")]
    [Tooltip("Time for the water veil to reach full cover (the reset fires at the end of this).")]
    [Min(0.05f)] public float coverDuration  = 0.7f;
    [Tooltip("How long the veil stays fully opaque (bubbles keep flowing the whole time).")]
    [Min(0f)]    public float holdDuration   = 0.15f;
    [Tooltip("Time for the veil to clear and reveal the fresh screen (bubbles keep rising through this).")]
    [Min(0.05f)] public float revealDuration = 0.7f;

    [Header("Look")]
    [Tooltip("Water veil colour that hides the screen at the covered peak. Lower its alpha for a lighter, " +
             "more see-through 'pure bubbles' feel.")]
    public Color waterColor = new Color(0.043f, 0.29f, 0.42f, 1f);
    [Tooltip("Optional bubble sprite. If empty, a soft highlighted soap-bubble circle is generated at runtime.")]
    public Sprite bubbleSprite;
    [Tooltip("How many bubbles are alive at once (they recycle bottom->top for a continuous stream).")]
    [Min(1)] public int bubbleCount = 150;
    [Tooltip("Bubble diameter range (screen pixels). A wide range gives the mixed big/small soap-bubble look.")]
    public Vector2 bubbleSizeRange = new Vector2(14f, 190f);
    [Range(0f, 1f)] public float bubbleAlpha = 0.9f;
    [Tooltip("How long each bubble takes to rise up the screen, in SECONDS (min..max, randomised per bubble). " +
             "This is the main 'how long before they fade' control — a bubble only fades once it reaches the " +
             "top, so bigger numbers = slower rise = they stay longer and fade nearer the top.")]
    public Vector2 bubbleRiseSeconds = new Vector2(1.3f, 2.6f);
    [Tooltip("Horizontal sway amplitude (px) so bubbles drift as they rise.")]
    public float drift = 22f;
    [Tooltip("Canvas sort order — high so it draws over everything on the big screen.")]
    public int sortingOrder = 30000;

    public bool IsPlaying { get; private set; }

    private Canvas _canvas;
    private CanvasGroup _group;
    private Image _veil;
    private RectTransform _bubbleRoot;
    private RectTransform[] _bubble;
    private Image[] _bimg;
    // Parallel per-bubble state (parallel arrays avoid struct-in-list churn).
    private float[] _bx, _by, _bsize, _bspd, _bphase;
    private float _w, _h;
    private bool _built;

    /// <summary>Play the wipe. onCovered fires at full cover (do the reset there); onComplete at the end.</summary>
    public void Play(System.Action onCovered, System.Action onComplete)
    {
        EnsureBuilt();
        if (IsPlaying) { Safe(onCovered); Safe(onComplete); return; }  // already flooding — don't stack
        StartCoroutine(Run(onCovered, onComplete));
    }

    private IEnumerator Run(System.Action onCovered, System.Action onComplete)
    {
        IsPlaying = true;
        _canvas.gameObject.SetActive(true);
        _group.alpha = 1f;

        _w = Mathf.Max(1, Screen.width);
        _h = Mathf.Max(1, Screen.height);

        // Seed every bubble just BELOW the screen, staggered, so they flood UP from the bottom (then recycle
        // for a continuous stream). Kept within ~0.6 screens below so even the lowest can rise off in time.
        for (int i = 0; i < _bubble.Length; i++)
        {
            RespawnBubble(i);
            _by[i] = -_h * 0.5f - Random.value * _h * 0.6f;
            ApplyBubble(i, 0f);
        }

        float veilEnd = coverDuration + holdDuration + revealDuration;
        float maxTime = veilEnd + bubbleRiseSeconds.y * 1.6f + 0.5f;   // safety cap so a straggler can't hang it
        float t = 0f;
        bool coveredFired = false;

        while (true)
        {
            float dt = Time.unscaledDeltaTime;

            // The VEIL fades in over the cover, holds, then fades out over the reveal — that's what hides the
            // reset and then uncovers the fresh screen. The BUBBLES are NOT tied to this timer; each fades only
            // by its own height (see ApplyBubble), so a bubble fades at the TOP, never mid-screen.
            float veilA;
            if (t < coverDuration)                     veilA = t / coverDuration;
            else if (t < coverDuration + holdDuration) veilA = 1f;
            else                                       veilA = 1f - Mathf.Clamp01((t - coverDuration - holdDuration) / revealDuration);
            var c = waterColor; c.a *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(veilA)); _veil.color = c;

            if (!coveredFired && t >= coverDuration) { coveredFired = true; Safe(onCovered); }

            // Rise every bubble. Recycle bottom->top for a continuous stream until the reveal starts; after that
            // stop recycling so the field flushes up and out (bubbles rise off the top and fade there).
            bool revealing = t >= coverDuration + holdDuration;
            bool allCleared = true;
            for (int i = 0; i < _bubble.Length; i++)
            {
                _by[i] += _bspd[i] * dt;
                if (_by[i] > _h * 0.5f + _bsize[i])
                {
                    if (!revealing) { RespawnBubble(i); _by[i] = -_h * 0.5f - _bsize[i]; }
                    // during reveal: it has cleared the top — leave it up there
                }
                else allCleared = false;   // still on / below the screen
                ApplyBubble(i, Time.unscaledTime);
            }

            // Finish once the veil has fully revealed AND every bubble has risen off the top (or the safety cap).
            if ((t >= veilEnd && allCleared) || t >= maxTime) break;

            t += dt;
            yield return null;
        }

        _canvas.gameObject.SetActive(false);
        IsPlaying = false;
        Safe(onComplete);
    }

    // New random size / x / speed / phase for a bubble; keeps its current y (caller sets that).
    private void RespawnBubble(int i)
    {
        _bsize[i]  = Random.Range(bubbleSizeRange.x, bubbleSizeRange.y);
        _bx[i]     = Random.Range(-_w * 0.5f - _bsize[i], _w * 0.5f + _bsize[i]);   // bleed past the edges
        _bspd[i]   = _h / Mathf.Max(0.05f, Random.Range(bubbleRiseSeconds.x, bubbleRiseSeconds.y)); // px/sec
        _bphase[i] = Random.Range(0f, Mathf.PI * 2f);
        _bubble[i].sizeDelta = new Vector2(_bsize[i], _bsize[i]);
    }

    // Place + fade a bubble by its vertical progress (fade in low, fade out near the top), plus horizontal sway.
    private void ApplyBubble(int i, float time)
    {
        float x = _bx[i] + Mathf.Sin(time * 1.6f + _bphase[i]) * drift;
        _bubble[i].anchoredPosition = new Vector2(x, _by[i]);

        float prog = Mathf.InverseLerp(-_h * 0.5f - _bsize[i], _h * 0.5f + _bsize[i], _by[i]); // 0 bottom -> 1 top
        float fade = Mathf.Clamp01(prog / 0.08f) * (1f - Mathf.Clamp01((prog - 0.92f) / 0.08f)); // in low, hold, out only at the very top
        _bimg[i].color = new Color(1f, 1f, 1f, bubbleAlpha * fade);
    }

    private void EnsureBuilt()
    {
        if (_built) return;
        _built = true;

        var canvasGO = new GameObject("BubbleTransitionCanvas");
        canvasGO.transform.SetParent(transform, false);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = sortingOrder;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        _group = canvasGO.AddComponent<CanvasGroup>();
        _group.interactable = false;
        _group.blocksRaycasts = false;   // big screen has no input; don't block anything

        // Water veil (full-screen, alpha animated — this is what actually hides the screen at the peak).
        var veilRT = NewRect("Veil", canvasGO.transform);
        veilRT.anchorMin = Vector2.zero; veilRT.anchorMax = Vector2.one;
        veilRT.offsetMin = Vector2.zero; veilRT.offsetMax = Vector2.zero;
        _veil = veilRT.gameObject.AddComponent<Image>();
        var start = waterColor; start.a = 0f; _veil.color = start;
        _veil.raycastTarget = false;

        // Bubble container (centre-anchored so bubble anchoredPosition is screen-centre relative).
        _bubbleRoot = NewRect("Bubbles", canvasGO.transform);
        _bubbleRoot.anchorMin = Vector2.zero; _bubbleRoot.anchorMax = Vector2.one;
        _bubbleRoot.offsetMin = Vector2.zero; _bubbleRoot.offsetMax = Vector2.zero;

        Sprite sprite = bubbleSprite != null ? bubbleSprite : GenerateBubbleSprite();

        _bubble = new RectTransform[bubbleCount];
        _bimg = new Image[bubbleCount];
        _bx = new float[bubbleCount]; _by = new float[bubbleCount]; _bsize = new float[bubbleCount];
        _bspd = new float[bubbleCount]; _bphase = new float[bubbleCount];
        for (int i = 0; i < bubbleCount; i++)
        {
            var rt = NewRect("Bubble", _bubbleRoot);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = false;
            _bubble[i] = rt;
            _bimg[i] = img;
        }

        _group.alpha = 0f;
        canvasGO.SetActive(false);
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Safe(System.Action a)
    {
        if (a == null) return;
        try { a(); } catch (System.Exception e) { Debug.LogException(e); }  // never leave the screen stuck covered
    }

    // A soft soap bubble: mostly transparent, a brighter ring, and a small highlight — matches the prototype.
    private Sprite GenerateBubbleSprite()
    {
        const int R = 64;
        var tex = new Texture2D(R, R, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Vector2 c = new Vector2(R * 0.5f, R * 0.5f);
        float rad = R * 0.5f - 1f;
        Vector2 hi = new Vector2(R * 0.32f, R * 0.68f);   // highlight dot
        float hiR = R * 0.15f;
        for (int y = 0; y < R; y++)
        for (int x = 0; x < R; x++)
        {
            Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
            float d = Vector2.Distance(p, c);
            float disk = 1f - Mathf.SmoothStep(rad * 0.7f, rad, d);                                    // faint body
            float rim  = Mathf.SmoothStep(rad * 0.6f, rad * 0.92f, d) * (1f - Mathf.SmoothStep(rad * 0.92f, rad, d));
            float a = Mathf.Clamp01(disk * 0.22f + rim * 0.95f);                                       // ring
            float h = Mathf.Clamp01(1f - Vector2.Distance(p, hi) / hiR);
            a = Mathf.Clamp01(a + h * 0.9f);                                                           // highlight
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, R, R), new Vector2(0.5f, 0.5f), 100f);
    }
}
