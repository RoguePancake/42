using System;
using System.Collections.Generic;

namespace Warship.Data;

// ═══════════════════════════════════════════════════════════════
//  CHUNK COORDINATE — Identifies a chunk in the world grid
// ═══════════════════════════════════════════════════════════════

public readonly struct ChunkCoord : IEquatable<ChunkCoord>
{
    public readonly int X;
    public readonly int Y;

    public ChunkCoord(int x, int y) { X = x; Y = y; }

    public bool Equals(ChunkCoord other) => X == other.X && Y == other.Y;
    public override bool Equals(object? obj) => obj is ChunkCoord c && Equals(c);
    public override int GetHashCode() => X * 73856093 ^ Y * 19349663;
    public override string ToString() => $"({X},{Y})";

    public static bool operator ==(ChunkCoord a, ChunkCoord b) => a.Equals(b);
    public static bool operator !=(ChunkCoord a, ChunkCoord b) => !a.Equals(b);
}

// ═══════════════════════════════════════════════════════════════
//  CHUNK DATA — A rectangular region of the world
//  Contains terrain tiles, structures, roads, walls, unit refs.
//  This is the source of truth — renderers READ from chunks.
// ═══════════════════════════════════════════════════════════════

public class ChunkData
{
    public const int Size = 64; // 64x64 tiles per chunk

    public ChunkCoord Coord;
    public bool IsDirty;       // Modified since last save
    public bool IsLoaded;

    // Tile grid — flat array [x + y * Size] for cache locality
    public TileData[] Tiles = new TileData[Size * Size];

    // Placed objects within this chunk
    public List<StructureData> Structures = new();
    public List<RoadSegment> Roads = new();
    public List<WallSegment> Walls = new();

    // Army IDs present in this chunk (updated each tick)
    public List<string> ArmyIds = new();

    // ── Accessors ──

    public ref TileData GetTile(int localX, int localY)
        => ref Tiles[localX + localY * Size];

    public TileData GetTileSafe(int localX, int localY)
    {
        if (localX < 0 || localX >= Size || localY < 0 || localY >= Size)
            return default;
        return Tiles[localX + localY * Size];
    }

    public void SetTile(int localX, int localY, TileData tile)
    {
        Tiles[localX + localY * Size] = tile;
        IsDirty = true;
    }

    /// <summary>World-space tile origin of this chunk.</summary>
    public int WorldOriginX => Coord.X * Size;
    public int WorldOriginY => Coord.Y * Size;

    /// <summary>Convert world tile coord to local chunk coord.</summary>
    public static (int localX, int localY) WorldToLocal(int worldX, int worldY)
        => (worldX & (Size - 1), worldY & (Size - 1)); // Works because Size is power of 2
}

// ═══════════════════════════════════════════════════════════════
//  TILE DATA — Per-tile information (16 bytes, cache-friendly)
// ═══════════════════════════════════════════════════════════════

public struct TileData
{
    public byte TerrainType;    // Maps to TerrainType enum (0-7)
    public byte Elevation;      // 0-255 height value for fine-grained relief
    public byte Moisture;       // 0-255 moisture value
    public byte Temperature;    // 0-255 temperature value

    public byte OwnerNationIdx; // Nation index (255 = unclaimed)
    public byte RoadMask;       // Bitmask: N=1, E=2, S=4, W=8, NE=16, SE=32, SW=64, NW=128
    public byte WallMask;       // Same bitmask layout for walls
    public byte StructureId;    // Index into chunk's Structures list (0 = none, 1-based)

    public bool HasRoad => RoadMask != 0;
    public bool HasWall => WallMask != 0;
    public bool HasStructure => StructureId != 0;
    public bool IsOwned => OwnerNationIdx != 255;

    public static readonly TileData Empty = new()
    {
        TerrainType = 0,
        Elevation = 0,
        Moisture = 0,
        Temperature = 0,
        OwnerNationIdx = 255,
        RoadMask = 0,
        WallMask = 0,
        StructureId = 0,
    };
}

// ═══════════════════════════════════════════════════════════════
//  DIRECTION MASKS — For road/wall connectivity
// ═══════════════════════════════════════════════════════════════

[Flags]
public enum DirectionMask : byte
{
    None = 0,
    N    = 1,
    E    = 2,
    S    = 4,
    W    = 8,
    NE   = 16,
    SE   = 32,
    SW   = 64,
    NW   = 128,
    Cardinals = N | E | S | W,
    All  = 255,
}

public static class DirectionHelper
{
    // Offsets for each direction bit (indexed by bit position 0-7)
    public static readonly (int dx, int dy)[] Offsets =
    {
        (0, -1),  // N
        (1, 0),   // E
        (0, 1),   // S
        (-1, 0),  // W
        (1, -1),  // NE
        (1, 1),   // SE
        (-1, 1),  // SW
        (-1, -1), // NW
    };

