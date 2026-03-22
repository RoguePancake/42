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
    
    // SNES-style base theme but cranked up
    private static readonly Color[] TerrainColors = new Color[]
    {
        new Color(0.08f, 0.16f, 0.32f),  // 0: Deep Water
        new Color(0.12f, 0.32f, 0.58f),  // 1: Water
        new Color(0.85f, 0.76f, 0.50f),  // 2: Sand
        new Color(0.32f, 0.62f, 0.30f),  // 3: Grass
        new Color(0.18f, 0.40f, 0.20f),  // 4: Forest
        new Color(0.48f, 0.56f, 0.38f),  // 5: Hills
        new Color(0.55f, 0.48f, 0.42f),  // 6: Mountain
        new Color(0.92f, 0.94f, 0.96f),  // 7: Snow
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
                    // Blend base noise
                    float n = noise.GetNoise2D(px * 15f + (t * 100), py * 15f); // Scale noise
                    float cell = detailsNoise.GetNoise2D(px * 10f, py * 10f);
                    
                    Color pixelColor = baseColor;
                    
                    if (t == (int)TerrainType.DeepWater || t == (int)TerrainType.Water)
                    {
                        // Water gets wavy lateral noise
                        pixelColor = n > 0.2f ? highlight : (cell < -0.2f ? shadow : baseColor);
                    }
                    else if (t == (int)TerrainType.Sand)
                    {
                        // Sand gets tiny speckles
                        pixelColor = cell > 0.6f ? shadow : (cell < -0.6f ? highlight : baseColor);
                    }
                    else if (t == (int)TerrainType.Grass)
                    {
                        // Grass gets sweeping tufts
                        pixelColor = n > 0.4f ? highlight : baseColor;
                        if (cell > 0.7f) pixelColor = new Color(0.9f, 0.85f, 0.2f); // Tiny flowers
                    }
                    else if (t == (int)TerrainType.Forest)
                    {
                        // Forest gets cellular canopy patterns
                        pixelColor = cell > 0.1f ? highlight : shadow;
                    }
                    else if (t == (int)TerrainType.Hills)
                    {
                        // Rolling gradients
                        pixelColor = n > 0f ? highlight : shadow;
                    }
                    else if (t == (int)TerrainType.Mountain)
                    {
                        // Jagged crags
                        pixelColor = cell > 0.2f ? shadow : baseColor;
                        if (py < 12 && cell > -0.2f) pixelColor = Colors.White; // Snow caps natively in the texture!
                    }
                    else if (t == (int)TerrainType.Snow)
                    {
                        pixelColor = cell > 0.5f ? highlight : baseColor;
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

        // 4. Draw AAA Cities
        foreach (var city in _world.Cities)
        {
            var pos = new Vector2(city.TileX * TileSize + (TileSize / 2f), city.TileY * TileSize + (TileSize / 2f));
            var natColor = _world.Nations[int.Parse(city.NationId.Split('_')[1])].NationColor;
            
            // Draw a multi-layered isometric castle icon using polygons
            if (city.IsCapital)
            {
                // Shadow
                DrawCircle(pos + new Vector2(0, 8), 16, new Color(0, 0, 0, 0.5f));
                
                // Keep base
                DrawRect(new Rect2(pos.X - 12, pos.Y - 12, 24, 24), Colors.DarkGray);
                DrawRect(new Rect2(pos.X - 12, pos.Y - 12, 12, 24), Colors.Gray); // left light
                
                // Roof
                Vector2[] roof = { new Vector2(pos.X - 16, pos.Y - 12), new Vector2(pos.X + 16, pos.Y - 12), new Vector2(pos.X, pos.Y - 24) };
                DrawPolygon(roof, new Color[] { natColor, natColor, natColor });
                
                // Nation Flag Pole
                DrawLine(new Vector2(pos.X, pos.Y - 24), new Vector2(pos.X, pos.Y - 38), Colors.DarkGoldenrod, 2);
                DrawRect(new Rect2(pos.X, pos.Y - 38, 10, 6), natColor);
                
                // Glow point
                DrawCircle(pos, 3, Colors.LightYellow);
            }
            else // Minor town
            {
                DrawCircle(pos + new Vector2(0, 4), 10, new Color(0, 0, 0, 0.4f));
                DrawRect(new Rect2(pos.X - 8, pos.Y - 8, 16, 16), Colors.SaddleBrown);
                Vector2[] roof = { new Vector2(pos.X - 10, pos.Y - 8), new Vector2(pos.X + 10, pos.Y - 8), new Vector2(pos.X, pos.Y - 16) };
                DrawPolygon(roof, new Color[] { natColor, natColor, natColor });
            }
        }

        // 5. Draw Awesome Units
        foreach (var unit in _world.Units)
        {
            var pos = new Vector2(unit.PixelX, unit.PixelY);
            var natColor = _world.Nations[int.Parse(unit.NationId.Split('_')[1])].NationColor;

            if (unit.Type == UnitType.Soldier)
            {
                // Tiny swarming dot
                DrawCircle(pos, 2.5f, natColor);
                // Optional slight glow if player's unit
                if (unit.NationId == _world.PlayerNationId)
                {
                    DrawCircle(pos, 1.5f, Colors.White);
                }
            }
            else if (unit.Type == UnitType.Tank)
            {
                // Selection highlight
                if (unit.Id == _selectedUnitId)
                    DrawArc(pos, 22, 0, Mathf.Pi * 2, 32, Colors.Yellow, 3);
                
                // Draw unit shadow
                DrawCircle(pos + new Vector2(0, 6), 14, new Color(0, 0, 0, 0.4f));

                // Tank Body
                DrawRect(new Rect2(pos.X - 14, pos.Y - 10, 28, 20), natColor);
                // Turret
                DrawCircle(pos, 8, Colors.DarkGray);
                // Barrel
                DrawLine(pos, pos + new Vector2(16, 0), Colors.DarkGray, 4);
            }
            else if (unit.Type == UnitType.Ship)
            {
                // Selection highlight
                if (unit.Id == _selectedUnitId)
                    DrawArc(pos, 22, 0, Mathf.Pi * 2, 32, Colors.Yellow, 3);
                    
                DrawCircle(pos + new Vector2(0, 6), 14, new Color(0, 0, 0, 0.4f));

                // Ship shape
                var points = new Vector2[] {
                    pos + new Vector2(-16, -10),
                    pos + new Vector2(12, -10),
                    pos + new Vector2(24, 0),
                    pos + new Vector2(12, 10),
                    pos + new Vector2(-16, 10)
                };
                DrawPolygon(points, new Color[] { natColor, natColor, natColor, natColor, natColor });
                // Bridge
                DrawRect(new Rect2(pos.X - 12, pos.Y - 6, 16, 12), Colors.Gray);
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
                // Red crosshair
                DrawArc(markerPos, 16, 0, Mathf.Pi * 2, 32, Colors.Red, 2);
                DrawLine(markerPos - new Vector2(24, 0), markerPos + new Vector2(24, 0), Colors.Red, 2);
                DrawLine(markerPos - new Vector2(0, 24), markerPos + new Vector2(0, 24), Colors.Red, 2);
            }
            else if (playerNation.GlobalMilitaryOrder == MilitaryOrder.Stage)
            {
                // Blue circle
                DrawArc(markerPos, 24, 0, Mathf.Pi * 2, 32, new Color(0.2f, 0.6f, 1f), 3);
            }
        }
        // 7. Draw VIP Characters
        foreach (var c in _world.Characters)
        {
            var pos = new Vector2(c.PixelX, c.PixelY);
            
            // Player Halo
            if (c.IsPlayer)
            {
                DrawArc(pos, 30, 0, Mathf.Pi * 2, 32, Colors.Cyan, 4);
                DrawArc(pos, 38, 0, Mathf.Pi * 2, 32, Colors.Cyan * new Color(1,1,1,0.5f), 2);
            }
            
            // Meeple Base
            DrawCircle(pos + new Vector2(0, 10), 12, new Color(0, 0, 0, 0.5f)); // Shadow
            DrawCircle(pos + new Vector2(0, -6), 6, Colors.Bisque); // Head
            
            // Body with nation color
            var natColor = _world.Nations[int.Parse(c.NationId.Split('_')[1])].NationColor;
            var body = new Vector2[] {
                pos + new Vector2(-8, 0),
                pos + new Vector2(8, 0),
                pos + new Vector2(10, 14),
                pos + new Vector2(-10, 14)
            };
            DrawPolygon(body, new Color[] { natColor, natColor, natColor.Darkened(0.2f), natColor.Darkened(0.2f) });
            
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
