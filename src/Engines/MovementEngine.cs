using System;
using System.Collections.Generic;
using Warship.Core;
using Warship.Data;
using Warship.Events;
using Warship.World;

namespace Warship.Engines;

/// <summary>
/// Pure C# engine for army movement along computed paths.
/// Replaces direct tile-teleport with smooth step-by-step path following.
///
/// Each army has a CurrentPath and PathIndex. Each tick, armies advance
/// along their paths based on movement speed, terrain cost, and road bonuses.
///
/// Subscribes to ArmyMoveRequested to compute paths via PathfindingEngine.
/// Publishes ArmyMovedEvent as armies reach each waypoint.
/// </summary>
public class MovementEngine
{
    private readonly ChunkManager _chunks;
    private readonly PathfindingEngine _pathfinding;
    private readonly WorldData _world;

    // Movement points remaining this tick per army
    private readonly Dictionary<string, float> _movePointsRemaining = new();

    public MovementEngine(ChunkManager chunks, PathfindingEngine pathfinding, WorldData world)
    {
        _chunks = chunks;
        _pathfinding = pathfinding;
        _world = world;

        EventBus.Instance?.Subscribe<ArmyMoveRequested>(OnArmyMoveRequested);
    }

    /// <summary>
    /// When an army move is requested, compute a path and assign it.
    /// </summary>
    private void OnArmyMoveRequested(ArmyMoveRequested ev)
    {
        var army = FindArmy(ev.ArmyId);
        if (army == null || !army.IsAlive) return;

        // Compute path using A* with domain awareness
        var path = _pathfinding.FindPath(
            army.TileX, army.TileY,
            ev.TargetX, ev.TargetY,
            army.PrimaryDomain);

        if (path == null || path.Length < 2)
        {
            // No valid path — notify
            EventBus.Instance?.Publish(new NotificationEvent(
                $"{army.Name}: No route to target", "warning"));
            return;
        }

        // Assign path to army
        army.CurrentPath = new List<(int x, int y)>(path);
        army.PathIndex = 0;
        army.TargetTileX = ev.TargetX;
        army.TargetTileY = ev.TargetY;
        army.CurrentOrder = MilitaryOrder.Attack; // Moving toward objective
    }

    /// <summary>
    /// Process one tick of movement for all armies.
    /// Call this from the simulation clock or turn pipeline.
    /// Armies advance along their paths proportional to move speed.
    /// </summary>
    public void ProcessTick(float deltaTime)
    {
        foreach (var army in _world.Armies)
        {
            if (!army.IsAlive) continue;
            if (army.CurrentPath == null || army.PathIndex >= army.CurrentPath.Count - 1)
                continue;

            // Movement points = speed * delta * supply factor
            float supplyFactor = Math.Clamp(army.Supply / 100f, 0.3f, 1.0f);
            float movePoints = army.MoveSpeed * deltaTime * supplyFactor;

            while (movePoints > 0 && army.PathIndex < army.CurrentPath.Count - 1)
            {
                var next = army.CurrentPath[army.PathIndex + 1];
                float stepCost = _chunks.GetMovementCost(next.x, next.y);

                if (stepCost >= 900f)
                {
                    // Path blocked — clear path
                    army.CurrentPath = null;
                    break;
                }

                if (movePoints >= stepCost)
                {
                    // Move to next waypoint
                    int oldX = army.TileX, oldY = army.TileY;
                    army.TileX = next.x;
                    army.TileY = next.y;
                    army.PathIndex++;
                    movePoints -= stepCost;

                    // Update pixel position target for smooth rendering
                    army.TargetPixelX = next.x * MapManagerConstants.TileSize + MapManagerConstants.TileSize / 2f;
                    army.TargetPixelY = next.y * MapManagerConstants.TileSize + MapManagerConstants.TileSize / 2f;

                    EventBus.Instance?.Publish(new ArmyMovedEvent(
                        army.Id, oldX, oldY, next.x, next.y));

                    // Supply drain from movement
                    army.Supply = Math.Max(0, army.Supply - 0.5f);
                }
                else
                {
                    break; // Not enough move points for next step
                }
            }

            // Path complete?
            if (army.CurrentPath != null && army.PathIndex >= army.CurrentPath.Count - 1)
            {
                army.CurrentPath = null;
                if (army.CurrentOrder == MilitaryOrder.Attack &&
                    army.TileX == army.TargetTileX && army.TileY == army.TargetTileY)
                {
                    army.CurrentOrder = MilitaryOrder.Standby;
                }
            }
        }
    }

    /// <summary>
    /// Update army chunk assignments. Call after movement processing.
    /// Armies register in the chunk they occupy for spatial queries.
    /// </summary>
    public void UpdateArmyChunkAssignments()
    {
        // Clear all army lists in loaded chunks
        foreach (var (_, chunk) in _chunks.GetLoadedChunks())
            chunk.ArmyIds.Clear();

        // Reassign
        foreach (var army in _world.Armies)
        {
            if (!army.IsAlive) continue;
            var chunk = _chunks.GetChunkAt(army.TileX, army.TileY);
            chunk?.ArmyIds.Add(army.Id);
        }
    }

    private ArmyData? FindArmy(string armyId)
    {
        foreach (var army in _world.Armies)
            if (army.Id == armyId) return army;
        return null;
    }
}
