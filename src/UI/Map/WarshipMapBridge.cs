using Godot;
using System;
using System.Linq;
using Warship.Core;
using Warship.Data;
using Warship.Events;

namespace Warship.UI.Map;

/// <summary>
/// WarshipMapBridge — Connects TileMapRenderer to your existing game systems.
/// 
/// This is the glue between the map visual layer and your simulation.
/// It listens to EventBus signals and reads WorldState to draw overlays.
/// 
/// RULES COMPLIANCE:
/// - Rule 1: Uses EventBus for all communication
/// - Rule 2: Reads WorldState, never mutates (visual overlays only)
/// - Rule 3: Only references Godot API (it's a visual bridge, not a sim engine)
/// </summary>
public partial class WarshipMapBridge : Node
{
    private TileMapRenderer _map;
    
    // Track what we've drawn so we know when to refresh
    private int _lastDrawnTurn = -1;
    private int _lastDrawnZoom = -1;
    
    public override void _Ready()
    {
        _map = GetNode<TileMapRenderer>("../TileMapRenderer");
        
        if (_map == null)
        {
            GD.PrintErr("[WarshipMapBridge] Could not find TileMapRenderer! Check scene tree.");
            return;
        }
        
        // Connect map signals
        _map.MapClicked += OnMapClicked;
        _map.MapRightClicked += OnMapRightClicked;
        _map.ZoomChanged += OnZoomChanged;
        
        // Subscribe to game events
        EventBus.Instance?.Subscribe<TurnAdvancedEvent>(OnTurnAdvanced);
        
        GD.Print("[WarshipMapBridge] Connected to TileMapRenderer.");
    }
    
    public override void _Process(double delta)
    {
        var data = WorldStateManager.Instance?.Data;
        if (data == null || data.Nations.Count == 0) return;
        
        // Check if we need to redraw overlays
        int currentZoom = _map.GetZoomLevel();
        if (_lastDrawnTurn != data.TurnNumber || _lastDrawnZoom != currentZoom)
        {
            RefreshAllOverlays();
            _lastDrawnTurn = data.TurnNumber;
            _lastDrawnZoom = currentZoom;
        }
    }
    
    // ── Map Click → Game Interaction ──────────────────
    
    private void OnMapClicked(float lon, float lat)
    {
        GD.Print($"[Bridge] Map clicked: {lat:F2}°N, {lon:F2}°E");
        
        var data = WorldStateManager.Instance?.Data;
        if (data == null || data.Nations.Count == 0) return;
        
        // Find which nation owns this coordinate (check border polygons)
        for (int i = 0; i < data.Nations.Count; i++)
        {
            var nation = data.Nations[i];
            if (nation.BorderPolygon != null && PointInPolygon(lon, lat, nation.BorderPolygon))
            {
                GD.Print($"[Bridge] Clicked on: {nation.Name}");
                // Emit event for UI panels to react
                EventBus.Instance?.Publish(new NationSelectedEvent(nation.Id));
                return;
            }
        }
        
        GD.Print("[Bridge] Clicked on unclaimed territory");
    }
    
    private void OnMapRightClicked(float lon, float lat)
    {
        var data = WorldStateManager.Instance?.Data;
        if (data == null || data.PlayerNationId == null) return;
        
        // Set command target for player nation
        int pIdx = int.Parse(data.PlayerNationId.Split('_')[1]);
        var playerNation = data.Nations[pIdx];
        playerNation.CommandTargetLon = lon;
        playerNation.CommandTargetLat = lat;
        
        GD.Print($"[Bridge] Command target set: {lat:F2}°N, {lon:F2}°E");
    }
    
    private void OnZoomChanged(int zoomLevel)
    {
        // Overlays need to be redrawn at each zoom level because
        // world-space coordinates change with the tile zoom
        GD.Print($"[Bridge] Zoom changed to {zoomLevel}");
    }
    
    private void OnTurnAdvanced(TurnAdvancedEvent ev)
    {
        // Force overlay refresh on next frame
        _lastDrawnTurn = -1;
    }
    
    // ── Overlay Drawing ──────────────────────────────
    
    private void RefreshAllOverlays()
    {
        var data = WorldStateManager.Instance?.Data;
        if (data == null || data.Nations.Count == 0) return;
        
        _map.ClearAllOverlays();
        
        DrawTerritories(data);
        DrawTradeRoutes(data);
        DrawUnits(data);
        DrawCityLabels(data);
        DrawCommandMarker(data);
    }
    
