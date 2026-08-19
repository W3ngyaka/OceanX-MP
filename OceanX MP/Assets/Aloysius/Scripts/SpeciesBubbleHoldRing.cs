using UnityEngine;
using UnityEngine.UI;

public class SpeciesBubbleHoldRing : MonoBehaviour
{
        public Image holdRing;
        public Sprite holdRingSprite;
        public Color holdRingColor = new Color(0.15f, 0.9f, 1f, 1f);
        public float holdRingScale = 1.06f;
        public float holdDuration = 0.5f;

    private SpeciesBubble _bubble;
    private bool _resolved;
    private float _holdTimer;
    private bool _isHolding;
    private bool _longPressTriggered;

    void Start() { EnsureResolved(); }

    void EnsureResolved()
    {
        if (_resolved) return;
        _resolved = true;

        _bubble = GetComponentInParent<SpeciesBubble>();
        ResolveRing();
        HideHoldRing();
    }

    void ResolveRing()
    {
        if (holdRing != null) { ConfigureForRadialFill(holdRing); return; }

        bool onBubbleRoot = GetComponent<SpeciesBubble>() != null;
        if (!onBubbleRoot)
        {
            var self = GetComponent<Image>();
            if (self != null) { holdRing = self; ConfigureForRadialFill(holdRing); return; }
        }

        if (holdRingSprite != null) CreateHoldRing();
    }

    public void BeginHold()
    {
        EnsureResolved();
        _isHolding = true;
        _holdTimer = 0f;
        _longPressTriggered = false;
    }

    public void Tick()
    {
        if (!_isHolding || _longPressTriggered) return;

        _holdTimer += Time.unscaledDeltaTime;

        float progress = Mathf.Clamp01(_holdTimer / holdDuration);
        SetHoldRing(progress);

        if (progress >= 1f)
        {
            _longPressTriggered = true;
            HideHoldRing();
            if (_bubble != null && FoodWebLines.Instance != null)
                FoodWebLines.Instance.ShowConnections(_bubble);
        }
    }

    public bool EndHold()
    {
        _isHolding = false;
        HideHoldRing();

        if (_longPressTriggered)
        {
            _longPressTriggered = false;
            if (FoodWebLines.Instance != null)
                FoodWebLines.Instance.HideConnections();
            return true;
        }
        return false;
    }

    public void CancelHold()
    {
        _isHolding = false;
        HideHoldRing();
    }

    static void ConfigureForRadialFill(Image img)
    {
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Radial360;
        img.fillOrigin = (int)Image.Origin360.Top;
        img.fillClockwise = true;
        img.raycastTarget = false;
    }

    void CreateHoldRing()
    {
        var go = new GameObject("HoldProgressRing", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);

        var brt = transform as RectTransform;
        Vector2 size = brt != null ? brt.rect.size : new Vector2(140f, 140f);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size * holdRingScale;
        rt.SetAsLastSibling();

        holdRing = go.GetComponent<Image>();
        holdRing.sprite = holdRingSprite;
        holdRing.color = holdRingColor;
        ConfigureForRadialFill(holdRing);
    }

    void SetHoldRing(float progress)
    {
        if (holdRing == null) return;
        holdRing.enabled = true;
        holdRing.fillAmount = Mathf.Clamp01(progress);
    }

    void HideHoldRing()
    {
        if (holdRing == null) return;
        holdRing.enabled = false;
        holdRing.fillAmount = 0f;
    }
}
