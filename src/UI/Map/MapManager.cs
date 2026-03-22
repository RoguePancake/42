using Godot;
using Warship.World;
using Warship.Data;

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
        GD.Print("[MapManager] Generating AAA world features...");
        _world = WorldGenerator.CreateWorld(MapWidth, MapHeight, Seed);

        GenerateHDTextures();
        
        GD.Print($"[MapManager] World generated and textures baked!");
        QueueRedraw();
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
