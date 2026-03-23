using Godot;
using Warship.Core;
using Warship.Events;

namespace Warship.UI.HUD;

/// <summary>
/// Top bar UI showing turn, date, and player stats.
/// Constructed fully in code so we don't need a clunky .tscn file for it.
/// </summary>
public partial class TopBar : Control
{
    private Label _turnLabel;
    private Label _statsLabel;
    
    public override void _Ready()
    {
        // Setup Control node
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        OffsetTop = 32; // Under News Ticker
        OffsetBottom = 80; // 48px tall

        // Background
        var bg = new ColorRect 
        { 
            Color = new Color(0.08f, 0.08f, 0.12f, 0.95f), 
            AnchorsPreset = (int)LayoutPreset.FullRect 
        };
        AddChild(bg);

        // Stylish bottom border
        var border = new ColorRect 
        { 
            Color = new Color(0.2f, 0.4f, 0.8f, 1f), 
            CustomMinimumSize = new Vector2(0, 4) 
        };
        border.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
        AddChild(border);

        // Layout container
        var hbox = new HBoxContainer 
        { 
            AnchorsPreset = (int)LayoutPreset.FullRect 
        };
        AddChild(hbox);

        // Margin to pad text nicely
        var leftMargin = new MarginContainer();
        leftMargin.AddThemeConstantOverride("margin_left", 20);
        
        var rightMargin = new MarginContainer();
        rightMargin.AddThemeConstantOverride("margin_right", 20);
        rightMargin.SizeFlagsHorizontal = SizeFlags.Expand | SizeFlags.ShrinkEnd;

        // Labels
        _turnLabel = new Label 
        { 
            Text = " Turn 1 | Jan 1900 ", 
            VerticalAlignment = VerticalAlignment.Center 
        };
        _turnLabel.AddThemeFontSizeOverride("font_size", 20);

        _statsLabel = new Label 
        { 
            Text = " TA: ... WA: ... BSA: ... [FAI: ...] ", 
            VerticalAlignment = VerticalAlignment.Center
        };
        _statsLabel.AddThemeFontSizeOverride("font_size", 20);

        leftMargin.AddChild(_turnLabel);
        rightMargin.AddChild(_statsLabel);

        hbox.AddChild(leftMargin);
        hbox.AddChild(rightMargin);

        // Subscribe to events
        EventBus.Instance?.Subscribe<TurnAdvancedEvent>(ev => {
            _turnLabel.Text = $" Turn {ev.Turn} | M{ev.Month} Y{ev.Year} ";
        });
    }

    public override void _Process(double delta)
    {
        // Simple data binding
        var data = WorldStateManager.Instance?.Data;
        if (data != null && data.Characters.Count > 0)
        {
            var pc = data.Characters.Find(c => c.IsPlayer);
            if (pc != null)
            {
                int natIdx = int.Parse(pc.NationId.Split('_')[1]);
                var nat = data.Nations[natIdx];
                _statsLabel.Text = $" Treasury: ${nat.Treasury:0}M  |  {pc.Role} {pc.Name}  |  TA: {pc.TerritoryAuthority:0}%  WA: {pc.WorldAuthority:0}%  BSA: {pc.BehindTheScenesAuthority:0}%  [FAI: {pc.FullAuthorityIndex:0}%] ";
            }
        }
    }
}
