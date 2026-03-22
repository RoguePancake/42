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
        // Full screen blocking overlay
        AnchorsPreset = (int)LayoutPreset.FullRect;
        MouseFilter = MouseFilterEnum.Stop; // Blocks clicks to the map
        Visible = false; // Hidden at start

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

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.08f, 0.08f, 0.95f),
            BorderColor = Colors.DarkRed,
            BorderWidthLeft = 4, BorderWidthTop = 4,
            BorderWidthRight = 4, BorderWidthBottom = 4,
            CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12,
            ShadowColor = new Color(0, 0, 0, 0.6f),
            ShadowSize = 10,
            ContentMarginLeft = 24, ContentMarginRight = 24,
            ContentMarginTop = 24, ContentMarginBottom = 24
        };
        window.AddThemeStyleboxOverride("panel", style);
        center.AddChild(window);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 20);
        window.AddChild(vbox);

        _titleLabel = new Label
        {
            Text = "CRISIS",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 28);
        _titleLabel.AddThemeColorOverride("font_color", Colors.Red);
        vbox.AddChild(_titleLabel);
        
        vbox.AddChild(new HSeparator());

        _descLabel = new Label
        {
            Text = "Description goes here.",
            AutowrapMode = TextServer.AutowrapMode.Word,
            CustomMinimumSize = new Vector2(400, 100)
        };
        _descLabel.AddThemeFontSizeOverride("font_size", 16);
        _descLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.95f));
        vbox.AddChild(_descLabel);

        _choiceBox = new VBoxContainer();
        _choiceBox.AddThemeConstantOverride("separation", 10);
        vbox.AddChild(_choiceBox);

        EventBus.Instance?.Subscribe<CrisisTriggeredEvent>(OnCrisisTriggered);
    }

    private void OnCrisisTriggered(CrisisTriggeredEvent ev)
    {
        // Must defer to main thread
        CallDeferred(nameof(ShowCrisis), ev.CrisisId, ev.Title, ev.Description, ev.Choices);
    }

    private void ShowCrisis(string crisisId, string title, string desc, string[] choices)
    {
        _currentCrisisId = crisisId;
        _titleLabel.Text = title;
        _descLabel.Text = desc;

        // Clear old buttons
        foreach (var child in _choiceBox.GetChildren())
        {
            child.QueueFree();
        }

        // Create new buttons
        for (int i = 0; i < choices.Length; i++)
        {
            int choiceIndex = i; // capture
            var btn = new Button { Text = choices[i], CustomMinimumSize = new Vector2(0, 44) };
            
            var style = new StyleBoxFlat { BgColor = new Color(0.2f, 0.15f, 0.15f), BorderColor = Colors.DarkRed, BorderWidthBottom = 2, CornerRadiusTopLeft=6, CornerRadiusBottomRight=6, CornerRadiusTopRight=6, CornerRadiusBottomLeft=6 };
            var hover = (StyleBoxFlat)style.Duplicate();
            hover.BgColor = new Color(0.4f, 0.2f, 0.2f);
            
            btn.AddThemeStyleboxOverride("normal", style);
            btn.AddThemeStyleboxOverride("hover", hover);
            
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
