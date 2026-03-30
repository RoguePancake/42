using System;
using System.Collections.Generic;
using Warship.Core;
using Warship.Data;
using Warship.Events;

namespace Warship.World;

/// <summary>
/// A* pathfinding engine that works across chunk boundaries.
/// Pure C# — no Godot dependency.
///
/// Features:
///   - Respects terrain movement costs
///   - Prefers roads (reduced cost)
///   - Avoids walls (increased cost or impassable)
///   - Works across chunk boundaries (auto-loads adjacent chunks)
///   - Scales to large maps via distance-bounded search
///
/// Subscribes to PathRequestEvent, publishes PathComputedEvent.
/// </summary>
public class PathfindingEngine
{
    private readonly ChunkManager _chunks;
    private const int MaxSearchNodes = 50000; // Safety cap for large maps
    private const float DiagonalCost = 1.414f;

    public PathfindingEngine(ChunkManager chunks)
    {
        _chunks = chunks;
        EventBus.Instance?.Subscribe<PathRequestEvent>(OnPathRequest);
    }

    private void OnPathRequest(PathRequestEvent ev)
    {
        var path = FindPath(ev.FromX, ev.FromY, ev.ToX, ev.ToY);
        if (path != null)
            EventBus.Instance?.Publish(new PathComputedEvent(ev.UnitId, path));
    }

    /// <summary>
    /// Find the optimal path between two world tile coordinates.
    /// Returns array of (x,y) waypoints, or null if no path exists.
    /// </summary>
    public (int x, int y)[]? FindPath(int fromX, int fromY, int toX, int toY)
    {
        if (!_chunks.InBounds(fromX, fromY) || !_chunks.InBounds(toX, toY))
            return null;

        // Quick reject: destination is impassable
        float destCost = _chunks.GetMovementCost(toX, toY);
        if (destCost >= 900f) return null;

        // Ensure both endpoints and path corridor have chunks loaded
        EnsureChunksAlongCorridor(fromX, fromY, toX, toY);

        var openSet = new PriorityQueue<PathNode, float>();
        var cameFrom = new Dictionary<long, long>();
        var gScore = new Dictionary<long, float>();
        var closedSet = new HashSet<long>();

        long startKey = TileKey(fromX, fromY);
        long goalKey = TileKey(toX, toY);

        gScore[startKey] = 0;
        openSet.Enqueue(new PathNode(fromX, fromY), Heuristic(fromX, fromY, toX, toY));

        int nodesExpanded = 0;

        while (openSet.Count > 0 && nodesExpanded < MaxSearchNodes)
        {
            var current = openSet.Dequeue();
            long currentKey = TileKey(current.X, current.Y);

            if (currentKey == goalKey)
                return ReconstructPath(cameFrom, current.X, current.Y, fromX, fromY);

            if (closedSet.Contains(currentKey))
                continue;
            closedSet.Add(currentKey);
            nodesExpanded++;

            float currentG = gScore.GetValueOrDefault(currentKey, float.MaxValue);

            // Expand all 8 neighbors
            for (int i = 0; i < 8; i++)
            {
                var (dx, dy) = DirectionHelper.Offsets[i];
                int nx = current.X + dx;
                int ny = current.Y + dy;

                if (!_chunks.InBounds(nx, ny)) continue;

                long neighborKey = TileKey(nx, ny);
                if (closedSet.Contains(neighborKey)) continue;

                // Movement cost
                float moveCost = _chunks.GetMovementCost(nx, ny);
                if (moveCost >= 900f) continue; // Impassable

                // Diagonal movement costs more
                bool diagonal = dx != 0 && dy != 0;
                float stepCost = diagonal ? moveCost * DiagonalCost : moveCost;

                // Wall penalty: crossing through a wall is very expensive
                var tile = _chunks.GetTile(current.X, current.Y);
                var dir = DirectionHelper.FromOffset(dx, dy);
                if ((tile.WallMask & (byte)dir) != 0)
                    stepCost += 50f; // Heavy penalty for walls

                float tentativeG = currentG + stepCost;

                if (tentativeG < gScore.GetValueOrDefault(neighborKey, float.MaxValue))
                {
                    gScore[neighborKey] = tentativeG;
                    cameFrom[neighborKey] = currentKey;
                    float fScore = tentativeG + Heuristic(nx, ny, toX, toY);
                    openSet.Enqueue(new PathNode(nx, ny), fScore);
                }
            }
        }

        return null; // No path found
    }

