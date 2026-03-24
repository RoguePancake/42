using System;
using System.Collections.Generic;
using Godot;
using Warship.Data;

namespace Warship.World;

/// <summary>
/// WorldGenerator — Creates the game world using real Earth geography.
/// 
/// Instead of procedural terrain, nations are placed at real coordinates
/// using GeoData definitions. The map tiles from OpenStreetMap provide
/// the visual terrain — this generator only handles game entities.
/// </summary>
public static class WorldGenerator
{
    public static WorldData CreateWorld(int seed, string playerNationName = "United Kingdom", string playerRole = "Defense Minister", string playerName = "J. Crawford", int focusIndex = 0)
    {
        var world = new WorldData
        {
            Seed = seed,
            // Map dimensions are no longer fixed tiles — the real world is infinite
            // These are kept for backward compat with any code that references them
            MapWidth = 360,    // Conceptual: degrees of longitude
            MapHeight = 180,   // Conceptual: degrees of latitude
        };

        var rng = new Random(seed);
        int nationIndex = 0;
        int cityIndex = 0;

        // ═══ Create nations from real-world data ═══
        foreach (var geo in GeoData.Nations)
        {
            var nation = new NationData
            {
                Id = $"N_{nationIndex}",
                Name = geo.Name,
                Archetype = geo.Archetype,
                NationColor = geo.NationColor,
                
                // Real-world coordinates
                CapitalLon = geo.CapitalLon,
                CapitalLat = geo.CapitalLat,
                BorderPolygon = geo.Border,
                
                // Legacy tile coords (approximate mapping for backward compat)
                CapitalX = (int)((geo.CapitalLon + 180f) / 360f * 80f),
                CapitalY = (int)((90f - geo.CapitalLat) / 180f * 50f),
                
                // Starting stats vary by archetype
                Treasury = GetStartingTreasury(geo.Archetype),
                Prestige = GetStartingPrestige(geo.Archetype),
                ProvinceCount = geo.Border.Length, // Approximate
            };

            world.Nations.Add(nation);

            // ═══ Capital city ═══
            world.Cities.Add(new CityData
            {
                Id = $"C_{cityIndex++}",
                NationId = nation.Id,
                Name = geo.CapitalName,
                Longitude = geo.CapitalLon,
                Latitude = geo.CapitalLat,
                TileX = nation.CapitalX,
                TileY = nation.CapitalY,
                IsCapital = true,
                Size = 3
            });

            // ═══ Other cities ═══
            foreach (var (name, lon, lat, size) in geo.Cities)
            {
                world.Cities.Add(new CityData
                {
                    Id = $"C_{cityIndex++}",
                    NationId = nation.Id,
                    Name = name,
                    Longitude = lon,
                    Latitude = lat,
                    TileX = (int)((lon + 180f) / 360f * 80f),
                    TileY = (int)((90f - lat) / 180f * 50f),
                    IsCapital = false,
                    Size = size
                });
            }

            // ═══ Spawn Military Units at bases ═══
            foreach (var (lon, lat) in geo.MilitaryBases)
            {
                // Tank group at each base
                for (int t = 0; t < 3; t++)
                {
                    float offsetLon = (float)(rng.NextDouble() - 0.5) * 0.5f;
                    float offsetLat = (float)(rng.NextDouble() - 0.5) * 0.5f;
                    
                    world.Units.Add(new UnitData
                    {
                        Id = $"{nation.Id}_Tank_{world.Units.Count}",
                        NationId = nation.Id,
                        Type = UnitType.Tank,
                        Longitude = lon + offsetLon,
                        Latitude = lat + offsetLat,
                        TargetLongitude = lon + offsetLon,
                        TargetLatitude = lat + offsetLat,
                        TileX = (int)((lon + 180f) / 360f * 80f),
                        TileY = (int)((90f - lat) / 180f * 50f),
                        PixelX = (int)((lon + 180f) / 360f * 80f) * 64 + 32,
                        PixelY = (int)((90f - lat) / 180f * 50f) * 64 + 32,
                        TargetPixelX = (int)((lon + 180f) / 360f * 80f) * 64 + 32,
                        TargetPixelY = (int)((90f - lat) / 180f * 50f) * 64 + 32,
                        CurrentOrder = MilitaryOrder.BorderWatch
                    });
                }

                // Ship at coastal bases
                if (IsCoastalBase(lon, lat))
                {
                    world.Units.Add(new UnitData
                    {
                        Id = $"{nation.Id}_Ship_{world.Units.Count}",
                        NationId = nation.Id,
                        Type = UnitType.Ship,
                        Longitude = lon + (float)(rng.NextDouble() - 0.5) * 1.0f,
                        Latitude = lat + (float)(rng.NextDouble() - 0.5) * 1.0f,
                        TargetLongitude = lon,
                        TargetLatitude = lat,
                        TileX = (int)((lon + 180f) / 360f * 80f),
                        TileY = (int)((90f - lat) / 180f * 50f),
                        CurrentOrder = MilitaryOrder.Patrol
                    });
                }
            }

            // ═══ Spawn Characters (VIPs) ═══
            string[] roles = { 
                "Head of State", 
                "Defense Minister", 
                "Foreign Minister", 
                "Director of Intelligence", 
                "Chief of Staff", 
                "Finance Minister", 
                "Interior Minister", 
                "Opposition Leader" 
            };
            
            bool isPlayerNation = geo.Name == playerNationName;
            if (isPlayerNation) world.PlayerNationId = nation.Id;

            for (int i = 0; i < roles.Length; i++)
            {
                string r = roles[i];
                bool isPlayer = isPlayerNation && r == playerRole;
                
                string cName = isPlayer ? playerName : (r == "Head of State" ? GetLeaderName(geo.Name, rng) : (r == "Defense Minister" ? GetGeneralName(geo.Name, rng) : GetRandomName(rng)));
                
                float ta = 30f;
                float wa = 20f;
                float bsa = 40f;
                
                if (r == "Head of State") { ta = 80; wa = 60; bsa = 70; }
                else if (r == "Defense Minister") { ta = 40; wa = 20; bsa = 60; }
                else if (r == "Foreign Minister") { ta = 15; wa = 80; bsa = 30; }
                else if (r == "Director of Intelligence") { ta = 30; wa = 40; bsa = 95; }
                else if (r == "Opposition Leader") { ta = 50; wa = 10; bsa = 30; }
                
                if (isPlayer)
                {
                    // Focus logic: "Balanced", "Territory Control (+TA)", "Global Influence (+WA)", "Shadow Broker (+BSA)"
                    if (focusIndex == 1) ta += 20;
                    if (focusIndex == 2) wa += 20;
                    if (focusIndex == 3) bsa += 20;
                    ta = Math.Clamp(ta, 0, 100);
                    wa = Math.Clamp(wa, 0, 100);
                    bsa = Math.Clamp(bsa, 0, 100);
                }

                world.Characters.Add(new CharacterData
                {
                    Id = $"{nation.Id}_Char_{i + 1}",
                    NationId = nation.Id,
                    Name = cName,
                    Role = r,
                    IsPlayer = isPlayer,
                    TileX = nation.CapitalX + (i % 2),
                    TileY = nation.CapitalY + (i / 2),
                    Longitude = geo.CapitalLon + (i * 0.05f),
                    Latitude = geo.CapitalLat + (i * 0.05f),
                    PixelX = (nation.CapitalX + (i%2)) * 64 + 32,
                    PixelY = (nation.CapitalY + (i/2)) * 64 + 32,
                    TargetPixelX = (nation.CapitalX + (i%2)) * 64 + 32,
                    TargetPixelY = (nation.CapitalY + (i/2)) * 64 + 32,
                    TerritoryAuthority = ta,
                    WorldAuthority = wa,
                    BehindTheScenesAuthority = bsa
                });
            }

            nationIndex++;
        }

        // ═══ Initialize ownership map for backward compat ═══
        // This is a simplified mapping — real borders are defined by BorderPolygon
        world.OwnershipMap = new int[80, 50];
        for (int x = 0; x < 80; x++)
            for (int y = 0; y < 50; y++)
                world.OwnershipMap[x, y] = -1;

        // Roughly fill ownership based on capital proximity
        for (int x = 0; x < 80; x++)
        {
            for (int y = 0; y < 50; y++)
            {
                float lon = x / 80f * 360f - 180f;
                float lat = 90f - y / 50f * 180f;
                
                int closest = -1;
                float closestDist = float.MaxValue;
                
                for (int n = 0; n < world.Nations.Count; n++)
                {
                    var nat = world.Nations[n];
                    float dx = lon - nat.CapitalLon;
                    float dy = lat - nat.CapitalLat;
                    float dist = dx * dx + dy * dy;
                    
                    if (dist < closestDist && dist < 2500f) // Limit territory radius
                    {
                        closestDist = dist;
                        closest = n;
                    }
                }
                
                world.OwnershipMap[x, y] = closest;
            }
        }

        // Generate terrain map for backward compat (simplified)
        world.TerrainMap = TerrainGenerator.Generate(80, 50, seed);

        // Empty river paths (real rivers come from map tiles now)
        world.RiverPaths = new List<Vector2[]>();

        return world;
    }

