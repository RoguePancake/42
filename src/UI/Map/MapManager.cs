using Godot;
using System.Linq;
using Warship.World;
using Warship.Data;
using Warship.Core;
using Warship.Events;
using Warship.UI.HUD;

namespace Warship.UI.Map;

/// <summary>
/// "AAA Graphics Update" Map Renderer
/// Uses programmatically generated high-definition textures (64x64) for terrain,
/// draws smooth rivers, isometric 3D-looking cities, and neon borders.
/// </summary>
public partial class MapManager : Node2D
{
    public const int TileSize = 64;  // Doubled from 32 for HD feel
    public const int MapWidth = 80;
    public const int MapHeight = 50;
    public const int Seed = 42;

    private WorldData? _world;
    private Texture2D[] _terrainTextures = new Texture2D[8];
    
    // UI State
    private string? _selectedUnitId;
    private DossierPanel? _dossierPanel;
    
    // Geopolitical Thriller "Holographic War Room" Theme
    private static readonly Color[] TerrainColors = new Color[]
    {
        new Color(0.02f, 0.03f, 0.05f),  // 0: Deep Water (Abyssal Black/Blue)
        new Color(0.05f, 0.08f, 0.12f),  // 1: Water (Dark Slate)
        new Color(0.12f, 0.15f, 0.20f),  // 2: Sand (Low Elevation Land)
        new Color(0.15f, 0.18f, 0.24f),  // 3: Grass (Standard Elevation)
        new Color(0.18f, 0.21f, 0.28f),  // 4: Forest (Mid Elevation)
        new Color(0.22f, 0.25f, 0.33f),  // 5: Hills (High Elevation)
        new Color(0.26f, 0.30f, 0.38f),  // 6: Mountain (Peaks)
        new Color(0.32f, 0.36f, 0.44f),  // 7: Snow (Highest Peaks)
    };

    public override void _Ready()
    {
        GD.Print("[MapManager] Getting world from WorldStateManager...");
        _world = WorldStateManager.Instance?.Data;

        if (_world != null)
        {
            GenerateHDTextures();
            GD.Print("[MapManager] Textures baked!");
        }

        // Listen for movement so we can trigger a redraw when a unit lands
        EventBus.Instance?.Subscribe<UnitMovedEvent>(OnUnitMoved);

        // Grab dossier panel reference (it's in the UILayer)
        _dossierPanel = GetNode<DossierPanel>("/root/Main/UILayer/DossierPanel");
    }
    
    private void OnUnitMoved(UnitMovedEvent ev)
    {
        var unit = _world?.Units.FirstOrDefault(u => u.Id == ev.UnitId);
        if (unit != null)
        {
            // Set the target pixel coordinate
            unit.TargetPixelX = ev.ToX * TileSize + TileSize / 2f;
            unit.TargetPixelY = ev.ToY * TileSize + TileSize / 2f;
        }
    }

    public override void _Process(double delta)
    {
        if (_world == null)
        {
            _world = WorldStateManager.Instance?.Data;
            if (_world != null && _world.TerrainMap != null)
            {
                GenerateHDTextures();
                GD.Print("[MapManager] Textures baked (Deferred)!");
                QueueRedraw();
            }
            return;
        }
        
        bool needsRedraw = false;
        
        // Interpolate moving units
        foreach (var u in _world.Units)
        {
            float speed = 300f * (float)delta; // pixels per second
            var currentPos = new Vector2(u.PixelX, u.PixelY);
            var targetPos = new Vector2(u.TargetPixelX, u.TargetPixelY);
            
            if (currentPos.DistanceTo(targetPos) > 1f)
            {
                if (currentPos.DistanceTo(targetPos) <= speed)
                {
                    u.PixelX = targetPos.X;
                    u.PixelY = targetPos.Y;
                }
                else
                {
                    var dir = (targetPos - currentPos).Normalized();
                    u.PixelX += dir.X * speed;
                    u.PixelY += dir.Y * speed;
                }
                needsRedraw = true;
            }
        }
        
        if (needsRedraw)
            QueueRedraw();
    }
    
