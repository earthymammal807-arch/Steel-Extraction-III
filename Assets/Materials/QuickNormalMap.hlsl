#ifndef QUICK_NORMAL_MAP_INCLUDED
#define QUICK_NORMAL_MAP_INCLUDED

// Requires URP Core for TransformTangentToWorld and UnpackNormalScale
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

/**
 * Samples a texture normal map and converts it instantly to ready-to-use World Space.
 */
float3 GetWorldSpaceNormalMap(
    float2 uv,
    float3 normalWS,
    float3 tangentWS,
    float3 bitangentWS,
    Texture2D normalTex,
    SamplerState normalSam,
    float normalScale
)
{
    // 1. Sample the packed texture using the texture's native macro style
    float4 packedNormal = normalTex.Sample(normalSam, uv);

    // 2. Unpack the texture data based on target platform rules (DXT5nm / BC5)
    float3 tangentNormal = UnpackNormalScale(packedNormal, normalScale);

    // 3. Convert the flat surface coordinates into world-space environment vectors
    half3x3 tangentToWorld = half3x3(tangentWS, bitangentWS, normalWS);
    float3 worldNormal = normalize(TransformTangentToWorld(tangentNormal, tangentToWorld));

    return worldNormal;
}

#endif // QUICK_NORMAL_MAP_INCLUDED
