using Godot;
using Warship.Core;
using Warship.Events;

namespace Warship.UI.HUD;

/// <summary>
/// Tab bar that sits at the top of the Main Section area, letting the player
/// switch between different views (Map, Intel, War Room, Economy).
/// Publishes ViewSwitchEvent on tab change.
/// </summary>
public partial class MainViewSwitcher : Control
{
    private string _activeView = "map";
    private Button _mapBtn = null!;
    private Button _intelBtn = null!;
    private Button _warBtn = null!;
    private Button _econBtn = null!;

    public override void _Ready()
    {
        AnchorLeft = 0f;
        AnchorRight = 1f;
        AnchorTop = 0f;
        AnchorBottom = 0f;
        OffsetLeft = UITheme.LeftSidebarWidth;
        OffsetRight = -UITheme.RightSidebarWidth;
        OffsetTop = UITheme.TopBarsTotal;
        OffsetBottom = UITheme.TopBarsTotal + UITheme.TabBarHeight;
        MouseFilter = MouseFilterEnum.Stop;

        var bg = new ColorRect
        {
            Color = UITheme.BgDark,
            AnchorsPreset = (int)LayoutPreset.FullRect
        };
        AddChild(bg);

        var hbox = new HBoxContainer
        {
            AnchorsPreset = (int)LayoutPreset.FullRect
        };
        hbox.AddThemeConstantOverride("separation", 0);
        AddChild(hbox);

        _mapBtn = MakeTab("MAP", "map");
        _intelBtn = MakeTab("INTEL", "intel");
        _warBtn = MakeTab("WAR ROOM", "warroom");
        _econBtn = MakeTab("ECONOMY", "economy");

        hbox.AddChild(_mapBtn);
        hbox.AddChild(_intelBtn);
        hbox.AddChild(_warBtn);
        hbox.AddChild(_econBtn);

        UpdateTabStyles();

        EventBus.Instance?.Subscribe<ViewSwitchEvent>(ev =>
        {
            _activeView = ev.ViewId;
            CallDeferred(nameof(UpdateTabStyles));
        });
    }

    private Button MakeTab(string text, string viewId)
    {
        var btn = new Button
        {
            Text = text,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, UITheme.TabBarHeight)
        };
        btn.AddThemeFontSizeOverride("font_size", UITheme.FontSmall);

        btn.Pressed += () =>
        {
            _activeView = viewId;
            UpdateTabStyles();
            EventBus.Instance?.Publish(new ViewSwitchEvent(viewId));
        };

        return btn;
    }

    private void UpdateTabStyles()
    {
        StyleTab(_mapBtn, _activeView == "map");
        StyleTab(_intelBtn, _activeView == "intel");
        StyleTab(_warBtn, _activeView == "warroom");
        StyleTab(_econBtn, _activeView == "economy");
    }

    private void StyleTab(Button btn, bool active)
    {
        var style = new StyleBoxFlat
        {
            BgColor = active ? UITheme.BgActive : UITheme.BgPanel,
            BorderColor = active ? UITheme.AccentBlue : UITheme.BorderSubtle,
            BorderWidthBottom = active ? UITheme.BorderThick : UITheme.BorderThin
        };

        var hover = (StyleBoxFlat)style.Duplicate();
        hover.BgColor = active ? UITheme.BgActive : UITheme.BgHover;

        btn.AddThemeStyleboxOverride("normal", style);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", hover);
        btn.AddThemeStyleboxOverride("focus", hover);
        btn.AddThemeColorOverride("font_color", active ? Colors.White : UITheme.TextDim);
        btn.AddThemeColorOverride("font_hover_color", active ? Colors.White : UITheme.TextPrimary);
    }
}
