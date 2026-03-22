# WARSHIP — Data Models
## C# Class Definitions for Claude Code

Copy these into `src/Data/Models.cs` and adapt as you build each phase.

---

## Core Models (Phase 7+)

```csharp
using System.Collections.Generic;

namespace Warship.Data
{
    // ═══════════════════════════════════
    // WORLD — Top level container
    // ═══════════════════════════════════
    
    public class WorldData
    {
        public int Seed;
        public int TurnNumber = 1;
        public int Year => TurnNumber / 12;
        public int Month => (TurnNumber % 12) + 1;
        public string PlayerNationId;

        public int MapWidth;
        public int MapHeight;
        public int[,] TerrainMap;    // tile index per cell
        public int[,] OwnershipMap;  // nation index per cell, -1 = unclaimed
        
        public List<NationData> Nations = new();
        public List<UnitData> Units = new();
        public List<CityData> Cities = new();
        public List<Vector2[]> RiverPaths = new();
        public List<TradeRoute> TradeRoutes = new();
        public List<NewsItem> CurrentNews = new();
        public List<NewsItem> NewsHistory = new();
    }

    // ═══════════════════════════════════
    // TERRAIN
    // ═══════════════════════════════════

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
        
        // Combat defender bonus
        public static float DefenseBonus(int terrain) => terrain switch
        {
            4 => 0.10f,  // Forest: +10%
            5 => 0.05f,  // Hills: +5%
            6 => 0.20f,  // Mountain: +20% (if units could be here)
            _ => 0f
        };
        
        // Movement cost multiplier
        public static float MoveCost(int terrain) => terrain switch
        {
            4 => 1.5f,   // Forest: slow
            5 => 1.3f,   // Hills: moderate
            2 => 1.2f,   // Sand: slightly slow
            7 => 1.5f,   // Snow: slow
            _ => 1.0f
        };
    }

    // ═══════════════════════════════════
    // NATION
    // ═══════════════════════════════════

    public enum NationArchetype
    {
        Hegemon,        // Military dominant, seeks control
        Commercial,     // Trade focused, prefers sanctions over war
        Revolutionary,  // Aggressive ideologues, unpredictable
        Traditionalist, // Conservative, defensive, culturally cohesive
        Survival,       // Desperate coalition, nothing to lose
        FreeState       // Player's nation
    }

    public class NationData
    {
        public string Id;
        public string Name;
        public NationArchetype Archetype;
        public string Color;          // hex color "#c03030"
        public bool IsPlayer = false;
        
        // Map
        public int CapitalX, CapitalY;
        public int ProvinceCount;      // tiles owned
        
        // Core stats (0.0 - 1.0 unless noted)
        public float Stability = 0.70f;
        public float Legitimacy = 0.60f;
        public float MilitaryStrength = 0.30f;
        public float WarExhaustion = 0f;     // 0-100
        public float Infamy = 0f;             // 0-100
        public float Prestige = 30f;          // 0-100
        
        // Economy
        public float Treasury = 1000f;
        public float GDP = 500f;
        public int MilitaryBudget = 35;       // % (must sum to 100)
        public int InfraBudget = 25;
        public int ResearchBudget = 25;
        public int SocialBudget = 15;
        
        // Population
        public long Population = 2000000;
        
        // War
        public List<string> AtWarWith = new();
        
        // Intel
        public List<SpyNetwork> SpyNetworks = new();
        
        // Factions (power 0-100)
        public float MilitaryFaction = 50f;
        public float MerchantFaction = 50f;
        public float NationalistFaction = 30f;
        public float ProgressiveFaction = 40f;
        public float ReligiousFaction = 35f;
        
        // Nuclear
        public int Warheads = 0;
        
        // Relations with other nations: Dictionary<nationId, value>
        public Dictionary<string, float> Relations = new();
        
        // Helpers
        public bool IsAtWarWith(string nationId) => AtWarWith.Contains(nationId);
        public float GetRelation(string nationId) => 
            Relations.TryGetValue(nationId, out float v) ? v : 0f;
    }

    // ═══════════════════════════════════
    // NATION DELTA — the ONLY way to change nation data
    // ═══════════════════════════════════

    public class NationDelta
    {
        public string NationId;
        public float? TreasuryDelta;
        public float? StabilityDelta;
        public float? LegitimacyDelta;
        public float? MilitaryDelta;
        public float? WarExhaustionDelta;
        public float? InfamyDelta;
        public float? PrestigeDelta;
        public float? GDPDelta;
    }

    // ═══════════════════════════════════
    // UNITS
    // ═══════════════════════════════════

    public enum UnitType
    {
        Tank,
        Soldier,
        Cannon,
        Ship,
        Fighter
    }

    public class UnitData
    {
        public string Id;
        public string NationId;
        public UnitType Type;
        public int TileX, TileY;
        public float Strength = 1.0f;    // 0.0 to 1.0
        public int MovesRemaining = 3;
        public int MaxMoves = 3;
        public bool IsSelected = false;
        public bool IsAlive = true;
        
        // Pixel position for smooth animation
        public float PixelX, PixelY;
        public float TargetPixelX, TargetPixelY;
        public bool IsMoving = false;
        
        public void ResetMoves() => MovesRemaining = MaxMoves;
    }

    // ═══════════════════════════════════
    // CITIES
    // ═══════════════════════════════════

    public class CityData
    {
        public string Id;
        public string NationId;
        public string Name;
        public int TileX, TileY;
        public bool IsCapital;
        public int Size = 1;  // 1=town, 2=city, 3=capital
    }

    // ═══════════════════════════════════
    // TRADE ROUTES
    // ═══════════════════════════════════

    public enum RouteStatus { Active, Threatened, Blocked }

    public class TradeRoute
    {
        public string NationAId;
        public string NationBId;
        public float Value;           // gold per turn
        public RouteStatus Status = RouteStatus.Active;
        
        // Visual path: array of pixel-space waypoints
        public float[] WaypointX;
        public float[] WaypointY;
    }

    // ═══════════════════════════════════
    // INTEL
    // ═══════════════════════════════════

    public class SpyNetwork
    {
        public string TargetNationId;
        public int Depth = 0;          // 0-5
        public int TurnsActive = 0;
        public int TurnsToNextDepth = 5;
    }

    // ═══════════════════════════════════
    // NEWS
    // ═══════════════════════════════════

    public enum NewsCategory { War, Trade, Diplomacy, Intel, Event, System }

    public class NewsItem
    {
        public string Headline;
        public NewsCategory Category;
        public int Turn;
        public int Priority;  // 0-100, 80+ = breaking
    }
}
```

