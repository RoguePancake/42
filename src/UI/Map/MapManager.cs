using Godot;
using Warship.World;
using Warship.Data;

namespace Warship.UI.Map;

/// <summary>
/// Renders the terrain map as colored rectangles. 
/// Simple and fast — no external tileset images needed.
/// </summary>
public partial class MapManager : Node2D
{
    public const int TileSize = 32;
    public const int MapWidth = 80;
    public const int MapHeight = 50;
    public const int Seed = 42;

    private WorldData? _world;

    // SNES-style terrain colors
    private static readonly Color[] TerrainColors = new Color[]
    {
        new Color(0.11f, 0.23f, 0.43f),  // 0: Deep Water — dark blue
        new Color(0.16f, 0.39f, 0.66f),  // 1: Water — medium blue
        new Color(0.83f, 0.72f, 0.44f),  // 2: Sand — warm tan
        new Color(0.28f, 0.66f, 0.28f),  // 3: Grass — rich green
        new Color(0.16f, 0.41f, 0.16f),  // 4: Forest — dark green
        new Color(0.48f, 0.55f, 0.35f),  // 5: Hills — green-brown
        new Color(0.54f, 0.46f, 0.38f),  // 6: Mountain — gray-brown
        new Color(0.88f, 0.91f, 0.94f),  // 7: Snow — white-blue
    };

    // Secondary shading colors for tile detail
    private static readonly Color[] TerrainAccents = new Color[]
    {
        new Color(0.08f, 0.18f, 0.35f),  // Deep Water accent
        new Color(0.20f, 0.45f, 0.72f),  // Water highlight
        new Color(0.75f, 0.65f, 0.38f),  // Sand shadow
        new Color(0.35f, 0.72f, 0.30f),  // Grass flowers
        new Color(0.12f, 0.30f, 0.10f),  // Forest shadow
        new Color(0.55f, 0.50f, 0.30f),  // Hills highlight
        new Color(0.70f, 0.65f, 0.60f),  // Mountain snow cap
        new Color(0.95f, 0.97f, 1.00f),  // Snow sparkle
    };

    public override void _Ready()
    {
        GD.Print("[MapManager] Generating world...");
        _world = WorldGenerator.CreateWorld(MapWidth, MapHeight, Seed);
        GD.Print($"[MapManager] World generated: {MapWidth}×{MapHeight}, seed={Seed}");
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_world == null || _world.TerrainMap == null || _world.OwnershipMap == null) return;

        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                int t = _world.TerrainMap[x, y];
                var pos = new Vector2(x * TileSize, y * TileSize);
                var size = new Vector2(TileSize, TileSize);

                // Base tile color
                DrawRect(new Rect2(pos, size), TerrainColors[t]);

