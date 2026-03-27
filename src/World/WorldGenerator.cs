using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Warship.Data;

namespace Warship.World;

/// <summary>
/// Procedural world generator. Creates a fantasy world where nations
/// emerge from geography — no predefined blocs or archetypes.
/// Geography determines national character: coastal nations trade,
/// mountain nations fortify, plains nations expand, island nations scheme.
/// </summary>
public static class WorldGenerator
{
    public const int NumNations = 6; // 5 AI + 1 player
    public const int CitiesPerNation = 4; // Capital + 3 cities
    public const int UnitsPerBase = 2;

    // Nation colors — distinct and readable on pixel map
    private static readonly Color[] NationColors = new Color[]
    {
        new Color(0.25f, 0.50f, 0.85f), // Blue
        new Color(0.85f, 0.25f, 0.20f), // Red
        new Color(0.20f, 0.72f, 0.35f), // Green
        new Color(0.80f, 0.65f, 0.15f), // Gold
        new Color(0.65f, 0.30f, 0.75f), // Purple
        new Color(0.90f, 0.55f, 0.15f), // Orange (player)
    };

    // Name pools for procedural nation naming
    private static readonly string[] NationPrefixes =
        { "Republic of", "Kingdom of", "Federation of", "Dominion of", "Union of", "Free State of",
          "Commonwealth of", "Empire of", "Principality of", "Confederacy of" };

    private static readonly string[] NationRoots =
        { "Valdria", "Korenth", "Ashenmoor", "Thalassia", "Drakmere", "Ironveil",
          "Solheim", "Blackhollow", "Windreach", "Stormvane", "Graymarch", "Cedarfall",
          "Frostgate", "Harrowfield", "Dunmere", "Silverbrook", "Thornwall", "Ravenport",
          "Highcross", "Deepwell" };

    private static readonly string[] CityNames =
        { "Haven", "Crossing", "Landing", "Falls", "Bridge", "Port", "Gate",
          "Watch", "Hold", "Keep", "Ford", "Hollow", "Ridge", "Spire", "Crest",
          "Basin", "Reach", "Bay", "Point", "Marsh", "Dell", "Bluff", "Harbor",
          "Forge", "Mill", "Tower", "Hearth", "Glen", "Mound", "Quarry" };

    private static readonly string[] CityPrefixes =
        { "North", "South", "East", "West", "Old", "New", "Upper", "Lower",
          "Iron", "Storm", "Stone", "River", "Lake", "Sea", "Dark", "Bright",
          "White", "Black", "Red", "Green", "Silver", "Gold", "Frost", "Sun" };

    private static readonly string[] FirstNames =
        { "James", "Eleanor", "Viktor", "Mei", "Aleksandr", "Sofia", "Henrik",
          "Yara", "Nikolai", "Celeste", "Otto", "Ingrid", "Rajan", "Tomás",
          "Freya", "Kazimir", "Lena", "Darius", "Maren", "Cyrus" };

    private static readonly string[] LastNames =
        { "Ashford", "Volkov", "Thornton", "Weiss", "Okafor", "Strand",
          "Reeves", "Kazakov", "Lindqvist", "Moreno", "Harker", "Dietrich",
          "Kwan", "Novak", "Sterling", "Vasquez", "Crane", "Bergström",
          "Nakamura", "Blackwell" };

