using System;

namespace Warship.Events;

// Marker interface for all game events
public interface IGameEvent { }

// Requests from Input -> Engine
public record UnitMoveRequested(string UnitId, int TargetX, int TargetY) : IGameEvent;

// Events broadcast by Engine -> UI
public record UnitMovedEvent(string UnitId, int FromX, int FromY, int ToX, int ToY) : IGameEvent;
public record TurnAdvancedEvent(int Turn, int Year, int Month) : IGameEvent;

// Political Actions (UI -> PoliticalEngine)
public record PoliticalActionEvent(string ActorId, string TargetId, string ActionType) : IGameEvent;

// Results (PoliticalEngine -> UI)
public record AuthorityChangedEvent(string CharacterId, string Meter, float OldValue, float NewValue, string Reason) : IGameEvent;
public record NotificationEvent(string Message, string Type) : IGameEvent; // Type: "success", "warning", "danger", "info"
