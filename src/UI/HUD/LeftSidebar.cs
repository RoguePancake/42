using Godot;

namespace Warship.UI.HUD;

/// <summary>
/// A strict, clean Left Sidebar matching the hand-drawn schematic.
/// Contains the core structural branches of the player's nation.
/// </summary>
public partial class LeftSidebar : Control
{
    public override void _Ready()
    {
        // Anchor to the Left edge of the screen, underneath the top bars, above the bottom bar
        SetAnchorsAndOffsetsPreset(LayoutPreset.LeftWide);
        CustomMinimumSize = new Vector2(250, 0); // 250px wide
        OffsetTop = 80; // Below News & TopBar
        OffsetBottom = 0; // Absolute bottom

        // Background
        var bg = new Panel();
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.11f, 0.13f, 1f), // Solid clean dark gray
            BorderColor = new Color(0.2f, 0.22f, 0.25f, 1f),
            BorderWidthRight = 2
        };
        bg.AddThemeStyleboxOverride("panel", style);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var vbox = new VBoxContainer();
        vbox.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 0);
        AddChild(vbox);

        // Header
        var header = new Label { Text = "CONTROL TABS AREA", HorizontalAlignment = HorizontalAlignment.Center };
        header.AddThemeFontSizeOverride("font_size", 14);
        header.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
        header.CustomMinimumSize = new Vector2(0, 30);
        header.VerticalAlignment = VerticalAlignment.Center;
        vbox.AddChild(header);
        vbox.AddChild(new HSeparator());

        // Buttons
        AddTabButton(vbox, "BRANCH'S", true);
        AddTabButton(vbox, "GOVERNMENT", true);
        vbox.AddChild(new HSeparator());
        AddTabButton(vbox, "ARMY", false);
        AddTabButton(vbox, "AIR FORCE", false);
        AddTabButton(vbox, "NAVY", false);
    }

    private void AddTabButton(VBoxContainer parent, string text, bool isTitle)
    {
        var btn = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, 50),
            Alignment = HorizontalAlignment.Left
        };
        
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 16);
        btn.AddChild(margin);

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.13f, 0.16f, 1f),
            BorderColor = new Color(0.15f, 0.17f, 0.2f, 1f),
            BorderWidthBottom = 1
        };
        
        var hover = (StyleBoxFlat)style.Duplicate();
        hover.BgColor = new Color(0.2f, 0.22f, 0.28f, 1f);

        btn.AddThemeStyleboxOverride("normal", style);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", hover);
        
        if (isTitle)
        {
            btn.AddThemeFontSizeOverride("font_size", 18);
            btn.AddThemeColorOverride("font_color", Colors.White);
        }
        else
        {
            btn.AddThemeFontSizeOverride("font_size", 16);
            btn.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.75f));
        }

        parent.AddChild(btn);
    }
}
