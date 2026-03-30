using Godot;
using System;
using System.Collections.Generic;
using Warship.Core;
using Warship.Data;
using Warship.World;

namespace Warship.UI.Map;

/// <summary>
/// Draws all entities on the map: player character, buildings, troop squads.
/// Redraws every frame (entities move). Uses _Draw() for immediate-mode rendering.
///
/// DRAW ORDER (bottom to top):
///   1. Building footprints (roads, walls, camps)
///   2. Troop squads (dot clusters with formation shapes)
///   3. Player character (always on top, highlighted)
///   4. Selection indicators (yellow ring around selected entity)
///   5. Patrol paths (dashed lines for patrolling squads)
///
/// All positions are in world-pixel space (tile * TileSize).
/// Viewport culling: only draws entities visible on screen.
/// </summary>
public partial class EntityRenderer : Node2D
{
    private const int TS = TerrainGenerator.TileSize;

    private WorldData? _world;

    // Selection state
    private int _selectedSquadId = -1;
    private int _selectedBuildingId = -1;

    // Animation
    private int _frame;

    public int SelectedSquadId
    {
        get => _selectedSquadId;
        set { _selectedSquadId = value; _selectedBuildingId = -1; QueueRedraw(); }
    }

    public int SelectedBuildingId
    {
        get => _selectedBuildingId;
        set { _selectedBuildingId = value; _selectedSquadId = -1; QueueRedraw(); }
    }

    public void Initialize(WorldData world)
    {
        _world = world;
        GD.Print($"[EntityRenderer] Initialized: {world.Buildings.Count} buildings, {world.Squads.Count} squads.");
    }

    public override void _Process(double delta)
    {
        if (_world == null) return;

        _frame++;

        // Interpolate squad pixel positions toward targets
        float speed = 60f * (float)delta; // pixels per second
        foreach (var squad in _world.Squads)
        {
            if (!squad.IsAlive) continue;
            var cur = new Vector2(squad.PixelX, squad.PixelY);
            var tgt = new Vector2(squad.TargetPixelX, squad.TargetPixelY);
            float dist = cur.DistanceTo(tgt);

            if (dist > 0.5f)
            {
                float step = Math.Min(speed * squad.MoveSpeed, dist);
                var dir = (tgt - cur).Normalized();
                squad.PixelX += dir.X * step;
                squad.PixelY += dir.Y * step;
            }
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_world == null) return;

        var viewRect = GetViewRect();

        DrawBuildings(viewRect);
        DrawSquads(viewRect);
        DrawPlayer(viewRect);
        DrawPatrolPaths(viewRect);
    }

    // ════════════════════════════════════════════════════════════════
    //  BUILDINGS
    // ════════════════════════════════════════════════════════════════

    private void DrawBuildings(Rect2 viewRect)
    {
        foreach (var bld in _world!.Buildings)
        {
            var center = TileCenter(bld.TileX, bld.TileY);
            if (!viewRect.HasPoint(center)) continue;

            Color color = BuildingInfo.GetColor(bld.Type);
            var tilePos = new Vector2(bld.TileX * TS, bld.TileY * TS);

            switch (bld.Type)
            {
                case BuildingType.TroopCamp:
                    DrawTroopCamp(tilePos, center, color, bld);
                    break;
                case BuildingType.BorderWall:
                    DrawBorderWall(tilePos, color);
                    break;
                case BuildingType.Road:
                    DrawRoad(tilePos, center);
                    break;
                case BuildingType.Watchtower:
                    DrawWatchtower(center, color);
                    break;
                case BuildingType.Storehouse:
                    DrawStorehouse(tilePos, color);
                    break;
            }

            // Selection highlight
            if (bld.Id == _selectedBuildingId)
            {
                DrawArc(center, TS * 0.6f, 0, Mathf.Pi * 2, 24, Colors.Yellow, 1.5f);
            }

            // Building label (when zoomed in enough)
            var camera = GetViewport().GetCamera2D();
            float zoom = camera?.Zoom.X ?? 1f;
            if (zoom > 1.5f)
            {
                var font = ThemeDB.FallbackFont;
                DrawString(font, center + new Vector2(-20, -TS * 0.6f),
                    BuildingInfo.DisplayName(bld.Type),
                    HorizontalAlignment.Center, 60, 8, Colors.White);
            }
        }
    }

