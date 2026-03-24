using Godot;
using System;
using System.Collections.Generic;
using System.IO;

namespace Warship.UI.Map;

/// <summary>
/// TileMapRenderer — Loads real-world OpenStreetMap tiles from disk and renders
/// them as a scrollable, zoomable world map in Godot 4.
/// 
/// Supports multiple tile styles (OSM vibrant, Topographic) switchable at runtime.
/// Government overlay (nation borders + territory colors) is a toggleable layer
/// drawn on top of whatever tile style is active.
/// 
/// ARCHITECTURE:
/// - Loads only visible tiles (frustum culling)
/// - Caches loaded textures in memory (LRU eviction)
/// - Smooth zoom interpolation between discrete tile zoom levels
/// - Integrates with Warship's EventBus via signals
/// - This script has NO dependencies on simulation engines
/// </summary>
public partial class TileMapRenderer : Node2D
{
    // ── Configuration ──────────────────────────────────────
    
    /// <summary>Path to main tile directory (Z/X/Y.png structure)</summary>
    [Export] public string TilePath = "res://assets/map/tiles";
    
    /// <summary>Path to topographic tiles (secondary style)</summary>
    [Export] public string TopoTilePath = "res://assets/map/tiles_topo";
    
    [Export] public int TileSize = 256;
    [Export] public int MinZoom = 0;
    [Export] public int MaxZoom = 10;
    
    /// <summary>Initial map center in lon/lat</summary>
    [Export] public float StartLon = 14.0f;
    [Export] public float StartLat = 48.5f;
    [Export] public float StartZoom = 4.0f;
    
    // Camera controls
    [Export] public float ZoomSpeed = 0.15f;
    [Export] public float PanSmoothing = 6.0f;
    [Export] public float KeyPanSpeed = 400.0f;
    
    /// <summary>Max tiles to keep in memory cache</summary>
    [Export] public int MaxCachedTiles = 2048;
    
    // ── Signals (connect these to WarshipMapBridge) ──────
    
    [Signal] public delegate void MapClickedEventHandler(float lon, float lat);
    [Signal] public delegate void MapRightClickedEventHandler(float lon, float lat);
    [Signal] public delegate void ZoomChangedEventHandler(int zoomLevel);
    [Signal] public delegate void ViewportChangedEventHandler(float lonMin, float latMin, float lonMax, float latMax);
    
    // ── Map Styles ──────────────────────────────────────
    
    public enum MapStyle
    {
        Standard,     // Vibrant OSM — green/blue
        Topographic,  // Elevation contours, terrain shading
    }
    
    private MapStyle _currentStyle = MapStyle.Standard;
    private bool _showGovernmentOverlay = true;
    
    // ── Internal State ─────────────────────────────────────
    
    private Camera2D _camera;
    private float _currentZoom = 4.0f;    // Continuous zoom (for smooth interpolation)
    private int _tileZoom = 4;            // Discrete zoom level (which tiles to load)
    private Vector2 _targetPos;
    private float _targetZoom;
    
    // Tile cache: "style/z/x/y" → (Texture2D, last_access_frame)
    private Dictionary<string, (ImageTexture tex, long frame)> _tileCache = new();
    private Dictionary<string, Sprite2D> _activeTiles = new();
    private Node2D _tileContainer;
    
    // Overlay layers (for game elements drawn on top)
    private Node2D _territoryLayer;
    private Node2D _unitLayer;
    private Node2D _routeLayer;
    private Node2D _uiLabelLayer;
    
    // Dragging
    private bool _isDragging = false;
    private long _frameCount = 0;
    
    // State for knowing when to rebuild overlays
    private int _lastOverlayZoom = -1;
    private MapStyle _lastOverlayStyle;
    private bool _lastOverlayVisible = true;
    
    // ════════════════════════════════════════════════════════
    // LIFECYCLE
    // ════════════════════════════════════════════════════════
    
