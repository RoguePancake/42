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

        var bg = new ColorRect
        {
            Color = new Color(UITheme.BgDarkest.R, UITheme.BgDarkest.G, UITheme.BgDarkest.B, 0.98f),
            AnchorsPreset = (int)LayoutPreset.FullRect
        };
        AddChild(bg);

        var center = new CenterContainer
        {
            AnchorsPreset = (int)LayoutPreset.FullRect
        };
        AddChild(center);

        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", UITheme.ModalWindowStyle(UITheme.AccentGold));
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
        title.AddThemeFontSizeOverride("font_size", UITheme.FontXL);
        title.AddThemeColorOverride("font_color", UITheme.AccentGold);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        vbox.AddChild(new HSeparator());

        AddFormLabel(vbox, "Select Nation:");
        _nationDropdown = new OptionButton();
        foreach (var n in _nations) _nationDropdown.AddItem(n);
        _nationDropdown.Selected = 5;
        vbox.AddChild(_nationDropdown);

        AddFormLabel(vbox, "Select Role:");
        _roleDropdown = new OptionButton();
        foreach (var r in _roles) _roleDropdown.AddItem(r);
        _roleDropdown.Selected = 1;
        vbox.AddChild(_roleDropdown);

        AddFormLabel(vbox, "Enter Name:");
        _nameInput = new LineEdit { PlaceholderText = "E.g. J. Crawford", Text = "J. Crawford" };
        vbox.AddChild(_nameInput);

        AddFormLabel(vbox, "Strategic Focus:");
        _focusDropdown = new OptionButton();
        foreach (var f in _focuses) _focusDropdown.AddItem(f);
        vbox.AddChild(_focusDropdown);

        vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });

        _startButton = new Button { Text = "INITIALIZE CLEARANCE" };
        UITheme.ApplyPrimaryButtonStyle(_startButton, UITheme.AccentGold);
        _startButton.AddThemeColorOverride("font_color", UITheme.BgDarkest);
        _startButton.Pressed += OnStartPressed;
        vbox.AddChild(_startButton);
    }

    private void AddFormLabel(VBoxContainer parent, string text)
    {
        var lbl = new Label { Text = text };
        lbl.AddThemeFontSizeOverride("font_size", UITheme.FontBody);
        lbl.AddThemeColorOverride("font_color", UITheme.TextSecondary);
        parent.AddChild(lbl);
    }

    private void OnStartPressed()
    {
        string selectedNation = _nations[_nationDropdown.Selected];
        string selectedRole = _roles[_roleDropdown.Selected];
        string playerName = string.IsNullOrWhiteSpace(_nameInput.Text) ? "Unknown Official" : _nameInput.Text;
        int focusIndex = _focusDropdown.Selected;

        WorldStateManager.Instance?.InitializeWorld(selectedNation, selectedRole, playerName, focusIndex);

        QueueFree();
    }
}
