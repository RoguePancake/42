using Godot;
using System;
using System.Collections.Generic;
using Warship.Core;
using Warship.Data;
using Warship.World;

namespace Warship.UI.Map;

/// <summary>
/// Minecraft-style chunk-based terrain renderer.
///
/// The map is divided into chunks (16x16 tiles each). Only chunks near the camera
/// are baked into textures and held in VRAM. Distant chunks are freed.
/// This gives constant memory usage regardless of total map size.
///
/// RENDERING PIPELINE per chunk:
///   1. Base terrain color from biome type
///   2. Relief shading — compare elevation with north neighbor for 3D look
///   3. Sub-tile pixel detail — 4x4 pixel blocks with color jitter for texture
///   4. Water depth gradient — shallow water brighter near coastlines
///   5. Terrain edge blending — soft transitions between biome types
///   6. Grid lines at low zoom — faint tile borders when zoomed in
///
/// STREAMING:
///   - Camera moves → calculate visible chunk range
///   - Queue any unloaded chunks in that range
///   - Bake max 4 chunks per frame (spread CPU load)
///   - Unload chunks far outside viewport
///   - Typically 50-200 chunks loaded at any time
///
/// 512x512 map = 32x32 chunks. Each chunk = 16x16 tiles × 16px = 256×256 pixel texture.
/// </summary>
public partial class ChunkRenderer : Node2D
{
    // ── Constants ──
    private const int ChunkTiles = 16;                                  // tiles per chunk side
    private const int TS = TerrainGenerator.TileSize;                   // pixels per tile (16)
    private const int ChunkPixels = ChunkTiles * TS;                    // 256 px per chunk side
    private const int SubBlock = 4;                                     // 4x4 pixel sub-blocks
    private const int SubsPerTile = TS / SubBlock;                      // 4 sub-blocks per tile side

    // ── Streaming config ──
    private const int LoadBuffer = 2;                                   // chunks beyond viewport to preload
    private const int UnloadBuffer = 4;                                 // chunks beyond load ring to free
    private const int MaxBakesPerFrame = 4;                             // spread baking across frames

    // ── Elevation rank per terrain type (for relief shading) ──
    private static readonly float[] ElevRank = { 0f, 0.5f, 2f, 3f, 3.5f, 5f, 7f, 8f };

    // ── State ──
    private int[]? _terrain;
    private int _mapW, _mapH;
    private int _chunksX, _chunksY;
    private int _seed;
    private bool _ready;

    private readonly Dictionary<long, Sprite2D> _loaded = new();
    private readonly List<(int cx, int cy)> _bakeQueue = new();
    private int _lastCamCX = -999, _lastCamCY = -999;

    // ════════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Initialize the renderer with terrain data. Call once after world generation.
    /// Does NOT bake anything upfront — chunks stream on demand in _Process.
    /// </summary>
    public void Initialize(int[] terrain, int mapW, int mapH, int seed)
    {
        // Clear any previous state
        foreach (var child in GetChildren())
            if (child is Node n) n.QueueFree();
        _loaded.Clear();
        _bakeQueue.Clear();

        _terrain = terrain;
        _mapW = mapW;
        _mapH = mapH;
        _seed = seed;
        _chunksX = (mapW + ChunkTiles - 1) / ChunkTiles;
        _chunksY = (mapH + ChunkTiles - 1) / ChunkTiles;
        _lastCamCX = -999;
        _ready = true;

        GD.Print($"[ChunkRenderer] Initialized: {_chunksX}x{_chunksY} chunks ({_chunksX * _chunksY} total), streaming on demand.");
    }

    // ════════════════════════════════════════════════════════════════
    //  FRAME UPDATE — Streaming logic
    // ════════════════════════════════════════════════════════════════

