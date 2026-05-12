#ifndef UNIVERSAL_FORWARD_LIT_DEPTH_NORMALS_PASS_INCLUDED
#define UNIVERSAL_FORWARD_LIT_DEPTH_NORMALS_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
#include "Assets/Junheng/Simulation/Shaders/Fish/Shared/Fish_Swimming_Motion.hlsl"
#include "Fish_Lit_Instanced_Utils.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float2 tailMaskUV   : TEXCOORD1;
    float3 normal       : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS  : SV_POSITION;
    #if defined(_ALPHATEST_ON)
        float2 uv       : TEXCOORD1;
    #endif
    float3 normalWS     : TEXCOORD2;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};


Varyings DepthNormalsVertex(Attributes input, uint svInstanceID: SV_InstanceID)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    #if defined(_ALPHATEST_ON)
        output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    #endif

    uint instanceID = GetIndirectInstanceID(svInstanceID);
    BoidInfo boidInfo = _Boids[instanceID + _BoidsBufferOffset];
    int boidGroupID = asint(boidInfo.boidID) & 0xFF;
    BoidRenderInfo boidRenderInfo = _BoidsRenderInfos[boidGroupID];

    // Apply swimming motion to the original vertex position. Use the swimming motion intensity to 
    // correctly adjust the swimming properties.
    float boidSwimIntensity = boidInfo.swimMotionIntensity;
    float sideToSideAmplitude = lerp(boidRenderInfo.minSideToSideAmplitude, boidRenderInfo.maxSideToSideAmplitude, boidSwimIntensity);
    float yawRotationAmplitude = lerp(boidRenderInfo.minYawRotationAmplitude, boidRenderInfo.maxYawRotationAmplitude, boidSwimIntensity);
    float rollRotationAmplitude = lerp(boidRenderInfo.minRollRotationAmplitude, boidRenderInfo.maxRollRotationAmplitude, boidSwimIntensity);
    float panningYawAmplitude = lerp(boidRenderInfo.minPanningYawAmplitude, boidRenderInfo.maxPanningYawAmplitude, boidSwimIntensity);
    float3 vertexPosition = ApplyFishSwimmingMotion(input.positionOS.xyz, input.tailMaskUV, boidInfo.currentSwimTime, 
        sideToSideAmplitude, yawRotationAmplitude, rollRotationAmplitude, panningYawAmplitude);

    // Rotate the boid in the correct direction.
    float4 boidRotation = LookRotation(boidInfo.direction);
    float4x4 vertexRotationMatrix = CreateRotationMatrix(boidRotation);
    float3 rotatedVertexPosition = mul(vertexRotationMatrix, float4(vertexPosition, 1.0)).xyz;

    // Calculate the final position of the boid.
    float3 position = (boidInfo.position - _SimulationAreaCenter) + rotatedVertexPosition;
    output.positionCS = TransformObjectToHClip(position);

    // Calculate the normal vector in the world space based on the updated model matrix.
    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normal, input.tangentOS, vertexRotationMatrix);
    output.normalWS = normalInputs.normalWS;

    return output;
}

void DepthNormalsFragment(
    Varyings input
    , out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if defined(_ALPHATEST_ON)
        Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
    #endif

    #if defined(LOD_FADE_CROSSFADE)
        LODFadeCrossFade(input.positionCS);
    #endif

    #if defined(_GBUFFER_NORMALS_OCT)
    float3 normalWS = normalize(input.normalWS);
    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);           // values between [-1, +1], must use fp32 on some platforms.
    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0,  1]
    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);      // values between [ 0,  1]
    outNormalWS = half4(packedNormalWS, 0.0);
    #else
    float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
    outNormalWS = half4(normalWS, 0.0);
    #endif

    #ifdef _WRITE_RENDERING_LAYERS
        uint renderingLayers = GetMeshRenderingLayer();
        outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
    #endif
}

#endif
