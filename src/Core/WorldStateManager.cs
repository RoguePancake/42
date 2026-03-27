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
        // World generation is now deferred until the player completes the setup screen
        // InitializeWorld() will be called from CharacterSetupPanel.
        var setupPanel = new Warship.UI.Menus.CharacterSetupPanel();
        GetNode("/root/Main/UILayer")?.CallDeferred("add_child", setupPanel);
        
        // Setup the "Simulation Engines" (hardcoded simple for now until Engines Phase)
        EventBus.Instance!.Subscribe<UnitMoveRequested>(OnUnitMoveRequested);
    }

    public void InitializeWorld(string playerRole, string playerName, int focusIndex)
    {
        GD.Print($"[WorldStateManager] Generating world for {playerName} ({playerRole})...");
        Data = WorldGenerator.CreateWorld(42, playerName, playerRole, focusIndex);
        
        // Notify the rest of the game that the world is ready
        EventBus.Instance!.Publish(new WorldReadyEvent(42, Data.PlayerNationId ?? ""));
    }


    /// <summary>
    /// Mini-engine for validating and executing unit movement requests.
    /// In the future this gets moved to MilitaryEngine / ActionEngine.
    /// </summary>
    private void OnUnitMoveRequested(UnitMoveRequested req)
    {
        var unit = Data.Units.FirstOrDefault(u => u.Id == req.UnitId);
        if (unit == null || !unit.IsAlive) return;

        // Path validation (Basic: simply check if target is valid terrain for this unit type)
        if (req.TargetX < 0 || req.TargetX >= Data.MapWidth || req.TargetY < 0 || req.TargetY >= Data.MapHeight) return;
        
        int targetTerrain = Data.TerrainMap![req.TargetX, req.TargetY];
        
        if (unit.Type == UnitType.Ship && TerrainRules.IsLand(targetTerrain)) return; // Ships need water
        if (unit.Type != UnitType.Ship && !TerrainRules.IsPassable(targetTerrain)) return; // Tanks avoid deep water/mountains

        // Actually perform the state mutation
        int oldX = unit.TileX;
        int oldY = unit.TileY;
        
        unit.TileX = req.TargetX;
        unit.TileY = req.TargetY;
        
        // Let the UI know the model just changed so it can animate
        EventBus.Instance!.Publish(new UnitMovedEvent(unit.Id, oldX, oldY, req.TargetX, req.TargetY));
    }
}
