#ifndef UNIVERSAL_SHADOW_CASTER_PASS_INCLUDED
#define UNIVERSAL_SHADOW_CASTER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif
#include "Assets/Junheng/Simulation/Shaders/Fish/Shared/Fish_Swimming_Motion.hlsl"
#include "Fish_Lit_Instanced_Utils.hlsl"

// Shadow Casting Light geometric parameters. These variables are used when applying the shadow Normal Bias and are set by UnityEngine.Rendering.Universal.ShadowUtils.SetupShadowCasterConstantBuffer in com.unity.render-pipelines.universal/Runtime/ShadowUtils.cs
// For Directional lights, _LightDirection is used when applying shadow Normal Bias.
// For Spot lights and Point lights, _LightPosition is used to compute the actual light direction because it is different at each shadow caster geometry vertex.
float3 _LightDirection;
float3 _LightPosition;

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
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
};

float4 GetShadowPositionHClip(Attributes input)
{
    float3 displacedPosition = ApplyFishSwimmingMotion(input.positionOS.xyz, input.tailMaskUV);
    float3 positionWS = TransformObjectToWorld(displacedPosition);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif

    return positionCS;
}

Varyings ShadowPassVertex(Attributes input, uint svInstanceID: SV_InstanceID)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

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
    float3 rotatedVertexPosition = mul(CreateRotationMatrix(boidRotation), float4(vertexPosition, 1.0)).xyz;

    // Calculate the final position of the boid.
    float3 position = (boidInfo.position - _SimulationAreaCenter) + rotatedVertexPosition;
    output.positionCS = TransformObjectToHClip(position);
    return output;
}

half4 ShadowPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);

    #if defined(_ALPHATEST_ON)
        Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
    #endif

    #if defined(LOD_FADE_CROSSFADE)
        LODFadeCrossFade(input.positionCS);
    #endif

    return 0;
}

#endif
