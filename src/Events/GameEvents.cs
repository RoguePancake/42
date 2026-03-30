using Warship.Core;
using Warship.Data;

namespace Warship.Events;

// ═══════════════════════════════════════════════════════════════
//  World lifecycle
// ═══════════════════════════════════════════════════════════════

public record WorldReadyEvent(int Seed) : IGameEvent;
public record TickEvent(int TickNumber) : IGameEvent;
public record SimSpeedChangedEvent(float Speed, string Label) : IGameEvent;

// ═══════════════════════════════════════════════════════════════
//  Player actions → game state
// ═══════════════════════════════════════════════════════════════

public record PlayerMoveEvent(int TargetTileX, int TargetTileY) : IGameEvent;

// Building placement
public record PlaceBuildingEvent(BuildingType Type, int TileX, int TileY) : IGameEvent;
public record BuildingPlacedEvent(int BuildingId, BuildingType Type, int TileX, int TileY) : IGameEvent;
public record BuildingFailedEvent(string Reason) : IGameEvent;

// Troop commands
public record SpawnSquadEvent(int CampId, int Count) : IGameEvent;
public record SquadSpawnedEvent(int SquadId) : IGameEvent;
public record SquadOrderEvent(int SquadId, SquadOrder Order, int TargetX, int TargetY) : IGameEvent;
public record SetPatrolEvent(int SquadId, int AX, int AY, int BX, int BY) : IGameEvent;

// Selection (UI ↔ map)
public record SelectTileEvent(int TileX, int TileY) : IGameEvent;
public record SelectSquadEvent(int SquadId) : IGameEvent;
public record SelectBuildingEvent(int BuildingId) : IGameEvent;
public record DeselectAllEvent() : IGameEvent;
