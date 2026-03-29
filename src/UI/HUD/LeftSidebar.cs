using Godot;
using Warship.Core;
using Warship.Data;
using Warship.Events;

namespace Warship.UI.HUD;

/// <summary>
/// Left sidebar — 250px wide. Shows the council name (government-type-aware),
/// quick action buttons, and a prominent "Open Council" button.
/// Adapts its header and available actions to the player's government type.
/// </summary>
public partial class LeftSidebar : Control
{
    private Label _councilNameLabel = null!;
    private Label _govTypeLabel = null!;
    private VBoxContainer _actionBox = null!;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.LeftWide);
        OffsetTop = 64;    // Below both top bars (32 + 32)
        OffsetRight = 250; // 250px wide
        OffsetBottom = 0;

        // Background
        var bg = new Panel();
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.11f, 0.13f, 1f),
            BorderColor = new Color(0.2f, 0.22f, 0.25f, 1f),
            BorderWidthRight = 2
        };
        bg.AddThemeStyleboxOverride("panel", style);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // Scrollable content
        var scroll = new ScrollContainer();
        scroll.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        AddChild(scroll);

        var vbox = new VBoxContainer();
        vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        vbox.AddThemeConstantOverride("separation", 0);
        scroll.AddChild(vbox);

        // Council name header (changes with government type)
        var headerPanel = new PanelContainer();
        var headerStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.07f, 0.1f),
            ContentMarginLeft = 12, ContentMarginTop = 10,
            ContentMarginBottom = 10, ContentMarginRight = 12,
        };
        headerPanel.AddThemeStyleboxOverride("panel", headerStyle);
        var headerVBox = new VBoxContainer();
        headerVBox.AddThemeConstantOverride("separation", 2);

        _councilNameLabel = new Label { Text = "THE COUNCIL" };
        _councilNameLabel.AddThemeFontSizeOverride("font_size", 16);
        _councilNameLabel.AddThemeColorOverride("font_color", Colors.Gold);
        headerVBox.AddChild(_councilNameLabel);

        _govTypeLabel = new Label { Text = "" };
        _govTypeLabel.AddThemeFontSizeOverride("font_size", 10);
        _govTypeLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
        headerVBox.AddChild(_govTypeLabel);

        headerPanel.AddChild(headerVBox);
        vbox.AddChild(headerPanel);

        // Open Council button (prominent)
        var openCouncilBtn = new Button
        {
            Text = "OPEN COUNCIL  [C]",
            CustomMinimumSize = new Vector2(0, 44),
        };
        var councilBtnStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.18f, 0.28f),
            BorderColor = new Color(0.3f, 0.4f, 0.6f),
            BorderWidthTop = 1, BorderWidthBottom = 1,
            ContentMarginLeft = 12,
        };
        var councilBtnHover = (StyleBoxFlat)councilBtnStyle.Duplicate();
        councilBtnHover.BgColor = new Color(0.2f, 0.25f, 0.4f);
        openCouncilBtn.AddThemeStyleboxOverride("normal", councilBtnStyle);
        openCouncilBtn.AddThemeStyleboxOverride("hover", councilBtnHover);
        openCouncilBtn.AddThemeFontSizeOverride("font_size", 14);
        openCouncilBtn.AddThemeColorOverride("font_color", Colors.Gold);
        openCouncilBtn.Pressed += () =>
        {
            EventBus.Instance?.Publish(new ViewSwitchEvent("council"));
        };
        vbox.AddChild(openCouncilBtn);

        // Quick actions (subset of council actions available inline)
        _actionBox = new VBoxContainer();
        _actionBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _actionBox.AddThemeConstantOverride("separation", 0);
        vbox.AddChild(_actionBox);

        // Subscribe to world ready to populate
        EventBus.Instance?.Subscribe<WorldReadyEvent>(OnWorldReady);
        EventBus.Instance?.Subscribe<TurnAdvancedEvent>(OnTurnAdvanced);
        CallDeferred(nameof(Rebuild));
    }

    private void OnWorldReady(WorldReadyEvent ev) => CallDeferred(nameof(Rebuild));
    private void OnTurnAdvanced(TurnAdvancedEvent ev) => CallDeferred(nameof(RefreshHeader));

    public override void _ExitTree()
    {
        EventBus.Instance?.Unsubscribe<WorldReadyEvent>(OnWorldReady);
        EventBus.Instance?.Unsubscribe<TurnAdvancedEvent>(OnTurnAdvanced);
    }

    private void Rebuild()
    {
        RefreshHeader();
        RebuildQuickActions();
    }

    private void RefreshHeader()
    {
        var data = WorldStateManager.Instance?.Data;
        if (data?.PlayerNationId == null) return;

        int pIdx = int.Parse(data.PlayerNationId.Split('_')[1]);
        var nation = data.Nations[pIdx];

        _councilNameLabel.Text = nation.Council.DisplayName.ToUpper();
        _govTypeLabel.Text = $"{nation.Name} \u2022 {nation.Archetype}";
    }

    private void RebuildQuickActions()
    {
        foreach (var child in _actionBox.GetChildren()) child.QueueFree();

        var data = WorldStateManager.Instance?.Data;
        if (data?.PlayerNationId == null)
        {
            // Default actions before world is ready
            BuildDefaultActions();
            return;
        }

        int pIdx = int.Parse(data.PlayerNationId.Split('_')[1]);
        var nation = data.Nations[pIdx];
        var govType = nation.Council.Type;

        // Quick Military
        AddCategoryHeader(_actionBox, "MILITARY", new Color(1f, 0.4f, 0.3f));
        AddQuickAction(_actionBox, "military", "Set Global Order", new Color(1f, 0.4f, 0.3f));
        AddQuickAction(_actionBox, "military", "Mobilize Reserves", new Color(1f, 0.4f, 0.3f));

        // Quick Diplomatic
        AddCategoryHeader(_actionBox, "DIPLOMATIC", new Color(0.3f, 0.6f, 1f));
        AddQuickAction(_actionBox, "diplomatic", "Propose Treaty", new Color(0.3f, 0.6f, 1f));
        AddQuickAction(_actionBox, "diplomatic", "Declare War", new Color(0.3f, 0.6f, 1f));

        // Quick Economic
        AddCategoryHeader(_actionBox, "ECONOMIC", new Color(0.3f, 0.9f, 0.4f));
        AddQuickAction(_actionBox, "economic", "Adjust Tax Rate", new Color(0.3f, 0.9f, 0.4f));

        // Quick Intelligence
        AddCategoryHeader(_actionBox, "INTELLIGENCE", new Color(0.8f, 0.6f, 1f));
        AddQuickAction(_actionBox, "intelligence", "Deploy Spy", new Color(0.8f, 0.6f, 1f));

        // Government-specific quick action
        string specialAction = govType switch
        {
            GovernmentType.RevolutionaryCommittee => "Purge Dissidents",
            GovernmentType.RoyalCourt => "Hold Feast",
            GovernmentType.MerchantSenate => "Issue Bonds",
            GovernmentType.Admiralty => "Commission Warship",
            GovernmentType.WarCouncil => "Rally Warriors",
            GovernmentType.ShadowCabinet => "Blackmail Official",
            GovernmentType.CentralCommittee => "Production Quota",
            GovernmentType.ImperialCourt => "Ennoble Loyalist",
            GovernmentType.SurvivalCouncil => "Ration Supplies",
            _ => "Fund Infrastructure",
        };
        AddCategoryHeader(_actionBox, "SPECIAL", new Color(0.9f, 0.8f, 0.3f));
        AddQuickAction(_actionBox, "special", specialAction, new Color(0.9f, 0.8f, 0.3f));
    }

    private void BuildDefaultActions()
    {
        AddCategoryHeader(_actionBox, "MILITARY", new Color(1f, 0.4f, 0.3f));
        AddQuickAction(_actionBox, "military", "Border Watch", new Color(1f, 0.4f, 0.3f));
        AddCategoryHeader(_actionBox, "DIPLOMATIC", new Color(0.3f, 0.6f, 1f));
        AddQuickAction(_actionBox, "diplomatic", "Send Envoy", new Color(0.3f, 0.6f, 1f));
    }

    private void AddCategoryHeader(VBoxContainer parent, string text, Color accentColor)
    {
        var container = new PanelContainer();
        var headerStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.09f, 0.11f, 1f),
            BorderColor = accentColor,
            BorderWidthLeft = 4,
            ContentMarginLeft = 12,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
        container.AddThemeStyleboxOverride("panel", headerStyle);

        var label = new Label { Text = text, VerticalAlignment = VerticalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_color", accentColor);
        container.AddChild(label);
        parent.AddChild(container);
    }

    private void AddQuickAction(VBoxContainer parent, string category, string text, Color accent)
    {
        var btn = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, 36),
            Alignment = HorizontalAlignment.Left
        };

        var normal = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.13f, 0.16f, 1f),
            BorderColor = new Color(0.15f, 0.17f, 0.2f, 1f),
            BorderWidthBottom = 1,
            ContentMarginLeft = 20
        };
        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = accent * 0.2f;

        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", hover);
        btn.AddThemeFontSizeOverride("font_size", 13);
        btn.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.8f));

        string actionId = text.ToLower().Replace(" ", "_");
        btn.Pressed += () =>
        {
            GD.Print($"[Sidebar] Quick action: {category}/{actionId}");
            EventBus.Instance?.Publish(new PlayerActionEvent(category, actionId));
        };

        parent.AddChild(btn);
    }
}