    public override void _UnhandledInput(InputEvent @event)
    {
        if (_world == null) return;
        
        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            var tile = PixelToTile(GetGlobalMousePosition());
            
            if (mb.ButtonIndex == MouseButton.Left)
            {
                // Priority 1: Check for Character click (opens Dossier)
                var clickedChar = _world.Characters.FirstOrDefault(c => c.TileX == tile.X && c.TileY == tile.Y);
                if (clickedChar != null)
                {
                    _dossierPanel?.ShowCharacter(clickedChar);
                    _selectedUnitId = null;
                    QueueRedraw();
                    GetViewport().SetInputAsHandled();
                    return;
                }

                // Priority 2: Check for Unit click
                var clickedUnit = _world.Units.FirstOrDefault(u => u.TileX == tile.X && u.TileY == tile.Y);
                if (clickedUnit != null)
                {
                    _selectedUnitId = clickedUnit.Id;
                    QueueRedraw();
                    GetViewport().SetInputAsHandled();
                }
                else if (_selectedUnitId != null)
                {
                    _selectedUnitId = null; // deselect
                    QueueRedraw();
                }
            }
            else if (mb.ButtonIndex == MouseButton.Right)
            {
                if (_selectedUnitId != null)
                {
                    // Issue move command via EventBus
                    EventBus.Instance?.Publish(new UnitMoveRequested(_selectedUnitId, tile.X, tile.Y));
                    GetViewport().SetInputAsHandled();
                }
                else
                {
                    // Right click on map with nothing selected sets global command target
                    int pIdx = int.Parse(_world.PlayerNationId.Split('_')[1]);
                    var playerNation = _world.Nations[pIdx];
                    playerNation.CommandTargetX = tile.X;
                    playerNation.CommandTargetY = tile.Y;
                    QueueRedraw();
                    GetViewport().SetInputAsHandled();
                }
            }
        }
    }

    /// <summary>
    /// Generates beautiful 64x64 textures for each terrain type using FastNoiseLite.
    /// This gives us the "AAA" pixel-perfect seamless look without needing external images.
    /// </summary>
    private void GenerateHDTextures()
    {
        var noise = new FastNoiseLite();
        noise.Seed = Seed;
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        
        var detailsNoise = new FastNoiseLite();
        detailsNoise.Seed = Seed + 1;
        detailsNoise.NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular;
        detailsNoise.CellularDistanceFunction = FastNoiseLite.CellularDistanceFunctionEnum.Euclidean;

        for (int t = 0; t < 8; t++)
        {
            var img = Image.CreateEmpty(TileSize, TileSize, false, Image.Format.Rgba8);
            Color baseColor = TerrainColors[t];
            Color highlight = baseColor.Lightened(0.15f);
            Color shadow = baseColor.Darkened(0.15f);

            for (int px = 0; px < TileSize; px++)
            {
                for (int py = 0; py < TileSize; py++)
                {
                    Color pixelColor = baseColor;

                    // Cyberpunk Grid System: Draw subtle scanlines and grid over everything
                    if (px == 0 || py == 0)
                    {
                        // Tile grid
                        pixelColor.A = 0.5f;
                        pixelColor = pixelColor.Lightened(0.1f);
                    }
                    if (px % 4 == 0)
                    {
                        // Sub-grid
                        pixelColor = pixelColor.Lightened(0.02f);
                    }

                    img.SetPixel(px, py, pixelColor);
                }
            }
            
            _terrainTextures[t] = ImageTexture.CreateFromImage(img);
        }
    }

    public override void _Draw()
    {
        if (_world == null || _world.TerrainMap == null || _world.OwnershipMap == null) return;

        // 1. Draw HD Terrain 
        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                int t = _world.TerrainMap[x, y];
                var pos = new Vector2(x * TileSize, y * TileSize);
                DrawTexture(_terrainTextures[t], pos);
            }
        }

        // 2. Draw Rivers (Thick, meandering bezier-like lines)
        var riverColor = TerrainColors[(int)TerrainType.Water].Lightened(0.2f);
        foreach (var river in _world.RiverPaths)
        {
            if (river.Length < 2) continue;
            var points = new Vector2[river.Length];
            for (int i = 0; i < river.Length; i++)
                points[i] = new Vector2(river[i].X * TileSize, river[i].Y * TileSize);
            
            // Draw wide river shadow
            DrawPolyline(points, new Color(0,0,0,0.3f), 8, true);
            // Draw flowing river
            DrawPolyline(points, riverColor, 6, true);
            // River center highlight
            DrawPolyline(points, Colors.White, 2, true);
        }

        // 3. Draw Splendid Borders & Overlays
        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                int owner = _world.OwnershipMap[x, y];
                if (owner == -1) continue;
                
                var natColor = _world.Nations[owner].NationColor;
                var pos = new Vector2(x * TileSize, y * TileSize);
                
                // Territory overlay tint
                DrawRect(new Rect2(pos, new Vector2(TileSize, TileSize)), new Color(natColor, 0.15f));
                
                // Glowing Borders
                float borderW = 4f;
                Color glow = natColor; // Opaque color for the border
                
                if (y == 0 || _world.OwnershipMap[x, y - 1] != owner)
                    DrawLine(pos, pos + new Vector2(TileSize, 0), glow, borderW);
                if (y == MapHeight - 1 || _world.OwnershipMap[x, y + 1] != owner)
                    DrawLine(pos + new Vector2(0, TileSize), pos + new Vector2(TileSize, TileSize), glow, borderW);
                if (x == 0 || _world.OwnershipMap[x - 1, y] != owner)
                    DrawLine(pos, pos + new Vector2(0, TileSize), glow, borderW);
                if (x == MapWidth - 1 || _world.OwnershipMap[x + 1, y] != owner)
                    DrawLine(pos + new Vector2(TileSize, 0), pos + new Vector2(TileSize, TileSize), glow, borderW);
            }
        }

        // 4. Draw Cyberpunk Node Cities
        foreach (var city in _world.Cities)
        {
            var pos = new Vector2(city.TileX * TileSize + (TileSize / 2f), city.TileY * TileSize + (TileSize / 2f));
            var natColor = _world.Nations[int.Parse(city.NationId.Split('_')[1])].NationColor;
            
            // Draw a high-tech glowing network node
            if (city.IsCapital)
            {
                // Capital: Hexagonal core
                Vector2[] hex = new Vector2[6];
                for(int i = 0; i < 6; i++) {
                    float angle = i * Mathf.Pi / 3f;
                    hex[i] = pos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 16f;
                }
                DrawPolygon(hex, new Color[] { natColor, natColor, natColor, natColor, natColor, natColor });
                DrawArc(pos, 22f, 0, Mathf.Pi*2, 6, Colors.White, 3f);
                DrawCircle(pos, 6f, Colors.White);
            }
            else // Minor node
            {
                DrawCircle(pos, 8f, natColor.Darkened(0.2f));
                DrawArc(pos, 12f, 0, Mathf.Pi*2, 8, natColor, 2f);
                DrawCircle(pos, 3f, Colors.White);
            }
        }

        // 5. Draw Cyber Units
        foreach (var unit in _world.Units)
        {
            if (!unit.IsAlive) continue;

            var pos = new Vector2(unit.PixelX, unit.PixelY);
            var natColor = _world.Nations[int.Parse(unit.NationId.Split('_')[1])].NationColor;

            if (unit.Type == UnitType.Soldier)
            {
                // Glowing blip
                DrawCircle(pos, 3f, natColor);
                DrawCircle(pos, 1.5f, Colors.White);
            }
            else
            {
                // Highlight Selection
                if (unit.Id == _selectedUnitId)
                    DrawArc(pos, 22, 0, Mathf.Pi * 2, 8, Colors.Yellow, 2);
                
                // Tactical Chevron
                Vector2[] chevron = {
                    pos + new Vector2(0, -12),
                    pos + new Vector2(8, 8),
                    pos + new Vector2(0, 4),
                    pos + new Vector2(-8, 8)
                };
                DrawPolygon(chevron, new Color[] { natColor, natColor, natColor, natColor });
            }
        }

        // 6. Draw Player Command Markers
        int pIdx = int.Parse(_world.PlayerNationId.Split('_')[1]);
        var playerNation = _world.Nations[pIdx];
        if (playerNation.CommandTargetX >= 0)
        {
            Vector2 markerPos = new Vector2(playerNation.CommandTargetX * 64 + 32, playerNation.CommandTargetY * 64 + 32);
            if (playerNation.GlobalMilitaryOrder == MilitaryOrder.Attack)
            {
                // Tactical Target Lock
                DrawArc(markerPos, 16, 0, Mathf.Pi * 2, 16, Colors.Red, 2);
                DrawLine(markerPos - new Vector2(24, 0), markerPos - new Vector2(10, 0), Colors.Red, 2);
                DrawLine(markerPos + new Vector2(10, 0), markerPos + new Vector2(24, 0), Colors.Red, 2);
                DrawLine(markerPos - new Vector2(0, 24), markerPos - new Vector2(0, 10), Colors.Red, 2);
                DrawLine(markerPos + new Vector2(0, 10), markerPos + new Vector2(0, 24), Colors.Red, 2);
            }
            else if (playerNation.GlobalMilitaryOrder == MilitaryOrder.Stage)
            {
                // Tactical Rally Point
                DrawArc(markerPos, 24, 0, Mathf.Pi * 2, 32, Colors.DodgerBlue, 2);
                DrawArc(markerPos, 32, 0, Mathf.Pi * 2, 4, Colors.DodgerBlue, 1);
            }
        }
        
        // 7. Draw VIP Characters (High Value Targets)
        foreach (var c in _world.Characters)
        {
            if (c.Role == "Eliminated") continue; // Don't draw assassinated targets
            var pos = new Vector2(c.PixelX, c.PixelY);
            
            // Player Halo
            if (c.IsPlayer)
            {
                DrawArc(pos, 28, 0, Mathf.Pi * 2, 8, Colors.Cyan, 3);
            }
            
            // HVT Diamond
            var natColor = _world.Nations[int.Parse(c.NationId.Split('_')[1])].NationColor;
            Vector2[] diamond = {
                pos + new Vector2(0, -18),
                pos + new Vector2(14, 0),
                pos + new Vector2(0, 18),
                pos + new Vector2(-14, 0)
            };
            
            // Outer diamond
            DrawPolyline(new Vector2[]{diamond[0], diamond[1], diamond[2], diamond[3], diamond[0]}, natColor, 3f);
            
            // Inner fill
            DrawPolygon(diamond, new Color[] { new Color(0,0,0,0.8f), new Color(0,0,0,0.8f), new Color(0,0,0,0.8f), new Color(0,0,0,0.8f) });
            
            // Center Core
            DrawCircle(pos, 4f, Colors.White);
            
            // Little name tag text
            var font = ThemeDB.FallbackFont;
            DrawString(font, pos + new Vector2(-16, -20), c.Role, HorizontalAlignment.Center, 32, 12, Colors.White);
        }
    }

    /// <summary>Get terrain type at a tile coordinate.</summary>
    public int GetTerrain(int x, int y)
    {
        if (_world == null || _world.TerrainMap == null || x < 0 || x >= MapWidth || y < 0 || y >= MapHeight)
            return 0;
        return _world.TerrainMap[x, y];
    }

    public static Vector2I PixelToTile(Vector2 pixel)
    {
        return new Vector2I((int)(pixel.X / TileSize), (int)(pixel.Y / TileSize));
    }

    public static Vector2 TileToPixel(int tx, int ty)
    {
        return new Vector2(tx * TileSize + TileSize / 2f, ty * TileSize + TileSize / 2f);
    }
}
