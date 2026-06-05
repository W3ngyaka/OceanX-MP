#ifndef FISH_LIT_INSTANCED_UTILS
#define FISH_LIT_INSTANCED_UTILS

float4x4 CalculateUpdatedModelMatrix(float4x4 originalModelMatrix, float4x4 rotationMatrix) 
{
    return mul(originalModelMatrix, rotationMatrix);
}

VertexNormalInputs GetVertexNormalInputs(float3 normalOS, float4 tangentOS, float4x4 vertexRotationMatrix)
{
    VertexNormalInputs tbn;

    float4x4 updatedModelMatrix = CalculateUpdatedModelMatrix(UNITY_MATRIX_M, vertexRotationMatrix);

    // mikkts space compliant. only normalize when extracting normal at frag.
    real sign = real(tangentOS.w) * GetOddNegativeScale();
    tbn.normalWS = SafeNormalize(mul((float3x3)updatedModelMatrix, normalOS));
    tbn.tangentWS = real3(SafeNormalize(mul((float3x3)updatedModelMatrix, tangentOS.xyz)));
    tbn.bitangentWS = real3(cross(tbn.normalWS, float3(tbn.tangentWS))) * sign;
    return tbn;
}

float4x4 CreateRotationMatrix(float4 quaternion)
{
    // Precalculate coordinate products
    float x = quaternion.x * 2.0F;
    float y = quaternion.y * 2.0F;
    float z = quaternion.z * 2.0F;
    float xx = quaternion.x * x;
    float yy = quaternion.y * y;
    float zz = quaternion.z * z;
    float xy = quaternion.x * y;
    float xz = quaternion.x * z;
    float yz = quaternion.y * z;
    float wx = quaternion.w * x;
    float wy = quaternion.w * y;
    float wz = quaternion.w * z;

    // Calculate 3x3 matrix from orthonormal basis
    float4x4 m;
    m._m00 = 1.0f - (yy + zz); m._m10 = xy + wz; m._m20 = xz - wy; m._m30 = 0.0F;
    m._m01 = xy - wz; m._m11 = 1.0f - (xx + zz); m._m21 = yz + wx; m._m31 = 0.0F;
    m._m02 = xz + wy; m._m12 = yz - wx; m._m22 = 1.0f - (xx + yy); m._m32 = 0.0F;
    m._m03 = 0.0F; m._m13 = 0.0F; m._m23 = 0.0F; m._m33 = 1.0F;
    return m;
}

float4 LookRotation(float3 forward) 
{
    float3 t1 = normalize(cross(float3(0, 1, 0), forward));
    float3x3 m = float3x3(t1, cross(forward, t1), forward);

    float3 u = float3(m._m00, m._m01, m._m02);
    float3 v = float3(m._m10, m._m11, m._m12);
    float3 w = float3(m._m20, m._m21, m._m22);

    uint u_sign = (asuint(u.x) & 0x80000000);
    float t = v.y + asfloat(asuint(w.z) ^ u_sign);
    uint4 u_mask = uint4(((int)u_sign >> 31).xxxx);
    uint4 t_mask = uint4((asint(t) >> 31).xxxx);

    float tr = 1.0f + abs(u.x);

    uint4 sign_flips = uint4(0x00000000, 0x80000000, 0x80000000, 0x80000000) ^ (u_mask & uint4(0x00000000, 0x80000000, 0x00000000, 0x80000000)) ^ (t_mask & uint4(0x80000000, 0x80000000, 0x80000000, 0x00000000));

    float4 value = float4(tr, u.y, w.x, v.z) + asfloat(asuint(float4(t, v.x, u.z, w.y)) ^ sign_flips);   // +---, +++-, ++-+, +-++

    value = asfloat((asuint(value) & ~u_mask) | (asuint(value.zwxy) & u_mask));
    value = asfloat((asuint(value.wzyx) & ~t_mask) | (asuint(value) & t_mask));
    value = normalize(value);
    return value;
}

#endif // FISH_LIT_INSTANCED_UTILS