using Godot;
using Warship.Core;
using Warship.Events;

namespace Warship.UI.HUD;

/// <summary>
/// A scrolling marquee at the bottom of the screen reporting live geopolitical events.
/// Adds immense immersion and life to the simulation.
/// </summary>
public partial class NewsTicker : Control
{
    private Label _scrollLabel = null!;
    private float _scrollSpeed = 80f; // Pixels per second
    private string _currentHeadlines = " /// SYSTEM ONLINE /// INITIATING GLOBAL MONITORING /// SECURE LINE ESTABLISHED /// ";

    public override void _Ready()
    {
        // Anchor to the very bottom of the screen
        SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
        CustomMinimumSize = new Vector2(0, 32);

        // Dark tech background
        var bg = new ColorRect
        {
            Color = new Color(0.04f, 0.04f, 0.06f, 0.98f),
            AnchorsPreset = (int)LayoutPreset.FullRect
        };
        AddChild(bg);

        // Top border
        var border = new ColorRect
        {
            Color = new Color(0.2f, 0.2f, 0.3f, 1f),
            CustomMinimumSize = new Vector2(0, 2)
        };
        border.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        AddChild(border);

        // Clipping wrapper so text doesn't display outside the ticker box
        var clipBox = new Control
        {
            ClipContents = true,
            AnchorsPreset = (int)LayoutPreset.FullRect
        };
        AddChild(clipBox);

        _scrollLabel = new Label
        {
            Text = _currentHeadlines,
            VerticalAlignment = VerticalAlignment.Center
        };
        _scrollLabel.AddThemeFontSizeOverride("font_size", 16);
        _scrollLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.8f, 1f)); // Sci-fi blue
        clipBox.AddChild(_scrollLabel);

        // Position it off screen to start scrolling
        _scrollLabel.Position = new Vector2(GetViewportRect().Size.X + 100, 4);

        // Subscribe to world events
        EventBus.Instance?.Subscribe<NotificationEvent>(OnGlobalEvent);
        EventBus.Instance?.Subscribe<CrisisTriggeredEvent>(OnCrisis);
    }

    private void OnGlobalEvent(NotificationEvent ev)
    {
        CallDeferred(nameof(AppendHeadline), ev.Message.ToUpper());
    }
    
    private void OnCrisis(CrisisTriggeredEvent ev)
    {
        CallDeferred(nameof(AppendHeadline), $"BREAKING NEWS: {ev.Title.ToUpper()} - {ev.Description.Substring(0, Mathf.Min(ev.Description.Length, 60))}...");
    }

    private void AppendHeadline(string headline)
    {
        _currentHeadlines += $"   ///   {headline} ";
        _scrollLabel.Text = _currentHeadlines;
    }

    public override void _Process(double delta)
    {
        // Scroll left continuously
        var pos = _scrollLabel.Position;
        pos.X -= _scrollSpeed * (float)delta;
        
        // If the entire text scrolled way past the left edge, restart it
        if (pos.X < -_scrollLabel.Size.X)
        {
            pos.X = GetViewportRect().Size.X + 100;
        }
        
        _scrollLabel.Position = pos;
    }
}
