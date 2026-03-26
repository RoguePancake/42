using Godot;
using Warship.Core;
using Warship.Events;

namespace Warship.UI.HUD;

/// <summary>
/// Left Column — 250px wide action menu with categorized command buttons.
/// Categories: Diplomatic, Military, Economic, Intelligence.
/// </summary>
public partial class LeftSidebar : Control
{
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.LeftWide);
        OffsetTop = 64;   // Below both top bars (32 + 32)
        OffsetRight = 250; // 250px wide
        OffsetBottom = 0;

        // Background
        var bg = new Panel();
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.11f, 0.13f, 1f),
            BorderColor = new Color(0.2f, 0.22f, 0.25f, 1f),
            BorderWidthRight = 2
        };
        bg.AddThemeStyleboxOverride("panel", style);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // Scrollable content
        var scroll = new ScrollContainer();
        scroll.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        AddChild(scroll);

        var vbox = new VBoxContainer();
        vbox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        vbox.AddThemeConstantOverride("separation", 0);
        scroll.AddChild(vbox);

        // Header
        var header = new Label
        {
            Text = "COMMANDS",
            HorizontalAlignment = HorizontalAlignment.Center,
            CustomMinimumSize = new Vector2(0, 36)
        };
        header.AddThemeFontSizeOverride("font_size", 13);
        header.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
        header.VerticalAlignment = VerticalAlignment.Center;
        vbox.AddChild(header);

        // Diplomatic
        AddCategoryHeader(vbox, "DIPLOMATIC", new Color(0.3f, 0.6f, 1f));
        AddActionButton(vbox, "diplomatic", "Propose Alliance");
        AddActionButton(vbox, "diplomatic", "Trade Agreement");
        AddActionButton(vbox, "diplomatic", "Send Envoy");
        AddActionButton(vbox, "diplomatic", "Declare War");

        // Military
        AddCategoryHeader(vbox, "MILITARY", new Color(1f, 0.4f, 0.3f));
        AddActionButton(vbox, "military", "Border Watch");
        AddActionButton(vbox, "military", "Patrol");
        AddActionButton(vbox, "military", "Stage Army");
        AddActionButton(vbox, "military", "Attack");

        // Economic
        AddCategoryHeader(vbox, "ECONOMIC", new Color(0.3f, 0.9f, 0.4f));
        AddActionButton(vbox, "economic", "Adjust Budget");
        AddActionButton(vbox, "economic", "Set Tariffs");
        AddActionButton(vbox, "economic", "Open Trade Route");

        // Intelligence
        AddCategoryHeader(vbox, "INTELLIGENCE", new Color(0.8f, 0.6f, 1f));
        AddActionButton(vbox, "intelligence", "Deploy Spy");
        AddActionButton(vbox, "intelligence", "Counter-Intel");
        AddActionButton(vbox, "intelligence", "Sabotage");
    }

    private void AddCategoryHeader(VBoxContainer parent, string text, Color accentColor)
    {
        var container = new PanelContainer();
        var headerStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.09f, 0.11f, 1f),
            BorderColor = accentColor,
            BorderWidthLeft = 4,
            ContentMarginLeft = 12,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
        container.AddThemeStyleboxOverride("panel", headerStyle);

        var label = new Label
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_color", accentColor);
        container.AddChild(label);
        parent.AddChild(container);
    }

    private void AddActionButton(VBoxContainer parent, string category, string text)
    {
        var btn = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, 40),
            Alignment = HorizontalAlignment.Left
        };

        var normal = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.13f, 0.16f, 1f),
            BorderColor = new Color(0.15f, 0.17f, 0.2f, 1f),
            BorderWidthBottom = 1,
            ContentMarginLeft = 20
        };

        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = new Color(0.2f, 0.22f, 0.28f, 1f);

        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.AddThemeStyleboxOverride("pressed", hover);
        btn.AddThemeFontSizeOverride("font_size", 14);
        btn.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.8f));

        // Wire button to publish PlayerActionEvent via EventBus
        string actionId = text.ToLower().Replace(" ", "_");
        btn.Pressed += () =>
        {
            GD.Print($"[LeftSidebar] Action: {category}/{actionId}");
            EventBus.Instance?.Publish(new PlayerActionEvent(category, actionId));
        };

        parent.AddChild(btn);
    }
}
