# WARSHIP / FULL AUTHORITY — Data Models
## C# Class Definitions (Post-Map Overhaul)

> **Updated 2026-03-23:** All entity models now include real-world coordinates (lon/lat)
> alongside legacy tile coordinates for backward compatibility.

---

## Core Models (Current State in `src/Data/Models.cs`)

```csharp
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

    // Legacy grid (backward compat for engines that reference tile coords)
    public int MapWidth;        // 360 (degrees longitude, conceptual)
    public int MapHeight;       // 180 (degrees latitude, conceptual)
    public int[,]? TerrainMap;  // terrain type per cell (from TerrainGenerator)
    public int[,]? OwnershipMap;// nation index per cell, -1 = unclaimed
    
    public List<NationData> Nations = new();
    public List<CityData> Cities = new();
    public List<Vector2[]> RiverPaths = new(); // Empty — real rivers from tiles now
    public List<UnitData> Units = new();
    public List<CharacterData> Characters = new();
}

public enum NationArchetype
{
    Hegemon,        // Military dominant (US)
    Commercial,     // Trade focused (China)
    Revolutionary,  // Aggressive, unpredictable (Russia)
    Traditionalist, // Conservative, defensive (EU)
    Survival,       // Desperate coalition (India)
    FreeState       // Player's nation (UK)
}

public class NationData
{
    public string Id = "";
    public string Name = "";
    public NationArchetype Archetype;
    public Color NationColor;
    public bool IsPlayer = false;
    
    // Map (legacy tile coords — backward compat)
    public int CapitalX, CapitalY;
    public int ProvinceCount;

    // Real-world coordinates (PRIMARY positioning system)
    public float CapitalLon;                       // Longitude of capital
    public float CapitalLat;                       // Latitude of capital  
    public float[][] BorderPolygon = Array.Empty<float[]>(); // [lon, lat] border points
    
    // Stats
    public float Treasury = 1000f;
    public float Prestige = 30f;

    // Military Command
    public MilitaryOrder GlobalMilitaryOrder = MilitaryOrder.BorderWatch;
    public int CommandTargetX = -1;
    public int CommandTargetY = -1;
    public float CommandTargetLon = float.NaN;     // Real-world command target
    public float CommandTargetLat = float.NaN;
}

public class CityData
{
    public string Id = "";
    public string NationId = "";
    public string Name = "City";
    public int TileX, TileY;       // Legacy
    public bool IsCapital;
    public int Size = 1;           // 1=town, 2=city, 3=capital
    
    // Real-world coordinates
    public float Longitude;
    public float Latitude;
}

public class UnitData
{
    public string Id = "";
    public string NationId = "";
    public UnitType Type;
    public int TileX, TileY;       // Legacy
    public float Strength = 1.0f;
    public bool IsAlive = true;

    public MilitaryOrder CurrentOrder = MilitaryOrder.Standby;
    
    // Pixel coordinates (legacy smooth animation)
    public float PixelX, PixelY;
    public float TargetPixelX, TargetPixelY;
    
    // Real-world coordinates
    public float Longitude;
    public float Latitude;
    public float TargetLongitude;
    public float TargetLatitude;
}

public class CharacterData
{
    public string Id = "";
    public string NationId = "";
    public string Name = "";
    public string Role = "Official";
    public bool IsPlayer = false;
    
    public int TileX, TileY;       // Legacy
    public float PixelX, PixelY;
    public float TargetPixelX, TargetPixelY;
    public bool IsMoving = false;
    
    // Real-world coordinates
    public float Longitude;
    public float Latitude;
    
    // Authority Meters (0.0 to 100.0)
    public float TerritoryAuthority = 30f;
    public float WorldAuthority = 20f;
    public float BehindTheScenesAuthority = 40f;
    
    public float FullAuthorityIndex => 
        (TerritoryAuthority + WorldAuthority + BehindTheScenesAuthority) / 3f;
}
```

---

## Real-World Geography (`src/Data/GeoData.cs`)

```csharp
public static class GeoData
{
    // 6 nations at real coordinates:
    // - United States (Hegemon) — Capital: Washington D.C.
    // - China (Commercial) — Capital: Beijing
    // - Russia (Revolutionary) — Capital: Moscow
    // - European Union (Traditionalist) — Capital: Brussels
    // - India (Survival) — Capital: New Delhi
    // - United Kingdom (FreeState/Player) — Capital: London
    
    // Each nation has:
    // - Simplified border polygon (~20-40 points)
    // - 8-17 real cities with coordinates
    // - 4-5 military base locations
    
    // 12 global trade routes following real shipping lanes
}
```

---

## Event Types (Current in `src/Events/GameEvents.cs`)

```csharp
namespace Warship.Events;

public interface IGameEvent { }

// Core
public record UnitMoveRequested(string UnitId, int TargetX, int TargetY) : IGameEvent;
public record UnitMovedEvent(string UnitId, int FromX, int FromY, int ToX, int ToY) : IGameEvent;
public record TurnAdvancedEvent(int Turn, int Year, int Month) : IGameEvent;

// Political
public record PoliticalActionEvent(string ActorId, string TargetId, string ActionType) : IGameEvent;
public record AuthorityChangedEvent(string CharacterId, string Meter, float OldValue, float NewValue, string Reason) : IGameEvent;
public record NotificationEvent(string Message, string Type) : IGameEvent;

// Crisis
public record CrisisTriggeredEvent(string CrisisId, string Title, string Description, string[] Choices) : IGameEvent;
public record CrisisResolvedEvent(string CrisisId, int ChoiceIndex) : IGameEvent;

// Map System (NEW)
public record NationSelectedEvent(string NationId) : IGameEvent;
public record MapStyleChangedEvent(string Style) : IGameEvent;
public record UnitMoveToCoordRequested(string UnitId, float Longitude, float Latitude) : IGameEvent;
```

---

## Coordinate Systems

The game now uses **two coordinate systems** simultaneously:

| System | Used By | Range | Purpose |
|--------|---------|-------|---------|
| **Tile (x, y)** | Legacy engines, TerrainMap, OwnershipMap | 0-80, 0-50 | Backward compat |
| **Lon/Lat** | TileMapRenderer, WarshipMapBridge, GeoData | -180° to 180°, -90° to 90° | Real-world positioning |

### Conversion
```csharp
// Lon/Lat → Tile (approximate)
int tileX = (int)((lon + 180f) / 360f * 80f);
int tileY = (int)((90f - lat) / 180f * 50f);

// Lon/Lat → World pixels (in TileMapRenderer)
Vector2 worldPos = renderer.LonLatToWorld(lon, lat, zoomLevel);

// World pixels → Lon/Lat
(float lon, float lat) = renderer.WorldToLonLat(worldPos, zoomLevel);
```

---

## Notes

- All entity models support both tile AND lon/lat coordinates
- New code should use lon/lat as the primary system
- Legacy tile coords are auto-calculated during world generation for backward compat
- The tile-based TerrainMap and OwnershipMap are still generated but are approximate
- Real territory detection uses `PointInPolygon` on the BorderPolygon arrays
