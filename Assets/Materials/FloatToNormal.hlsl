#ifndef PERFORMANT_NOISE_INCLUDED
#define PERFORMANT_NOISE_INCLUDED

// 1. KEEP THIS FOR YOUR CODE-BASED SHADERS
float3 ValueToNormal(float heightValue, float bumpStrength)
{
    float dhdx = ddx(heightValue);
    float dhdy = ddy(heightValue);
    float3 normal = float3(-dhdx * bumpStrength, -dhdy * bumpStrength, 1.0);
    return normalize(normal);
}

// 2. ADD THIS FOR YOUR SHADER GRAPHS
void ValueToNormal_float(float heightValue, float bumpStrength, out float3 OutNormal)
{
    // Simply routes the data to your original function safely
    OutNormal = ValueToNormal(heightValue, bumpStrength);
}

#endif // PERFORMANT_NOISE_INCLUDED
