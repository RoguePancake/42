using Godot;
using System.Collections.Generic;
using Warship.Core;
using Warship.Events;

namespace Warship.UI.HUD;

/// <summary>
/// Displays transient popup messages (toast notifications) for political action results.
/// Example: "Assassination Failed!" or "Funded Militia: TA +5%"
/// </summary>
public partial class NotificationManager : Control
{
    private VBoxContainer _container = null!;
    
    // Track active notifications to remove them later
    private readonly List<Control> _activePopups = new();

    public override void _Ready()
    {
        // Anchoring to Top-Left explicitly prevents it from hiding under the Dossier panel
        SetAnchorsAndOffsetsPreset(LayoutPreset.TopLeft);
        
        // Push it inward to avoid MacOS/UI cropping and clear the new LeftSidebar
        OffsetTop = 90; // Just below TopBar in the map viewport
        OffsetBottom = 600; // ample height
        OffsetLeft = 270; // 250 for sidebar + 20 safe margin
        OffsetRight = 570; // 300px width
        
        // Mouse filter to let clicks pass through
        MouseFilter = MouseFilterEnum.Ignore;

        _container = new VBoxContainer();
        _container.AddThemeConstantOverride("separation", 10);
        _container.MouseFilter = MouseFilterEnum.Ignore;
        // The container fills the rigid bounds we just defined
        _container.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_container);

        EventBus.Instance?.Subscribe<NotificationEvent>(OnNotification);
    }

    private void OnNotification(NotificationEvent ev)
    {
        // Must defer to main thread since EventBus might fire from elsewhere
        CallDeferred(nameof(ShowToast), ev.Message, ev.Type);
    }

    private void ShowToast(string message, string type)
    {
        var panel = new PanelContainer();
        panel.MouseFilter = MouseFilterEnum.Ignore;

        var style = new StyleBoxFlat
        {
            CornerRadiusTopLeft = 6, CornerRadiusBottomLeft = 6,
            CornerRadiusTopRight = 6, CornerRadiusBottomRight = 6,
            ContentMarginBottom = 12, ContentMarginTop = 12,
            ContentMarginLeft = 16, ContentMarginRight = 16,
            ShadowColor = new Color(0, 0, 0, 0.4f),
            ShadowSize = 4
        };

        // Darken base colors to look sleek
        style.BgColor = type switch
        {
            "success" => new Color(0.1f, 0.3f, 0.15f, 0.9f),
            "danger" => new Color(0.4f, 0.1f, 0.1f, 0.9f),
            "warning" => new Color(0.4f, 0.3f, 0.1f, 0.9f),
            _ => new Color(0.15f, 0.2f, 0.3f, 0.9f) // info
        };
        
        // Solid left border
        style.BorderWidthLeft = 4;
        style.BorderColor = type switch
        {
            "success" => new Color(0.3f, 0.8f, 0.4f),
            "danger" => new Color(0.9f, 0.2f, 0.2f),
            "warning" => new Color(0.9f, 0.7f, 0.2f),
            _ => new Color(0.3f, 0.6f, 0.9f)
        };

        panel.AddThemeStyleboxOverride("panel", style);

        var lbl = new Label
        {
            Text = message,
            AutowrapMode = TextServer.AutowrapMode.Word,
            CustomMinimumSize = new Vector2(250, 0)
        };
        lbl.AddThemeFontSizeOverride("font_size", 14);
        panel.AddChild(lbl);

        _container.AddChild(panel);
        _activePopups.Add(panel);

        // Auto-destroy after 4 seconds using SceneTreeTimer
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
