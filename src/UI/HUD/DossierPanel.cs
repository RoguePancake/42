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
    private VBoxContainer _actionBox = null!;
    private Button _closeBtn = null!;

    private CharacterData? _target;
    private bool _isVisible = false;
    private float _slideProgress = 0f;
    private const float PanelWidth = 300f;
    private const float RightSidebarWidth = 250f;

    public override void _Ready()
    {
        // Start off-screen to the right, accounting for RightSidebar
        Size = new Vector2(PanelWidth, 600);
        Position = new Vector2(GetViewportRect().Size.X, 64); // Below both top bars

        // Dark background panel
        _bg = new Panel();
        var styleBg = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.06f, 0.10f, 0.96f),
            BorderColor = new Color(0.15f, 0.35f, 0.7f, 1f),
            BorderWidthLeft = 3,
            CornerRadiusTopLeft = 8,
            CornerRadiusBottomLeft = 8
        };
        _bg.AddThemeStyleboxOverride("panel", styleBg);
        _bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_bg);

        // Content container
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(margin);

        _content = new VBoxContainer();
        _content.AddThemeConstantOverride("separation", 10);
        margin.AddChild(_content);

        // === HEADER ===
        var headerBox = new HBoxContainer();
        _content.AddChild(headerBox);

        _nameLabel = new Label { Text = "???" };
        _nameLabel.AddThemeFontSizeOverride("font_size", 22);
        _nameLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        headerBox.AddChild(_nameLabel);

        _closeBtn = new Button { Text = "✕", CustomMinimumSize = new Vector2(32, 32) };
        _closeBtn.Pressed += () => Hide();
        headerBox.AddChild(_closeBtn);

        // === ROLE & NATION ===
        _roleLabel = new Label { Text = "Role: ---" };
        _roleLabel.AddThemeFontSizeOverride("font_size", 16);
        _roleLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.85f));
        _content.AddChild(_roleLabel);

        _nationLabel = new Label { Text = "Nation: ---" };
        _nationLabel.AddThemeFontSizeOverride("font_size", 16);
        _nationLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.75f));
        _content.AddChild(_nationLabel);

        // Separator
        _content.AddChild(new HSeparator());

        // Separator
        _content.AddChild(new HSeparator());

        // === ACTION BUTTONS ===
        var actionsLabel = new Label { Text = "Actions" };
        actionsLabel.AddThemeFontSizeOverride("font_size", 18);
        actionsLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.9f));
        _content.AddChild(actionsLabel);

        _actionBox = new VBoxContainer();
        _actionBox.AddThemeConstantOverride("separation", 6);
        _content.AddChild(_actionBox);

        // Listen for turn changes to update display
        EventBus.Instance?.Subscribe<TurnAdvancedEvent>(_ => {
            if (_target != null)
            {
                CallDeferred(nameof(RefreshDisplay));
            }
        });
    }

    private void RefreshDisplay()
    {
        if (_target != null) ShowCharacter(_target);
    }

    /// <summary>Open the dossier for a specific character.</summary>
    public void ShowCharacter(CharacterData character)
    {
        _target = character;
        _isVisible = true;

        // Update labels
        _nameLabel.Text = character.Name;
        _roleLabel.Text = $"Role: {character.Role}";

        // Find nation name
        var world = WorldStateManager.Instance?.Data;
        if (world != null)
        {
            int natIdx = int.Parse(character.NationId.Split('_')[1]);
            _nationLabel.Text = $"Nation: {world.Nations[natIdx].Name}";
        }

        // Build action buttons
        foreach (var child in _actionBox.GetChildren())
            child.QueueFree();

        if (character.IsPlayer)
        {
            AddActionButton("📋 Review Intel", "Analyze your own intelligence networks");
            AddActionButton("💰 Fund Militia", "Spend resources to recruit local militia");
            AddActionButton("🛡 Fortify", "Improve defenses in controlled territories");
        }
        else
        {
            AddActionButton("🔍 Investigate", "Gather intelligence on this target");
            AddActionButton("💵 Bribe", "Attempt to buy their loyalty");
            AddActionButton("⚠️ Threaten", "Intimidate them into compliance");
            AddActionButton("🗡 Eliminate", "Arrange an... accident");
        }
    }

    private void AddActionButton(string text, string tooltip)
    {
        var btn = new Button
        {
            Text = text,
            TooltipText = tooltip,
            CustomMinimumSize = new Vector2(0, 36)
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.14f, 0.22f),
            BorderColor = new Color(0.25f, 0.3f, 0.5f),
            BorderWidthBottom = 1,
            BorderWidthTop = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 8,
            ContentMarginRight = 8
        };
        btn.AddThemeStyleboxOverride("normal", style);

        var hoverStyle = (StyleBoxFlat)style.Duplicate();
        hoverStyle.BgColor = new Color(0.18f, 0.22f, 0.35f);
        hoverStyle.BorderColor = new Color(0.4f, 0.5f, 0.8f);
        btn.AddThemeStyleboxOverride("hover", hoverStyle);

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
            "📋 Review Intel" => "review_intel",
            "💰 Fund Militia" => "fund_militia",
            "🛡 Fortify" => "fortify",
            "🔍 Investigate" => "investigate",
            "💵 Bribe" => "bribe",
            "⚠️ Threaten" => "threaten",
            "🗡 Eliminate" => "eliminate",
            _ => "unknown"
        };
        
        if (commandType != "unknown")
        {
            EventBus.Instance?.Publish(new PoliticalActionEvent(player.Id, _target.Id, commandType));
        }
    }

    public new void Hide()
    {
        _isVisible = false;
        _target = null;
    }

    public override void _Process(double delta)
    {
        // Slide animation — dock left of the RightSidebar (not on top of it)
        float dockX = GetViewportRect().Size.X - RightSidebarWidth - PanelWidth;
        float hiddenX = GetViewportRect().Size.X - RightSidebarWidth;
        float target = _isVisible ? dockX : hiddenX;
        float current = Position.X;
        float newX = Mathf.Lerp(current, target, 8f * (float)delta);
        Position = new Vector2(newX, 64);
    }
}
