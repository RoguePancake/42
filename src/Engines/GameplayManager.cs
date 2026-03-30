using Godot;
using System;
using System.Linq;
using Warship.Core;
using Warship.Data;
using Warship.Events;
using Warship.World;

namespace Warship.Engines;

/// <summary>
/// Handles all gameplay actions: building placement, squad spawning, squad orders,
/// player movement, and patrol AI. Listens to events, mutates WorldData.
///
/// This is a Godot Node so it can run _Process for patrol/movement AI each frame.
/// All actual game logic lives here — UI only fires events, this resolves them.
/// </summary>
public partial class GameplayManager : Node
{
    private WorldData? _world;
    private int _nextBuildingId = 100;
    private int _nextSquadId = 100;

    public override void _Ready()
    {
        EventBus.Instance?.Subscribe<WorldReadyEvent>(OnWorldReady);
        EventBus.Instance?.Subscribe<TickEvent>(OnTick);
        EventBus.Instance?.Subscribe<PlaceBuildingEvent>(OnPlaceBuilding);
        EventBus.Instance?.Subscribe<SpawnSquadEvent>(OnSpawnSquad);
        EventBus.Instance?.Subscribe<SquadOrderEvent>(OnSquadOrder);
        EventBus.Instance?.Subscribe<SetPatrolEvent>(OnSetPatrol);
        EventBus.Instance?.Subscribe<PlayerMoveEvent>(OnPlayerMove);

        GD.Print("[GameplayManager] Ready, listening for events.");
    }

    private void OnWorldReady(WorldReadyEvent ev)
    {
        _world = WorldStateManager.Instance?.World;
    }

    // ════════════════════════════════════════════════════════════════
    //  TICK ECONOMY — runs every game tick
    // ════════════════════════════════════════════════════════════════

