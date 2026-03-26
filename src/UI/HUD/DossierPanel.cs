using Godot;
using System.Linq;
using Warship.Data;
using Warship.Core;
using Warship.Events;

namespace Warship.UI.HUD;

/// <summary>
/// Side panel that slides open when a Character (VIP) is clicked on the map.
/// Shows their dossier: name, role, nation, authority meters, and action buttons.
/// </summary>
public partial class DossierPanel : Control
{
    private Panel _bg = null!;
    private VBoxContainer _content = null!;
    private Label _nameLabel = null!;
    private Label _roleLabel = null!;
    private Label _nationLabel = null!;
    private ProgressBar _taBar = null!;
    private ProgressBar _waBar = null!;
    private ProgressBar _bsaBar = null!;
    private Label _faiLabel = null!;
    private VBoxContainer _actionBox = null!;
    private Button _closeBtn = null!;

    private CharacterData? _target;
    private bool _isVisible = false;
    private const float PanelWidth = 300f;

    public override void _Ready()
    {
        Size = new Vector2(PanelWidth, 600);
        Position = new Vector2(GetViewportRect().Size.X, UITheme.TopBarsTotal);

        _bg = new Panel();
        var styleBg = new StyleBoxFlat
        {
            BgColor = new Color(UITheme.BgPanel.R, UITheme.BgPanel.G, UITheme.BgPanel.B, 0.97f),
            BorderColor = UITheme.AccentBlueDim,
            BorderWidthLeft = UITheme.BorderThick,
            CornerRadiusTopLeft = UITheme.CornerRadiusLg,
            CornerRadiusBottomLeft = UITheme.CornerRadiusLg
        };
        _bg.AddThemeStyleboxOverride("panel", styleBg);
        _bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_bg);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", UITheme.PaddingMedium);
        margin.AddThemeConstantOverride("margin_right", UITheme.PaddingMedium);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(margin);

        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 10);
        margin.AddChild(_content);

        // Header
        var headerBox = new HBoxContainer();
        _content.AddChild(headerBox);

        _nameLabel = new Label { Text = "???" };
        _nameLabel.AddThemeFontSizeOverride("font_size", UITheme.FontXL);
        _nameLabel.AddThemeColorOverride("font_color", UITheme.TextPrimary);
        _nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        headerBox.AddChild(_nameLabel);

        _closeBtn = new Button { Text = "X", CustomMinimumSize = new Vector2(32, 32) };
        UITheme.ApplyButtonStyle(_closeBtn);
        _closeBtn.Pressed += () => Hide();
        headerBox.AddChild(_closeBtn);

        // Role & Nation
        _roleLabel = new Label { Text = "Role: ---" };
        _roleLabel.AddThemeFontSizeOverride("font_size", UITheme.FontMedium);
        _roleLabel.AddThemeColorOverride("font_color", UITheme.TextSecondary);
        _content.AddChild(_roleLabel);

        _nationLabel = new Label { Text = "Nation: ---" };
        _nationLabel.AddThemeFontSizeOverride("font_size", UITheme.FontMedium);
        _nationLabel.AddThemeColorOverride("font_color", UITheme.TextDim);
        _content.AddChild(_nationLabel);

        _content.AddChild(new HSeparator());

        // Authority meters
        _content.AddChild(MakeBarSection("Territory Authority (TA)", out _taBar, UITheme.CatEconomic));
        _content.AddChild(MakeBarSection("World Authority (WA)", out _waBar, UITheme.CatDiplomatic));
        _content.AddChild(MakeBarSection("Shadow Authority (BSA)", out _bsaBar, UITheme.CatIntelligence));

        _faiLabel = new Label { Text = "Full Authority Index: 0%" };
        _faiLabel.AddThemeFontSizeOverride("font_size", UITheme.FontLarge);
        _faiLabel.AddThemeColorOverride("font_color", UITheme.AccentGold);
        _faiLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _content.AddChild(_faiLabel);

        _content.AddChild(new HSeparator());

        // Actions
        var actionsLabel = new Label { Text = "Actions" };
        actionsLabel.AddThemeFontSizeOverride("font_size", UITheme.FontMedium);
        actionsLabel.AddThemeColorOverride("font_color", UITheme.TextPrimary);
        _content.AddChild(actionsLabel);

        _actionBox = new VBoxContainer();
        _actionBox.AddThemeConstantOverride("separation", 6);
        _content.AddChild(_actionBox);

        EventBus.Instance?.Subscribe<AuthorityChangedEvent>(ev => {
            if (_target != null && (ev.CharacterId == _target.Id || _target.IsPlayer))
                CallDeferred(nameof(RefreshDisplay));
        });
    }

    private void RefreshDisplay()
    {
        if (_target != null) ShowCharacter(_target);
    }

    private VBoxContainer MakeBarSection(string label, out ProgressBar bar, Color color)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 2);

        var lbl = new Label { Text = label };
        lbl.AddThemeFontSizeOverride("font_size", UITheme.FontSmall);
        lbl.AddThemeColorOverride("font_color", UITheme.TextSecondary);
        box.AddChild(lbl);

        bar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 0,
            CustomMinimumSize = new Vector2(0, 20),
            ShowPercentage = true
        };
        UITheme.ApplyBarStyle(bar, color);
        box.AddChild(bar);

        return box;
    }

    public void ShowCharacter(CharacterData character)
    {
        _target = character;
        _isVisible = true;

        _nameLabel.Text = character.Name;
        _roleLabel.Text = $"Role: {character.Role}";

        var world = WorldStateManager.Instance?.Data;
        if (world != null)
        {
            int natIdx = int.Parse(character.NationId.Split('_')[1]);
            _nationLabel.Text = $"Nation: {world.Nations[natIdx].Name}";
        }

        _taBar.Value = character.TerritoryAuthority;
        _waBar.Value = character.WorldAuthority;
        _bsaBar.Value = character.BehindTheScenesAuthority;
        _faiLabel.Text = $"Full Authority Index: {character.FullAuthorityIndex:0.0}%";

        foreach (var child in _actionBox.GetChildren())
            child.QueueFree();

        if (character.IsPlayer)
        {
            AddActionButton("Review Intel", "Analyze your own networks");
            AddActionButton("Fund Militia (+TA)", "Spend resources to boost local control");
            AddActionButton("Public Address (+WA)", "Broadcast to raise world standing");
        }
        else
        {
            AddActionButton("Investigate", "Gather intelligence on this target");
            AddActionButton("Bribe", "Attempt to buy their loyalty");
            AddActionButton("Threaten", "Intimidate them into compliance");
            AddActionButton("Eliminate", "Arrange an... accident");
        }
    }

    private void AddActionButton(string text, string tooltip)
    {
        var btn = new Button
        {
            Text = text,
            TooltipText = tooltip,
            CustomMinimumSize = new Vector2(0, UITheme.ButtonHeightSm)
        };
        UITheme.ApplyButtonStyle(btn, UITheme.AccentBlueDim);

        btn.Pressed += () => OnActionPressed(text);
        _actionBox.AddChild(btn);
    }

    private void OnActionPressed(string action)
    {
        if (_target == null) return;

        var world = WorldStateManager.Instance?.Data;
        if (world == null) return;

        var player = world.Characters.FirstOrDefault(c => c.IsPlayer);
        if (player == null) return;

        string commandType = action switch
        {
            "Review Intel" => "review_intel",
            "Fund Militia (+TA)" => "fund_militia",
            "Public Address (+WA)" => "public_address",
            "Investigate" => "investigate",
            "Bribe" => "bribe",
            "Threaten" => "threaten",
            "Eliminate" => "eliminate",
            _ => "unknown"
        };

        if (commandType != "unknown")
            EventBus.Instance?.Publish(new PoliticalActionEvent(player.Id, _target.Id, commandType));
    }

    public new void Hide()
    {
        _isVisible = false;
        _target = null;
    }

    public override void _Process(double delta)
    {
        float dockX = GetViewportRect().Size.X - UITheme.RightSidebarWidth - PanelWidth;
        float hiddenX = GetViewportRect().Size.X - UITheme.RightSidebarWidth;
        float target = _isVisible ? dockX : hiddenX;
        float newX = Mathf.Lerp(Position.X, target, 8f * (float)delta);
        Position = new Vector2(newX, UITheme.TopBarsTotal);
    }
}
