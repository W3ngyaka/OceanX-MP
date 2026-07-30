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

// ---- Resting-in-cave pose + mouth gape (OceanX cave AI, MorayCaveDirector) --------------------
// A moray whose head is near an active cave anchor "rests": its body is laid STRAIGHT BACK INTO the
// rock from the cave mouth (occluded by the reef, which hides the head-path trail bunching a truly
// still eel would otherwise show), its swim undulation fades out, and its lower jaw gapes rhythmically
// (buccal breathing). All driven by proximity to the anchors below, so it eases in/out as the eel
// arrives at / leaves its cave — no per-instance state needed. Empty (count 0) => everything below is
// inert and the eel renders exactly as the free-swimming path.
#define MORAY_MAX_REST_ANCHORS 8
float4 _MorayRestAnchorPos[MORAY_MAX_REST_ANCHORS]; // xyz = cave-mouth world pos (head sits here); w = rest WEIGHT 0..1
float4 _MorayRestAnchorDir[MORAY_MAX_REST_ANCHORS]; // xyz = head-out direction (head faces here, body curls back)
int   _MorayRestAnchorCount;      // number of active anchors (0 => no resting)
float _MorayRestRadius;           // MATCH radius: how near the head must be to bind to an anchor (metres)
float _MorayRestFullDistance;     // (unused - retained for binding compatibility)
float _MorayRestCurlRadius;       // radius (m) of the resting body's horizontal coil; smaller = tighter curl
float _MorayRestUndulationScale;  // swim-sway amplitude retained while fully resting (0..1, small)
float _MorayMouthMaxAngle;        // max jaw open angle (degrees); 0 => no gape
float _MorayMouthRate;            // breathing cycles per second
float _MorayMouthLength;          // object-space length of the jaw region back from the head tip
float _MorayMouthHingeY;          // object-space Y below which vertices count as lower jaw

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

// Resting rest-amount [0..1] for a head at `headPosWorld`. The amount is the WEIGHT the director ramps
// on the eel's own cave anchor (0 while swimming in / leaving, ramping to 1 once it has settled), NOT a
// function of live distance — so the eel swims in fully normally and doesn't flicker as it mills at the
// mouth. Distance is used only to BIND the eel to its nearest anchor (within _MorayRestRadius). Outputs
// the chosen anchor's head position + head-out direction. 0 anchors / none in range => 0 (free-swimming).
float MorayRestAmount(float3 headPosWorld, out float3 anchorPos, out float3 anchorDir)
{
    float bestDist = _MorayRestRadius;
    float weight   = 0.0;
    anchorPos = headPosWorld;
    anchorDir = float3(0, 0, 1);
    int count = min(_MorayRestAnchorCount, MORAY_MAX_REST_ANCHORS);
    [loop] for (int i = 0; i < count; i++)
    {
        float3 ap = _MorayRestAnchorPos[i].xyz;
        float  d  = distance(headPosWorld, ap);
        if (d < bestDist)
        {
            bestDist  = d;
            weight    = saturate(_MorayRestAnchorPos[i].w);
            anchorPos = ap;
            anchorDir = _MorayRestAnchorDir[i].xyz;
        }
    }
    return weight;
}

// A resting eel's body point at arclength `s` behind the head. The head sits at `headWorld` — the eel's
// OWN live head position, NOT the fixed cave point — facing `anchorDir` (out of the cave); the body arcs
// to the side on a horizontal coil of radius _MorayRestCurlRadius, receding into the cave. Anchoring to
// the live head means the pose does not translate the eel across the water into place (no "dragged to the
// cave"): it just reshapes the body and turns the head where the eel already swam to. Outputs the roll-free
// travel frame at that point (belly-down), like MoraySpinePoint.
float3 MorayRestSpinePoint(float3 headWorld, float3 anchorDir, float s,
                           out float3 axisRight, out float3 axisUp, out float3 axisFwd)
{
    float3 worldUp = float3(0.0, 1.0, 0.0);
    float3 fwd0 = anchorDir - worldUp * dot(anchorDir, worldUp);       // head-out, flattened to horizontal
    fwd0 = (dot(fwd0, fwd0) < 1e-5) ? anchorDir : normalize(fwd0);
    float3 dirIn = -fwd0;                                              // INTO the cave: the body extends this way
    float3 side = cross(worldUp, dirIn);
    side = (dot(side, side) < 1e-5) ? float3(1, 0, 0) : normalize(side);

    // Head tip (s=0) at headWorld; as s grows toward the tail the body recedes INTO the cave (dirIn) and
    // curls to the side. So the head pokes OUT (faces +fwd0 = anchorDir) with the body coiled in the hole.
    float  R      = max(_MorayRestCurlRadius, 0.05);
    float  ang    = s / R;                                             // constant-curvature arc
    float3 center = headWorld + side * R;
    float3 pos    = center - side * (R * cos(ang)) + dirIn * (R * sin(ang));
    float3 tangent = normalize(side * sin(ang) + dirIn * cos(ang));    // toward the tail (into the cave)
    float3 fwd     = -tangent;                                         // frame forward faces the HEAD (out), like the swim frame

    float3 right = cross(worldUp, fwd);
    right = (dot(right, right) < 1e-5) ? cross(float3(0, 0, 1), fwd) : right;
    right = normalize(right);
    float3 up = normalize(cross(fwd, right));

    axisRight = right;
    axisUp    = up;
    axisFwd   = fwd;
    return pos;
}

