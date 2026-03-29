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
    private Button _councilBtn = null!;

    // Layout constants matching sidebar/topbar positions
    private const float TopOffset = 64f;   // Below both top bars
    private const float LeftOffset = 250f;  // Right of LeftSidebar
    private const float RightOffset = 250f; // Left of RightSidebar
    private const float TabHeight = 32f;

    public override void _Ready()
    {
        // Position: full width between sidebars, just below top bars
        AnchorLeft = 0f;
        AnchorRight = 1f;
        AnchorTop = 0f;
        AnchorBottom = 0f;
        OffsetLeft = LeftOffset;
        OffsetRight = -RightOffset;
        OffsetTop = TopOffset;
        OffsetBottom = TopOffset + TabHeight;
        MouseFilter = MouseFilterEnum.Stop;

        // Background
        var bg = new ColorRect
        {
            Color = new Color(0.06f, 0.06f, 0.09f, 0.95f),
            AnchorsPreset = (int)LayoutPreset.FullRect
        };
        AddChild(bg);

        // Tab container
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
        _councilBtn = MakeTab("COUNCIL", "council");

        hbox.AddChild(_mapBtn);
        hbox.AddChild(_intelBtn);
        hbox.AddChild(_warBtn);
        hbox.AddChild(_econBtn);
        hbox.AddChild(_councilBtn);

        // Start with map active
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
            CustomMinimumSize = new Vector2(0, TabHeight)
        };
        btn.AddThemeFontSizeOverride("font_size", 12);

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
        StyleTab(_councilBtn, _activeView == "council");
    }

    private void StyleTab(Button btn, bool active)
    {
        var style = new StyleBoxFlat
        {
            BgColor = active
                ? new Color(0.15f, 0.25f, 0.5f, 1f)
                : new Color(0.08f, 0.09f, 0.12f, 1f),
            BorderColor = active
                ? new Color(0.3f, 0.5f, 0.9f, 1f)
                : new Color(0.15f, 0.17f, 0.2f, 1f),
            BorderWidthBottom = active ? 3 : 1
        };

        var hover = (StyleBoxFlat)style.Duplicate();
        hover.BgColor = new Color(0.18f, 0.28f, 0.55f, 1f);

        btn.AddThemeStyleboxOverride("normal", style);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", hover);
        btn.AddThemeColorOverride("font_color", active ? Colors.White : new Color(0.5f, 0.5f, 0.6f));
    }
}