---

## Event Types (Phase 13+)

```csharp
namespace Warship.Events
{
    // Marker interface
    public interface IGameEvent { }

    // System
    public record TurnAdvancedEvent(int Turn, int Year, int Month) : IGameEvent;
    public record WorldReadyEvent(int Seed, string PlayerNationId) : IGameEvent;
    
    // Units
    public record UnitMovedEvent(string UnitId, int FromX, int FromY, int ToX, int ToY) : IGameEvent;
    public record UnitDestroyedEvent(string UnitId, string KilledByNationId) : IGameEvent;
    
    // Combat
    public record BattleResolvedEvent(
        string AttackerUnitId, string DefenderUnitId,
        int TileX, int TileY,
        bool AttackerWon,
        float AttackerDamage, float DefenderDamage
    ) : IGameEvent;
    
    // War
    public record WarDeclaredEvent(string AggressorId, string DefenderId) : IGameEvent;
    public record PeaceSignedEvent(string NationAId, string NationBId) : IGameEvent;
    
    // Economy
    public record ResourcesCollectedEvent(string NationId, float Income) : IGameEvent;
    public record NationStatsChangedEvent(string NationId) : IGameEvent;
    
    // Diplomacy
    public record RelationsChangedEvent(string NationAId, string NationBId, float NewValue) : IGameEvent;
    
    // Intel
    public record SpyNetworkAdvancedEvent(string OwnerId, string TargetId, int NewDepth) : IGameEvent;
    
    // News
    public record NewsPublishedEvent(string Headline, string Category, int Priority) : IGameEvent;
}
```

---

## Engine Interface (Phase 13)

```csharp
namespace Warship.Core
{
    public interface ISimEngine
    {
        string EngineName { get; }
        int Priority { get; }
        void Initialize(WorldStateManager state, EventBus bus);
        void OnTurnPhase(TurnContext ctx, TurnPhase phase);
        void Shutdown();
    }

    public enum TurnPhase
    {
        TurnOpen = 0,
        Resource = 1,
        Economy = 2,
        Trade = 3,
        Unrest = 4,
        Politics = 5,
        Diplomacy = 6,
        AIDecision = 7,
        PlayerAction = 8,
        Military = 9,
        Intel = 10,
        Events = 11,
        News = 12,
        TurnClose = 13
    }

    public class TurnContext
    {
        public int TurnNumber;
        public int Year;
        public int Month;
    }
}
```

---

## Notes

- Start with all models in one file (`Models.cs`). Split later when it gets big.
- All fields are public for simplicity. Add properties later if needed.
- NationDelta uses nullable floats — only non-null fields get applied.
- UnitData has both tile position AND pixel position for smooth animation.
- Keep it simple. Add fields as you need them, not before.
