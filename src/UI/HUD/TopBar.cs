using Godot;
using Warship.Core;
using Warship.Data;
using Warship.Events;

namespace Warship.UI.HUD;

/// <summary>
/// Top bar HUD: shows player name, gold, food, and current tile info.
/// Draws as a dark bar across the top of the screen.
/// </summary>
public partial class TopBar : Control
{
    private WorldData? _world;

    public override void _Ready()
    {
        EventBus.Instance?.Subscribe<WorldReadyEvent>(_ =>
            _world = WorldStateManager.Instance?.World);

        CustomMinimumSize = new Vector2(0, 32);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_world == null) return;

        var size = GetViewportRect().Size;
        float barH = 32;

        // Background
        DrawRect(new Rect2(0, 0, size.X, barH), new Color(0.05f, 0.05f, 0.15f, 0.85f));

        var font = ThemeDB.FallbackFont;
        int fs = 14;
        var p = _world.Player;

        // Player name
        DrawString(font, new Vector2(10, 22), p.Name, HorizontalAlignment.Left, -1, fs, Colors.Gold);

        // Gold
        DrawString(font, new Vector2(180, 22), $"Gold: {p.Gold}", HorizontalAlignment.Left, -1, fs, Colors.Yellow);

        // Food
        DrawString(font, new Vector2(340, 22), $"Food: {p.Food}", HorizontalAlignment.Left, -1, fs, Colors.LimeGreen);

        // Squads / Buildings count
        DrawString(font, new Vector2(480, 22),
            $"Squads: {_world.Squads.Count}  Buildings: {_world.Buildings.Count}",
            HorizontalAlignment.Left, -1, fs, Colors.LightGray);

        // Position
        DrawString(font, new Vector2(size.X - 180, 22),
            $"Pos: ({p.TileX}, {p.TileY})",
            HorizontalAlignment.Left, -1, fs, Colors.LightBlue);
    }
}
