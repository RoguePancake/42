using Godot;
using System.Collections.Generic;
using Warship.Data;
using Warship.World;

namespace Warship.UI.Map;

/// <summary>
/// Streams terrain chunks based on camera viewport — Minecraft-style.
///
/// The 6000x3600 map = 188x113 chunks (32x32 tiles each).
/// Only chunks near the camera are baked into textures. Distant chunks are freed.
/// Typically 200-400 chunks loaded = constant VRAM regardless of map size.
///
/// Terrain rendering:
///   - Height-based relief shading (north-neighbor comparison)
///   - Sub-tile 4x4 pixel detail with color jitter
///   - Water depth gradient near coastlines
///   - River polylines drawn on a separate layer above terrain
/// </summary>
public partial class TerrainChunkRenderer : Node2D
{
    private const int ChunkTiles = 32;
    private const int TileSize = MapManagerConstants.TileSize;
    private const int SubBlock = 4;  // 4x4 pixel sub-blocks per tile

    // Streaming
    private const int LoadBuffer = 3;     // chunks beyond viewport to keep loaded
    private const int UnloadBuffer = 5;   // chunks beyond load ring to unload
    private const int MaxBakesPerFrame = 4;

    // Terrain color palette
    private static readonly Color[] Colors =
    {
        new(0.15f, 0.21f, 0.43f),  // 0 DeepWater
        new(0.25f, 0.30f, 0.85f),  // 1 Water
        new(0.86f, 0.82f, 0.55f),  // 2 Sand
        new(0.55f, 0.73f, 0.28f),  // 3 Grass
        new(0.05f, 0.46f, 0.07f),  // 4 Forest
        new(0.47f, 0.44f, 0.40f),  // 5 Hills
        new(0.54f, 0.50f, 0.46f),  // 6 Mountain
        new(0.96f, 0.97f, 0.98f),  // 7 Snow
    };

    private static readonly float[] ElevationRank =
        { 0f, 0.5f, 2f, 3f, 3.5f, 5f, 7f, 8f };

    private WorldData? _world;
    private int _chunksX, _chunksY;
    private bool _initialized;

    // Loaded chunks: key = cx * 10000 + cy
    private readonly Dictionary<int, Sprite2D> _loaded = new();
    private readonly List<(int cx, int cy)> _bakeQueue = new();
    private int _lastCamCX = -999, _lastCamCY = -999;

    public void Initialize(WorldData world)
    {
        if (world.TerrainMap == null)
        {
            GD.PrintErr("[TerrainChunks] No terrain map!");
            return;
        }

        // Clear old chunks
        foreach (var child in GetChildren())
            if (child is Node n) n.QueueFree();
        _loaded.Clear();
        _bakeQueue.Clear();

        _world = world;
        _chunksX = (world.MapWidth + ChunkTiles - 1) / ChunkTiles;
        _chunksY = (world.MapHeight + ChunkTiles - 1) / ChunkTiles;
        _initialized = true;
        _lastCamCX = -999;

        GD.Print($"[TerrainChunks] Streaming {_chunksX}x{_chunksY} chunks on demand");

        // Draw rivers on a layer above terrain
        if (world.RiverPaths.Count > 0)
        {
            var riverLayer = new RiverDrawLayer();
            riverLayer.Name = "Rivers";
            riverLayer.SetRivers(world.RiverPaths, TileSize);
            AddChild(riverLayer);
        }
    }

    public override void _Process(double delta)
    {
        if (!_initialized || _world == null) return;

        var camera = GetViewport().GetCamera2D();
        if (camera == null) return;

        var camPos = camera.GlobalPosition;
        float zoom = camera.Zoom.X;
        var vpSize = GetViewportRect().Size;

        // Visible chunk range
        float halfW = vpSize.X / (2f * zoom);
        float halfH = vpSize.Y / (2f * zoom);
        int chunkPx = ChunkTiles * TileSize;

        int minCX = System.Math.Max(0, (int)((camPos.X - halfW) / chunkPx) - LoadBuffer);
        int maxCX = System.Math.Min(_chunksX - 1, (int)((camPos.X + halfW) / chunkPx) + LoadBuffer);
        int minCY = System.Math.Max(0, (int)((camPos.Y - halfH) / chunkPx) - LoadBuffer);
        int maxCY = System.Math.Min(_chunksY - 1, (int)((camPos.Y + halfH) / chunkPx) + LoadBuffer);

        // Only re-evaluate when camera moves to a new chunk
        int camCX = (int)(camPos.X / chunkPx);
        int camCY = (int)(camPos.Y / chunkPx);

        if (camCX != _lastCamCX || camCY != _lastCamCY)
        {
            _lastCamCX = camCX;
            _lastCamCY = camCY;

            // Queue chunks to load
            _bakeQueue.Clear();
            for (int cx = minCX; cx <= maxCX; cx++)
                for (int cy = minCY; cy <= maxCY; cy++)
                    if (!_loaded.ContainsKey(Key(cx, cy)))
                        _bakeQueue.Add((cx, cy));

            // Unload distant chunks
            int uMinX = minCX - UnloadBuffer;
            int uMaxX = maxCX + UnloadBuffer;
            int uMinY = minCY - UnloadBuffer;
            int uMaxY = maxCY + UnloadBuffer;

            var remove = new List<int>();
            foreach (var (key, sprite) in _loaded)
            {
                int cx = key / 10000, cy = key % 10000;
                if (cx < uMinX || cx > uMaxX || cy < uMinY || cy > uMaxY)
                {
                    sprite.QueueFree();
                    remove.Add(key);
                }
            }
            foreach (int k in remove)
                _loaded.Remove(k);
        }

        // Bake queued chunks (spread across frames)
        int baked = 0;
        while (_bakeQueue.Count > 0 && baked < MaxBakesPerFrame)
        {
            var (cx, cy) = _bakeQueue[0];
            _bakeQueue.RemoveAt(0);

            int key = Key(cx, cy);
            if (_loaded.ContainsKey(key)) continue;

            var sprite = BakeChunk(cx, cy);
            if (sprite != null)
            {
                AddChild(sprite);
                _loaded[key] = sprite;
            }
            baked++;
        }
    }