    private void DrawTroopCamp(Vector2 pos, Vector2 center, Color color, BuildingData bld)
    {
        // Tent shape: brown base rectangle + colored triangle roof
        float w = TS * 0.8f, h = TS * 0.5f;
        DrawRect(new Rect2(center.X - w / 2, center.Y - h / 4, w, h), new Color(0.45f, 0.30f, 0.15f));

        // Triangle roof
        var roof = new Vector2[]
        {
            center + new Vector2(-w / 2, -h / 4),
            center + new Vector2(w / 2, -h / 4),
            center + new Vector2(0, -h),
        };
        DrawPolygon(roof, new[] { color, color, color });

        // Flag on top
        DrawLine(center + new Vector2(0, -h), center + new Vector2(0, -h - 4), Colors.DarkGoldenrod, 1.5f);
        DrawRect(new Rect2(center.X, center.Y - h - 4, 4, 3), color);

        // Garrison count indicator (small bar)
        if (bld.GarrisonCap > 0)
        {
            float fillPct = (float)bld.GarrisonCount / bld.GarrisonCap;
            float barW = TS * 0.7f;
            var barPos = center + new Vector2(-barW / 2, h / 2 + 2);
            DrawRect(new Rect2(barPos, new Vector2(barW, 2)), new Color(0.2f, 0.2f, 0.2f));
            DrawRect(new Rect2(barPos, new Vector2(barW * fillPct, 2)), Colors.LimeGreen);
        }
    }

    private void DrawBorderWall(Vector2 pos, Color color)
    {
        // Thick horizontal line across the tile (like a wall segment)
        float margin = 1f;
        DrawRect(new Rect2(pos.X + margin, pos.Y + TS * 0.35f, TS - margin * 2, TS * 0.3f), color);

        // Battlements on top (small rectangles)
        for (int i = 0; i < 3; i++)
        {
            float bx = pos.X + margin + i * (TS / 3f);
            DrawRect(new Rect2(bx + 1, pos.Y + TS * 0.25f, TS / 3f - 2, TS * 0.12f), color.Lightened(0.2f));
        }
    }

    private void DrawRoad(Vector2 pos, Vector2 center)
    {
        // Dirt-colored path through center of tile
        Color roadColor = new Color(0.55f, 0.48f, 0.35f);
        float w = TS * 0.4f;
        DrawRect(new Rect2(center.X - w / 2, pos.Y, w, TS), roadColor);
        DrawRect(new Rect2(pos.X, center.Y - w / 2, TS, w), roadColor);
    }

    private void DrawWatchtower(Vector2 center, Color color)
    {
        // Tall narrow tower with observation platform
        float baseW = TS * 0.3f, baseH = TS * 0.6f;
        DrawRect(new Rect2(center.X - baseW / 2, center.Y - baseH / 2, baseW, baseH), color.Darkened(0.2f));

        // Platform on top (wider)
        float platW = TS * 0.5f;
        DrawRect(new Rect2(center.X - platW / 2, center.Y - baseH / 2 - 2, platW, 3), color);

        // Vision radius circle (faint)
        float visionRadius = 5f * TS; // 5 tile reveal radius
        DrawArc(center, visionRadius, 0, Mathf.Pi * 2, 32, new Color(color, 0.15f), 1f);
    }

    private void DrawStorehouse(Vector2 pos, Color color)
    {
        // Wide building
        float margin = 2f;
        DrawRect(new Rect2(pos.X + margin, pos.Y + margin, TS - margin * 2, TS - margin * 2), color);
        // Door
        DrawRect(new Rect2(pos.X + TS * 0.35f, pos.Y + TS * 0.55f, TS * 0.3f, TS * 0.4f), color.Darkened(0.3f));
    }

