using Godot;
using System.Collections.Generic;
using Warship.Data;
using Warship.World;

namespace Warship.UI.Map;

/// <summary>
/// Full detail renderer (LOD Micro).
/// Draws when zoomed in close:
///   - Full-resolution terrain textures (32px tiles with sub-block detail)
///   - Structure buildings with pixel art
///   - Road surfaces with connectivity
///   - Wall segments with facing
///   - Detailed unit silhouettes
///
/// Reuses the existing TerrainChunkRenderer's baking logic for terrain,
/// then overlays structures/roads/walls from chunk data.
///
/// Reads from ChunkManager — never creates game data.
/// </summary>
public partial class MicroRenderer : Node2D
{
    private const int TileSize = MapManagerConstants.TileSize;

    // Structure drawing colors
    private static readonly Color WallSandbag = new(0.65f, 0.55f, 0.35f);
    private static readonly Color WallConcrete = new(0.60f, 0.60f, 0.60f);
    private static readonly Color WallFortified = new(0.45f, 0.45f, 0.50f);

    private ChunkManager? _chunks;
    private WorldData? _world;
    private bool _initialized;

    // Baked micro-detail overlays per chunk (structures, roads, walls)
    private readonly Dictionary<ChunkCoord, Sprite2D> _overlayChunks = new();

    public void Initialize(ChunkManager chunks, WorldData world)
    {
        _chunks = chunks;
        _world = world;
        _initialized = true;
        GD.Print("[MicroRenderer] Initialized.");
    }

    /// <summary>Bake a detailed overlay texture for structures/roads/walls in a chunk.</summary>
    public void BakeChunkOverlay(ChunkCoord coord, ChunkData chunk)
    {
        // Skip chunks with nothing to overlay
        if (chunk.Structures.Count == 0 && chunk.Roads.Count == 0 && chunk.Walls.Count == 0)
        {
            UnloadChunkOverlay(coord);
            return;
        }

        if (_overlayChunks.TryGetValue(coord, out var old))
        {
            old.QueueFree();
            _overlayChunks.Remove(coord);
        }

        int imgW = ChunkData.Size * TileSize;
        int imgH = ChunkData.Size * TileSize;
        var img = Image.CreateEmpty(imgW, imgH, false, Image.Format.Rgba8);

        // Draw roads
        foreach (var road in chunk.Roads)
        {
            int localFromX = road.FromX - chunk.WorldOriginX;
            int localFromY = road.FromY - chunk.WorldOriginY;
            int localToX = road.ToX - chunk.WorldOriginX;
            int localToY = road.ToY - chunk.WorldOriginY;

            Color roadColor = RoadColor(road.Type);
            int width = RoadWidth(road.Type);

            // Draw a line between tile centers
            int fx = localFromX * TileSize + TileSize / 2;
            int fy = localFromY * TileSize + TileSize / 2;
            int tx = localToX * TileSize + TileSize / 2;
            int ty = localToY * TileSize + TileSize / 2;

            DrawLineOnImage(img, imgW, imgH, fx, fy, tx, ty, roadColor, width);
        }

        // Draw walls
        foreach (var wall in chunk.Walls)
        {
            int localX = wall.TileX - chunk.WorldOriginX;
            int localY = wall.TileY - chunk.WorldOriginY;
            if (localX < 0 || localX >= ChunkData.Size || localY < 0 || localY >= ChunkData.Size)
                continue;

            Color wallColor = WallColor(wall.Type);
            int px = localX * TileSize;
            int py = localY * TileSize;

            // Draw wall segments on tile edges based on facing
            if ((wall.Facing & DirectionMask.N) != 0)
                FillRect(img, imgW, imgH, px, py, TileSize, 3, wallColor);
            if ((wall.Facing & DirectionMask.S) != 0)
                FillRect(img, imgW, imgH, px, py + TileSize - 3, TileSize, 3, wallColor);
            if ((wall.Facing & DirectionMask.W) != 0)
                FillRect(img, imgW, imgH, px, py, 3, TileSize, wallColor);
            if ((wall.Facing & DirectionMask.E) != 0)
                FillRect(img, imgW, imgH, px + TileSize - 3, py, 3, TileSize, wallColor);
        }

        // Draw structures
        foreach (var structure in chunk.Structures)
        {
            int localX = structure.TileX - chunk.WorldOriginX;
            int localY = structure.TileY - chunk.WorldOriginY;
            if (localX < 0 || localX >= ChunkData.Size || localY < 0 || localY >= ChunkData.Size)
                continue;

            DrawStructure(img, imgW, imgH, localX, localY, structure);
        }

        var texture = ImageTexture.CreateFromImage(img);
        var sprite = new Sprite2D
        {
            Texture = texture,
            Centered = false,
            Position = new Vector2(coord.X * ChunkData.Size * TileSize, coord.Y * ChunkData.Size * TileSize),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        };

        AddChild(sprite);
        _overlayChunks[coord] = sprite;
    }

