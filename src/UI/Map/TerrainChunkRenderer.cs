using Godot;
using System.Collections.Generic;
using Warship.Data;
using Warship.World;

namespace Warship.UI.Map;

/// <summary>
/// Streams terrain chunks based on camera position — Minecraft-style.
///
/// Only chunks near the camera are baked and held in VRAM. Distant chunks
/// are freed. 6000x3600 map = 188x113 = ~21,000 total chunks, but only
/// ~200-400 loaded at any time = constant VRAM usage.
///
/// KEY TECHNIQUES:
///   1. Height-based relief shading — north-neighbor comparison
///   2. Sub-tile 4x4 pixel detail with jitter
///   3. Water depth gradient near coastlines
///   4. Seamless chunk boundaries
///   5. Camera-based streaming — load ring around viewport, free the rest
///
/// Each chunk = 32x32 tiles = 1024x1024 pixel texture (~4MB VRAM).
/// Load radius keeps ~200-400 chunks loaded at any time = 800MB-1.6GB max.
/// </summary>
public partial class TerrainChunkRenderer : Node2D
{
    public const int ChunkTiles = 32;  // 32x32 tiles per chunk
    public const int TileSize = MapManagerConstants.TileSize; // 32px
    public const int SubBlock = 4;     // 4x4 pixel sub-blocks within each tile

    // ── Streaming config ────────────────────────────────────────
    // How many chunks beyond the viewport edge to keep loaded
    private const int LoadBufferChunks = 3;
    // How many chunks beyond the load ring before unloading
    private const int UnloadBufferChunks = 5;
    // Max chunks to bake per frame (spread load across frames)
    private const int MaxBakesPerFrame = 4;

