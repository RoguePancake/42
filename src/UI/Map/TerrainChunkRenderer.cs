using Godot;
using Warship.Data;
using Warship.World;

namespace Warship.UI.Map;

/// <summary>
/// Bakes the 600x360 terrain into chunk textures (32x32 tiles per chunk).
/// Each chunk is a Sprite2D with a pre-baked ImageTexture.
/// Godot's built-in frustum culling handles which chunks are visible.
///
/// 600/32 = ~19 chunks wide, 360/32 = ~12 chunks tall = ~228 chunks total.
/// Each chunk = 32*32px * 32 tiles = 1024x1024 pixel texture.
/// </summary>
public partial class TerrainChunkRenderer : Node2D
{
    public const int ChunkTiles = 32;  // 32x32 tiles per chunk
    public const int TileSize = MapManagerConstants.TileSize; // 32px

    // SNES-style terrain colors
    private static readonly Color[] TerrainColors =
    {
        new(0.08f, 0.16f, 0.32f),  // 0: Deep Water
        new(0.12f, 0.32f, 0.58f),  // 1: Water
        new(0.85f, 0.76f, 0.50f),  // 2: Sand
        new(0.32f, 0.62f, 0.30f),  // 3: Grass
        new(0.18f, 0.40f, 0.20f),  // 4: Forest
        new(0.48f, 0.56f, 0.38f),  // 5: Hills
        new(0.55f, 0.48f, 0.42f),  // 6: Mountain
        new(0.92f, 0.94f, 0.96f),  // 7: Snow
    };

    private bool _baked = false;

    /// <summary>
    /// Bake terrain into chunk sprite textures. Call once after world gen.
    /// </summary>
    public void BakeChunks(WorldData world)
    {
        if (world.TerrainMap == null) return;

        // Clear any existing chunks
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }

        int mapW = world.MapWidth;
        int mapH = world.MapHeight;
        int chunksX = (mapW + ChunkTiles - 1) / ChunkTiles;
        int chunksY = (mapH + ChunkTiles - 1) / ChunkTiles;
        int seed = world.Seed;

        GD.Print($"[ChunkRenderer] Baking {chunksX}x{chunksY} = {chunksX * chunksY} chunks...");

        for (int cx = 0; cx < chunksX; cx++)
        {
            for (int cy = 0; cy < chunksY; cy++)
            {
                int startTileX = cx * ChunkTiles;
                int startTileY = cy * ChunkTiles;
                int tilesW = System.Math.Min(ChunkTiles, mapW - startTileX);
                int tilesH = System.Math.Min(ChunkTiles, mapH - startTileY);

                int imgW = tilesW * TileSize;
                int imgH = tilesH * TileSize;
                var img = Image.CreateEmpty(imgW, imgH, false, Image.Format.Rgba8);

                // Paint each tile's pixels
                for (int tx = 0; tx < tilesW; tx++)
                {
                    for (int ty = 0; ty < tilesH; ty++)
                    {
                        int worldTX = startTileX + tx;
                        int worldTY = startTileY + ty;
                        int terrain = world.TerrainMap[worldTX, worldTY];
                        Color baseColor = TerrainColors[terrain];
                        Color highlight = baseColor.Lightened(0.12f);
                        Color shadow = baseColor.Darkened(0.12f);

                        int pxStart = tx * TileSize;
                        int pyStart = ty * TileSize;

                        for (int px = 0; px < TileSize; px++)
                        {
                            for (int py = 0; py < TileSize; py++)
                            {
                                // Deterministic pixel-level detail using hash
                                float n = TerrainGenerator.HashFloat(seed, worldTX * 10000 + worldTY * 100 + px * 37 + py);
                                Color c = baseColor;

                                switch (terrain)
                                {
                                    case 0: // Deep Water — dark waves
                                    case 1: // Water — lighter waves
                                        c = n > 0.7f ? highlight : (n < 0.15f ? shadow : baseColor);
                                        break;
                                    case 2: // Sand — speckles
                                        c = n > 0.85f ? shadow : (n < 0.1f ? highlight : baseColor);
                                        break;
                                    case 3: // Grass — tufts and flowers
                                        c = n > 0.8f ? highlight : baseColor;
                                        if (n > 0.95f) c = new Color(0.9f, 0.85f, 0.2f); // Flower
                                        break;
                                    case 4: // Forest — canopy
                                        c = n > 0.5f ? highlight : shadow;
                                        break;
                                    case 5: // Hills — rolling
                                        c = n > 0.5f ? highlight : shadow;
                                        break;
                                    case 6: // Mountain — crags + snow caps
                                        c = n > 0.6f ? shadow : baseColor;
                                        if (py < TileSize / 3 && n > 0.4f) c = Colors.White;
                                        break;
                                    case 7: // Snow — sparkle
                                        c = n > 0.8f ? highlight : baseColor;
                                        break;
                                }

                                img.SetPixel(pxStart + px, pyStart + py, c);
                            }
                        }
                    }
                }

                // Create sprite from image
                var texture = ImageTexture.CreateFromImage(img);
                var sprite = new Sprite2D
                {
                    Texture = texture,
                    Centered = false,
                    Position = new Vector2(startTileX * TileSize, startTileY * TileSize),
                };
                AddChild(sprite);
            }
        }

        _baked = true;
        GD.Print("[ChunkRenderer] Baking complete!");
    }

    /// <summary>Draw rivers on top of terrain (called once after baking).</summary>
    public void DrawRivers(WorldData world)
    {
        if (world.RiverPaths.Count == 0) return;

        var riverLayer = new RiverDrawLayer();
        riverLayer.SetRivers(world.RiverPaths, TileSize);
        AddChild(riverLayer);
    }

    public bool IsBaked => _baked;
}

/// <summary>Draws rivers as polylines on a separate layer above terrain chunks.</summary>
public partial class RiverDrawLayer : Node2D
{
    private System.Collections.Generic.List<Vector2[]> _riverPixelPaths = new();

    public void SetRivers(System.Collections.Generic.List<Vector2[]> riverTilePaths, int tileSize)
    {
        _riverPixelPaths.Clear();
        foreach (var path in riverTilePaths)
        {
            var pixelPath = new Vector2[path.Length];
            for (int i = 0; i < path.Length; i++)
                pixelPath[i] = new Vector2(
                    path[i].X * tileSize + tileSize / 2f,
                    path[i].Y * tileSize + tileSize / 2f);
            _riverPixelPaths.Add(pixelPath);
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        var riverColor = new Color(0.15f, 0.35f, 0.65f);
        foreach (var path in _riverPixelPaths)
        {
            if (path.Length < 2) continue;
            DrawPolyline(path, new Color(0, 0, 0, 0.3f), 5, true);
            DrawPolyline(path, riverColor, 3, true);
            DrawPolyline(path, Colors.White * new Color(1, 1, 1, 0.3f), 1, true);
        }
    }
}
