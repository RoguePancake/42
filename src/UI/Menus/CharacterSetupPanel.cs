using System.Linq;
using Godot;
using Warship.Core;
using Warship.Data;
using Warship.World;

namespace Warship.UI.Menus;

public partial class CharacterSetupPanel : Control
{
    private const int CustomNationIdx = 13; // index for "Custom Nation..." entry

    private OptionButton _nationDropdown = null!;
    private OptionButton _roleDropdown = null!;
    private LineEdit _nameInput = null!;
    private OptionButton _focusDropdown = null!;
    private Label _nationDesc = null!;
    private Button _startButton = null!;

    // Custom nation fields (hidden unless Custom is selected)
    private Control _customPanel = null!;
    private LineEdit _customNameInput = null!;
    private OptionButton _customArchetypeDropdown = null!;
    private Label _customInfo = null!;

    private string[] _roles = {
        "Head of State",
        "Defense Minister",
        "Foreign Minister",
        "Director of Intelligence",
        "Chief of Staff",
        "Finance Minister",
        "Interior Minister",
        "Opposition Leader"
    };

    private string[] _focuses = {
        "Balanced",
        "Territory Control (+TA)",
        "Global Influence (+WA)",
        "Shadow Broker (+BSA)"
    };

    // Archetypes available for custom nation (subset — not all make sense for player-built)
    private NationArchetype[] _customArchetypes = {
        NationArchetype.Hegemon, NationArchetype.Commercial, NationArchetype.Revolutionary,
        NationArchetype.Traditionalist, NationArchetype.Industrial, NationArchetype.Naval,
        NationArchetype.FreeState, NationArchetype.Guerrilla, NationArchetype.Intelligence,
    };

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var bg = new ColorRect {
            Color = new Color(0.02f, 0.02f, 0.05f, 0.98f),
            AnchorsPreset = (int)LayoutPreset.FullRect
        };
        AddChild(bg);

        var scroll = new ScrollContainer {
            AnchorsPreset = (int)LayoutPreset.FullRect
        };
        AddChild(scroll);

        var center = new CenterContainer {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(500, 0),
        };
        scroll.AddChild(center);

