using System;
using Warship.Core;
using Warship.Data;

namespace Warship.World;

/// <summary>
/// Procedural Minecraft-style terrain generator.
/// Produces a flat int[] grid of terrain types.
/// Uses Perlin-like noise with continent blobs, mountain spines, and biome zones.
/// Pure C# — zero Godot API.
/// </summary>
public static class TerrainGenerator
{
    public const int DefaultWidth = 512;
    public const int DefaultHeight = 512;
    public const int TileSize = 16; // pixels per tile for rendering

    /// <summary>
    /// Generate a complete terrain map. Same seed = identical map.
    /// Returns flat array indexed as [x + y * width].
    /// </summary>
    public static int[] Generate(int width, int height, int seed)
    {
        var map = new int[width * height];
        var elevation = new float[width * height];
        var moisture = new float[width * height];

        // ── Step 1: Continent blobs ──
        int numContinents = 3 + SimRng.HashInt(seed, 0) % 3; // 3-5
        var continents = new (float cx, float cy, float radius)[numContinents];
        for (int i = 0; i < numContinents; i++)
        {
            continents[i] = (
                SimRng.Hash(seed, i * 3 + 100) * width * 0.7f + width * 0.15f,
                SimRng.Hash(seed, i * 3 + 101) * height * 0.7f + height * 0.15f,
                40f + SimRng.Hash(seed, i * 3 + 102) * 80f
            );
        }

        // ── Step 2: Base elevation + moisture ──
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = x + y * width;
                float freq = 4.0f / width;

                // Base noise
                float elev = Fbm(x * freq, y * freq, seed, 6);

                // Continent influence
                float boost = 0f;
                for (int c = 0; c < numContinents; c++)
                {
                    float dx = x - continents[c].cx;
                    float dy = y - continents[c].cy;
                    float warp = Fbm(x * 0.02f + c * 50, y * 0.02f + c * 50, seed + 10 + c, 3) * 25f;
                    float dist = MathF.Sqrt(dx * dx + dy * dy) + warp;
                    float inf = Math.Clamp(1f - dist / continents[c].radius, 0f, 1f);
                    boost = Math.Max(boost, inf * inf * 0.55f);
                }

                elev = elev * 0.45f + boost;

                // Edge falloff (ocean at borders)
                float edgeX = Math.Min(x, width - 1 - x) / (width * 0.08f);
                float edgeY = Math.Min(y, height - 1 - y) / (height * 0.08f);
                elev *= Math.Clamp(Math.Min(edgeX, edgeY), 0f, 1f);

                elevation[idx] = elev;

                // Moisture
                float mFreq = 6.0f / width;
                moisture[idx] = Fbm(x * mFreq + 200, y * mFreq + 200, seed + 1, 4);
            }
        }

        // ── Step 3: Mountain spines ──
        int numRanges = 3 + SimRng.HashInt(seed, 50) % 3;
        for (int r = 0; r < numRanges; r++)
        {
            float px = SimRng.Hash(seed, r * 5 + 200) * width;
            float py = SimRng.Hash(seed, r * 5 + 201) * height;
            float angle = SimRng.Hash(seed, r * 5 + 202) * MathF.PI * 2f;
            int length = 40 + (int)(SimRng.Hash(seed, r * 5 + 203) * 80f);
            float rangeW = 2f + SimRng.Hash(seed, r * 5 + 204) * 4f;

            for (int step = 0; step < length; step++)
            {
                angle += (Fbm(px * 0.03f, py * 0.03f, seed + 100 + r, 2) - 0.5f) * 0.5f;
                px += MathF.Cos(angle) * 2f;
                py += MathF.Sin(angle) * 2f;

                int ix = (int)px, iy = (int)py;
                int rw = (int)rangeW;
                for (int dx = -rw; dx <= rw; dx++)
                {
                    for (int dy = -rw; dy <= rw; dy++)
                    {
                        int tx = ix + dx, ty = iy + dy;
                        if (tx < 0 || tx >= width || ty < 0 || ty >= height) continue;
                        float dist = MathF.Sqrt(dx * dx + dy * dy);
                        if (dist > rangeW) continue;
                        int tidx = tx + ty * width;
                        elevation[tidx] = Math.Min(elevation[tidx] + (1f - dist / rangeW) * 0.35f, 1f);
                    }
                }
            }
        }

        // ── Step 4: Classify biomes ──
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = x + y * width;
                float elev = elevation[idx];
                float moist = moisture[idx];
                float temp = 1f - Math.Abs(y - height / 2f) / (height / 2f); // latitude

                Terrain t;
                if (elev < 0.18f)
                    t = Terrain.DeepWater;
                else if (elev < 0.25f)
                    t = Terrain.Water;
                else if (elev < 0.30f)
                    t = Terrain.Sand;
                else if (elev < 0.55f)
                {
                    if (temp < 0.2f)
                        t = moist > 0.45f ? Terrain.Snow : Terrain.Hills;
                    else if (moist > 0.55f)
                        t = Terrain.Forest;
                    else if (moist < 0.35f && temp > 0.6f)
                        t = Terrain.Sand;
                    else
                        t = Terrain.Grass;
                }
                else if (elev < 0.65f)
                    t = Terrain.Hills;
                else if (elev < 0.80f)
                    t = Terrain.Mountain;
                else
                    t = Terrain.Snow;

                map[idx] = (int)t;
            }
        }

        // ── Step 5: Coastal sand strips ──
        var copy = (int[])map.Clone();
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                int idx = x + y * width;
                int t = copy[idx];
                if (t == (int)Terrain.Grass || t == (int)Terrain.Forest)
                {
                    bool nearWater = copy[(x - 1) + y * width] <= 1
                                  || copy[(x + 1) + y * width] <= 1
                                  || copy[x + (y - 1) * width] <= 1
                                  || copy[x + (y + 1) * width] <= 1;
                    if (nearWater && SimRng.Hash(seed, x * 1000 + y) > 0.4f)
                        map[idx] = (int)Terrain.Sand;
                }
            }
        }

        return map;
    }

    /// <summary>Find a valid starting position for the player (grass, near center).</summary>
    public static (int x, int y) FindStartPosition(int[] map, int width, int height, int seed)
    {
        int cx = width / 2, cy = height / 2;

        // Spiral outward from center until we find grass
        for (int radius = 0; radius < Math.Max(width, height) / 2; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Math.Abs(dx) != radius && Math.Abs(dy) != radius) continue;
                    int x = cx + dx, y = cy + dy;
                    if (x < 0 || x >= width || y < 0 || y >= height) continue;
                    if (map[x + y * width] == (int)Terrain.Grass)
                        return (x, y);
                }
            }
        }
        return (cx, cy); // fallback
    }

    // ── Noise functions (pure C#) ──

    private static float Fbm(float x, float y, int seed, int octaves)
    {
        float total = 0f, amp = 1f, freq = 1f, max = 0f;
        for (int i = 0; i < octaves; i++)
        {
            total += ValueNoise(x * freq, y * freq, seed + i) * amp;
            max += amp;
            amp *= 0.5f;
            freq *= 2f;
        }
        return total / max;
    }

    private static float ValueNoise(float x, float y, int seed)
    {
        int ix = (int)MathF.Floor(x), iy = (int)MathF.Floor(y);
        float fx = x - ix, fy = y - iy;
        fx = fx * fx * (3f - 2f * fx); // smoothstep
        fy = fy * fy * (3f - 2f * fy);

        float v00 = GridHash(ix, iy, seed);
        float v10 = GridHash(ix + 1, iy, seed);
        float v01 = GridHash(ix, iy + 1, seed);
        float v11 = GridHash(ix + 1, iy + 1, seed);

        float top = v00 + fx * (v10 - v00);
        float bot = v01 + fx * (v11 - v01);
        return top + fy * (bot - top);
    }

    private static float GridHash(int x, int y, int seed)
    {
        int h = seed;
        h ^= x * 374761393;
        h ^= y * 668265263;
        h = (h ^ (h >> 13)) * 1274126177;
        h = h ^ (h >> 16);
        return (h & 0x7FFFFFFF) / (float)0x7FFFFFFF;
    }
}
