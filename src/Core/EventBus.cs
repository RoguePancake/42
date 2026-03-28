using System;
using System.Collections.Generic;
using Godot;
using Warship.Events;

namespace Warship.Core;

/// <summary>
/// Central nervous system of the game.
/// All communication between UI (Map, Buttons) and Game Logic (Engines) routes through here.
/// </summary>
public partial class EventBus : Node
{
    public static EventBus? Instance { get; private set; }

    private readonly Dictionary<Type, List<object>> _subscribers = new();

    public override void _EnterTree()
    {
        if (Instance != null) { QueueFree(); return; }
        Instance = this;
    }

    /// <summary>Subscribe a callback to a specific game event.</summary>
    public void Subscribe<T>(Action<T> handler) where T : IGameEvent
    {
        var type = typeof(T);
        if (!_subscribers.ContainsKey(type))
            _subscribers[type] = new List<object>();
        
        _subscribers[type].Add(handler);
    }

    /// <summary>Remove a specific callback from a game event.</summary>
    public void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
    {
        var type = typeof(T);
        if (_subscribers.TryGetValue(type, out var handlers))
            handlers.Remove(handler);
    }

    /// <summary>Fire an event immediately to all subscribers.</summary>
    public void Publish<T>(T gameEvent) where T : IGameEvent
    {
        var type = typeof(T);
        if (_subscribers.TryGetValue(type, out var handlers))
        {
            foreach (var handler in handlers)
            {
                // We cast back to the explicit Action<T> and invoke it
                ((Action<T>)handler)(gameEvent);
            }
        }
    }
}