    public static DirectionMask Opposite(DirectionMask dir) => dir switch
    {
        DirectionMask.N  => DirectionMask.S,
        DirectionMask.E  => DirectionMask.W,
        DirectionMask.S  => DirectionMask.N,
        DirectionMask.W  => DirectionMask.E,
        DirectionMask.NE => DirectionMask.SW,
        DirectionMask.SE => DirectionMask.NW,
        DirectionMask.SW => DirectionMask.NE,
        DirectionMask.NW => DirectionMask.SE,
        _ => DirectionMask.None,
    };

    public static DirectionMask FromOffset(int dx, int dy) => (dx, dy) switch
    {
        (0, -1)  => DirectionMask.N,
        (1, 0)   => DirectionMask.E,
        (0, 1)   => DirectionMask.S,
        (-1, 0)  => DirectionMask.W,
        (1, -1)  => DirectionMask.NE,
        (1, 1)   => DirectionMask.SE,
        (-1, 1)  => DirectionMask.SW,
        (-1, -1) => DirectionMask.NW,
        _ => DirectionMask.None,
    };
}

// ═══════════════════════════════════════════════════════════════
//  STRUCTURES — Buildings placed on the map
// ═══════════════════════════════════════════════════════════════

public enum StructureType
{
    // Military
    Barracks,
    Watchtower,
    Fortress,
    AirBase,
    NavalBase,
    MissileSilo,
    Bunker,
    SupplyDepot,

    // Economic
    Farm,
    Mine,
    OilWell,
    Factory,
    PowerPlant,
    TradePost,
    Port,
    Market,

    // Civilian
    Settlement,
    Hospital,
    RadioTower,
    ResearchLab,

    // Defensive
    MineField,
    ArtilleryEmplacement,
    AntiAirBattery,
    CoastalGun,
}

public class StructureData
{
    public string Id = "";
    public StructureType Type;
    public int TileX, TileY;          // World tile coords
    public string OwnerNationId = "";
    public int HP = 100;
    public int MaxHP = 100;
    public bool IsOperational = true;

    /// <summary>Footprint in tiles (most are 1x1, some larger).</summary>
    public int Width = 1;
    public int Height = 1;

    public static int GetMaxHP(StructureType type) => type switch
    {
        StructureType.Fortress => 500,
        StructureType.Bunker => 300,
        StructureType.NavalBase => 400,
        StructureType.AirBase => 350,
        StructureType.MissileSilo => 250,
        _ => 100
    };

    public static bool RequiresCoastal(StructureType type) => type switch
    {
        StructureType.NavalBase => true,
        StructureType.Port => true,
        StructureType.CoastalGun => true,
        _ => false
    };

    public static bool RequiresLand(StructureType type) => type switch
    {
        StructureType.NavalBase => false,
        StructureType.Port => false,
        _ => true
    };
}

// ═══════════════════════════════════════════════════════════════
//  ROADS — Connections between tiles, affect pathfinding cost
// ═══════════════════════════════════════════════════════════════

public enum RoadType
{
    Dirt,       // 0.7x movement cost
    Paved,      // 0.5x movement cost
    Highway,    // 0.3x movement cost
    Rail,       // 0.2x movement cost (units only, not combat)
}

public class RoadSegment
{
    public int FromX, FromY;   // World tile
    public int ToX, ToY;       // World tile (adjacent)
    public RoadType Type;

    public static float MovementMultiplier(RoadType type) => type switch
    {
        RoadType.Dirt => 0.7f,
        RoadType.Paved => 0.5f,
        RoadType.Highway => 0.3f,
        RoadType.Rail => 0.2f,
        _ => 1.0f,
    };
}

// ═══════════════════════════════════════════════════════════════
//  WALLS — Defensive barriers between tiles
// ═══════════════════════════════════════════════════════════════

public enum WallType
{
    Sandbag,    // Light cover, fast to build
    Concrete,   // Medium defense, blocks vehicles
    Fortified,  // Heavy defense, requires explosives to breach
}

public class WallSegment
{
    public int TileX, TileY;       // World tile the wall is on
    public DirectionMask Facing;   // Which edges have walls
    public WallType Type;
    public int HP = 100;

    public static int GetMaxHP(WallType type) => type switch
    {
        WallType.Sandbag => 50,
        WallType.Concrete => 200,
        WallType.Fortified => 500,
        _ => 100,
    };

    public static float DefenseMultiplier(WallType type) => type switch
    {
        WallType.Sandbag => 1.3f,
        WallType.Concrete => 1.8f,
        WallType.Fortified => 2.5f,
        _ => 1.0f,
    };
}

// ═══════════════════════════════════════════════════════════════
//  LOD LEVEL — Zoom level of detail state
// ═══════════════════════════════════════════════════════════════

public enum LodLevel
{
    Macro = 0,    // Strategic overview — color terrain, icons
    Hybrid = 1,   // Transitional — simplified tiles + icons
    Micro = 2,    // Full detail — textures, buildings, units
}
