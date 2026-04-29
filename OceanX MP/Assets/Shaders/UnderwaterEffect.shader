Shader "Custom/UnderwaterEffect"
{
    Properties
    {
        _ShallowColor        ("Shallow Color",      Color)       = (0.02, 0.55, 0.52, 1)
        _DeepColor           ("Deep Color",         Color)       = (0.00, 0.10, 0.14, 1)
        _DepthBlend          ("Depth Blend",        Range(0,1))  = 0.55
        _CausticScale        ("Caustic Scale",      Float)       = 2.5
        _CausticSpeed        ("Caustic Speed",      Float)       = 0.10
        _CausticIntensity    ("Caustic Intensity",  Range(0,2))  = 0.35
        _FogColor            ("Fog Color",          Color)       = (0.00, 0.18, 0.20, 1)
        _FogDensity          ("Fog Density",        Range(0,1))  = 0.55
        _GodRayColor         ("God Ray Color",      Color)       = (0.55, 0.95, 0.75, 1)
        _GodRayIntensity     ("God Ray Intensity",  Range(0,3))  = 1.6
        _GodRaySpeed         ("God Ray Speed",      Float)       = 0.05
        _GodRayWidth         ("God Ray Width",      Range(1,20)) = 11.0
        _GodRayCount         ("God Ray Count",      Range(2,12)) = 8.0
        _TopGlow             ("Top Glow",           Range(0,3))  = 1.8
        _TopGlowColor        ("Top Glow Color",     Color)       = (0.60, 1.00, 0.80, 1)
        _TopGlowFalloff      ("Top Glow Falloff",   Range(1,20)) = 7.0
        _WarpStrength        ("Warp Strength",      Range(0,0.1))= 0.012
        _WarpSpeed           ("Warp Speed",         Float)       = 0.3
        _VignetteStrength    ("Vignette Strength",  Range(0,2))  = 1.1
        _AberrationStrength  ("Aberration",         Range(0,0.02))= 0.002
        _ColorTintStrength   ("Color Tint Strength",Range(0,1))  = 0.50
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "UnderwaterPass"
            HLSLPROGRAM
            #pragma vertex   FullscreenVert
            #pragma fragment UnderwaterFrag
            #pragma target   3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // URP RenderGraph Blit.hlsl binds the source as _BlitTexture / sampler_LinearClamp
            // Do NOT declare _MainTex — it won't receive the camera buffer in RenderGraph mode.

            TEXTURE2D(_CausticTex); SAMPLER(sampler_CausticTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float  _DepthBlend;
                float  _ColorTintStrength;
                float  _CausticScale;
                float  _CausticSpeed;
                float  _CausticIntensity;
                float4 _FogColor;
                float  _FogDensity;
                float4 _GodRayColor;
                float  _GodRayIntensity;
                float  _GodRaySpeed;
                float  _GodRayWidth;
                float  _GodRayCount;
                float  _TopGlow;
                float4 _TopGlowColor;
                float  _TopGlowFalloff;
                float  _WarpStrength;
                float  _WarpSpeed;
                float  _VignetteStrength;
                float  _AberrationStrength;
            CBUFFER_END

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5);
            }

            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i),            hash(i+float2(1,0)), f.x),
                            lerp(hash(i+float2(0,1)),hash(i+float2(1,1)), f.x), f.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0, a = 0.5;
                UNITY_UNROLL
                for (int i = 0; i < 5; i++) { v += a*noise(p); p = p*2.1 + 100.0; a *= 0.5; }
                return v;
            }

            float godRays(float2 uv, float t)
            {
                float rays = 0.0;
                int count = (int)_GodRayCount;
                for (int i = 0; i < 12; i++)
                {
                    if (i >= count) break;
                    float fi      = (float)i / _GodRayCount;
                    float sway    = sin(t * _GodRaySpeed + fi * 6.2831) * 0.04;
                    float cx      = fi + sway + 0.05;
                    float dist    = abs(uv.x - cx);
                    float shaft   = pow(max(0.0, 1.0 - dist * _GodRayWidth), 3.0);
                    float vertFade= pow(max(0.0, 1.0 - uv.y * 1.1), 0.6);
                    float flicker = 0.75 + 0.25 * sin(t * _GodRaySpeed * 3.0 + fi * 13.7);
                    rays += shaft * vertFade * flicker;
                }
                return rays * _GodRayIntensity;
            }

            float topGlow(float2 uv, float t)
            {
                float wave = fbm(float2(uv.x * 3.0 + t * 0.15, t * 0.1)) * 0.04;
                return pow(max(0.0, 1.0 - (uv.y + wave) * _TopGlowFalloff), 2.0) * _TopGlow;
            }

            float causticPattern(float2 uv, float t)
            {
                float2 q  = uv * _CausticScale;
                float  n1 = fbm(q + float2( t*_CausticSpeed,       t*_CausticSpeed*0.7));
                float  n2 = fbm(q + float2(-t*_CausticSpeed*0.8,   t*_CausticSpeed*0.5) + 3.2);
                return smoothstep(0.2, 0.8, 1.0 - abs(n1 - n2));
            }

            Varyings FullscreenVert(Attributes input) { return Vert(input); }

            half4 UnderwaterFrag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float  t  = _Time.y;

                // 1. Warp UVs for refraction ripple
                float2 warpUV   = uv * 3.5;
                float  wx       = fbm(warpUV + float2(t * _WarpSpeed, 0.0));
                float  wy       = fbm(warpUV + float2(0.0, t * _WarpSpeed * 0.7) + 5.3);
                float2 warpedUV = uv + float2(wx - 0.5, wy - 0.5) * _WarpStrength;

                // 2. Sample camera color via _BlitTexture (URP RenderGraph standard)
                float2 aberr = (uv - 0.5) * _AberrationStrength;
                float3 scene;
                scene.r = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, warpedUV + aberr).r;
                scene.g = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, warpedUV        ).g;
                scene.b = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, warpedUV - aberr).b;

                // 3. Water tint
                float  depthFactor = saturate(uv.y * 0.7 + _DepthBlend);
                float3 waterColor  = lerp(_ShallowColor.rgb, _DeepColor.rgb, depthFactor);
                scene = lerp(scene, waterColor, _ColorTintStrength);

                // 4. Caustics
                float caustFade = exp(-depthFactor * 2.5);
                float caust     = causticPattern(uv, t) * caustFade * _CausticIntensity;
                float2 cUV1     = uv * _CausticScale + float2( t*_CausticSpeed,      t*_CausticSpeed*0.6);
                float2 cUV2     = uv * _CausticScale - float2( t*_CausticSpeed*0.7,  t*_CausticSpeed);
                float  cTex     = SAMPLE_TEXTURE2D(_CausticTex, sampler_CausticTex, cUV1).r
                                * SAMPLE_TEXTURE2D(_CausticTex, sampler_CausticTex, cUV2).r * 2.0;
                scene += (caust + cTex * caustFade * _CausticIntensity) * float3(0.4, 0.9, 0.7);

                // 5. God rays
                scene += godRays(uv, t) * _GodRayColor.rgb;

                // 6. Top surface glow
                scene += topGlow(uv, t) * _TopGlowColor.rgb;

                // 7. Depth fog
                float fogAmt = saturate(depthFactor * _FogDensity * 1.8);
                scene = lerp(scene, _FogColor.rgb, fogAmt);

                // 8. Particles
                UNITY_UNROLL
                for (int p = 0; p < 12; p++)
                {
                    float fp  = (float)p;
                    float2 pp = float2(
                        frac(hash(float2(fp, 0.0)) + t * 0.012 * hash(float2(fp, 1.0))),
                        frac(hash(float2(fp, 2.0)) + t * 0.009)
                    );
                    scene += smoothstep(0.008, 0.0, length(uv - pp))
                             * 0.6 * float3(0.6, 1.0, 0.8) * (1.0 - depthFactor);
                }

                // 9. Vignette
                float2 vigUV = uv * 2.0 - 1.0;
                scene *= 1.0 - dot(vigUV, vigUV) * _VignetteStrength * 0.3;

                // 10. Tone map + kelp-forest green grade
                scene  = scene / (scene + 0.7) * 1.5;
                scene.g *= 1.08;
                scene  = pow(saturate(scene), float3(0.88, 0.85, 0.92));

                return half4(scene, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
