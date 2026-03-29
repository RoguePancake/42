using Godot;
using System.Linq;
using Warship.Core;
using Warship.Data;
using Warship.Events;

namespace Warship.UI.Panels;

/// <summary>
/// Tactical combat interface — replaces the old 4-button MilitaryCommandPanel.
///
/// Layout (bottom-left, 480x220):
///   ┌─ COMBAT COMMAND ─────────────────────────────────────┐
///   │  ARMIES (list)        │  SELECTED ARMY DETAILS       │
///   │  > 1st Selvaran Guard │  Strength: 350               │
///   │    2nd Border Patrol  │  Morale: 87%  Supply: 92%    │
///   │    3rd Coastal Watch  │  Formation: [Col][Spr][Wdg]  │
///   │                       │  Order: [Def][Ptl][Atk][Ret] │
///   │                       ├──────────────────────────────│
///   │  BATTLE LOG           │  COMPOSITION                 │
///   │  1st Guard defeats... │  Infantry: 200  Tank: 50     │
///   │  Coastal repels...    │  Artillery: 30  AntiAir: 20  │
///   └───────────────────────┴──────────────────────────────┘
/// </summary>
public partial class CombatCommandPanel : Control
{
    private VBoxContainer _armyListBox = null!;
    private VBoxContainer _detailBox = null!;
    private VBoxContainer _compositionBox = null!;
    private VBoxContainer _battleLogBox = null!;
    private Label _selectedNameLabel = null!;
    private Label _strengthLabel = null!;
    private Label _moraleLabel = null!;
    private Label _supplyLabel = null!;
    private Label _orgLabel = null!;
    private Label _noSelectionLabel = null!;
    private HBoxContainer _formationRow = null!;
    private HBoxContainer _orderRow = null!;
    private Control _detailContainer = null!;

    private string? _selectedArmyId;

    private static readonly Color AccentBlue = new(0.25f, 0.4f, 0.7f);
    private static readonly Color DarkBg = new(0.08f, 0.09f, 0.12f, 0.95f);

    public override void _Ready()
    {
        // Position: bottom area, left of center
        SetAnchorsAndOffsetsPreset(LayoutPreset.BottomLeft);
        OffsetLeft = 260;       // Right of LeftSidebar
        OffsetRight = 740;      // 480px wide
        OffsetTop = -220;       // 220px tall
        OffsetBottom = 0;

        // Background
        var bg = new Panel();
        var bgStyle = new StyleBoxFlat
        {
            BgColor = DarkBg,
            BorderColor = AccentBlue,
            BorderWidthTop = 2,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
        };
        bg.AddThemeStyleboxOverride("panel", bgStyle);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        AddChild(margin);

        var mainHBox = new HBoxContainer();
        mainHBox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(mainHBox);

        // Left column: Army list + battle log
        var leftCol = new VBoxContainer { CustomMinimumSize = new Vector2(180, 0), SizeFlagsVertical = SizeFlags.ExpandFill };
        leftCol.AddThemeConstantOverride("separation", 4);
        mainHBox.AddChild(leftCol);

        var armiesHeader = new Label { Text = "ARMIES" };
        armiesHeader.AddThemeFontSizeOverride("font_size", 12);
        armiesHeader.AddThemeColorOverride("font_color", AccentBlue);
        leftCol.AddChild(armiesHeader);

        var armyScroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            CustomMinimumSize = new Vector2(170, 80),
        };
        leftCol.AddChild(armyScroll);

        _armyListBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _armyListBox.AddThemeConstantOverride("separation", 2);
        armyScroll.AddChild(_armyListBox);

        // Battle log
        var logHeader = new Label { Text = "BATTLE LOG" };
        logHeader.AddThemeFontSizeOverride("font_size", 10);
        logHeader.AddThemeColorOverride("font_color", new Color(0.6f, 0.4f, 0.4f));
        leftCol.AddChild(logHeader);

        var logScroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(170, 50),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        leftCol.AddChild(logScroll);

        _battleLogBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _battleLogBox.AddThemeConstantOverride("separation", 2);
        logScroll.AddChild(_battleLogBox);

        // Separator
        mainHBox.AddChild(new VSeparator());

