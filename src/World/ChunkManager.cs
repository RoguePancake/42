using System;
using System.Collections.Generic;
using Warship.Data;

namespace Warship.World;

/// <summary>
/// Manages chunk lifecycle: loading, unloading, caching, and data access.
/// Pure C# — no Godot dependency. The single source of truth for chunk data.
///
/// Chunks are 64x64 tiles. A 6000x3600 world = 94x57 chunks = 5,358 total.
/// Only nearby chunks are loaded (~200-600 depending on zoom).
/// Modified chunks are cached; unmodified chunks are regenerated on demand.
/// </summary>
public class ChunkManager
{
    public int WorldWidthTiles { get; private set; }
    public int WorldHeightTiles { get; private set; }
    public int ChunksX { get; private set; }
    public int ChunksY { get; private set; }

    // Loaded chunks indexed by ChunkCoord
    private readonly Dictionary<ChunkCoord, ChunkData> _loaded = new();

    // Dirty chunks that have been modified (structures, roads, walls placed)
    // These survive unload and get restored when the chunk reloads
    private readonly Dictionary<ChunkCoord, ChunkData> _dirtyCache = new();

    // Reference to the flat terrain/ownership arrays (read-only bridge)
    private int[,]? _terrainMap;
    private int[,]? _ownershipMap;
    private int _seed;

    // Stats
    public int LoadedCount => _loaded.Count;
    public int DirtyCacheCount => _dirtyCache.Count;

    /// <summary>
    /// Initialize from existing WorldData (bridges flat arrays into chunk system).
    /// </summary>
    public void Initialize(WorldData world)
    {
        WorldWidthTiles = world.MapWidth;
        WorldHeightTiles = world.MapHeight;
        ChunksX = (WorldWidthTiles + ChunkData.Size - 1) / ChunkData.Size;
        ChunksY = (WorldHeightTiles + ChunkData.Size - 1) / ChunkData.Size;
        _terrainMap = world.TerrainMap;
        _ownershipMap = world.OwnershipMap;
        _seed = world.Seed;

        _loaded.Clear();
        _dirtyCache.Clear();
    }

    /// <summary>
    /// Update which chunks are loaded based on a focal point and radius.
    /// Call each frame from the rendering layer with camera position.
    /// </summary>
    /// <param name="centerTileX">Camera center in tile coords.</param>
    /// <param name="centerTileY">Camera center in tile coords.</param>
    /// <param name="loadRadius">How many chunks beyond center to load.</param>
    /// <param name="unloadRadius">How many chunks beyond load ring to unload.</param>
    /// <param name="maxLoadsPerUpdate">Throttle to avoid frame spikes.</param>
    /// <returns>List of newly loaded chunk coords (for renderer to bake).</returns>
    public List<ChunkCoord> UpdateLoadedChunks(
        int centerTileX, int centerTileY,
        int loadRadius, int unloadRadius,
        int maxLoadsPerUpdate = 8)
    {
        int centerCX = centerTileX / ChunkData.Size;
        int centerCY = centerTileY / ChunkData.Size;

        var newlyLoaded = new List<ChunkCoord>();
        int loaded = 0;

        // Load chunks within radius
        int minCX = Math.Max(0, centerCX - loadRadius);
        int maxCX = Math.Min(ChunksX - 1, centerCX + loadRadius);
        int minCY = Math.Max(0, centerCY - loadRadius);
        int maxCY = Math.Min(ChunksY - 1, centerCY + loadRadius);

        for (int cx = minCX; cx <= maxCX && loaded < maxLoadsPerUpdate; cx++)
        {
            for (int cy = minCY; cy <= maxCY && loaded < maxLoadsPerUpdate; cy++)
            {
                var coord = new ChunkCoord(cx, cy);
                if (_loaded.ContainsKey(coord)) continue;

                var chunk = LoadChunk(coord);
                _loaded[coord] = chunk;
                newlyLoaded.Add(coord);
                loaded++;
            }
        }

        // Unload chunks beyond unload radius
        int uMinCX = centerCX - unloadRadius;
        int uMaxCX = centerCX + unloadRadius;
        int uMinCY = centerCY - unloadRadius;
        int uMaxCY = centerCY + unloadRadius;

        var toRemove = new List<ChunkCoord>();
        foreach (var (coord, chunk) in _loaded)
        {
            if (coord.X < uMinCX || coord.X > uMaxCX ||
                coord.Y < uMinCY || coord.Y > uMaxCY)
            {
                // Cache if dirty, otherwise discard
                if (chunk.IsDirty)
                    _dirtyCache[coord] = chunk;

                toRemove.Add(coord);
            }
        }

        foreach (var coord in toRemove)
            _loaded.Remove(coord);

        return newlyLoaded;
    }

    /// <summary>Load or restore a chunk.</summary>
    private ChunkData LoadChunk(ChunkCoord coord)
    {
        // Restore from dirty cache if previously modified
        if (_dirtyCache.TryGetValue(coord, out var cached))
        {
            cached.IsLoaded = true;
            _dirtyCache.Remove(coord);
            return cached;
        }

        // Generate fresh from terrain data
        return GenerateChunkFromTerrain(coord);
    }

