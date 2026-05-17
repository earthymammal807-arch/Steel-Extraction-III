#ifndef PERFORMANT_NOISE_INCLUDED
#define PERFORMANT_NOISE_INCLUDED

// Instantly generates a 3D normal vector from any single float value
float3 ValueToNormal(float heightValue, float bumpStrength)
{
    // Calculate how much the value changes between neighboring pixels
    float dhdx = ddx(heightValue); // Horizontal slope
    float dhdy = ddy(heightValue); // Vertical slope

    // Construct the tangent normal vector
    float3 normal = float3(-dhdx * bumpStrength, -dhdy * bumpStrength, 1.0);
    
    return normalize(normal);
}


#endif // PERFORMANT_NOISE_INCLUDED
