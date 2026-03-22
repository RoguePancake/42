using System;
using System.Collections.Generic;
using Godot;
using Warship.Data;

namespace Warship.World;

/// <summary>
/// Oversees creating the entire world: terrain (via TerrainGenerator)
/// and then populating nations, distributing land via BFS territory expansion.
/// </summary>
public static class WorldGenerator
{
    private static readonly string[] NationNames = { 
        "Valeria", "Gondor", "Zendia", "Kraal", "Aethel", "Durotar" 
    };

    private static readonly Color[] NationColors = {
        new Color(0.8f, 0.2f, 0.2f), // Red
        new Color(0.2f, 0.4f, 0.8f), // Blue
        new Color(0.8f, 0.8f, 0.2f), // Yellow
        new Color(0.6f, 0.2f, 0.8f), // Purple
        new Color(0.2f, 0.8f, 0.4f), // Green
        new Color(0.8f, 0.5f, 0.1f)  // Orange
    };
    
    // Add possible minor city names
    private static readonly string[] CityNames = {
        "Oakhaven", "Riverbend", "Ironforge", "Sunpeak", 
        "Frostford", "Eldoria", "Grimwall", "Valewood", 
        "Amberfall", "Windhelm"
    };

    public static WorldData CreateWorld(int width, int height, int seed)
    {
        var world = new WorldData
        {
            Seed = seed,
            MapWidth = width,
            MapHeight = height,
            TerrainMap = TerrainGenerator.Generate(width, height, seed),
            OwnershipMap = new int[width, height]
        };

        // Initialize ownership to -1 (unclaimed)
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                world.OwnershipMap[x, y] = -1;

        // Step 1: Find valid land tiles for capitals
        var rng = new Random(seed);
        var validLand = new List<Vector2I>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (TerrainRules.IsPassable(world.TerrainMap[x, y]))
                {
                    validLand.Add(new Vector2I(x, y));
                }
            }
        }

        // Keep shuffling and picking capitals so they are far apart
        var capitals = new List<Vector2I>();
        for (int i = 0; i < 6 && validLand.Count > 0; i++)
        {
            Vector2I bestPick = validLand[0];
            float maxDist = -1f;

            // Pick a set of random tiles, choose the one furthest from existing capitals
            for (int k = 0; k < 20; k++)
            {
                var cand = validLand[rng.Next(validLand.Count)];
                float distToClosest = float.MaxValue;
                foreach (var cap in capitals)
                {
                    float d = new Vector2(cand.X - cap.X, cand.Y - cap.Y).Length();
                    if (d < distToClosest) distToClosest = d;
                }
                
                if (distToClosest > maxDist)
                {
                    maxDist = distToClosest;
                    bestPick = cand;
                }
            }
            
            capitals.Add(bestPick);
            validLand.Remove(bestPick);

            // Create nation
            world.Nations.Add(new NationData
            {
                Id = $"N_{i}",
                Name = NationNames[i],
                Archetype = (NationArchetype)(i % 6),
                NationColor = NationColors[i],
                CapitalX = bestPick.X,
                CapitalY = bestPick.Y,
                ProvinceCount = 1
            });

            world.OwnershipMap[bestPick.X, bestPick.Y] = i; // Claim capital
        }

        // Step 2: Flood fill border expansion (Voronoi-ish BFS)
        var queue = new Queue<(Vector2I tile, int nationIndex)>();
        foreach (var nation in world.Nations)
        {
            int index = int.Parse(nation.Id.Split('_')[1]);
            queue.Enqueue((new Vector2I(nation.CapitalX, nation.CapitalY), index));
        }

        var dx = new[] { 0, 1, 0, -1 };
        var dy = new[] { -1, 0, 1, 0 };

        while (queue.Count > 0)
        {
            var (tile, natIdx) = queue.Dequeue();

            // Try spreading to 4 neighbors
            for (int d = 0; d < 4; d++)
            {
                int nx = tile.X + dx[d];
                int ny = tile.Y + dy[d];

                // Bounds check
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;

                // Stop at water or impassable mountains
                if (!TerrainRules.IsPassable(world.TerrainMap[nx, ny])) continue;

                // Stop if already claimed
                if (world.OwnershipMap[nx, ny] != -1) continue;

                // Claim it
                world.OwnershipMap[nx, ny] = natIdx;
                world.Nations[natIdx].ProvinceCount++;

                // Queue next (with 10% chance to skip this tile from growing further, for organic jagged borders)
                if (rng.NextDouble() > 0.1)
                {
                    queue.Enqueue((new Vector2I(nx, ny), natIdx));
                }
            }
        }

        // Step 3: Instantiate Cities (Capitals & Minor cities)
        int cityIndex = 0;
        foreach (var nation in world.Nations)
        {
            // Capital
            world.Cities.Add(new CityData
            {
                Id = $"C_{cityIndex++}",
                NationId = nation.Id,
                Name = nation.Name + " Prime",
                TileX = nation.CapitalX,
                TileY = nation.CapitalY,
                IsCapital = true,
                Size = 3
            });

            // If the nation is big enough, maybe spawn a minor city
            if (nation.ProvinceCount > 50)
            {
                // Find a random tile owned by this nation that isn't the capital
                for (int attempt = 0; attempt < 100; attempt++)
                {
                    int rx = rng.Next(width);
                    int ry = rng.Next(height);
                    if (world.OwnershipMap[rx, ry] == int.Parse(nation.Id.Split('_')[1]))
                    {
                        // Needs to be far from capital
                        if (new Vector2(rx - nation.CapitalX, ry - nation.CapitalY).Length() > 8)
                        {
                            world.Cities.Add(new CityData
                            {
                                Id = $"C_{cityIndex++}",
                                NationId = nation.Id,
                                Name = CityNames[rng.Next(CityNames.Length)],
                                TileX = rx,
                                TileY = ry,
                                IsCapital = false,
                                Size = 1 + rng.Next(2) // 1 or 2
                            });
                            break;
                        }
                    }
                }
            }
        }

        // Step 4: River Generation
        // We simulate rain falling on mountains and flowing downhill into the sea
        for (int i = 0; i < 15; i++)
        {
            int rx = rng.Next(width);
            int ry = rng.Next(height);
            
            // Only start rivers in mountains or hills
            int t = world.TerrainMap[rx, ry];
            if (t != (int)TerrainType.Mountain && t != (int)TerrainType.Hills)
                continue;

            var path = new List<Vector2>();
            int cx = rx;
            int cy = ry;
            bool hitOcean = false;

            for (int step = 0; step < 100; step++) // Max river length
            {
                // Add jitter to center point so rivers meander
                float jitterX = (float)(rng.NextDouble() - 0.5) * 0.4f;
                float jitterY = (float)(rng.NextDouble() - 0.5) * 0.4f;
                path.Add(new Vector2(cx + 0.5f + jitterX, cy + 0.5f + jitterY));

                int currentTerrain = world.TerrainMap[cx, cy];
                if (!TerrainRules.IsLand(currentTerrain))
                {
                    hitOcean = true;
                    break;
                }

                // Look for lowest neighbor
                int bestX = cx;
                int bestY = cy;
                int lowestTerrain = currentTerrain;

                // Check 8 neighbors
                int[] ndx = { 0, 1, 1, 1, 0, -1, -1, -1 };
                int[] ndy = { -1, -1, 0, 1, 1, 1, 0, -1 };
                
                // Shuffle neighbor checks so they pick randomly if flat
                int startIndex = rng.Next(8);
                
                for (int d = 0; d < 8; d++)
                {
                    int nIdx = (startIndex + d) % 8;
                    int nx = cx + ndx[nIdx];
                    int ny = cy + ndy[nIdx];
                    
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        int nt = world.TerrainMap[nx, ny];
                        // Don't flow uphill
                        if (nt <= lowestTerrain)
                        {
                            // Avoid going back to a tile already in the path
                            bool alreadyVisited = false;
                            foreach (var p in path)
                            {
                                if ((int)p.X == nx && (int)p.Y == ny) { alreadyVisited = true; break; }
                            }
                            
                            if (!alreadyVisited)
                            {
                                lowestTerrain = nt;
                                bestX = nx;
                                bestY = ny;
                            }
                        }
                    }
                }

                // If nowhere lower to go
                if (bestX == cx && bestY == cy)
                    break;

                cx = bestX;
                cy = bestY;
            }

            // Only keep rivers that reach water and have some length
            if (hitOcean && path.Count > 4)
            {
                world.RiverPaths.Add(path.ToArray());
            }
        }

        return world;
    }
}
