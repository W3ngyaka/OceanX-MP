#ifndef RAY_WING_LIT_INSTANCED_INPUT_INCLUDED
#define RAY_WING_LIT_INSTANCED_INPUT_INCLUDED

#include "UnityIndirect.cginc"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Assets/Junheng/Shaders/Compute/BoidSimulationData.hlsl"
#include "Assets/Akil/Shaders/Ray_Wing_Lit/LitInput.hlsl"

// General boids simulation properties.
uint _BoidsBufferOffset;
float3 _SimulationAreaCenter;

// Buffer containing information about position, rotation, speed, acceleration, boid group ID and any other
// property specific to each individual boid. This information is provided for each boid in the simulation.
StructuredBuffer<BoidInfo> _Boids;

// Buffer containing the information about the swimming properties of each boid group. Use the
// boid group ID to get the correct information from this buffer. (Unused by the ray wing motion,
// which is driven by the material properties, but kept for parity with the boid render pipeline.)
StructuredBuffer<BoidRenderInfo> _BoidsRenderInfos;

#endif
