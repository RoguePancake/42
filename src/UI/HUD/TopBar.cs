using Godot;
using Warship.Core;
using Warship.Events;

namespace Warship.UI.HUD;

/// <summary>
/// Top Bar B — 32px tall bar showing turn counter, date, and player stats.
/// Sits directly below the alert bar (Top Bar A).
/// </summary>
public partial class TopBar : Control
{
    private Label _turnLabel = null!;
    private Label _statsLabel = null!;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        OffsetTop = 32;  // Under alert bar (Top Bar A)
        OffsetBottom = 64; // 32px tall (was 48px)

        // Background
        var bg = new ColorRect
        {
            Color = new Color(0.08f, 0.08f, 0.12f, 0.95f),
            AnchorsPreset = (int)LayoutPreset.FullRect
        };
        AddChild(bg);

        // Bottom border
        var border = new ColorRect
        {
            Color = new Color(0.2f, 0.4f, 0.8f, 1f),
            CustomMinimumSize = new Vector2(0, 3)
        };
        border.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
        AddChild(border);

        // Layout container
        var hbox = new HBoxContainer
        {
            AnchorsPreset = (int)LayoutPreset.FullRect
        };
        AddChild(hbox);

        // Left margin
        var leftMargin = new MarginContainer();
        leftMargin.AddThemeConstantOverride("margin_left", 16);

        // Right margin
        var rightMargin = new MarginContainer();
        rightMargin.AddThemeConstantOverride("margin_right", 16);
        rightMargin.SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.ShrinkEnd;

        // Labels (smaller font to fit 32px height)
        _turnLabel = new Label
        {
            Text = " Turn 1 | Jan 1900 ",
            VerticalAlignment = VerticalAlignment.Center
        };
        _turnLabel.AddThemeFontSizeOverride("font_size", 14);

        _statsLabel = new Label
        {
            Text = " Treasury: ... | Provinces: ... ",
            VerticalAlignment = VerticalAlignment.Center
        };
        _statsLabel.AddThemeFontSizeOverride("font_size", 14);

        leftMargin.AddChild(_turnLabel);
        rightMargin.AddChild(_statsLabel);

        hbox.AddChild(leftMargin);
        hbox.AddChild(rightMargin);

        // Subscribe to events
        EventBus.Instance?.Subscribe<TurnAdvancedEvent>(ev => {
            string speed = SimulationClock.Instance != null ? $"{SimulationClock.Instance.SpeedMultiplier}x" : "";
            _turnLabel.Text = $" {speed} | Tick {ev.Turn} | M{ev.Month} Y{ev.Year} ";
        });
        EventBus.Instance?.Subscribe<SimPausedEvent>(ev => {
            var data = WorldStateManager.Instance?.Data;
            if (ev.IsPaused && data != null)
                _turnLabel.Text = $" PAUSED | Tick {data.TurnNumber} | M{data.Month} Y{data.Year} ";
        });
    }

    public override void _Process(double delta)
    {
        var data = WorldStateManager.Instance?.Data;
        if (data != null && data.Characters.Count > 0)
        {
            var pc = data.Characters.Find(c => c.IsPlayer);
            if (pc != null)
            {
                int natIdx = int.Parse(pc.NationId.Split('_')[1]);
                var nat = data.Nations[natIdx];
                _statsLabel.Text = $" Treasury: ${nat.Treasury:0}M  |  {pc.Role} {pc.Name}  |  Provinces: {nat.ProvinceCount}  |  Stability: {nat.Stability:0}% ";
            }
        }
    }
}
