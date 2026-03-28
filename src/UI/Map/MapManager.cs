using Godot;
using System.Linq;
using Warship.World;
using Warship.Data;
using Warship.Core;
using Warship.Events;
using Warship.UI.HUD;

namespace Warship.UI.Map;

/// <summary>
/// Map orchestrator — manages layered renderers for the 6000x3600 tile world.
///
/// Layer stack (bottom to top):
///   1. TerrainChunkRenderer — baked terrain textures (static)
///   2. TerritoryBorderRenderer — territory tints + borders + cities (redraws on change)
///   3. ArmySwarmRenderer — pixel-dot army swarms (redraws every frame)
///
/// Handles input for army selection and movement commands.
/// </summary>
public partial class MapManager : Node2D
{
    public const int TileSize = MapManagerConstants.TileSize;
    public int MapWidth => _world?.MapWidth ?? TerrainGenerator.DefaultWidth;
    public int MapHeight => _world?.MapHeight ?? TerrainGenerator.DefaultHeight;

    private WorldData? _world;

    // Layer nodes
    private TerrainChunkRenderer? _terrainLayer;
    private TerritoryBorderRenderer? _territoryLayer;
    private ArmySwarmRenderer? _armyLayer;

    // UI references
    private DossierPanel? _dossierPanel;

    private bool _worldInitialized = false;

    public override void _Ready()
    {
        GD.Print("[MapManager] Initializing layered renderer...");

        // Create layer nodes in draw order
        _terrainLayer = new TerrainChunkRenderer { Name = "TerrainLayer" };
        _territoryLayer = new TerritoryBorderRenderer { Name = "TerritoryLayer" };
        _armyLayer = new ArmySwarmRenderer { Name = "ArmyLayer" };

        AddChild(_terrainLayer);
        AddChild(_territoryLayer);
        AddChild(_armyLayer);

        // Listen for world ready
        EventBus.Instance?.Subscribe<WorldReadyEvent>(OnWorldReady);
        EventBus.Instance?.Subscribe<UnitMovedEvent>(OnUnitMoved);

        // Try to grab dossier panel
        _dossierPanel = GetNode<DossierPanel>("/root/Main/UILayer/DossierPanel");
    }

    private void OnWorldReady(WorldReadyEvent ev)
    {
        _world = WorldStateManager.Instance?.Data;
        if (_world == null) return;

        GD.Print("[MapManager] World ready — baking terrain chunks...");
        _terrainLayer?.BakeChunks(_world);
        _terrainLayer?.DrawRivers(_world);
        _territoryLayer?.ForceRedraw();
        _worldInitialized = true;

        GD.Print("[MapManager] All layers initialized!");
    }

    private void OnUnitMoved(UnitMovedEvent ev)
    {
        // Legacy event compatibility — find army by matching pattern
        // In the new system, army movement is handled directly
    }

    public override void _Process(double delta)
    {
        if (!_worldInitialized && _world == null)
        {
            _world = WorldStateManager.Instance?.Data;
            if (_world != null && _world.TerrainMap != null)
            {
                OnWorldReady(new WorldReadyEvent(_world.Seed, _world.PlayerNationId ?? ""));
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_world == null) return;

        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            var worldPos = GetGlobalMousePosition();
            var tile = PixelToTile(worldPos);

            if (mb.ButtonIndex == MouseButton.Left)
            {
                // Check for character click first
                var clickedChar = _world.Characters.FirstOrDefault(c =>
                    c.TileX == tile.X && c.TileY == tile.Y);
                if (clickedChar != null)
                {
                    _dossierPanel?.ShowCharacter(clickedChar);
                    _armyLayer!.SelectedArmyId = null;
                    GetViewport().SetInputAsHandled();
                    return;
                }

                // Check for army click
                var clickedArmy = _armyLayer?.GetArmyAtPosition(worldPos);
                if (clickedArmy != null)
                {
                    _armyLayer!.SelectedArmyId = clickedArmy.Id;
                    GD.Print($"[Map] Selected army: {clickedArmy.Name} ({clickedArmy.TotalStrength} troops)");
                    GetViewport().SetInputAsHandled();
                }
                else
                {
                    _armyLayer!.SelectedArmyId = null;
                }
            }
            else if (mb.ButtonIndex == MouseButton.Right)
            {
                if (_armyLayer?.SelectedArmyId != null)
                {
                    // Move selected army to clicked tile
                    var army = _world.Armies.FirstOrDefault(a => a.Id == _armyLayer.SelectedArmyId);
                    if (army != null)
                    {
                        army.TargetTileX = tile.X;
                        army.TargetTileY = tile.Y;
                        army.TargetPixelX = tile.X * TileSize + TileSize / 2f;
                        army.TargetPixelY = tile.Y * TileSize + TileSize / 2f;
                        army.CurrentOrder = MilitaryOrder.Attack;
                        army.Formation = FormationType.Wedge;
                        GD.Print($"[Map] Army {army.Name} moving to ({tile.X}, {tile.Y})");
                        GetViewport().SetInputAsHandled();
                    }
                }
                else
                {
                    // Set global command target
                    if (_world.PlayerNationId != null)
                    {
                        int pIdx = int.Parse(_world.PlayerNationId.Split('_')[1]);
                        var playerNation = _world.Nations[pIdx];
                        playerNation.CommandTargetX = tile.X;
                        playerNation.CommandTargetY = tile.Y;
                        _territoryLayer?.ForceRedraw();
                        GetViewport().SetInputAsHandled();
                    }
                }
            }
        }
    }

    public int GetTerrain(int x, int y)
    {
        if (_world == null || _world.TerrainMap == null ||
            x < 0 || x >= _world.MapWidth || y < 0 || y >= _world.MapHeight)
            return 0;
        return _world.TerrainMap[x, y];
    }

    public static Vector2I PixelToTile(Vector2 pixel)
    {
        return new Vector2I((int)(pixel.X / TileSize), (int)(pixel.Y / TileSize));
    }

    public static Vector2 TileToPixel(int tx, int ty)
    {
        return new Vector2(tx * TileSize + TileSize / 2f, ty * TileSize + TileSize / 2f);
    }
}
