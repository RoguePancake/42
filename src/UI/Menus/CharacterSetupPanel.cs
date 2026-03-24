using Godot;
using Warship.Core;
using Warship.Data;

namespace Warship.UI.Menus;

public partial class CharacterSetupPanel : Control
{
    private OptionButton _nationDropdown = null!;
    private OptionButton _roleDropdown = null!;
    private LineEdit _nameInput = null!;
    private OptionButton _focusDropdown = null!;
    private Button _startButton = null!;

    private string[] _nations = { "United States", "China", "Russia", "European Union", "India", "United Kingdom" };
    
    private string[] _roles = {
        "Head of State",
        "Defense Minister",
        "Foreign Minister",
        "Director of Intelligence",
        "Chief of Staff",
        "Finance Minister",
        "Interior Minister",
        "Opposition Leader"
    };

    private string[] _focuses = {
        "Balanced",
        "Territory Control (+TA)",
        "Global Influence (+WA)",
        "Shadow Broker (+BSA)"
    };

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        
        // Full screen dark overlay
        var bg = new ColorRect {
            Color = new Color(0.02f, 0.02f, 0.05f, 0.98f),
            AnchorsPreset = (int)LayoutPreset.FullRect
        };
        AddChild(bg);

        var center = new CenterContainer {
            AnchorsPreset = (int)LayoutPreset.FullRect
        };
        AddChild(center);

        var panel = new PanelContainer();
        var style = new StyleBoxFlat {
            BgColor = new Color(0.1f, 0.12f, 0.16f, 1f),
            BorderColor = new Color(0.83f, 0.66f, 0.29f, 1f), // Gold #D4A84B
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            CornerRadiusTopLeft = 8, CornerRadiusBottomRight = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8
        };
        panel.AddThemeStyleboxOverride("panel", style);
        center.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 30);
        margin.AddThemeConstantOverride("margin_right", 30);
        margin.AddThemeConstantOverride("margin_top", 30);
        margin.AddThemeConstantOverride("margin_bottom", 30);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 15);
        margin.AddChild(vbox);

        var title = new Label { Text = "EXECUTIVE CLEARANCE REQUIRED" };
        title.AddThemeFontSizeOverride("font_size", 24);
        title.AddThemeColorOverride("font_color", new Color(0.83f, 0.66f, 0.29f, 1f));
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        vbox.AddChild(new HSeparator());

        // Target Nation
        vbox.AddChild(new Label { Text = "Select Nation:" });
        _nationDropdown = new OptionButton();
        foreach (var n in _nations) _nationDropdown.AddItem(n);
        // Default to UK (FreeState)
        _nationDropdown.Selected = 5;
        vbox.AddChild(_nationDropdown);

        // Role
        vbox.AddChild(new Label { Text = "Select Role:" });
        _roleDropdown = new OptionButton();
        foreach (var r in _roles) _roleDropdown.AddItem(r);
        _roleDropdown.Selected = 1; // Default to Defense Minister
        vbox.AddChild(_roleDropdown);

        // Name
        vbox.AddChild(new Label { Text = "Enter Name:" });
        _nameInput = new LineEdit { PlaceholderText = "E.g. J. Crawford", Text = "J. Crawford" };
        vbox.AddChild(_nameInput);

        // Focus
        vbox.AddChild(new Label { Text = "Strategic Focus:" });
        _focusDropdown = new OptionButton();
        foreach (var f in _focuses) _focusDropdown.AddItem(f);
        vbox.AddChild(_focusDropdown);

        vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });

        _startButton = new Button { Text = "INITIALIZE CLEARANCE" };
        _startButton.AddThemeFontSizeOverride("font_size", 18);
        var btnStyle = new StyleBoxFlat { BgColor = new Color(0.83f, 0.66f, 0.29f, 1f), CornerRadiusTopLeft = 4, CornerRadiusBottomRight = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4 };
        _startButton.AddThemeStyleboxOverride("normal", btnStyle);
        _startButton.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.1f, 1f));
        _startButton.Pressed += OnStartPressed;
        vbox.AddChild(_startButton);
    }

    private void OnStartPressed()
    {
        string selectedNation = _nations[_nationDropdown.Selected];
        string selectedRole = _roles[_roleDropdown.Selected];
        string playerName = string.IsNullOrWhiteSpace(_nameInput.Text) ? "Unknown Official" : _nameInput.Text;
        int focusIndex = _focusDropdown.Selected;

        // Pass this config to WorldStateManager
        WorldStateManager.Instance?.InitializeWorld(selectedNation, selectedRole, playerName, focusIndex);
        
        // Hide self
        QueueFree();
    }
}
