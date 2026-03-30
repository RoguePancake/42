using Godot;
using System;
using Warship.Core;
using Warship.Data;
using Warship.Events;
using Warship.UI.Map;

namespace Warship.UI.HUD;

/// <summary>
/// Build menu on the left side. Shows available buildings with costs.
/// Click a button to enter build mode, then click the map to place.
/// Press ESC or click same button again to cancel build mode.
/// </summary>
public partial class BuildMenu : Control
{
    private WorldData? _world;
    private MapManager? _mapManager;
    private BuildingType? _activeBuild;

    // Button layout
    private static readonly BuildingType[] BuildOptions =
    {
        BuildingType.TroopCamp,
        BuildingType.BorderWall,
        BuildingType.Road,
        BuildingType.Watchtower,
        BuildingType.Storehouse,
    };

    public override void _Ready()
    {
        EventBus.Instance?.Subscribe<WorldReadyEvent>(_ =>
        {
            _world = WorldStateManager.Instance?.World;
            _mapManager = GetNode<MapManager>("/root/Main/MapManager");
        });

        EventBus.Instance?.Subscribe<BuildingPlacedEvent>(_ =>
        {
            // Stay in build mode after placing (for walls/roads)
            // but deduct was already done by GameplayManager
        });

        EventBus.Instance?.Subscribe<BuildingFailedEvent>(ev =>
            GD.Print($"[BuildMenu] Build failed: {ev.Reason}"));

        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (e is InputEventKey key && key.Pressed)
        {
            // ESC cancels build mode
            if (key.Keycode == Key.Escape && _activeBuild != null)
            {
                _activeBuild = null;
                _mapManager?.SetBuildMode(null);
                GetViewport().SetInputAsHandled();
                QueueRedraw();
                return;
            }

            // Number keys 1-5 for quick build selection
            int num = key.Keycode switch
            {
                Key.Key1 => 0,
                Key.Key2 => 1,
                Key.Key3 => 2,
                Key.Key4 => 3,
                Key.Key5 => 4,
                _ => -1,
            };

            if (num >= 0 && num < BuildOptions.Length)
            {
                ToggleBuildMode(BuildOptions[num]);
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void ToggleBuildMode(BuildingType type)
    {
        if (_activeBuild == type)
        {
            _activeBuild = null;
            _mapManager?.SetBuildMode(null);
        }
        else
        {
            _activeBuild = type;
            _mapManager?.SetBuildMode(type);
        }
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_world == null) return;

        float panelW = 180;
        float startY = 40; // below top bar
        float btnH = 36;
        float gap = 4;

        // Panel background
        float panelH = BuildOptions.Length * (btnH + gap) + 30;
        DrawRect(new Rect2(0, startY, panelW, panelH), new Color(0.05f, 0.05f, 0.15f, 0.8f));

        var font = ThemeDB.FallbackFont;

        // Title
        DrawString(font, new Vector2(10, startY + 18), "BUILD [1-5]",
            HorizontalAlignment.Left, -1, 12, Colors.White);

        float y = startY + 28;
        for (int i = 0; i < BuildOptions.Length; i++)
        {
            var type = BuildOptions[i];
            int cost = BuildingInfo.GoldCost(type);
            bool canAfford = _world.Player.Gold >= cost;
            bool active = _activeBuild == type;

            // Button background
            Color bgColor = active ? new Color(0.2f, 0.4f, 0.7f, 0.9f)
                           : canAfford ? new Color(0.12f, 0.12f, 0.25f, 0.9f)
                           : new Color(0.15f, 0.08f, 0.08f, 0.9f);
            DrawRect(new Rect2(4, y, panelW - 8, btnH), bgColor);

            // Border
            Color borderColor = active ? Colors.Gold : new Color(0.3f, 0.3f, 0.4f);
            DrawRect(new Rect2(4, y, panelW - 8, btnH), borderColor, false, 1f);

            // Text
            Color textColor = canAfford ? Colors.White : new Color(0.5f, 0.3f, 0.3f);
            string label = $"{i + 1}. {BuildingInfo.DisplayName(type)}";
            DrawString(font, new Vector2(10, y + 16), label,
                HorizontalAlignment.Left, -1, 11, textColor);
            DrawString(font, new Vector2(10, y + 30), $"   ${cost}",
                HorizontalAlignment.Left, -1, 10, canAfford ? Colors.Yellow : Colors.DarkRed);

            y += btnH + gap;
        }

        // Build mode indicator
        if (_activeBuild != null)
        {
            DrawString(font, new Vector2(10, y + 15),
                $"Placing: {BuildingInfo.DisplayName(_activeBuild.Value)}",
                HorizontalAlignment.Left, -1, 11, Colors.Gold);
            DrawString(font, new Vector2(10, y + 30),
                "Click map | ESC cancel",
                HorizontalAlignment.Left, -1, 10, Colors.LightGray);
        }
    }
}
