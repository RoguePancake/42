using Godot;
using Warship.Data;
using Warship.Core;
using Warship.World;

namespace Warship.UI.Map;

/// <summary>
/// Draws territory overlays and pre-computed border polylines.
/// Territory = semi-transparent nation color tint per tile.
/// Borders = glowing colored lines at ownership boundaries.
///
/// Only redraws when territory changes (city captured, turn advance).
/// Uses pre-computed NationBorderLines from WorldGenerator.
/// </summary>
public partial class TerritoryBorderRenderer : Node2D
{
    private const int TileSize = MapManagerConstants.TileSize;

    private WorldData? _world;
    private int _lastDrawnTurn = -1;
    private bool _needsRedraw = true;

    public void ForceRedraw()
    {
        _needsRedraw = true;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _world = WorldStateManager.Instance?.Data;
        if (_world == null) return;

        if (_world.TurnNumber != _lastDrawnTurn || _needsRedraw)
        {
            _lastDrawnTurn = _world.TurnNumber;
            _needsRedraw = false;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_world == null || _world.OwnershipMap == null) return;

        int mapW = _world.MapWidth;
        int mapH = _world.MapHeight;

        // Get camera viewport for culling territory overlay tiles
        var camera = GetViewport().GetCamera2D();
        Rect2 viewRect;
        if (camera != null)
        {
            var vpSize = GetViewportRect().Size;
            var zoom = camera.Zoom;
            var camPos = camera.GlobalPosition;
            float halfW = vpSize.X / (2f * zoom.X);
            float halfH = vpSize.Y / (2f * zoom.Y);
            viewRect = new Rect2(camPos.X - halfW, camPos.Y - halfH, halfW * 2, halfH * 2);
        }
        else
        {
            viewRect = new Rect2(0, 0, mapW * TileSize, mapH * TileSize);
        }

        // Convert view rect to tile range
        int tileMinX = System.Math.Max(0, (int)(viewRect.Position.X / TileSize) - 1);
        int tileMinY = System.Math.Max(0, (int)(viewRect.Position.Y / TileSize) - 1);
        int tileMaxX = System.Math.Min(mapW - 1, (int)(viewRect.End.X / TileSize) + 1);
        int tileMaxY = System.Math.Min(mapH - 1, (int)(viewRect.End.Y / TileSize) + 1);

        // 1. Territory tint overlay (only visible tiles)
        for (int x = tileMinX; x <= tileMaxX; x++)
        {
            for (int y = tileMinY; y <= tileMaxY; y++)
            {
                int owner = _world.OwnershipMap[x, y];
                if (owner < 0 || owner >= _world.Nations.Count) continue;

                var nationColor = _world.Nations[owner].NationColor;
                float alpha = _world.Nations[owner].Id == _world.PlayerNationId ? 0.18f : 0.10f;
                var pos = new Vector2(x * TileSize, y * TileSize);
                DrawRect(new Rect2(pos, new Vector2(TileSize, TileSize)), new Color(nationColor, alpha));
            }
        }

        // 2. Draw city icons and control zone indicators
        foreach (var city in _world.Cities)
        {
            var cityPixel = new Vector2(city.TileX * TileSize + TileSize / 2f,
                                        city.TileY * TileSize + TileSize / 2f);

            // Skip if not in view
            if (!viewRect.HasPoint(cityPixel)) continue;

            int nIdx = int.Parse(city.NationId.Split('_')[1]);
            var natColor = _world.Nations[nIdx].NationColor;

            if (city.IsCapital)
            {
                // Capital: large castle icon
                DrawCircle(cityPixel + new Vector2(0, 5), 12, new Color(0, 0, 0, 0.5f));
                DrawRect(new Rect2(cityPixel.X - 8, cityPixel.Y - 8, 16, 16), Colors.DarkGray);
                Vector2[] roof = { cityPixel + new Vector2(-10, -8), cityPixel + new Vector2(10, -8), cityPixel + new Vector2(0, -18) };
                DrawPolygon(roof, new[] { natColor, natColor, natColor });
                DrawLine(cityPixel + new Vector2(0, -18), cityPixel + new Vector2(0, -28), Colors.DarkGoldenrod, 2);
                DrawRect(new Rect2(cityPixel.X, cityPixel.Y - 28, 7, 5), natColor);
                DrawCircle(cityPixel, 2, Colors.LightYellow);
            }
            else if (city.Size >= 2)
            {
                // City: medium building
                DrawCircle(cityPixel + new Vector2(0, 3), 8, new Color(0, 0, 0, 0.4f));
                DrawRect(new Rect2(cityPixel.X - 6, cityPixel.Y - 6, 12, 12), Colors.SaddleBrown);
                Vector2[] roof = { cityPixel + new Vector2(-8, -6), cityPixel + new Vector2(8, -6), cityPixel + new Vector2(0, -13) };
                DrawPolygon(roof, new[] { natColor, natColor, natColor });
            }
            else
            {
                // Town: small dot
                DrawCircle(cityPixel, 4, natColor);
                DrawCircle(cityPixel, 2, Colors.White);
            }

            // City name label
            float zoom = camera?.Zoom.X ?? 1f;
            if (zoom > 0.3f || city.IsCapital)
            {
                var font = ThemeDB.FallbackFont;
                int fontSize = city.IsCapital ? 12 : 9;
                DrawString(font, cityPixel + new Vector2(-30, city.IsCapital ? -32 : -16),
                    city.Name, HorizontalAlignment.Center, 60, fontSize,
                    city.IsCapital ? Colors.Gold : Colors.White);
            }
        }

        // 3. Nation borders from pre-computed segments (only visible ones)
        foreach (var (nIdx, segments) in _world.NationBorderLines)
        {
            if (nIdx >= _world.Nations.Count) continue;
            var borderColor = _world.Nations[nIdx].NationColor;
            float borderWidth = _world.Nations[nIdx].Id == _world.PlayerNationId ? 3f : 2f;

            foreach (var seg in segments)
            {
                if (seg.Length < 2) continue;
                // Cull segments outside view
                if (!viewRect.HasPoint(seg[0]) && !viewRect.HasPoint(seg[1])) continue;
                DrawLine(seg[0], seg[1], borderColor, borderWidth);
            }
        }

        // 4. Player command marker
        if (_world.PlayerNationId != null)
        {
            int pIdx = int.Parse(_world.PlayerNationId.Split('_')[1]);
            var playerNation = _world.Nations[pIdx];
            if (playerNation.CommandTargetX >= 0)
            {
                var markerPos = new Vector2(
                    playerNation.CommandTargetX * TileSize + TileSize / 2,
                    playerNation.CommandTargetY * TileSize + TileSize / 2);

                Color markerColor = playerNation.GlobalMilitaryOrder == MilitaryOrder.Attack
                    ? Colors.Red : new Color(0.2f, 0.6f, 1f);

                DrawArc(markerPos, 16, 0, Mathf.Pi * 2, 32, markerColor, 2);
                DrawLine(markerPos - new Vector2(20, 0), markerPos + new Vector2(20, 0), markerColor, 2);
                DrawLine(markerPos - new Vector2(0, 20), markerPos + new Vector2(0, 20), markerColor, 2);
            }
        }
    }
}
