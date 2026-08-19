using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ButtonCooldownOverlay : MonoBehaviour
{
        public Image overlay;

        public Color sweepColor = new Color(0f, 0f, 0f, 0.55f);

        public bool clockwise = true;

        public bool hideWhenReady = true;

    void Awake()
    {
        if (overlay == null) overlay = GetComponent<Image>();
        if (overlay == null) return;

        ApplyStyle();
        SetSweep(0f);
    }

    void Update()
    {
        if (overlay == null) return;

        SetSweep(RecoveryFraction());
    }

    private float RecoveryFraction()
    {
        var source = TabletAddRemoveUIGPU.Instance;
        if (source == null) return 0f;

        float duration = source.CooldownDuration;
        if (duration <= 0f) return 0f;

        return Mathf.Clamp01(source.CooldownRemaining / duration);
    }

    private void ApplyStyle()
    {
        overlay.raycastTarget = false;
        overlay.type = Image.Type.Filled;
        overlay.fillMethod = Image.FillMethod.Radial360;
        overlay.fillOrigin = (int)Image.Origin360.Top;
        overlay.fillClockwise = clockwise;
        overlay.color = sweepColor;
    }

    private void SetSweep(float fraction)
    {
        overlay.fillAmount = fraction;
        if (hideWhenReady) overlay.enabled = fraction > 0f;
    }
}
