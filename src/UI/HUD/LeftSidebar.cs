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
        OffsetTop = UITheme.TopBarsTotal;
        OffsetRight = UITheme.LeftSidebarWidth;
        OffsetBottom = 0;

        var bg = new Panel();
        bg.AddThemeStyleboxOverride("panel", UITheme.SidebarStyle(rightBorder: true));
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

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
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(0, UITheme.RowHeight)
        };
        header.AddThemeFontSizeOverride("font_size", UITheme.FontSmall);
        header.AddThemeColorOverride("font_color", UITheme.TextDim);
        vbox.AddChild(header);

        // Diplomatic
        AddCategoryHeader(vbox, "DIPLOMATIC", UITheme.CatDiplomatic);
        AddActionButton(vbox, "diplomatic", "Propose Alliance", UITheme.CatDiplomatic);
        AddActionButton(vbox, "diplomatic", "Trade Agreement", UITheme.CatDiplomatic);
        AddActionButton(vbox, "diplomatic", "Send Envoy", UITheme.CatDiplomatic);
        AddActionButton(vbox, "diplomatic", "Declare War", UITheme.CatDiplomatic);

        // Military
        AddCategoryHeader(vbox, "MILITARY", UITheme.CatMilitary);
        AddActionButton(vbox, "military", "Border Watch", UITheme.CatMilitary);
        AddActionButton(vbox, "military", "Patrol", UITheme.CatMilitary);
        AddActionButton(vbox, "military", "Stage Army", UITheme.CatMilitary);
        AddActionButton(vbox, "military", "Attack", UITheme.CatMilitary);

        // Economic
        AddCategoryHeader(vbox, "ECONOMIC", UITheme.CatEconomic);
        AddActionButton(vbox, "economic", "Adjust Budget", UITheme.CatEconomic);
        AddActionButton(vbox, "economic", "Set Tariffs", UITheme.CatEconomic);
        AddActionButton(vbox, "economic", "Open Trade Route", UITheme.CatEconomic);

        // Intelligence
        AddCategoryHeader(vbox, "INTELLIGENCE", UITheme.CatIntelligence);
        AddActionButton(vbox, "intelligence", "Deploy Spy", UITheme.CatIntelligence);
        AddActionButton(vbox, "intelligence", "Counter-Intel", UITheme.CatIntelligence);
        AddActionButton(vbox, "intelligence", "Sabotage", UITheme.CatIntelligence);
    }

    private void AddCategoryHeader(VBoxContainer parent, string text, Color accentColor)
    {
        var container = new PanelContainer();
        container.AddThemeStyleboxOverride("panel", UITheme.CategoryHeaderStyle(accentColor));

        var label = new Label
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.AddThemeFontSizeOverride("font_size", UITheme.FontSmall);
        label.AddThemeColorOverride("font_color", accentColor);
        container.AddChild(label);
        parent.AddChild(container);
    }

    private void AddActionButton(VBoxContainer parent, string category, string text, Color accent)
    {
        var btn = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, UITheme.ButtonHeight),
            Alignment = HorizontalAlignment.Left
        };

        UITheme.ApplyButtonStyle(btn, accent);

        string actionId = text.ToLower().Replace(" ", "_");
        btn.Pressed += () =>
        {
            GD.Print($"[LeftSidebar] Action: {category}/{actionId}");
            EventBus.Instance?.Publish(new PlayerActionEvent(category, actionId));
        };

        parent.AddChild(btn);
    }
}