    public override void _Process(double delta)
    {
        if (!_ready || _terrain == null) return;

        var camera = GetViewport().GetCamera2D();
        if (camera == null) return;

        var camPos = camera.GlobalPosition;
        float zoom = camera.Zoom.X;
        var vpSize = GetViewportRect().Size;

        // ── Calculate visible chunk range ──
        float halfW = vpSize.X / (2f * zoom);
        float halfH = vpSize.Y / (2f * zoom);

        int minCX = Math.Max(0, (int)((camPos.X - halfW) / ChunkPixels) - LoadBuffer);
        int maxCX = Math.Min(_chunksX - 1, (int)((camPos.X + halfW) / ChunkPixels) + LoadBuffer);
        int minCY = Math.Max(0, (int)((camPos.Y - halfH) / ChunkPixels) - LoadBuffer);
        int maxCY = Math.Min(_chunksY - 1, (int)((camPos.Y + halfH) / ChunkPixels) + LoadBuffer);

        // ── Only re-evaluate when camera crosses a chunk boundary ──
        int camCX = (int)(camPos.X / ChunkPixels);
        int camCY = (int)(camPos.Y / ChunkPixels);

        if (camCX != _lastCamCX || camCY != _lastCamCY)
        {
            _lastCamCX = camCX;
            _lastCamCY = camCY;

            // Queue missing chunks
            _bakeQueue.Clear();
            for (int cy = minCY; cy <= maxCY; cy++)
                for (int cx = minCX; cx <= maxCX; cx++)
                    if (!_loaded.ContainsKey(ChunkKey(cx, cy)))
                        _bakeQueue.Add((cx, cy));

            // Unload distant chunks
            int uMinX = minCX - UnloadBuffer, uMaxX = maxCX + UnloadBuffer;
            int uMinY = minCY - UnloadBuffer, uMaxY = maxCY + UnloadBuffer;

            var remove = new List<long>();
            foreach (var (key, sprite) in _loaded)
            {
                UnpackKey(key, out int cx, out int cy);
                if (cx < uMinX || cx > uMaxX || cy < uMinY || cy > uMaxY)
                {
                    sprite.QueueFree();
                    remove.Add(key);
                }
            }
            foreach (long k in remove)
                _loaded.Remove(k);
        }

        // ── Bake queued chunks (spread across frames) ──
        int baked = 0;
        while (_bakeQueue.Count > 0 && baked < MaxBakesPerFrame)
        {
            var (cx, cy) = _bakeQueue[0];
            _bakeQueue.RemoveAt(0);

            long key = ChunkKey(cx, cy);
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

    // ════════════════════════════════════════════════════════════════
    //  CHUNK BAKING — The pixel-art terrain rendering pipeline
    // ════════════════════════════════════════════════════════════════

    private Sprite2D? BakeChunk(int cx, int cy)
    {
        if (_terrain == null) return null;

        int startTX = cx * ChunkTiles;
        int startTY = cy * ChunkTiles;
        int tilesW = Math.Min(ChunkTiles, _mapW - startTX);
        int tilesH = Math.Min(ChunkTiles, _mapH - startTY);
        if (tilesW <= 0 || tilesH <= 0) return null;

        int imgW = tilesW * TS;
        int imgH = tilesH * TS;
        var img = Image.CreateEmpty(imgW, imgH, false, Image.Format.Rgba8);

        for (int ty = 0; ty < tilesH; ty++)
        {
            for (int tx = 0; tx < tilesW; tx++)
            {
                int worldX = startTX + tx;
                int worldY = startTY + ty;
                int terrain = GetTerrain(worldX, worldY);

                // ── Step 1: Base biome color ──
                Color baseColor = TerrainInfo.GetColor(terrain);

                // ── Step 2: Relief shading (north-neighbor elevation comparison) ──
                float myElev = GetElevation(worldX, worldY);
                float northElev = GetElevation(worldX, worldY - 1);
                float diff = myElev - northElev;

                float shade = 1.0f;
                if (diff > 0.3f) shade = 1.15f;       // south-facing slope = lit
                else if (diff > 0.1f) shade = 1.07f;
                else if (diff < -0.3f) shade = 0.82f;  // north-facing = shadow
                else if (diff < -0.1f) shade = 0.90f;

                // ── Step 3: Water depth gradient ──
                if (terrain <= (int)Terrain.Water)
                {
                    int coastDist = CoastDistance(worldX, worldY, 5);
                    if (coastDist <= 1) shade *= 1.30f;       // shallow = bright
                    else if (coastDist <= 2) shade *= 1.15f;
                    else if (coastDist <= 3) shade *= 1.07f;
                    // Deep water stays dark
                }

                // ── Step 4: Terrain edge blending ──
                // Check if any neighbor is a different terrain type
                // If so, slightly tint toward neighbor's color for soft edges
                Color blendedBase = baseColor;
                if (terrain >= (int)Terrain.Sand) // only blend land tiles
                {
                    blendedBase = BlendWithNeighbors(worldX, worldY, baseColor, terrain);
                }

                Color shadedBase = Shade(blendedBase, shade);

                // ── Step 5: Sub-tile pixel detail (4x4 blocks with jitter) ──
                int pxBase = tx * TS;
                int pyBase = ty * TS;

                for (int sy = 0; sy < SubsPerTile; sy++)
                {
                    for (int sx = 0; sx < SubsPerTile; sx++)
                    {
                        // Deterministic jitter per sub-block
                        float jitter = SimRng.Hash(_seed,
                            worldX * 5003 + worldY * 3001 + sx * 71 + sy * 37);
                        float subShade = 1.0f + (jitter - 0.5f) * 0.10f;

                        // Extra detail for specific terrain types
                        Color subColor = shadedBase;

                        if (terrain == (int)Terrain.Forest)
                        {
                            // Forest: dark/light canopy variation
                            float treeness = SimRng.Hash(_seed, worldX * 7919 + worldY * 6271 + sx * 13 + sy * 17);
                            if (treeness > 0.6f)
                                subShade *= 0.85f; // dark tree shadow
                            else if (treeness > 0.3f)
                                subShade *= 1.05f; // canopy highlight
                        }
                        else if (terrain == (int)Terrain.Grass)
                        {
                            // Grass: occasional flower/dirt specks
                            float speck = SimRng.Hash(_seed, worldX * 4001 + worldY * 2903 + sx * 41 + sy * 29);
                            if (speck > 0.95f)
                                subColor = new Color(0.85f, 0.75f, 0.20f); // yellow flower
                            else if (speck > 0.92f)
                                subColor = new Color(0.55f, 0.45f, 0.30f); // dirt patch
                        }
                        else if (terrain == (int)Terrain.Mountain)
                        {
                            // Mountain: snow caps on upper sub-blocks
                            float snowChance = SimRng.Hash(_seed, worldX * 3571 + worldY * 1999 + sx + sy * 4);
                            if (sy == 0 && snowChance > 0.4f)
                                subColor = new Color(0.92f, 0.93f, 0.95f); // snow cap
                        }
                        else if (terrain == (int)Terrain.Sand)
                        {
                            // Sand: slight dune ridges
                            float dune = SimRng.Hash(_seed, worldX * 2111 + worldY * 1777 + sx * 7 + sy * 3);
                            if (dune > 0.8f)
                                subShade *= 1.08f; // dune crest
                            else if (dune < 0.15f)
                                subShade *= 0.92f; // dune shadow
                        }
                        else if (terrain == (int)Terrain.Water || terrain == (int)Terrain.DeepWater)
                        {
                            // Water: slight wave pattern
                            float wave = SimRng.Hash(_seed, worldX * 1511 + worldY * 1301 + sx * 3 + sy * 11);
                            if (wave > 0.85f)
                                subShade *= 1.12f; // wave crest
                        }
                        else if (terrain == (int)Terrain.Snow)
                        {
                            // Snow: subtle sparkle
                            float sparkle = SimRng.Hash(_seed, worldX * 8011 + worldY * 7013 + sx * 19 + sy * 23);
                            if (sparkle > 0.9f)
                                subShade *= 1.08f;
                        }
                        else if (terrain == (int)Terrain.Hills)
                        {
                            // Hills: rocky texture
                            float rock = SimRng.Hash(_seed, worldX * 6007 + worldY * 5003 + sx * 31 + sy * 37);
                            if (rock > 0.7f)
                                subShade *= 0.88f; // rock shadow
                            else if (rock < 0.2f)
                                subShade *= 1.10f; // rock highlight
                        }

                        subColor = Shade(subColor, subShade);

                        // Fill the 4x4 pixel sub-block
                        int bx = pxBase + sx * SubBlock;
                        int by = pyBase + sy * SubBlock;
                        for (int py = 0; py < SubBlock; py++)
                            for (int px = 0; px < SubBlock; px++)
                                img.SetPixel(bx + px, by + py, subColor);
                    }
                }

                // ── Step 6: Tile border (faint grid line on right and bottom edges) ──
                // Only visible when zoomed in — creates the Minecraft map grid feel
                Color gridColor = Shade(shadedBase, 0.85f);
                for (int p = 0; p < TS; p++)
                {
                    // Right edge
                    if (pxBase + TS - 1 < imgW)
                        img.SetPixel(pxBase + TS - 1, pyBase + p, gridColor);
                    // Bottom edge
                    if (pyBase + TS - 1 < imgH)
                        img.SetPixel(pxBase + p, pyBase + TS - 1, gridColor);
                }
            }
        }

        var texture = ImageTexture.CreateFromImage(img);
        return new Sprite2D
        {
            Texture = texture,
            Centered = false,
            Position = new Vector2(startTX * TS, startTY * TS),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        };
    }

    // ════════════════════════════════════════════════════════════════
    //  TERRAIN QUERY HELPERS
    // ════════════════════════════════════════════════════════════════

    private int GetTerrain(int x, int y)
    {
        if (_terrain == null || x < 0 || x >= _mapW || y < 0 || y >= _mapH) return 0;
        return _terrain[x + y * _mapW];
    }

    /// <summary>
    /// Get an elevation value for a tile based on terrain type + noise.
    /// Used for relief shading — not stored, computed on the fly.
    /// </summary>
    private float GetElevation(int x, int y)
    {
        int t = GetTerrain(x, y);
        if (t < 0 || t >= ElevRank.Length) t = 0;
        float baseElev = ElevRank[t];
        float noise = SimRng.Hash(_seed, x * 9173 + y * 4111) * 1.2f;
        return baseElev + noise;
    }

    /// <summary>
    /// How far is the nearest land tile from this water tile?
    /// Used for water depth gradient (shallow = bright, deep = dark).
    /// </summary>
    private int CoastDistance(int x, int y, int maxDist)
    {
        for (int d = 1; d <= maxDist; d++)
        {
            for (int dx = -d; dx <= d; dx++)
            {
                for (int dy = -d; dy <= d; dy++)
                {
                    if (Math.Abs(dx) != d && Math.Abs(dy) != d) continue; // only ring
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || nx >= _mapW || ny < 0 || ny >= _mapH) continue;
                    if (GetTerrain(nx, ny) >= (int)Terrain.Sand) return d;
                }
            }
        }
        return maxDist + 1;
    }

    /// <summary>
    /// Blend a tile's color slightly toward neighboring terrain colors
    /// for soft biome transitions (instead of harsh pixel edges).
    /// </summary>
    private Color BlendWithNeighbors(int x, int y, Color baseColor, int myTerrain)
    {
        Color blend = baseColor;
        int count = 1;

        // Check 4 cardinal neighbors
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };
        for (int i = 0; i < 4; i++)
        {
            int nt = GetTerrain(x + dx[i], y + dy[i]);
            if (nt != myTerrain && nt >= (int)Terrain.Sand) // only blend with different land
            {
                Color nc = TerrainInfo.GetColor(nt);
                blend = new Color(
                    blend.R + nc.R * 0.15f,
                    blend.G + nc.G * 0.15f,
                    blend.B + nc.B * 0.15f,
                    1f);
                count++;
            }
        }

        if (count > 1)
        {
            // Normalize the blend (base color is weighted more heavily)
            float total = 1f + (count - 1) * 0.15f;
            blend = new Color(blend.R / total, blend.G / total, blend.B / total, 1f);
        }

        return blend;
    }

    // ════════════════════════════════════════════════════════════════
    //  UTILITY
    // ════════════════════════════════════════════════════════════════

    private static long ChunkKey(int cx, int cy) => (long)cx * 100000 + cy;

    private static void UnpackKey(long key, out int cx, out int cy)
    {
        cx = (int)(key / 100000);
        cy = (int)(key % 100000);
    }

    private static Color Shade(Color c, float mul)
    {
        return new Color(
            Math.Clamp(c.R * mul, 0f, 1f),
            Math.Clamp(c.G * mul, 0f, 1f),
            Math.Clamp(c.B * mul, 0f, 1f),
            c.A);
    }
}