    // ── Minecraft map color palette ─────────────────────────────
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
        0.0f, 0.5f, 2.0f, 3.0f, 3.5f, 5.0f, 7.0f, 8.0f
    };

    // ── State ───────────────────────────────────────────────────
    private WorldData? _world;
    private int _chunksX, _chunksY;
    private bool _initialized = false;

    // Loaded chunk sprites indexed by (cx, cy) packed as cx * 10000 + cy
    private readonly Dictionary<int, Sprite2D> _loadedChunks = new();

    // Track which chunks we need to load (queue spread across frames)
    private readonly List<(int cx, int cy)> _bakeQueue = new();

    // Last known camera chunk position (avoid re-checking every frame)
    private int _lastCamChunkX = -999, _lastCamChunkY = -999;

    /// <summary>
    /// Initialize streaming with world data. Does NOT bake anything upfront.
    /// Chunks are baked on-demand in _Process based on camera position.
    /// </summary>
    public void BakeChunks(WorldData world)
    {
        if (world.TerrainMap == null) return;

        // Clear any existing chunks
        foreach (var child in GetChildren())
            if (child is Sprite2D) child.QueueFree();
        _loadedChunks.Clear();
        _bakeQueue.Clear();

        _world = world;
        _chunksX = (world.MapWidth + ChunkTiles - 1) / ChunkTiles;
        _chunksY = (world.MapHeight + ChunkTiles - 1) / ChunkTiles;
        _initialized = true;
        _lastCamChunkX = -999; // force first update

        GD.Print($"[ChunkRenderer] Streaming mode: {_chunksX}x{_chunksY} = {_chunksX * _chunksY} total chunks, loading on demand");
    }

    public override void _Process(double delta)
    {
        if (!_initialized || _world == null) return;

        // Get camera position to determine which chunks to load
        var camera = GetViewport().GetCamera2D();
        if (camera == null) return;

        var camPos = camera.GlobalPosition;
        float zoom = camera.Zoom.X;
        var vpSize = GetViewportRect().Size;

        // Calculate visible chunk range
        float halfW = vpSize.X / (2f * zoom);
        float halfH = vpSize.Y / (2f * zoom);

        int minChunkX = (int)((camPos.X - halfW) / (ChunkTiles * TileSize)) - LoadBufferChunks;
        int maxChunkX = (int)((camPos.X + halfW) / (ChunkTiles * TileSize)) + LoadBufferChunks;
        int minChunkY = (int)((camPos.Y - halfH) / (ChunkTiles * TileSize)) - LoadBufferChunks;
        int maxChunkY = (int)((camPos.Y + halfH) / (ChunkTiles * TileSize)) + LoadBufferChunks;

        // Clamp to map bounds
        minChunkX = System.Math.Max(0, minChunkX);
        maxChunkX = System.Math.Min(_chunksX - 1, maxChunkX);
        minChunkY = System.Math.Max(0, minChunkY);
        maxChunkY = System.Math.Min(_chunksY - 1, maxChunkY);

        // Check if camera has moved to a different chunk — skip expensive work if not
        int camChunkX = (int)(camPos.X / (ChunkTiles * TileSize));
        int camChunkY = (int)(camPos.Y / (ChunkTiles * TileSize));

        if (camChunkX != _lastCamChunkX || camChunkY != _lastCamChunkY)
        {
            _lastCamChunkX = camChunkX;
            _lastCamChunkY = camChunkY;

            // Queue chunks that need loading
            _bakeQueue.Clear();
            for (int cx = minChunkX; cx <= maxChunkX; cx++)
            {
                for (int cy = minChunkY; cy <= maxChunkY; cy++)
                {
                    int key = ChunkKey(cx, cy);
                    if (!_loadedChunks.ContainsKey(key))
                    {
                        _bakeQueue.Add((cx, cy));
                    }
                }
            }

            // Unload distant chunks
            int unloadMinX = minChunkX - UnloadBufferChunks;
            int unloadMaxX = maxChunkX + UnloadBufferChunks;
            int unloadMinY = minChunkY - UnloadBufferChunks;
            int unloadMaxY = maxChunkY + UnloadBufferChunks;

            var toRemove = new List<int>();
            foreach (var (key, sprite) in _loadedChunks)
            {
                int cx = key / 10000;
                int cy = key % 10000;
                if (cx < unloadMinX || cx > unloadMaxX || cy < unloadMinY || cy > unloadMaxY)
                {
                    sprite.QueueFree();
                    toRemove.Add(key);
                }
            }
            foreach (var key in toRemove)
                _loadedChunks.Remove(key);
        }

        // Bake queued chunks (spread across frames)
        int baked = 0;
        while (_bakeQueue.Count > 0 && baked < MaxBakesPerFrame)
        {
            var (cx, cy) = _bakeQueue[0];
            _bakeQueue.RemoveAt(0);

            int key = ChunkKey(cx, cy);
            if (_loadedChunks.ContainsKey(key)) continue; // already loaded

            var sprite = BakeSingleChunk(cx, cy);
            if (sprite != null)
            {
                AddChild(sprite);
                _loadedChunks[key] = sprite;
            }
            baked++;
        }
    }

    /// <summary>Bake a single chunk into a Sprite2D texture.</summary>
    private Sprite2D? BakeSingleChunk(int cx, int cy)
    {
        if (_world?.TerrainMap == null) return null;

        int mapW = _world.MapWidth;
        int mapH = _world.MapHeight;
        int seed = _world.Seed;

        int startTileX = cx * ChunkTiles;
        int startTileY = cy * ChunkTiles;
        int tilesW = System.Math.Min(ChunkTiles, mapW - startTileX);
        int tilesH = System.Math.Min(ChunkTiles, mapH - startTileY);
        if (tilesW <= 0 || tilesH <= 0) return null;

        int imgW = tilesW * TileSize;
        int imgH = tilesH * TileSize;
        var img = Image.CreateEmpty(imgW, imgH, false, Image.Format.Rgba8);

        for (int tx = 0; tx < tilesW; tx++)
        {
            for (int ty = 0; ty < tilesH; ty++)
            {
                int worldTX = startTileX + tx;
                int worldTY = startTileY + ty;
                int terrain = _world.TerrainMap[worldTX, worldTY];
                int pxStart = tx * TileSize;
                int pyStart = ty * TileSize;

                // Step 1: Base color
                Color baseColor = TerrainColors[terrain];

                // Step 2: Height-based relief shading
                float myElev = GetTileElevation(_world.TerrainMap, worldTX, worldTY, mapW, mapH, seed);
                float northElev = GetTileElevation(_world.TerrainMap, worldTX, worldTY - 1, mapW, mapH, seed);
                float heightDiff = myElev - northElev;

                float shadeMul = 1.0f;
                if (heightDiff > 0.3f)
                    shadeMul = 1.12f;
                else if (heightDiff > 0.1f)
                    shadeMul = 1.06f;
                else if (heightDiff < -0.3f)
                    shadeMul = 0.85f;
                else if (heightDiff < -0.1f)
                    shadeMul = 0.92f;

                // Step 3: Water depth gradient
                if (terrain <= 1)
                {
                    int coastDist = GetCoastDistance(_world.TerrainMap, worldTX, worldTY, mapW, mapH, 6);
                    if (coastDist <= 1)
                        shadeMul *= 1.25f;
                    else if (coastDist <= 3)
                        shadeMul *= 1.12f;
                }

                Color shadedBase = ApplyShade(baseColor, shadeMul);

                // Step 4: Sub-block pixel detail (4x4 blocks with jitter)
                int subCountX = TileSize / SubBlock;
                int subCountY = TileSize / SubBlock;

                for (int sx = 0; sx < subCountX; sx++)
                {
                    for (int sy = 0; sy < subCountY; sy++)
                    {
                        float jitter = TerrainGenerator.HashFloat(seed,
                            worldTX * 5003 + worldTY * 3001 + sx * 71 + sy * 37);

                        float subShade = 1.0f + (jitter - 0.5f) * 0.08f;
                        Color subColor = ApplyShade(shadedBase, subShade);

                        int bx = pxStart + sx * SubBlock;
                        int by = pyStart + sy * SubBlock;
                        for (int px = 0; px < SubBlock; px++)
                            for (int py = 0; py < SubBlock; py++)
                                img.SetPixel(bx + px, by + py, subColor);
                    }
                }
            }
        }

        var texture = ImageTexture.CreateFromImage(img);
        return new Sprite2D
        {
            Texture = texture,
            Centered = false,
            Position = new Vector2(startTileX * TileSize, startTileY * TileSize),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        };
    }

    private static int ChunkKey(int cx, int cy) => cx * 10000 + cy;

    // ── Shared helpers (unchanged) ──────────────────────────────

    private static float GetTileElevation(int[,] terrainMap, int x, int y, int mapW, int mapH, int seed)
    {
        x = System.Math.Clamp(x, 0, mapW - 1);
        y = System.Math.Clamp(y, 0, mapH - 1);
        float baseElev = ElevationRank[terrainMap[x, y]];
        float noise = TerrainGenerator.HashFloat(seed, x * 9173 + y * 4111) * 1.2f;
        return baseElev + noise;
    }

    private static int GetCoastDistance(int[,] terrainMap, int x, int y, int mapW, int mapH, int maxDist)
    {
        for (int d = 1; d <= maxDist; d++)
        {
            for (int dx = -d; dx <= d; dx++)
            {
                for (int dy = -d; dy <= d; dy++)
                {
                    if (System.Math.Abs(dx) != d && System.Math.Abs(dy) != d) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || nx >= mapW || ny < 0 || ny >= mapH) continue;
                    if (terrainMap[nx, ny] >= 2) return d;
                }
            }
        }
        return maxDist + 1;
    }

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

    public bool IsBaked => _initialized;
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
        var riverColor = new Color(0.25f, 0.30f, 0.85f);
        var riverShadow = new Color(0.15f, 0.20f, 0.55f);
        foreach (var path in _riverPixelPaths)
        {
            if (path.Length < 2) continue;
            DrawPolyline(path, riverShadow, 5, true);
            DrawPolyline(path, riverColor, 3, true);
        }
    }
}
