using Godot;
using System.Collections.Generic;
using Warship.Core;
using Warship.Events;

namespace Warship.UI.HUD;

/// <summary>
/// Displays transient popup messages (toast notifications) for political action results.
/// </summary>
public partial class NotificationManager : Control
{
    private VBoxContainer _container = null!;
    private readonly List<Control> _activePopups = new();

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);

        OffsetTop = UITheme.TopBarsTotal + UITheme.TabBarHeight + UITheme.PaddingSmall;
        OffsetBottom = 600;
        OffsetLeft = UITheme.LeftSidebarWidth + UITheme.SidebarGap;
        OffsetRight = UITheme.LeftSidebarWidth + UITheme.SidebarGap + 300;

        MouseFilter = MouseFilterEnum.Ignore;

        _container = new VBoxContainer();
        _container.AddThemeConstantOverride("separation", UITheme.PaddingSmall);
        _container.MouseFilter = MouseFilterEnum.Ignore;
        _container.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_container);

        EventBus.Instance?.Subscribe<NotificationEvent>(OnNotification);
    }

    private void OnNotification(NotificationEvent ev)
    {
        CallDeferred(nameof(ShowToast), ev.Message, ev.Type);
    }

    private void ShowToast(string message, string type)
    {
        var panel = new PanelContainer();
        panel.MouseFilter = MouseFilterEnum.Ignore;
        panel.AddThemeStyleboxOverride("panel", UITheme.ToastStyle(type));

        var lbl = new Label
        {
            Text = message,
            AutowrapMode = TextServer.AutowrapMode.Word,
            CustomMinimumSize = new Vector2(250, 0)
        };
        lbl.AddThemeFontSizeOverride("font_size", UITheme.FontBody);
        lbl.AddThemeColorOverride("font_color", UITheme.TextPrimary);
        panel.AddChild(lbl);

        _container.AddChild(panel);
        _activePopups.Add(panel);

        GetTree().CreateTimer(4.0f).Timeout += () =>
        {
            if (IsInstanceValid(panel))
            {
                var tween = CreateTween();
                tween.TweenProperty(panel, "modulate:a", 0f, 0.5f);
                tween.TweenCallback(Callable.From(() =>
                {
                    _activePopups.Remove(panel);
                    panel.QueueFree();
                }));
            }
        };
    }
}
