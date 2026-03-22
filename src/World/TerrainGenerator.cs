using System;

namespace Warship.World;

/// <summary>
/// Generates procedural terrain using layered value noise.
/// Pure C# — no Godot dependency.
/// </summary>
public static class TerrainGenerator
{
    public enum Terrain
    {
        DeepWater = 0,
        Water = 1,
        Sand = 2,
        Grass = 3,
        Forest = 4,
        Hills = 5,
        Mountain = 6,
        Snow = 7
    }

    /// <summary>
    /// Generate a terrain map. Same seed = same map every time.
    /// </summary>
    public static int[,] Generate(int width, int height, int seed)
    {
        var map = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float elevation = FBM(x * 0.04f, y * 0.04f, seed, 5);
                float moisture = FBM(x * 0.05f + 100f, y * 0.05f + 100f, seed + 1, 4);

                // Edge falloff — ocean wraps around the map
                float edgeX = Math.Min(x, width - 1 - x) / (width * 0.15f);
                float edgeY = Math.Min(y, height - 1 - y) / (height * 0.15f);
                float edgeDist = Math.Min(edgeX, edgeY);
                elevation *= Math.Clamp(edgeDist, 0f, 1f);

                // Height → terrain type
                Terrain terrain;
                if (elevation < 0.20f)
                    terrain = Terrain.DeepWater;
                else if (elevation < 0.28f)
                    terrain = Terrain.Water;
                else if (elevation < 0.33f)
                    terrain = Terrain.Sand;
                else if (elevation < 0.50f)
                    terrain = moisture > 0.55f ? Terrain.Forest : Terrain.Grass;
                else if (elevation < 0.62f)
                    terrain = Terrain.Hills;
                else if (elevation < 0.78f)
                    terrain = Terrain.Mountain;
                else
                    terrain = Terrain.Snow;

                map[x, y] = (int)terrain;
            }
        }

        return map;
    }

    // ─── Noise Functions ─────────────────────────────────

    /// <summary>Fractal Brownian Motion — layered noise for natural terrain.</summary>
    private static float FBM(float x, float y, int seed, int octaves)
    {
        float total = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float maxValue = 0f;

        for (int i = 0; i < octaves; i++)
        {
            total += ValueNoise(x * frequency, y * frequency, seed + i) * amplitude;
            maxValue += amplitude;
            amplitude *= 0.5f;
            frequency *= 2f;
        }

        return total / maxValue; // Normalize to 0-1
    }

    /// <summary>Smooth value noise with bilinear interpolation.</summary>
    private static float ValueNoise(float x, float y, int seed)
    {
        int ix = (int)MathF.Floor(x);
        int iy = (int)MathF.Floor(y);
        float fx = x - ix;
        float fy = y - iy;

        // Smoothstep for smoother interpolation
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);

        float v00 = Hash(ix, iy, seed);
        float v10 = Hash(ix + 1, iy, seed);
        float v01 = Hash(ix, iy + 1, seed);
        float v11 = Hash(ix + 1, iy + 1, seed);

        float top = v00 + fx * (v10 - v00);
        float bottom = v01 + fx * (v11 - v01);
        return top + fy * (bottom - top);
    }

    /// <summary>Hash function for pseudo-random value per grid point (0-1).</summary>
    private static float Hash(int x, int y, int seed)
    {
        int h = seed;
        h ^= x * 374761393;
        h ^= y * 668265263;
        h = (h ^ (h >> 13)) * 1274126177;
        h = h ^ (h >> 16);
        return (h & 0x7FFFFFFF) / (float)0x7FFFFFFF;
    }
}
