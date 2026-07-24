#ifndef MORAY_UNIVERSAL_DEPTH_ONLY_PASS_INCLUDED
#define MORAY_UNIVERSAL_DEPTH_ONLY_PASS_INCLUDED

// Moray depth-only pass — same spine placement as the forward pass, position only. Kept present so
// URP depth-priming / depth-prepass configurations write the moray's real (deformed) depth instead
// of falling back to the rigid mesh silhouette.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif
#include "Assets/Junheng/Shaders/Fish/Fish_Lit_Instanced/Fish_Lit_Instanced_Utils.hlsl"
// Trail buffers + ApplyMoraySpine come from Moray_Lit_Instanced_Input.hlsl, included by the .shader.

struct Attributes
{
    float4 position     : POSITION;
    float2 texcoord     : TEXCOORD0;
    float2 tailMaskUV   : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    #if defined(_ALPHATEST_ON)
        float2 uv       : TEXCOORD0;
    #endif
    float4 positionCS   : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings DepthOnlyVertex(Attributes input, uint svInstanceID: SV_InstanceID)
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

    float3 position;
    if (_MorayDebugStraight > 0)
    {
        float3 rotated = mul(CreateRotationMatrix(LookRotation(boidInfo.direction)), float4(input.position.xyz, 1.0)).xyz;
        position = (boidInfo.position - _SimulationAreaCenter) + rotated;
    }
    else
    {
        float4x4 unusedFrame;
        position = ApplyMoraySpine(input.position.xyz, morayIndex, boidInfo.position,
            boidInfo.direction, boidInfo.currentSwimTime, unusedFrame);
    }

    output.positionCS = TransformObjectToHClip(position);
    return output;
}

half DepthOnlyFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if defined(_ALPHATEST_ON)
        Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
    #endif

    #if defined(LOD_FADE_CROSSFADE)
        LODFadeCrossFade(input.positionCS);
    #endif

    return input.positionCS.z;
}
#endif