    /// <summary>Remove overlay for an unloaded chunk.</summary>
    public void UnloadChunkOverlay(ChunkCoord coord)
    {
        if (_overlayChunks.TryGetValue(coord, out var sprite))
        {
            sprite.QueueFree();
            _overlayChunks.Remove(coord);
        }
    }

    /// <summary>Rebuild overlay for a specific chunk (after build/demolish).</summary>
    public void InvalidateChunk(ChunkCoord coord)
    {
        if (_chunks == null) return;
        var loadedChunks = _chunks.GetLoadedChunks();
        if (loadedChunks.TryGetValue(coord, out var chunk))
            BakeChunkOverlay(coord, chunk);
    }

    public void ClearAll()
    {
        foreach (var (_, sprite) in _overlayChunks)
            sprite.QueueFree();
        _overlayChunks.Clear();
    }

    // ═══ Structure pixel art drawing ═══

    private void DrawStructure(Image img, int imgW, int imgH,
        int localTileX, int localTileY, StructureData structure)
    {
        int px = localTileX * TileSize;
        int py = localTileY * TileSize;
        int margin = 4;

        // Base footprint
        Color baseColor = StructureBaseColor(structure.Type);
        Color roofColor = baseColor.Lightened(0.2f);
        Color shadowColor = new(0, 0, 0, 0.3f);

        // Shadow
        FillRect(img, imgW, imgH, px + margin + 2, py + margin + 2,
            TileSize - margin * 2, TileSize - margin * 2, shadowColor);

        // Building body
        FillRect(img, imgW, imgH, px + margin, py + margin,
            TileSize - margin * 2, TileSize - margin * 2, baseColor);

        // Roof/top detail
        FillRect(img, imgW, imgH, px + margin + 2, py + margin + 2,
            TileSize - margin * 2 - 4, TileSize - margin * 2 - 4, roofColor);

        // Type-specific details
        switch (structure.Type)
        {
            case StructureType.Watchtower:
                // Tall narrow tower
                int cx = px + TileSize / 2;
                int cy = py + TileSize / 2;
                FillRect(img, imgW, imgH, cx - 3, cy - 8, 6, 16, baseColor.Darkened(0.2f));
                SetPixelSafe(img, imgW, imgH, cx, cy - 9, Colors.Yellow);
                break;

            case StructureType.Fortress:
                // Corner turrets
                FillRect(img, imgW, imgH, px + 2, py + 2, 6, 6, baseColor.Darkened(0.3f));
                FillRect(img, imgW, imgH, px + TileSize - 8, py + 2, 6, 6, baseColor.Darkened(0.3f));
                FillRect(img, imgW, imgH, px + 2, py + TileSize - 8, 6, 6, baseColor.Darkened(0.3f));
                FillRect(img, imgW, imgH, px + TileSize - 8, py + TileSize - 8, 6, 6, baseColor.Darkened(0.3f));
                break;

            case StructureType.Factory:
                // Smokestacks
                FillRect(img, imgW, imgH, px + 8, py + 2, 3, 8, new Color(0.3f, 0.3f, 0.3f));
                FillRect(img, imgW, imgH, px + TileSize - 11, py + 2, 3, 8, new Color(0.3f, 0.3f, 0.3f));
                break;

            case StructureType.Farm:
                // Field lines
                for (int i = 0; i < 3; i++)
                {
                    int fy = py + margin + 4 + i * 6;
                    FillRect(img, imgW, imgH, px + margin + 2, fy,
                        TileSize - margin * 2 - 4, 1, new Color(0.35f, 0.55f, 0.15f));
                }
                break;
        }
    }