    /// <summary>
    /// Build a ChunkData by reading from the flat TerrainMap + OwnershipMap arrays.
    /// </summary>
    private ChunkData GenerateChunkFromTerrain(ChunkCoord coord)
    {
        var chunk = new ChunkData
        {
            Coord = coord,
            IsLoaded = true,
            IsDirty = false,
        };

        int originX = coord.X * ChunkData.Size;
        int originY = coord.Y * ChunkData.Size;

        for (int lx = 0; lx < ChunkData.Size; lx++)
        {
            for (int ly = 0; ly < ChunkData.Size; ly++)
            {
                int wx = originX + lx;
                int wy = originY + ly;

                if (wx >= WorldWidthTiles || wy >= WorldHeightTiles)
                {
                    chunk.Tiles[lx + ly * ChunkData.Size] = TileData.Empty;
                    continue;
                }

                byte terrain = _terrainMap != null ? (byte)_terrainMap[wx, wy] : (byte)0;
                byte owner = 255;
                if (_ownershipMap != null)
                {
                    int o = _ownershipMap[wx, wy];
                    owner = o >= 0 && o < 255 ? (byte)o : (byte)255;
                }

                // Derive elevation/moisture/temperature from terrain gen hashes
                byte elev = (byte)(TerrainGenerator.HashFloat(_seed, wx * 9173 + wy * 4111) * 255);
                byte moist = (byte)(TerrainGenerator.HashFloat(_seed + 1, wx * 7019 + wy * 3037) * 255);
                float latFactor = 1f - Math.Abs(wy - WorldHeightTiles / 2f) / (WorldHeightTiles / 2f);
                byte temp = (byte)(Math.Clamp(latFactor, 0f, 1f) * 255);

                chunk.Tiles[lx + ly * ChunkData.Size] = new TileData
                {
                    TerrainType = terrain,
                    Elevation = elev,
                    Moisture = moist,
                    Temperature = temp,
                    OwnerNationIdx = owner,
                    RoadMask = 0,
                    WallMask = 0,
                    StructureId = 0,
                };
            }
        }

        return chunk;
    }

    // ═══════════════════════════════════════════════════════════
    //  DATA ACCESS — Read/write tile data by world coordinates
    // ═══════════════════════════════════════════════════════════

    /// <summary>Get the chunk containing a world tile, or null if not loaded.</summary>
    public ChunkData? GetChunkAt(int worldTileX, int worldTileY)
    {
        var coord = WorldToChunkCoord(worldTileX, worldTileY);
        _loaded.TryGetValue(coord, out var chunk);
        return chunk;
    }

    /// <summary>Get tile data at world coordinates. Returns TileData.Empty if not loaded.</summary>
    public TileData GetTile(int worldX, int worldY)
    {
        var chunk = GetChunkAt(worldX, worldY);
        if (chunk == null) return TileData.Empty;
        var (lx, ly) = ChunkData.WorldToLocal(worldX, worldY);
        return chunk.GetTileSafe(lx, ly);
    }

    /// <summary>Set tile data at world coordinates. Marks chunk dirty.</summary>
    public bool SetTile(int worldX, int worldY, TileData tile)
    {
        var chunk = GetChunkAt(worldX, worldY);
        if (chunk == null) return false;
        var (lx, ly) = ChunkData.WorldToLocal(worldX, worldY);
        chunk.SetTile(lx, ly, tile);
        return true;
    }

    /// <summary>Check if a world tile is within a loaded chunk.</summary>
    public bool IsLoaded(int worldX, int worldY)
        => GetChunkAt(worldX, worldY) != null;

    /// <summary>Get all currently loaded chunks.</summary>
    public IReadOnlyDictionary<ChunkCoord, ChunkData> GetLoadedChunks() => _loaded;

    /// <summary>Force-load a specific chunk (for pathfinding across chunk boundaries).</summary>
    public ChunkData EnsureLoaded(ChunkCoord coord)
    {
        if (_loaded.TryGetValue(coord, out var existing))
            return existing;

        var chunk = LoadChunk(coord);
        _loaded[coord] = chunk;
        return chunk;
    }

    // ═══════════════════════════════════════════════════════════
    //  STRUCTURE / ROAD / WALL PLACEMENT
    // ═══════════════════════════════════════════════════════════

    /// <summary>Place a structure at world coordinates. Returns false if invalid.</summary>
    public bool PlaceStructure(StructureData structure)
    {
        var chunk = GetChunkAt(structure.TileX, structure.TileY);
        if (chunk == null) return false;

        var (lx, ly) = ChunkData.WorldToLocal(structure.TileX, structure.TileY);
        ref var tile = ref chunk.GetTile(lx, ly);

        if (tile.HasStructure) return false; // Already occupied

        chunk.Structures.Add(structure);
        tile.StructureId = (byte)chunk.Structures.Count; // 1-based index
        chunk.IsDirty = true;
        return true;
    }

