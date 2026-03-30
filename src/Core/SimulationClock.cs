using Godot;
using Warship.Core;
using Warship.Events;

namespace Warship.Core;

/// <summary>
/// Real-time simulation clock. Time flows continuously at adjustable speed.
/// Each "tick" advances the game world by one step.
///
/// Controls:
///   SPACE  — toggle pause
///   + / =  — speed up
///   - / _  — slow down
///
/// Speeds: Pause (0x), Normal (1x), Fast (2x), Faster (5x), Max (10x)
/// At 1x: 1 tick every 2 real seconds.
/// At 10x: 10 ticks per 2 seconds = 5 ticks/sec.
///
/// Publishes TickEvent every tick so all systems can advance.
/// Publishes SimSpeedChangedEvent when speed changes.
/// </summary>
public partial class SimulationClock : Node
{
    private static readonly float[] SpeedLevels = { 0f, 1f, 2f, 5f, 10f };
    private static readonly string[] SpeedLabels = { "PAUSED", "1x", "2x", "5x", "10x" };

    private const float BaseTickInterval = 2.0f; // seconds per tick at 1x

    private int _speedIndex = 1; // start at 1x
    private float _accumulator;
    private int _tickNumber;
    private bool _worldReady;

    public float CurrentSpeed => SpeedLevels[_speedIndex];
    public string CurrentSpeedLabel => SpeedLabels[_speedIndex];
    public bool IsPaused => _speedIndex == 0;
    public int TickNumber => _tickNumber;

    public override void _Ready()
    {
        EventBus.Instance?.Subscribe<WorldReadyEvent>(_ => _worldReady = true);
        GD.Print("[SimClock] Ready. SPACE=pause, +/-=speed.");
    }

    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (e is not InputEventKey key || !key.Pressed) return;

        if (key.Keycode == Key.Space)
        {
            // Toggle pause: if paused go to 1x, if running go to 0x
            _speedIndex = _speedIndex == 0 ? 1 : 0;
            OnSpeedChanged();
            GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Key.Equal || key.Keycode == Key.KpAdd) // + key
        {
            if (_speedIndex < SpeedLevels.Length - 1)
            {
                _speedIndex++;
                OnSpeedChanged();
            }
            GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Key.Minus || key.Keycode == Key.KpSubtract) // - key
        {
            if (_speedIndex > 0)
            {
                _speedIndex--;
                OnSpeedChanged();
            }
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (!_worldReady || IsPaused) return;

        float speed = CurrentSpeed;
        float tickInterval = BaseTickInterval / speed;

        _accumulator += (float)delta;

        while (_accumulator >= tickInterval)
        {
            _accumulator -= tickInterval;
            _tickNumber++;

            EventBus.Instance?.Publish(new TickEvent(_tickNumber));
        }
    }

    private void OnSpeedChanged()
    {
        _accumulator = 0f;
        EventBus.Instance?.Publish(new SimSpeedChangedEvent(CurrentSpeed, CurrentSpeedLabel));
        GD.Print($"[SimClock] Speed: {CurrentSpeedLabel}");
    }
}