    // ── Helper Methods ──────────────────────────────

    private static float GetStartingTreasury(NationArchetype archetype) => archetype switch
    {
        NationArchetype.Hegemon => 5000f,
        NationArchetype.Commercial => 4500f,
        NationArchetype.Revolutionary => 2000f,
        NationArchetype.Traditionalist => 3500f,
        NationArchetype.Survival => 1500f,
        NationArchetype.FreeState => 800f,
        _ => 1000f
    };

    private static float GetStartingPrestige(NationArchetype archetype) => archetype switch
    {
        NationArchetype.Hegemon => 90f,
        NationArchetype.Commercial => 70f,
        NationArchetype.Revolutionary => 40f,
        NationArchetype.Traditionalist => 60f,
        NationArchetype.Survival => 25f,
        NationArchetype.FreeState => 30f,
        _ => 30f
    };

    private static bool IsCoastalBase(float lon, float lat)
    {
        // Rough check — bases near ocean edges
        // Pearl Harbor, Portsmouth, Plymouth, San Diego, Shanghai, Mumbai, etc.
        return Math.Abs(lat) < 50 && (
            Math.Abs(lon) > 100 || // Pacific edges
            lon < -70 ||           // US East coast
            (lon > -10 && lon < 5 && lat > 49) || // UK
            (lon > 70 && lon < 90 && lat < 25) ||  // India coast
            (lon > 110 && lon < 125 && lat < 35)   // China coast
        );
    }

