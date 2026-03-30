using System;

namespace Warship.Events;

// Marker interface for all game events
public interface IGameEvent { }

// Requests from Input -> Engine
public record UnitMoveRequested(string UnitId, int TargetX, int TargetY) : IGameEvent;

// Events broadcast by Engine -> UI
public record UnitMovedEvent(string UnitId, int FromX, int FromY, int ToX, int ToY) : IGameEvent;
public record TurnAdvancedEvent(int Turn, int Year, int Month) : IGameEvent;
public record WorldReadyEvent(int Seed, string PlayerNationId) : IGameEvent;

// Political Actions (UI -> PoliticalEngine)
public record PoliticalActionEvent(string ActorId, string TargetId, string ActionType) : IGameEvent;

// Results (PoliticalEngine -> UI)
public record NotificationEvent(string Message, string Type) : IGameEvent; // Type: "success", "warning", "danger", "info"

// Crisis System
public record CrisisTriggeredEvent(string CrisisId, string Title, string Description, string[] Choices) : IGameEvent;
public record CrisisResolvedEvent(string CrisisId, int ChoiceIndex) : IGameEvent;

// Map System
public record NationSelectedEvent(string NationId) : IGameEvent;
public record MapStyleChangedEvent(string Style) : IGameEvent;
public record UnitMoveToCoordRequested(string UnitId, float Longitude, float Latitude) : IGameEvent;

// Main View System
public record ViewSwitchEvent(string ViewId) : IGameEvent; // "map", "intel", "warroom", "economy"

// Hot Zone Maps
public record HotZonePinEvent(int SlotIndex, int CenterTileX, int CenterTileY, string Label) : IGameEvent; // Pin a hot zone (0-2)
public record HotZoneClearEvent(int SlotIndex) : IGameEvent; // Clear a hot zone slot

// Player Actions (UI -> Engines)
public record PlayerActionEvent(string Category, string Action) : IGameEvent;

// Army System
public record ArmyMoveRequested(string ArmyId, int TargetX, int TargetY) : IGameEvent;
public record ArmyMovedEvent(string ArmyId, int FromX, int FromY, int ToX, int ToY) : IGameEvent;
public record ArmyCreatedEvent(string ArmyId, string NationId) : IGameEvent;
public record ArmyDestroyedEvent(string ArmyId, string NationId) : IGameEvent;

// Territory / City Capture
public record CityCapturedEvent(string CityId, string OldNationId, string NewNationId) : IGameEvent;
public record TerritoryChangedEvent(string CityId, string OldNationId, string NewNationId) : IGameEvent;
public record BattleResolvedEvent(string AttackerArmyId, string DefenderArmyId, bool AttackerWon,
    int AttackerLosses, int DefenderLosses) : IGameEvent;

// Simulation Clock
public record SimSpeedChangedEvent(float Speed) : IGameEvent;
public record SimPausedEvent(bool IsPaused) : IGameEvent;

// Custom Nation Placement
public record CustomNationPlacementModeEvent(bool Active) : IGameEvent; // UI enters/exits capital placement mode
public record CustomNationCapitalPlacedEvent(int TileX, int TileY) : IGameEvent; // Player clicked map to place capital

// Interrupt System ("The Phone Rings")
public record InterruptTriggeredEvent(string Id, string Title, string Description,
    float TimerSeconds, Warship.Data.InterruptChoice[] Choices, int DefaultChoiceIndex,
    Warship.Data.InterruptPriority Priority) : IGameEvent;
public record InterruptResolvedEvent(string Id, int ChoiceIndex, bool WasTimeout) : IGameEvent;

// Council System (Government Actions)
public record CouncilActionEvent(string NationId, Warship.Data.CouncilActionCategory Category,
    string ActionId) : IGameEvent; // Player issues council order
public record AdviserOpinionEvent(string AdviserId, string ActionId, bool Approves,
    string Advice) : IGameEvent; // Adviser reacts to proposed action

// Combat Command (Per-Army Orders)
public record ArmyOrderEvent(string ArmyId, Warship.Data.MilitaryOrder Order) : IGameEvent;
public record ArmyFormationEvent(string ArmyId, Warship.Data.FormationType Formation) : IGameEvent;
public record ArmySelectedEvent(string? ArmyId) : IGameEvent; // null = deselected

// ── World Engine Events ──

// LOD System
public record LodChangedEvent(Warship.Data.LodLevel OldLod, Warship.Data.LodLevel NewLod, float Zoom) : IGameEvent;

// Chunk System
public record ChunkLoadedEvent(Warship.Data.ChunkCoord Coord) : IGameEvent;
public record ChunkUnloadedEvent(Warship.Data.ChunkCoord Coord) : IGameEvent;

// Build System
public record BuildRequestEvent(Warship.Data.StructureType Type, int TileX, int TileY, string NationId) : IGameEvent;
public record BuildCompletedEvent(string StructureId, Warship.Data.StructureType Type, int TileX, int TileY) : IGameEvent;
public record BuildFailedEvent(string Reason, int TileX, int TileY) : IGameEvent;
public record DemolishRequestEvent(int TileX, int TileY) : IGameEvent;

// Road System
public record RoadBuildRequestEvent(int FromX, int FromY, int ToX, int ToY, Warship.Data.RoadType Type) : IGameEvent;
public record RoadBuiltEvent(int FromX, int FromY, int ToX, int ToY, Warship.Data.RoadType Type) : IGameEvent;

// Wall System
public record WallBuildRequestEvent(int TileX, int TileY, Warship.Data.DirectionMask Facing, Warship.Data.WallType Type) : IGameEvent;
public record WallBuiltEvent(int TileX, int TileY, Warship.Data.DirectionMask Facing) : IGameEvent;

// Pathfinding
public record PathRequestEvent(string UnitId, int FromX, int FromY, int ToX, int ToY) : IGameEvent;
public record PathComputedEvent(string UnitId, (int x, int y)[] Path) : IGameEvent;
