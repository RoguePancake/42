using Godot;
using System.Collections.Generic;
using Warship.Data;
using Warship.World;

namespace Warship.UI.Map;

/// <summary>
/// Strategic overview renderer (LOD Macro + Hybrid).
/// Draws simplified representations visible when zoomed out:
///   - Color-coded terrain blocks (1 pixel per tile at macro, 4x4 at hybrid)
///   - Nation territory overlays
///   - Icons for cities and structures
///   - Lines for roads
///   - Dot markers for armies
///
/// Reads from ChunkManager — never creates game data.
/// Chunk-based: only renders loaded chunks within viewport.
/// </summary>
public partial class MacroRenderer : Node2D
{
    private const int TileSize = MapManagerConstants.TileSize;

    // Terrain colors (same palette as TerrainChunkRenderer for consistency)
    private static readonly Color[] TerrainColors =
    {
        new(0.12f, 0.18f, 0.38f),  // 0 DeepWater
        new(0.20f, 0.28f, 0.72f),  // 1 Water
        new(0.80f, 0.76f, 0.50f),  // 2 Sand
        new(0.48f, 0.66f, 0.24f),  // 3 Grass
        new(0.08f, 0.40f, 0.10f),  // 4 Forest
        new(0.42f, 0.40f, 0.36f),  // 5 Hills
        new(0.50f, 0.46f, 0.42f),  // 6 Mountain
        new(0.92f, 0.94f, 0.96f),  // 7 Snow
    };

    // Road colors by type
    private static readonly Color RoadDirt = new(0.55f, 0.45f, 0.30f);
    private static readonly Color RoadPaved = new(0.40f, 0.40f, 0.40f);
    private static readonly Color RoadHighway = new(0.65f, 0.65f, 0.65f);
    private static readonly Color RoadRail = new(0.30f, 0.30f, 0.50f);

    private ChunkManager? _chunks;
    private WorldData? _world;
    private bool _initialized;

    // Baked macro chunk textures (key = ChunkCoord hash)
    private readonly Dictionary<ChunkCoord, Sprite2D> _bakedChunks = new();

    public void Initialize(ChunkManager chunks, WorldData world)
    {
        _chunks = chunks;
        _world = world;
        _initialized = true;
        GD.Print("[MacroRenderer] Initialized.");
    }

    /// <summary>Bake a macro-resolution texture for a chunk.</summary>
    public void BakeChunk(ChunkCoord coord, ChunkData chunk)
    {
        // Remove old sprite if exists
        if (_bakedChunks.TryGetValue(coord, out var old))
        {
            old.QueueFree();
            _bakedChunks.Remove(coord);
        }

        int chunkPx = ChunkData.Size * TileSize;
        // Macro: render at 1/4 resolution (8px per tile instead of 32)
        int scale = 4;
        int imgSize = ChunkData.Size * (TileSize / scale);
        var img = Image.CreateEmpty(imgSize, imgSize, false, Image.Format.Rgba8);

        for (int lx = 0; lx < ChunkData.Size; lx++)
        {
            for (int ly = 0; ly < ChunkData.Size; ly++)
            {
                int wx = chunk.WorldOriginX + lx;
                int wy = chunk.WorldOriginY + ly;
                if (wx >= (_world?.MapWidth ?? 0) || wy >= (_world?.MapHeight ?? 0))
                    continue;

                var tile = chunk.GetTileSafe(lx, ly);
                Color baseColor = tile.TerrainType < TerrainColors.Length
                    ? TerrainColors[tile.TerrainType]
                    : TerrainColors[0];

                // Nation tint
                if (tile.IsOwned && _world != null && tile.OwnerNationIdx < _world.Nations.Count)
                {
                    var nation = _world.Nations[tile.OwnerNationIdx];
                    baseColor = baseColor.Lerp(nation.NationColor, 0.15f);
                }

                // Fill the scaled pixel block
                int px = lx * (TileSize / scale);
                int py = ly * (TileSize / scale);
                int blockSize = TileSize / scale;
                for (int bx = 0; bx < blockSize; bx++)
                    for (int by = 0; by < blockSize; by++)
                        img.SetPixel(px + bx, py + by, baseColor);

                // Road overlay (draw a cross pattern for tiles with roads)
                if (tile.HasRoad)
                {
                    Color roadColor = RoadPaved; // Default
                    int cx = px + blockSize / 2;
                    int cy = py + blockSize / 2;
                    int roadW = System.Math.Max(1, blockSize / 4);

                    for (int r = -roadW; r <= roadW; r++)
                    {
                        // Draw road connections based on mask
                        byte mask = tile.RoadMask;
                        if ((mask & (byte)DirectionMask.N) != 0)
                            for (int d = 0; d <= blockSize / 2; d++)
                                SetPixelSafe(img, cx + r, cy - d, imgSize, roadColor);
                        if ((mask & (byte)DirectionMask.S) != 0)
                            for (int d = 0; d <= blockSize / 2; d++)
                                SetPixelSafe(img, cx + r, cy + d, imgSize, roadColor);
                        if ((mask & (byte)DirectionMask.E) != 0)
                            for (int d = 0; d <= blockSize / 2; d++)
                                SetPixelSafe(img, cx + d, cy + r, imgSize, roadColor);
                        if ((mask & (byte)DirectionMask.W) != 0)
                            for (int d = 0; d <= blockSize / 2; d++)
                                SetPixelSafe(img, cx - d, cy + r, imgSize, roadColor);
                    }
                }
            }
        }

        var texture = ImageTexture.CreateFromImage(img);
        var sprite = new Sprite2D
        {
            Texture = texture,
            Centered = false,
            Position = new Vector2(coord.X * chunkPx, coord.Y * chunkPx),
            Scale = new Vector2(scale, scale), // Scale up to match world coords
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        };

        AddChild(sprite);
        _bakedChunks[coord] = sprite;
    }