    private static readonly string[] USLeaders = { "Harrison", "Mitchell", "Crawford", "Bennett", "Sullivan" };
    private static readonly string[] ChinaLeaders = { "Wei", "Zhang", "Liu", "Chen", "Yang" };
    private static readonly string[] RussiaLeaders = { "Volkov", "Petrov", "Kozlov", "Sokolov", "Morozov" };
    private static readonly string[] EULeaders = { "Müller", "Dubois", "Rossi", "García", "Van Berg" };
    private static readonly string[] IndiaLeaders = { "Sharma", "Patel", "Reddy", "Singh", "Kumar" };
    private static readonly string[] UKLeaders = { "Whitmore", "Ashford", "Pemberton", "Blackwell", "Stirling" };

    private static string GetLeaderName(string nationName, Random rng) => nationName switch
    {
        "United States" => "President " + USLeaders[rng.Next(USLeaders.Length)],
        "China" => "Chairman " + ChinaLeaders[rng.Next(ChinaLeaders.Length)],
        "Russia" => "President " + RussiaLeaders[rng.Next(RussiaLeaders.Length)],
        "European Union" => "Chancellor " + EULeaders[rng.Next(EULeaders.Length)],
        "India" => "Prime Minister " + IndiaLeaders[rng.Next(IndiaLeaders.Length)],
        "United Kingdom" => "PM " + UKLeaders[rng.Next(UKLeaders.Length)],
        _ => "Leader " + nationName
    };

    private static readonly string[] USGenerals = { "Bradley", "Marshall", "Hayes", "Thornton", "Reeves" };
    private static readonly string[] ChinaGenerals = { "Zhao", "Sun", "Wu", "Li", "Zhou" };
    private static readonly string[] RussiaGenerals = { "Ivanov", "Kuznetsov", "Orlov", "Karpov", "Vasiliev" };
    private static readonly string[] EUGenerals = { "Fischer", "Laurent", "Bianchi", "Moreno", "De Vries" };
    private static readonly string[] IndiaGenerals = { "Rao", "Nair", "Gupta", "Das", "Iyer" };
    private static readonly string[] UKGenerals = { "Crawford", "Harding", "Montgomery", "Wavell", "Slim" };

    private static string GetGeneralName(string nationName, Random rng) => nationName switch
    {
        "United States" => "Gen. " + USGenerals[rng.Next(USGenerals.Length)],
        "China" => "Gen. " + ChinaGenerals[rng.Next(ChinaGenerals.Length)],
        "Russia" => "Gen. " + RussiaGenerals[rng.Next(RussiaGenerals.Length)],
        "European Union" => "Gen. " + EUGenerals[rng.Next(EUGenerals.Length)],
        "India" => "Gen. " + IndiaGenerals[rng.Next(IndiaGenerals.Length)],
        "United Kingdom" => "Gen. " + UKGenerals[rng.Next(UKGenerals.Length)],
        _ => "General"
    };

    private static readonly string[] GenericFirsts = { "James", "Robert", "John", "Michael", "David", "William", "Richard", "Joseph", "Thomas", "Charles", "Wei", "Li", "Zhang", "Liu", "Chen", "Yang", "Zhao", "Huang" };
    private static readonly string[] GenericLasts = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Ivanov", "Smirnov", "Kuznetsov", "Popov", "Vasiliev", "Petrov", "Sokolov" };

    private static string GetRandomName(Random rng) => $"{GenericFirsts[rng.Next(GenericFirsts.Length)]} {GenericLasts[rng.Next(GenericLasts.Length)]}";
}
