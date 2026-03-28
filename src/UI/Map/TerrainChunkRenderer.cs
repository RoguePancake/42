using Godot;
using Warship.Data;
using Warship.World;

namespace Warship.UI.Map;

/// <summary>
/// Bakes the 2000x1200 terrain into chunk textures using Minecraft map-style rendering.
///
/// KEY TECHNIQUES (matching Minecraft's cartography table output):
///   1. Height-based relief shading — compare each tile's elevation to its
///      northern neighbor. Higher = brighter, lower = darker. Creates the
///      3D topographic pop that makes Minecraft maps look alive.
///   2. Sub-tile pixel detail — each 32x32 tile is filled with 4x4 pixel
///      "sub-blocks", each with slight color jitter, simulating the dense
///      pixel-per-block look of a Minecraft map at 1:1 scale.
///   3. Water depth gradient — shallow water near coastlines is lighter,
///      deep ocean further out is darker.
///   4. Seamless chunk boundaries — no visible grid lines between chunks.
///
/// 2000/32 = ~63 chunks wide, 1200/32 = ~38 chunks tall = ~2394 chunks total.
/// Each chunk = 32*32px * 32 tiles = 1024x1024 pixel texture.
/// </summary>
public partial class TerrainChunkRenderer : Node2D
{
    public const int ChunkTiles = 32;  // 32x32 tiles per chunk
    public const int TileSize = MapManagerConstants.TileSize; // 32px
    public const int SubBlock = 4;     // 4x4 pixel sub-blocks within each tile

    // ── Minecraft map color palette ─────────────────────────────
    // Matched to Minecraft's actual map rendering colors
    private static readonly Color[] TerrainColors =
    {
        new(0.15f, 0.21f, 0.43f),  // 0: Deep Water — dark ocean
        new(0.25f, 0.30f, 0.85f),  // 1: Water — Minecraft blue
        new(0.86f, 0.82f, 0.55f),  // 2: Sand — warm desert tan
        new(0.55f, 0.73f, 0.28f),  // 3: Grass — iconic Minecraft green
        new(0.05f, 0.46f, 0.07f),  // 4: Forest — dark oak canopy green
        new(0.47f, 0.44f, 0.40f),  // 5: Hills — stone grey-brown
        new(0.54f, 0.50f, 0.46f),  // 6: Mountain — lighter stone
        new(0.96f, 0.97f, 0.98f),  // 7: Snow — near-white
    };

    // Elevation rank per terrain type (used for height shading)
    private static readonly float[] ElevationRank =
    {
        0.0f,   // Deep Water
        0.5f,   // Water
        2.0f,   // Sand
        3.0f,   // Grass
        3.5f,   // Forest
        5.0f,   // Hills
        7.0f,   // Mountain
        8.0f,   // Snow
    };

    private bool _baked = false;

