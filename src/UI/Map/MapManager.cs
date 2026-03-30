using Godot;
using Warship.Core;
using Warship.Data;
using Warship.Events;
using Warship.World;

namespace Warship.UI.Map;

/// <summary>
/// Map orchestrator. Owns the two render layers and handles all map input.
///
/// Layer stack (bottom to top):
///   1. ChunkRenderer  — terrain textures (static, streamed)
///   2. EntityRenderer  — buildings, troops, player (redraws every frame)
///
/// Input handling:
///   Left-click  → select entity or tile
///   Right-click → issue command (move player, move squad, place building)
/// </summary>
public partial class MapManager : Node2D
{
    private const int TS = TerrainGenerator.TileSize;

    private ChunkRenderer? _chunks;
    private EntityRenderer? _entities;
    private MapCamera? _camera;
    private WorldData? _world;
    private bool _ready;

    // Current build mode (set by HUD build menu)
    private BuildingType? _buildMode;

    public void SetBuildMode(BuildingType? type) => _buildMode = type;
    public EntityRenderer? Entities => _entities;

    public override void _Ready()
    {
        GD.Print("[MapManager] Creating render layers...");

        _chunks = new ChunkRenderer { Name = "ChunkLayer" };
        _entities = new EntityRenderer { Name = "EntityLayer" };

        AddChild(_chunks);
        AddChild(_entities);

        // Listen for world ready
        EventBus.Instance?.Subscribe<WorldReadyEvent>(OnWorldReady);

        GD.Print("[MapManager] Layers created. Waiting for world data.");
    }

    private void OnWorldReady(WorldReadyEvent ev)
    {
        _world = WorldStateManager.Instance?.World;
        if (_world == null || _world.TerrainMap.Length == 0)
        {
            GD.PrintErr("[MapManager] WorldReady but no terrain data!");
            return;
        }

        // Find camera sibling
        _camera = GetParent()?.GetNodeOrNull<MapCamera>("MapCamera");

        // Initialize layers
        _chunks!.Initialize(_world.TerrainMap, _world.MapWidth, _world.MapHeight, _world.Seed);
        _entities!.Initialize(_world);

        // Center camera on player
        if (_camera != null)
            _camera.CenterOnTile(_world.Player.TileX, _world.Player.TileY);

        _ready = true;
        GD.Print($"[MapManager] World loaded: {_world.MapWidth}x{_world.MapHeight}, player at ({_world.Player.TileX}, {_world.Player.TileY}).");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_ready || _world == null || _camera == null) return;

        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            var tile = _camera.ScreenToTile(mb.Position);

            // Bounds check
            if (tile.X < 0 || tile.X >= _world.MapWidth || tile.Y < 0 || tile.Y >= _world.MapHeight)
                return;

            if (mb.ButtonIndex == MouseButton.Left)
                HandleLeftClick(mb.Position, tile);
            else if (mb.ButtonIndex == MouseButton.Right)
                HandleRightClick(tile);
        }
    }

    private void HandleLeftClick(Vector2 screenPos, Vector2I tile)
    {
        // If in build mode, place building
        if (_buildMode != null)
        {
            EventBus.Instance?.Publish(new PlaceBuildingEvent(_buildMode.Value, tile.X, tile.Y));
            return;
        }

        // Try to select a squad
        var worldPos = GetGlobalMousePosition();
        var squad = _entities!.GetSquadAt(worldPos);
        if (squad != null)
        {
            _entities.SelectedSquadId = squad.Id;
            EventBus.Instance?.Publish(new SelectSquadEvent(squad.Id));
            GetViewport().SetInputAsHandled();
            return;
        }

        // Try to select a building
        var building = _entities.GetBuildingAt(tile.X, tile.Y);
        if (building != null)
        {
            _entities.SelectedBuildingId = building.Id;
            EventBus.Instance?.Publish(new SelectBuildingEvent(building.Id));
            GetViewport().SetInputAsHandled();
            return;
        }

        // Click on empty tile — deselect and select tile
        _entities.SelectedSquadId = -1;
        _entities.SelectedBuildingId = -1;
        EventBus.Instance?.Publish(new SelectTileEvent(tile.X, tile.Y));
        GetViewport().SetInputAsHandled();
    }

    private void HandleRightClick(Vector2I tile)
    {
        // If a squad is selected, issue move/patrol command
        if (_entities!.SelectedSquadId >= 0)
        {
            EventBus.Instance?.Publish(new SquadOrderEvent(
                _entities.SelectedSquadId, SquadOrder.MoveTo, tile.X, tile.Y));
            GetViewport().SetInputAsHandled();
            return;
        }

        // Otherwise, move player character
        if (TerrainInfo.IsPassable(_world!.TerrainMap[tile.X + tile.Y * _world.MapWidth]))
        {
            EventBus.Instance?.Publish(new PlayerMoveEvent(tile.X, tile.Y));
            GetViewport().SetInputAsHandled();
        }
    }
}
