#ifndef MORAY_SPINE_MOTION
#define MORAY_SPINE_MOTION

// -----------------------------------------------------------------------------
// OceanX MP - Moray serpentine spine sweep (path-following eel body).
//
// The other fish are drawn by rotating a rigid mesh to face travel direction
// (LookRotation(direction)) and adding a canned sine wobble. On a long eel that
// reads as a stiff plank pivoting about its middle. Instead, this sweeps the body
// along the HEAD'S RECENT PATH:
//
//   - The simulation appends the moray's head position to a per-moray ring buffer
//     (_MorayTrail) at a fixed arclength spacing (_MorayTrailSpacing). See the
//     compute kernel's "Moray trail append".
//   - Here, each vertex's along-body coordinate (object-space Z, head at +Z) maps
//     to an arclength `s` BEHIND the head. We sample the ring at `s`, get a point
//     and a travel frame, and plant the vertex's cross-section (its X/Y) there.
//   - So when the head turns, the bend is baked into the trail and flows down the
//     body as it advances: genuine slither that wraps around coral.
//
// Object-local space of giant-moray.fbx (matches the fish/ray convention):
//     X = lateral (left/right of the body)
//     Y = dorsal/ventral (back/belly)
//     Z = length (head at +Z, tail at -Z)  <- consumed as the arclength parameter
//
// The frame is deliberately ROLL-FREE and belly-down (built from world up), which
// avoids Frenet twist-flips and keeps a bottom-hugging eel's belly toward the
// seabed. Degenerate stretches (freshly seeded ring, or a near-vertical dive) fall
// back to the boid's heading, so a just-spawned moray renders as a straight eel
// along its direction and relaxes into trailing as it swims.
// -----------------------------------------------------------------------------

// Bound per-frame to the moray material by BoidSpawnerGPU (moray spawner only).
// Same ComputeBuffers the kernel writes as UAVs, bound here read-only.
StructuredBuffer<float4> _MorayTrail;       // xyz = world sample position
StructuredBuffer<int>    _MorayTrailCursor; // newest ring index per moray

int   _MorayTrailCount;          // K ring samples per moray
float _MorayTrailSpacing;        // metres between samples (Δs)
float _MorayHeadLocalZ;          // object-space Z of the head tip (mesh bounds max Z)
float _MorayBodyLength;          // object-space head->tail length (mesh bounds size Z)
float _MorayUndulationAmplitude; // lateral swim sway (metres) at the tail; 0 = path only
float _MorayUndulationWaves;     // number of wavelengths along the body length
float _MorayUndulationSpeed;     // swim-wave beat (cycles/sec)
int   _MorayDebugStraight;       // 1 => bypass the spine and render the rigid straight eel (A/B compare)
int   _MorayFlipNormals;         // 1 => negate object-space normals (for a mirrored / reverse-wound FBX import)
float _MoraySmoothingWindow;     // arclength (metres) each side used to estimate a steady body tangent
float _MorayUndulationHeadHold;  // fraction of body behind the head kept still; smaller => sway starts nearer the head

// Position at arclength `s` behind the head. Linear interp with the continuous headProgress offset:
// samples are recorded at fixed spacing but the head moves CONTINUOUSLY between recordings, so
// headProgress (how far the head currently leads the newest sample, 0..spacing) makes the whole body
// glide and each append seamless (otherwise the rear pinned to stationary samples and hopped — the old
// "lags forward" artifact). Split out so the tangent can be estimated over a WIDE baseline.
float3 MoraySamplePos(int morayBase, int K, int cursor, float spacing,
                      float3 headPos, float3 newest, float headProgress, float s)
{
    s = max(s, 0.0);
    if (s <= headProgress)
    {
        float f = (headProgress > 1e-5) ? saturate(s / headProgress) : 0.0;
        return lerp(headPos, newest, f);
    }
    float bb = min((s - headProgress) / spacing, (float)(K - 2) - 1e-3);
    int   n0 = (int)floor(bb);
    float f  = bb - (float)n0;
    int idx0 = ((cursor - n0)       % K + K) % K;
    int idx1 = ((cursor - (n0 + 1)) % K + K) % K;
    return lerp(_MorayTrail[morayBase + idx0].xyz, _MorayTrail[morayBase + idx1].xyz, f);
}

