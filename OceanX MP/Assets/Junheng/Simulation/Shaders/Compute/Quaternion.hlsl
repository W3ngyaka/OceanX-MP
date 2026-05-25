#ifndef QUATERION_INCLUDED
#define QUATERION_INCLUDED

struct quaternion {
    float4 value;
};

float3 Multiply(quaternion q, float3 v)
{
    float3 t = 2 * cross(q.value.xyz, v);
    return v + q.value.w * t + cross(q.value.xyz, t);
}

quaternion InitializeQuaternion(float4 value)
{
    quaternion q;
    q.value = value;
    return q;
}

bool IsEqualUsingDot(float dotProduct)
{
    return dotProduct > 0.999999;
}

float Angle(quaternion a, quaternion b)
{
    float angle = min(abs(dot(a.value, b.value)), 1.0);
    return IsEqualUsingDot(angle) ? 0.0 : (acos(angle) * 2.0 * 57.29578);
}

quaternion NLerp(quaternion q1, quaternion q2, float t)
{
    float dt = dot(q1.value, q2.value);
    if(dt < 0.0)
    {
        q2.value = -q2.value;
    }

    quaternion q;
    q = InitializeQuaternion(lerp(q1.value, q2.value, t));
    q.value = normalize(q.value);
    return q;
}

quaternion Slerp(quaternion q1, quaternion q2, float t) 
{
    float dt = dot(q1.value, q2.value);
    if (dt < 0.0)
    {
        dt = -dt;
        q2.value = -q2.value;
    }

    if (dt < 0.9995)
    {
        float angle = acos(dt);
        float s = rsqrt(1.0 - dt * dt);    // 1.0f / sin(angle)
        float w1 = sin(angle * (1.0 - t)) * s;
        float w2 = sin(angle * t) * s;
        return InitializeQuaternion(q1.value * w1 + q2.value * w2);
    }
    else
    {
        // if the angle is small, use linear interpolation
        return NLerp(q1, q2, t);
    }
}

quaternion RotateTowards(quaternion from, quaternion to, float maxDegreesDelta)
{
    float angle = Angle(from, to);
    if(angle == 0.0)
    {
        return to;
    }

    return Slerp(from, to, min(1.0, maxDegreesDelta / angle));
}

quaternion InitializeQuaternion(float3x3 m)
{
    float3 u = m[0];
    float3 v = m[1];
    float3 w = m[2];

    uint u_sign = (asuint(u.x) & 0x80000000);
    float t = v.y + asfloat(asuint(w.z) ^ u_sign);
    uint4 u_mask = uint4(((int)u_sign >> 31).xxxx);
    uint4 t_mask = uint4((asint(t) >> 31).xxxx);

    float tr = 1.0f + abs(u.x);

    uint4 sign_flips = uint4(0x00000000, 0x80000000, 0x80000000, 0x80000000) ^ (u_mask & uint4(0x00000000, 0x80000000, 0x00000000, 0x80000000)) ^ (t_mask & uint4(0x80000000, 0x80000000, 0x80000000, 0x00000000));

    quaternion q;

    q.value = float4(tr, u.y, w.x, v.z) + asfloat(asuint(float4(t, v.x, u.z, w.y)) ^ sign_flips);   // +---, +++-, ++-+, +-++

    q.value = asfloat((asuint(q.value) & ~u_mask) | (asuint(q.value.zwxy) & u_mask));
    q.value = asfloat((asuint(q.value.wzyx) & ~t_mask) | (asuint(q.value) & t_mask));
    q.value = normalize(q.value);

    return q;
}

quaternion LookRotation(float3 forward, float3 up) 
{
    float3 t = normalize(cross(up, forward));
    return InitializeQuaternion(float3x3(t, cross(forward, t), forward));
}

#endif // QUATERNION_INCLUDED