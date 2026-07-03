#ifndef RAY_WING_MOTION
#define RAY_WING_MOTION

// -----------------------------------------------------------------------------
// OceanX MP - Ray wing motion (stingray undulation).
//
// Unity OBJECT-LOCAL space of stingray.fbx:
//     X = span (wingtip to wingtip)
//     Y = disc thickness  <- FLAP HAPPENS HERE (up/down)
//     Z = length (head to tail)  <- WAVE TRAVELS HERE (ripple sweeps head->tail)
//
// Two SEPARATE axes:
//   - Displacement is VERTICAL (Y): wingtips move up and down = the flap.
//   - Wave PHASE advances with Z: the up/down timing sweeps down the length,
//     so the flap ripples from head to tail like a real ray.
//   - Amplitude = baked UV1.x: 0 at centreline (still middle) -> 1 at tips.
//
// Properties (LitInput.hlsl CBUFFER):
//   _WingFlapSpeed, _WingFlapAmplitude, _WingSpanWaves (ripples down length),
//   _WingAmpFalloff
// -----------------------------------------------------------------------------

float3 ApplyRayWingMotion(float3 originalLocalSpacePosition, float2 wingUV,
    float flapSpeed, float flapAmplitude, float lengthWaves, float ampFalloff)
{
    // Amplitude: 0 at centreline, rising to tips; falloff (>=1) keeps mid still.
    float ampMask = pow(saturate(wingUV.x), max(ampFalloff, 1.0));

    // Temporal oscillation.
    float t = _Time.y * flapSpeed * 6.28318530718;

    // Travelling-wave phase DOWN THE LENGTH (Z): makes the flap ripple head->tail
    // instead of the whole wing snapping up/down at once.
    float lengthPhase = originalLocalSpacePosition.z * lengthWaves * 6.28318530718;

    // Displacement is VERTICAL (Y) = the flap. Scaled 0.01 to match the fish
    // shader's Blender->Unity amplitude convention.
    float yOffset = sin(t + lengthPhase) * ampMask * flapAmplitude * 0.01;

    return originalLocalSpacePosition + float3(0.0, yOffset, 0.0);
}

// Two-arg overload matching ApplyFishSwimmingMotion's call shape.
float3 ApplyRayWingMotion(float3 originalLocalSpacePosition, float2 wingUV)
{
    return ApplyRayWingMotion(originalLocalSpacePosition, wingUV,
        _WingFlapSpeed, _WingFlapAmplitude, _WingSpanWaves, _WingAmpFalloff);
}

#endif // RAY_WING_MOTION
