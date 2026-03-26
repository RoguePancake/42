using Godot;
using Warship.Core;
using Warship.Events;

namespace Warship.UI.HUD;

/// <summary>
/// Top Bar A — a full-width notification/alert bar at the very top of the screen.
/// Displays color-coded alerts and notifications (replaces the old scrolling marquee).
/// </summary>
public partial class NewsTicker : Control
{
    private HBoxContainer _alertContainer = null!;
    private Label _latestAlert = null!;
    private ColorRect _bg = null!;
    private const int MaxVisibleAlerts = 3;

    // Alert colors by type
    private static readonly Color AlertInfo = new(0.3f, 0.6f, 1f);
    private static readonly Color AlertWarning = new(1f, 0.8f, 0.2f);
    private static readonly Color AlertDanger = new(1f, 0.3f, 0.3f);
    private static readonly Color AlertSuccess = new(0.3f, 0.9f, 0.4f);

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        CustomMinimumSize = new Vector2(0, 32);
        MouseFilter = MouseFilterEnum.Ignore;

        // Dark background
        _bg = new ColorRect
        {
            Color = new Color(0.04f, 0.04f, 0.06f, 0.98f),
            AnchorsPreset = (int)LayoutPreset.FullRect
        };
        AddChild(_bg);

        // Bottom border
        var border = new ColorRect
        {
            Color = new Color(0.2f, 0.2f, 0.3f, 1f),
            CustomMinimumSize = new Vector2(0, 2)
        };
        border.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
        AddChild(border);

        // Alert layout
        _alertContainer = new HBoxContainer
        {
            AnchorsPreset = (int)LayoutPreset.FullRect
        };
        _alertContainer.AddThemeConstantOverride("separation", 20);
        AddChild(_alertContainer);

        // Left icon/label
        var icon = new Label
        {
            Text = " ALERTS ",
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(80, 0)
        };
        icon.AddThemeFontSizeOverride("font_size", 12);
        icon.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
        _alertContainer.AddChild(icon);

        // Separator
        var sep = new VSeparator();
        _alertContainer.AddChild(sep);

        // Latest alert label (fills remaining space)
        _latestAlert = new Label
        {
            Text = "SYSTEM ONLINE — GLOBAL MONITORING ACTIVE",
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ClipText = true
        };
        _latestAlert.AddThemeFontSizeOverride("font_size", 13);
        _latestAlert.AddThemeColorOverride("font_color", AlertInfo);
        _alertContainer.AddChild(_latestAlert);

        // Subscribe to events
        EventBus.Instance?.Subscribe<NotificationEvent>(OnNotification);
        EventBus.Instance?.Subscribe<CrisisTriggeredEvent>(OnCrisis);
    }

    private void OnNotification(NotificationEvent ev)
    {
        CallDeferred(nameof(ShowAlert), ev.Message.ToUpper(), ev.Type);
    }

    private void OnCrisis(CrisisTriggeredEvent ev)
    {
        CallDeferred(nameof(ShowAlert), $"CRISIS: {ev.Title.ToUpper()}", "danger");
    }

    private void ShowAlert(string message, string type)
    {
        var color = type switch
        {
            "danger" => AlertDanger,
            "warning" => AlertWarning,
            "success" => AlertSuccess,
            _ => AlertInfo
        };

        _latestAlert.Text = message;
        _latestAlert.RemoveThemeColorOverride("font_color");
        _latestAlert.AddThemeColorOverride("font_color", color);

        // Flash the background briefly
        _bg.Color = new Color(color.R * 0.15f, color.G * 0.15f, color.B * 0.15f, 0.98f);
    }

    public override void _Process(double delta)
    {
        // Fade background back to default over time
        _bg.Color = _bg.Color.Lerp(new Color(0.04f, 0.04f, 0.06f, 0.98f), (float)delta * 2f);
    }
}
