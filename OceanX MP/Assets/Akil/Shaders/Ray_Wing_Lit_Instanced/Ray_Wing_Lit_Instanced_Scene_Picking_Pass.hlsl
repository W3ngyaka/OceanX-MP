#ifndef LIT_SCENE_PICKING_PASS
#define LIT_SCENE_PICKING_PASS

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Assets/Akil/Shaders/Shared/Ray_Wing_Motion.hlsl"
#include "Assets/Akil/Shaders/Ray_Wing_Lit_Instanced/Ray_Wing_Lit_Instanced_Utils.hlsl"

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

    // Apply wing-flap motion to the original vertex position.
    float3 vertexPosition = ApplyRayWingMotion(input.positionOS.xyz, input.tailMaskUV);

    // Sweep the tail toward the direction the ray is turning (signedTurnRate from the sim).
    vertexPosition = ApplyRayTurnTailBend(vertexPosition, boidInfo.signedTurnRate);

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
