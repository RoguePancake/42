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
        // Position bottom-right, inset from RightSidebar and BottomPanel
        AnchorsPreset = (int)LayoutPreset.BottomRight;
        OffsetLeft = -180 - UITheme.RightSidebarWidth;
        OffsetTop = -64 - UITheme.BottomPanelHeight;

        _button = new Button
        {
            Text = "END TURN",
            CustomMinimumSize = new Vector2(160, 48)
        };

        UITheme.ApplyPrimaryButtonStyle(_button, UITheme.AccentBlue);
        _button.AddThemeFontSizeOverride("font_size", UITheme.FontLarge);

        _button.Pressed += OnEndTurn;
        AddChild(_button);
    }

    private void OnEndTurn()
    {
        var data = WorldStateManager.Instance?.Data;
        if (data == null) return;

        data.TurnNumber++;

        GD.Print($"[EndTurn] Turn {data.TurnNumber} | Month {data.Month} Year {data.Year}");

        EventBus.Instance?.Publish(new TurnAdvancedEvent(data.TurnNumber, data.Year, data.Month));

        _button.Text = $"END TURN ({data.TurnNumber})";
    }
}
