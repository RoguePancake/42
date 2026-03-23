using Godot;

namespace Warship.UI.HUD;

/// <summary>
/// A strict, clean Right Sidebar matching the hand-drawn schematic.
/// Contains the Spy Network and drops down to more controls.
/// </summary>
public partial class RightSidebar : Control
{
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.RightWide);
        CustomMinimumSize = new Vector2(250, 0); // 250px wide
        OffsetTop = 80; // Below News & TopBar
        OffsetBottom = 0; // Absolute bottom

        var bg = new Panel();
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.11f, 0.13f, 1f),
            BorderColor = new Color(0.2f, 0.22f, 0.25f, 1f),
            BorderWidthLeft = 2
        };
        bg.AddThemeStyleboxOverride("panel", style);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var vbox = new VBoxContainer();
        vbox.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 0);
        AddChild(vbox);

        var header = new Label { Text = "MORE CONTROL TABS", HorizontalAlignment = HorizontalAlignment.Center };
        header.AddThemeFontSizeOverride("font_size", 14);
        header.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
        header.CustomMinimumSize = new Vector2(0, 40);
        header.VerticalAlignment = VerticalAlignment.Center;
        vbox.AddChild(header);
        
        var arrow = new Label { Text = "v\n|\nv", HorizontalAlignment = HorizontalAlignment.Center };
        arrow.AddThemeFontSizeOverride("font_size", 12);
        arrow.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.5f));
        vbox.AddChild(arrow);
        
        vbox.AddChild(new HSeparator());

        var btnSpy = new Button { Text = "SPY NETWORK", CustomMinimumSize = new Vector2(0, 50) };
        var styleBtn = new StyleBoxFlat { BgColor = new Color(0.12f, 0.13f, 0.16f, 1f), BorderColor = new Color(0.15f, 0.17f, 0.2f, 1f), BorderWidthBottom = 1 };
        var hoverBtn = (StyleBoxFlat)styleBtn.Duplicate();
        hoverBtn.BgColor = new Color(0.18f, 0.2f, 0.25f, 1f);
        
        btnSpy.AddThemeStyleboxOverride("normal", styleBtn);
        btnSpy.AddThemeStyleboxOverride("hover", hoverBtn);
        btnSpy.AddThemeStyleboxOverride("pressed", hoverBtn);
        btnSpy.AddThemeFontSizeOverride("font_size", 16);
        btnSpy.AddThemeColorOverride("font_color", Colors.White);
        vbox.AddChild(btnSpy);
    }
}
