using Godot;
using Warship.Core;
using Warship.Events;

namespace Warship.UI.HUD;

public partial class CrisisPanel : Control
{
    private Panel _bg = null!;
    private Label _titleLabel = null!;
    private Label _descLabel = null!;
    private VBoxContainer _choiceBox = null!;

    private string _currentCrisisId = "";

    public override void _Ready()
    {
        AnchorsPreset = (int)LayoutPreset.FullRect;
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;

        var dimBg = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.7f),
            MouseFilter = MouseFilterEnum.Stop
        };
        dimBg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(dimBg);

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var window = new PanelContainer();
        window.CustomMinimumSize = new Vector2(500, 350);
        window.AddThemeStyleboxOverride("panel", UITheme.ModalWindowStyle(UITheme.AccentCrimson));
        center.AddChild(window);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 20);
        window.AddChild(vbox);

        _titleLabel = new Label
        {
            Text = "CRISIS",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", UITheme.FontTitle);
        _titleLabel.AddThemeColorOverride("font_color", UITheme.AccentCrimson);
        vbox.AddChild(_titleLabel);

        vbox.AddChild(new HSeparator());

        _descLabel = new Label
        {
            Text = "Description goes here.",
            AutowrapMode = TextServer.AutowrapMode.Word,
            CustomMinimumSize = new Vector2(400, 100)
        };
        _descLabel.AddThemeFontSizeOverride("font_size", UITheme.FontMedium);
        _descLabel.AddThemeColorOverride("font_color", UITheme.TextPrimary);
        vbox.AddChild(_descLabel);

        _choiceBox = new VBoxContainer();
        _choiceBox.AddThemeConstantOverride("separation", 10);
        vbox.AddChild(_choiceBox);

        EventBus.Instance?.Subscribe<CrisisTriggeredEvent>(OnCrisisTriggered);
    }

    private void OnCrisisTriggered(CrisisTriggeredEvent ev)
    {
        CallDeferred(nameof(ShowCrisis), ev.CrisisId, ev.Title, ev.Description, ev.Choices);
    }

    private void ShowCrisis(string crisisId, string title, string desc, string[] choices)
    {
        _currentCrisisId = crisisId;
        _titleLabel.Text = title;
        _descLabel.Text = desc;

        foreach (var child in _choiceBox.GetChildren())
            child.QueueFree();

        for (int i = 0; i < choices.Length; i++)
        {
            int choiceIndex = i;
            var btn = new Button { Text = choices[i], CustomMinimumSize = new Vector2(0, 44) };
            UITheme.ApplyButtonStyle(btn, UITheme.AccentCrimson);

            btn.Pressed += () => ResolveCrisis(choiceIndex);
            _choiceBox.AddChild(btn);
        }

        Visible = true;
    }

    private void ResolveCrisis(int choiceIndex)
    {
        Visible = false;
        EventBus.Instance?.Publish(new CrisisResolvedEvent(_currentCrisisId, choiceIndex));
    }
}
