using UnityEngine;
using UnityEngine.UI;

// FFXIV-style recovery sweep for the Add button: a dark radial wipe over the icon that unwinds
// clockwise as the cooldown recovers, so a swallowed press reads as "not ready yet" instead of
// "the button is broken".
//
// Purely presentational. TabletAddRemoveUIGPU owns the actual gate and the interactable state;
// this only visualises what it reports, so the two can never disagree about whether Add is ready.
[RequireComponent(typeof(Image))]
public class ButtonCooldownOverlay : MonoBehaviour
{
    [Tooltip("The sweep image. Leave blank to use the Image on this GameObject.")]
    public Image overlay;

    [Tooltip("Colour of the un-recovered portion. Alpha controls how dark the icon looks while recovering.")]
    public Color sweepColor = new Color(0f, 0f, 0f, 0.55f);

    [Tooltip("Sweep clockwise from the top, like an FFXIV action icon.")]
    public bool clockwise = true;

    [Tooltip("Hide the overlay entirely when ready, so it can't tint the icon at rest.")]
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

    /// <summary>How much of the cooldown is still to run, 0 (ready) to 1 (just fired).</summary>
    private float RecoveryFraction()
    {
        var source = TabletAddRemoveUIGPU.Instance;
        if (source == null) return 0f;

        float duration = source.CooldownDuration;
        if (duration <= 0f) return 0f;   // cooldown disabled

        return Mathf.Clamp01(source.CooldownRemaining / duration);
    }

    /// <summary>One-time Image configuration: this is a radial fill, not a plain sprite.</summary>
    private void ApplyStyle()
    {
        overlay.raycastTarget = false;   // must never eat the button's own presses
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