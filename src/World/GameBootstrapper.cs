using Godot;
using Warship.Core;
using Warship.Data;
using Warship.Events;

namespace Warship.World;

/// <summary>
/// Runs once at startup. Generates the world, spawns the player, and fires WorldReadyEvent.
/// Attached as a Node in the scene tree — runs in _Ready after all autoloads are up.
/// </summary>
public partial class GameBootstrapper : Node
{
    public override void _Ready()
    {
        GD.Print("[Bootstrapper] Starting world generation...");

        int seed = 42;
        int width = TerrainGenerator.DefaultWidth;
        int height = TerrainGenerator.DefaultHeight;

        // Generate terrain
        var terrain = TerrainGenerator.Generate(width, height, seed);
        GD.Print($"[Bootstrapper] Terrain generated: {width}x{height} tiles.");

        // Find player start position
        var (startX, startY) = TerrainGenerator.FindStartPosition(terrain, width, height, seed);
        GD.Print($"[Bootstrapper] Player start: ({startX}, {startY}).");

        // Build world data
        var world = new WorldData
        {
            Seed = seed,
            MapWidth = width,
            MapHeight = height,
            TerrainMap = terrain,
            Player = new PlayerData
            {
                Name = "Commander",
                TileX = startX,
                TileY = startY,
                Gold = 1000,
                Food = 500,
            },
        };

        // Give the player a starting troop camp at their position
        var startCamp = new BuildingData
        {
            Id = 1,
            Type = BuildingType.TroopCamp,
            TileX = startX,
            TileY = startY,
            Health = 100,
            GarrisonCap = 200,
            GarrisonCount = 100,
        };
        world.Buildings.Add(startCamp);

        // Give the player a starting squad near the camp
        int ts = TerrainGenerator.TileSize;
        var startSquad = new TroopSquadData
        {
            Id = 1,
            Name = "1st Platoon",
            Count = 100,
            TileX = startX + 1,
            TileY = startY,
            PixelX = (startX + 1) * ts + ts / 2f,
            PixelY = startY * ts + ts / 2f,
            TargetPixelX = (startX + 1) * ts + ts / 2f,
            TargetPixelY = startY * ts + ts / 2f,
            Order = SquadOrder.Idle,
            Morale = 100f,
            MoveSpeed = 2f,
        };
        world.Squads.Add(startSquad);

        // Commit to WorldStateManager
        WorldStateManager.Instance!.World = world;

        // Fire world ready event — map and HUD will pick this up
        EventBus.Instance!.Publish(new WorldReadyEvent(seed));

        GD.Print("[Bootstrapper] World ready. Camp and squad spawned.");
    }
}
