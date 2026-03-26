using Godot;

namespace Warship.UI.HUD;

public partial class VictoryPanel : Control
{
    private ColorRect _dimBg = null!;
    private PanelContainer _window = null!;

    public override void _Ready()
    {
        AnchorsPreset = (int)LayoutPreset.FullRect;
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;

        _dimBg = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.85f),
            MouseFilter = MouseFilterEnum.Stop
        };
        _dimBg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_dimBg);

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        _window = new PanelContainer();
        _window.CustomMinimumSize = new Vector2(600, 400);
        _window.AddThemeStyleboxOverride("panel", UITheme.ModalWindowStyle(UITheme.AccentCrimson));
        center.AddChild(_window);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 30);
        _window.AddChild(vbox);

        var title = new Label
        {
            Text = "FULL AUTHORITY ACHIEVED",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", UITheme.FontTitle);
        title.AddThemeColorOverride("font_color", UITheme.AccentCrimson);
        vbox.AddChild(title);

        vbox.AddChild(new HSeparator());

        var desc = new Label
        {
            Text = "The shadows bend to your command. The military marches at your word. And the world trembles at your state. You have consolidated unchecked power.\n\nYou have achieved Full Authority.\nThe Nation is Yours.",
            AutowrapMode = TextServer.AutowrapMode.Word,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        desc.AddThemeFontSizeOverride("font_size", UITheme.FontMedium);
        desc.AddThemeColorOverride("font_color", UITheme.TextPrimary);
        vbox.AddChild(desc);

        var hbox = new HBoxContainer();
        hbox.Alignment = BoxContainer.AlignmentMode.Center;
        hbox.AddThemeConstantOverride("separation", UITheme.PaddingLarge);
        vbox.AddChild(hbox);

        var btnContinue = new Button { Text = "CONTINUE SIMULATION", CustomMinimumSize = new Vector2(250, 50) };
        UITheme.ApplyPrimaryButtonStyle(btnContinue, UITheme.AccentBlueDim);
        btnContinue.Pressed += () => Visible = false;
        hbox.AddChild(btnContinue);

        var btnExit = new Button { Text = "MAIN MENU", CustomMinimumSize = new Vector2(150, 50) };
        UITheme.ApplyButtonStyle(btnExit);
        btnExit.AddThemeFontSizeOverride("font_size", UITheme.FontMedium);
        btnExit.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
        hbox.AddChild(btnExit);
    }

    public void ShowVictory()
    {
        Visible = true;
    }
}