    private static Color StructureBaseColor(StructureType type) => type switch
    {
        StructureType.Barracks => new Color(0.50f, 0.35f, 0.25f),
        StructureType.Watchtower => new Color(0.55f, 0.50f, 0.45f),
        StructureType.Fortress => new Color(0.45f, 0.42f, 0.40f),
        StructureType.AirBase => new Color(0.50f, 0.50f, 0.55f),
        StructureType.NavalBase => new Color(0.30f, 0.40f, 0.55f),
        StructureType.MissileSilo => new Color(0.40f, 0.40f, 0.42f),
        StructureType.Bunker => new Color(0.38f, 0.38f, 0.36f),
        StructureType.SupplyDepot => new Color(0.50f, 0.45f, 0.30f),
        StructureType.Farm => new Color(0.45f, 0.60f, 0.25f),
        StructureType.Mine => new Color(0.45f, 0.40f, 0.35f),
        StructureType.OilWell => new Color(0.25f, 0.25f, 0.25f),
        StructureType.Factory => new Color(0.50f, 0.45f, 0.40f),
        StructureType.PowerPlant => new Color(0.55f, 0.55f, 0.30f),
        StructureType.TradePost => new Color(0.60f, 0.50f, 0.25f),
        StructureType.Port => new Color(0.35f, 0.45f, 0.60f),
        StructureType.Market => new Color(0.60f, 0.45f, 0.20f),
        StructureType.Settlement => new Color(0.55f, 0.50f, 0.40f),
        StructureType.Hospital => new Color(0.85f, 0.85f, 0.85f),
        StructureType.RadioTower => new Color(0.50f, 0.50f, 0.55f),
        StructureType.ResearchLab => new Color(0.40f, 0.50f, 0.60f),
        _ => new Color(0.50f, 0.50f, 0.50f),
    };

    private static Color RoadColor(RoadType type) => type switch
    {
        RoadType.Dirt => new Color(0.55f, 0.45f, 0.30f, 0.8f),
        RoadType.Paved => new Color(0.40f, 0.40f, 0.40f, 0.9f),
        RoadType.Highway => new Color(0.65f, 0.65f, 0.65f, 0.9f),
        RoadType.Rail => new Color(0.30f, 0.30f, 0.50f, 0.9f),
        _ => new Color(0.50f, 0.50f, 0.50f, 0.8f),
    };

    private static int RoadWidth(RoadType type) => type switch
    {
        RoadType.Dirt => 2,
        RoadType.Paved => 3,
        RoadType.Highway => 5,
        RoadType.Rail => 2,
        _ => 2,
    };

    private static Color WallColor(WallType type) => type switch
    {
        WallType.Sandbag => new Color(0.65f, 0.55f, 0.35f, 0.9f),
        WallType.Concrete => new Color(0.60f, 0.60f, 0.60f, 0.9f),
        WallType.Fortified => new Color(0.45f, 0.45f, 0.50f, 0.9f),
        _ => new Color(0.50f, 0.50f, 0.50f, 0.8f),
    };

    // ═══ Image drawing helpers ═══

    private static void FillRect(Image img, int imgW, int imgH,
        int x, int y, int w, int h, Color color)
    {
        for (int px = x; px < x + w; px++)
            for (int py = y; py < y + h; py++)
                SetPixelSafe(img, imgW, imgH, px, py, color);
    }

    private static void DrawLineOnImage(Image img, int imgW, int imgH,
        int x0, int y0, int x1, int y1, Color color, int width)
    {
        // Bresenham's line with width
        int dx = System.Math.Abs(x1 - x0);
        int dy = System.Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        int hw = width / 2;

        while (true)
        {
            for (int wx = -hw; wx <= hw; wx++)
                for (int wy = -hw; wy <= hw; wy++)
                    SetPixelSafe(img, imgW, imgH, x0 + wx, y0 + wy, color);

            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    private static void SetPixelSafe(Image img, int imgW, int imgH, int x, int y, Color c)
    {
        if (x >= 0 && x < imgW && y >= 0 && y < imgH)
        {
            if (c.A < 1.0f)
            {
                // Alpha blend
                var existing = img.GetPixel(x, y);
                var blended = existing.Lerp(c, c.A);
                img.SetPixel(x, y, new Color(blended.R, blended.G, blended.B, System.Math.Max(existing.A, c.A)));
            }
            else
            {
                img.SetPixel(x, y, c);
            }
        }
    }
}
