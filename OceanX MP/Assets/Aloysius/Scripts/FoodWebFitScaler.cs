using UnityEngine;

// Uniformly scales the food-web contents so the whole radial cluster fits inside
// the available panel area at any resolution/aspect ratio. Never scales above 1
// (won't blow the layout up on huge screens). Preserves each child's own
// authored localScale by multiplying the fit factor onto it.
//
// Put this on FoodWebLayer. It scales the listed content roots (or all direct
// children if none are listed).
[ExecuteAlways]
public class FoodWebFitScaler : MonoBehaviour
{
    [Tooltip("The design-time area the food web was laid out for (canvas ref res or the panel size).")]
    public Vector2 designSize = new Vector2(1920f, 1080f);

    [Tooltip("Content roots to scale. If empty, all direct children are scaled.")]
    public Transform[] contentRoots;

    [Tooltip("Extra safety margin (0.9 = leave 10% breathing room so nothing touches the edge).")]
    [Range(0.5f, 1f)] public float fitMargin = 0.95f;

    [Tooltip("Never scale larger than the original layout.")]
    public bool clampToOne = true;

    [Tooltip("Measure available space from the root Canvas (true = reacts to real screen resolution) instead of this object's own rect.")]
    public bool measureAgainstCanvas = true;

    [Tooltip("When scaling this object itself: fit its real rendered box (rect x base scale) into the available area, accounting for non-uniform base scale.")]
    public bool fitSelfRenderedSize = true;

    // Persisted authored scales. We overwrite localScale every frame in edit mode
    // ([ExecuteAlways]), so reading localScale on reload would treat an already-fit
    // value as the authored base and compound the shrink each session. Serializing
    // the base keeps the non-uniform baked scale stable across save/reload.
    [SerializeField, HideInInspector] private Vector3[] _persistentBaseScales;
    [SerializeField, HideInInspector] private bool _hasPersistentBase;

    private RectTransform _rt;
    private RectTransform _canvasRt;
    private Transform[] _targets;
    private Vector3[] _baseScales;
    private float _lastFactor = -1f;
    private Vector2 _lastSize;

    void OnEnable()
    {
        _rt = GetComponent<RectTransform>();
        var cv = GetComponentInParent<Canvas>();
        if (cv != null) _canvasRt = cv.rootCanvas.GetComponent<RectTransform>();
        CacheTargets();
        Apply(true);
    }

    void CacheTargets()
    {
        if (contentRoots != null && contentRoots.Length > 0)
            _targets = contentRoots;
        else
        {
            var list = new System.Collections.Generic.List<Transform>();
            foreach (Transform c in transform) list.Add(c);
            _targets = list.ToArray();
        }

        // Reuse the persisted authored scales when they still match the target set.
        if (_hasPersistentBase && _persistentBaseScales != null && _persistentBaseScales.Length == _targets.Length)
        {
            _baseScales = _persistentBaseScales;
            return;
        }

        _baseScales = new Vector3[_targets.Length];
        for (int i = 0; i < _targets.Length; i++)
            _baseScales[i] = _targets[i] != null ? _targets[i].localScale : Vector3.one;

        _persistentBaseScales = _baseScales;
        _hasPersistentBase = true;
    }

    void Update()
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        Apply(false);
    }

    void Apply(bool force)
    {
        if (_rt == null || _targets == null) return;

        Vector2 size = (measureAgainstCanvas && _canvasRt != null) ? _canvasRt.rect.size : _rt.rect.size;
        if (!force && size == _lastSize) return;
        _lastSize = size;

        if (size.x <= 1f || size.y <= 1f) return;

        float factor;
        if (fitSelfRenderedSize && _targets.Length == 1 && _targets[0] == transform)
        {
            // Real rendered size of this panel at base scale.
            Vector2 rectSize = _rt.rect.size;
            Vector3 baseS = _baseScales[0];
            float renderedW = rectSize.x * Mathf.Abs(baseS.x);
            float renderedH = rectSize.y * Mathf.Abs(baseS.y);
            float fx = size.x / Mathf.Max(1f, renderedW);
            float fy = size.y / Mathf.Max(1f, renderedH);
            factor = Mathf.Min(fx, fy) * fitMargin;
        }
        else
        {
            float fx = size.x / designSize.x;
            float fy = size.y / designSize.y;
            factor = Mathf.Min(fx, fy) * fitMargin;
        }
        if (clampToOne) factor = Mathf.Min(factor, 1f);

        if (!force && Mathf.Abs(factor - _lastFactor) < 0.0005f) return;
        _lastFactor = factor;

        for (int i = 0; i < _targets.Length; i++)
        {
            if (_targets[i] == null) continue;
            _targets[i].localScale = _baseScales[i] * factor;
        }
    }

    // Re-capture authored scales (call if you tweak child scales in the editor).
    [ContextMenu("Recache Base Scales")]
    public void RecacheBaseScales()
    {
        // Force a fresh capture from the current localScale — call this only when the
        // targets are at their authored (unfit) scale, e.g. reference resolution.
        _hasPersistentBase = false;
        _persistentBaseScales = null;
        CacheTargets();
        _lastFactor = -1f;
        Apply(true);
    }
}