// World-space spine point at arclength `s` behind the head, plus the roll-free travel frame there.
//
// The travel direction (which orients each body slice) is estimated over a WINDOW of
// _MoraySmoothingWindow metres either side of the sample, NOT between adjacent samples. When the head is
// shoved off a rock by the penetration backstop the trail gets a sharp kink; a narrow tangent would swing
// the per-slice frame hard, so the body "jitters / rotates to follow". A wide baseline low-passes the
// kink, so the body flows through it smoothly. Larger window = steadier but rounds off genuine tight
// curves; smaller = crisper but jitterier near kinks.
float3 MoraySpinePoint(uint morayIndex, float3 headPos, float3 headDir, float s,
                       out float3 axisRight, out float3 axisUp, out float3 axisFwd)
{
    int   K         = max(_MorayTrailCount, 2);
    int   cursor    = _MorayTrailCursor[morayIndex];
    int   morayBase = (int)morayIndex * K;
    float spacing   = max(_MorayTrailSpacing, 1e-4);

    float3 newest       = _MorayTrail[morayBase + cursor].xyz;
    float  headProgress = min(length(headPos - newest), spacing);

    float3 pos = MoraySamplePos(morayBase, K, cursor, spacing, headPos, newest, headProgress, s);

    // Tangent that orients each body slice. Baseline defaults to half a sample spacing (tight: the frame
    // follows the local curve, so the body genuinely bends along the path). _MoraySmoothingWindow widens
    // it to low-pass kink jitter — but too wide decouples orientation from the curve and the body flattens
    // to a rigid plank, so keep it modest.
    float  w      = max(_MoraySmoothingWindow, spacing * 0.5);
    float3 ahead  = MoraySamplePos(morayBase, K, cursor, spacing, headPos, newest, headProgress, s - w);
    float3 behind = MoraySamplePos(morayBase, K, cursor, spacing, headPos, newest, headProgress, s + w);
    float3 fwd    = ahead - behind;                            // toward the head
    fwd = (dot(fwd, fwd) < 1e-8) ? headDir : normalize(fwd);   // seeded / not-yet-moved => heading

    // Roll-free, belly-down frame from world up.
    float3 worldUp = float3(0.0, 1.0, 0.0);
    float3 right   = cross(worldUp, fwd);
    if (dot(right, right) < 1e-5) right = cross(float3(0.0, 0.0, 1.0), fwd); // near-vertical dive fallback
    right = normalize(right);
    float3 up = normalize(cross(fwd, right));

    axisRight = right;
    axisUp    = up;
    axisFwd   = fwd;
    return pos;
}

// Full per-vertex deformation. posOS = object-space vertex. Returns the sim-space position
// (already offset by _SimulationAreaCenter, exactly like the fish shader) and, via frameMatrix,
// the per-vertex rotation used to transform the normal/tangent.
float3 ApplyMoraySpine(float3 posOS, uint morayIndex, float3 headPosWorld, float3 headDir,
                       float currentSwimTime, out float4x4 frameMatrix)
{
    float bodyLen = max(_MorayBodyLength, 1e-3);
    float s = clamp(_MorayHeadLocalZ - posOS.z, 0.0, bodyLen); // arclength behind the head

    float3 right, up, fwd;
    float3 spine = MoraySpinePoint(morayIndex, headPosWorld, headDir, s, right, up, fwd);

    // The vertex's cross-section (its components perpendicular to the body axis) rides the frame.
    float3 offset = right * posOS.x + up * posOS.y;

    // Serpentine swim wave: a lateral (horizontal) anguilliform undulation travelling head->tail. This is
    // the moray's actual swimming motion when going straight — the path supplies only the turning curves.
    // Driven by _Time.y for a smooth, frame-rate-independent beat: the per-boid swim clock advances unevenly
    // for a slow fish, which is what made the wave look "framey". Amplitude concentrates toward the tail
    // (mask squared) so the head barely sways and the tail whips, like a real eel.
    // Amplitude ramps LINEARLY from _MorayUndulationHeadHold (fraction of body behind the head kept still)
    // out to full at the tail. Linear (not squared) so the sway begins close to the head instead of only
    // kicking in well down the body; lower the hold to start it even nearer the head.
    float ampMask = saturate((s / bodyLen - _MorayUndulationHeadHold) / max(1e-3, 1.0 - _MorayUndulationHeadHold));
    float phase = s * _MorayUndulationWaves * 6.28318530718
                - _Time.y * _MorayUndulationSpeed * 6.28318530718;
    offset += right * (sin(phase) * ampMask * _MorayUndulationAmplitude);

    // Rotation matrix mapping object axes -> world frame (columns = right, up, fwd). Normals only.
    frameMatrix = float4x4(
        right.x, up.x, fwd.x, 0.0,
        right.y, up.y, fwd.y, 0.0,
        right.z, up.z, fwd.z, 0.0,
        0.0,     0.0,  0.0,   1.0);

    return (spine - _SimulationAreaCenter) + offset;
}

#endif // MORAY_SPINE_MOTION
