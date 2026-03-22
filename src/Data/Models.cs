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
}
