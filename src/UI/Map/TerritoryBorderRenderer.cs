using Godot;
using Warship.Data;
using Warship.Core;
using Warship.World;

namespace Warship.UI.Map;

/// <summary>
/// Draws territory overlays, city icons, nation borders, and command markers.
/// Only redraws when territory changes or turn advances.
///
/// Uses viewport culling — only draws what's visible on screen.
/// </summary>
public partial class TerritoryBorderRenderer : Node2D
{
    private const int TileSize = MapManagerConstants.TileSize;

    private WorldData? _world;
    private int _lastTurn = -1;
    private bool _needsRedraw = true;

    public void Initialize(WorldData world)
    {
        _world = world;
        _needsRedraw = true;
        QueueRedraw();
        GD.Print("[TerritoryBorders] Initialized.");
    }

    public void ForceRedraw()
    {
        _needsRedraw = true;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_world == null)
        {
            _world = WorldStateManager.Instance?.Data;
            if (_world != null) { _needsRedraw = true; QueueRedraw(); }
            return;
        }

        if (_world.TurnNumber != _lastTurn || _needsRedraw)
        {
            _lastTurn = _world.TurnNumber;
            _needsRedraw = false;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_world?.OwnershipMap == null) return;

        var viewRect = GetViewRect();
        int mapW = _world.MapWidth, mapH = _world.MapHeight;