    public override void _Ready()
    {
        // Create layer hierarchy
        _tileContainer = new Node2D { Name = "TileContainer" };
        AddChild(_tileContainer);
        
        _territoryLayer = new Node2D { Name = "TerritoryLayer" };
        AddChild(_territoryLayer);
        
        _routeLayer = new Node2D { Name = "RouteLayer" };
        AddChild(_routeLayer);
        
        _unitLayer = new Node2D { Name = "UnitLayer" };
        AddChild(_unitLayer);
        
        _uiLabelLayer = new Node2D { Name = "UILabelLayer" };
        AddChild(_uiLabelLayer);
        
        // Camera
        _camera = new Camera2D();
        _camera.MakeCurrent();
        AddChild(_camera);
        
        // Initial position
        _targetZoom = StartZoom;
        _currentZoom = StartZoom;
        _tileZoom = (int)Math.Round(StartZoom);
        _targetPos = LonLatToWorld(StartLon, StartLat, _tileZoom);
        _camera.Position = _targetPos;
        _camera.Zoom = Vector2.One;
        
        GD.Print($"[TileMapRenderer] Ready. Main tiles at '{TilePath}', topo at '{TopoTilePath}'");
        GD.Print($"[TileMapRenderer] Zoom range {MinZoom}-{MaxZoom}, starting at {StartZoom}");
        
        UpdateVisibleTiles();
    }
    
    public override void _Process(double delta)
    {
        _frameCount++;
        float d = (float)delta;
        
        // Smooth camera movement
        _camera.Position = _camera.Position.Lerp(_targetPos, PanSmoothing * d);
        
        // Smooth zoom
        _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, PanSmoothing * d);
        
        // Calculate effective camera zoom from continuous zoom
        int newTileZoom = Mathf.Clamp((int)Math.Round(_currentZoom), MinZoom, MaxZoom);
        float zoomFraction = _currentZoom - newTileZoom;
        float cameraScale = Mathf.Pow(2, zoomFraction);
        _camera.Zoom = new Vector2(cameraScale, cameraScale);
        
        // If tile zoom level changed, reload tiles
        if (newTileZoom != _tileZoom)
        {
            int oldZoom = _tileZoom;
            _tileZoom = newTileZoom;
            
            // Recalculate camera position for new zoom level
            var (lon, lat) = WorldToLonLat(_camera.Position, oldZoom);
            _targetPos = LonLatToWorld(lon, lat, _tileZoom);
            _camera.Position = _targetPos;
            
            EmitSignal(SignalName.ZoomChanged, _tileZoom);
        }
        