        // Right column: Selected army details
        var rightCol = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        rightCol.AddThemeConstantOverride("separation", 4);
        mainHBox.AddChild(rightCol);

        // "No army selected" label
        _noSelectionLabel = new Label
        {
            Text = "Select an army from the list\nor click one on the map.",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        _noSelectionLabel.AddThemeFontSizeOverride("font_size", 12);
        _noSelectionLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.5f));
        rightCol.AddChild(_noSelectionLabel);

        // Detail container (hidden until army selected)
        _detailContainer = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        ((VBoxContainer)_detailContainer).AddThemeConstantOverride("separation", 3);
        _detailContainer.Visible = false;
        rightCol.AddChild(_detailContainer);
        var detailVBox = (VBoxContainer)_detailContainer;

        _selectedNameLabel = new Label { Text = "" };
        _selectedNameLabel.AddThemeFontSizeOverride("font_size", 15);
        _selectedNameLabel.AddThemeColorOverride("font_color", Colors.White);
        detailVBox.AddChild(_selectedNameLabel);

        // Stats row
        var statsRow = new HBoxContainer();
        statsRow.AddThemeConstantOverride("separation", 12);
        _strengthLabel = MakeStatLabel("STR: ---");
        _moraleLabel = MakeStatLabel("MRL: ---");
        _supplyLabel = MakeStatLabel("SUP: ---");
        _orgLabel = MakeStatLabel("ORG: ---");
        statsRow.AddChild(_strengthLabel);
        statsRow.AddChild(_moraleLabel);
        statsRow.AddChild(_supplyLabel);
        statsRow.AddChild(_orgLabel);
        detailVBox.AddChild(statsRow);

        // Formation buttons
        var formLabel = new Label { Text = "FORMATION" };
        formLabel.AddThemeFontSizeOverride("font_size", 10);
        formLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
        detailVBox.AddChild(formLabel);

        _formationRow = new HBoxContainer();
        _formationRow.AddThemeConstantOverride("separation", 4);
        AddFormationButton(_formationRow, "Column", FormationType.Column, "+20% spd, -10% def");
        AddFormationButton(_formationRow, "Spread", FormationType.Spread, "Balanced");
        AddFormationButton(_formationRow, "Wedge", FormationType.Wedge, "+15% atk, -5% def");
        AddFormationButton(_formationRow, "Circle", FormationType.Circle, "+20% def, no move");
        detailVBox.AddChild(_formationRow);

        // Order buttons
        var orderLabel = new Label { Text = "ORDERS" };
        orderLabel.AddThemeFontSizeOverride("font_size", 10);
        orderLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
        detailVBox.AddChild(orderLabel);

        _orderRow = new HBoxContainer();
        _orderRow.AddThemeConstantOverride("separation", 4);
        AddOrderButton(_orderRow, "Defend", MilitaryOrder.BorderWatch, new Color(0.3f, 0.7f, 0.3f));
        AddOrderButton(_orderRow, "Patrol", MilitaryOrder.Patrol, new Color(0.3f, 0.6f, 1f));
        AddOrderButton(_orderRow, "Stage", MilitaryOrder.Stage, new Color(0.7f, 0.6f, 0.2f));
        AddOrderButton(_orderRow, "Attack", MilitaryOrder.Attack, new Color(0.9f, 0.25f, 0.2f));
        AddOrderButton(_orderRow, "Retreat", MilitaryOrder.Standby, new Color(0.5f, 0.5f, 0.5f));
        detailVBox.AddChild(_orderRow);

        // Composition
        _compositionBox = new VBoxContainer();
        _compositionBox.AddThemeConstantOverride("separation", 1);
        detailVBox.AddChild(_compositionBox);

        // Subscribe to events
        EventBus.Instance?.Subscribe<ArmySelectedEvent>(OnArmySelected);
        EventBus.Instance?.Subscribe<BattleResolvedEvent>(OnBattleResolved);
        EventBus.Instance?.Subscribe<TurnAdvancedEvent>(_ => CallDeferred(nameof(RefreshArmyList)));

        CallDeferred(nameof(RefreshArmyList));
    }

    public override void _ExitTree()
    {
        EventBus.Instance?.Unsubscribe<ArmySelectedEvent>(OnArmySelected);
        EventBus.Instance?.Unsubscribe<BattleResolvedEvent>(OnBattleResolved);
    }

    private void OnArmySelected(ArmySelectedEvent ev)
    {
        _selectedArmyId = ev.ArmyId;
        CallDeferred(nameof(RefreshDetails));
    }

    private void OnBattleResolved(BattleResolvedEvent ev)
    {
        CallDeferred(nameof(AddBattleLogEntry), ev.AttackerArmyId, ev.DefenderArmyId,
            ev.AttackerWon, ev.AttackerLosses, ev.DefenderLosses);
    }

    private void AddBattleLogEntry(string attackerId, string defenderId,
        bool attackerWon, int atkLoss, int defLoss)
    {
        var data = WorldStateManager.Instance?.Data;
        if (data == null) return;

        var atk = data.Armies.FirstOrDefault(a => a.Id == attackerId);
        var def = data.Armies.FirstOrDefault(a => a.Id == defenderId);
        string text = attackerWon
            ? $"{atk?.Name ?? "?"} defeated {def?.Name ?? "?"} (-{atkLoss}/{defLoss})"
            : $"{def?.Name ?? "?"} repelled {atk?.Name ?? "?"} (-{defLoss}/{atkLoss})";

        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeFontSizeOverride("font_size", 9);
        label.AddThemeColorOverride("font_color", attackerWon ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.8f, 0.4f, 0.4f));
        _battleLogBox.AddChild(label);

        // Keep only last 20 entries
        while (_battleLogBox.GetChildCount() > 20)
            _battleLogBox.GetChild(0).QueueFree();
    }

    private void RefreshArmyList()
    {
        var data = WorldStateManager.Instance?.Data;
        if (data == null || data.PlayerNationId == null) return;

        foreach (var child in _armyListBox.GetChildren()) child.QueueFree();

        var playerArmies = data.Armies
            .Where(a => a.NationId == data.PlayerNationId && a.IsAlive)
            .ToList();

        if (playerArmies.Count == 0)
        {
            var noArmies = new Label { Text = "No armies." };
            noArmies.AddThemeFontSizeOverride("font_size", 11);
            noArmies.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.5f));
            _armyListBox.AddChild(noArmies);
            return;
        }

        foreach (var army in playerArmies)
        {
            var btn = new Button
            {
                Text = $"{army.Name} ({army.TotalStrength})",
                CustomMinimumSize = new Vector2(0, 24),
                Alignment = HorizontalAlignment.Left,
            };

            bool isSelected = army.Id == _selectedArmyId;
            var style = new StyleBoxFlat
            {
                BgColor = isSelected ? AccentBlue * 0.4f : new Color(0.1f, 0.11f, 0.14f),
                ContentMarginLeft = 8,
                CornerRadiusTopLeft = 2, CornerRadiusBottomLeft = 2,
                CornerRadiusTopRight = 2, CornerRadiusBottomRight = 2,
            };
            var hover = (StyleBoxFlat)style.Duplicate();
            hover.BgColor = AccentBlue * 0.3f;

            btn.AddThemeStyleboxOverride("normal", style);
            btn.AddThemeStyleboxOverride("hover", hover);
            btn.AddThemeFontSizeOverride("font_size", 11);

            // Order status indicator color
            Color orderColor = army.CurrentOrder switch
            {
                MilitaryOrder.Attack => new Color(1f, 0.3f, 0.3f),
                MilitaryOrder.Patrol => new Color(0.3f, 0.6f, 1f),
                MilitaryOrder.BorderWatch => new Color(0.3f, 0.7f, 0.3f),
                MilitaryOrder.Stage => new Color(0.7f, 0.6f, 0.2f),
                _ => new Color(0.5f, 0.5f, 0.5f),
            };
            btn.AddThemeColorOverride("font_color", orderColor);

            string armyId = army.Id;
            btn.Pressed += () =>
            {
                _selectedArmyId = armyId;
                EventBus.Instance?.Publish(new ArmySelectedEvent(armyId));
                RefreshArmyList();
                RefreshDetails();
            };

            _armyListBox.AddChild(btn);
        }

        RefreshDetails();
    }

    private void RefreshDetails()
    {
        var data = WorldStateManager.Instance?.Data;
        if (data == null || _selectedArmyId == null)
        {
            _noSelectionLabel.Visible = true;
            _detailContainer.Visible = false;
            return;
        }

        var army = data.Armies.FirstOrDefault(a => a.Id == _selectedArmyId);
        if (army == null || !army.IsAlive)
        {
            _selectedArmyId = null;
            _noSelectionLabel.Visible = true;
            _detailContainer.Visible = false;
            return;
        }

        _noSelectionLabel.Visible = false;
        _detailContainer.Visible = true;

        _selectedNameLabel.Text = army.Name;
        _strengthLabel.Text = $"STR: {army.TotalStrength}";
        _moraleLabel.Text = $"MRL: {army.Morale:0}%";
        _moraleLabel.AddThemeColorOverride("font_color",
            army.Morale > 60 ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.9f, 0.4f, 0.3f));
        _supplyLabel.Text = $"SUP: {army.Supply:0}%";
        _orgLabel.Text = $"ORG: {army.Organization:0}%";

        // Composition breakdown
        foreach (var child in _compositionBox.GetChildren()) child.QueueFree();
        var compRow = new HBoxContainer();
        compRow.AddThemeConstantOverride("separation", 8);
        int count = 0;
        foreach (var (type, qty) in army.Composition.OrderByDescending(kv => kv.Value))
        {
            var label = new Label { Text = $"{type}: {qty}" };
            label.AddThemeFontSizeOverride("font_size", 10);
            label.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.7f));
            compRow.AddChild(label);
            count++;
            if (count >= 4)
            {
                _compositionBox.AddChild(compRow);
                compRow = new HBoxContainer();
                compRow.AddThemeConstantOverride("separation", 8);
                count = 0;
            }
        }
        if (count > 0) _compositionBox.AddChild(compRow);
    }

    private void AddFormationButton(HBoxContainer parent, string text, FormationType formation, string tooltip)
    {
        var btn = new Button { Text = text, CustomMinimumSize = new Vector2(60, 24), TooltipText = tooltip };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.13f, 0.17f),
            CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
        };
        var hover = (StyleBoxFlat)style.Duplicate();
        hover.BgColor = new Color(0.2f, 0.25f, 0.35f);
        btn.AddThemeStyleboxOverride("normal", style);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeFontSizeOverride("font_size", 10);
        btn.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.8f));

        btn.Pressed += () =>
        {
            if (_selectedArmyId == null) return;
            var data = WorldStateManager.Instance?.Data;
            var army = data?.Armies.FirstOrDefault(a => a.Id == _selectedArmyId);
            if (army != null)
            {
                army.Formation = formation;
                GD.Print($"[Combat] {army.Name} formation -> {formation}");
                EventBus.Instance?.Publish(new ArmyFormationEvent(army.Id, formation));
            }
        };

        parent.AddChild(btn);
    }

    private void AddOrderButton(HBoxContainer parent, string text, MilitaryOrder order, Color color)
    {
        var btn = new Button { Text = text, CustomMinimumSize = new Vector2(52, 24) };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.13f, 0.17f),
            CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
        };
        var hover = (StyleBoxFlat)style.Duplicate();
        hover.BgColor = color * 0.4f;
        btn.AddThemeStyleboxOverride("normal", style);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeFontSizeOverride("font_size", 10);
        btn.AddThemeColorOverride("font_color", color);

        btn.Pressed += () =>
        {
            if (_selectedArmyId == null) return;
            var data = WorldStateManager.Instance?.Data;
            var army = data?.Armies.FirstOrDefault(a => a.Id == _selectedArmyId);
            if (army != null)
            {
                army.CurrentOrder = order;
                GD.Print($"[Combat] {army.Name} order -> {order}");
                EventBus.Instance?.Publish(new ArmyOrderEvent(army.Id, order));
                RefreshArmyList();
            }
        };

        parent.AddChild(btn);
    }

    private static Label MakeStatLabel(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 11);
        label.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.8f));
        return label;
    }
}
