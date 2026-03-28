using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Warship.Data;

// ═══════════════════════════════════════════════════════════════
//  WORLD DATA — The entire game state
// ═══════════════════════════════════════════════════════════════

public class WorldData
{
    public int Seed;
    public int TurnNumber = 1;
    public int Year => TurnNumber / 12;
    public int Month => (TurnNumber % 12) + 1;
    public string? PlayerNationId;

    public int MapWidth;
    public int MapHeight;
    public int[,]? TerrainMap;          // terrain type per cell
    public int[,]? OwnershipMap;        // nation index per cell, -1 = unclaimed
    public int[,]? CityOwnershipMap;    // city index per cell, -1 = no city control

    public List<NationData> Nations = new();
    public List<CityData> Cities = new();
    public List<Vector2[]> RiverPaths = new();
    public List<ArmyData> Armies = new();
    public List<CharacterData> Characters = new();

    // Legacy compatibility — old code referencing Units still compiles
    [System.Obsolete("Use Armies instead. Units is kept for backward compat.")]
    public List<UnitData> Units = new();

    // Pre-computed border polylines per nation (recomputed on territory change)
    public Dictionary<int, List<Vector2[]>> NationBorderLines = new();
}

// ═══════════════════════════════════════════════════════════════
//  TERRAIN
// ═══════════════════════════════════════════════════════════════

public enum TerrainType
{
    DeepWater = 0,
    Water = 1,
    Sand = 2,
    Grass = 3,
    Forest = 4,
    Hills = 5,
    Mountain = 6,
    Snow = 7
}

public static class TerrainRules
{
    public static bool IsPassable(int terrain) => terrain >= 2 && terrain != 6;
    public static bool IsLand(int terrain) => terrain >= 2;

    /// <summary>Movement cost multiplier for army pathfinding.</summary>
    public static float MovementCost(int terrain) => terrain switch
    {
        (int)TerrainType.DeepWater => 999f,  // Impassable for land
        (int)TerrainType.Water => 999f,
        (int)TerrainType.Sand => 1.5f,
        (int)TerrainType.Grass => 1.0f,
        (int)TerrainType.Forest => 2.0f,
        (int)TerrainType.Hills => 2.5f,
        (int)TerrainType.Mountain => 999f,   // Impassable
        (int)TerrainType.Snow => 3.0f,
        _ => 1.0f
    };

    /// <summary>Naval movement cost.</summary>
    public static float NavalMovementCost(int terrain) => terrain switch
    {
        (int)TerrainType.DeepWater => 1.0f,
        (int)TerrainType.Water => 1.0f,
        _ => 999f  // Can't sail on land
    };
}

// ═══════════════════════════════════════════════════════════════
//  NATIONS
// ═══════════════════════════════════════════════════════════════

public enum NationArchetype
{
    Hegemon, Commercial, Revolutionary, Traditionalist, Survival, FreeState
}

public class NationData
{
    public string Id = "";
    public string Name = "";
    public NationArchetype Archetype;
    public Color NationColor;
    public bool IsPlayer = false;

    // Capital tile position
    public int CapitalX, CapitalY;
    public int ProvinceCount;      // tiles owned

    // Stats
    public float Treasury = 1000f;
    public float Prestige = 30f;
    public bool IsAlive = true;

    // Military Command
    public MilitaryOrder GlobalMilitaryOrder = MilitaryOrder.BorderWatch;
    public int CommandTargetX = -1;
    public int CommandTargetY = -1;
}

// ═══════════════════════════════════════════════════════════════
//  CITIES — Territory anchors. Capture cities to conquer nations.
// ═══════════════════════════════════════════════════════════════

public class CityData
{
    public string Id = "";
    public string NationId = "";
    public string Name = "City";
    public int TileX, TileY;
    public bool IsCapital;
    public int Size = 1;  // 1=town, 2=city, 3=capital

    public int HP = 100;
    public int MaxHP => Size switch { 3 => 400, 2 => 200, _ => 100 };

