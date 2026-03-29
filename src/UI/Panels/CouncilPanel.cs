using Godot;
using System.Linq;
using Warship.Core;
using Warship.Data;
using Warship.Events;

namespace Warship.UI.Panels;

/// <summary>
/// The Council Panel — the player's governing body interface.
/// Name changes by government type: National Assembly, Royal Court, Politburo, etc.
/// Shows advisers, their opinions, and lets the player issue government actions.
///
/// Layout (full-screen overlay, toggleable):
///   ┌──────────────────────────────────────────────────┐
///   │  ★ NATIONAL ASSEMBLY  (or Royal Court, etc.)     │
///   │  "Your advisers await your decision."            │
///   ├────────────────┬─────────────────────────────────┤
///   │  ADVISERS      │  ACTIONS                        │
///   │  [portrait]    │  ┌─ DOMESTIC ─────────────────┐ │
///   │  Gen. Vasquez  │  │ Adjust Tax Rate            │ │
///   │  Military      │  │ Fund Infrastructure        │ │
///   │  "Attack now!" │  │ Declare Martial Law        │ │
///   │                │  │ Suppress Dissent           │ │
///   │  [portrait]    │  └────────────────────────────┘ │
///   │  Dr. Lin       │  ┌─ MILITARY ─────────────────┐ │
///   │  Economic      │  │ Set Defense Budget         │ │
///   │  "We need $"   │  │ Authorize Operation        │ │
///   │  ...           │  │ Approve Conscription       │ │
///   │                │  │ Nuclear Authorization      │ │
///   │                │  └────────────────────────────┘ │
///   │                │  ┌─ DIPLOMATIC ───────────────┐ │
///   │                │  │ ...                        │ │
///   └────────────────┴─────────────────────────────────┘
/// </summary>
public partial class CouncilPanel : Control
{
    private VBoxContainer _adviserList = null!;
    private VBoxContainer _actionList = null!;
    private Label _titleLabel = null!;
    private Label _subtitleLabel = null!;
    private bool _isVisible = false;

    // Adviser role display names
    private static string RoleDisplayName(AdviserRole role) => role switch
    {
        AdviserRole.Military => "Military Adviser",
        AdviserRole.Economic => "Economic Adviser",
        AdviserRole.Intelligence => "Intelligence Adviser",
        AdviserRole.Diplomatic => "Diplomatic Adviser",
        AdviserRole.FleetAdmiral => "Fleet Admiral",
        AdviserRole.PartyCommissar => "Party Commissar",
        AdviserRole.ChiefEngineer => "Chief Engineer",
        AdviserRole.CourtChamberlain => "Court Chamberlain",
        AdviserRole.TradeGuildmaster => "Trade Guildmaster",
        AdviserRole.TribalElder => "Tribal Elder",
        AdviserRole.Spymaster => "Spymaster",
        AdviserRole.NuclearOfficer => "Nuclear Officer",
        AdviserRole.ResourceWarden => "Resource Warden",
        _ => role.ToString()
    };

    // Colors per adviser role
    private static Color RoleColor(AdviserRole role) => role switch
    {
        AdviserRole.Military => new Color(1f, 0.4f, 0.3f),
        AdviserRole.Economic => new Color(0.3f, 0.9f, 0.4f),
        AdviserRole.Intelligence => new Color(0.8f, 0.6f, 1f),
        AdviserRole.Diplomatic => new Color(0.3f, 0.6f, 1f),
        _ => new Color(0.9f, 0.8f, 0.3f), // Specialist = gold
    };

