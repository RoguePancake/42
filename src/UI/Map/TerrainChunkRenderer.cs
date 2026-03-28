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

    // Minecraft-style terrain colors (top-down map palette)
    private static readonly Color[] TerrainColors =
    {
        new(0.15f, 0.21f, 0.43f),  // 0: Deep Water — dark ocean blue
        new(0.25f, 0.25f, 1.00f),  // 1: Water — bright Minecraft blue
        new(0.87f, 0.83f, 0.57f),  // 2: Sand — warm tan
        new(0.50f, 0.70f, 0.22f),  // 3: Grass — iconic Minecraft green
        new(0.00f, 0.49f, 0.00f),  // 4: Forest — dark green
        new(0.44f, 0.44f, 0.44f),  // 5: Hills — stone grey
        new(0.50f, 0.50f, 0.50f),  // 6: Mountain — lighter grey
        new(1.00f, 1.00f, 1.00f),  // 7: Snow — pure white
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

                        // Minecraft-style: one flat color per tile with subtle per-tile shade variation
                        float tileShade = TerrainGenerator.HashFloat(seed, worldTX * 7919 + worldTY * 6271);
                        Color tileColor;
                        if (tileShade < 0.33f)
                            tileColor = shadow;
                        else if (tileShade > 0.66f)
                            tileColor = highlight;
                        else
                            tileColor = baseColor;

                        // Fill entire tile with one solid color (blocky Minecraft look)
                        for (int px = 0; px < TileSize; px++)
                        {
                            for (int py = 0; py < TileSize; py++)
                            {
                                img.SetPixel(pxStart + px, pyStart + py, tileColor);
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
        var riverColor = new Color(0.25f, 0.25f, 1.0f); // Minecraft blue
        foreach (var path in _riverPixelPaths)
        {
            if (path.Length < 2) continue;
            DrawPolyline(path, riverColor, 3, true);
        }
    }
}