// Geometric lower-jaw gape (no mesh mask): rotates vertices near the head tip and below the hinge line
// open about a hinge across the mesh X axis. Tuned live via the _MorayMouth* params. gape 0 => no-op.
float3 MorayMouthDeform(float3 posOS, float gape)
{
    if (gape <= 1e-4 || _MorayMouthMaxAngle <= 0.0 || _MorayMouthLength <= 1e-4) return posOS;

    float sLocal = _MorayHeadLocalZ - posOS.z;                          // 0 at head tip, grows backward
    float inJaw  = 1.0 - smoothstep(0.0, _MorayMouthLength, sLocal);    // 1 at tip -> 0 at mouth length back
    float below  = saturate((_MorayMouthHingeY - posOS.y) / max(1e-3, _MorayMouthLength * 0.5)); // ventral
    float w = inJaw * below;
    if (w <= 1e-4) return posOS;

    float  ang   = radians(_MorayMouthMaxAngle) * gape * w;
    float2 hinge = float2(_MorayMouthHingeY, _MorayHeadLocalZ - _MorayMouthLength); // (y, z)
    float2 rel   = float2(posOS.y - hinge.x, posOS.z - hinge.y);
    float  ca = cos(ang), sa = sin(ang);
    float2 rot = float2(rel.x * ca - rel.y * sa, rel.x * sa + rel.y * ca);          // swing jaw down/open
    posOS.y = hinge.x + rot.x;
    posOS.z = hinge.y + rot.y;
    return posOS;
}

// Full per-vertex deformation. posOS = object-space vertex. Returns the sim-space position
// (already offset by _SimulationAreaCenter, exactly like the fish shader) and, via frameMatrix,
// the per-vertex rotation used to transform the normal/tangent.
float3 ApplyMoraySpine(float3 posOS, uint morayIndex, float3 headPosWorld, float3 headDir,
                       float currentSwimTime, out float4x4 frameMatrix)
{
    // How much this eel is resting in a cave, and where. Drives the pose, the mouth and the sway fade.
    float3 anchorPos, anchorDir;
    float  rest = MorayRestAmount(headPosWorld, anchorPos, anchorDir);
    anchorDir = (dot(anchorDir, anchorDir) < 1e-6) ? headDir : normalize(anchorDir);

    // Lower-jaw gape: only when resting, breathing on a slow sine (buccal pumping).
    float gape = rest * saturate(0.5 + 0.5 * sin(_Time.y * _MorayMouthRate * 6.28318530718));
    posOS = MorayMouthDeform(posOS, gape);

    float bodyLen = max(_MorayBodyLength, 1e-3);
    float s = clamp(_MorayHeadLocalZ - posOS.z, 0.0, bodyLen); // arclength behind the head

    float3 right, up, fwd;
    float3 spine = MoraySpinePoint(morayIndex, headPosWorld, headDir, s, right, up, fwd);

    // Resting: the body shape comes entirely from the head-path trail the eel actually swam (the director
    // routes it along a curling path into the cave, then the compute-shader freeze pins the head so the
    // trail holds that shape). So there is NO synthetic pose override here — `rest` only calms the swim
    // undulation (below) and drives the mouth, letting the real swum coil read as a settled, still eel.

    // The vertex's cross-section (its components perpendicular to the body axis) rides the frame.
    float3 offset = right * posOS.x + up * posOS.y;

    // Serpentine swim wave: a lateral (horizontal) anguilliform undulation travelling head->tail. This is
    // the moray's actual swimming motion when going straight — the path supplies only the turning curves.
    // Driven by _Time.y for a smooth, frame-rate-independent beat: the per-boid swim clock advances unevenly
    // for a slow fish, which is what made the wave look "framey". Amplitude concentrates toward the tail
    // (mask squared) so the head barely sways and the tail whips, like a real eel.
    // Amplitude ramps LINEARLY from _MorayUndulationHeadHold (fraction of body behind the head kept still)
    // out to full at the tail. Linear (not squared) so the sway begins close to the head instead of only
    // kicking in well down the body; lower the hold to start it even nearer the head. Faded out while
    // resting so a caved eel is nearly still.
    float ampMask = saturate((s / bodyLen - _MorayUndulationHeadHold) / max(1e-3, 1.0 - _MorayUndulationHeadHold));
    float phase = s * _MorayUndulationWaves * 6.28318530718
                - _Time.y * _MorayUndulationSpeed * 6.28318530718;
    float undScale = lerp(1.0, saturate(_MorayRestUndulationScale), rest);
    offset += right * (sin(phase) * ampMask * _MorayUndulationAmplitude * undScale);

    // Rotation matrix mapping object axes -> world frame (columns = right, up, fwd). Normals only.
    frameMatrix = float4x4(
        right.x, up.x, fwd.x, 0.0,
        right.y, up.y, fwd.y, 0.0,
        right.z, up.z, fwd.z, 0.0,
        0.0,     0.0,  0.0,   1.0);

    return (spine - _SimulationAreaCenter) + offset;
}

#endif // MORAY_SPINE_MOTION
