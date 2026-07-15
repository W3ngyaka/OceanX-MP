// URP lit shader for the seagrass / seaweed clumps (the SM_Algae_* meshes).
//
// It is an ordinary lit surface plus a vertex-stage sway, so the blades lean with a current instead of
// standing frozen. The sway is driven entirely by position and time -- no scripts, no per-instance data.
//
// HOW THE BEND IS ANCHORED
//   The algae meshes pivot at the base of the blade (verified: Algae_1..11 all have bounds.min.y ~= 0),
//   so a vertex's OBJECT-space Y is its height above the root. That is what the bend is masked by, which
//   is what keeps the roots planted in the sand while the tips travel.
//
//   !! DO NOT mark the algae renderers "Batching Static" !!
//   Static batching bakes vertices into world space, so positionOS.y would become a WORLD height. Every
//   blade's root would then read as a large height instead of 0, and whole clumps would slide back and
//   forth rigidly, roots and all, instead of bending. These renderers are all non-static today and the
//   SRP Batcher already handles them; leave them that way.
//
//   Roughly a third of the blades are tilted off world-up (by up to ~19 degrees). Their bend mask is
//   therefore up to ~5% short at the tip, which is not perceptible. The root still sits at
//   positionOS.y == 0, so it stays anchored no matter how the blade is rotated.
//
// KNOWN LIMITATION: normals are not re-derived after bending, so lighting reflects the blade's rest
// pose rather than its swayed pose. At these amplitudes it does not read as wrong, and re-deriving them
// would cost more than the effect is worth. Raise _SwayStrength far past ~0.4 and it will start to show.
Shader "OceanX/Seagrass_Lit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor]   _BaseColor("Base Color", Color) = (1,1,1,1)
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0

        [Header(Current Sway)]
        [Space(4)]
        // Horizontal direction the current pushes the blades. Y is ignored; only XZ is used.
        _SwayDirection("Current Direction (XZ)", Vector) = (1, 0, 0.35, 0)
        // Height of a fully grown blade, in metres. The bend mask reaches full strength here, so blades
        // SHORTER than this bend proportionally less -- which is what you want, and why one value covers
        // meshes ranging from 1.28m (Algae_1) to 3.22m (Algae_10).
        _BladeHeight("Blade Height (m)", Float) = 2.0
        // How far the tip travels from rest, in metres.
        _SwayStrength("Sway Strength (m at tip)", Range(0.0, 1.0)) = 0.18
        _SwaySpeed("Sway Speed", Range(0.0, 5.0)) = 0.9
        // Distance in metres between wave crests as the current rolls across the meadow. Large values
        // make nearby clumps lean together; small values make the meadow ripple.
        _SwayWavelength("Wave Length (m)", Range(0.5, 40.0)) = 12.0
        // Bend curve. 1 shears the whole blade uniformly; higher concentrates the bend toward the tip.
        _Stiffness("Stiffness", Range(1.0, 5.0)) = 2.0
        // Small fast ripple layered on top so the blades never look like a metronome.
        _FlutterStrength("Flutter Strength", Range(0.0, 0.3)) = 0.035
        _FlutterSpeed("Flutter Speed", Range(0.0, 12.0)) = 4.0

        [HideInInspector] _Cull("__cull", Float) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "Queue" = "Geometry"
        }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        // One CBUFFER shared by every pass. The SRP Batcher requires the layout to match exactly across
        // passes, so it lives here rather than being repeated per pass.
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            half   _Smoothness;
            half   _Metallic;
            float4 _SwayDirection;
            float  _BladeHeight;
            float  _SwayStrength;
            float  _SwaySpeed;
            float  _SwayWavelength;
            float  _Stiffness;
            float  _FlutterStrength;
            float  _FlutterSpeed;
            float  _Cull;
        CBUFFER_END

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        // World-space offset for one vertex.
        //   positionOS : object-space vertex position; its Y is the height above the root (see header).
        //   positionWS : the vertex's UNDISPLACED world position, used only to phase the wave.
        //
        // The phase comes from WORLD position rather than the object's origin so that the meadow reads as
        // a single current rolling through it, and so it does not depend on per-object data.
        float3 SeagrassSwayWS(float3 positionOS, float3 positionWS)
        {
            // 0 at the root, 1 at the tip.
            float heightFrac = saturate(positionOS.y / max(_BladeHeight, 1e-4));
            float bend = pow(heightFrac, _Stiffness);

            float2 dir = _SwayDirection.xz;
            dir = dot(dir, dir) > 1e-6 ? normalize(dir) : float2(1.0, 0.0);

            // TWO_PI / wavelength converts a distance in metres into radians of phase.
            float phase = dot(positionWS.xz, dir) * (TWO_PI / max(_SwayWavelength, 1e-3)) + _Time.y * _SwaySpeed;
            float wave  = sin(phase);
            float flutter = sin(phase * 2.7 + positionWS.y * 1.7 + _Time.y * _FlutterSpeed) * _FlutterStrength;

            // Total horizontal lean at this vertex. Contains `bend`, so it is 0 at the root.
            float lean = (wave * _SwayStrength + flutter) * bend;

            float3 offsetWS;
            offsetWS.xz = dir * lean;
            // A blade pivots on an arc, so leaning costs height: a tip leaning `lean` from a root `L`
            // away drops by L - sqrt(L*L - lean*lean), and lean^2/(2L) is that to first order. Without
            // this the blade visibly stretches as it leans.
            offsetWS.y  = -(lean * lean) / (2.0 * max(_BladeHeight, 1e-4));
            return offsetWS;
        }

        // Every pass must displace identically, or the shadow and depth passes will disagree with the
        // lit pass and the blades will self-shadow against their own rest pose.
        float3 SeagrassPositionWS(float3 positionOS)
        {
            float3 positionWS = TransformObjectToWorld(positionOS);
            return positionWS + SeagrassSwayWS(positionOS, positionWS);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex SeagrassForwardVertex
            #pragma fragment SeagrassForwardFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS       : POSITION;
                float3 normalOS         : NORMAL;
                float2 uv               : TEXCOORD0;
                float2 staticLightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv                       : TEXCOORD0;
                float3 positionWS               : TEXCOORD1;
                half3  normalWS                 : TEXCOORD2;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 3);
                half4  fogFactorAndVertexLight  : TEXCOORD4;
                float4 shadowCoord              : TEXCOORD5;
                // Unused while the project uses Light Probe Groups rather than Adaptive Probe Volumes
                // (URP asset: m_LightProbeSystem = 0), but OUTPUT_SH4 takes it as an out-param
                // regardless, so it has to exist.
                float4 probeOcclusion           : TEXCOORD6;
                float4 positionCS               : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings SeagrassForwardVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = SeagrassPositionWS(input.positionOS.xyz);
                half3  normalWS   = TransformObjectToWorldNormal(input.normalOS);

                output.uv         = TRANSFORM_TEX(input.uv, _BaseMap);
                output.positionWS = positionWS;
                output.normalWS   = normalWS;
                output.positionCS = TransformWorldToHClip(positionWS);

                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
                OUTPUT_SH4(positionWS, normalWS, GetWorldSpaceNormalizeViewDir(positionWS), output.vertexSH, output.probeOcclusion);

                half3 vertexLight = VertexLighting(positionWS, normalWS);
                half  fogFactor   = ComputeFogFactor(output.positionCS.z);
                output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
                output.shadowCoord = TransformWorldToShadowCoord(positionWS);
                return output;
            }

            half4 SeagrassForwardFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo     = albedo.rgb;
                surfaceData.alpha      = 1.0h;
                surfaceData.metallic   = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.occlusion  = 1.0h;
                surfaceData.normalTS   = half3(0.0h, 0.0h, 1.0h);

                InputData inputData = (InputData)0;
                inputData.positionWS      = input.positionWS;
                inputData.normalWS        = NormalizeNormalPerPixel(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord     = input.shadowCoord;
                inputData.fogCoord        = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
                inputData.vertexLighting  = input.fogFactorAndVertexLight.yzw;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                // Non-APV path only: the project is on Light Probe Groups, so PROBE_VOLUMES_L1/L2 are
                // never defined and URP's APV branch is unreachable here. 1702 of the algae renderers
                // ARE lightmapped, so LIGHTMAP_ON is live and this must be a real SAMPLE_GI rather than
                // a bare SampleSH.
                inputData.bakedGI    = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return half4(color.rgb, 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex SeagrassShadowVertex
            #pragma fragment SeagrassShadowFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings SeagrassShadowVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // Same displacement as the lit pass, so a blade's shadow tracks the blade it came from.
                float3 positionWS = SeagrassPositionWS(input.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);

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
                output.positionCS = positionCS;
                return output;
            }

            half4 SeagrassShadowFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex SeagrassDepthVertex
            #pragma fragment SeagrassDepthFragment
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings SeagrassDepthVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Must match the lit pass, or depth-based effects (SSAO, the water's depth fade) would
                // read the blade at its rest pose while it is drawn swayed.
                output.positionCS = TransformWorldToHClip(SeagrassPositionWS(input.positionOS.xyz));
                return output;
            }

            half4 SeagrassDepthFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Lit"
}
