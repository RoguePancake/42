using Godot;

namespace Warship.UI.HUD;

/// <summary>
/// A strict, clean Bottom Panel matching the hand-drawn schematic.
/// Displays Custom Data, Graphs, Maps, and More.
/// </summary>
public partial class BottomPanel : Control
{
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
        OffsetTop = -200; // 200px tall
        OffsetBottom = 0; // sits flush at the very bottom
        OffsetLeft = 250; // clears the Left Sidebar
        OffsetRight = -250; // clears the Right Sidebar

        var bg = new Panel();
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.11f, 0.13f, 1f), // Solid clean dark gray
            BorderColor = new Color(0.2f, 0.22f, 0.25f, 1f),
            BorderWidthTop = 2
        };
        bg.AddThemeStyleboxOverride("panel", style);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var header = new Label { Text = "CUSTOM DATA, GRAPHS, MAPS, AND MORE", HorizontalAlignment = HorizontalAlignment.Center };
        header.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        header.AddThemeFontSizeOverride("font_size", 20);
        header.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
        header.VerticalAlignment = VerticalAlignment.Center;
        
        AddChild(header);
    }
}
