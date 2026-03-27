using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Warship.Data;

namespace Warship.World;

/// <summary>
/// Procedural world generator. Creates a 600x360 fantasy world where nations
/// emerge from geography. Spawns armies (not individual units), assigns
/// city-centric territory, and pre-computes border polylines.
/// </summary>
public static class WorldGenerator
{
    public const int NumNations = 6;         // 5 AI + 1 player
    public const int CitiesPerNation = 5;    // Capital + 4 cities
    public const int ArmiesPerNation = 5;    // Starting armies

    // Nation colors — distinct and readable
    private static readonly Color[] NationColors =
    {
        new(0.25f, 0.50f, 0.85f), // Blue
        new(0.85f, 0.25f, 0.20f), // Red
        new(0.20f, 0.72f, 0.35f), // Green
        new(0.80f, 0.65f, 0.15f), // Gold
        new(0.65f, 0.30f, 0.75f), // Purple
        new(0.90f, 0.55f, 0.15f), // Orange (player)
    };

    private static readonly string[] NationPrefixes =
        { "Republic of", "Kingdom of", "Federation of", "Dominion of", "Union of",
          "Free State of", "Commonwealth of", "Empire of", "Principality of", "Confederacy of" };

    private static readonly string[] NationRoots =
        { "Valdria", "Korenth", "Ashenmoor", "Thalassia", "Drakmere", "Ironveil",
          "Solheim", "Blackhollow", "Windreach", "Stormvane", "Graymarch", "Cedarfall",
          "Frostgate", "Harrowfield", "Dunmere", "Silverbrook", "Thornwall", "Ravenport",
          "Highcross", "Deepwell" };

    private static readonly string[] CityPrefixes =
        { "North", "South", "East", "West", "Old", "New", "Upper", "Lower",
          "Iron", "Storm", "Stone", "River", "Lake", "Sea", "Dark", "Bright",
          "White", "Black", "Red", "Green", "Silver", "Gold", "Frost", "Sun" };

    private static readonly string[] CitySuffixes =
        { "Haven", "Crossing", "Landing", "Falls", "Bridge", "Port", "Gate",
          "Watch", "Hold", "Keep", "Ford", "Hollow", "Ridge", "Spire", "Crest",
          "Bay", "Point", "Forge", "Mill", "Tower", "Hearth", "Glen", "Harbor" };

    private static readonly string[] FirstNames =
        { "James", "Eleanor", "Viktor", "Mei", "Aleksandr", "Sofia", "Henrik",
          "Yara", "Nikolai", "Celeste", "Otto", "Ingrid", "Rajan", "Tomas",
          "Freya", "Kazimir", "Lena", "Darius", "Maren", "Cyrus" };

    private static readonly string[] LastNames =
        { "Ashford", "Volkov", "Thornton", "Weiss", "Okafor", "Strand",
          "Reeves", "Kazakov", "Lindqvist", "Moreno", "Harker", "Dietrich",
          "Kwan", "Novak", "Sterling", "Vasquez", "Crane", "Nakamura", "Blackwell" };

    private static readonly string[] ArmyNames =
        { "1st Army", "2nd Army", "3rd Army", "Iron Guard", "Northern Command",
          "Southern Command", "Eastern Front", "Western Front", "Royal Guard",
          "Expeditionary Force", "Home Defense", "Strike Group", "Coastal Fleet",
          "Deep Fleet", "Air Wing" };

