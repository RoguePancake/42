using Godot;
using Warship.Core;
using Warship.Data;
using Warship.Events;

namespace Warship.UI.HUD;

/// <summary>
/// Bottom-right "End Turn" button and turn counter.
/// Dispatches TurnAdvancedEvent through the EventBus.
/// </summary>
public partial class EndTurnButton : Control
{
    private Button _button = null!;

    public override void _Ready()
    {
        // Position bottom-right
        AnchorsPreset = (int)LayoutPreset.BottomRight;
        OffsetLeft = -180;
        OffsetTop = -64;

        _button = new Button
        {
            Text = "⏭ END TURN",
            CustomMinimumSize = new Vector2(160, 48)
        };

        // Style it
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.25f, 0.55f),
            BorderColor = new Color(0.3f, 0.5f, 0.9f),
            BorderWidthBottom = 2, BorderWidthTop = 2,
            BorderWidthLeft = 2, BorderWidthRight = 2,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            ContentMarginLeft = 12, ContentMarginRight = 12,
            ContentMarginTop = 8, ContentMarginBottom = 8
        };
        _button.AddThemeStyleboxOverride("normal", style);

        var hover = (StyleBoxFlat)style.Duplicate();
        hover.BgColor = new Color(0.22f, 0.35f, 0.7f);
        _button.AddThemeStyleboxOverride("hover", hover);

        var pressed = (StyleBoxFlat)style.Duplicate();
        pressed.BgColor = new Color(0.1f, 0.18f, 0.4f);
        _button.AddThemeStyleboxOverride("pressed", pressed);

        _button.AddThemeFontSizeOverride("font_size", 18);

        _button.Pressed += OnEndTurn;
        AddChild(_button);
    }

    private void OnEndTurn()
    {
        var data = WorldStateManager.Instance?.Data;
        if (data == null) return;

        data.TurnNumber++;

        GD.Print($"[EndTurn] Turn {data.TurnNumber} | Month {data.Month} Year {data.Year}");

        // Broadcast
        EventBus.Instance?.Publish(new TurnAdvancedEvent(data.TurnNumber, data.Year, data.Month));

        _button.Text = $"⏭ END TURN ({data.TurnNumber})";
    }
}
