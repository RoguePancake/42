using Godot;

namespace Warship.UI.Menus;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        AnchorsPreset = (int)LayoutPreset.FullRect;

        var bg = new ColorRect
        {
            Color = UITheme.BgDarkest,
            MouseFilter = MouseFilterEnum.Ignore
        };
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // Subtle accent line at top
        var topLine = new ColorRect
        {
            Color = UITheme.AccentCrimson,
            CustomMinimumSize = new Vector2(0, 3)
        };
        topLine.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        AddChild(topLine);

        var title = new Label
        {
            Text = "FULL AUTHORITY",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", UITheme.FontHuge);
        title.AddThemeColorOverride("font_color", UITheme.AccentCrimson);
        title.SetAnchorsPreset(LayoutPreset.TopWide, true);
        title.OffsetTop = 150;
        title.OffsetBottom = 250;
        AddChild(title);

        var subtitle = new Label
        {
            Text = "GEOPOLITICAL THRILLER SIMULATOR",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        subtitle.AddThemeFontSizeOverride("font_size", UITheme.FontLarge);
        subtitle.AddThemeColorOverride("font_color", UITheme.TextDim);
        subtitle.SetAnchorsPreset(LayoutPreset.TopWide, true);
        subtitle.OffsetTop = 245;
        AddChild(subtitle);

        // Buttons
        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.Center, true);
        vbox.AddThemeConstantOverride("separation", UITheme.PaddingLarge);
        vbox.CustomMinimumSize = new Vector2(320, 0);
        vbox.OffsetTop = 50;
        AddChild(vbox);

        var btnStart = CreateMenuButton("NEW CAMPAIGN", UITheme.CatEconomic);
        btnStart.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
        vbox.AddChild(btnStart);

        var btnContinue = CreateMenuButton("CONTINUE SAVED GAME", UITheme.TextDim);
        btnContinue.Disabled = true;
        vbox.AddChild(btnContinue);

        var btnExit = CreateMenuButton("EXIT TO DESKTOP", UITheme.AccentCrimson);
        btnExit.Pressed += () => GetTree().Quit();
        vbox.AddChild(btnExit);

        var version = new Label
        {
            Text = "v0.8.0 - Prototype Build",
            HorizontalAlignment = HorizontalAlignment.Right
        };
        version.AddThemeFontSizeOverride("font_size", UITheme.FontBody);
        version.AddThemeColorOverride("font_color", UITheme.TextDim);
        version.SetAnchorsPreset(LayoutPreset.BottomWide, true);
        version.OffsetTop = -30;
        version.OffsetRight = -20;
        AddChild(version);

        GD.Print("[MainMenu] Initialized and waiting for commander.");
    }

    private Button CreateMenuButton(string text, Color accentColor)
    {
        var btn = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, 54)
        };

        var normal = new StyleBoxFlat
        {
            BgColor = UITheme.BgElevated,
            BorderColor = UITheme.BorderMedium,
            BorderWidthBottom = UITheme.BorderThick,
            CornerRadiusTopLeft = UITheme.CornerRadius,
            CornerRadiusTopRight = UITheme.CornerRadius,
            CornerRadiusBottomLeft = UITheme.CornerRadius,
            CornerRadiusBottomRight = UITheme.CornerRadius,
            ContentMarginLeft = UITheme.PaddingMedium,
            ContentMarginRight = UITheme.PaddingMedium,
            ContentMarginTop = UITheme.PaddingSmall,
            ContentMarginBottom = UITheme.PaddingSmall
        };

        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = accentColor * 0.35f;
        hover.BorderColor = accentColor;

        var pressed = (StyleBoxFlat)normal.Duplicate();
        pressed.BgColor = accentColor * 0.2f;
        pressed.BorderColor = accentColor;

        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", pressed);
        btn.AddThemeStyleboxOverride("focus", hover);
        btn.AddThemeFontSizeOverride("font_size", UITheme.FontLarge);
        btn.AddThemeColorOverride("font_color", UITheme.TextPrimary);
        btn.AddThemeColorOverride("font_hover_color", Colors.White);

        return btn;
    }
}