    public static WorldData CreateWorld(int seed, string playerName = "J. Crawford",
        string playerRole = "Defense Minister", int focusIndex = 0)
    {
        var rng = new Random(seed);
        int mapW = TerrainGenerator.DefaultWidth;
        int mapH = TerrainGenerator.DefaultHeight;

        // ═══ Generate terrain + rivers ═══
        var (terrain, riverPaths) = TerrainGenerator.GenerateWorld(mapW, mapH, seed);

        // ═══ Find city locations ═══
        int totalCities = NumNations * CitiesPerNation + 15;
        var citySpots = TerrainGenerator.FindCityLocations(terrain, riverPaths, mapW, mapH, seed, totalCities);

        var world = new WorldData
        {
            Seed = seed,
            MapWidth = mapW,
            MapHeight = mapH,
            TerrainMap = terrain,
            OwnershipMap = new int[mapW, mapH],
            CityOwnershipMap = new int[mapW, mapH],
        };

        // Init maps to unclaimed
        for (int x = 0; x < mapW; x++)
            for (int y = 0; y < mapH; y++)
            {
                world.OwnershipMap[x, y] = -1;
                world.CityOwnershipMap[x, y] = -1;
            }

        // ═══ Create nations ═══
        int nationCount = Math.Min(NumNations, citySpots.Count);
        var capitalSpots = citySpots.Take(nationCount).ToList();
        var remainingSpots = citySpots.Skip(nationCount).ToList();
        int playerIndex = nationCount - 1; // Player = last (underdog)

        int nameIdx = rng.Next(NationRoots.Length);
        for (int n = 0; n < nationCount; n++)
        {
            bool isPlayer = (n == playerIndex);
            string root = NationRoots[(nameIdx + n) % NationRoots.Length];
            string prefix = NationPrefixes[rng.Next(NationPrefixes.Length)];

            world.Nations.Add(new NationData
            {
                Id = $"N_{n}",
                Name = isPlayer ? $"Free State of {root}" : $"{prefix} {root}",
                Archetype = NationArchetype.FreeState,
                NationColor = NationColors[n % NationColors.Length],
                IsPlayer = isPlayer,
                CapitalX = capitalSpots[n].x,
                CapitalY = capitalSpots[n].y,
                Treasury = isPlayer ? 800f : 1500f + rng.Next(2000),
                Prestige = isPlayer ? 30f : 40f + rng.Next(50),
            });
        }

        if (nationCount > 0)
            world.PlayerNationId = $"N_{playerIndex}";

        // ═══ Create cities (capital + secondary) ═══
        int cityIdx = 0;
        for (int n = 0; n < nationCount; n++)
        {
            var nation = world.Nations[n];
            string root = NationRoots[(nameIdx + n) % NationRoots.Length];

            // Capital
            world.Cities.Add(new CityData
            {
                Id = $"C_{cityIdx}",
                NationId = nation.Id,
                Name = root,
                TileX = capitalSpots[n].x,
                TileY = capitalSpots[n].y,
                IsCapital = true,
                Size = 3,
                HP = 400,
                CityIndex = cityIdx,
            });
            cityIdx++;
        }

        // Assign remaining cities to nearest nation
        var citiesPerNation = new int[nationCount];
        foreach (var spot in remainingSpots)
        {
            int nearestNation = -1;
            float nearestDist = float.MaxValue;
            for (int n = 0; n < nationCount; n++)
            {
                if (citiesPerNation[n] >= CitiesPerNation - 1) continue;
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

            string cityName = $"{CityPrefixes[rng.Next(CityPrefixes.Length)]} {CitySuffixes[rng.Next(CitySuffixes.Length)]}";
            int size = spot.quality > 5f ? 2 : 1;
            world.Cities.Add(new CityData
            {
                Id = $"C_{cityIdx}",
                NationId = $"N_{nearestNation}",
                Name = cityName,
                TileX = spot.x,
                TileY = spot.y,
                IsCapital = false,
                Size = size,
                HP = size == 2 ? 200 : 100,
                CityIndex = cityIdx,
            });
            citiesPerNation[nearestNation]++;
            cityIdx++;
        }

        // ═══ City-centric territory assignment ═══
        AssignCityTerritory(world, terrain, mapW, mapH);

        // ═══ Derive nation ownership from city ownership ═══
        DeriveNationOwnership(world, mapW, mapH);

        // ═══ Pre-compute border polylines ═══
        ComputeNationBorders(world, mapW, mapH);

        // ═══ Analyze geography for nation traits ═══
        for (int n = 0; n < nationCount; n++)
        {
            var nation = world.Nations[n];
            var profile = AnalyzeGeography(world, terrain, n, mapW, mapH);
            nation.Archetype = profile.archetype;
            nation.ProvinceCount = profile.tileCount;
            if (!nation.IsPlayer)
            {
                nation.Treasury = profile.baseTreasury;
                nation.Prestige = profile.basePrestige;
            }
        }

        // ═══ Build river paths for rendering ═══
        world.RiverPaths = new List<Vector2[]>();
        foreach (var rp in riverPaths)
        {
            var points = new List<Vector2>();
            for (int i = 0; i < rp.Length - 1; i += 2)
                points.Add(new Vector2(rp[i], rp[i + 1]));
            if (points.Count >= 2)
                world.RiverPaths.Add(points.ToArray());
        }

        // ═══ Spawn armies ═══
        for (int n = 0; n < nationCount; n++)
        {
            var nation = world.Nations[n];
            var nationCities = world.Cities.Where(c => c.NationId == nation.Id).ToList();
            bool isPlayer = nation.IsPlayer;
            int armyCount = isPlayer ? 3 : ArmiesPerNation;

            for (int a = 0; a < armyCount && a < nationCities.Count; a++)
            {
                var city = nationCities[a % nationCities.Count];
                bool isCoastal = IsNearWater(terrain, city.TileX, city.TileY, mapW, mapH);
                string armyName = a < ArmyNames.Length ? ArmyNames[a] : $"Army {a + 1}";

                var army = new ArmyData
                {
                    Id = $"{nation.Id}_A_{world.Armies.Count}",
                    NationId = nation.Id,
                    Name = armyName,
                    TileX = city.TileX,
                    TileY = city.TileY,
                    TargetTileX = city.TileX,
                    TargetTileY = city.TileY,
                    PixelX = city.TileX * MapManagerConstants.TileSize + MapManagerConstants.TileSize / 2f,
                    PixelY = city.TileY * MapManagerConstants.TileSize + MapManagerConstants.TileSize / 2f,
                    CurrentOrder = city.IsCapital ? MilitaryOrder.BorderWatch : MilitaryOrder.Standby,
                    Formation = city.IsCapital ? FormationType.Circle : FormationType.Spread,
                };
                army.TargetPixelX = army.PixelX;
                army.TargetPixelY = army.PixelY;

                // Composition varies by nation strength and position
                if (isPlayer)
                {
                    // Player starts small
                    army.Composition[UnitType.Infantry] = 200 + rng.Next(100);
                    army.Composition[UnitType.Tank] = 20 + rng.Next(20);
                    army.Composition[UnitType.Artillery] = 10 + rng.Next(10);
                    if (a == 0) army.Composition[UnitType.AntiAir] = 10;
                }
                else
                {
                    // AI nations are bigger
                    army.Composition[UnitType.Infantry] = 500 + rng.Next(500);
                    army.Composition[UnitType.Tank] = 50 + rng.Next(80);
                    army.Composition[UnitType.Artillery] = 20 + rng.Next(30);
                    army.Composition[UnitType.AntiAir] = 10 + rng.Next(20);
                    if (a == 0) army.Composition[UnitType.Fighter] = 24 + rng.Next(24);
                }

                // Coastal cities get naval units
                if (isCoastal && a > 0)
                {
                    army.Composition[UnitType.Destroyer] = 4 + rng.Next(8);
                    if (!isPlayer) army.Composition[UnitType.Carrier] = rng.Next(2);
                    army.Composition[UnitType.Submarine] = 2 + rng.Next(4);
                }

                // One army per nation gets a garrison assignment
                if (a == 0)
                    army.GarrisonCityId = city.Id;

                world.Armies.Add(army);
            }
        }

        // ═══ Spawn characters ═══
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
                    TileX = capital.TileX + (i % 3),
                    TileY = capital.TileY + (i / 3),
                    PixelX = px, PixelY = py,
                    TargetPixelX = px, TargetPixelY = py,
                    TerritoryAuthority = ta,
                    WorldAuthority = wa,
                    BehindTheScenesAuthority = bsa,
                });
            }
        }

        GD.Print($"[WorldGen] {nationCount} nations, {world.Cities.Count} cities, {world.Armies.Count} armies, {world.RiverPaths.Count} rivers");
        return world;
    }

    // ═══ City-centric territory — each city controls a radius ═══
    private static void AssignCityTerritory(WorldData world, int[,] terrain, int w, int h)
    {
        var dist = new float[w, h];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                dist[x, y] = float.MaxValue;

        var queue = new Queue<(int x, int y, int cityIdx)>();
        foreach (var city in world.Cities)
        {
            queue.Enqueue((city.TileX, city.TileY, city.CityIndex));
            dist[city.TileX, city.TileY] = 0;
            world.CityOwnershipMap![city.TileX, city.TileY] = city.CityIndex;
        }

        int[] dxs = { -1, 1, 0, 0 };
        int[] dys = { 0, 0, -1, 1 };

        while (queue.Count > 0)
        {
            var (cx, cy, cidx) = queue.Dequeue();
            float cd = dist[cx, cy];
            var city = world.Cities[cidx];

            // Don't expand beyond city's control radius
            if (cd >= city.ControlRadius) continue;

            for (int d = 0; d < 4; d++)
            {
                int nx = cx + dxs[d], ny = cy + dys[d];
                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;

                int t = terrain[nx, ny];
                if (t <= (int)TerrainGenerator.Terrain.Water) continue;

                float cost = t switch
                {
                    (int)TerrainGenerator.Terrain.Mountain => 4f,
                    (int)TerrainGenerator.Terrain.Hills => 2f,
                    (int)TerrainGenerator.Terrain.Forest => 1.5f,
                    (int)TerrainGenerator.Terrain.Snow => 2.5f,
                    _ => 1f
                };

                float nd = cd + cost;
                if (nd < dist[nx, ny])
                {
                    dist[nx, ny] = nd;
                    world.CityOwnershipMap![nx, ny] = cidx;
                    queue.Enqueue((nx, ny, cidx));
                }
            }
        }
    }

    // ═══ Nation ownership derived from city ownership ═══
    private static void DeriveNationOwnership(WorldData world, int w, int h)
    {
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                int cidx = world.CityOwnershipMap![x, y];
                if (cidx < 0 || cidx >= world.Cities.Count)
                {
                    world.OwnershipMap![x, y] = -1;
                    continue;
                }
                var city = world.Cities[cidx];
                int nIdx = int.Parse(city.NationId.Split('_')[1]);
                world.OwnershipMap![x, y] = nIdx;
            }
        }

        // Update province counts
        foreach (var nation in world.Nations)
        {
            int nIdx = int.Parse(nation.Id.Split('_')[1]);
            int count = 0;
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    if (world.OwnershipMap![x, y] == nIdx) count++;
            nation.ProvinceCount = count;
        }
    }

    // ═══ Pre-compute nation border segments as polylines ═══
    public static void ComputeNationBorders(WorldData world, int w, int h)
    {
        world.NationBorderLines.Clear();

        for (int nIdx = 0; nIdx < world.Nations.Count; nIdx++)
        {
            var segments = new List<Vector2[]>();

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (world.OwnershipMap![x, y] != nIdx) continue;

                    float px = x * MapManagerConstants.TileSize;
                    float py = y * MapManagerConstants.TileSize;
                    int ts = MapManagerConstants.TileSize;

                    // Check each edge — if neighbor is different, record border segment
                    if (y == 0 || world.OwnershipMap[x, y - 1] != nIdx)
                        segments.Add(new[] { new Vector2(px, py), new Vector2(px + ts, py) });
                    if (y == h - 1 || world.OwnershipMap[x, y + 1] != nIdx)
                        segments.Add(new[] { new Vector2(px, py + ts), new Vector2(px + ts, py + ts) });
                    if (x == 0 || world.OwnershipMap[x - 1, y] != nIdx)
                        segments.Add(new[] { new Vector2(px, py), new Vector2(px, py + ts) });
                    if (x == w - 1 || world.OwnershipMap[x + 1, y] != nIdx)
                        segments.Add(new[] { new Vector2(px + ts, py), new Vector2(px + ts, py + ts) });
                }
            }

            world.NationBorderLines[nIdx] = segments;
        }
    }

    // ═══ Geography analysis ═══
    private static (NationArchetype archetype, int tileCount, float baseTreasury, float basePrestige)
        AnalyzeGeography(WorldData world, int[,] terrain, int nationIdx, int w, int h)
    {
        int tileCount = 0, coastalTiles = 0, mountainTiles = 0, forestTiles = 0, grassTiles = 0;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (world.OwnershipMap![x, y] != nationIdx) continue;
                tileCount++;

                int t = terrain[x, y];
                if (t == (int)TerrainGenerator.Terrain.Mountain) mountainTiles++;
                if (t == (int)TerrainGenerator.Terrain.Forest) forestTiles++;
                if (t == (int)TerrainGenerator.Terrain.Grass) grassTiles++;

                if (x > 0 && terrain[x - 1, y] <= 1 ||
                    x < w - 1 && terrain[x + 1, y] <= 1 ||
                    y > 0 && terrain[x, y - 1] <= 1 ||
                    y < h - 1 && terrain[x, y + 1] <= 1)
                    coastalTiles++;
            }
        }

        if (tileCount == 0) tileCount = 1;
        float coastRatio = coastalTiles / (float)tileCount;
        float mountRatio = mountainTiles / (float)tileCount;
        float grassRatio = grassTiles / (float)tileCount;

        NationArchetype archetype;
        float treasury, prestige;

        if (coastRatio > 0.12f)
        {
            archetype = NationArchetype.Commercial;
            treasury = 3500f + coastalTiles * 10f;
            prestige = 60f;
        }
        else if (mountRatio > 0.10f)
        {
            archetype = NationArchetype.Traditionalist;
            treasury = 2500f;
            prestige = 50f + mountainTiles * 2f;
        }
        else if (grassRatio > 0.35f && tileCount > 2000)
        {
            archetype = NationArchetype.Hegemon;
            treasury = 4000f + tileCount;
            prestige = 80f;
        }
        else if (tileCount < 1500)
        {
            archetype = NationArchetype.Survival;
            treasury = 1200f;
            prestige = 25f;
        }
        else
        {
            archetype = NationArchetype.Revolutionary;
            treasury = 2000f + forestTiles * 3f;
            prestige = 40f;
        }

        return (archetype, tileCount, treasury, prestige);
    }

    private static bool IsNearWater(int[,] terrain, int x, int y, int w, int h)
    {
        for (int dx = -4; dx <= 4; dx++)
            for (int dy = -4; dy <= 4; dy++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx >= 0 && nx < w && ny >= 0 && ny < h && terrain[nx, ny] <= 1)
                    return true;
            }
        return false;
    }
}

/// <summary>Shared tile size constant.</summary>
public static class MapManagerConstants
{
    public const int TileSize = 32; // 32px tiles for the 600x360 world
}
