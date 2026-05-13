#ifndef LIT_SCENE_SELECTION_PASS
#define LIT_SCENE_SELECTION_PASS

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#include "Assets/Junheng/Simulation/Shaders/Fish/Shared/Fish_Swimming_Motion.hlsl"

int _ObjectId;
int _PassValue;

struct Attributes
{
    float4 positionOS : POSITION;
    float2 texcoord   : TEXCOORD0;
    float2 tailMaskUV   : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings SceneSelectionVert(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float3 displacedPosition = ApplyFishSwimmingMotion(input.positionOS.xyz, input.tailMaskUV);
    output.positionCS = TransformObjectToHClip(displacedPosition);
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

    return output;
}

half4 SceneSelectionFrag(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    return half4(_ObjectId, _PassValue, 1.0, 1.0);
}

#endif // LIT_SCENE_SELECTION_PASS