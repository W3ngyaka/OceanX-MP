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

// -----------------------------------------------------------------------------
// Turn-driven tail sweep (instanced / boid ray only).
//
// signedTurn comes from the boid buffer (BoidInfo.signedTurnRate), written by the
// simulation: sign = which way the ray is banking, magnitude in [0,1] = how hard.
//
// The stingray.fbx tail is the -Z (head/disc is +Z, tail whip runs to ~z=-3.6),
// so vertices behind the pivot are swept sideways along X (the span/lateral axis).
// The sweep grows from 0 at the pivot to its max at the tail tip via a power curve,
// which reads as the tail curving into the turn rather than shearing rigidly.
//
// Properties (LitInput.hlsl CBUFFER):
//   _TailTurnAmplitude (lateral units at the tip; negative flips the sweep side),
//   _TailTurnPivotZ (local Z where the sweep begins), _TailTurnLength (pivot->tip
//   span used to normalise), _TailTurnExponent (whip curve sharpness).
// -----------------------------------------------------------------------------
float3 ApplyRayTurnTailBend(float3 localPos, float signedTurn,
    float amplitude, float pivotZ, float length, float exponent)
{
    // 0 across the disc/body (z >= pivot), ramping to 1 at the tail tip (toward -Z).
    float tailFactor = saturate((pivotZ - localPos.z) / max(length, 1e-3));
    tailFactor = pow(tailFactor, max(exponent, 1e-3));

    // Lateral (span = X) sweep toward the turn direction.
    float xOffset = signedTurn * amplitude * tailFactor;
    return localPos + float3(xOffset, 0.0, 0.0);
}

// Convenience overload pulling the tuning from the material properties.
float3 ApplyRayTurnTailBend(float3 localPos, float signedTurn)
{
    return ApplyRayTurnTailBend(localPos, signedTurn,
        _TailTurnAmplitude, _TailTurnPivotZ, _TailTurnLength, _TailTurnExponent);
}

#endif // RAY_WING_MOTION