        // WASD panning
        Vector2 panDir = Vector2.Zero;
        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up)) panDir.Y -= 1;
        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down)) panDir.Y += 1;
        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left)) panDir.X -= 1;
        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right)) panDir.X += 1;
        if (panDir != Vector2.Zero)
        {
            _targetPos += panDir.Normalized() * KeyPanSpeed * d / cameraScale;
        }
        
        // Update tiles every few frames (not every frame for performance)
        if (_frameCount % 3 == 0)
        {
            UpdateVisibleTiles();
        }
        
        // Evict old cache entries periodically
        if (_frameCount % 120 == 0)
        {
            EvictCache();
        }
    }
    
    // ════════════════════════════════════════════════════════
    // INPUT
    // ════════════════════════════════════════════════════════
    
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseBtn)
        {
            // Zoom
            if (mouseBtn.ButtonIndex == MouseButton.WheelUp && mouseBtn.Pressed)
            {
                _targetZoom = Mathf.Clamp(_targetZoom + ZoomSpeed, MinZoom, MaxZoom);
                GetViewport().SetInputAsHandled();
            }
            else if (mouseBtn.ButtonIndex == MouseButton.WheelDown && mouseBtn.Pressed)
            {
                _targetZoom = Mathf.Clamp(_targetZoom - ZoomSpeed, MinZoom, MaxZoom);
                GetViewport().SetInputAsHandled();
            }
            // Drag to pan (middle click)
            else if (mouseBtn.ButtonIndex == MouseButton.Middle)
            {
                _isDragging = mouseBtn.Pressed;
                GetViewport().SetInputAsHandled();
            }
            // Left click = map interaction
            else if (mouseBtn.ButtonIndex == MouseButton.Left && mouseBtn.Pressed)
            {
                Vector2 worldPos = GetGlobalMousePosition();
                var (lon, lat) = WorldToLonLat(worldPos, _tileZoom);
                EmitSignal(SignalName.MapClicked, lon, lat);
                GetViewport().SetInputAsHandled();
            }
            // Right click = command
            else if (mouseBtn.ButtonIndex == MouseButton.Right && mouseBtn.Pressed)
            {
                Vector2 worldPos = GetGlobalMousePosition();
                var (lon, lat) = WorldToLonLat(worldPos, _tileZoom);
                EmitSignal(SignalName.MapRightClicked, lon, lat);
                GetViewport().SetInputAsHandled();
            }
        }
        
        if (@event is InputEventMouseMotion mouseMotion && _isDragging)
        {
            _targetPos -= mouseMotion.Relative / _camera.Zoom;
            GetViewport().SetInputAsHandled();
        }
        
        // Keyboard shortcuts for map modes
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            switch (key.Keycode)
            {
                case Key.F1:
                    SetMapStyle(MapStyle.Standard);
                    break;
                case Key.F2:
                    SetMapStyle(MapStyle.Topographic);
                    break;
                case Key.G:
                    ToggleGovernmentOverlay();
                    break;
            }
        }
    }
    
    // ════════════════════════════════════════════════════════
    // MAP STYLE & GOVERNMENT OVERLAY
    // ════════════════════════════════════════════════════════
    
    /// <summary>Switch between OSM Standard and Topographic tile styles.</summary>
    public void SetMapStyle(MapStyle style)
    {
        if (_currentStyle == style) return;
        _currentStyle = style;
        
        // Clear all active tile sprites — they'll reload with new style
        foreach (var kvp in _activeTiles)
            kvp.Value.QueueFree();
        _activeTiles.Clear();
        
        // Adjust max zoom per style (topo only goes to 9)
        MaxZoom = style == MapStyle.Topographic ? 9 : 10;
        _targetZoom = Mathf.Clamp(_targetZoom, MinZoom, MaxZoom);
        
        GD.Print($"[TileMapRenderer] Style changed to: {style}");
        UpdateVisibleTiles();
    }
    
    /// <summary>Toggle the government overlay (nation borders + territory tinting).</summary>
    public void ToggleGovernmentOverlay()
    {
        _showGovernmentOverlay = !_showGovernmentOverlay;
        _territoryLayer.Visible = _showGovernmentOverlay;
        _routeLayer.Visible = _showGovernmentOverlay;
        _uiLabelLayer.Visible = _showGovernmentOverlay;
        GD.Print($"[TileMapRenderer] Government overlay: {(_showGovernmentOverlay ? "ON" : "OFF")}");
    }
    
    /// <summary>Set government overlay visibility explicitly.</summary>
    public void SetGovernmentOverlay(bool visible)
    {
        _showGovernmentOverlay = visible;
        _territoryLayer.Visible = visible;
        _routeLayer.Visible = visible;
        _uiLabelLayer.Visible = visible;
    }
    
    public MapStyle GetCurrentStyle() => _currentStyle;
    public bool IsGovernmentOverlayVisible() => _showGovernmentOverlay;
    
    // ════════════════════════════════════════════════════════
    // TILE LOADING & RENDERING
    // ════════════════════════════════════════════════════════
    
    private string GetCurrentTilePath()
    {
        return _currentStyle switch
        {
            MapStyle.Topographic => TopoTilePath,
            _ => TilePath,
        };
    }
    
    private void UpdateVisibleTiles()
    {
        int z = _tileZoom;
        int n = 1 << z; // 2^zoom = number of tiles per axis
        
        // Calculate visible world-space viewport
        Vector2 viewSize = GetViewportRect().Size / _camera.Zoom;
        Vector2 camPos = _camera.Position;
        
        float worldLeft = camPos.X - viewSize.X / 2;
        float worldRight = camPos.X + viewSize.X / 2;
        float worldTop = camPos.Y - viewSize.Y / 2;
        float worldBottom = camPos.Y + viewSize.Y / 2;
        
        // Convert to tile coordinates (with padding)
        int tileLeft   = Math.Max(0, (int)Math.Floor(worldLeft / TileSize) - 1);
        int tileRight  = Math.Min(n - 1, (int)Math.Ceiling(worldRight / TileSize) + 1);
        int tileTop    = Math.Max(0, (int)Math.Floor(worldTop / TileSize) - 1);
        int tileBottom = Math.Min(n - 1, (int)Math.Ceiling(worldBottom / TileSize) + 1);
        
        // Build style prefix for cache keys
        string stylePrefix = _currentStyle.ToString();
        
        // Track which tiles should be visible
        HashSet<string> neededKeys = new();
        
        for (int x = tileLeft; x <= tileRight; x++)
        {
            for (int y = tileTop; y <= tileBottom; y++)
            {
                string key = $"{stylePrefix}/{z}/{x}/{y}";
                neededKeys.Add(key);
                
                if (!_activeTiles.ContainsKey(key))
                {
                    // Load and display this tile
                    var tex = LoadTile(z, x, y);
                    if (tex != null)
                    {
                        var sprite = new Sprite2D();
                        sprite.Texture = tex;
                        sprite.Centered = false;
                        sprite.Position = new Vector2(x * TileSize, y * TileSize);
                        sprite.TextureFilter = CanvasItem.TextureFilterEnum.Linear;
                        _tileContainer.AddChild(sprite);
                        _activeTiles[key] = sprite;
                    }
                }
            }
        }
        
        // Remove tiles that are no longer visible
        List<string> toRemove = new();
        foreach (var kvp in _activeTiles)
        {
            if (!neededKeys.Contains(kvp.Key))
            {
                kvp.Value.QueueFree();
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var key in toRemove)
        {
            _activeTiles.Remove(key);
        }
    }
    
    private ImageTexture LoadTile(int z, int x, int y)
    {
        string stylePrefix = _currentStyle.ToString();
        string key = $"{stylePrefix}/{z}/{x}/{y}";
        
        // Check memory cache first
        if (_tileCache.TryGetValue(key, out var cached))
        {
            _tileCache[key] = (cached.tex, _frameCount); // Update access time
            return cached.tex;
        }
        
        // Build file path based on current style
        string basePath = GetCurrentTilePath();
        string filePath = $"{basePath}/{z}/{x}/{y}.png";
        
        // Try loading as a resource first (works for res:// paths)
        if (filePath.StartsWith("res://"))
        {
            if (ResourceLoader.Exists(filePath))
            {
                var tex = GD.Load<Texture2D>(filePath);
                if (tex != null)
                {
                    var imgTex = tex as ImageTexture;
                    if (imgTex == null)
                    {
                        var img = tex.GetImage();
                        imgTex = ImageTexture.CreateFromImage(img);
                    }
                    _tileCache[key] = (imgTex, _frameCount);
                    return imgTex;
                }
            }
        }
        
        // Try loading from absolute/user path
        string absPath = ProjectSettings.GlobalizePath(filePath);
        if (File.Exists(absPath))
        {
            var img = new Image();
            var err = img.Load(absPath);
            if (err == Error.Ok)
            {
                var tex = ImageTexture.CreateFromImage(img);
                _tileCache[key] = (tex, _frameCount);
                return tex;
            }
        }
        
        // Tile not found — try fallback: load a lower-zoom tile and scale it
        if (z > MinZoom)
        {
            var fallback = LoadTile(z - 1, x / 2, y / 2);
            // Could crop and scale the parent tile here for smoother LOD
            // For now just return null (shows as empty)
        }
        
        return null;
    }
    
    private void EvictCache()
    {
        if (_tileCache.Count <= MaxCachedTiles) return;
        
        var entries = new List<(string key, long frame)>();
        foreach (var kvp in _tileCache)
        {
            entries.Add((kvp.Key, kvp.Value.frame));
        }
        entries.Sort((a, b) => a.frame.CompareTo(b.frame));
        
        int toRemove = _tileCache.Count - MaxCachedTiles + 64; // Remove extra for headroom
        for (int i = 0; i < toRemove && i < entries.Count; i++)
        {
            _tileCache.Remove(entries[i].key);
        }
    }
    
    // ════════════════════════════════════════════════════════
    // COORDINATE CONVERSION
    // ════════════════════════════════════════════════════════
    
    /// <summary>Convert lon/lat to world-space pixel position at given zoom.</summary>
    public Vector2 LonLatToWorld(float lon, float lat, int zoom)
    {
        int n = 1 << zoom;
        float x = (lon + 180.0f) / 360.0f * n * TileSize;
        float latRad = Mathf.DegToRad(lat);
        float y = (1.0f - Mathf.Log(Mathf.Tan(latRad) + 1.0f / Mathf.Cos(latRad)) / Mathf.Pi) / 2.0f * n * TileSize;
        return new Vector2(x, y);
    }
    
    /// <summary>Convert world-space pixel position to lon/lat at given zoom.</summary>
    public (float lon, float lat) WorldToLonLat(Vector2 worldPos, int zoom)
    {
        int n = 1 << zoom;
        float lon = worldPos.X / (n * TileSize) * 360.0f - 180.0f;
        float latRad = Mathf.Atan(Mathf.Sinh(Mathf.Pi * (1.0f - 2.0f * worldPos.Y / (n * TileSize))));
        float lat = Mathf.RadToDeg(latRad);
        return (lon, lat);
    }
    
    /// <summary>Convert lon/lat to screen position (for UI overlays).</summary>
    public Vector2 LonLatToScreen(float lon, float lat)
    {
        Vector2 worldPos = LonLatToWorld(lon, lat, _tileZoom);
        return (worldPos - _camera.Position) * _camera.Zoom + GetViewportRect().Size / 2;
    }
    
    // ════════════════════════════════════════════════════════
    // PUBLIC API
    // ════════════════════════════════════════════════════════
    
    /// <summary>Center the map on a lon/lat with optional zoom level.</summary>
    public void FocusOn(float lon, float lat, float zoom = -1)
    {
        if (zoom > 0) _targetZoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
        int z = (int)Math.Round(_targetZoom);
        _targetPos = LonLatToWorld(lon, lat, z);
    }
    
    /// <summary>Get current map center as lon/lat.</summary>
    public (float lon, float lat) GetCenter()
    {
        return WorldToLonLat(_camera.Position, _tileZoom);
    }
    
    /// <summary>Get current discrete zoom level.</summary>
    public int GetZoomLevel() => _tileZoom;
    
    /// <summary>Access the territory overlay layer for nation visuals.</summary>
    public Node2D GetTerritoryLayer() => _territoryLayer;
    
    /// <summary>Access the unit overlay layer for armies/fleets.</summary>
    public Node2D GetUnitLayer() => _unitLayer;
    
    /// <summary>Access the route overlay layer for trade routes, frontlines.</summary>
    public Node2D GetRouteLayer() => _routeLayer;
    
    /// <summary>Access the UI label layer for city names, popups.</summary>
    public Node2D GetUILabelLayer() => _uiLabelLayer;
    
    /// <summary>
    /// Draw a colored polygon over a territory.
    /// Uses lon/lat coordinates, renders to the territory layer.
    /// </summary>
    public void DrawTerritory(float[][] lonLatPoints, Color color, float opacity = 0.15f)
    {
        var polyPoints = new Vector2[lonLatPoints.Length];
        for (int i = 0; i < lonLatPoints.Length; i++)
        {
            polyPoints[i] = LonLatToWorld(lonLatPoints[i][0], lonLatPoints[i][1], _tileZoom);
        }
        
        var poly = new Polygon2D();
        poly.Polygon = polyPoints;
        poly.Color = new Color(color.R, color.G, color.B, opacity);
        _territoryLayer.AddChild(poly);
    }
    
    /// <summary>
    /// Draw a glowing border line around a territory polygon.
    /// </summary>
    public void DrawBorder(float[][] lonLatPoints, Color color, float width = 3f)
    {
        var points = new Vector2[lonLatPoints.Length + 1]; // +1 to close the loop
        for (int i = 0; i < lonLatPoints.Length; i++)
        {
            points[i] = LonLatToWorld(lonLatPoints[i][0], lonLatPoints[i][1], _tileZoom);
        }
        points[lonLatPoints.Length] = points[0]; // Close the polygon
        
        // Glow shadow
        var glowLine = new Line2D();
        glowLine.Points = points;
        glowLine.DefaultColor = new Color(color.R, color.G, color.B, 0.3f);
        glowLine.Width = width + 4;
        _territoryLayer.AddChild(glowLine);
        
        // Main border
        var borderLine = new Line2D();
        borderLine.Points = points;
        borderLine.DefaultColor = color;
        borderLine.Width = width;
        _territoryLayer.AddChild(borderLine);
    }
    
    /// <summary>Place a unit marker at lon/lat. Returns the Sprite2D for updates.</summary>
    public Sprite2D PlaceUnit(float lon, float lat, Texture2D icon)
    {
        var sprite = new Sprite2D();
        sprite.Texture = icon;
        sprite.Position = LonLatToWorld(lon, lat, _tileZoom);
        sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        _unitLayer.AddChild(sprite);
        return sprite;
    }
    
    /// <summary>Draw a line between two lon/lat points (trade routes, frontlines).</summary>
    public Line2D DrawRoute(float lon1, float lat1, float lon2, float lat2, Color color, float width = 2, bool dashed = false)
    {
        var line = new Line2D();
        line.AddPoint(LonLatToWorld(lon1, lat1, _tileZoom));
        line.AddPoint(LonLatToWorld(lon2, lat2, _tileZoom));
        line.DefaultColor = color;
        line.Width = width;
        _routeLayer.AddChild(line);
        return line;
    }
    
    /// <summary>Add a text label at lon/lat (city names, nation names).</summary>
    public Label PlaceLabel(float lon, float lat, string text, int fontSize = 14, Color? color = null)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color ?? Colors.White);
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.8f));
        label.AddThemeConstantOverride("shadow_offset_x", 2);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
        
        // Position the label in world space
        Vector2 worldPos = LonLatToWorld(lon, lat, _tileZoom);
        label.Position = worldPos;
        _uiLabelLayer.AddChild(label);
        return label;
    }
    
    /// <summary>Clear all overlay elements from a specific layer.</summary>
    public void ClearLayer(Node2D layer)
    {
        foreach (Node child in layer.GetChildren())
        {
            child.QueueFree();
        }
    }
    
    /// <summary>Clear all overlay layers (territories, routes, units, labels).</summary>
    public void ClearAllOverlays()
    {
        ClearLayer(_territoryLayer);
        ClearLayer(_routeLayer);
        ClearLayer(_unitLayer);
        ClearLayer(_uiLabelLayer);
    }
}
