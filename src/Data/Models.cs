using System;
using System.Collections.Generic;
using Godot;

namespace Warship.Data;

// ═══════════════════════════════════════════════════════════════
//  WORLD — The top-level container for ALL game state
// ═══════════════════════════════════════════════════════════════

public class WorldData
{
    public int Seed;
    public int TickNumber;

    // Map dimensions (in tiles)
    public int MapWidth;
    public int MapHeight;

    // Terrain grid: terrain type per tile. Index as [x + y * MapWidth].
    public int[] TerrainMap = Array.Empty<int>();

    // Entities
    public PlayerData Player = new();
    public List<BuildingData> Buildings = new();
    public List<TroopSquadData> Squads = new();
}

// ═══════════════════════════════════════════════════════════════
//  TERRAIN
// ═══════════════════════════════════════════════════════════════

public enum Terrain
{
    DeepWater = 0,
    Water = 1,
    Sand = 2,
    Grass = 3,
    Forest = 4,
    Hills = 5,
    Mountain = 6,
    Snow = 7,
}

public static class TerrainInfo
{
    public static bool IsLand(int t) => t >= (int)Terrain.Sand;
    public static bool IsPassable(int t) => t >= (int)Terrain.Sand && t != (int)Terrain.Mountain;
    public static bool IsBuildable(int t) => t == (int)Terrain.Grass || t == (int)Terrain.Sand || t == (int)Terrain.Hills;

    public static float MoveCost(int t) => t switch
    {
        (int)Terrain.Sand => 1.5f,
        (int)Terrain.Grass => 1.0f,
        (int)Terrain.Forest => 2.0f,
        (int)Terrain.Hills => 2.5f,
        (int)Terrain.Snow => 3.0f,
        _ => 999f,
    };

    public static Color GetColor(int t) => t switch
    {
        (int)Terrain.DeepWater => new Color(0.12f, 0.18f, 0.40f),
        (int)Terrain.Water => new Color(0.20f, 0.35f, 0.75f),
        (int)Terrain.Sand => new Color(0.86f, 0.82f, 0.55f),
        (int)Terrain.Grass => new Color(0.40f, 0.68f, 0.25f),
        (int)Terrain.Forest => new Color(0.10f, 0.42f, 0.12f),
        (int)Terrain.Hills => new Color(0.50f, 0.47f, 0.40f),
        (int)Terrain.Mountain => new Color(0.55f, 0.52f, 0.48f),
        (int)Terrain.Snow => new Color(0.94f, 0.95f, 0.96f),
        _ => Colors.Magenta,
    };
}

// ═══════════════════════════════════════════════════════════════
//  PLAYER CHARACTER
// ═══════════════════════════════════════════════════════════════

public class PlayerData
{
    public string Name = "Commander";
    public int TileX, TileY;       // Position on the tile grid
    public int Gold = 1000;         // Starting currency
    public int Food = 500;
}

// ═══════════════════════════════════════════════════════════════
//  BUILDINGS — Placed on the map by the player
// ═══════════════════════════════════════════════════════════════

public enum BuildingType
{
    TroopCamp,      // Spawns and houses 100-500 troops
    BorderWall,     // Defensive wall segment (1 tile)
    Road,           // Speeds up movement
    Watchtower,     // Reveals fog in a radius
    Storehouse,     // Stores resources
}

public class BuildingData
{
    public int Id;
    public BuildingType Type;
    public int TileX, TileY;
    public int Health = 100;        // 0 = destroyed

    // TroopCamp specific
    public int GarrisonCap = 200;   // Max troops housed
    public int GarrisonCount;       // Current troops inside
}

public static class BuildingInfo
{
    public static int GoldCost(BuildingType t) => t switch
    {
        BuildingType.TroopCamp => 200,
        BuildingType.BorderWall => 50,
        BuildingType.Road => 20,
        BuildingType.Watchtower => 100,
        BuildingType.Storehouse => 150,
        _ => 0,
    };

    public static Color GetColor(BuildingType t) => t switch
    {
        BuildingType.TroopCamp => new Color(0.8f, 0.6f, 0.2f),
        BuildingType.BorderWall => new Color(0.5f, 0.5f, 0.5f),
        BuildingType.Road => new Color(0.6f, 0.55f, 0.4f),
        BuildingType.Watchtower => new Color(0.3f, 0.5f, 0.8f),
        BuildingType.Storehouse => new Color(0.6f, 0.4f, 0.2f),
        _ => Colors.White,
    };

    public static string DisplayName(BuildingType t) => t switch
    {
        BuildingType.TroopCamp => "Troop Camp",
        BuildingType.BorderWall => "Border Wall",
        BuildingType.Road => "Road",
        BuildingType.Watchtower => "Watchtower",
        BuildingType.Storehouse => "Storehouse",
        _ => "Unknown",
    };
}

// ═══════════════════════════════════════════════════════════════
//  TROOP SQUADS — Groups of soldiers with orders
// ═══════════════════════════════════════════════════════════════

public enum SquadOrder
{
    Idle,           // Standing at position
    Patrol,         // Moving between two waypoints
    MoveTo,         // Moving to a target tile
    Garrison,       // Inside a building
}

public class TroopSquadData
{
    public int Id;
    public string Name = "Squad";
    public int Count = 100;         // Number of troops (100-500)
    public int MaxCount = 500;

    // Position (tile coords)
    public int TileX, TileY;

    // Smooth pixel position for rendering
    public float PixelX, PixelY;
    public float TargetPixelX, TargetPixelY;

    // Orders
    public SquadOrder Order = SquadOrder.Idle;
    public int TargetTileX, TargetTileY;

    // Patrol: two waypoints, ping-pong between them
    public int PatrolAX, PatrolAY;
    public int PatrolBX, PatrolBY;
    public bool PatrolGoingToB = true;

    // Stats
    public float Morale = 100f;
    public float MoveSpeed = 2f;    // Tiles per second (at 1x speed)
    public bool IsAlive = true;
}