        var panel = new PanelContainer();
        var style = new StyleBoxFlat {
            BgColor = new Color(0.1f, 0.12f, 0.16f, 1f),
            BorderColor = new Color(0.83f, 0.66f, 0.29f, 1f),
            BorderWidthTop = 2, BorderWidthBottom = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
            CornerRadiusTopLeft = 8, CornerRadiusBottomRight = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8
        };
        panel.AddThemeStyleboxOverride("panel", style);
        center.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 30);
        margin.AddThemeConstantOverride("margin_right", 30);
        margin.AddThemeConstantOverride("margin_top", 30);
        margin.AddThemeConstantOverride("margin_bottom", 30);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        margin.AddChild(vbox);

        var title = new Label { Text = "EXECUTIVE CLEARANCE REQUIRED" };
        title.AddThemeFontSizeOverride("font_size", 24);
        title.AddThemeColorOverride("font_color", new Color(0.83f, 0.66f, 0.29f, 1f));
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        vbox.AddChild(new HSeparator());

        // ── Nation picker ──
        vbox.AddChild(new Label { Text = "Select Nation:" });
        _nationDropdown = new OptionButton();
        for (int i = 0; i < WorldGenerator.Templates.Length; i++)
        {
            var t = WorldGenerator.Templates[i];
            string tier = t.Tier == NationTier.Large ? "Major" : "Minor";
            _nationDropdown.AddItem($"{t.Name}  [{tier} \u2014 {t.Archetype}]");
        }
        _nationDropdown.AddItem("\u2726 Custom Nation...");
        _nationDropdown.Selected = 6; // Default: Selvara
        _nationDropdown.ItemSelected += OnNationChanged;
        vbox.AddChild(_nationDropdown);

        // Nation description
        _nationDesc = new Label {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(420, 0),
        };
        _nationDesc.AddThemeFontSizeOverride("font_size", 13);
        _nationDesc.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.8f, 1f));
        vbox.AddChild(_nationDesc);
        UpdateNationDescription(6);

        // ── Custom nation panel (hidden by default) ──
        _customPanel = new VBoxContainer();
        ((VBoxContainer)_customPanel).AddThemeConstantOverride("separation", 8);
        _customPanel.Visible = false;
        vbox.AddChild(_customPanel);

        _customPanel.AddChild(new HSeparator());
        var customTitle = new Label { Text = "FORGE YOUR NATION" };
        customTitle.AddThemeFontSizeOverride("font_size", 16);
        customTitle.AddThemeColorOverride("font_color", new Color(0.83f, 0.66f, 0.29f, 1f));
        _customPanel.AddChild(customTitle);

        _customPanel.AddChild(new Label { Text = "Nation Name:" });
        _customNameInput = new LineEdit { PlaceholderText = "E.g. Republic of Novagrad", Text = "" };
        _customPanel.AddChild(_customNameInput);

        _customPanel.AddChild(new Label { Text = "Government Type:" });
        _customArchetypeDropdown = new OptionButton();
        foreach (var a in _customArchetypes)
            _customArchetypeDropdown.AddItem(a.ToString());
        _customArchetypeDropdown.Selected = 6; // FreeState default
        _customPanel.AddChild(_customArchetypeDropdown);

        _customInfo = new Label {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(420, 0),
        };
        _customInfo.AddThemeFontSizeOverride("font_size", 12);
        _customInfo.AddThemeColorOverride("font_color", new Color(0.6f, 0.65f, 0.5f, 1f));
        _customInfo.Text = "After setup, click the map to place your capital.\n" +
            "Starting resources will be derived from the terrain around your capital.\n" +
            "You'll start as a Small nation: 4 cities, 2 armies.";
        _customPanel.AddChild(_customInfo);

        vbox.AddChild(new HSeparator());

        // Role
        vbox.AddChild(new Label { Text = "Select Role:" });
        _roleDropdown = new OptionButton();
        foreach (var r in _roles) _roleDropdown.AddItem(r);
        _roleDropdown.Selected = 1;
        vbox.AddChild(_roleDropdown);

        // Name
        vbox.AddChild(new Label { Text = "Enter Name:" });
        _nameInput = new LineEdit { PlaceholderText = "E.g. J. Crawford", Text = "J. Crawford" };
        vbox.AddChild(_nameInput);

        // Focus
        vbox.AddChild(new Label { Text = "Strategic Focus:" });
        _focusDropdown = new OptionButton();
        foreach (var f in _focuses) _focusDropdown.AddItem(f);
        vbox.AddChild(_focusDropdown);

        vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 6) });

        _startButton = new Button { Text = "INITIALIZE CLEARANCE" };
        _startButton.AddThemeFontSizeOverride("font_size", 18);
        var btnStyle = new StyleBoxFlat {
            BgColor = new Color(0.83f, 0.66f, 0.29f, 1f),
            CornerRadiusTopLeft = 4, CornerRadiusBottomRight = 4,
            CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4
        };
        _startButton.AddThemeStyleboxOverride("normal", btnStyle);
        _startButton.AddThemeColorOverride("font_color", new Color(0.1f, 0.1f, 0.1f, 1f));
        _startButton.Pressed += OnStartPressed;
        vbox.AddChild(_startButton);
    }

    private void OnNationChanged(long index)
    {
        bool isCustom = (int)index == CustomNationIdx;
        _customPanel.Visible = isCustom;

        if (isCustom)
        {
            _nationDesc.Text = "Build your own nation from scratch. Choose a name, government type, " +
                "and place your capital on the map. Your starting resources, terrain advantages, " +
                "and military composition will be determined by where you settle.";
            _startButton.Text = "PLACE CAPITAL ON MAP";
        }
        else
        {
            _startButton.Text = "INITIALIZE CLEARANCE";
            UpdateNationDescription((int)index);
        }
    }

    private void UpdateNationDescription(int index)
    {
        if (index < 0 || index >= WorldGenerator.Templates.Length) return;
        var t = WorldGenerator.Templates[index];
        string traits = t.Traits.Length > 0
            ? string.Join(", ", t.Traits.Select(tr => FormatTrait(tr)))
            : "None";
        _nationDesc.Text = $"{t.Description}\n\n" +
            $"Traits: {traits}\n" +
            $"Cities: {t.CityCount}  |  Armies: {t.ArmyCount}  |  Treasury: {t.StartingTreasury:F0}";
    }

    private static string FormatTrait(NationTrait trait) => trait switch
    {
        NationTrait.CarrierDoctrine => "Carrier Doctrine",
        NationTrait.MassConscription => "Mass Conscription",
        NationTrait.FortressDefense => "Fortress Defense",
        NationTrait.ArmoredBlitz => "Armored Blitz",
        NationTrait.NavalSupremacy => "Naval Supremacy",
        NationTrait.NuclearDeterrent => "Nuclear Deterrent",
        NationTrait.SubmarineWolf => "Submarine Wolf Pack",
        NationTrait.TradeEmpire => "Trade Empire",
        NationTrait.IndustrialBase => "Industrial Base",
        NationTrait.RareEarthMonopoly => "Rare Earth Monopoly",
        NationTrait.OilWeapon => "Oil Weapon",
        NationTrait.SovereignWealth => "Sovereign Wealth",
        NationTrait.SpyMaster => "Spy Master",
        NationTrait.NeutralBroker => "Neutral Broker",
        NationTrait.GuerrillaResistance => "Guerrilla Resistance",
        NationTrait.RemnantPride => "Remnant Pride",
        NationTrait.NuclearAmbiguity => "Nuclear Ambiguity",
        NationTrait.ProliferationTarget => "Proliferation Target",
        NationTrait.PorcupineDefense => "Porcupine Defense",
        NationTrait.UnsiegeableDesert => "Unsiegeable Desert",
        NationTrait.CorporateDiplomacy => "Corporate Diplomacy",
        _ => trait.ToString()
    };

    private void OnStartPressed()
    {
        string selectedRole = _roles[_roleDropdown.Selected];
        string playerName = string.IsNullOrWhiteSpace(_nameInput.Text) ? "Unknown Official" : _nameInput.Text;
        int focusIndex = _focusDropdown.Selected;
        int nationIndex = _nationDropdown.Selected;

        if (nationIndex == CustomNationIdx)
        {
            // Custom nation flow: generate world with 12 AI nations, then enter map-click mode
            string customName = string.IsNullOrWhiteSpace(_customNameInput.Text) ? "New Republic" : _customNameInput.Text;
            var archetype = _customArchetypes[_customArchetypeDropdown.Selected];

            WorldStateManager.Instance?.InitializeCustomNation(
                selectedRole, playerName, focusIndex, customName, archetype);
        }
        else
        {
            WorldStateManager.Instance?.InitializeWorld(selectedRole, playerName, focusIndex, nationIndex);
        }

        QueueFree();
    }
}
