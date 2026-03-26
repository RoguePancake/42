using Godot;
using Warship.Core;
using Warship.Events;

namespace Warship.UI.HUD;

/// <summary>
/// Right Column — 250px wide intel/diplomacy panel.
/// Shows diplomatic relations with each nation and spy intel status.
/// </summary>
public partial class RightSidebar : Control
{
    private VBoxContainer _relationsBox = null!;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.RightWide);
        OffsetTop = UITheme.TopBarsTotal;
        OffsetLeft = -UITheme.RightSidebarWidth;
        OffsetBottom = 0;

        var bg = new Panel();
        bg.AddThemeStyleboxOverride("panel", UITheme.SidebarStyle(rightBorder: false));
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

        var header = new Label
        {
            Text = "INTEL & DIPLOMACY",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            CustomMinimumSize = new Vector2(0, UITheme.RowHeight)
        };
        header.AddThemeFontSizeOverride("font_size", UITheme.FontSmall);
        header.AddThemeColorOverride("font_color", UITheme.TextDim);
        vbox.AddChild(header);

        AddSectionHeader(vbox, "RELATIONS", UITheme.CatDiplomatic);

        _relationsBox = new VBoxContainer();
        _relationsBox.AddThemeConstantOverride("separation", 0);
        vbox.AddChild(_relationsBox);

        AddSectionHeader(vbox, "SPY NETWORK", UITheme.CatIntelligence);
        AddIntelRow(vbox, "Active Agents", "0");
        AddIntelRow(vbox, "Intel Quality", "LOW");
        AddIntelRow(vbox, "Counter-Intel", "NORMAL");

        CallDeferred(nameof(PopulateRelations));
        EventBus.Instance?.Subscribe<TurnAdvancedEvent>(_ => CallDeferred(nameof(PopulateRelations)));
    }

    private void PopulateRelations()
    {
        foreach (var child in _relationsBox.GetChildren())
            child.QueueFree();

        var data = WorldStateManager.Instance?.Data;
        if (data == null) return;

        foreach (var nation in data.Nations)
        {
            if (nation.IsPlayer) continue;

            var status = nation.Archetype switch
            {
                Data.NationArchetype.Hegemon => "HOSTILE",
                Data.NationArchetype.Commercial => "NEUTRAL",
                Data.NationArchetype.Revolutionary => "WARY",
                Data.NationArchetype.Traditionalist => "COOL",
                Data.NationArchetype.Survival => "FRIENDLY",
                _ => "UNKNOWN"
            };

            var color = status switch
            {
                "HOSTILE"  => UITheme.StatusHostile,
                "WARY"     => UITheme.StatusWary,
                "COOL"     => UITheme.StatusCool,
                "NEUTRAL"  => UITheme.StatusNeutral,
                "FRIENDLY" => UITheme.StatusFriendly,
                _          => UITheme.TextDim
            };

            AddRelationRow(nation.Name, status, color, nation.NationColor);
        }
    }

    private void AddSectionHeader(VBoxContainer parent, string text, Color accentColor)
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

    private void AddRelationRow(string nationName, string status, Color statusColor, Color nationColor)
    {
        var row = new PanelContainer();
        row.AddThemeStyleboxOverride("panel", UITheme.RowStyle());

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", UITheme.PaddingSmall);

        var colorDot = new ColorRect
        {
            Color = nationColor,
            CustomMinimumSize = new Vector2(8, 8)
        };
        hbox.AddChild(colorDot);

        var nameLabel = new Label
        {
            Text = nationName,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        nameLabel.AddThemeFontSizeOverride("font_size", UITheme.FontSmall);
        nameLabel.AddThemeColorOverride("font_color", UITheme.TextPrimary);
        hbox.AddChild(nameLabel);

        var statusLabel = new Label
        {
            Text = status,
            VerticalAlignment = VerticalAlignment.Center
        };
        statusLabel.AddThemeFontSizeOverride("font_size", UITheme.FontTiny);
        statusLabel.AddThemeColorOverride("font_color", statusColor);
        hbox.AddChild(statusLabel);

        row.AddChild(hbox);
        _relationsBox.AddChild(row);
    }

    private void AddIntelRow(VBoxContainer parent, string label, string value)
    {
        var row = new PanelContainer();
        row.AddThemeStyleboxOverride("panel", UITheme.RowStyle());

        var hbox = new HBoxContainer();

        var nameLabel = new Label
        {
            Text = label,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        nameLabel.AddThemeFontSizeOverride("font_size", UITheme.FontSmall);
        nameLabel.AddThemeColorOverride("font_color", UITheme.TextSecondary);
        hbox.AddChild(nameLabel);

        var valueLabel = new Label
        {
            Text = value,
            VerticalAlignment = VerticalAlignment.Center
        };
        valueLabel.AddThemeFontSizeOverride("font_size", UITheme.FontSmall);
        valueLabel.AddThemeColorOverride("font_color", UITheme.CatIntelligence);
        hbox.AddChild(valueLabel);

        row.AddChild(hbox);
        parent.AddChild(row);
    }
}
