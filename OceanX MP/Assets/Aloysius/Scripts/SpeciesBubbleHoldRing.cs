using UnityEngine;
using UnityEngine.UI;

// Press-and-hold gesture + radial progress ring for a SpeciesBubble.
//
// This may live EITHER on the bubble root OR on a dedicated ring child (e.g.
// "Holdmullet") so the ring can be authored, positioned and sized in edit mode.
// The owning SpeciesBubble forwards pointer-down / pointer-up / per-frame ticks
// here; on a completed hold this reveals the food-web connections, and a short
// press is reported back as a tap.
//
// Ring resolution order:
//   1. An explicitly assigned Hold Ring image.
//   2. If this component is on a ring child (no SpeciesBubble here), the Image on
//      THIS GameObject — the authored ring you can see/size in edit mode.
//   3. Otherwise auto-create a hidden ring child from Hold Ring Sprite (legacy).
public class SpeciesBubbleHoldRing : MonoBehaviour
{
    [Tooltip("The radial-fill ring Image. Leave empty to use an Image on THIS object " +
             "(when the component is on a ring child), else one is auto-created from Hold Ring Sprite.")]
    public Image holdRing;
    [Tooltip("Sprite for an AUTO-created ring (used only when Hold Ring is empty and this " +
             "component sits on the bubble root). Ignored when you author the ring yourself.")]
    public Sprite holdRingSprite;
    [Tooltip("Tint for an AUTO-created ring. For an authored ring, set the colour on its Image.")]
    public Color holdRingColor = new Color(0.15f, 0.9f, 1f, 1f);
    [Tooltip("Size of an AUTO-created ring relative to the bubble (1 = same size). For an " +
             "authored ring, size it via its own RectTransform instead.")]
    public float holdRingScale = 1.06f;
    [Tooltip("Seconds the bubble must be held before the food-web connections reveal.")]
    public float holdDuration = 0.5f;

    private SpeciesBubble _bubble;
    private bool _resolved;
    private float _holdTimer;
    private bool _isHolding;
    private bool _longPressTriggered;

    void Start() { EnsureResolved(); }

    // Resolve the bubble + ring once, then hide the ring so an authored one doesn't show
    // until a hold. Lazy so it is robust to active state / script start order.
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

        // On a ring child (no SpeciesBubble on this object) the ring is this object's Image.
        bool onBubbleRoot = GetComponent<SpeciesBubble>() != null;
        if (!onBubbleRoot)
        {
            var self = GetComponent<Image>();
            if (self != null) { holdRing = self; ConfigureForRadialFill(holdRing); return; }
        }

        // Legacy: auto-create a ring child from the sprite (root-mounted, no authored ring).
        if (holdRingSprite != null) CreateHoldRing();
    }

    // Begin timing a press (called from SpeciesBubble.OnPointerDown).
    public void BeginHold()
    {
        EnsureResolved();
        _isHolding = true;
        _holdTimer = 0f;
        _longPressTriggered = false;
    }

    // Advance the hold each frame while pressed (called from SpeciesBubble.Update).
    public void Tick()
    {
        if (!_isHolding || _longPressTriggered) return;

        _holdTimer += Time.unscaledDeltaTime;

        // Grow the ring toward completion so holding reads as a deliberate gesture.
        float progress = Mathf.Clamp01(_holdTimer / holdDuration);
        SetHoldRing(progress);

        if (progress >= 1f)
        {
            _longPressTriggered = true;
            HideHoldRing(); // the food-web lines are the feedback now
            if (_bubble != null && FoodWebLines.Instance != null)
                FoodWebLines.Instance.ShowConnections(_bubble);
        }
    }

    // End a press (called from SpeciesBubble.OnPointerUp). Returns true if it was a
    // completed hold (connections were revealed), so the caller should NOT treat it as a tap.
    public bool EndHold()
    {
        _isHolding = false;
        HideHoldRing(); // clear a partial ring if the user let go early

        if (_longPressTriggered)
        {
            _longPressTriggered = false;
            if (FoodWebLines.Instance != null)
                FoodWebLines.Instance.HideConnections();
            return true;
        }
        return false;
    }

    // Abort an in-progress press with no side effects (e.g. the bubble became locked).
    public void CancelHold()
    {
        _isHolding = false;
        HideHoldRing();
    }

    // Make an Image behave as a radial progress ring without touching its sprite/colour/size.
    static void ConfigureForRadialFill(Image img)
    {
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Radial360;
        img.fillOrigin = (int)Image.Origin360.Top;
        img.fillClockwise = true;
        img.raycastTarget = false;
    }

    // Auto-create a ring child (legacy path when no ring is authored).
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
        rt.SetAsLastSibling(); // draw on top of the bubble art

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