        DrawTerritoryTints(viewRect, mapW, mapH);
        DrawCities(viewRect);
        DrawBorders(viewRect);
        DrawCommandMarker();
    }

    private void DrawTerritoryTints(Rect2 viewRect, int mapW, int mapH)
    {
        int minX = System.Math.Max(0, (int)(viewRect.Position.X / TileSize) - 1);
        int minY = System.Math.Max(0, (int)(viewRect.Position.Y / TileSize) - 1);
        int maxX = System.Math.Min(mapW - 1, (int)(viewRect.End.X / TileSize) + 1);
        int maxY = System.Math.Min(mapH - 1, (int)(viewRect.End.Y / TileSize) + 1);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                int owner = _world!.OwnershipMap![x, y];
                if (owner < 0 || owner >= _world.Nations.Count) continue;

                var nation = _world.Nations[owner];
                float alpha = nation.Id == _world.PlayerNationId ? 0.18f : 0.10f;
                var pos = new Vector2(x * TileSize, y * TileSize);
                DrawRect(new Rect2(pos, new Vector2(TileSize, TileSize)),
                    new Color(nation.NationColor, alpha));
            }
        }
    }

    private void DrawCities(Rect2 viewRect)
    {
        var camera = GetViewport().GetCamera2D();
        float zoom = camera?.Zoom.X ?? 1f;

        foreach (var city in _world!.Cities)
        {
            var center = new Vector2(
                city.TileX * TileSize + TileSize / 2f,
                city.TileY * TileSize + TileSize / 2f);

            if (!viewRect.HasPoint(center)) continue;

            // Get nation color safely
            int nIdx = MapManager.GetNationIndex(_world, city.NationId);
            Color natColor = nIdx >= 0 ? _world.Nations[nIdx].NationColor : Godot.Colors.Gray;

            if (city.IsCapital)
                DrawCapitalIcon(center, natColor);
            else if (city.Size >= 2)
                DrawCityIcon(center, natColor);
            else
                DrawTownIcon(center, natColor);

            // City name label (show capitals always, others when zoomed in)
            if (zoom > 0.3f || city.IsCapital)
            {
                var font = ThemeDB.FallbackFont;
                int fontSize = city.IsCapital ? 12 : 9;
                float yOff = city.IsCapital ? -32f : -16f;
                DrawString(font, center + new Vector2(-30, yOff),
                    city.Name, HorizontalAlignment.Center, 60, fontSize,
                    city.IsCapital ? Godot.Colors.Gold : Godot.Colors.White);
            }
        }
    }

    private void DrawCapitalIcon(Vector2 c, Color color)
    {
        // Shadow
        DrawCircle(c + new Vector2(0, 5), 12, new Color(0, 0, 0, 0.5f));
        // Castle base
        DrawRect(new Rect2(c.X - 8, c.Y - 8, 16, 16), Godot.Colors.DarkGray);
        // Roof
        DrawPolygon(
            new[] { c + new Vector2(-10, -8), c + new Vector2(10, -8), c + new Vector2(0, -18) },
            new[] { color, color, color });
        // Flagpole + flag
        DrawLine(c + new Vector2(0, -18), c + new Vector2(0, -28), Godot.Colors.DarkGoldenrod, 2);
        DrawRect(new Rect2(c.X, c.Y - 28, 7, 5), color);
        // Center dot
        DrawCircle(c, 2, Godot.Colors.LightYellow);
    }

    private void DrawCityIcon(Vector2 c, Color color)
    {
        DrawCircle(c + new Vector2(0, 3), 8, new Color(0, 0, 0, 0.4f));
        DrawRect(new Rect2(c.X - 6, c.Y - 6, 12, 12), Godot.Colors.SaddleBrown);
        DrawPolygon(
            new[] { c + new Vector2(-8, -6), c + new Vector2(8, -6), c + new Vector2(0, -13) },
            new[] { color, color, color });
    }

    private void DrawTownIcon(Vector2 c, Color color)
    {
        DrawCircle(c, 4, color);
        DrawCircle(c, 2, Godot.Colors.White);
    }

    private void DrawBorders(Rect2 viewRect)
    {
        foreach (var (nIdx, segments) in _world!.NationBorderLines)
        {
            if (nIdx < 0 || nIdx >= _world.Nations.Count) continue;

            var nation = _world.Nations[nIdx];
            Color borderColor = nation.NationColor;
            float width = nation.Id == _world.PlayerNationId ? 3f : 2f;

            foreach (var seg in segments)
            {
                if (seg.Length < 2) continue;
                // Cull segments outside viewport
                bool visible = false;
                for (int i = 0; i < seg.Length; i++)
                {
                    if (viewRect.HasPoint(seg[i])) { visible = true; break; }
                }
                if (!visible) continue;

                if (seg.Length == 2)
                    DrawLine(seg[0], seg[1], borderColor, width);
                else
                    DrawPolyline(seg, borderColor, width, true);
            }
        }
    }

    private void DrawCommandMarker()
    {
        if (_world!.PlayerNationId == null) return;

        var player = MapManager.GetNationById(_world, _world.PlayerNationId);
        if (player == null || player.CommandTargetX < 0) return;

        var pos = new Vector2(
            player.CommandTargetX * TileSize + TileSize / 2,
            player.CommandTargetY * TileSize + TileSize / 2);

        Color markerColor = player.GlobalMilitaryOrder == MilitaryOrder.Attack
            ? Godot.Colors.Red : new Color(0.2f, 0.6f, 1f);

        DrawArc(pos, 16, 0, Mathf.Pi * 2, 32, markerColor, 2);
        DrawLine(pos - new Vector2(20, 0), pos + new Vector2(20, 0), markerColor, 2);
        DrawLine(pos - new Vector2(0, 20), pos + new Vector2(0, 20), markerColor, 2);
    }

    // ── Helpers ──

    private Rect2 GetViewRect()
    {
        var camera = GetViewport().GetCamera2D();
        if (camera != null)
        {
            var vpSize = GetViewportRect().Size;
            var camPos = camera.GlobalPosition;
            float halfW = vpSize.X / (2f * camera.Zoom.X);
            float halfH = vpSize.Y / (2f * camera.Zoom.Y);
            return new Rect2(camPos.X - halfW, camPos.Y - halfH, halfW * 2, halfH * 2);
        }
        int mapW = _world?.MapWidth ?? 6000;
        int mapH = _world?.MapHeight ?? 3600;
        return new Rect2(0, 0, mapW * TileSize, mapH * TileSize);
    }
}