    /// <summary>Remove a baked chunk sprite when it gets unloaded.</summary>
    public void UnloadChunk(ChunkCoord coord)
    {
        if (_bakedChunks.TryGetValue(coord, out var sprite))
        {
            sprite.QueueFree();
            _bakedChunks.Remove(coord);
        }
    }

    /// <summary>Draw dynamic overlays: city icons, structure markers, army dots.</summary>
    public override void _Draw()
    {
        if (!_initialized || _world == null) return;

        var viewRect = GetViewRect();

        // City icons (simplified at macro level)
        foreach (var city in _world.Cities)
        {
            var center = new Vector2(
                city.TileX * TileSize + TileSize / 2f,
                city.TileY * TileSize + TileSize / 2f);

            if (!viewRect.HasPoint(center)) continue;

            int nIdx = MapManager.GetNationIndex(_world, city.NationId);
            Color natColor = nIdx >= 0 ? _world.Nations[nIdx].NationColor : Colors.Gray;

            if (city.IsCapital)
            {
                // Star marker for capitals
                DrawCircle(center, 10, Colors.Black.Lerp(Colors.Transparent, 0.4f));
                DrawCircle(center, 8, natColor);
                DrawCircle(center, 3, Colors.Gold);
            }
            else
            {
                float size = city.Size >= 2 ? 5f : 3f;
                DrawCircle(center, size + 1, Colors.Black.Lerp(Colors.Transparent, 0.5f));
                DrawCircle(center, size, natColor);
            }
        }

        // Structure icons at hybrid zoom
        var camera = GetViewport().GetCamera2D();
        float zoom = camera?.Zoom.X ?? 0.01f;

        if (zoom > ZoomController.MacroToHybrid && _chunks != null)
        {
            foreach (var (_, chunk) in _chunks.GetLoadedChunks())
            {
                foreach (var structure in chunk.Structures)
                {
                    var pos = new Vector2(
                        structure.TileX * TileSize + TileSize / 2f,
                        structure.TileY * TileSize + TileSize / 2f);

                    if (!viewRect.HasPoint(pos)) continue;

                    Color sColor = StructureIconColor(structure.Type);
                    DrawRect(new Rect2(pos.X - 4, pos.Y - 4, 8, 8), sColor);
                    DrawRect(new Rect2(pos.X - 3, pos.Y - 3, 6, 6), sColor.Lightened(0.3f));
                }
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_initialized)
            QueueRedraw(); // Dynamic overlays update each frame
    }

    public void ClearAll()
    {
        foreach (var (_, sprite) in _bakedChunks)
            sprite.QueueFree();
        _bakedChunks.Clear();
    }

    private static Color StructureIconColor(StructureType type) => type switch
    {
        StructureType.Barracks or StructureType.Fortress or StructureType.Bunker
            => new Color(0.8f, 0.2f, 0.2f),
        StructureType.Factory or StructureType.Mine or StructureType.OilWell
            => new Color(0.6f, 0.5f, 0.2f),
        StructureType.Farm or StructureType.Settlement
            => new Color(0.3f, 0.7f, 0.3f),
        StructureType.Port or StructureType.NavalBase
            => new Color(0.2f, 0.4f, 0.8f),
        _ => new Color(0.5f, 0.5f, 0.5f),
    };

    private static void SetPixelSafe(Image img, int x, int y, int size, Color c)
    {
        if (x >= 0 && x < size && y >= 0 && y < size)
            img.SetPixel(x, y, c);
    }

    private Rect2 GetViewRect()
    {
        var camera = GetViewport().GetCamera2D();
        if (camera != null)
        {
            var vpSize = GetViewportRect().Size;
            var camPos = camera.GlobalPosition;
            float halfW = vpSize.X / (2f * camera.Zoom.X);
            float halfH = vpSize.Y / (2f * camera.Zoom.Y);
            return new Rect2(camPos.X - halfW, camPos.Y - halfH, halfW * 2, halfH * 2);
        }
        return new Rect2(0, 0, 192000, 115200);
    }
}
