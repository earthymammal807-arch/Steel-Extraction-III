Shader "Custom/VoronoiDTE" 
{
    Properties 
    {
        _Scale          ("Voronoi Scale",    Range(1.0,  500.0)) = 15.0
        _BlackPoint     ("BlackPoint value", Range(0.0,   1.0)) = 0.0
        _WhitePoint     ("WhitePoint value", Range(0.0,   1.0)) = 0.24
        _NoiseScale     ("Noise Scale",      Range(0.0,  200.0)) = 12.0
        _EdgeFadeStart  ("Edge Fade Start",  Range(0.0, 1000.0)) = 20.0
        _NoiseFadeStart ("Noise Fade Start", Range(0.0, 1000.0)) = 15.0
        _DickLand ("Color", Vector) = (1.0, 0.0, 0.0, 0.0)

        // ── Restored Tiling and Offset fields ──────────────────
        _NormalMapA     ("Normal Map A (Mask = 0)", 2D) = "bump" {}
        _NormalScaleA   ("Normal Scale A", Range(0.0, 2.0)) = 1.0
        
        _NormalMapB     ("Normal Map B (Mask = 1)", 2D) = "bump" {}
        _NormalScaleB   ("Normal Scale B", Range(0.0, 2.0)) = 1.0
    }

    SubShader 
    {
        Tags 
        { 
            "RenderType"     = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue"          = "Geometry" 
        }

        Pass 
        {
            Name "ForwardLit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "VoronoiDTE.hlsl"
            #include "SimpleBrownNoise.hlsl"
            #include "QuickNormalMap.hlsl"

            struct Attributes 
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
            };

            struct Varyings 
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 tangentWS   : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float3 positionWS  : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float _Scale;
                float _WhitePoint;
                float _BlackPoint;
                float _NoiseScale;
                float _EdgeFadeStart;
                float _NoiseFadeStart;
                float3 _DickLand;
                
                float _NormalScaleA;
                float4 _NormalMapA_ST; // Unity uses this to pass Tiling/Offset to the CBUFFER
                
                float _NormalScaleB;
                float4 _NormalMapB_ST; // Unity uses this to pass Tiling/Offset to the CBUFFER
            CBUFFER_END

            TEXTURE2D(_NormalMapA); SAMPLER(sampler_NormalMapA);
            TEXTURE2D(_NormalMapB); SAMPLER(sampler_NormalMapB);
            

            Varyings vert(Attributes input) 
            {
                Varyings output;
                output.positionCS  = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS  = TransformObjectToWorld(input.positionOS.xyz);
                output.uv          = input.uv;

                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS    = normalInputs.normalWS;
                output.tangentWS   = normalInputs.tangentWS;
                output.bitangentWS = normalInputs.bitangentWS;

                return output;
            }

            float4 frag(Varyings input) : SV_Target 
            {
                // 1. Distance & Masking Fades
                float dist      = length(input.positionWS - _WorldSpaceCameraPos);
                float edgeFade  = saturate(dist / _EdgeFadeStart);          
                float noiseFade = saturate(1.0 - (dist / _NoiseFadeStart)); 

                // 2. Anti-Aliased Procedural Vorono
                float2 scaledUV = input.uv * _Scale;
                float  lines    = voronoi_DTE(scaledUV);
                float  delta    = fwidth(lines);

                float smoothBlack = lerp(_BlackPoint, 0.4, edgeFade) - delta;
                float smoothWhite = lerp(_WhitePoint, 0.6, edgeFade) + delta;
                lines = smoothstep(smoothBlack, smoothWhite, lines);

                // 3. Noise & Mask Calculation
                float noise = brownian_noise(scaledUV * _NoiseScale, 12) * noiseFade;
                float mask  = saturate(lines - noise);

                // ── 4. Apply Tiling & Offset to the normal map UVs ──
                float2 uvNormalA = TRANSFORM_TEX(input.uv, _NormalMapA);
                float2 uvNormalB = TRANSFORM_TEX(input.uv, _NormalMapB);

                // 5. Sample World Space Normal Maps via Include (using the scaled UVs)
                float3 normalA = GetWorldSpaceNormalMap(uvNormalA, input.normalWS, input.tangentWS, input.bitangentWS, _NormalMapA, sampler_NormalMapA, _NormalScaleA);
                float3 normalB = GetWorldSpaceNormalMap(uvNormalB, input.normalWS, input.tangentWS, input.bitangentWS, _NormalMapB, sampler_NormalMapB, _NormalScaleB);
                float3 finalNormal = normalize(lerp(normalA, normalB, mask));

                // 6. Minimal URP Surface Lighting Data
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS      = input.positionWS;
                lightingInput.normalWS        = finalNormal;
                lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lightingInput.bakedGI         = SampleSH(finalNormal);

                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif

                SurfaceData surface = (SurfaceData)0;
                surface.albedo     = _DickLand;
                surface.smoothness = 0.5;
                surface.alpha      = 1.0;

                return UniversalFragmentPBR(lightingInput, surface);
            }
            ENDHLSL
        }
    }
}
