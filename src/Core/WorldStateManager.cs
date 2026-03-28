using System;
using System.Linq;
using Godot;
using Warship.Data;
using Warship.Events;
using Warship.World;

namespace Warship.Core;

/// <summary>
/// Holds the master WorldData (the entire game state).
/// Processes deltas and mutation requests coming from the EventBus.
/// </summary>
public partial class WorldStateManager : Node
{
    public static WorldStateManager? Instance { get; private set; }

    public WorldData Data { get; private set; } = new();

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        // World generation deferred until setup screen completes
        var setupPanel = new Warship.UI.Menus.CharacterSetupPanel();
        GetNode("/root/Main/UILayer")?.CallDeferred("add_child", setupPanel);

        // Subscribe to movement requests (legacy + army)
        EventBus.Instance!.Subscribe<UnitMoveRequested>(OnUnitMoveRequested);
    }

    // Stored config for custom nation (used after map click)
    private string? _pendingPlayerRole, _pendingPlayerName;
    private int _pendingFocusIndex;
    private string? _pendingCustomName;
    private NationArchetype _pendingCustomArchetype;

    public void InitializeWorld(string playerRole, string playerName, int focusIndex, int nationIndex = 6)
    {
        GD.Print($"[WorldStateManager] Generating world for {playerName} ({playerRole}), nation #{nationIndex}...");
        Data = WorldGenerator.CreateWorld(42, playerName, playerRole, focusIndex, nationIndex);

        EventBus.Instance!.Publish(new WorldReadyEvent(42, Data.PlayerNationId ?? ""));
    }

    /// <summary>
    /// Custom nation flow: generate world with 12 AI nations, show map, wait for capital click.
    /// </summary>
    public void InitializeCustomNation(string playerRole, string playerName, int focusIndex,
        string customName, NationArchetype archetype)
    {
        GD.Print($"[WorldStateManager] Custom nation mode: {customName} ({archetype}). Click map to place capital.");
        _pendingPlayerRole = playerRole;
        _pendingPlayerName = playerName;
        _pendingFocusIndex = focusIndex;
        _pendingCustomName = customName;
        _pendingCustomArchetype = archetype;

        // Generate world with 12 AI nations (no player nation yet — playerNationIndex=-1 uses first 12 templates)
        Data = WorldGenerator.CreateWorldWithoutPlayer(42);

        // Subscribe to capital placement click
        EventBus.Instance!.Subscribe<CustomNationCapitalPlacedEvent>(OnCustomCapitalPlaced);

        // Show map so player can click, and signal placement mode
        EventBus.Instance!.Publish(new WorldReadyEvent(42, ""));
        EventBus.Instance!.Publish(new CustomNationPlacementModeEvent(true));
    }

    private void OnCustomCapitalPlaced(CustomNationCapitalPlacedEvent ev)
    {
        EventBus.Instance!.Unsubscribe<CustomNationCapitalPlacedEvent>(OnCustomCapitalPlaced);
        EventBus.Instance!.Publish(new CustomNationPlacementModeEvent(false));

        GD.Print($"[WorldStateManager] Placing custom capital at ({ev.TileX}, {ev.TileY})...");

        // Add the custom nation to the existing world
        WorldGenerator.AddCustomNation(
            Data, ev.TileX, ev.TileY,
            _pendingCustomName ?? "New Republic",
            _pendingCustomArchetype,
            _pendingPlayerName ?? "J. Crawford",
            _pendingPlayerRole ?? "Defense Minister",
            _pendingFocusIndex);

        // Re-publish WorldReady with the new player nation
        EventBus.Instance!.Publish(new WorldReadyEvent(42, Data.PlayerNationId ?? ""));
    }

    /// <summary>
    /// Handle army movement requests. Validates terrain and updates army target.
    /// Legacy UnitMoveRequested still works — tries army matching first.
    /// </summary>
    private void OnUnitMoveRequested(UnitMoveRequested req)
    {
        if (req.TargetX < 0 || req.TargetX >= Data.MapWidth ||
            req.TargetY < 0 || req.TargetY >= Data.MapHeight) return;

        // Try to find an army with this ID
        var army = Data.Armies.FirstOrDefault(a => a.Id == req.UnitId);
        if (army != null && army.IsAlive)
        {
            int targetTerrain = Data.TerrainMap![req.TargetX, req.TargetY];

            // Validate based on army's primary domain
            if (army.PrimaryDomain == UnitDomain.Naval && TerrainRules.IsLand(targetTerrain)) return;
            if (army.PrimaryDomain == UnitDomain.Land && !TerrainRules.IsPassable(targetTerrain)) return;

            int oldX = army.TileX, oldY = army.TileY;
            army.TileX = req.TargetX;
            army.TileY = req.TargetY;
            army.TargetTileX = req.TargetX;
            army.TargetTileY = req.TargetY;
            army.TargetPixelX = req.TargetX * MapManagerConstants.TileSize + MapManagerConstants.TileSize / 2f;
            army.TargetPixelY = req.TargetY * MapManagerConstants.TileSize + MapManagerConstants.TileSize / 2f;

            EventBus.Instance!.Publish(new UnitMovedEvent(army.Id, oldX, oldY, req.TargetX, req.TargetY));
            return;
        }

        // Legacy fallback: try individual units
#pragma warning disable CS0612
        var unit = Data.Units.FirstOrDefault(u => u.Id == req.UnitId);
        if (unit != null && unit.IsAlive)
        {
            int targetTerrain = Data.TerrainMap![req.TargetX, req.TargetY];
            int oldX = unit.TileX, oldY = unit.TileY;
            unit.TileX = req.TargetX;
            unit.TileY = req.TargetY;
            EventBus.Instance!.Publish(new UnitMovedEvent(unit.Id, oldX, oldY, req.TargetX, req.TargetY));
        }
#pragma warning restore CS0612
    }
}
