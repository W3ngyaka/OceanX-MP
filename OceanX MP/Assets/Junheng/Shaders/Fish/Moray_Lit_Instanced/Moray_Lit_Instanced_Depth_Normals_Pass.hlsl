#ifndef MORAY_UNIVERSAL_FORWARD_LIT_DEPTH_NORMALS_PASS_INCLUDED
#define MORAY_UNIVERSAL_FORWARD_LIT_DEPTH_NORMALS_PASS_INCLUDED

// Moray depth-normals pass — writes the deformed normals to the camera normals texture (SSAO, and the
// deferred path's normal prepass). Same spine placement + optional normal flip as the other moray passes.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
#include "Assets/Junheng/Shaders/Fish/Fish_Lit_Instanced/Fish_Lit_Instanced_Utils.hlsl"
// Trail buffers + ApplyMoraySpine come from Moray_Lit_Instanced_Input.hlsl, included by the .shader.

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
    uint morayIndex = instanceID;

    float4x4 vertexRotationMatrix;
    float3 position;
    if (_MorayDebugStraight > 0)
    {
        float4 boidRotation = LookRotation(boidInfo.direction);
        vertexRotationMatrix = CreateRotationMatrix(boidRotation);
        float3 rotated = mul(vertexRotationMatrix, float4(input.positionOS.xyz, 1.0)).xyz;
        position = (boidInfo.position - _SimulationAreaCenter) + rotated;
    }
    else
    {
        position = ApplyMoraySpine(input.positionOS.xyz, morayIndex, boidInfo.position,
            boidInfo.direction, boidInfo.currentSwimTime, vertexRotationMatrix);
    }

    output.positionCS = TransformObjectToHClip(position);

    float3 normalOS  = _MorayFlipNormals > 0 ? -input.normal        : input.normal;
    float4 tangentOS = _MorayFlipNormals > 0 ? float4(-input.tangentOS.xyz, input.tangentOS.w) : input.tangentOS;
    VertexNormalInputs normalInputs = GetVertexNormalInputs(normalOS, tangentOS, vertexRotationMatrix);
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
    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
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
