#ifndef MORAY_LIT_INSTANCED_INPUT_INCLUDED
#define MORAY_LIT_INSTANCED_INPUT_INCLUDED

// Reuse the fish instanced input verbatim — it declares _Boids / _BoidsRenderInfos /
// _BoidsBufferOffset / _SimulationAreaCenter and pulls in BoidSimulationData + Fish_Lit_Input.
// Then layer on the moray trail buffers + spine uniforms (Moray_Spine_Motion needs
// _SimulationAreaCenter, so it MUST come after the fish input include).
#include "Assets/Junheng/Shaders/Fish/Fish_Lit_Instanced/Fish_Lit_Instanced_Input.hlsl"
#include "Moray_Spine_Motion.hlsl"

#endif // MORAY_LIT_INSTANCED_INPUT_INCLUDED
