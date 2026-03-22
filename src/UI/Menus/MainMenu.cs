using Godot;

namespace Warship.UI.Menus;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        // Setup pure code-driven UI to ensure it launches perfectly
        AnchorsPreset = (int)LayoutPreset.FullRect;

        // Deep red/black background
        var bg = new ColorRect
        {
            Color = new Color(0.05f, 0.05f, 0.08f), // Very dark blue/black
            MouseFilter = MouseFilterEnum.Ignore
        };
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // Huge Title Label
        var title = new Label
        {
            Text = "FULL AUTHORITY",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 72);
        title.AddThemeColorOverride("font_color", Colors.Crimson);
        title.SetAnchorsPreset(LayoutPreset.TopWide, true);
        title.OffsetTop = 150;
        title.OffsetBottom = 250;
        AddChild(title);

        var subtitle = new Label
        {
            Text = "GEOPOLITICAL THRILLER SIMULATOR",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        subtitle.AddThemeFontSizeOverride("font_size", 20);
        subtitle.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.7f));
        subtitle.SetAnchorsPreset(LayoutPreset.TopWide, true);
        subtitle.OffsetTop = 240;
        AddChild(subtitle);

        // VBox for Buttons
        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.Center, true);
        vbox.AddThemeConstantOverride("separation", 24);
        vbox.CustomMinimumSize = new Vector2(300, 0);
        vbox.OffsetTop = 50; // Push down a bit from true center
        AddChild(vbox);

        var btnStart = CreateMenuButton("NEW CAMPAIGN", Colors.LightGreen);
        btnStart.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
        vbox.AddChild(btnStart);

        var btnContinue = CreateMenuButton("CONTINUE SAVED GAME", Colors.Gray);
        btnContinue.Disabled = true; // For later
        vbox.AddChild(btnContinue);

        var btnExit = CreateMenuButton("EXIT TO DESKTOP", Colors.Crimson);
        btnExit.Pressed += () => GetTree().Quit();
        vbox.AddChild(btnExit);
        
        // Version string
        var version = new Label
        {
            Text = "v0.8.0 - Prototype Build",
            HorizontalAlignment = HorizontalAlignment.Right
        };
        version.AddThemeFontSizeOverride("font_size", 14);
        version.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.5f));
        version.SetAnchorsPreset(LayoutPreset.BottomWide, true);
        version.OffsetTop = -30;
        version.OffsetRight = -20;
        AddChild(version);
        
        GD.Print("[MainMenu] Initialized and waiting for commander.");
    }

    private Button CreateMenuButton(string text, Color hoverHighlight)
    {
        var btn = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, 50)
        };
        btn.AddThemeFontSizeOverride("font_size", 20);
        
        var normal = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.12f, 0.15f, 0.9f),
            BorderColor = new Color(0.3f, 0.3f, 0.4f),
            BorderWidthBottom = 4,
            CornerRadiusTopLeft = 4, CornerRadiusBottomRight = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4
        };
        
        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = hoverHighlight * 0.4f;
        hover.BorderColor = hoverHighlight;
        
        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", hover);
        
        return btn;
    }
}