    // ════════════════════════════════════════════════════════════════
    //  TROOP SQUADS
    // ════════════════════════════════════════════════════════════════

    private void DrawSquads(Rect2 viewRect)
    {
        foreach (var squad in _world!.Squads)
        {
            if (!squad.IsAlive) continue;

            var center = new Vector2(squad.PixelX, squad.PixelY);
            if (!viewRect.HasPoint(center)) continue;

            // Determine dot count: 1 dot per 10 troops
            int dotCount = Math.Max(1, squad.Count / 10);
            if (dotCount > 50) dotCount = 50;

            // Squad color: green for player troops
            Color squadColor = new Color(0.2f, 0.7f, 0.2f);

            // Order indicator color tint
            if (squad.Order == SquadOrder.Patrol)
                squadColor = new Color(0.2f, 0.5f, 0.8f); // blue for patrol
            else if (squad.Order == SquadOrder.MoveTo)
                squadColor = new Color(0.8f, 0.7f, 0.2f); // yellow for moving

            float spread = Math.Min(TS * 0.8f, dotCount * 0.5f + 4f);
            int hash = squad.Id * 73856093;

            for (int i = 0; i < dotCount; i++)
            {
                var dotPos = GetDotPos(hash, i, spread, center);
                DrawRect(new Rect2(dotPos.X, dotPos.Y, 2f, 2f), squadColor);
            }

            // Selection highlight
            if (squad.Id == _selectedSquadId)
            {
                DrawArc(center, spread + 4, 0, Mathf.Pi * 2, 24, Colors.Yellow, 1.5f);
            }

            // Squad label
            var camera = GetViewport().GetCamera2D();
            float zoom = camera?.Zoom.X ?? 1f;
            if (zoom > 0.8f || squad.Id == _selectedSquadId)
            {
                var font = ThemeDB.FallbackFont;
                string label = $"{squad.Name} ({squad.Count})";
                DrawString(font, center + new Vector2(-30, -spread - 6),
                    label, HorizontalAlignment.Center, 80, 8,
                    squad.Id == _selectedSquadId ? Colors.Gold : Colors.White);
            }
        }
    }

    private Vector2 GetDotPos(int hash, int index, float spread, Vector2 center)
    {
        int h1 = SimRng.HashInt(hash, index * 2);
        int h2 = SimRng.HashInt(hash, index * 2 + 1);

        float angle = (h1 & 0xFFFF) / 65535f * Mathf.Pi * 2f;
        float radius = (h2 & 0xFFFF) / 65535f * spread;

        // Gentle jitter animation
        float jAngle = angle + _frame * 0.02f;
        float jRadius = radius + Mathf.Sin(_frame * 0.04f + index * 0.3f) * 1.5f;

        return center + new Vector2(Mathf.Cos(jAngle) * jRadius, Mathf.Sin(jAngle) * jRadius);
    }

    // ════════════════════════════════════════════════════════════════
    //  PLAYER CHARACTER
    // ════════════════════════════════════════════════════════════════

    private void DrawPlayer(Rect2 viewRect)
    {
        var p = _world!.Player;
        var center = TileCenter(p.TileX, p.TileY);
        if (!viewRect.HasPoint(center)) return;

        // Player marker: bright colored diamond
        float size = TS * 0.35f;
        var diamond = new Vector2[]
        {
            center + new Vector2(0, -size),
            center + new Vector2(size, 0),
            center + new Vector2(0, size),
            center + new Vector2(-size, 0),
        };
        Color playerColor = new Color(1f, 0.85f, 0.1f); // gold
        DrawPolygon(diamond, new[] { playerColor, playerColor, playerColor, playerColor });

        // Outline
        for (int i = 0; i < 4; i++)
            DrawLine(diamond[i], diamond[(i + 1) % 4], Colors.DarkGoldenrod, 1.5f);

        // Pulse effect (subtle size oscillation)
        float pulse = 1f + Mathf.Sin(_frame * 0.08f) * 0.1f;
        DrawArc(center, size * 1.3f * pulse, 0, Mathf.Pi * 2, 16, new Color(playerColor, 0.4f), 1f);

        // Name label
        var font = ThemeDB.FallbackFont;
        DrawString(font, center + new Vector2(-25, -size - 5),
            p.Name, HorizontalAlignment.Center, 60, 9, Colors.Gold);
    }

