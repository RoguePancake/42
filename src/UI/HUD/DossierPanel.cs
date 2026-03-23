using Godot;
using System;
using System.Linq;
using Warship.Data;
using Warship.Core;
using Warship.Engines;
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
    private Label _intelLabel = null!;
    private ProgressBar _taBar = null!;
    private ProgressBar _waBar = null!;
    private ProgressBar _bsaBar = null!;
    private Label _taLabel = null!;
    private Label _waLabel = null!;
    private Label _bsaLabel = null!;
    private StyleBoxFlat _taFill = null!;
    private StyleBoxFlat _waFill = null!;
    private StyleBoxFlat _bsaFill = null!;
    private Label _faiLabel = null!;
    private VBoxContainer _actionBox = null!;
    private Button _closeBtn = null!;

    // Original bar colors for fog desaturation
    private static readonly Color TaColor = new(0.2f, 0.7f, 0.3f);
    private static readonly Color WaColor = new(0.3f, 0.5f, 0.9f);
    private static readonly Color BsaColor = new(0.7f, 0.2f, 0.6f);
    private static readonly Color FoggedColor = new(0.3f, 0.3f, 0.35f);

    private CharacterData? _target;
    private bool _isVisible = false;
    private float _slideProgress = 0f;
    private const float PanelWidth = 320f;

    public override void _Ready()
    {
        // Start off-screen to the right
        Size = new Vector2(PanelWidth, 600);
        Position = new Vector2(GetViewportRect().Size.X, 80); // Below News & TopBar

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

        // === INTEL LEVEL INDICATOR ===
        _intelLabel = new Label { Text = "" };
        _intelLabel.AddThemeFontSizeOverride("font_size", 14);
        _intelLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _content.AddChild(_intelLabel);

        // === AUTHORITY METERS ===
        _content.AddChild(MakeBarSection("Territory Authority (TA)", out _taBar, out _taLabel, out _taFill, TaColor));
        _content.AddChild(MakeBarSection("World Authority (WA)", out _waBar, out _waLabel, out _waFill, WaColor));
        _content.AddChild(MakeBarSection("Shadow Authority (BSA)", out _bsaBar, out _bsaLabel, out _bsaFill, BsaColor));

        // FAI composite
        _faiLabel = new Label { Text = "Full Authority Index: 0%" };
        _faiLabel.AddThemeFontSizeOverride("font_size", 20);
        _faiLabel.AddThemeColorOverride("font_color", Colors.Gold);
        _faiLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _content.AddChild(_faiLabel);

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

        // Listen for authority changes to update bars live
        EventBus.Instance?.Subscribe<AuthorityChangedEvent>(ev => {
            if (_target != null && (ev.CharacterId == _target.Id || _target.IsPlayer))
                CallDeferred(nameof(RefreshDisplay));
        });

        // Refresh when intel changes (fog levels may shift)
        EventBus.Instance?.Subscribe<IntelChangedEvent>(ev => {
            if (_target != null && ev.TargetNationId == _target.NationId)
                CallDeferred(nameof(RefreshDisplay));
        });
    }

    private void RefreshDisplay()
    {
        if (_target != null) ShowCharacter(_target);
    }

    private VBoxContainer MakeBarSection(string label, out ProgressBar bar, out Label labelRef, out StyleBoxFlat fillRef, Color color)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 2);

        labelRef = new Label { Text = label };
        labelRef.AddThemeFontSizeOverride("font_size", 13);
        labelRef.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.7f));
        box.AddChild(labelRef);

        bar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 100,
            Value = 0,
            CustomMinimumSize = new Vector2(0, 22),
            ShowPercentage = true
        };
        fillRef = new StyleBoxFlat { BgColor = color, CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 };
        var bgStyle = new StyleBoxFlat { BgColor = new Color(0.15f, 0.15f, 0.2f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 };
        bar.AddThemeStyleboxOverride("fill", fillRef);
        bar.AddThemeStyleboxOverride("background", bgStyle);
        box.AddChild(bar);

        return box;
    }

    /// <summary>Open the dossier for a specific character.</summary>
    public void ShowCharacter(CharacterData character)
    {
        _target = character;
        _isVisible = true;

        // Update labels
        _nameLabel.Text = character.Name;
        _roleLabel.Text = $"Role: {character.Role}";

        var world = WorldStateManager.Instance?.Data;
        if (world == null) return;

        int natIdx = int.Parse(character.NationId.Split('_')[1]);
        _nationLabel.Text = $"Nation: {world.Nations[natIdx].Name}";

        // ─── FOG OF WAR ─────────────────────────────────
        bool isSelf = character.IsPlayer;
        IntelLevel intel = IntelLevel.Complete; // default for self
        float intelPts = 100f;

        if (!isSelf && world.PlayerNationId != null)
        {
            intel = IntelligenceEngine.GetIntelLevel(world, world.PlayerNationId, character.NationId);
            intelPts = IntelligenceEngine.GetIntelPoints(world, world.PlayerNationId, character.NationId);
        }

        // Intel indicator
        if (isSelf)
        {
            _intelLabel.Text = "";
        }
        else
        {
            var (intelColor, intelTag) = intel switch
            {
                IntelLevel.Complete  => (Colors.Gold, "COMPLETE"),
                IntelLevel.Confirmed => (Colors.Green, "CONFIRMED"),
                IntelLevel.Observed  => (Colors.Yellow, "OBSERVED"),
                IntelLevel.Rumor     => (Colors.Orange, "RUMOR"),
                _                    => (Colors.Red, "UNKNOWN")
            };
            _intelLabel.Text = $"INTEL: {intelTag} ({intelPts:0}/{100})";
            _intelLabel.AddThemeColorOverride("font_color", intelColor);
        }

        // Fog seed: stable per target per turn
        int fogSeed = character.NationId.GetHashCode() ^ world.TurnNumber;

        // Apply fogged values to bars
        ApplyFoggedBar(_taBar, _taLabel, _taFill, "Territory Authority (TA)",
            character.TerritoryAuthority, intel, fogSeed, TaColor);
        ApplyFoggedBar(_waBar, _waLabel, _waFill, "World Authority (WA)",
            character.WorldAuthority, intel, fogSeed + 1, WaColor);
        ApplyFoggedBar(_bsaBar, _bsaLabel, _bsaFill, "Shadow Authority (BSA)",
            character.BehindTheScenesAuthority, intel, fogSeed + 2, BsaColor);

        // FAI
        if (isSelf || intel == IntelLevel.Complete)
        {
            _faiLabel.Text = $"Full Authority Index: {character.FullAuthorityIndex:0.0}%";
            _faiLabel.AddThemeColorOverride("font_color", Colors.Gold);
        }
        else if (intel == IntelLevel.Unknown)
        {
            _faiLabel.Text = "Full Authority Index: ???";
            _faiLabel.AddThemeColorOverride("font_color", Colors.Gray);
        }
        else
        {
            float foggedFai = IntelligenceEngine.GetFoggedValue(
                character.FullAuthorityIndex, intel, fogSeed + 3);
            _faiLabel.Text = $"Full Authority Index: ~{foggedFai:0}%";
            _faiLabel.AddThemeColorOverride("font_color", Colors.Gold.Lerp(Colors.Gray, 0.3f));
        }

        // Build action buttons
        foreach (var child in _actionBox.GetChildren())
            child.QueueFree();

        if (character.IsPlayer)
        {
            AddActionButton("📋 Review Intel", "Analyze your own networks (+BSA, +intel on all rivals)");
            AddActionButton("💰 Fund Militia (+TA)", "Spend resources to boost local control");
            AddActionButton("🎙 Public Address (+WA)", "Broadcast to raise world standing");
        }
        else
        {
            AddActionButton("🔍 Investigate", "Gather intelligence on this target (+15 intel pts)");
            AddActionButton("💵 Bribe", "Attempt to buy their loyalty");
            AddActionButton("⚠️ Threaten", "Intimidate them into compliance");
            AddActionButton("🗡 Eliminate", "Arrange an... accident");
        }
    }

    private void ApplyFoggedBar(ProgressBar bar, Label label, StyleBoxFlat fill,
        string baseName, float realValue, IntelLevel intel, int seed, Color originalColor)
    {
        float fogged = IntelligenceEngine.GetFoggedValue(realValue, intel, seed);

        if (fogged < 0) // Unknown
        {
            bar.Value = 0;
            bar.ShowPercentage = false;
            label.Text = $"{baseName}: ???";
            fill.BgColor = FoggedColor;
        }
        else
        {
            bar.Value = fogged;
            bar.ShowPercentage = intel == IntelLevel.Complete;
            float desaturation = intel switch
            {
                IntelLevel.Complete => 0f,
                IntelLevel.Confirmed => 0.15f,
                IntelLevel.Observed => 0.3f,
                _ => 0.5f // Rumor
            };
            fill.BgColor = originalColor.Lerp(FoggedColor, desaturation);

            string prefix = intel == IntelLevel.Complete ? "" : "~";
            label.Text = $"{baseName}: {prefix}{fogged:0}%";
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
            "💰 Fund Militia (+TA)" => "fund_militia",
            "🎙 Public Address (+WA)" => "public_address",
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
        // Slide animation
        float target = _isVisible ? GetViewportRect().Size.X - PanelWidth : GetViewportRect().Size.X;
        float current = Position.X;
        float newX = Mathf.Lerp(current, target, 8f * (float)delta);
        Position = new Vector2(newX, 80);
    }
}