    /// <summary>
    /// Bake terrain into chunk sprite textures with Minecraft map-style rendering.
    /// Call once after world gen.
    /// </summary>
    public void BakeChunks(WorldData world)
    {
        if (world.TerrainMap == null) return;

        foreach (var child in GetChildren())
            child.QueueFree();

        int mapW = world.MapWidth;
        int mapH = world.MapHeight;
        int chunksX = (mapW + ChunkTiles - 1) / ChunkTiles;
        int chunksY = (mapH + ChunkTiles - 1) / ChunkTiles;
        int seed = world.Seed;

        GD.Print($"[ChunkRenderer] Baking {chunksX}x{chunksY} = {chunksX * chunksY} chunks (Minecraft style)...");

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

                for (int tx = 0; tx < tilesW; tx++)
                {
                    for (int ty = 0; ty < tilesH; ty++)
                    {
                        int worldTX = startTileX + tx;
                        int worldTY = startTileY + ty;
                        int terrain = world.TerrainMap[worldTX, worldTY];
                        int pxStart = tx * TileSize;
                        int pyStart = ty * TileSize;

                        // ── Step 1: Base color from terrain type ──
                        Color baseColor = TerrainColors[terrain];

                        // ── Step 2: Height-based relief shading ──
                        // Minecraft compares each block to its northern neighbor.
                        // Higher than north = brighten, lower = darken.
                        float myElev = GetTileElevation(world.TerrainMap, worldTX, worldTY, mapW, mapH, seed);
                        float northElev = GetTileElevation(world.TerrainMap, worldTX, worldTY - 1, mapW, mapH, seed);
                        float heightDiff = myElev - northElev;

                        // Shade multiplier: positive diff = brighter, negative = darker
                        float shadeMul = 1.0f;
                        if (heightDiff > 0.3f)
                            shadeMul = 1.12f;  // noticeably brighter (south-facing slope lit)
                        else if (heightDiff > 0.1f)
                            shadeMul = 1.06f;  // slightly brighter
                        else if (heightDiff < -0.3f)
                            shadeMul = 0.85f;  // noticeably darker (north-facing shadow)
                        else if (heightDiff < -0.1f)
                            shadeMul = 0.92f;  // slightly darker

                        // ── Step 3: Water depth — lighten water near coastlines ──
                        if (terrain <= 1)
                        {
                            int coastDist = GetCoastDistance(world.TerrainMap, worldTX, worldTY, mapW, mapH, 6);
                            if (coastDist <= 1)
                                shadeMul *= 1.25f;  // very near shore — bright shallow water
                            else if (coastDist <= 3)
                                shadeMul *= 1.12f;  // nearshore
                            // Deep ocean stays dark
                        }

                        Color shadedBase = ApplyShade(baseColor, shadeMul);

                        // ── Step 4: Per-sub-block pixel detail ──
                        // Divide each 32x32 tile into 4x4 pixel sub-blocks (8x8 grid).
                        // Each sub-block gets a slight color jitter for that dense
                        // Minecraft map texture.
                        int subCountX = TileSize / SubBlock;
                        int subCountY = TileSize / SubBlock;

                        for (int sx = 0; sx < subCountX; sx++)
                        {
                            for (int sy = 0; sy < subCountY; sy++)
                            {
                                // Deterministic jitter per sub-block
                                float jitter = TerrainGenerator.HashFloat(seed,
                                    worldTX * 5003 + worldTY * 3001 + sx * 71 + sy * 37);

                                // Very subtle variation: +-4% brightness
                                float subShade = 1.0f + (jitter - 0.5f) * 0.08f;
                                Color subColor = ApplyShade(shadedBase, subShade);

                                // Fill the 4x4 sub-block
                                int bx = pxStart + sx * SubBlock;
                                int by = pyStart + sy * SubBlock;
                                for (int px = 0; px < SubBlock; px++)
                                {
                                    for (int py = 0; py < SubBlock; py++)
                                    {
                                        img.SetPixel(bx + px, by + py, subColor);
                                    }
                                }
                            }
                        }
                    }
                }

                var texture = ImageTexture.CreateFromImage(img);
                var sprite = new Sprite2D
                {
                    Texture = texture,
                    Centered = false,
                    Position = new Vector2(startTileX * TileSize, startTileY * TileSize),
                    TextureFilter = CanvasItem.TextureFilterEnum.Nearest, // crisp pixels, no blur
                };
                AddChild(sprite);
            }
        }

        _baked = true;
        GD.Print("[ChunkRenderer] Baking complete!");
    }

    /// <summary>
    /// Get a continuous elevation value for a tile, combining terrain rank
    /// with small per-tile noise for natural variation within same terrain types.
    /// </summary>
    private static float GetTileElevation(int[,] terrainMap, int x, int y, int mapW, int mapH, int seed)
    {
        // Clamp to map bounds (edges repeat)
        x = System.Math.Clamp(x, 0, mapW - 1);
        y = System.Math.Clamp(y, 0, mapH - 1);

        int terrain = terrainMap[x, y];
        float baseElev = ElevationRank[terrain];

        // Add per-tile noise so tiles of the same type still have height variation
        float noise = TerrainGenerator.HashFloat(seed, x * 9173 + y * 4111) * 1.2f;
        return baseElev + noise;
    }

    /// <summary>
    /// Count how many tiles to the nearest land from a water tile.
    /// Used for shallow/deep water coloring.
    /// </summary>
    private static int GetCoastDistance(int[,] terrainMap, int x, int y, int mapW, int mapH, int maxDist)
    {
        for (int d = 1; d <= maxDist; d++)
        {
            for (int dx = -d; dx <= d; dx++)
            {
                for (int dy = -d; dy <= d; dy++)
                {
                    if (System.Math.Abs(dx) != d && System.Math.Abs(dy) != d) continue; // only check ring
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || nx >= mapW || ny < 0 || ny >= mapH) continue;
                    if (terrainMap[nx, ny] >= 2) return d; // land found
                }
            }
        }
        return maxDist + 1; // deep ocean
    }

    /// <summary>Apply a brightness multiplier to a color, clamping to [0,1].</summary>
    private static Color ApplyShade(Color c, float mul)
    {
        return new Color(
            System.Math.Clamp(c.R * mul, 0f, 1f),
            System.Math.Clamp(c.G * mul, 0f, 1f),
            System.Math.Clamp(c.B * mul, 0f, 1f),
            c.A);
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
        var riverColor = new Color(0.25f, 0.30f, 0.85f); // Match water color
        var riverShadow = new Color(0.15f, 0.20f, 0.55f);
        foreach (var path in _riverPixelPaths)
        {
            if (path.Length < 2) continue;
            DrawPolyline(path, riverShadow, 5, true); // dark outline
            DrawPolyline(path, riverColor, 3, true);   // river body
        }
    }
}
