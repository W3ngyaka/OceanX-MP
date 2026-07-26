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

        overlay.raycastTarget = false;   // must never eat the button's own presses
        overlay.type = Image.Type.Filled;
        overlay.fillMethod = Image.FillMethod.Radial360;
        overlay.fillOrigin = (int)Image.Origin360.Top;
        overlay.fillClockwise = clockwise;
        overlay.color = sweepColor;
        overlay.fillAmount = 0f;
    }

    void Update()
    {
        if (overlay == null) return;

        var src = TabletAddRemoveUIGPU.Instance;
        if (src == null)
        {
            overlay.fillAmount = 0f;
            if (hideWhenReady) overlay.enabled = false;
            return;
        }

        float dur = src.CooldownDuration;
        float amt = dur <= 0f ? 0f : Mathf.Clamp01(src.CooldownRemaining / dur);
        overlay.fillAmount = amt;
        if (hideWhenReady) overlay.enabled = amt > 0f;
    }
}
