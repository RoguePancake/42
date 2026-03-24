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
public record AuthorityChangedEvent(string CharacterId, string Meter, float OldValue, float NewValue, string Reason) : IGameEvent;
public record NotificationEvent(string Message, string Type) : IGameEvent; // Type: "success", "warning", "danger", "info"

// Crisis System
public record CrisisTriggeredEvent(string CrisisId, string Title, string Description, string[] Choices) : IGameEvent;
public record CrisisResolvedEvent(string CrisisId, int ChoiceIndex) : IGameEvent;

// Map System
public record NationSelectedEvent(string NationId) : IGameEvent;
public record MapStyleChangedEvent(string Style) : IGameEvent;
public record UnitMoveToCoordRequested(string UnitId, float Longitude, float Latitude) : IGameEvent;
