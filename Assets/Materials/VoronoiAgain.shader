Shader "Custom/VoronoiDTE" 
{
    Properties 
    {
        _Scale      ("Voronoi Scale",    Range(1.0,  500.0)) = 15.0
        _BlackPoint ("BlackPoint value", Range(0.0,   1.0)) = 0.0
        _WhitePoint ("WhitePoint value", Range(0.0,   1.0)) = 0.24
        _BumpStrength ("Bump Strength",  Range(0.0,  200.0)) = 5.0
        _NoiseScale ("Noise Scale",      Range(0.0,  200.0)) = 12.0
        _BumpFadeStart  ("Bump Fade Start",  Range(0.0, 1000.0)) = 30.0
        _EdgeFadeStart  ("Edge Fade Start",  Range(0.0, 1000.0)) = 20.0
        _NoiseFadeStart ("Noise Fade Start", Range(0.0, 1000.0)) = 15.0
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
            #include "FloatToNormal.hlsl"

            // ── STRUCTS ───────────────────────────────────────────────

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

            // ── CONSTANT BUFFER ───────────────────────────────────────

            CBUFFER_START(UnityPerMaterial)
                float _Scale;
                float _WhitePoint;
                float _BlackPoint;
                float _BumpStrength;
                float _NoiseScale;
                float _BumpFadeStart;
                float _EdgeFadeStart;
                float _NoiseFadeStart;
            CBUFFER_END

            // ── VERTEX SHADER ─────────────────────────────────────────

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

            // ── FRAGMENT SHADER ───────────────────────────────────────

            float4 frag(Varyings input) : SV_Target 
            {
                // ── Distance from camera ──────────────────────────────
                float dist = length(input.positionWS - _WorldSpaceCameraPos);

                // ── LOD fades (all 0→1 as distance increases) ─────────
                float bumpFade  = saturate(1.0 - (dist / _BumpFadeStart));  // bumps flatten at distance
                float edgeFade  = saturate(dist / _EdgeFadeStart);          // edges soften at distance
                float noiseFade = saturate(1.0 - (dist / _NoiseFadeStart)); // noise fades first

                // ── Voronoi ───────────────────────────────────────────
                float2 scaledUV = input.uv * _Scale;
                float  lines    = voronoi_DTE(scaledUV);

                // Soften black/white contrast at distance to kill aliasing
                float adjustedBlack = lerp(_BlackPoint, 0.4, edgeFade);
                float adjustedWhite = lerp(_WhitePoint, 0.6, edgeFade);
                lines = smoothstep(adjustedBlack, adjustedWhite, lines);

                // ── Noise (fades out earliest) ────────────────────────
                float noise = brownian_noise(scaledUV * _NoiseScale, 12) * noiseFade;

                // ── Normal generation with distance-faded strength ────
                float finalBump  = _BumpStrength * bumpFade;
                float3 tanNormal = ValueToNormal(lines - noise, finalBump);

                // ── TBN → world space ─────────────────────────────────
                float3 N = normalize(input.normalWS);
                float3 T = normalize(input.tangentWS);
                float3 B = normalize(input.bitangentWS);

                float3x3 TBN       = float3x3(T, B, N);
                float3 finalNormal = normalize(mul(tanNormal, TBN));

                // ── Shadow coord ──────────────────────────────────────
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #else
                    float4 shadowCoord = float4(0, 0, 0, 0);
                #endif

                // ── Lighting ──────────────────────────────────────────
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS              = input.positionWS;
                lightingInput.normalWS                = finalNormal;
                lightingInput.viewDirectionWS         = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lightingInput.shadowCoord             = shadowCoord;
                lightingInput.fogCoord                = 0;
                lightingInput.vertexLighting          = half3(0, 0, 0);
                lightingInput.bakedGI                 = SampleSH(finalNormal);
                lightingInput.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lightingInput.shadowMask              = half4(1, 1, 1, 1);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo     = float3(0.3, 0.0, 0.0);
                surface.metallic   = 0.0;
                surface.specular   = float3(0, 0, 0);
                surface.smoothness = 0.5;
                surface.occlusion  = 1.0;
                surface.emission   = float3(0, 0, 0);
                surface.alpha      = 1.0;

                return UniversalFragmentPBR(lightingInput, surface);
            }

            ENDHLSL
        }
    }
}