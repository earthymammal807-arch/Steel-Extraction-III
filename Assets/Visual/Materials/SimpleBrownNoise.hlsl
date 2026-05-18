#ifndef BROWNIAN_NOISE_INCLUDED
#define BROWNIAN_NOISE_INCLUDED

// Simple 2D hash function to generate random values
float hash2d(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
}

// Basic 2D Value Noise (Bilinear interpolation between random points)
float value_noise(float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    
    // Smooth step interpolation curve
    float2 u = f * f * (3.0 - 2.0 * f);

    // Mix four grid corners together
    return lerp(lerp(hash2d(i + float2(0.0, 0.0)), hash2d(i + float2(1.0, 0.0)), u.x),
                lerp(hash2d(i + float2(0.0, 1.0)), hash2d(i + float2(1.0, 1.0)), u.x), u.y);
}

// The Brownian Motion (FBM) algorithm
float brownian_noise(float2 uv, int octaves)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    // Loop through layers, accumulating detail
    for (int i = 0; i < octaves; i++)
    {
        value += amplitude * value_noise(uv * frequency);
        frequency *= 2.0; // Double the frequency each step (smaller details)
        amplitude *= 0.5; // Halve the amplitude each step (fainter details)
    }
    
    return value;
}

#endif // BROWNIAN_NOISE_INCLUDED
