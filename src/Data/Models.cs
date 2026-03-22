using System.Collections.Generic;
using Godot;

namespace Warship.Data;

public class WorldData
{
    public int Seed;
    public int TurnNumber = 1;
    public int Year => TurnNumber / 12;
    public int Month => (TurnNumber % 12) + 1;
    public string? PlayerNationId;

    public int MapWidth;
    public int MapHeight;
    public int[,]? TerrainMap;    // tile index per cell
    public int[,]? OwnershipMap;  // nation index per cell, -1 = unclaimed
    
    public List<NationData> Nations = new();
    public List<CityData> Cities = new();
    public List<Vector2[]> RiverPaths = new();
    public List<UnitData> Units = new();
    public List<CharacterData> Characters = new();
}

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
    // Can units walk on this terrain?
    public static bool IsPassable(int terrain) => terrain >= 2 && terrain != 6;
    
    // Is this land (not water)?
    public static bool IsLand(int terrain) => terrain >= 2;
}

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
    
    // Map
    public int CapitalX, CapitalY;
    public int ProvinceCount;      // tiles owned
    
    // Stats
    public float Treasury = 1000f;
    public float Prestige = 30f;
}

public class CityData
{
    public string Id = "";
    public string NationId = "";
    public string Name = "City";
    public int TileX, TileY;
    public bool IsCapital;
    public int Size = 1;  // 1=town, 2=city, 3=capital
}

public enum UnitType
{
    Tank, Soldier, Cannon, Ship, Fighter
}

public class UnitData
{
    public string Id = "";
    public string NationId = "";
    public UnitType Type;
    public int TileX, TileY;
    public float Strength = 1.0f;
    public int MovesRemaining = 3;
    public int MaxMoves = 3;
    public bool IsSelected = false;
    public bool IsAlive = true;
    
    // Pixel coordinates for smooth animation
    public float PixelX, PixelY;
    public float TargetPixelX, TargetPixelY;
    public bool IsMoving = false;
}

public class CharacterData
{
    public string Id = "";
    public string NationId = "";
    public string Name = "";
    public string Role = "Official"; // e.g., President, General, Intel Chief
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
    
    // Composite Full Authority Index
    public float FullAuthorityIndex => (TerritoryAuthority + WorldAuthority + BehindTheScenesAuthority) / 3f;
}
