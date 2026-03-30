using System;
using System.Collections.Generic;
using Godot;

namespace Warship.Core;

/// <summary>
/// Typed publish/subscribe event bus. ALL system communication goes through here.
/// No system ever calls another system directly.
/// </summary>
public partial class EventBus : Node
{
    public static EventBus? Instance { get; private set; }

    private readonly Dictionary<Type, List<object>> _subs = new();

    public override void _EnterTree()
    {
        if (Instance != null) { QueueFree(); return; }
        Instance = this;
        GD.Print("[EventBus] Online.");
    }

    public void Subscribe<T>(Action<T> handler) where T : IGameEvent
    {
        var t = typeof(T);
        if (!_subs.ContainsKey(t))
            _subs[t] = new List<object>();
        _subs[t].Add(handler);
    }

    public void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
    {
        if (_subs.TryGetValue(typeof(T), out var list))
            list.Remove(handler);
    }

    public void Publish<T>(T ev) where T : IGameEvent
    {
        if (_subs.TryGetValue(typeof(T), out var list))
        {
            // Iterate a copy in case handlers modify the list
            var snapshot = list.ToArray();
            foreach (var handler in snapshot)
                ((Action<T>)handler)(ev);
        }
    }
}

/// <summary>Marker interface for all game events.</summary>
public interface IGameEvent { }
