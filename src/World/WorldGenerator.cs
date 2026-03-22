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

        return world;
    }
}
