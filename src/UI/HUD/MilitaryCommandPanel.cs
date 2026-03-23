using Godot;
using Warship.Core;
using Warship.Data;

namespace Warship.UI.HUD;

/// <summary>
/// Allows the player to set Global Military Orders for their 500-troop army.
/// </summary>
public partial class MilitaryCommandPanel : Control
{
    private Label _statusLabel = null!;

    public override void _Ready()
    {
        // Position bottom-left but clear the new Left Sidebar and Bottom Panel
        SetAnchorsAndOffsetsPreset(LayoutPreset.BottomLeft);
        OffsetTop = -420; // 200px tall
        OffsetBottom = -220; // 20px above the 200px custom bottom panel
        OffsetLeft = 270; // 20px right of the 250px Left Sidebar
        OffsetRight = 490; // 220px wide

        var bg = new Panel();
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.15f, 0.9f),
            BorderColor = new Color(0.3f, 0.4f, 0.6f),
            BorderWidthTop = 2, BorderWidthRight = 2,
            CornerRadiusTopRight = 8
        };
        bg.AddThemeStyleboxOverride("panel", style);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var vbox = new VBoxContainer();
        vbox.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 6);
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddChild(vbox);
        AddChild(margin);

        var title = new Label { Text = "ARMY COMMAND" };
        title.AddThemeFontSizeOverride("font_size", 16);
        title.AddThemeColorOverride("font_color", Colors.LightSteelBlue);
        vbox.AddChild(title);

        _statusLabel = new Label { Text = "Current: Standby" };
        _statusLabel.AddThemeFontSizeOverride("font_size", 12);
        _statusLabel.AddThemeColorOverride("font_color", Colors.Gray);
        vbox.AddChild(_statusLabel);

        vbox.AddChild(new HSeparator());

        AddButton(vbox, "🛡 Border Watch", MilitaryOrder.BorderWatch, Colors.LightGreen);
        AddButton(vbox, "🚶‍♂️ Patrol", MilitaryOrder.Patrol, Colors.LightBlue);
        AddButton(vbox, "🔵 Stage Army", MilitaryOrder.Stage, new Color(0.4f, 0.6f, 1f));
        AddButton(vbox, "⚔️ ATTACK", MilitaryOrder.Attack, Colors.Crimson);
    }

    private void AddButton(VBoxContainer parent, string text, MilitaryOrder order, Color highlight)
    {
        var btn = new Button { Text = text, CustomMinimumSize = new Vector2(0, 24) };
        
        var style = new StyleBoxFlat { BgColor = new Color(0.15f, 0.15f, 0.2f), CornerRadiusTopLeft = 4, CornerRadiusBottomRight = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4 };
        var hover = (StyleBoxFlat)style.Duplicate();
        hover.BgColor = highlight * 0.5f;

        btn.AddThemeStyleboxOverride("normal", style);
        btn.AddThemeStyleboxOverride("hover", hover);

        btn.Pressed += () => 
        {
            var world = WorldStateManager.Instance?.Data;
            if (world != null)
            {
                int pIdx = int.Parse(world.PlayerNationId!.Split('_')[1]);
                var nation = world.Nations[pIdx];
                nation.GlobalMilitaryOrder = order;
                _statusLabel.Text = $"Current: {order}";
                _statusLabel.AddThemeColorOverride("font_color", highlight);

                // Need to redraw map if we have target markers
                GetNode<Map.MapManager>("/root/Main/MapManager")?.QueueRedraw();
            }
        };
        parent.AddChild(btn);
    }
}