    /// <summary>Remove a structure at world coordinates.</summary>
    public bool RemoveStructure(int worldX, int worldY)
    {
        var chunk = GetChunkAt(worldX, worldY);
        if (chunk == null) return false;

        var (lx, ly) = ChunkData.WorldToLocal(worldX, worldY);
        ref var tile = ref chunk.GetTile(lx, ly);

        if (!tile.HasStructure) return false;

        int idx = tile.StructureId - 1;
        if (idx >= 0 && idx < chunk.Structures.Count)
            chunk.Structures.RemoveAt(idx);

        // Reindex remaining structures
        for (int i = 0; i < ChunkData.Size * ChunkData.Size; i++)
        {
            ref var t = ref chunk.Tiles[i];
            if (t.StructureId > tile.StructureId)
                t.StructureId--;
        }

        tile.StructureId = 0;
        chunk.IsDirty = true;
        return true;
    }

    /// <summary>Place a road between two adjacent world tiles.</summary>
    public bool PlaceRoad(int fromX, int fromY, int toX, int toY, RoadType type)
    {
        int dx = toX - fromX;
        int dy = toY - fromY;
        if (Math.Abs(dx) > 1 || Math.Abs(dy) > 1) return false; // Must be adjacent

        var dir = DirectionHelper.FromOffset(dx, dy);
        var opposite = DirectionHelper.Opposite(dir);
        if (dir == DirectionMask.None) return false;

        // Set road mask on source tile
        var fromChunk = GetChunkAt(fromX, fromY);
        if (fromChunk == null) return false;
        var (flx, fly) = ChunkData.WorldToLocal(fromX, fromY);
        ref var fromTile = ref fromChunk.GetTile(flx, fly);
        fromTile.RoadMask |= (byte)dir;
        fromChunk.IsDirty = true;

        // Set road mask on destination tile (opposite direction)
        var toChunk = GetChunkAt(toX, toY);
        if (toChunk != null)
        {
            var (tlx, tly) = ChunkData.WorldToLocal(toX, toY);
            ref var toTile = ref toChunk.GetTile(tlx, tly);
            toTile.RoadMask |= (byte)opposite;
            toChunk.IsDirty = true;
        }

        // Store road segment in the source chunk
        fromChunk.Roads.Add(new RoadSegment
        {
            FromX = fromX, FromY = fromY,
            ToX = toX, ToY = toY,
            Type = type
        });

        return true;
    }

    /// <summary>Place a wall on a tile facing specific directions.</summary>
    public bool PlaceWall(int worldX, int worldY, DirectionMask facing, WallType type)
    {
        var chunk = GetChunkAt(worldX, worldY);
        if (chunk == null) return false;

        var (lx, ly) = ChunkData.WorldToLocal(worldX, worldY);
        ref var tile = ref chunk.GetTile(lx, ly);
        tile.WallMask |= (byte)facing;
        chunk.IsDirty = true;

        chunk.Walls.Add(new WallSegment
        {
            TileX = worldX, TileY = worldY,
            Facing = facing,
            Type = type,
            HP = WallSegment.GetMaxHP(type)
        });

        return true;
    }

    // ═══════════════════════════════════════════════════════════
    //  COORDINATE HELPERS
    // ═══════════════════════════════════════════════════════════

    public static ChunkCoord WorldToChunkCoord(int worldTileX, int worldTileY)
        => new(worldTileX / ChunkData.Size, worldTileY / ChunkData.Size);

    public static (int worldX, int worldY) ChunkOrigin(ChunkCoord coord)
        => (coord.X * ChunkData.Size, coord.Y * ChunkData.Size);

    /// <summary>Is this world tile coordinate valid?</summary>
    public bool InBounds(int worldX, int worldY)
        => worldX >= 0 && worldX < WorldWidthTiles && worldY >= 0 && worldY < WorldHeightTiles;

    /// <summary>Get movement cost for a tile, factoring in roads.</summary>
    public float GetMovementCost(int worldX, int worldY)
    {
        if (!InBounds(worldX, worldY)) return 999f;

        var tile = GetTile(worldX, worldY);
        float baseCost = TerrainRules.MovementCost(tile.TerrainType);

        // Roads reduce movement cost
        if (tile.HasRoad)
        {
            // Find the best road type on this tile
            var chunk = GetChunkAt(worldX, worldY);
            if (chunk != null)
            {
                float bestMult = 1.0f;
                foreach (var road in chunk.Roads)
                {
                    if ((road.FromX == worldX && road.FromY == worldY) ||
                        (road.ToX == worldX && road.ToY == worldY))
                    {
                        float m = RoadSegment.MovementMultiplier(road.Type);
                        if (m < bestMult) bestMult = m;
                    }
                }
                baseCost *= bestMult;
            }
        }

        return baseCost;
    }
}
