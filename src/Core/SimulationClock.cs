using Godot;
using Warship.Data;
using Warship.Events;

namespace Warship.Core;

/// <summary>
/// Real-time simulation clock. Replaces the manual EndTurnButton.
/// Ticks the simulation automatically at adjustable speeds.
/// SPACE toggles pause. Each tick advances TurnNumber and fires TurnAdvancedEvent.
/// </summary>
public partial class SimulationClock : Node
{
    public static SimulationClock? Instance { get; private set; }

    public bool IsPaused { get; private set; } = true;
    public float SpeedMultiplier { get; private set; } = 1.0f;
    public int CurrentTick => WorldStateManager.Instance?.Data?.TurnNumber ?? 0;

    private const float BaseTickInterval = 2.0f; // seconds per tick at 1x
    private static readonly float[] SpeedLevels = { 1f, 2f, 5f, 10f };

    private double _accumulator;

    public override void _EnterTree()
    {
        if (Instance != null) { QueueFree(); return; }
        Instance = this;
    }

    public override void _Ready()
    {
        _accumulator = 0.0;
        GD.Print("[SimulationClock] Online. Starts paused. Press SPACE to begin.");
    }

    public override void _Process(double delta)
    {
        if (IsPaused || SpeedMultiplier <= 0f) return;

        _accumulator += delta;
        float interval = BaseTickInterval / SpeedMultiplier;

        while (_accumulator >= interval)
        {
            _accumulator -= interval;
            ExecuteTick();
        }
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Space)
        {
            TogglePause();
            GetViewport().SetInputAsHandled();
        }
    }

    private void ExecuteTick()
    {
        var data = WorldStateManager.Instance?.Data;
        if (data == null) return;

        data.TurnNumber++;
        GD.Print($"[SimClock] Tick {data.TurnNumber} | M{data.Month} Y{data.Year} @ {SpeedMultiplier}x");
        EventBus.Instance?.Publish(new TurnAdvancedEvent(data.TurnNumber, data.Year, data.Month));
    }

    public void TogglePause()
    {
        IsPaused = !IsPaused;
        _accumulator = 0.0;
        GD.Print($"[SimClock] {(IsPaused ? "PAUSED" : $"RUNNING @ {SpeedMultiplier}x")}");
        EventBus.Instance?.Publish(new SimPausedEvent(IsPaused));
    }

    public void SetSpeed(float multiplier)
    {
        SpeedMultiplier = multiplier;
        if (IsPaused && multiplier > 0f)
        {
            IsPaused = false;
            EventBus.Instance?.Publish(new SimPausedEvent(false));
        }
        GD.Print($"[SimClock] Speed set to {SpeedMultiplier}x");
        EventBus.Instance?.Publish(new SimSpeedChangedEvent(SpeedMultiplier));
    }

    public void CycleSpeed()
    {
        int idx = System.Array.IndexOf(SpeedLevels, SpeedMultiplier);
        int next = (idx + 1) % SpeedLevels.Length;
        SetSpeed(SpeedLevels[next]);
    }
}
