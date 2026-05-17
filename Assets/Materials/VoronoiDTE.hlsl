#ifndef VORONOI_INCLUDED
#define VORONOI_INCLUDED

// 2D Hash function
float2 hash2(float2 p)
{
    p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
    return frac(sin(p) * 43758.5453);
}

// Voronoi Distance-to-Edge function
float voronoi_DTE(float2 uv)
{
    float2 grid_id = floor(uv); // Which integer cell are we in?
    float2 grid_uv = frac(uv); // Where inside that cell are we? (0.0 to 1.0)
    
    float2 closest_point = float2(0.0, 0.0);
    float min_dist = 1.0;

    // First pass: Find the closest center point
    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 neighbor_offset = float2((float) x, (float) y);
            
            // Generate a random point inside this specific neighbor cell
            float2 point_pos = hash2(grid_id + neighbor_offset);
            
            // Vector from current pixel to the generated point
            float2 r = neighbor_offset + point_pos - grid_uv;
            
            // Squared distance (faster than sqrt)
            float d = dot(r, r);

            if (d < min_dist)
            {
                min_dist = d;
                closest_point = r; // Save the winner!
            }
        }
    }

    // Second pass: Find distance to the border/edge
    float edge_dist = 8.0;
    
    for (int y_edge = -1; y_edge <= 1; y_edge++)
    {
        for (int x_edge = -1; x_edge <= 1; x_edge++)
        {
            float2 neighbor = float2((float) x_edge, (float) y_edge);
            float2 point_pos = hash2(grid_id + neighbor);
            float2 r = neighbor + point_pos - grid_uv;

            // Skip the closest point itself
            if (dot(closest_point - r, closest_point - r) > 0.00001)
            {
                // MATH: The distance to the line between two points
                float dist_to_midline = dot(0.5 * (closest_point + r), normalize(r - closest_point));
                edge_dist = min(edge_dist, dist_to_midline);
            }
        }
    }
    
    return edge_dist;
}

#endif