    private void OnTick(TickEvent ev)
    {
        if (_world == null) return;
        _world.TickNumber = ev.TickNumber;

        // Troop camps produce troops every 3 ticks
        if (ev.TickNumber % 3 == 0)
        {
            foreach (var bld in _world.Buildings)
            {
                if (bld.Type != BuildingType.TroopCamp) continue;
                if (bld.GarrisonCount >= bld.GarrisonCap) continue;

                // Produce 10 troops per camp per cycle (costs 5 food)
                if (_world.Player.Food >= 5)
                {
                    int produce = Math.Min(10, bld.GarrisonCap - bld.GarrisonCount);
                    bld.GarrisonCount += produce;
                    _world.Player.Food -= 5;
                }
            }
        }

        // Passive income: +5 gold per tick
        _world.Player.Gold += 5;

        // Passive food: +2 food per tick (farms/foraging)
        _world.Player.Food += 2;

        // Squad upkeep: -1 food per 100 troops per tick
        foreach (var squad in _world.Squads)
        {
            if (!squad.IsAlive) continue;
            int foodCost = Math.Max(1, squad.Count / 100);
            _world.Player.Food -= foodCost;
        }

        // Starvation: if food goes negative, squads lose morale
        if (_world.Player.Food < 0)
        {
            _world.Player.Food = 0;
            foreach (var squad in _world.Squads)
            {
                if (!squad.IsAlive) continue;
                squad.Morale = Math.Max(0, squad.Morale - 5f);
            }
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  BUILDING PLACEMENT
    // ════════════════════════════════════════════════════════════════

    private void OnPlaceBuilding(PlaceBuildingEvent ev)
    {
        if (_world == null) return;

        // Validate: in bounds
        if (ev.TileX < 0 || ev.TileX >= _world.MapWidth || ev.TileY < 0 || ev.TileY >= _world.MapHeight)
        {
            EventBus.Instance?.Publish(new BuildingFailedEvent("Out of bounds."));
            return;
        }

        // Validate: terrain is buildable
        int terrain = _world.TerrainMap[ev.TileX + ev.TileY * _world.MapWidth];
        if (!TerrainInfo.IsBuildable(terrain))
        {
            EventBus.Instance?.Publish(new BuildingFailedEvent("Can't build here — terrain not suitable."));
            return;
        }

        // Validate: no building already there
        if (_world.Buildings.Any(b => b.TileX == ev.TileX && b.TileY == ev.TileY))
        {
            EventBus.Instance?.Publish(new BuildingFailedEvent("Tile already occupied."));
            return;
        }

        // Validate: enough gold
        int cost = BuildingInfo.GoldCost(ev.Type);
        if (_world.Player.Gold < cost)
        {
            EventBus.Instance?.Publish(new BuildingFailedEvent($"Not enough gold. Need {cost}, have {_world.Player.Gold}."));
            return;
        }

        // Place it
        _world.Player.Gold -= cost;

        var building = new BuildingData
        {
            Id = _nextBuildingId++,
            Type = ev.Type,
            TileX = ev.TileX,
            TileY = ev.TileY,
            Health = 100,
            GarrisonCap = ev.Type == BuildingType.TroopCamp ? 200 : 0,
        };
        _world.Buildings.Add(building);

        EventBus.Instance?.Publish(new BuildingPlacedEvent(building.Id, ev.Type, ev.TileX, ev.TileY));
        GD.Print($"[Gameplay] Built {BuildingInfo.DisplayName(ev.Type)} at ({ev.TileX}, {ev.TileY}). Gold: {_world.Player.Gold}");
    }

    // ════════════════════════════════════════════════════════════════
    //  SQUAD SPAWNING
    // ════════════════════════════════════════════════════════════════

    private void OnSpawnSquad(SpawnSquadEvent ev)
    {
        if (_world == null) return;

        // Find the camp
        var camp = _world.Buildings.FirstOrDefault(b => b.Id == ev.CampId && b.Type == BuildingType.TroopCamp);
        if (camp == null) return;

        // Validate: camp has enough garrisoned troops
        int count = Math.Clamp(ev.Count, 10, camp.GarrisonCount);
        if (count <= 0) return;

        camp.GarrisonCount -= count;

        int ts = TerrainGenerator.TileSize;
        var squad = new TroopSquadData
        {
            Id = _nextSquadId++,
            Name = $"Squad {_nextSquadId - 100}",
            Count = count,
            TileX = camp.TileX + 1,
            TileY = camp.TileY,
            PixelX = (camp.TileX + 1) * ts + ts / 2f,
            PixelY = camp.TileY * ts + ts / 2f,
            TargetPixelX = (camp.TileX + 1) * ts + ts / 2f,
            TargetPixelY = camp.TileY * ts + ts / 2f,
            Order = SquadOrder.Idle,
            Morale = 100f,
            MoveSpeed = 2f,
        };
        _world.Squads.Add(squad);

        EventBus.Instance?.Publish(new SquadSpawnedEvent(squad.Id));
        GD.Print($"[Gameplay] Spawned {squad.Name} ({count} troops) from camp {ev.CampId}.");
    }

    // ════════════════════════════════════════════════════════════════
    //  SQUAD ORDERS
    // ════════════════════════════════════════════════════════════════

    private void OnSquadOrder(SquadOrderEvent ev)
    {
        if (_world == null) return;
        var squad = _world.Squads.FirstOrDefault(s => s.Id == ev.SquadId);
        if (squad == null || !squad.IsAlive) return;

        squad.Order = ev.Order;
        squad.TargetTileX = ev.TargetX;
        squad.TargetTileY = ev.TargetY;

        int ts = TerrainGenerator.TileSize;
        squad.TargetPixelX = ev.TargetX * ts + ts / 2f;
        squad.TargetPixelY = ev.TargetY * ts + ts / 2f;

        GD.Print($"[Gameplay] {squad.Name} → {ev.Order} at ({ev.TargetX}, {ev.TargetY}).");
    }

    private void OnSetPatrol(SetPatrolEvent ev)
    {
        if (_world == null) return;
        var squad = _world.Squads.FirstOrDefault(s => s.Id == ev.SquadId);
        if (squad == null || !squad.IsAlive) return;

        squad.Order = SquadOrder.Patrol;
        squad.PatrolAX = ev.AX;
        squad.PatrolAY = ev.AY;
        squad.PatrolBX = ev.BX;
        squad.PatrolBY = ev.BY;
        squad.PatrolGoingToB = true;

        int ts = TerrainGenerator.TileSize;
        squad.TargetPixelX = ev.BX * ts + ts / 2f;
        squad.TargetPixelY = ev.BY * ts + ts / 2f;

        GD.Print($"[Gameplay] {squad.Name} → Patrol between ({ev.AX},{ev.AY}) and ({ev.BX},{ev.BY}).");
    }

    // ════════════════════════════════════════════════════════════════
    //  PLAYER MOVEMENT
    // ════════════════════════════════════════════════════════════════

    private void OnPlayerMove(PlayerMoveEvent ev)
    {
        if (_world == null) return;
        _world.Player.TileX = ev.TargetTileX;
        _world.Player.TileY = ev.TargetTileY;
    }

    // ════════════════════════════════════════════════════════════════
    //  FRAME UPDATE — Patrol AI + squad arrival detection
    // ════════════════════════════════════════════════════════════════

    public override void _Process(double delta)
    {
        if (_world == null) return;

        int ts = TerrainGenerator.TileSize;

        foreach (var squad in _world.Squads)
        {
            if (!squad.IsAlive) continue;

            // Check if squad arrived at target
            float dist = new Godot.Vector2(squad.PixelX, squad.PixelY)
                .DistanceTo(new Godot.Vector2(squad.TargetPixelX, squad.TargetPixelY));

            if (dist < 2f)
            {
                // Update tile position to match pixel position
                squad.TileX = (int)(squad.PixelX / ts);
                squad.TileY = (int)(squad.PixelY / ts);

                if (squad.Order == SquadOrder.MoveTo)
                {
                    // Arrived at destination — go idle
                    squad.Order = SquadOrder.Idle;
                }
                else if (squad.Order == SquadOrder.Patrol)
                {
                    // Arrived at patrol waypoint — flip to other waypoint
                    squad.PatrolGoingToB = !squad.PatrolGoingToB;
                    int nextX = squad.PatrolGoingToB ? squad.PatrolBX : squad.PatrolAX;
                    int nextY = squad.PatrolGoingToB ? squad.PatrolBY : squad.PatrolAY;
                    squad.TargetPixelX = nextX * ts + ts / 2f;
                    squad.TargetPixelY = nextY * ts + ts / 2f;
                }
            }
        }
    }
}
