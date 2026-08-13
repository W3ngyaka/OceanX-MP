using UnityEngine;

[ExecuteAlways]
public class FoodWebFitScaler : MonoBehaviour
{
        public Vector2 designSize = new Vector2(1920f, 1080f);

        public Transform[] contentRoots;

        [Range(0.5f, 1f)] public float fitMargin = 0.95f;

        public bool clampToOne = true;

        public bool measureAgainstCanvas = true;

        public bool fitSelfRenderedSize = true;

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

    [ContextMenu("Recache Base Scales")]
    public void RecacheBaseScales()
    {

        _hasPersistentBase = false;
        _persistentBaseScales = null;
        CacheTargets();
        _lastFactor = -1f;
        Apply(true);
    }
}
