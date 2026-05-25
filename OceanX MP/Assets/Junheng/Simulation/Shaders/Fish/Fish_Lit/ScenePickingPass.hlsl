#ifndef LIT_SCENE_PICKING_PASS
#define LIT_SCENE_PICKING_PASS

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Assets/Junheng/Simulation/Shaders/Fish/Shared/Fish_Swimming_Motion.hlsl"

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

Varyings ScenePickingVert(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float3 displacedPosition = ApplyFishSwimmingMotion(input.positionOS.xyz, input.tailMaskUV);
    output.positionCS = TransformObjectToHClip(displacedPosition);

    return output;
}

half4 ScenePickingFrag(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    return unity_SelectionID;
}

#endif // LIT_SCENE_PICKING_PASS