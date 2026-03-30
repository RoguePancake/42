using System.Collections.Generic;
using Warship.Core;
using Warship.Data;
using Warship.Events;
using Warship.World;

namespace Warship.Engines;

/// <summary>
/// Pure C# engine for road auto-connectivity.
/// When a road is built, automatically updates neighbor connections
/// so roads visually link together without manual wiring.
///
/// Subscribes to RoadBuiltEvent to update adjacent tiles.
/// </summary>
public class RoadEngine
{
    private readonly ChunkManager _chunks;

    public RoadEngine(ChunkManager chunks)
    {
        _chunks = chunks;
        EventBus.Instance?.Subscribe<RoadBuiltEvent>(OnRoadBuilt);
    }

    private void OnRoadBuilt(RoadBuiltEvent ev)
    {
        // When a road is placed, check all 4 cardinal neighbors of both endpoints
        // and auto-connect any adjacent roads
        AutoConnectNeighbors(ev.FromX, ev.FromY);
        AutoConnectNeighbors(ev.ToX, ev.ToY);
    }

    /// <summary>
    /// For a tile with a road, check all 4 cardinal neighbors.
    /// If a neighbor also has a road, ensure both tiles have matching
    /// direction masks so they connect visually.
    /// </summary>
    private void AutoConnectNeighbors(int tileX, int tileY)
    {
        var tile = _chunks.GetTile(tileX, tileY);
        if (!tile.HasRoad) return;

        for (int i = 0; i < 4; i++) // Cardinals only (N, E, S, W)
        {
            var (dx, dy) = DirectionHelper.Offsets[i];
            int nx = tileX + dx, ny = tileY + dy;

            if (!_chunks.InBounds(nx, ny)) continue;

            var neighbor = _chunks.GetTile(nx, ny);
            if (!neighbor.HasRoad) continue;

            var dir = (DirectionMask)(1 << i);
            var opposite = DirectionHelper.Opposite(dir);

            // Ensure this tile points toward neighbor
            var chunk = _chunks.GetChunkAt(tileX, tileY);
            if (chunk != null)
            {
                var (lx, ly) = ChunkData.WorldToLocal(tileX, tileY);
                ref var t = ref chunk.GetTile(lx, ly);
                if ((t.RoadMask & (byte)dir) == 0)
                {
                    t.RoadMask |= (byte)dir;
                    chunk.IsDirty = true;
                }
            }

            // Ensure neighbor points back toward this tile
            var neighborChunk = _chunks.GetChunkAt(nx, ny);
            if (neighborChunk != null)
            {
                var (nlx, nly) = ChunkData.WorldToLocal(nx, ny);
                ref var nt = ref neighborChunk.GetTile(nlx, nly);
                if ((nt.RoadMask & (byte)opposite) == 0)
                {
                    nt.RoadMask |= (byte)opposite;
                    neighborChunk.IsDirty = true;
                }
            }
        }
    }

    /// <summary>
    /// Check if two tiles are connected by road (for pathfinding bonus).
    /// </summary>
    public bool AreRoadConnected(int fromX, int fromY, int toX, int toY)
    {
        int dx = toX - fromX;
        int dy = toY - fromY;
        var dir = DirectionHelper.FromOffset(dx, dy);
        if (dir == DirectionMask.None) return false;

        var fromTile = _chunks.GetTile(fromX, fromY);
        return (fromTile.RoadMask & (byte)dir) != 0;
    }
}
