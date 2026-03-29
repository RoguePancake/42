using Godot;
using Warship.Core;
using Warship.Events;

namespace Warship.UI.HUD;

/// <summary>
/// Lower Main Section — 200px tall data dashboard between the sidebars.
/// Shows nation stats with HP-bar style meters: Treasury, Prestige, Authority, Units.
/// </summary>
public partial class BottomPanel : Control
{
    private Label _treasuryValue = null!;
    private Label _prestigeValue = null!;
    private ProgressBar _treasuryBar = null!;
    private ProgressBar _prestigeBar = null!;
    private Label _taValue = null!;
    private Label _waValue = null!;
    private Label _bsaValue = null!;
    private ProgressBar _taBar = null!;
    private ProgressBar _waBar = null!;
    private ProgressBar _bsaBar = null!;
    private Label _unitCountLabel = null!;
    private Label _provinceCountLabel = null!;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
        OffsetTop = -200;
        OffsetBottom = 0;
        OffsetLeft = 250;   // Clears LeftSidebar
        OffsetRight = -250; // Clears RightSidebar

        // Background
        var bg = new Panel();
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.11f, 0.13f, 1f),
            BorderColor = new Color(0.2f, 0.4f, 0.8f, 1f),
            BorderWidthTop = 3
        };
        bg.AddThemeStyleboxOverride("panel", style);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // Main layout: two columns
        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        AddChild(margin);

        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 24);
        margin.AddChild(columns);

        // Left column: Economy stats
        var leftCol = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        leftCol.AddThemeConstantOverride("separation", 4);
        columns.AddChild(leftCol);

        AddColumnHeader(leftCol, "ECONOMY", new Color(0.3f, 0.9f, 0.4f));
        (_treasuryBar, _treasuryValue) = AddStatBar(leftCol, "Treasury", 0, 10000, new Color(0.3f, 0.8f, 0.3f));
        (_prestigeBar, _prestigeValue) = AddStatBar(leftCol, "Prestige", 0, 100, new Color(0.9f, 0.8f, 0.2f));

        // Counts row
        var countsRow = new HBoxContainer();
        countsRow.AddThemeConstantOverride("separation", 20);
        leftCol.AddChild(countsRow);

        _provinceCountLabel = new Label { Text = "Provinces: 4" };
        _provinceCountLabel.AddThemeFontSizeOverride("font_size", 12);
        _provinceCountLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.7f));
        countsRow.AddChild(_provinceCountLabel);

        _unitCountLabel = new Label { Text = "Units: 0" };
        _unitCountLabel.AddThemeFontSizeOverride("font_size", 12);
        _unitCountLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.7f));
        countsRow.AddChild(_unitCountLabel);

        // Separator
        var sep = new VSeparator();
        columns.AddChild(sep);

        // Right column: Authority meters
        var rightCol = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        rightCol.AddThemeConstantOverride("separation", 4);
        columns.AddChild(rightCol);

        AddColumnHeader(rightCol, "AUTHORITY", new Color(0.5f, 0.7f, 1f));
        (_taBar, _taValue) = AddStatBar(rightCol, "Territory (TA)", 0, 100, new Color(0.3f, 0.6f, 1f));
        (_waBar, _waValue) = AddStatBar(rightCol, "World (WA)", 0, 100, new Color(0.4f, 0.8f, 1f));
        (_bsaBar, _bsaValue) = AddStatBar(rightCol, "Behind Scenes (BSA)", 0, 100, new Color(0.6f, 0.5f, 1f));

        EventBus.Instance?.Subscribe<TurnAdvancedEvent>(_ => CallDeferred(nameof(RefreshData)));
        CallDeferred(nameof(RefreshData));
    }

    private void AddColumnHeader(VBoxContainer parent, string text, Color color)
    {
        var label = new Label
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, 24)
        };
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_color", color);
        parent.AddChild(label);
    }

    private (ProgressBar bar, Label value) AddStatBar(VBoxContainer parent, string label, float min, float max, Color barColor)
    {
        var row = new VBoxContainer();
        row.AddThemeConstantOverride("separation", 2);

        var headerRow = new HBoxContainer();

        var nameLabel = new Label
        {
            Text = label,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 12);
        nameLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.7f));
        headerRow.AddChild(nameLabel);

        var valueLabel = new Label { Text = "---" };
        valueLabel.AddThemeFontSizeOverride("font_size", 12);
        valueLabel.AddThemeColorOverride("font_color", Colors.White);
        headerRow.AddChild(valueLabel);

        row.AddChild(headerRow);

        var bar = new ProgressBar
        {
            MinValue = min,
            MaxValue = max,
            Value = 0,
            CustomMinimumSize = new Vector2(0, 12),
            ShowPercentage = false
        };

        // Style the bar
        var barBg = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.06f, 0.08f, 1f),
            CornerRadiusTopLeft = 2, CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2, CornerRadiusBottomRight = 2
        };
        bar.AddThemeStyleboxOverride("background", barBg);

        var barFill = new StyleBoxFlat
        {
            BgColor = barColor,
            CornerRadiusTopLeft = 2, CornerRadiusTopRight = 2,
            CornerRadiusBottomLeft = 2, CornerRadiusBottomRight = 2
        };
        bar.AddThemeStyleboxOverride("fill", barFill);

        row.AddChild(bar);
        parent.AddChild(row);

        return (bar, valueLabel);
    }

    private void RefreshData()
    {
        var data = WorldStateManager.Instance?.Data;
        if (data == null) return;

        var pc = data.Characters.Find(c => c.IsPlayer);
        if (pc == null) return;

        int natIdx = int.Parse(pc.NationId.Split('_')[1]);
        var nat = data.Nations[natIdx];

        _treasuryBar.Value = System.Math.Clamp(nat.Treasury, 0, 10000);
        _treasuryValue.Text = $"${nat.Treasury:0}M";

        _prestigeBar.Value = System.Math.Clamp(nat.Prestige, 0, 100);
        _prestigeValue.Text = $"{nat.Prestige:0}";

        _taBar.Value = pc.TerritoryAuthority;
        _taValue.Text = $"{pc.TerritoryAuthority:0}%";

        _waBar.Value = pc.WorldAuthority;
        _waValue.Text = $"{pc.WorldAuthority:0}%";

        _bsaBar.Value = pc.BehindTheScenesAuthority;
        _bsaValue.Text = $"{pc.BehindTheScenesAuthority:0}%";

        _provinceCountLabel.Text = $"Provinces: {nat.ProvinceCount}";

        int unitCount = 0;
        foreach (var a in data.Armies)
            if (a.NationId == nat.Id && a.IsAlive) unitCount += a.TotalStrength;
        _unitCountLabel.Text = $"Forces: {unitCount}";
    }

    // Data refreshes via TurnAdvancedEvent subscription — no per-frame polling needed
}
