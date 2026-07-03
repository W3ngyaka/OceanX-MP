#ifndef RAY_WING_MOTION_INCLUDED
#define RAY_WING_MOTION_INCLUDED

// ---------------------------------------------------------------------------
// OceanX MP — Ray wing vertical-flap vertex motion.
//
// Sibling to Fish_Swimming_Motion.hlsl, but for a stingray/ray:
//   - Displacement is VERTICAL (object-space Y-up after Unity import; the flat
//     disc's thickness axis). The fish version pushed sideways along length;
//     this pushes UP/DOWN so the wings flap like a real ray.
//   - Amplitude grows from the centreline out to the wingtips, so the disc
//     midline stays (near) still and the tips move most.
//   - The wave travels ACROSS the span (phase runs wingtip -> wingtip), so the
//     two wings lead/lag instead of snapping together.
//
// UV1 CONVENTION (must match the Blender bake exactly):
//   UV1.x = amplitude mask   : 0 at centreline (rigid) -> 1 at wingtips
//   UV1.y = spanwise phase    : 0 at one wingtip -> 0.5 centre -> 1 other tip
//
// Feed UV1 into the vertex shader as TEXCOORD1 (float2 wingUV : TEXCOORD1),
// same slot the fish shader used for tailMaskUV.
// ---------------------------------------------------------------------------

// Tunables (expose as material properties, mirror the fish shader's naming):
//   _WingFlapSpeed      : oscillation speed (rad/sec-ish; scaled by _Time.y)
//   _WingFlapAmplitude  : max vertical displacement at the wingtips (obj units)
//   _WingSpanWaves      : how many wave crests fit across the full span
//   _WingAmpFalloff     : shapes centre->tip amplitude ramp (1 = linear,
//                          >1 = flatter centre / sharper tips = stiller middle)
//   _WingTipEase        : optional extra tip softening (0 = off)

// tipAmp: shape the raw amplitude mask so the middle stays extra still.
float RayWing_Amplitude(float ampMask, float falloff)
{
    // pow keeps 0 at 0 and 1 at 1, but bows the curve so mid-span barely moves.
    return pow(saturate(ampMask), max(falloff, 1.0));
}

// Returns vertical (object-space) offset for this vertex.
// wingUV = UV1 (x=amplitude mask, y=spanwise phase 0..1)
float RayWing_VerticalOffset(
    float2 wingUV,
    float  time,
    float  flapSpeed,
    float  flapAmplitude,
    float  spanWaves,
    float  ampFalloff)
{
    float ampMask = RayWing_Amplitude(wingUV.x, ampFalloff);

    // Phase: base temporal oscillation + spanwise travel so the wave moves
    // out toward the tips rather than both wings hitting peak at once.
    // (wingUV.y - 0.5) makes the two sides symmetric about the centreline.
    float spanPhase = (wingUV.y - 0.5) * spanWaves * 6.2831853; // 2*PI per wave
    float phase     = time * flapSpeed + spanPhase;

    // Vertical displacement: zero on the centreline, grows to the tips.
    return sin(phase) * ampMask * flapAmplitude;
}

// Convenience: apply straight to an object-space position.
// Assumes Unity's Y-up object space after FBX import (vertical = +Y).
// If your ray imports flat in a different axis, change the .y below.
float3 RayWing_ApplyToPositionOS(
    float3 positionOS,
    float2 wingUV,
    float  time,
    float  flapSpeed,
    float  flapAmplitude,
    float  spanWaves,
    float  ampFalloff)
{
    float dy = RayWing_VerticalOffset(
        wingUV, time, flapSpeed, flapAmplitude, spanWaves, ampFalloff);
    positionOS.y += dy;   // <-- vertical axis. Change if your import axis differs.
    return positionOS;
}

#endif // RAY_WING_MOTION_INCLUDED
