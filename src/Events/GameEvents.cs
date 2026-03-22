using System;

namespace Warship.Events;

// Marker interface for all game events
public interface IGameEvent { }

// Requests from Input -> Engine
public record UnitMoveRequested(string UnitId, int TargetX, int TargetY) : IGameEvent;

// Events broadcast by Engine -> UI
public record UnitMovedEvent(string UnitId, int FromX, int FromY, int ToX, int ToY) : IGameEvent;
public record TurnAdvancedEvent(int Turn, int Year, int Month) : IGameEvent;
