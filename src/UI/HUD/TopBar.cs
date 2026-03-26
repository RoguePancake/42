using Godot;
using Warship.Core;
using Warship.Events;

namespace Warship.UI.HUD;

/// <summary>
/// Top Bar B — 32px tall bar showing turn counter, date, and player stats.
/// Sits directly below the alert bar (Top Bar A).
/// Event-driven: refreshes on TurnAdvancedEvent instead of polling every frame.
/// </summary>
public partial class TopBar : Control
{
    private Label _turnLabel = null!;
    private Label _statsLabel = null!;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        OffsetTop = UITheme.AlertBarHeight;
        OffsetBottom = UITheme.TopBarsTotal;

        var bg = new ColorRect
        {
            Color = UITheme.BgPanel,
            AnchorsPreset = (int)LayoutPreset.FullRect
        };
        AddChild(bg);

        var border = new ColorRect
        {
            Color = UITheme.BorderAccent,
            CustomMinimumSize = new Vector2(0, UITheme.BorderThick)
        };
        border.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
        AddChild(border);

        var hbox = new HBoxContainer
        {
            AnchorsPreset = (int)LayoutPreset.FullRect
        };
        AddChild(hbox);

        var leftMargin = new MarginContainer();
        leftMargin.AddThemeConstantOverride("margin_left", UITheme.PaddingMedium);

        var rightMargin = new MarginContainer();
        rightMargin.AddThemeConstantOverride("margin_right", UITheme.PaddingMedium);
        rightMargin.SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.ShrinkEnd;

        _turnLabel = new Label
        {
            Text = " Turn 1 | Jan 1900 ",
            VerticalAlignment = VerticalAlignment.Center
        };
        _turnLabel.AddThemeFontSizeOverride("font_size", UITheme.FontBody);
        _turnLabel.AddThemeColorOverride("font_color", UITheme.TextPrimary);

        _statsLabel = new Label
        {
            Text = " TA: ... WA: ... BSA: ... [FAI: ...] ",
            VerticalAlignment = VerticalAlignment.Center
        };
        _statsLabel.AddThemeFontSizeOverride("font_size", UITheme.FontBody);
        _statsLabel.AddThemeColorOverride("font_color", UITheme.TextSecondary);

        leftMargin.AddChild(_turnLabel);
        rightMargin.AddChild(_statsLabel);

        hbox.AddChild(leftMargin);
        hbox.AddChild(rightMargin);

        EventBus.Instance?.Subscribe<TurnAdvancedEvent>(OnTurnAdvanced);

        // Initial data refresh
        CallDeferred(nameof(RefreshStats));
    }

    private void OnTurnAdvanced(TurnAdvancedEvent ev)
    {
        _turnLabel.Text = $" Turn {ev.Turn} | M{ev.Month} Y{ev.Year} ";
        CallDeferred(nameof(RefreshStats));
    }

    private void RefreshStats()
    {
        var data = WorldStateManager.Instance?.Data;
        if (data == null || data.Characters.Count == 0) return;

        var pc = data.Characters.Find(c => c.IsPlayer);
        if (pc == null) return;

        int natIdx = int.Parse(pc.NationId.Split('_')[1]);
        var nat = data.Nations[natIdx];
        _statsLabel.Text = $" Treasury: ${nat.Treasury:0}M  |  {pc.Role} {pc.Name}  |  TA: {pc.TerritoryAuthority:0}%  WA: {pc.WorldAuthority:0}%  BSA: {pc.BehindTheScenesAuthority:0}%  [FAI: {pc.FullAuthorityIndex:0}%] ";
    }
}
