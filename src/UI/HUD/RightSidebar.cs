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
        OffsetTop = 64;     // Below both top bars (32 + 32)
        OffsetLeft = -250;  // 250px wide
        OffsetBottom = 0;

        // Background
        var bg = new Panel();
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.11f, 0.13f, 1f),
            BorderColor = new Color(0.2f, 0.22f, 0.25f, 1f),
            BorderWidthLeft = 2
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
            Text = "INTEL & DIPLOMACY",
            HorizontalAlignment = HorizontalAlignment.Center,
            CustomMinimumSize = new Vector2(0, 36)
        };
        header.AddThemeFontSizeOverride("font_size", 13);
        header.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
        header.VerticalAlignment = VerticalAlignment.Center;
        vbox.AddChild(header);

        // Diplomacy section
        AddSectionHeader(vbox, "RELATIONS", new Color(0.3f, 0.6f, 1f));

        _relationsBox = new VBoxContainer();
        _relationsBox.AddThemeConstantOverride("separation", 0);
        vbox.AddChild(_relationsBox);

        // Intel section
        AddSectionHeader(vbox, "SPY NETWORK", new Color(0.8f, 0.6f, 1f));
        AddIntelRow(vbox, "Active Agents", "0");
        AddIntelRow(vbox, "Intel Quality", "LOW");
        AddIntelRow(vbox, "Counter-Intel", "NORMAL");

        // Populate relations from world data
        CallDeferred(nameof(PopulateRelations));

        EventBus.Instance?.Subscribe<TurnAdvancedEvent>(_ => CallDeferred(nameof(PopulateRelations)));
    }

    private void PopulateRelations()
    {
        foreach (var child in _relationsBox.GetChildren())
            child.QueueFree();

        var data = WorldStateManager.Instance?.Data;
        if (data == null || data.PlayerNationId == null) return;

        int pIdx = int.Parse(data.PlayerNationId.Split('_')[1]);
        var playerNation = data.Nations[pIdx];

        foreach (var nation in data.Nations)
        {
            if (nation.Id == data.PlayerNationId) continue;
            if (!nation.IsAlive) continue;

            var dipStatus = playerNation.Relations.GetValueOrDefault(nation.Id, Data.DiplomaticStatus.Neutral);

            var status = dipStatus switch
            {
                Data.DiplomaticStatus.Allied => "ALLIED",
                Data.DiplomaticStatus.Friendly => "FRIENDLY",
                Data.DiplomaticStatus.Neutral => "NEUTRAL",
                Data.DiplomaticStatus.Wary => "WARY",
                Data.DiplomaticStatus.Hostile => "HOSTILE",
                Data.DiplomaticStatus.AtWar => "AT WAR",
                _ => "UNKNOWN"
            };

            var color = dipStatus switch
            {
                Data.DiplomaticStatus.Allied => new Color(0.2f, 0.8f, 1f),
                Data.DiplomaticStatus.Friendly => new Color(0.3f, 0.9f, 0.4f),
                Data.DiplomaticStatus.Neutral => new Color(0.6f, 0.6f, 0.7f),
                Data.DiplomaticStatus.Wary => new Color(0.9f, 0.7f, 0.2f),
                Data.DiplomaticStatus.Hostile => new Color(1f, 0.4f, 0.2f),
                Data.DiplomaticStatus.AtWar => new Color(1f, 0.15f, 0.15f),
                _ => new Color(0.5f, 0.5f, 0.5f)
            };

            AddRelationRow(nation.Name, status, color, nation.NationColor);
        }
    }

    private void AddSectionHeader(VBoxContainer parent, string text, Color accentColor)
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

    private void AddRelationRow(string nationName, string status, Color statusColor, Color nationColor)
    {
        var row = new PanelContainer();
        var rowStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.13f, 0.16f, 1f),
            BorderColor = new Color(0.15f, 0.17f, 0.2f, 1f),
            BorderWidthBottom = 1,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
        row.AddThemeStyleboxOverride("panel", rowStyle);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 8);

        // Nation color indicator
        var colorDot = new ColorRect
        {
            Color = nationColor,
            CustomMinimumSize = new Vector2(8, 8)
        };
        hbox.AddChild(colorDot);

        // Nation name
        var nameLabel = new Label
        {
            Text = nationName,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 13);
        nameLabel.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.8f));
        hbox.AddChild(nameLabel);

        // Status
        var statusLabel = new Label
        {
            Text = status,
            VerticalAlignment = VerticalAlignment.Center
        };
        statusLabel.AddThemeFontSizeOverride("font_size", 11);
        statusLabel.AddThemeColorOverride("font_color", statusColor);
        hbox.AddChild(statusLabel);

        row.AddChild(hbox);
        _relationsBox.AddChild(row);
    }

    private void AddIntelRow(VBoxContainer parent, string label, string value)
    {
        var row = new PanelContainer();
        var rowStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.13f, 0.16f, 1f),
            BorderColor = new Color(0.15f, 0.17f, 0.2f, 1f),
            BorderWidthBottom = 1,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
        row.AddThemeStyleboxOverride("panel", rowStyle);

        var hbox = new HBoxContainer();

        var nameLabel = new Label
        {
            Text = label,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        };
        nameLabel.AddThemeFontSizeOverride("font_size", 13);
        nameLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.65f));
        hbox.AddChild(nameLabel);

        var valueLabel = new Label
        {
            Text = value,
            VerticalAlignment = VerticalAlignment.Center
        };
        valueLabel.AddThemeFontSizeOverride("font_size", 13);
        valueLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.6f, 1f));
        hbox.AddChild(valueLabel);

        row.AddChild(hbox);
        parent.AddChild(row);
    }
}
