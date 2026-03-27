using System;
using System.Collections.Generic;

namespace Warship.World;

/// <summary>
/// Generates a procedural fantasy world with continents, oceans, mountain ranges,
/// rivers, and biomes. Minecraft-minimap aesthetic but with strategic depth.
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

    // Default world size — big enough for interesting geography
    public const int DefaultWidth = 200;
    public const int DefaultHeight = 120;

    /// <summary>
    /// Generate a full world: terrain grid + river paths.
    /// Same seed = identical world every time.
    /// </summary>
    public static (int[,] terrain, List<int[]> riverPaths) GenerateWorld(int width, int height, int seed)
    {
        var terrain = GenerateTerrain(width, height, seed);
        var rivers = GenerateRivers(terrain, width, height, seed);
        return (terrain, rivers);
    }

    /// <summary>
    /// Generate terrain map with continents, mountain spines, and biome zones.
    /// </summary>
    public static int[,] GenerateTerrain(int width, int height, int seed)
    {
        var map = new int[width, height];
        var elevation = new float[width, height];
        var moisture = new float[width, height];
        var temperature = new float[width, height];

        // ═══ Step 1: Base elevation with continent blobs ═══
        // Use multiple noise octaves + continent seeds for landmass shaping
        int continentSeed = seed;
        int numContinents = 4 + HashInt(seed, 0) % 3; // 4-6 continents

        // Pre-compute continent centers
        var continentCenters = new (float cx, float cy, float radius)[numContinents];
        for (int i = 0; i < numContinents; i++)
        {
            float cx = HashFloat(seed, i * 3 + 100) * (width * 0.7f) + width * 0.15f;
            float cy = HashFloat(seed, i * 3 + 101) * (height * 0.6f) + height * 0.2f;
            float radius = 18f + HashFloat(seed, i * 3 + 102) * 28f; // 18-46 tile radius
            continentCenters[i] = (cx, cy, radius);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Base noise elevation
                float elev = FBM(x * 0.025f, y * 0.025f, seed, 6);

                // Continent influence — raise land near continent centers
                float continentBoost = 0f;
                for (int c = 0; c < numContinents; c++)
                {
                    var (cx, cy, radius) = continentCenters[c];
                    float dx = x - cx;
                    float dy = y - cy;

                    // Warped distance for organic shapes
                    float warp = FBM(x * 0.06f + c * 50, y * 0.06f + c * 50, seed + 10 + c, 3) * 12f;
                    float dist = MathF.Sqrt(dx * dx + dy * dy) + warp;
                    float influence = Math.Clamp(1f - dist / radius, 0f, 1f);
                    influence = influence * influence; // Sharper falloff
                    continentBoost = Math.Max(continentBoost, influence * 0.55f);
                }

                elev = elev * 0.45f + continentBoost;

                // Edge falloff — ocean at map borders
                float edgeX = Math.Min(x, width - 1 - x) / (width * 0.08f);
                float edgeY = Math.Min(y, height - 1 - y) / (height * 0.08f);
                float edge = Math.Clamp(Math.Min(edgeX, edgeY), 0f, 1f);
                elev *= edge;

                elevation[x, y] = elev;

                // ═══ Step 2: Moisture (rain shadow from mountains, distance from coast) ═══
                float moist = FBM(x * 0.035f + 200f, y * 0.035f + 200f, seed + 1, 4);
                moisture[x, y] = moist;

                // ═══ Step 3: Temperature (latitude + elevation based) ═══
                float latFactor = 1f - Math.Abs(y - height / 2f) / (height / 2f); // 1 at equator, 0 at poles
                temperature[x, y] = latFactor;
            }
        }

        // ═══ Step 4: Mountain spine generation ═══
        // Create mountain ranges along tectonic-like lines
        int numRanges = 3 + HashInt(seed, 50) % 3;
        for (int r = 0; r < numRanges; r++)
        {
            float startX = HashFloat(seed, r * 5 + 200) * width;
            float startY = HashFloat(seed, r * 5 + 201) * height;
            float angle = HashFloat(seed, r * 5 + 202) * MathF.PI * 2f;
            int length = 30 + (int)(HashFloat(seed, r * 5 + 203) * 60f);
            float rangeWidth = 3f + HashFloat(seed, r * 5 + 204) * 5f;

            float px = startX, py = startY;
            for (int step = 0; step < length; step++)
            {
                // Wander the angle slightly each step
                angle += (FBM(px * 0.1f, py * 0.1f, seed + 100 + r, 2) - 0.5f) * 0.6f;
                px += MathF.Cos(angle) * 2f;
                py += MathF.Sin(angle) * 2f;

                int ix = (int)px, iy = (int)py;
                int rangeW = (int)rangeWidth;
                for (int dx = -rangeW; dx <= rangeW; dx++)
                {
                    for (int dy = -rangeW; dy <= rangeW; dy++)
                    {
                        int tx = ix + dx, ty = iy + dy;
                        if (tx < 0 || tx >= width || ty < 0 || ty >= height) continue;
                        float dist = MathF.Sqrt(dx * dx + dy * dy);
                        if (dist > rangeWidth) continue;
                        float boost = (1f - dist / rangeWidth) * 0.35f;
                        elevation[tx, ty] = Math.Min(elevation[tx, ty] + boost, 1f);
                    }
                }
            }
        }

        // ═══ Step 5: Classify terrain from elevation + moisture + temperature ═══
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float elev = elevation[x, y];
                float moist = moisture[x, y];
                float temp = temperature[x, y];

                Terrain t;
                if (elev < 0.18f)
                    t = Terrain.DeepWater;
                else if (elev < 0.25f)
                    t = Terrain.Water;
                else if (elev < 0.30f)
                    t = Terrain.Sand;
                else if (elev < 0.55f)
                {
                    // Biome selection: temp + moisture
                    if (temp < 0.2f)
                        t = moist > 0.45f ? Terrain.Snow : Terrain.Hills; // Cold
                    else if (moist > 0.55f)
                        t = Terrain.Forest;
                    else if (moist < 0.35f && temp > 0.6f)
                        t = Terrain.Sand; // Desert
                    else
                        t = Terrain.Grass;
                }
                else if (elev < 0.65f)
                    t = Terrain.Hills;
                else if (elev < 0.80f)
                    t = Terrain.Mountain;
                else
                    t = Terrain.Snow;

                map[x, y] = (int)t;
            }
        }

        // ═══ Step 6: Polish — add coastal sand strips ═══
        var copy = (int[,])map.Clone();
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (copy[x, y] == (int)Terrain.Grass || copy[x, y] == (int)Terrain.Forest)
                {
                    bool nearWater = copy[x - 1, y] <= 1 || copy[x + 1, y] <= 1 ||
                                     copy[x, y - 1] <= 1 || copy[x, y + 1] <= 1;
                    if (nearWater && HashFloat(seed, x * 1000 + y) > 0.4f)
                        map[x, y] = (int)Terrain.Sand;
                }
            }
        }

        return map;
    }

    /// <summary>
    /// Generate rivers that flow from mountains/hills downhill to the sea.
    /// Returns list of paths, each path is an array of [x, y] pairs.
    /// </summary>
    public static List<int[]> GenerateRivers(int[,] terrain, int width, int height, int seed)
    {
        var rivers = new List<int[]>();
        int numRivers = 6 + HashInt(seed, 300) % 6; // 6-11 rivers

        for (int r = 0; r < numRivers; r++)
        {
            // Find a mountain or hill tile to start from
            int attempts = 0;
            int sx = 0, sy = 0;
            bool found = false;
            while (attempts < 200)
            {
                sx = (int)(HashFloat(seed, r * 100 + attempts + 400) * width);
                sy = (int)(HashFloat(seed, r * 100 + attempts + 401) * height);
                sx = Math.Clamp(sx, 0, width - 1);
                sy = Math.Clamp(sy, 0, height - 1);
                int t = terrain[sx, sy];
                if (t == (int)Terrain.Mountain || t == (int)Terrain.Hills || t == (int)Terrain.Snow)
                {
                    found = true;
                    break;
                }
                attempts++;
            }
            if (!found) continue;

            // Flow downhill toward water
            var path = new List<int>();
            int cx = sx, cy = sy;
            var visited = new HashSet<long>();
            int maxSteps = 150;

            for (int step = 0; step < maxSteps; step++)
            {
                path.Add(cx);
                path.Add(cy);

                long key = (long)cx * height + cy;
                if (visited.Contains(key)) break;
                visited.Add(key);

                // Reached water — done
                if (terrain[cx, cy] <= (int)Terrain.Water && step > 2) break;

                // Find lowest neighbor (with some randomness for meandering)
                int bestX = cx, bestY = cy;
                int bestElev = TerrainElevationRank(terrain[cx, cy]);

                int[] dxs = { -1, 1, 0, 0, -1, 1, -1, 1 };
                int[] dys = { 0, 0, -1, 1, -1, -1, 1, 1 };
                for (int d = 0; d < 8; d++)
                {
                    int nx = cx + dxs[d], ny = cy + dys[d];
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                    int nElev = TerrainElevationRank(terrain[nx, ny]);
                    // Add slight randomness for meandering
                    float jitter = (HashFloat(seed, step * 100 + d + r * 1000) - 0.5f) * 2f;
                    if (nElev + jitter < bestElev)
                    {
                        bestElev = nElev;
                        bestX = nx;
                        bestY = ny;
                    }
                }

                if (bestX == cx && bestY == cy) break; // Stuck in local minimum
                cx = bestX;
                cy = bestY;
            }

            if (path.Count >= 8) // At least 4 points
                rivers.Add(path.ToArray());
        }

        return rivers;
    }

    /// <summary>
    /// Find valid locations for cities. Returns list of (x, y, quality) tuples.
    /// Quality is higher for river-adjacent, coastal, or fertile land.
    /// </summary>
    public static List<(int x, int y, float quality)> FindCityLocations(
        int[,] terrain, List<int[]> rivers, int width, int height, int seed, int count)
    {
        // Build a desirability map
        var desirability = new float[width, height];

        // River adjacency bonus
        var riverTiles = new HashSet<long>();
        foreach (var river in rivers)
        {
            for (int i = 0; i < river.Length - 1; i += 2)
            {
                int rx = river[i], ry = river[i + 1];
                riverTiles.Add((long)rx * height + ry);
            }
        }

        for (int x = 2; x < width - 2; x++)
        {
            for (int y = 2; y < height - 2; y++)
            {
                int t = terrain[x, y];
                if (t <= (int)Terrain.Water || t >= (int)Terrain.Mountain)
                    continue; // Can't build on water or mountains

                float score = 0f;

                // Land type bonus
                if (t == (int)Terrain.Grass) score += 3f;
                else if (t == (int)Terrain.Forest) score += 1.5f;
                else if (t == (int)Terrain.Hills) score += 1f;
                else if (t == (int)Terrain.Sand) score += 0.5f;

                // River adjacency — huge bonus
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        if (riverTiles.Contains((long)(x + dx) * height + (y + dy)))
                            score += 2f;
                    }
                }

                // Coastal bonus (near water but on land)
                bool coastal = false;
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx >= 0 && nx < width && ny >= 0 && ny < height &&
                            terrain[nx, ny] <= (int)Terrain.Water)
                        {
                            coastal = true;
                            break;
                        }
                    }
                    if (coastal) break;
                }
                if (coastal) score += 2.5f;

                // Slight randomness
                score += HashFloat(seed, x * 1000 + y + 5000) * 1.5f;

                desirability[x, y] = score;
            }
        }

        // Pick top locations with minimum spacing
        var locations = new List<(int x, int y, float quality)>();
        int minSpacing = 12; // Minimum tiles between cities

        for (int i = 0; i < count * 3 && locations.Count < count; i++)
        {
            // Find the highest unoccupied desirability
            float bestScore = 0f;
            int bestX = -1, bestY = -1;

            for (int x = 2; x < width - 2; x++)
            {
                for (int y = 2; y < height - 2; y++)
                {
                    if (desirability[x, y] > bestScore)
                    {
                        bestScore = desirability[x, y];
                        bestX = x;
                        bestY = y;
                    }
                }
            }

            if (bestX < 0) break;

            locations.Add((bestX, bestY, bestScore));

            // Zero out area around this city so next pick is spaced out
            for (int dx = -minSpacing; dx <= minSpacing; dx++)
            {
                for (int dy = -minSpacing; dy <= minSpacing; dy++)
                {
                    int nx = bestX + dx, ny = bestY + dy;
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                        desirability[nx, ny] = 0f;
                }
            }
        }

        return locations;
    }

    // ═══ Elevation ranking for river flow ═══
    private static int TerrainElevationRank(int terrain) => terrain switch
    {
        0 => 0,  // DeepWater
        1 => 1,  // Water
        2 => 2,  // Sand
        3 => 3,  // Grass
        4 => 4,  // Forest
        5 => 5,  // Hills
        6 => 7,  // Mountain
        7 => 8,  // Snow
        _ => 3
    };

    // ═══ Noise Functions ═══

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

        return total / maxValue;
    }

    private static float ValueNoise(float x, float y, int seed)
    {
        int ix = (int)MathF.Floor(x);
        int iy = (int)MathF.Floor(y);
        float fx = x - ix;
        float fy = y - iy;

        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);

        float v00 = HashGridFloat(ix, iy, seed);
        float v10 = HashGridFloat(ix + 1, iy, seed);
        float v01 = HashGridFloat(ix, iy + 1, seed);
        float v11 = HashGridFloat(ix + 1, iy + 1, seed);

        float top = v00 + fx * (v10 - v00);
        float bottom = v01 + fx * (v11 - v01);
        return top + fy * (bottom - top);
    }

    private static float HashGridFloat(int x, int y, int seed)
    {
        int h = seed;
        h ^= x * 374761393;
        h ^= y * 668265263;
        h = (h ^ (h >> 13)) * 1274126177;
        h = h ^ (h >> 16);
        return (h & 0x7FFFFFFF) / (float)0x7FFFFFFF;
    }

    // Deterministic hash helpers
    private static int HashInt(int seed, int index)
    {
        int h = seed ^ (index * 374761393);
        h = (h ^ (h >> 13)) * 1274126177;
        return Math.Abs(h ^ (h >> 16));
    }

    private static float HashFloat(int seed, int index)
    {
        return (HashInt(seed, index) & 0x7FFFFFFF) / (float)0x7FFFFFFF;
    }
}
