using System;
using Warship.Core;
using Warship.Data;
using Warship.Events;
using Warship.World;

namespace Warship.Engines;

/// <summary>
/// Pure C# engine for the build system.
/// Validates placement requests, checks terrain/collision/ownership,
/// and commits builds to ChunkManager.
///
/// Subscribes to BuildRequestEvent, RoadBuildRequestEvent, WallBuildRequestEvent.
/// Publishes BuildCompletedEvent, BuildFailedEvent, RoadBuiltEvent, WallBuiltEvent.
///
/// No Godot dependency. Communicates only through EventBus.
/// </summary>
public class BuildEngine
{
    private readonly ChunkManager _chunks;
    private readonly WorldData _world;
    private int _nextStructureId = 1;

    public BuildEngine(ChunkManager chunks, WorldData world)
    {
        _chunks = chunks;
        _world = world;

        EventBus.Instance?.Subscribe<BuildRequestEvent>(OnBuildRequest);
        EventBus.Instance?.Subscribe<DemolishRequestEvent>(OnDemolishRequest);
        EventBus.Instance?.Subscribe<RoadBuildRequestEvent>(OnRoadBuildRequest);
        EventBus.Instance?.Subscribe<WallBuildRequestEvent>(OnWallBuildRequest);
    }

    // ═══════════════════════════════════════════════════════════
    //  STRUCTURE PLACEMENT
    // ═══════════════════════════════════════════════════════════

    private void OnBuildRequest(BuildRequestEvent ev)
    {
        var result = ValidatePlacement(ev.Type, ev.TileX, ev.TileY, ev.NationId);
        if (result != null)
        {
            EventBus.Instance?.Publish(new BuildFailedEvent(result, ev.TileX, ev.TileY));
            return;
        }

        var structure = new StructureData
        {
            Id = $"struct_{_nextStructureId++}",
            Type = ev.Type,
            TileX = ev.TileX,
            TileY = ev.TileY,
            OwnerNationId = ev.NationId,
            HP = StructureData.GetMaxHP(ev.Type),
            MaxHP = StructureData.GetMaxHP(ev.Type),
        };

        if (_chunks.PlaceStructure(structure))
        {
            EventBus.Instance?.Publish(new BuildCompletedEvent(
                structure.Id, structure.Type, ev.TileX, ev.TileY));
        }
        else
        {
            EventBus.Instance?.Publish(new BuildFailedEvent(
                "Chunk not loaded or tile occupied", ev.TileX, ev.TileY));
        }
    }

    /// <summary>
    /// Validate whether a structure can be placed at the given location.
    /// Returns null if valid, or an error message string.
    /// </summary>
    public string? ValidatePlacement(StructureType type, int tileX, int tileY, string nationId)
    {
        // Bounds check
        if (!_chunks.InBounds(tileX, tileY))
            return "Out of bounds";

        // Must be in a loaded chunk
        if (!_chunks.IsLoaded(tileX, tileY))
            return "Area not loaded";

        var tile = _chunks.GetTile(tileX, tileY);

        // Terrain validation
        if (StructureData.RequiresLand(type))
        {
            if (!TerrainRules.IsLand(tile.TerrainType))
                return "Requires land terrain";
            if (tile.TerrainType == (byte)TerrainType.Mountain)
                return "Cannot build on mountains";
        }

        if (StructureData.RequiresCoastal(type))
        {
            if (!IsCoastal(tileX, tileY))
                return "Requires coastal location";
        }

        // Collision: no structure already present
        if (tile.HasStructure)
            return "Tile already has a structure";

        // Ownership: must own the territory or be unclaimed
        if (tile.IsOwned)
        {
            int ownerIdx = tile.OwnerNationIdx;
            if (ownerIdx >= 0 && ownerIdx < _world.Nations.Count)
            {
                if (_world.Nations[ownerIdx].Id != nationId)
                    return "Territory belongs to another nation";
            }
        }

        return null; // Valid
    }

    private bool IsCoastal(int tileX, int tileY)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = tileX + dx, ny = tileY + dy;
                if (!_chunks.InBounds(nx, ny)) continue;
                var neighbor = _chunks.GetTile(nx, ny);
                if (!TerrainRules.IsLand(neighbor.TerrainType))
                    return true;
            }
        }
        return false;
    }

    // ═══════════════════════════════════════════════════════════
    //  DEMOLISH
    // ═══════════════════════════════════════════════════════════

    private void OnDemolishRequest(DemolishRequestEvent ev)
    {
        if (_chunks.RemoveStructure(ev.TileX, ev.TileY))
        {
            EventBus.Instance?.Publish(new NotificationEvent(
                $"Structure demolished at ({ev.TileX}, {ev.TileY})", "info"));
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  ROAD PLACEMENT
    // ═══════════════════════════════════════════════════════════

    private void OnRoadBuildRequest(RoadBuildRequestEvent ev)
    {
        // Validate: both tiles must be passable land
        if (!_chunks.InBounds(ev.FromX, ev.FromY) || !_chunks.InBounds(ev.ToX, ev.ToY))
        {
            EventBus.Instance?.Publish(new BuildFailedEvent("Out of bounds", ev.FromX, ev.FromY));
            return;
        }

        var fromTile = _chunks.GetTile(ev.FromX, ev.FromY);
        var toTile = _chunks.GetTile(ev.ToX, ev.ToY);

        if (!TerrainRules.IsLand(fromTile.TerrainType) || !TerrainRules.IsLand(toTile.TerrainType))
        {
            EventBus.Instance?.Publish(new BuildFailedEvent("Roads require land terrain", ev.FromX, ev.FromY));
            return;
        }

        if (_chunks.PlaceRoad(ev.FromX, ev.FromY, ev.ToX, ev.ToY, ev.Type))
        {
            EventBus.Instance?.Publish(new RoadBuiltEvent(ev.FromX, ev.FromY, ev.ToX, ev.ToY, ev.Type));
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  WALL PLACEMENT
    // ═══════════════════════════════════════════════════════════

    private void OnWallBuildRequest(WallBuildRequestEvent ev)
    {
        if (!_chunks.InBounds(ev.TileX, ev.TileY))
        {
            EventBus.Instance?.Publish(new BuildFailedEvent("Out of bounds", ev.TileX, ev.TileY));
            return;
        }

        var tile = _chunks.GetTile(ev.TileX, ev.TileY);
        if (!TerrainRules.IsLand(tile.TerrainType))
        {
            EventBus.Instance?.Publish(new BuildFailedEvent("Walls require land terrain", ev.TileX, ev.TileY));
            return;
        }

        if (_chunks.PlaceWall(ev.TileX, ev.TileY, ev.Facing, ev.Type))
        {
            EventBus.Instance?.Publish(new WallBuiltEvent(ev.TileX, ev.TileY, ev.Facing));
        }
    }
}