    public override void _Ready()
    {
        // Full screen overlay, hidden by default
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop; // Block clicks to map when open

        // Dim background
        var dimBg = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.7f),
        };
        dimBg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(dimBg);

        // Main window (centered, 900x600)
        var window = new PanelContainer();
        window.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        window.OffsetLeft = -450;
        window.OffsetRight = 450;
        window.OffsetTop = -300;
        window.OffsetBottom = 300;
        var windowStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.09f, 0.12f, 0.98f),
            BorderColor = new Color(0.25f, 0.35f, 0.55f),
            BorderWidthTop = 3, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            ContentMarginLeft = 16, ContentMarginRight = 16,
            ContentMarginTop = 12, ContentMarginBottom = 12,
        };
        window.AddThemeStyleboxOverride("panel", windowStyle);
        AddChild(window);

        var outerVBox = new VBoxContainer();
        outerVBox.AddThemeConstantOverride("separation", 8);
        window.AddChild(outerVBox);

        // Header row: title + close button
        var headerRow = new HBoxContainer();
        outerVBox.AddChild(headerRow);

        _titleLabel = new Label { Text = "THE COUNCIL", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _titleLabel.AddThemeFontSizeOverride("font_size", 22);
        _titleLabel.AddThemeColorOverride("font_color", Colors.Gold);
        headerRow.AddChild(_titleLabel);

        _subtitleLabel = new Label { Text = "" };
        _subtitleLabel.AddThemeFontSizeOverride("font_size", 12);
        _subtitleLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));

        var closeBtn = new Button { Text = "X", CustomMinimumSize = new Vector2(32, 32) };
        var closeBtnStyle = new StyleBoxFlat { BgColor = new Color(0.6f, 0.15f, 0.15f), CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 };
        closeBtn.AddThemeStyleboxOverride("normal", closeBtnStyle);
        closeBtn.Pressed += () => Hide();
        headerRow.AddChild(closeBtn);

        outerVBox.AddChild(_subtitleLabel);
        outerVBox.AddChild(new HSeparator());

        // Body: advisers left, actions right
        var body = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 16);
        outerVBox.AddChild(body);

        // Left: Adviser list (280px wide, scrollable)
        var adviserScroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(280, 0),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        body.AddChild(adviserScroll);

        _adviserList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _adviserList.AddThemeConstantOverride("separation", 8);
        adviserScroll.AddChild(_adviserList);

        // Vertical separator
        body.AddChild(new VSeparator());

        // Right: Action categories (scrollable)
        var actionScroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        body.AddChild(actionScroll);

        _actionList = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _actionList.AddThemeConstantOverride("separation", 4);
        actionScroll.AddChild(_actionList);

        // Subscribe to toggle event
        EventBus.Instance?.Subscribe<ViewSwitchEvent>(OnViewSwitch);
    }

    public override void _ExitTree()
    {
        EventBus.Instance?.Unsubscribe<ViewSwitchEvent>(OnViewSwitch);
    }

    private void OnViewSwitch(ViewSwitchEvent ev)
    {
        if (ev.ViewId == "council")
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        _isVisible = !_isVisible;
        Visible = _isVisible;
        if (_isVisible) Refresh();
    }

    public void Refresh()
    {
        var data = WorldStateManager.Instance?.Data;
        if (data == null || data.PlayerNationId == null) return;

        int pIdx = int.Parse(data.PlayerNationId.Split('_')[1]);
        var nation = data.Nations[pIdx];
        var council = nation.Council;

        // Update header
        _titleLabel.Text = council.DisplayName.ToUpper();
        _subtitleLabel.Text = GetFlavorText(council.Type);

        // Rebuild adviser cards
        foreach (var child in _adviserList.GetChildren()) child.QueueFree();
        foreach (var adviser in council.Advisers)
        {
            _adviserList.AddChild(BuildAdviserCard(adviser));
        }

        // Rebuild action list
        foreach (var child in _actionList.GetChildren()) child.QueueFree();
        BuildActionCategory(_actionList, "DOMESTIC", CouncilActionCategory.Domestic,
            new Color(0.5f, 0.8f, 0.5f), nation, council);
        BuildActionCategory(_actionList, "MILITARY", CouncilActionCategory.Military,
            new Color(1f, 0.4f, 0.3f), nation, council);
        BuildActionCategory(_actionList, "DIPLOMATIC", CouncilActionCategory.Diplomatic,
            new Color(0.3f, 0.6f, 1f), nation, council);
        BuildActionCategory(_actionList, "INTELLIGENCE", CouncilActionCategory.Intelligence,
            new Color(0.8f, 0.6f, 1f), nation, council);
    }

    private Control BuildAdviserCard(AdviserData adviser)
    {
        var card = new PanelContainer();
        var cardStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.13f, 0.16f),
            BorderColor = RoleColor(adviser.Role) * 0.6f,
            BorderWidthLeft = 3,
            ContentMarginLeft = 10, ContentMarginRight = 8,
            ContentMarginTop = 8, ContentMarginBottom = 8,
            CornerRadiusTopRight = 4, CornerRadiusBottomRight = 4,
        };
        card.AddThemeStyleboxOverride("panel", cardStyle);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 2);

        // Name
        var nameLabel = new Label { Text = adviser.Name };
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        nameLabel.AddThemeColorOverride("font_color", Colors.White);
        vbox.AddChild(nameLabel);

        // Role
        var roleLabel = new Label { Text = RoleDisplayName(adviser.Role) };
        roleLabel.AddThemeFontSizeOverride("font_size", 11);
        roleLabel.AddThemeColorOverride("font_color", RoleColor(adviser.Role));
        vbox.AddChild(roleLabel);

        // Loyalty/Competence bars
        var statsRow = new HBoxContainer();
        statsRow.AddThemeConstantOverride("separation", 8);

        var loyaltyLabel = new Label { Text = $"Loyalty: {adviser.Loyalty * 100:0}%" };
        loyaltyLabel.AddThemeFontSizeOverride("font_size", 10);
        loyaltyLabel.AddThemeColorOverride("font_color",
            adviser.Loyalty > 0.6f ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.9f, 0.4f, 0.3f));
        statsRow.AddChild(loyaltyLabel);

        var compLabel = new Label { Text = $"Skill: {adviser.Competence * 100:0}%" };
        compLabel.AddThemeFontSizeOverride("font_size", 10);
        compLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.7f));
        statsRow.AddChild(compLabel);

        vbox.AddChild(statsRow);

        // Current advice (if any)
        if (!string.IsNullOrEmpty(adviser.CurrentAdvice))
        {
            var adviceLabel = new Label
            {
                Text = $"\"{adviser.CurrentAdvice}\"",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(240, 0),
            };
            adviceLabel.AddThemeFontSizeOverride("font_size", 10);
            adviceLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.5f));
            vbox.AddChild(adviceLabel);
        }

        card.AddChild(vbox);
        return card;
    }

    private void BuildActionCategory(VBoxContainer parent, string title,
        CouncilActionCategory category, Color accent, NationData nation, CouncilData council)
    {
        // Category header
        var header = new PanelContainer();
        var headerStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.07f, 0.09f),
            BorderColor = accent,
            BorderWidthLeft = 4,
            ContentMarginLeft = 12, ContentMarginTop = 6, ContentMarginBottom = 6,
        };
        header.AddThemeStyleboxOverride("panel", headerStyle);
        var headerLabel = new Label { Text = title };
        headerLabel.AddThemeFontSizeOverride("font_size", 13);
        headerLabel.AddThemeColorOverride("font_color", accent);
        header.AddChild(headerLabel);
        parent.AddChild(header);

        // Actions per category
        string[] actions = GetActionsForCategory(category, council.Type);
        foreach (var action in actions)
        {
            AddCouncilActionButton(parent, category, action, accent, nation.Id);
        }
    }

    private void AddCouncilActionButton(VBoxContainer parent, CouncilActionCategory category,
        string actionText, Color accent, string nationId)
    {
        var btn = new Button
        {
            Text = actionText,
            CustomMinimumSize = new Vector2(0, 32),
            Alignment = HorizontalAlignment.Left,
        };

        var normal = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.11f, 0.14f),
            BorderColor = new Color(0.13f, 0.15f, 0.18f),
            BorderWidthBottom = 1,
            ContentMarginLeft = 20,
        };
        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = accent * 0.25f;

        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", hover);
        btn.AddThemeFontSizeOverride("font_size", 13);
        btn.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.8f));

        string actionId = actionText.ToLower().Replace(" ", "_");
        btn.Pressed += () =>
        {
            GD.Print($"[Council] {category}/{actionId}");
            EventBus.Instance?.Publish(new CouncilActionEvent(nationId, category, actionId));
        };

        parent.AddChild(btn);
    }

    private static string[] GetActionsForCategory(CouncilActionCategory category, GovernmentType govType)
    {
        return category switch
        {
            CouncilActionCategory.Domestic => govType switch
            {
                GovernmentType.RevolutionaryCommittee => new[]
                    { "Adjust Tax Rate", "Fund Infrastructure", "Declare Martial Law", "Purge Dissidents", "Revolutionary Rally" },
                GovernmentType.RoyalCourt => new[]
                    { "Adjust Tax Rate", "Fund Infrastructure", "Declare Martial Law", "Hold Feast", "Ennoble Loyalist" },
                GovernmentType.MerchantSenate => new[]
                    { "Adjust Tax Rate", "Fund Infrastructure", "Deregulate Markets", "Corporate Subsidy", "Issue Bonds" },
                GovernmentType.WarCouncil => new[]
                    { "Adjust Tax Rate", "Fortify Settlement", "Declare Blood Oath", "Rally Warriors", "Exile Traitor" },
                GovernmentType.ShadowCabinet => new[]
                    { "Adjust Tax Rate", "Fund Infrastructure", "Install Surveillance", "Blackmail Official", "Disappear Dissident" },
                _ => new[]
                    { "Adjust Tax Rate", "Fund Infrastructure", "Declare Martial Law", "Hold Elections", "Suppress Dissent" },
            },
            CouncilActionCategory.Military => govType switch
            {
                GovernmentType.Admiralty => new[]
                    { "Set Defense Budget", "Authorize Naval Operation", "Approve Conscription", "Commission Warship", "Blockade Order" },
                _ => new[]
                    { "Set Defense Budget", "Authorize Operation", "Approve Conscription", "Nuclear Authorization", "Mobilize Reserves" },
            },
            CouncilActionCategory.Diplomatic => new[]
                { "Propose Treaty", "Declare War", "Impose Sanctions", "Request Aid", "Send Envoy", "Recall Ambassador" },
            CouncilActionCategory.Intelligence => govType switch
            {
                GovernmentType.ShadowCabinet => new[]
                    { "Deploy Agent", "Deep Cover Operation", "Counter-Intelligence", "Approve Assassination", "Fabricate Evidence", "Double Agent" },
                _ => new[]
                    { "Deploy Spy", "Counter-Intelligence", "Approve Assassination", "Sabotage Mission", "Steal Technology" },
            },
            _ => System.Array.Empty<string>()
        };
    }

    private static string GetFlavorText(GovernmentType type) => type switch
    {
        GovernmentType.FederalCouncil => "The council awaits your orders, Mr. President.",
        GovernmentType.RevolutionaryCommittee => "The committee is in permanent session. The revolution demands action.",
        GovernmentType.MerchantSenate => "The senators have reviewed the quarterly projections. What say you?",
        GovernmentType.RoyalCourt => "Your Majesty, the court is assembled. We await your royal decree.",
        GovernmentType.CentralCommittee => "Comrade Chairman, the five-year plan requires adjustments.",
        GovernmentType.Admiralty => "Admiral, the fleet awaits your command.",
        GovernmentType.NationalAssembly => "The assembly is in session. The people's representatives await your proposal.",
        GovernmentType.WarCouncil => "The war chiefs gather around the fire. Speak, and we shall act.",
        GovernmentType.ShadowCabinet => "The room is secure. No one beyond these walls will know what is decided here.",
        GovernmentType.ImperialCourt => "The court remembers the glory that was. What shall we do to restore it?",
        GovernmentType.SurvivalCouncil => "Resources are critically low. Every decision matters. Choose wisely.",
        _ => "Your advisers await your decision."
    };

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.C)
        {
            Toggle();
            GetViewport().SetInputAsHandled();
        }
    }
}