                // Add pixel detail based on terrain type
                DrawTileDetail(x, y, t, pos);
            }
        }
        
        // Draw borders ON TOP of terrain details
        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                int owner = _world.OwnershipMap[x, y];
                if (owner == -1) continue;
                
                var natColor = _world.Nations[owner].NationColor;
                var pos = new Vector2(x * TileSize, y * TileSize);
                
                // Draw a simple 20% transparent overlay on the tile
                DrawRect(new Rect2(pos, new Vector2(TileSize, TileSize)), new Color(natColor, 0.2f));
                
                // Draw opaque border lines if neighbor is different or unclaimed
                // Top
                if (y == 0 || _world.OwnershipMap[x, y - 1] != owner)
                    DrawLine(pos, pos + new Vector2(TileSize, 0), natColor, 2);
                // Bottom
                if (y == MapHeight - 1 || _world.OwnershipMap[x, y + 1] != owner)
                    DrawLine(pos + new Vector2(0, TileSize), pos + new Vector2(TileSize, TileSize), natColor, 2);
                // Left
                if (x == 0 || _world.OwnershipMap[x - 1, y] != owner)
                    DrawLine(pos, pos + new Vector2(0, TileSize), natColor, 2);
                // Right
                if (x == MapWidth - 1 || _world.OwnershipMap[x + 1, y] != owner)
                    DrawLine(pos + new Vector2(TileSize, 0), pos + new Vector2(TileSize, TileSize), natColor, 2);
            }
        }
    }

    /// <summary>
    /// Adds SNES-style pixel detail to each tile — dots, lines, shapes.
    /// Uses a deterministic hash so details are consistent.
    /// </summary>
    private void DrawTileDetail(int tx, int ty, int terrain, Vector2 pos)
    {
        int hash = (tx * 7919 + ty * 104729) & 0xFFFF;
        Color accent = TerrainAccents[terrain];

        switch (terrain)
        {
            case 0: // Deep Water — wave lines
                if ((hash & 3) == 0)
                {
                    float waveY = pos.Y + 10 + (hash % 12);
                    DrawLine(new Vector2(pos.X + 4, waveY), new Vector2(pos.X + 28, waveY), accent, 1);
                }
                break;

            case 1: // Water — foam dots
                if ((hash & 3) < 2)
                {
                    DrawCircle(new Vector2(pos.X + 8 + (hash % 16), pos.Y + 8 + ((hash >> 4) % 16)), 1.5f, accent);
                }
                break;

            case 2: // Sand — pebble dots
                for (int i = 0; i < 3; i++)
                {
                    int px = ((hash >> (i * 3)) % 24) + 4;
                    int py = ((hash >> (i * 3 + 1)) % 24) + 4;
                    DrawCircle(new Vector2(pos.X + px, pos.Y + py), 1, accent);
                }
                break;

            case 3: // Grass — flower dots
                for (int i = 0; i < 4; i++)
                {
                    int px = ((hash >> (i * 4)) % 26) + 3;
                    int py = ((hash >> (i * 4 + 2)) % 26) + 3;
                    Color flowerColor = (i % 2 == 0) ? accent : new Color(0.9f, 0.85f, 0.2f);
                    DrawCircle(new Vector2(pos.X + px, pos.Y + py), 1, flowerColor);
                }
                break;

            case 4: // Forest — tree shapes (trunk + canopy circle)
            {
                int trees = 1 + (hash & 1);
                for (int i = 0; i < trees; i++)
                {
                    float cx = pos.X + 8 + (i * 14) + (hash % 6);
                    float cy = pos.Y + 12 + ((hash >> 2) % 8);
                    // Trunk
                    DrawLine(new Vector2(cx, cy + 4), new Vector2(cx, cy + 10), new Color(0.35f, 0.22f, 0.10f), 2);
                    // Canopy
                    DrawCircle(new Vector2(cx, cy), 5, accent);
                    DrawCircle(new Vector2(cx, cy - 2), 3, TerrainColors[4]);
                }
                break;
            }

            case 5: // Hills — curved hill line
            {
                float cx = pos.X + 16;
                float cy = pos.Y + 18;
                DrawArc(new Vector2(cx, cy), 10, Mathf.Pi * 0.15f, Mathf.Pi * 0.85f, 8, accent, 2);
                break;
            }

            case 6: // Mountain — triangle peak with snow cap
            {
                float cx = pos.X + 16;
                float baseY = pos.Y + 28;
                var peak = new Vector2(cx, pos.Y + 4);
                var left = new Vector2(cx - 12, baseY);
                var right = new Vector2(cx + 12, baseY);
                // Mountain body
                DrawLine(left, peak, accent, 2);
                DrawLine(peak, right, accent, 2);
                DrawLine(left, right, accent, 1);
                // Snow cap
                DrawCircle(new Vector2(cx, pos.Y + 7), 3, TerrainAccents[7]);
                break;
            }

            case 7: // Snow — sparkle dots
                for (int i = 0; i < 5; i++)
                {
                    int px = ((hash >> (i * 3)) % 28) + 2;
                    int py = ((hash >> (i * 3 + 1)) % 28) + 2;
                    DrawCircle(new Vector2(pos.X + px, pos.Y + py), 0.8f, accent);
                }
                break;
        }
    }

    /// <summary>Get terrain type at a tile coordinate.</summary>
    public int GetTerrain(int x, int y)
    {
        if (_world == null || _world.TerrainMap == null || x < 0 || x >= MapWidth || y < 0 || y >= MapHeight)
            return 0;
        return _world.TerrainMap[x, y];
    }

    /// <summary>Convert pixel position to tile coordinate.</summary>
    public static Vector2I PixelToTile(Vector2 pixel)
    {
        return new Vector2I((int)(pixel.X / TileSize), (int)(pixel.Y / TileSize));
    }

    /// <summary>Convert tile coordinate to pixel center.</summary>
    public static Vector2 TileToPixel(int tx, int ty)
    {
        return new Vector2(tx * TileSize + TileSize / 2f, ty * TileSize + TileSize / 2f);
    }
}