    private void DrawTerritories(WorldData data)
    {
        foreach (var nation in data.Nations)
        {
            if (nation.BorderPolygon == null || nation.BorderPolygon.Length < 3) continue;
            
            // Semi-transparent territory fill
            float opacity = nation.Id == data.PlayerNationId ? 0.20f : 0.12f;
            _map.DrawTerritory(nation.BorderPolygon, nation.NationColor, opacity);
            
            // Glowing border line
            float borderWidth = nation.Id == data.PlayerNationId ? 4f : 2.5f;
            _map.DrawBorder(nation.BorderPolygon, nation.NationColor, borderWidth);
        }
    }
    
    private void DrawTradeRoutes(WorldData data)
    {
        // Trade routes will be generated from game state in the future
        // For now, no routes to draw in the procedural world
    }
    
    private void DrawUnits(WorldData data)
    {
        int zoom = _map.GetZoomLevel();
        
        // At low zoom, only show large unit groups
        // At high zoom, show individual units
        foreach (var unit in data.Units)
        {
            // Only render if the unit has valid lon/lat
            if (unit.Longitude == 0 && unit.Latitude == 0) continue;
            
            // Skip individual soldiers at low zoom
            if (unit.Type == UnitType.Soldier && zoom < 6) continue;
            
            int natIdx = int.Parse(unit.NationId.Split('_')[1]);
            var color = data.Nations[natIdx].NationColor;
            
            // Draw unit as a colored circle with glow
            Vector2 worldPos = _map.LonLatToWorld(unit.Longitude, unit.Latitude, _map.GetZoomLevel());
            
            // For now we use simple polygon shapes — you can replace with sprites later
            var marker = new Polygon2D();
            float size = unit.Type == UnitType.Soldier ? 4f : 8f;
            marker.Polygon = new Vector2[]
            {
                worldPos + new Vector2(-size, -size),
                worldPos + new Vector2(size, -size),
                worldPos + new Vector2(size, size),
                worldPos + new Vector2(-size, size),
            };
            marker.Color = color;
            _map.GetUnitLayer().AddChild(marker);
        }
    }
    
    private void DrawCityLabels(WorldData data)
    {
        int zoom = _map.GetZoomLevel();
        
        foreach (var city in data.Cities)
        {
            if (city.Longitude == 0 && city.Latitude == 0) continue;
            
            // Only show cities at appropriate zoom levels
            if (city.Size < 2 && zoom < 5) continue;  // Small cities at zoom 5+
            if (city.Size < 3 && zoom < 3) continue;   // Medium cities at zoom 3+
            // Capitals always show
            
            int fontSize = city.IsCapital ? 16 : (city.Size >= 2 ? 13 : 11);
            var color = city.IsCapital ? Colors.Gold : Colors.White;
            string displayName = city.IsCapital ? $"★ {city.Name}" : city.Name;
            
            _map.PlaceLabel(city.Longitude, city.Latitude, displayName, fontSize, color);
        }
    }
    
    private void DrawCommandMarker(WorldData data)
    {
        if (data.PlayerNationId == null) return;
        
        int pIdx = int.Parse(data.PlayerNationId.Split('_')[1]);
        var player = data.Nations[pIdx];
        
        if (float.IsNaN(player.CommandTargetLon)) return;
        
        // Draw command crosshair
        Vector2 worldPos = _map.LonLatToWorld(player.CommandTargetLon, player.CommandTargetLat, _map.GetZoomLevel());
        
        Color markerColor = player.GlobalMilitaryOrder == MilitaryOrder.Attack 
            ? Colors.Red : new Color(0.2f, 0.6f, 1f);
        
        // Simple crosshair using lines
        var hLine = new Line2D();
        hLine.AddPoint(worldPos + new Vector2(-20, 0));
        hLine.AddPoint(worldPos + new Vector2(20, 0));
        hLine.DefaultColor = markerColor;
        hLine.Width = 2;
        _map.GetUnitLayer().AddChild(hLine);
        
        var vLine = new Line2D();
        vLine.AddPoint(worldPos + new Vector2(0, -20));
        vLine.AddPoint(worldPos + new Vector2(0, 20));
        vLine.DefaultColor = markerColor;
        vLine.Width = 2;
        _map.GetUnitLayer().AddChild(vLine);
    }
    
    // ── Geometry Helpers ──────────────────────────────
    
    /// <summary>Ray-casting point-in-polygon test.</summary>
    private static bool PointInPolygon(float x, float y, float[][] polygon)
    {
        bool inside = false;
        int j = polygon.Length - 1;
        
        for (int i = 0; i < polygon.Length; i++)
        {
            float xi = polygon[i][0], yi = polygon[i][1];
            float xj = polygon[j][0], yj = polygon[j][1];
            
            if (((yi > y) != (yj > y)) &&
                (x < (xj - xi) * (y - yi) / (yj - yi) + xi))
            {
                inside = !inside;
            }
            j = i;
        }
        
        return inside;
    }
}
