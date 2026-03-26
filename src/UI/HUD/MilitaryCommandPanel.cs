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
        // Position: floating panel just right of LeftSidebar, above BottomPanel
        SetAnchorsAndOffsetsPreset(LayoutPreset.BottomLeft);
        OffsetTop = -(UITheme.BottomPanelHeight + UITheme.SidebarGap + 200);
        OffsetBottom = -(UITheme.BottomPanelHeight + UITheme.SidebarGap);
        OffsetLeft = UITheme.LeftSidebarWidth + UITheme.SidebarGap;
        OffsetRight = UITheme.LeftSidebarWidth + UITheme.SidebarGap + 220;

        var bg = new Panel();
        var style = new StyleBoxFlat
        {
            BgColor = new Color(UITheme.BgSurface.R, UITheme.BgSurface.G, UITheme.BgSurface.B, 0.95f),
            BorderColor = UITheme.BorderAccent,
            BorderWidthTop = UITheme.BorderMediumW,
            BorderWidthRight = UITheme.BorderMediumW,
            CornerRadiusTopRight = UITheme.CornerRadiusLg
        };
        bg.AddThemeStyleboxOverride("panel", style);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        margin.AddChild(vbox);

        var title = new Label { Text = "ARMY COMMAND" };
        title.AddThemeFontSizeOverride("font_size", UITheme.FontMedium);
        title.AddThemeColorOverride("font_color", UITheme.TextAccent);
        vbox.AddChild(title);

        _statusLabel = new Label { Text = "Current: Standby" };
        _statusLabel.AddThemeFontSizeOverride("font_size", UITheme.FontSmall);
        _statusLabel.AddThemeColorOverride("font_color", UITheme.TextDim);
        vbox.AddChild(_statusLabel);

        vbox.AddChild(new HSeparator());

        AddButton(vbox, "Border Watch", MilitaryOrder.BorderWatch, UITheme.CatEconomic);
        AddButton(vbox, "Patrol", MilitaryOrder.Patrol, UITheme.CatDiplomatic);
        AddButton(vbox, "Stage Army", MilitaryOrder.Stage, UITheme.AccentBlue);
        AddButton(vbox, "ATTACK", MilitaryOrder.Attack, UITheme.CatMilitary);
    }

    private void AddButton(VBoxContainer parent, string text, MilitaryOrder order, Color highlight)
    {
        var btn = new Button { Text = text, CustomMinimumSize = new Vector2(0, UITheme.ButtonHeightSm) };
        UITheme.ApplyButtonStyle(btn, highlight);

        btn.Pressed += () =>
        {
            var world = WorldStateManager.Instance?.Data;
            if (world != null)
            {
                int pIdx = int.Parse(world.PlayerNationId!.Split('_')[1]);
                var nation = world.Nations[pIdx];
                nation.GlobalMilitaryOrder = order;
                _statusLabel.Text = $"Current: {order}";
                _statusLabel.RemoveThemeColorOverride("font_color");
                _statusLabel.AddThemeColorOverride("font_color", highlight);
            }
        };
        parent.AddChild(btn);
    }
}
