using Godot;
using Warship.Core;
using Warship.Events;

namespace Warship.UI.HUD;

/// <summary>
/// Speed control bar replacing EndTurnButton.
/// Shows: [⏸] [1x] [2x] [5x] [10x]  Tick 47 | M11 Y3
/// </summary>
public partial class SpeedControlBar : Control
{
    private Button _pauseBtn = null!;
    private Button _speed1Btn = null!;
    private Button _speed2Btn = null!;
    private Button _speed5Btn = null!;
    private Button _speed10Btn = null!;
    private Label _tickLabel = null!;
    private Button? _activeSpeedBtn;

    // SNES blue palette
    private static readonly Color NormalBg = new(0.15f, 0.25f, 0.55f);
    private static readonly Color HoverBg = new(0.22f, 0.35f, 0.7f);
    private static readonly Color ActiveBg = new(0.3f, 0.55f, 0.95f);
    private static readonly Color BorderColor = new(0.3f, 0.5f, 0.9f);

    public override void _Ready()
    {
        // Position: bottom-right, above BottomPanel, clear of RightSidebar
        AnchorsPreset = (int)LayoutPreset.BottomRight;
        OffsetLeft = -420 - 250;   // Clear the 250px RightSidebar
        OffsetTop = -56 - 200;     // Sit above the 200px BottomPanel
        OffsetRight = -250;
        OffsetBottom = -200;

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 4);
        AddChild(hbox);

        _pauseBtn = MakeButton("⏸", 48);
        _speed1Btn = MakeButton("1x", 48);
        _speed2Btn = MakeButton("2x", 48);
        _speed5Btn = MakeButton("5x", 48);
        _speed10Btn = MakeButton("10x", 52);

        _tickLabel = new Label
        {
            Text = "  PAUSED",
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(160, 48)
        };
        _tickLabel.AddThemeFontSizeOverride("font_size", 14);
        _tickLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.8f, 1.0f));

        hbox.AddChild(_pauseBtn);
        hbox.AddChild(_speed1Btn);
        hbox.AddChild(_speed2Btn);
        hbox.AddChild(_speed5Btn);
        hbox.AddChild(_speed10Btn);
        hbox.AddChild(_tickLabel);

        // Wire buttons
        _pauseBtn.Pressed += () => SimulationClock.Instance?.TogglePause();
        _speed1Btn.Pressed += () => SimulationClock.Instance?.SetSpeed(1f);
        _speed2Btn.Pressed += () => SimulationClock.Instance?.SetSpeed(2f);
        _speed5Btn.Pressed += () => SimulationClock.Instance?.SetSpeed(5f);
        _speed10Btn.Pressed += () => SimulationClock.Instance?.SetSpeed(10f);

        // Subscribe to clock events
        EventBus.Instance?.Subscribe<SimPausedEvent>(OnPauseChanged);
        EventBus.Instance?.Subscribe<SimSpeedChangedEvent>(OnSpeedChanged);
        EventBus.Instance?.Subscribe<TurnAdvancedEvent>(OnTick);

        // Initial state: paused, 1x highlighted
        HighlightButton(_speed1Btn);
        UpdatePauseVisual(true);
    }

    private void OnPauseChanged(SimPausedEvent ev)
    {
        UpdatePauseVisual(ev.IsPaused);
    }

    private void OnSpeedChanged(SimSpeedChangedEvent ev)
    {
        Button? btn = ev.Speed switch
        {
            1f => _speed1Btn,
            2f => _speed2Btn,
            5f => _speed5Btn,
            10f => _speed10Btn,
            _ => null
        };
        if (btn != null) HighlightButton(btn);
    }

    private void OnTick(TurnAdvancedEvent ev)
    {
        var clock = SimulationClock.Instance;
        string speedStr = clock != null ? $"{clock.SpeedMultiplier}x" : "?";
        _tickLabel.Text = $"  {speedStr} | Tick {ev.Turn} | M{ev.Month} Y{ev.Year}";
    }

    private void UpdatePauseVisual(bool paused)
    {
        _pauseBtn.Text = paused ? "▶" : "⏸";

        if (paused)
        {
            var data = WorldStateManager.Instance?.Data;
            if (data != null)
                _tickLabel.Text = $"  PAUSED | Tick {data.TurnNumber} | M{data.Month} Y{data.Year}";
            else
                _tickLabel.Text = "  PAUSED";
        }
    }

    private void HighlightButton(Button btn)
    {
        // Reset previous active
        if (_activeSpeedBtn != null)
            ApplyNormalStyle(_activeSpeedBtn);

        ApplyActiveStyle(btn);
        _activeSpeedBtn = btn;
    }

    private Button MakeButton(string text, float width)
    {
        var btn = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(width, 44)
        };
        btn.AddThemeFontSizeOverride("font_size", 16);
        ApplyNormalStyle(btn);
        return btn;
    }

    private static void ApplyNormalStyle(Button btn)
    {
        var style = MakeStyleBox(NormalBg);
        btn.AddThemeStyleboxOverride("normal", style);

        var hover = MakeStyleBox(HoverBg);
        btn.AddThemeStyleboxOverride("hover", hover);

        var pressed = MakeStyleBox(new Color(0.1f, 0.18f, 0.4f));
        btn.AddThemeStyleboxOverride("pressed", pressed);
    }

    private static void ApplyActiveStyle(Button btn)
    {
        var style = MakeStyleBox(ActiveBg);
        btn.AddThemeStyleboxOverride("normal", style);
        btn.AddThemeStyleboxOverride("hover", style);
        btn.AddThemeStyleboxOverride("pressed", style);
    }

    private static StyleBoxFlat MakeStyleBox(Color bg)
    {
        return new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = BorderColor,
            BorderWidthBottom = 2, BorderWidthTop = 2,
            BorderWidthLeft = 2, BorderWidthRight = 2,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            ContentMarginLeft = 8, ContentMarginRight = 8,
            ContentMarginTop = 4, ContentMarginBottom = 4
        };
    }
}
