Shader"Custom/VoronoiDTE"
{
    Properties
    {
        _Scale ("Voronoi Scale", Range(1.0, 50.0)) = 15.0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry"
        }
        Pass
        {
Name"ForwardLit"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

            // Custom file — use full path from project root
#include "Assets/Materials/shaders/VoronoiDTE.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

            CBUFFER_START(UnityPerMaterial)
float _Scale;
CBUFFER_END

            Varyingsvert(
Attributes input)
            {
Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv         = input.
uv;
                return
output;
            }

float4 frag(Varyings input) : SV_Target
{
    float2 scaledUV = input.uv * _Scale;
    float lines = voronoi_DTE(scaledUV);
    return float4(lines, lines, lines, 1.0);
}
            ENDHLSL
        }
    }
}