    public static WorldData CreateWorld(int seed, string playerName = "J. Crawford", string playerRole = "Defense Minister", int focusIndex = 0)
    {
        var rng = new Random(seed);

        int mapW = TerrainGenerator.DefaultWidth;
        int mapH = TerrainGenerator.DefaultHeight;

        // ═══ Generate terrain + rivers ═══
        var (terrain, riverPaths) = TerrainGenerator.GenerateWorld(mapW, mapH, seed);

        // ═══ Find good city locations ═══
        int totalCities = NumNations * CitiesPerNation + 10; // Extra buffer
        var citySpots = TerrainGenerator.FindCityLocations(terrain, riverPaths, mapW, mapH, seed, totalCities);

        // ═══ Assign cities to nations via flood-fill from capitals ═══
        // First N spots become capitals (they're the highest quality)
        if (citySpots.Count < NumNations)
        {
            GD.Print("[WorldGen] Warning: Not enough city locations, reducing nations");
        }

        var world = new WorldData
        {
            Seed = seed,
            MapWidth = mapW,
            MapHeight = mapH,
            TerrainMap = terrain,
            OwnershipMap = new int[mapW, mapH],
        };

        // Init ownership to unclaimed
        for (int x = 0; x < mapW; x++)
            for (int y = 0; y < mapH; y++)
                world.OwnershipMap[x, y] = -1;

        // ═══ Create nations at best city spots ═══
        int nationCount = Math.Min(NumNations, citySpots.Count);
        var capitalSpots = citySpots.Take(nationCount).ToList();
        var remainingSpots = citySpots.Skip(nationCount).ToList();

        // Player is the last nation (smallest/most isolated — the underdog)
        int playerIndex = nationCount - 1;

        int nameIdx = rng.Next(NationRoots.Length);
        for (int n = 0; n < nationCount; n++)
        {
            bool isPlayer = (n == playerIndex);
            string root = NationRoots[(nameIdx + n) % NationRoots.Length];
            string prefix = NationPrefixes[rng.Next(NationPrefixes.Length)];
            string nationName = isPlayer ? $"Free State of {root}" : $"{prefix} {root}";

            var nation = new NationData
            {
                Id = $"N_{n}",
                Name = nationName,
                Archetype = NationArchetype.FreeState, // Will be overwritten by geography analysis
                NationColor = NationColors[n % NationColors.Length],
                IsPlayer = isPlayer,
                CapitalX = capitalSpots[n].x,
                CapitalY = capitalSpots[n].y,
                // No lon/lat — this is a fantasy world, tile coords are primary
                CapitalLon = 0, CapitalLat = 0,
                Treasury = isPlayer ? 800f : 1500f + rng.Next(2000),
                Prestige = isPlayer ? 30f : 40f + rng.Next(50),
            };

            world.Nations.Add(nation);

            // Capital city
            world.Cities.Add(new CityData
            {
                Id = $"C_{world.Cities.Count}",
                NationId = nation.Id,
                Name = root, // Capital shares nation name
                TileX = capitalSpots[n].x,
                TileY = capitalSpots[n].y,
                IsCapital = true,
                Size = 3,
            });
        }

        if (nationCount > 0)
            world.PlayerNationId = $"N_{playerIndex}";

        // ═══ Assign remaining cities to nearest nation ═══
        int citiesAssigned = 0;
        var citiesPerNation = new int[nationCount]; // Track how many each nation has

        foreach (var spot in remainingSpots)
        {
            // Find nearest nation capital
            int nearestNation = -1;
            float nearestDist = float.MaxValue;
            for (int n = 0; n < nationCount; n++)
            {
                if (citiesPerNation[n] >= CitiesPerNation - 1) continue; // Cap secondary cities
                float dx = spot.x - capitalSpots[n].x;
                float dy = spot.y - capitalSpots[n].y;
                float dist = dx * dx + dy * dy;
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestNation = n;
                }
            }
            if (nearestNation < 0) continue;

            string cityName = $"{CityPrefixes[rng.Next(CityPrefixes.Length)]} {CityNames[rng.Next(CityNames.Length)]}";
            world.Cities.Add(new CityData
            {
                Id = $"C_{world.Cities.Count}",
                NationId = $"N_{nearestNation}",
                Name = cityName,
                TileX = spot.x,
                TileY = spot.y,
                IsCapital = false,
                Size = spot.quality > 5f ? 2 : 1,
            });

            citiesPerNation[nearestNation]++;
            citiesAssigned++;
        }

        // ═══ Territory assignment via flood-fill from all cities ═══
        AssignTerritory(world, terrain, mapW, mapH);

        // ═══ Analyze geography to set nation traits ═══
        for (int n = 0; n < nationCount; n++)
        {
            var nation = world.Nations[n];
            var profile = AnalyzeGeography(world, terrain, riverPaths, n, mapW, mapH);
            nation.Archetype = profile.archetype;
            nation.ProvinceCount = profile.tileCount;

            // Adjust starting stats based on geography
            if (!nation.IsPlayer)
            {
                nation.Treasury = profile.baseTreasury;
                nation.Prestige = profile.basePrestige;
            }
        }

        // ═══ Build river Vector2 paths for rendering ═══
        world.RiverPaths = new List<Vector2[]>();
        foreach (var rp in riverPaths)
        {
            var points = new List<Vector2>();
            for (int i = 0; i < rp.Length - 1; i += 2)
                points.Add(new Vector2(rp[i], rp[i + 1]));
            if (points.Count >= 2)
                world.RiverPaths.Add(points.ToArray());
        }