    /// <summary>Control radius in tiles. Bigger cities own more land.</summary>
    public int ControlRadius => Size switch { 3 => 35, 2 => 25, _ => 15 };

    /// <summary>Index in WorldData.Cities list (set during world gen).</summary>
    public int CityIndex;

    // Siege state
    public string? BesiegingNationId;  // null = not under siege
    public int SiegeTurns;
}

// ═══════════════════════════════════════════════════════════════
//  UNIT TYPES — Everything that fights
// ═══════════════════════════════════════════════════════════════

public enum UnitType
{
    // Land
    Infantry,
    Tank,
    Artillery,
    AntiAir,

    // Air
    Fighter,
    Bomber,
    Transport,

    // Naval
    Destroyer,
    Carrier,
    Submarine,
    LandingCraft,

    // Special
    Missile,
    NuclearMissile
}

public enum UnitDomain { Land, Air, Naval, Special }

public static class UnitRules
{
    public static UnitDomain GetDomain(UnitType type) => type switch
    {
        UnitType.Infantry => UnitDomain.Land,
        UnitType.Tank => UnitDomain.Land,
        UnitType.Artillery => UnitDomain.Land,
        UnitType.AntiAir => UnitDomain.Land,
        UnitType.Fighter => UnitDomain.Air,
        UnitType.Bomber => UnitDomain.Air,
        UnitType.Transport => UnitDomain.Air,
        UnitType.Destroyer => UnitDomain.Naval,
        UnitType.Carrier => UnitDomain.Naval,
        UnitType.Submarine => UnitDomain.Naval,
        UnitType.LandingCraft => UnitDomain.Naval,
        UnitType.Missile => UnitDomain.Special,
        UnitType.NuclearMissile => UnitDomain.Special,
        _ => UnitDomain.Land
    };

    /// <summary>Combat strength per unit of this type.</summary>
    public static float AttackPower(UnitType type) => type switch
    {
        UnitType.Infantry => 1f,
        UnitType.Tank => 5f,
        UnitType.Artillery => 4f,
        UnitType.AntiAir => 2f,
        UnitType.Fighter => 6f,
        UnitType.Bomber => 8f,
        UnitType.Destroyer => 7f,
        UnitType.Carrier => 3f,
        UnitType.Submarine => 6f,
        UnitType.Missile => 15f,
        UnitType.NuclearMissile => 100f,
        _ => 1f
    };

    /// <summary>Defense strength per unit.</summary>
    public static float DefensePower(UnitType type) => type switch
    {
        UnitType.Infantry => 1.5f,
        UnitType.Tank => 4f,
        UnitType.Artillery => 1f,
        UnitType.AntiAir => 3f,
        UnitType.Fighter => 3f,
        UnitType.Carrier => 8f,
        UnitType.Destroyer => 5f,
        UnitType.Submarine => 2f,
        _ => 1f
    };

    /// <summary>Tiles moved per turn.</summary>
    public static int Speed(UnitType type) => type switch
    {
        UnitType.Infantry => 2,
        UnitType.Tank => 4,
        UnitType.Artillery => 1,
        UnitType.Fighter => 8,
        UnitType.Bomber => 6,
        UnitType.Destroyer => 5,
        UnitType.Carrier => 3,
        UnitType.Submarine => 4,
        UnitType.Missile => 12,
        _ => 2
    };
}

// ═══════════════════════════════════════════════════════════════
//  ARMIES — Groups of units that move and fight together.
//  1 pixel dot = 10 troops. An army of 500 = 50 visible dots.
// ═══════════════════════════════════════════════════════════════

public enum MilitaryOrder
{
    Standby,
    BorderWatch,
    Patrol,
    Stage,
    Attack
}

public enum FormationType
{
    Column,    // Moving in line
    Spread,    // Defensive spread
    Wedge,     // Attack formation
    Circle     // Staging/garrison
}

