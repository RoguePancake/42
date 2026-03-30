using Godot;
using System.Linq;
using Warship.Data;
using Warship.Core;
using Warship.Events;
using Warship.World;
using Warship.UI.HUD;

namespace Warship.UI.Map;

/// <summary>
/// Map orchestrator — owns the 3-layer rendering stack and handles map input.
///
/// Layers (bottom to top):
///   1. TerrainChunkRenderer  — baked terrain textures, streamed by camera
///   2. TerritoryBorderRenderer — nation tints, borders, city icons
///   3. ArmySwarmRenderer — animated army dot swarms with LOD
/// </summary>
public partial class MapManager : Node2D
{
    public const int TileSize = MapManagerConstants.TileSize;
    public int MapWidth => _world?.MapWidth ?? TerrainGenerator.DefaultWidth;
    public int MapHeight => _world?.MapHeight ?? TerrainGenerator.DefaultHeight;

    private WorldData? _world;
    private bool _ready;

    private TerrainChunkRenderer? _terrain;
    private TerritoryBorderRenderer? _territory;
    private ArmySwarmRenderer? _armies;
    private DossierPanel? _dossier;

    public override void _Ready()
    {
        GD.Print("[MapManager] Initializing...");

        _terrain = new TerrainChunkRenderer { Name = "TerrainLayer" };
        _territory = new TerritoryBorderRenderer { Name = "TerritoryLayer" };
        _armies = new ArmySwarmRenderer { Name = "ArmyLayer" };

        AddChild(_terrain);
        AddChild(_territory);
        AddChild(_armies);

        EventBus.Instance?.Subscribe<WorldReadyEvent>(OnWorldReady);

        // Grab dossier panel if it exists
        _dossier = GetNodeOrNull<DossierPanel>("/root/Main/UILayer/DossierPanel");

        GD.Print("[MapManager] Layers created.");
    }

    private void OnWorldReady(WorldReadyEvent ev)
    {
        _world = WorldStateManager.Instance?.Data;
        if (_world?.TerrainMap == null)
        {
            GD.PrintErr("[MapManager] WorldReady but no terrain data!");
            return;
        }

        GD.Print($"[MapManager] World ready — {_world.MapWidth}x{_world.MapHeight} tiles");
        _terrain?.Initialize(_world);
        _territory?.Initialize(_world);
        _armies?.Initialize(_world);
        _ready = true;
        GD.Print("[MapManager] All layers initialized.");
    }

    public override void _Process(double delta)
    {
        // Fallback: if we missed the event, try to pick up world data
        if (!_ready)
        {
            var data = WorldStateManager.Instance?.Data;
            if (data?.TerrainMap != null)
            {
                _world = data;
                GD.Print("[MapManager] Late init — picking up world data from WSM.");
                _terrain?.Initialize(_world);
                _territory?.Initialize(_world);
                _armies?.Initialize(_world);
                _ready = true;
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_world == null || !_ready) return;

        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            var worldPos = GetGlobalMousePosition();
            var tile = PixelToTile(worldPos);

            if (mb.ButtonIndex == MouseButton.Left)
                HandleLeftClick(worldPos, tile);
            else if (mb.ButtonIndex == MouseButton.Right)
                HandleRightClick(tile);
        }
    }

    private void HandleLeftClick(Vector2 worldPos, Vector2I tile)
    {
        if (_world == null || _armies == null) return;

        // Check character click
        var character = _world.Characters.FirstOrDefault(c =>
            c.TileX == tile.X && c.TileY == tile.Y);
        if (character != null)
        {
            _dossier?.ShowCharacter(character);
            _armies.SelectedArmyId = null;
            GetViewport().SetInputAsHandled();
            return;
        }

        // Check army click
        var army = _armies.GetArmyAtPosition(worldPos);
        if (army != null)
        {
            _armies.SelectedArmyId = army.Id;
            GD.Print($"[Map] Selected: {army.Name} ({army.TotalStrength} troops)");
        }
        else
        {
            _armies.SelectedArmyId = null;
        }
        GetViewport().SetInputAsHandled();
    }

    private void HandleRightClick(Vector2I tile)
    {
        if (_world == null || _armies == null) return;

        if (_armies.SelectedArmyId != null)
        {
            // Move selected army
            var army = _world.Armies.FirstOrDefault(a => a.Id == _armies.SelectedArmyId);
            if (army != null)
            {
                army.TargetTileX = tile.X;
                army.TargetTileY = tile.Y;
                army.TargetPixelX = tile.X * TileSize + TileSize / 2f;
                army.TargetPixelY = tile.Y * TileSize + TileSize / 2f;
                army.CurrentOrder = MilitaryOrder.Attack;
                GD.Print($"[Map] {army.Name} → ({tile.X}, {tile.Y})");
            }
        }
        else if (_world.PlayerNationId != null)
        {
            // Set global command target
            var player = GetNationById(_world, _world.PlayerNationId);
            if (player != null)
            {
                player.CommandTargetX = tile.X;
                player.CommandTargetY = tile.Y;
                _territory?.ForceRedraw();
            }
        }
        GetViewport().SetInputAsHandled();
    }

    // ── Helpers ──

    public static Vector2I PixelToTile(Vector2 pixel)
    {
        return new Vector2I(
            (int)(pixel.X / TileSize),
            (int)(pixel.Y / TileSize));
    }

    public static Vector2 TileToPixel(int tx, int ty)
    {
        return new Vector2(
            tx * TileSize + TileSize / 2f,
            ty * TileSize + TileSize / 2f);
    }

    /// <summary>Find a nation by ID safely (no string split hacks).</summary>
    public static NationData? GetNationById(WorldData world, string nationId)
    {
        return world.Nations.FirstOrDefault(n => n.Id == nationId);
    }

    /// <summary>Get nation index safely.</summary>
    public static int GetNationIndex(WorldData world, string nationId)
    {
        for (int i = 0; i < world.Nations.Count; i++)
            if (world.Nations[i].Id == nationId) return i;
        return -1;
    }
}