    private Sprite2D? BakeChunk(int cx, int cy)
    {
        if (_world?.TerrainMap == null) return null;

        int mapW = _world.MapWidth, mapH = _world.MapHeight;
        int seed = _world.Seed;
        int startX = cx * ChunkTiles, startY = cy * ChunkTiles;
        int tilesW = System.Math.Min(ChunkTiles, mapW - startX);
        int tilesH = System.Math.Min(ChunkTiles, mapH - startY);
        if (tilesW <= 0 || tilesH <= 0) return null;

        int imgW = tilesW * TileSize, imgH = tilesH * TileSize;
        var img = Image.CreateEmpty(imgW, imgH, false, Image.Format.Rgba8);

        for (int tx = 0; tx < tilesW; tx++)
        {
            for (int ty = 0; ty < tilesH; ty++)
            {
                int wx = startX + tx, wy = startY + ty;
                int terrain = _world.TerrainMap[wx, wy];
                if (terrain < 0 || terrain >= Colors.Length) terrain = 0;

                Color baseColor = Colors[terrain];

                // Relief shading: compare with north neighbor
                float myElev = TileElevation(_world.TerrainMap, wx, wy, mapW, mapH, seed);
                float northElev = TileElevation(_world.TerrainMap, wx, wy - 1, mapW, mapH, seed);
                float diff = myElev - northElev;

                float shade = 1.0f;
                if (diff > 0.3f) shade = 1.12f;
                else if (diff > 0.1f) shade = 1.06f;
                else if (diff < -0.3f) shade = 0.85f;
                else if (diff < -0.1f) shade = 0.92f;

                // Water depth gradient
                if (terrain <= 1)
                {
                    int coastDist = CoastDistance(_world.TerrainMap, wx, wy, mapW, mapH, 6);
                    if (coastDist <= 1) shade *= 1.25f;
                    else if (coastDist <= 3) shade *= 1.12f;
                }

                Color shaded = Shade(baseColor, shade);

                // Sub-block detail (4x4 pixel blocks with jitter)
                int pxBase = tx * TileSize, pyBase = ty * TileSize;
                int subCount = TileSize / SubBlock;

                for (int sx = 0; sx < subCount; sx++)
                {
                    for (int sy = 0; sy < subCount; sy++)
                    {
                        float jitter = TerrainGenerator.HashFloat(seed,
                            wx * 5003 + wy * 3001 + sx * 71 + sy * 37);
                        float subShade = 1.0f + (jitter - 0.5f) * 0.08f;
                        Color subColor = Shade(shaded, subShade);

                        int bx = pxBase + sx * SubBlock;
                        int by = pyBase + sy * SubBlock;
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
            Position = new Vector2(startX * TileSize, startY * TileSize),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        };
    }

    // ── Helpers ──

    private static int Key(int cx, int cy) => cx * 10000 + cy;

    private static float TileElevation(int[,] map, int x, int y, int w, int h, int seed)
    {
        x = System.Math.Clamp(x, 0, w - 1);
        y = System.Math.Clamp(y, 0, h - 1);
        float baseElev = ElevationRank[map[x, y]];
        float noise = TerrainGenerator.HashFloat(seed, x * 9173 + y * 4111) * 1.2f;
        return baseElev + noise;
    }

    private static int CoastDistance(int[,] map, int x, int y, int w, int h, int maxDist)
    {
        for (int d = 1; d <= maxDist; d++)
        {
            for (int dx = -d; dx <= d; dx++)
            {
                for (int dy = -d; dy <= d; dy++)
                {
                    if (System.Math.Abs(dx) != d && System.Math.Abs(dy) != d) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                    if (map[nx, ny] >= 2) return d;
                }
            }
        }
        return maxDist + 1;
    }

    private static Color Shade(Color c, float mul)
    {
        return new Color(
            System.Math.Clamp(c.R * mul, 0f, 1f),
            System.Math.Clamp(c.G * mul, 0f, 1f),
            System.Math.Clamp(c.B * mul, 0f, 1f),
            c.A);
    }
}

/// <summary>Draws river polylines above terrain chunks.</summary>
public partial class RiverDrawLayer : Node2D
{
    private readonly List<Vector2[]> _paths = new();

    public void SetRivers(List<Vector2[]> tilePaths, int tileSize)
    {
        _paths.Clear();
        foreach (var path in tilePaths)
        {
            var pixels = new Vector2[path.Length];
            for (int i = 0; i < path.Length; i++)
                pixels[i] = new Vector2(
                    path[i].X * tileSize + tileSize / 2f,
                    path[i].Y * tileSize + tileSize / 2f);
            _paths.Add(pixels);
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        var blue = new Color(0.25f, 0.30f, 0.85f);
        var shadow = new Color(0.15f, 0.20f, 0.55f);
        foreach (var path in _paths)
        {
            if (path.Length < 2) continue;
            DrawPolyline(path, shadow, 5, true);
            DrawPolyline(path, blue, 3, true);
        }
    }
}