    // ════════════════════════════════════════════════════════════════
    //  PATROL PATHS
    // ════════════════════════════════════════════════════════════════

    private void DrawPatrolPaths(Rect2 viewRect)
    {
        foreach (var squad in _world!.Squads)
        {
            if (!squad.IsAlive || squad.Order != SquadOrder.Patrol) continue;

            var a = TileCenter(squad.PatrolAX, squad.PatrolAY);
            var b = TileCenter(squad.PatrolBX, squad.PatrolBY);

            // Only draw if at least one endpoint is visible
            if (!viewRect.HasPoint(a) && !viewRect.HasPoint(b)) continue;

            // Dashed line between patrol points
            Color pathColor = new Color(0.3f, 0.6f, 0.9f, 0.6f);
            DrawDashedLine(a, b, pathColor, 1.5f, 6f);

            // Waypoint markers (small circles)
            DrawArc(a, 3, 0, Mathf.Pi * 2, 12, pathColor, 1.5f);
            DrawArc(b, 3, 0, Mathf.Pi * 2, 12, pathColor, 1.5f);
        }
    }

    private void DrawDashedLine(Vector2 from, Vector2 to, Color color, float width, float dashLen)
    {
        float totalLen = from.DistanceTo(to);
        if (totalLen < 1f) return;
        var dir = (to - from).Normalized();
        float drawn = 0f;
        bool on = true;

        while (drawn < totalLen)
        {
            float segLen = Math.Min(dashLen, totalLen - drawn);
            if (on)
            {
                var segStart = from + dir * drawn;
                var segEnd = from + dir * (drawn + segLen);
                DrawLine(segStart, segEnd, color, width);
            }
            drawn += segLen;
            on = !on;
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  HIT TESTING — Find what the player clicked on
    // ════════════════════════════════════════════════════════════════

    /// <summary>Find a squad near a world position (for click selection).</summary>
    public TroopSquadData? GetSquadAt(Vector2 worldPos)
    {
        if (_world == null) return null;
        foreach (var squad in _world.Squads)
        {
            if (!squad.IsAlive) continue;
            var center = new Vector2(squad.PixelX, squad.PixelY);
            if (center.DistanceTo(worldPos) < TS * 1.5f)
                return squad;
        }
        return null;
    }

    /// <summary>Find a building at a tile position.</summary>
    public BuildingData? GetBuildingAt(int tileX, int tileY)
    {
        if (_world == null) return null;
        foreach (var bld in _world.Buildings)
            if (bld.TileX == tileX && bld.TileY == tileY)
                return bld;
        return null;
    }

    // ════════════════════════════════════════════════════════════════
    //  UTILITY
    // ════════════════════════════════════════════════════════════════

    private static Vector2 TileCenter(int tx, int ty)
    {
        return new Vector2(tx * TS + TS / 2f, ty * TS + TS / 2f);
    }

    private Rect2 GetViewRect()
    {
        var camera = GetViewport().GetCamera2D();
        if (camera != null)
        {
            var vpSize = GetViewportRect().Size;
            var camPos = camera.GlobalPosition;
            float halfW = vpSize.X / (2f * camera.Zoom.X);
            float halfH = vpSize.Y / (2f * camera.Zoom.Y);
            float margin = TS * 5;
            return new Rect2(
                camPos.X - halfW - margin, camPos.Y - halfH - margin,
                halfW * 2 + margin * 2, halfH * 2 + margin * 2);
        }
        return new Rect2(0, 0, _world!.MapWidth * TS, _world.MapHeight * TS);
    }
}
