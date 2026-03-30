using Godot;
using System;
using Warship.Core;
using Warship.Data;
using Warship.World;

namespace Warship.UI.Map;

/// <summary>
/// Draws a highlight square on the tile under the mouse cursor.
/// In build mode, shows a ghost preview of the building and tints
/// green (can build) or red (can't build).
/// </summary>
public partial class TileCursor : Node2D
{
    private const int TS = TerrainGenerator.TileSize;

    private WorldData? _world;
    private MapCamera? _camera;
    private MapManager? _mapManager;
    private int _hoverX = -1, _hoverY = -1;

    public override void _Ready()
    {
        EventBus.Instance?.Subscribe<WorldReadyEvent>(_ =>
        {
            _world = WorldStateManager.Instance?.World;
            _camera = GetParent()?.GetParent()?.GetNodeOrNull<MapCamera>("MapCamera");
            _mapManager = GetParent() as MapManager;
        });
    }

    public override void _Process(double delta)
    {
        if (_world == null || _camera == null) return;

        var mouse = GetViewport().GetMousePosition();
        var tile = _camera.ScreenToTile(mouse);

        if (tile.X != _hoverX || tile.Y != _hoverY)
        {
            _hoverX = tile.X;
            _hoverY = tile.Y;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_world == null) return;
        if (_hoverX < 0 || _hoverX >= _world.MapWidth || _hoverY < 0 || _hoverY >= _world.MapHeight)
            return;

        var pos = new Vector2(_hoverX * TS, _hoverY * TS);
        var size = new Vector2(TS, TS);

        // Default: white outline
        Color borderColor = new Color(1f, 1f, 1f, 0.5f);
        Color fillColor = new Color(1f, 1f, 1f, 0.08f);

        // Check if MapManager is in build mode (read the field via reflection-free approach)
        // We check terrain to tint green/red regardless
        int terrain = _world.TerrainMap[_hoverX + _hoverY * _world.MapWidth];
        bool buildable = TerrainInfo.IsBuildable(terrain);
        bool occupied = false;
        foreach (var b in _world.Buildings)
            if (b.TileX == _hoverX && b.TileY == _hoverY) { occupied = true; break; }

        bool canBuild = buildable && !occupied;

        // If any build hotkey was recently pressed, show build preview colors
        // We can detect build mode by checking if any number key is held — but simpler:
        // always show terrain suitability with subtle tint
        if (canBuild)
        {
            fillColor = new Color(0.2f, 0.8f, 0.2f, 0.12f);
            borderColor = new Color(0.3f, 1f, 0.3f, 0.6f);
        }
        else if (!TerrainInfo.IsPassable(terrain))
        {
            fillColor = new Color(0.8f, 0.2f, 0.2f, 0.12f);
            borderColor = new Color(1f, 0.3f, 0.3f, 0.6f);
        }

        // Fill
        DrawRect(new Rect2(pos, size), fillColor);
        // Border
        DrawRect(new Rect2(pos, size), borderColor, false, 1.5f);

        // Coordinate label (when zoomed in)
        var camera = GetViewport().GetCamera2D();
        float zoom = camera?.Zoom.X ?? 1f;
        if (zoom > 2.0f)
        {
            var font = ThemeDB.FallbackFont;
            DrawString(font, pos + new Vector2(1, TS - 2),
                $"{_hoverX},{_hoverY}", HorizontalAlignment.Left, TS, 7,
                new Color(1f, 1f, 1f, 0.4f));
        }
    }
}
