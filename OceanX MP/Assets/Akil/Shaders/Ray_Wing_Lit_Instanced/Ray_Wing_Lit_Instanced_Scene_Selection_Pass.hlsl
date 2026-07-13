#ifndef LIT_SCENE_SELECTION_PASS
#define LIT_SCENE_SELECTION_PASS

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#include "Assets/Akil/Shaders/Shared/Ray_Wing_Motion.hlsl"
#include "Assets/Akil/Shaders/Ray_Wing_Lit_Instanced/Ray_Wing_Lit_Instanced_Utils.hlsl"

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

Varyings SceneSelectionVert(Attributes input, uint svInstanceID: SV_InstanceID)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    uint instanceID = GetIndirectInstanceID(svInstanceID);
    BoidInfo boidInfo = _Boids[instanceID + _BoidsBufferOffset];

    // Apply wing-flap motion to the original vertex position.
    float3 vertexPosition = ApplyRayWingMotion(input.positionOS.xyz, input.tailMaskUV);

    // Rotate the boid in the correct direction.
    float4 boidRotation = LookRotation(boidInfo.direction);
    float3 rotatedVertexPosition = mul(CreateRotationMatrix(boidRotation), float4(vertexPosition, 1.0)).xyz;

    // Calculate the final position of the boid.
    float3 position = (boidInfo.position - _SimulationAreaCenter) + rotatedVertexPosition;
    output.positionCS = TransformObjectToHClip(position);
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

    return output;
}

half4 SceneSelectionFrag(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    return half4(_ObjectId, _PassValue, 1.0, 1.0);
}

#endif // LIT_SCENE_SELECTION_PASS
