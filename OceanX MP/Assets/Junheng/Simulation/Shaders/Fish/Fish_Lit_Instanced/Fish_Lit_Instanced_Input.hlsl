#ifndef FISH_LIT_INSTANCED_INPUT_INCLUDED
#define FISH_LIT_INSTANCED_INPUT_INCLUDED

#include "UnityIndirect.cginc"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Assets/Junheng/Simulation/Shaders/Compute/BoidSimulationData.hlsl"
#include "Fish_Lit_Input.hlsl"

// General boids simulation properties.
uint _BoidsBufferOffset;  
float3 _SimulationAreaCenter;

// Buffer containing information about position, rotation, speed, acceleration, boid group ID and any other
// property specific to each individual boid. This information is provided for each boid in the simulation.
StructuredBuffer<BoidInfo> _Boids;

// Buffer containing the information aboud the swimming properties of each boid group. Use the
// boid group ID to get the correct information from this buffer.
StructuredBuffer<BoidRenderInfo> _BoidsRenderInfos;

#endif