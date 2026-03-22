using Godot;

namespace Warship.UI.HUD;

public partial class VictoryPanel : Control
{
    private ColorRect _dimBg = null!;
    private PanelContainer _window = null!;

    public override void _Ready()
    {
        AnchorsPreset = (int)LayoutPreset.FullRect;
        MouseFilter = MouseFilterEnum.Stop; // Blocks input to map
        Visible = false; // Hidden at start
        
        _dimBg = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.85f),
            MouseFilter = MouseFilterEnum.Stop
        };
        _dimBg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_dimBg);

        _window = new PanelContainer();
        _window.SetAnchorsPreset(LayoutPreset.Center, true);
        _window.CustomMinimumSize = new Vector2(600, 400);

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.1f, 0.95f),
            BorderColor = Colors.Crimson,
            BorderWidthLeft = 4, BorderWidthTop = 4,
            BorderWidthRight = 4, BorderWidthBottom = 4,
            CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12,
            ShadowColor = new Color(0, 0, 0, 0.6f),
            ShadowSize = 25,
            ContentMarginLeft = 40, ContentMarginRight = 40,
            ContentMarginTop = 40, ContentMarginBottom = 40
        };
        _window.AddThemeStyleboxOverride("panel", style);
        AddChild(_window);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 30);
        _window.AddChild(vbox);

        var title = new Label
        {
            Text = "FULL AUTHORITY ACHIEVED",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 36);
        title.AddThemeColorOverride("font_color", Colors.Crimson);
        vbox.AddChild(title);
        
        vbox.AddChild(new HSeparator());

        var desc = new Label
        {
            Text = "The shadows bend to your command. The military marches at your word. And the world trembles at your state. You have consolidated unchecked power.\n\nYou have achieved Full Authority.\nThe Nation is Yours.",
            AutowrapMode = TextServer.AutowrapMode.Word,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        desc.AddThemeFontSizeOverride("font_size", 18);
        desc.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.9f));
        vbox.AddChild(desc);

        var hbox = new HBoxContainer();
        hbox.Alignment = BoxContainer.AlignmentMode.Center;
        hbox.AddThemeConstantOverride("separation", 20);
        vbox.AddChild(hbox);

        var btnContinue = new Button { Text = "CONTINUE SIMULATION", CustomMinimumSize = new Vector2(250, 50) };
        btnContinue.AddThemeFontSizeOverride("font_size", 16);
        
        var styleBtn = new StyleBoxFlat { BgColor = new Color(0.2f, 0.2f, 0.25f), BorderColor = Colors.DarkGray, BorderWidthBottom = 2, CornerRadiusTopLeft = 4, CornerRadiusBottomRight = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4 };
        var hoverBtn = (StyleBoxFlat)styleBtn.Duplicate();
        hoverBtn.BgColor = new Color(0.3f, 0.3f, 0.4f);
        
        btnContinue.AddThemeStyleboxOverride("normal", styleBtn);
        btnContinue.AddThemeStyleboxOverride("hover", hoverBtn);
        btnContinue.AddThemeStyleboxOverride("pressed", hoverBtn);
        
        btnContinue.Pressed += () => Visible = false; // Just hide and keep playing
        hbox.AddChild(btnContinue);

        var btnExit = new Button { Text = "MAIN MENU", CustomMinimumSize = new Vector2(150, 50) };
        btnExit.AddThemeFontSizeOverride("font_size", 16);
        btnExit.AddThemeStyleboxOverride("normal", styleBtn);
        btnExit.AddThemeStyleboxOverride("hover", hoverBtn);
        btnExit.AddThemeStyleboxOverride("pressed", hoverBtn);
        
        btnExit.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
        hbox.AddChild(btnExit);
    }

    public void ShowVictory()
    {
        Visible = true;
    }
}
