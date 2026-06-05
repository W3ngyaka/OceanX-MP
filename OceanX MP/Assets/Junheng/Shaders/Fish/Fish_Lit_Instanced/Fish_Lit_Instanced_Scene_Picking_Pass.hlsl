#ifndef LIT_SCENE_PICKING_PASS
#define LIT_SCENE_PICKING_PASS

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Assets/Junheng/Shaders/Fish/Shared/Fish_Swimming_Motion.hlsl"
#include "Fish_Lit_Instanced_Utils.hlsl"

float4 _SelectionID;

struct Attributes
{
    float4 positionOS : POSITION;
    float2 tailMaskUV : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings ScenePickingVert(Attributes input, uint svInstanceID: SV_InstanceID)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

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

half4 ScenePickingFrag(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    return unity_SelectionID;
}

#endif // LIT_SCENE_PICKING_PASS