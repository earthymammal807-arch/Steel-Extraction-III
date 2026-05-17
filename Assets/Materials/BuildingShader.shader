Shader "Custom/BuildingShader"
{
    // ==========================================
    // 1. MATERIAL PROPERTIES (Inspector UI)
    // ==========================================
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        _BumpScale("Procedural Normal Scale", Range(0.0, 10.0)) = 1.0
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _Occlusion("Ambient Occlusion", Range(0.0, 1.0)) = 1.0
    }

    SubShader
    {
        // ==========================================
        // 2. PIPELINE & RENDER TAGS
        // ==========================================
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry"
        }

        // ==========================================
        // PASS 1: FORWARD LIGHTING (Renders the object)
        // ==========================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;      
                float4 tangentOS    : TANGENT;     
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 positionWS   : TEXCOORD3;   
                float3 normalWS     : TEXCOORD1;   
                float4 tangentWS    : TANGENT;     
                float2 uv           : TEXCOORD0;
                float4 shadowCoord  : TEXCOORD5;   
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST; 
                half _BumpScale;
                half _Metallic;
                half _Smoothness;
                half _Occlusion;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                
                OUT.positionHCS = vertexInput.positionCS;
                OUT.positionWS = vertexInput.positionWS;
                OUT.normalWS = normalInput.normalWS;
                OUT.tangentWS = float4(normalInput.tangentWS, IN.tangentOS.w); 
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                
                OUT.shadowCoord = GetShadowCoord(vertexInput);
                
                return OUT;
            }

            float3 ValueToNormal(float heightValue, float bumpStrength)
            {
                float dhdx = ddx(heightValue); 
                float dhdy = ddy(heightValue); 

                float3 normal = float3(-dhdx * bumpStrength, -dhdy * bumpStrength, 1.0);
                return normalize(normal);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float3 surfaceColor = (texColor * _BaseColor).rgb;
                
                float grayscaleHeight = dot(surfaceColor, float3(0.2126, 0.7152, 0.0722));
                float3 normalTS = ValueToNormal(grayscaleHeight, _BumpScale);

                float3 bitangent = cross(IN.normalWS, IN.tangentWS.xyz) * IN.tangentWS.w;
                float3x3 tangentToWorld = float3x3(IN.tangentWS.xyz, bitangent, IN.normalWS);
                float3 worldNormal = normalize(mul(normalTS, tangentToWorld));

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = worldNormal;
                inputData.viewDirectionWS = SafeNormalize(GetCameraPositionWS() - IN.positionWS);
                inputData.shadowCoord = IN.shadowCoord;
                inputData.bakedGI = SampleSH(worldNormal); 

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = surfaceColor;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = float3(0.0, 0.0, 0.0); 
                surfaceData.smoothness = _Smoothness;
                surfaceData.occlusion = _Occlusion;
                surfaceData.alpha = texColor.a;

                half4 finalColor = UniversalFragmentPBR(inputData, surfaceData);
                
                return finalColor;
            }
            ENDHLSL
        }

        // ==========================================
        // PASS 2: SHADOW CASTER (Generates the shadows)
        // ==========================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ColorMask 0 
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Global URP parameters containing uniform shadow bias values
            // x = depth bias, y = normal bias
            float4 _ShadowBias; 
            float3 _LightDirection;

            struct ShadowAttributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct ShadowVaryings
            {
                float4 positionCS   : SV_POSITION;
            };

            ShadowVaryings ShadowVert(ShadowAttributes IN)
            {
                ShadowVaryings OUT;
                
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                
                // FIXED: Manual shadow bias calculation to prevent undeclared identifier errors
                // This pushes vertices slightly away from the light based on surface angles
                float invNdotL = 1.0 - saturate(dot(normalWS, _LightDirection));
                float scale = invNdotL * _ShadowBias.y;

                // Apply normal bias offset
                positionWS += normalWS * scale.xxx;
                
                // Transform to clip space
                float4 positionCS = TransformWorldToHClip(positionWS);
                
                // Apply depth bias offset
                #if UNITY_REVERSED_Z
                    positionCS.z -= _ShadowBias.x;
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z += _ShadowBias.x;
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                
                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 ShadowFrag(ShadowVaryings IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