        // ═══ Spawn military units at cities ═══
        foreach (var city in world.Cities)
        {
            int unitCount = city.IsCapital ? 3 : UnitsPerBase;
            for (int u = 0; u < unitCount; u++)
            {
                float ox = (float)(rng.NextDouble() - 0.5) * 3f;
                float oy = (float)(rng.NextDouble() - 0.5) * 3f;
                float px = city.TileX * MapManagerConstants.TileSize + MapManagerConstants.TileSize / 2f + ox * MapManagerConstants.TileSize * 0.3f;
                float py = city.TileY * MapManagerConstants.TileSize + MapManagerConstants.TileSize / 2f + oy * MapManagerConstants.TileSize * 0.3f;

                bool isCoastal = IsNearWater(terrain, city.TileX, city.TileY, mapW, mapH);
                var unitType = (isCoastal && u == unitCount - 1) ? UnitType.Ship : UnitType.Tank;

                world.Units.Add(new UnitData
                {
                    Id = $"{city.NationId}_U_{world.Units.Count}",
                    NationId = city.NationId,
                    Type = unitType,
                    TileX = city.TileX + (int)ox,
                    TileY = city.TileY + (int)oy,
                    PixelX = px, PixelY = py,
                    TargetPixelX = px, TargetPixelY = py,
                    CurrentOrder = city.IsCapital ? MilitaryOrder.BorderWatch : MilitaryOrder.Standby,
                });
            }
        }

        // ═══ Spawn characters (VIPs) ═══
        string[] roles = { "Head of State", "Defense Minister", "Foreign Minister",
                           "Director of Intelligence", "Chief of Staff",
                           "Finance Minister", "Interior Minister", "Opposition Leader" };

        for (int n = 0; n < nationCount; n++)
        {
            var nation = world.Nations[n];
            var capital = world.Cities.First(c => c.NationId == nation.Id && c.IsCapital);

            for (int i = 0; i < roles.Length; i++)
            {
                string role = roles[i];
                bool isPlayer = nation.IsPlayer && role == playerRole;
                string cName = isPlayer ? playerName :
                    $"{FirstNames[rng.Next(FirstNames.Length)]} {LastNames[rng.Next(LastNames.Length)]}";

                float ta = 30f, wa = 20f, bsa = 40f;
                if (role == "Head of State") { ta = 80; wa = 60; bsa = 70; }
                else if (role == "Defense Minister") { ta = 40; wa = 20; bsa = 60; }
                else if (role == "Foreign Minister") { ta = 15; wa = 80; bsa = 30; }
                else if (role == "Director of Intelligence") { ta = 30; wa = 40; bsa = 95; }
                else if (role == "Opposition Leader") { ta = 50; wa = 10; bsa = 30; }

                if (isPlayer)
                {
                    if (focusIndex == 1) ta += 20;
                    if (focusIndex == 2) wa += 20;
                    if (focusIndex == 3) bsa += 20;
                    ta = Math.Clamp(ta, 0, 100);
                    wa = Math.Clamp(wa, 0, 100);
                    bsa = Math.Clamp(bsa, 0, 100);
                }

                float px = capital.TileX * MapManagerConstants.TileSize + MapManagerConstants.TileSize / 2f;
                float py = capital.TileY * MapManagerConstants.TileSize + MapManagerConstants.TileSize / 2f;

                world.Characters.Add(new CharacterData
                {
                    Id = $"{nation.Id}_Char_{i + 1}",
                    NationId = nation.Id,
                    Name = cName,
                    Role = role,
                    IsPlayer = isPlayer,
                    TileX = capital.TileX + (i % 2),
                    TileY = capital.TileY + (i / 2),
                    PixelX = px, PixelY = py,
                    TargetPixelX = px, TargetPixelY = py,
                    TerritoryAuthority = ta,
                    WorldAuthority = wa,
                    BehindTheScenesAuthority = bsa,
                });
            }
        }