public class ArmyData
{
    public string Id = "";
    public string NationId = "";
    public string Name = "Army";

    // Composition: UnitType → count of that unit
    public Dictionary<UnitType, int> Composition = new();

    /// <summary>Total troop/vehicle/ship count across all types.</summary>
    public int TotalStrength => Composition.Values.Sum();

    /// <summary>Number of swarm dots to render (1 dot per 10 troops).</summary>
    public int SwarmDotCount => System.Math.Max(1, TotalStrength / 10);

    // Position (tile grid)
    public int TileX, TileY;
    public int TargetTileX, TargetTileY;

    // Smooth pixel position for rendering
    public float PixelX, PixelY;
    public float TargetPixelX, TargetPixelY;

    // State
    public MilitaryOrder CurrentOrder = MilitaryOrder.Standby;
    public FormationType Formation = FormationType.Spread;
    public float Morale = 100f;     // 0-100
    public float Supply = 100f;     // 0-100
    public float Organization = 100f; // 0-100, how well-organized (drops in combat)
    public bool IsAlive = true;

    // Garrison assignment (null = field army)
    public string? GarrisonCityId;

    // Pathfinding
    public List<(int x, int y)>? CurrentPath;
    public int PathIndex;

    /// <summary>Primary domain based on majority composition.</summary>
    public UnitDomain PrimaryDomain
    {
        get
        {
            int land = 0, naval = 0, air = 0;
            foreach (var (type, count) in Composition)
            {
                var domain = UnitRules.GetDomain(type);
                if (domain == UnitDomain.Land) land += count;
                else if (domain == UnitDomain.Naval) naval += count;
                else if (domain == UnitDomain.Air) air += count;
            }
            if (naval > land && naval > air) return UnitDomain.Naval;
            if (air > land && air > naval) return UnitDomain.Air;
            return UnitDomain.Land;
        }
    }

    /// <summary>Total attack strength weighted by unit types.</summary>
    public float TotalAttackPower => Composition.Sum(kv => kv.Value * UnitRules.AttackPower(kv.Key));

    /// <summary>Total defense strength weighted by unit types.</summary>
    public float TotalDefensePower => Composition.Sum(kv => kv.Value * UnitRules.DefensePower(kv.Key));

    /// <summary>Movement speed = slowest unit in the army.</summary>
    public int MoveSpeed => Composition.Count == 0 ? 2 :
        Composition.Keys.Min(t => UnitRules.Speed(t));
}

// ═══════════════════════════════════════════════════════════════
//  LEGACY UNIT DATA — Kept for backward compatibility
// ═══════════════════════════════════════════════════════════════

[System.Obsolete("Use ArmyData instead")]
public class UnitData
{
    public string Id = "";
    public string NationId = "";
    public UnitType Type;
    public int TileX, TileY;
    public float Strength = 1.0f;
    public bool IsAlive = true;
    public MilitaryOrder CurrentOrder = MilitaryOrder.Standby;
    public float PixelX, PixelY;
    public float TargetPixelX, TargetPixelY;
    public float Longitude, Latitude;
    public float TargetLongitude, TargetLatitude;
}

// ═══════════════════════════════════════════════════════════════
//  CHARACTERS — VIPs on the map
// ═══════════════════════════════════════════════════════════════

public class CharacterData
{
    public string Id = "";
    public string NationId = "";
    public string Name = "";
    public string Role = "Official";
    public bool IsPlayer = false;

    // Position on map
    public int TileX, TileY;
    public float PixelX, PixelY;
    public float TargetPixelX, TargetPixelY;
    public bool IsMoving = false;

    // Authority Meters (0.0 to 100.0)
    public float TerritoryAuthority = 30f;
    public float WorldAuthority = 20f;
    public float BehindTheScenesAuthority = 40f;

    public float FullAuthorityIndex => (TerritoryAuthority + WorldAuthority + BehindTheScenesAuthority) / 3f;
}