    /// <summary>
    /// Find path for a specific unit domain (land vs naval).
    /// </summary>
    public (int x, int y)[]? FindPath(int fromX, int fromY, int toX, int toY, UnitDomain domain)
    {
        if (!_chunks.InBounds(fromX, fromY) || !_chunks.InBounds(toX, toY))
            return null;

        EnsureChunksAlongCorridor(fromX, fromY, toX, toY);

        var openSet = new PriorityQueue<PathNode, float>();
        var cameFrom = new Dictionary<long, long>();
        var gScore = new Dictionary<long, float>();
        var closedSet = new HashSet<long>();

        long startKey = TileKey(fromX, fromY);
        long goalKey = TileKey(toX, toY);

        gScore[startKey] = 0;
        openSet.Enqueue(new PathNode(fromX, fromY), Heuristic(fromX, fromY, toX, toY));

        int nodesExpanded = 0;

        while (openSet.Count > 0 && nodesExpanded < MaxSearchNodes)
        {
            var current = openSet.Dequeue();
            long currentKey = TileKey(current.X, current.Y);

            if (currentKey == goalKey)
                return ReconstructPath(cameFrom, current.X, current.Y, fromX, fromY);

            if (closedSet.Contains(currentKey))
                continue;
            closedSet.Add(currentKey);
            nodesExpanded++;

            float currentG = gScore.GetValueOrDefault(currentKey, float.MaxValue);

            for (int i = 0; i < 8; i++)
            {
                var (dx, dy) = DirectionHelper.Offsets[i];
                int nx = current.X + dx;
                int ny = current.Y + dy;

                if (!_chunks.InBounds(nx, ny)) continue;

                long neighborKey = TileKey(nx, ny);
                if (closedSet.Contains(neighborKey)) continue;

                var neighborTile = _chunks.GetTile(nx, ny);

                // Domain-specific passability
                float moveCost;
                if (domain == UnitDomain.Naval)
                {
                    moveCost = TerrainRules.NavalMovementCost(neighborTile.TerrainType);
                }
                else
                {
                    moveCost = _chunks.GetMovementCost(nx, ny);
                }

                if (moveCost >= 900f) continue;

                bool diagonal = dx != 0 && dy != 0;
                float stepCost = diagonal ? moveCost * DiagonalCost : moveCost;

                // Wall penalty (only for land units)
                if (domain == UnitDomain.Land)
                {
                    var tile = _chunks.GetTile(current.X, current.Y);
                    var dir = DirectionHelper.FromOffset(dx, dy);
                    if ((tile.WallMask & (byte)dir) != 0)
                        stepCost += 50f;
                }

                float tentativeG = currentG + stepCost;

                if (tentativeG < gScore.GetValueOrDefault(neighborKey, float.MaxValue))
                {
                    gScore[neighborKey] = tentativeG;
                    cameFrom[neighborKey] = currentKey;
                    float fScore = tentativeG + Heuristic(nx, ny, toX, toY);
                    openSet.Enqueue(new PathNode(nx, ny), fScore);
                }
            }
        }

        return null;
    }

    // ═══════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════

    private static float Heuristic(int ax, int ay, int bx, int by)
    {
        // Octile distance (tighter than Manhattan, admissible for 8-directional)
        int dx = Math.Abs(ax - bx);
        int dy = Math.Abs(ay - by);
        return Math.Max(dx, dy) + (DiagonalCost - 1f) * Math.Min(dx, dy);
    }

    private static long TileKey(int x, int y) => (long)x << 32 | (uint)y;

    private static (int x, int y) FromTileKey(long key)
        => ((int)(key >> 32), (int)(key & 0xFFFFFFFF));

    private static (int x, int y)[] ReconstructPath(
        Dictionary<long, long> cameFrom, int endX, int endY, int startX, int startY)
    {
        var path = new List<(int x, int y)>();
        long current = TileKey(endX, endY);
        long startKey = TileKey(startX, startY);

        while (current != startKey)
        {
            var (x, y) = FromTileKey(current);
            path.Add((x, y));

            if (!cameFrom.TryGetValue(current, out var prev))
                break;
            current = prev;
        }

        path.Add((startX, startY));
        path.Reverse();
        return path.ToArray();
    }

    /// <summary>
    /// Ensure chunks along the corridor between two points are loaded
    /// so pathfinding can traverse them.
    /// </summary>
    private void EnsureChunksAlongCorridor(int fromX, int fromY, int toX, int toY)
    {
        int minCX = Math.Min(fromX, toX) / ChunkData.Size - 1;
        int maxCX = Math.Max(fromX, toX) / ChunkData.Size + 1;
        int minCY = Math.Min(fromY, toY) / ChunkData.Size - 1;
        int maxCY = Math.Max(fromY, toY) / ChunkData.Size + 1;

        minCX = Math.Max(0, minCX);
        maxCX = Math.Min(_chunks.ChunksX - 1, maxCX);
        minCY = Math.Max(0, minCY);
        maxCY = Math.Min(_chunks.ChunksY - 1, maxCY);

        for (int cx = minCX; cx <= maxCX; cx++)
            for (int cy = minCY; cy <= maxCY; cy++)
                _chunks.EnsureLoaded(new ChunkCoord(cx, cy));
    }

    private readonly record struct PathNode(int X, int Y);
}