        GD.Print($"[WorldGen] Created world: {nationCount} nations, {world.Cities.Count} cities, {world.Units.Count} units, {world.RiverPaths.Count} rivers");
        return world;
    }

    // ═══ Territory flood-fill from cities ═══
    private static void AssignTerritory(WorldData world, int[,] terrain, int w, int h)
    {
        // BFS from each city simultaneously — each tile goes to the nearest city's nation
        var dist = new float[w, h];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                dist[x, y] = float.MaxValue;

        var queue = new Queue<(int x, int y, int nationIdx)>();
        foreach (var city in world.Cities)
        {
            int nIdx = int.Parse(city.NationId.Split('_')[1]);
            queue.Enqueue((city.TileX, city.TileY, nIdx));
            dist[city.TileX, city.TileY] = 0;
            world.OwnershipMap[city.TileX, city.TileY] = nIdx;
        }

        int[] dxs = { -1, 1, 0, 0 };
        int[] dys = { 0, 0, -1, 1 };

        while (queue.Count > 0)
        {
            var (cx, cy, nIdx) = queue.Dequeue();
            float cd = dist[cx, cy];

            for (int d = 0; d < 4; d++)
            {
                int nx = cx + dxs[d], ny = cy + dys[d];
                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;

                int t = terrain[nx, ny];
                if (t <= (int)TerrainGenerator.Terrain.Water) continue; // Don't claim water

                // Movement cost varies by terrain
                float cost = t switch
                {
                    (int)TerrainGenerator.Terrain.Mountain => 4f,
                    (int)TerrainGenerator.Terrain.Hills => 2f,
                    (int)TerrainGenerator.Terrain.Forest => 1.5f,
                    _ => 1f
                };

                float nd = cd + cost;
                if (nd < dist[nx, ny])
                {
                    dist[nx, ny] = nd;
                    world.OwnershipMap[nx, ny] = nIdx;
                    queue.Enqueue((nx, ny, nIdx));
                }
            }
        }
    }

    // ═══ Geography analysis — determines nation character ═══
    private static (NationArchetype archetype, int tileCount, float baseTreasury, float basePrestige)
        AnalyzeGeography(WorldData world, int[,] terrain, List<int[]> rivers, int nationIdx, int w, int h)
    {
        int tileCount = 0;
        int coastalTiles = 0;
        int mountainTiles = 0;
        int forestTiles = 0;
        int grassTiles = 0;
        int riverTiles = 0;

        // Count territory composition
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (world.OwnershipMap[x, y] != nationIdx) continue;
                tileCount++;

                int t = terrain[x, y];
                if (t == (int)TerrainGenerator.Terrain.Mountain) mountainTiles++;
                if (t == (int)TerrainGenerator.Terrain.Forest) forestTiles++;
                if (t == (int)TerrainGenerator.Terrain.Grass) grassTiles++;

                // Check coastal
                bool nearWater = false;
                if (x > 0 && terrain[x - 1, y] <= 1) nearWater = true;
                if (x < w - 1 && terrain[x + 1, y] <= 1) nearWater = true;
                if (y > 0 && terrain[x, y - 1] <= 1) nearWater = true;
                if (y < h - 1 && terrain[x, y + 1] <= 1) nearWater = true;
                if (nearWater) coastalTiles++;
            }
        }

        // River influence
        var riverSet = new HashSet<long>();
        foreach (var rp in rivers)
            for (int i = 0; i < rp.Length - 1; i += 2)
                riverSet.Add((long)rp[i] * h + rp[i + 1]);

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if (world.OwnershipMap[x, y] == nationIdx && riverSet.Contains((long)x * h + y))
                    riverTiles++;

        // Determine archetype from geography ratios
        if (tileCount == 0) tileCount = 1;
        float coastRatio = coastalTiles / (float)tileCount;
        float mountRatio = mountainTiles / (float)tileCount;
        float grassRatio = grassTiles / (float)tileCount;

        NationArchetype archetype;
        float treasury, prestige;

        if (coastRatio > 0.15f && riverTiles > 3)
        {
            archetype = NationArchetype.Commercial; // Trade power
            treasury = 3500f + coastalTiles * 30f;
            prestige = 60f;
        }
        else if (mountRatio > 0.12f)
        {
            archetype = NationArchetype.Traditionalist; // Fortress nation
            treasury = 2500f;
            prestige = 50f + mountainTiles * 5f;
        }
        else if (grassRatio > 0.4f && tileCount > 300)
        {
            archetype = NationArchetype.Hegemon; // Wide open, expansionist
            treasury = 4000f + tileCount * 5f;
            prestige = 80f;
        }
        else if (tileCount < 200)
        {
            archetype = NationArchetype.Survival; // Small, desperate
            treasury = 1200f;
            prestige = 25f;
        }
        else
        {
            archetype = NationArchetype.Revolutionary; // Ideologically driven
            treasury = 2000f + forestTiles * 10f;
            prestige = 40f;
        }

        return (archetype, tileCount, treasury, prestige);
    }

    private static bool IsNearWater(int[,] terrain, int x, int y, int w, int h)
    {
        for (int dx = -3; dx <= 3; dx++)
            for (int dy = -3; dy <= 3; dy++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx >= 0 && nx < w && ny >= 0 && ny < h && terrain[nx, ny] <= 1)
                    return true;
            }
        return false;
    }
}

/// <summary>
/// Shared constants for the tile-based map system.
/// </summary>
public static class MapManagerConstants
{
    public const int TileSize = 64;
}
