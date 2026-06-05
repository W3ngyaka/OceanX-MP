#ifndef UNIVERSAL_DEPTH_ONLY_PASS_INCLUDED
#define UNIVERSAL_DEPTH_ONLY_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif
#include "Assets/Junheng/Shaders/Fish/Shared/Fish_Swimming_Motion.hlsl"
#include "Fish_Lit_Instanced_Utils.hlsl"

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
    int boidGroupID = asint(boidInfo.boidID) & 0xFF;
    BoidRenderInfo boidRenderInfo = _BoidsRenderInfos[boidGroupID];

    // Apply swimming motion to the original vertex position. Use the swimming motion intensity to 
    // correctly adjust the swimming properties.
    float boidSwimIntensity = boidInfo.swimMotionIntensity;
    float sideToSideAmplitude = lerp(boidRenderInfo.minSideToSideAmplitude, boidRenderInfo.maxSideToSideAmplitude, boidSwimIntensity);
    float yawRotationAmplitude = lerp(boidRenderInfo.minYawRotationAmplitude, boidRenderInfo.maxYawRotationAmplitude, boidSwimIntensity);
    float rollRotationAmplitude = lerp(boidRenderInfo.minRollRotationAmplitude, boidRenderInfo.maxRollRotationAmplitude, boidSwimIntensity);
    float panningYawAmplitude = lerp(boidRenderInfo.minPanningYawAmplitude, boidRenderInfo.maxPanningYawAmplitude, boidSwimIntensity);
    float3 vertexPosition = ApplyFishSwimmingMotion(input.position.xyz, input.tailMaskUV, boidInfo.currentSwimTime, 
        sideToSideAmplitude, yawRotationAmplitude, rollRotationAmplitude, panningYawAmplitude);

    // Rotate the boid in the correct direction.
    float4 boidRotation = LookRotation(boidInfo.direction);
    float3 rotatedVertexPosition = mul(CreateRotationMatrix(boidRotation), float4(vertexPosition, 1.0)).xyz;

    // Calculate the final position of the boid.
    float3 position = (boidInfo.position - _SimulationAreaCenter) + rotatedVertexPosition;
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
