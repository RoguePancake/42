using Godot;
using Warship.Core;
using Warship.Events;

namespace Warship.UI.HUD;

/// <summary>
/// Top Bar A — full-width alert bar at the very top of the screen.
/// Displays color-coded alerts and notifications.
/// </summary>
public partial class NewsTicker : Control
{
    private HBoxContainer _alertContainer = null!;
    private Label _latestAlert = null!;
    private ColorRect _bg = null!;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        CustomMinimumSize = new Vector2(0, UITheme.AlertBarHeight);
        MouseFilter = MouseFilterEnum.Ignore;

        _bg = new ColorRect
        {
            Color = UITheme.BgDarkest,
            AnchorsPreset = (int)LayoutPreset.FullRect
        };
        AddChild(_bg);

        var border = new ColorRect
        {
            Color = UITheme.BorderSubtle,
            CustomMinimumSize = new Vector2(0, 2)
        };
        border.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
        AddChild(border);

        _alertContainer = new HBoxContainer
        {
            AnchorsPreset = (int)LayoutPreset.FullRect
        };
        _alertContainer.AddThemeConstantOverride("separation", 16);
        AddChild(_alertContainer);

        var icon = new Label
        {
            Text = " ALERTS ",
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(80, 0)
        };
        icon.AddThemeFontSizeOverride("font_size", UITheme.FontSmall);
        icon.AddThemeColorOverride("font_color", UITheme.TextDim);
        _alertContainer.AddChild(icon);

        _alertContainer.AddChild(new VSeparator());

        _latestAlert = new Label
        {
            Text = "SYSTEM ONLINE — GLOBAL MONITORING ACTIVE",
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ClipText = true
        };
        _latestAlert.AddThemeFontSizeOverride("font_size", UITheme.FontSmall);
        _latestAlert.AddThemeColorOverride("font_color", UITheme.AlertInfo);
        _alertContainer.AddChild(_latestAlert);

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
            "danger"  => UITheme.AlertDanger,
            "warning" => UITheme.AlertWarning,
            "success" => UITheme.AlertSuccess,
            _         => UITheme.AlertInfo
        };

        _latestAlert.Text = message;
        _latestAlert.RemoveThemeColorOverride("font_color");
        _latestAlert.AddThemeColorOverride("font_color", color);

        // Flash background tinted to alert color
        _bg.Color = new Color(color.R * 0.12f, color.G * 0.12f, color.B * 0.12f, 0.98f);
    }

    public override void _Process(double delta)
    {
        // Fade background back to default
        _bg.Color = _bg.Color.Lerp(UITheme.BgDarkest, (float)delta * 2f);
    }
}
