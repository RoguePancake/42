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
    public const int NumNations = 13;

    // ═══════════════════════════════════════════════════════════════
    //  NATION TEMPLATES — The 13 named nations of the world
    //  Real-world inspirations in comments. See docs/NATION_RESEARCH.md
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Preferred terrain for capital placement scoring.</summary>
    public enum TerrainPreference { Plains, Coastal, Mountain, Desert, Forest, Island, Mixed }

    /// <summary>Military composition style for army spawning.</summary>
    public enum MilitaryProfile
    {
        CombinedArms,   // Hegemon — balanced with carriers
        MassInfantry,   // Revolutionary — quantity over quality
        TechDefense,    // Commercial — small but advanced
        Fortified,      // Traditionalist — artillery + anti-air
        TankHeavy,      // Industrial — armored blitzkrieg
        NavalDominant,  // Naval — destroyers, carriers, subs
        NuclearSmall,   // FreeState — small army + 1 nuke
        TokenForce,     // TradeCity — barely any military
        GuerrillaLight, // Guerrilla — pure infantry, rough terrain
        BalancedSmall,  // Intelligence/Remnant — small balanced
        SubmarineFleet, // IslandNaval — subs + shore defense
        Minimal,        // ResourceCursed — weakest military
    }

    public record NationTemplate(
        string Name,
        NationArchetype Archetype,
        NationTier Tier,
        int CityCount,          // Capital + secondaries
        int ArmyCount,
        float StartingTreasury,
        Color NationColor,
        TerrainPreference PreferredTerrain,
        MilitaryProfile MilProfile,
        // Starting resources (0-100 scale)
        float Iron, float Oil, float Uranium,
        float Electronics, float Manpower, float Food,
        float Stability,
        NationTrait[] Traits,   // Unique passive abilities
        string Description,     // Gameplay description
        string Lore             // Alternate-history backstory
    );

    public static readonly NationTemplate[] Templates =
    {
        // ══════════════════════════════════════════════════════════
        //  6 LARGE NATIONS — The Superpowers
        //  Each is an alternate-history "what if?" civilization
        // ══════════════════════════════════════════════════════════

        new("United States Alliance",    // #0 — What if democratic expansionism went continental?
            NationArchetype.Hegemon, NationTier.Large,
            CityCount: 12, ArmyCount: 8, StartingTreasury: 5000f,
            new Color(0.20f, 0.40f, 0.80f), // Deep Blue
            TerrainPreference.Plains, MilitaryProfile.CombinedArms,
            Iron: 40, Oil: 50, Uranium: 30, Electronics: 60, Manpower: 50, Food: 70,
            Stability: 85,
            new[] { NationTrait.CarrierDoctrine, NationTrait.TradeEmpire },
            "Military superpower. Carrier strike groups, combined arms doctrine, global projection. Richest food producer.",
            "Born from a federation of frontier colonies that industrialized faster than anyone predicted. " +
            "Their carrier fleets project power across every ocean. Their currency IS the global economy. " +
            "But decades of overreach have stretched them thin — and the debt is coming due."),

        new("Republic of Valdria",       // #1 — What if the Soviet dream survived through reform?
            NationArchetype.Revolutionary, NationTier.Large,
            CityCount: 10, ArmyCount: 7, StartingTreasury: 3000f,
            new Color(0.80f, 0.20f, 0.20f), // Deep Red
            TerrainPreference.Forest, MilitaryProfile.MassInfantry,
            Iron: 35, Oil: 40, Uranium: 20, Electronics: 15, Manpower: 80, Food: 60,
            Stability: 70,
            new[] { NationTrait.MassConscription, NationTrait.OilWeapon },
            "Ideological powerhouse. Largest infantry reserves, artillery doctrine, energy leverage. Will drown you in soldiers.",
            "The revolution never collapsed — it adapted. Valdria reformed just enough to survive, keeping the state apparatus " +
            "while allowing controlled markets. Their generals still believe in the old doctrine: quantity has a quality all its own. " +
            "Eleven time zones of strategic depth. The last invader who tried is still thawing."),

        new("Meridian Confederation",    // #2 — What if the Hanseatic League became a tech superstate?
            NationArchetype.Commercial, NationTier.Large,
            CityCount: 9, ArmyCount: 5, StartingTreasury: 6000f,
            new Color(0.20f, 0.72f, 0.35f), // Green
            TerrainPreference.Coastal, MilitaryProfile.TechDefense,
            Iron: 30, Oil: 10, Uranium: 10, Electronics: 90, Manpower: 30, Food: 20,
            Stability: 90,
            new[] { NationTrait.RareEarthMonopoly, NationTrait.CorporateDiplomacy },
            "Trade league turned superpower. Controls 90% of advanced electronics. Can starve your factories with an embargo.",
            "What began as a merchants' guild linking coastal city-states evolved into the world's most sophisticated economy. " +
            "The Meridian doesn't need the largest army — they manufacture everyone else's weapons systems. " +
            "Every chip in every missile guidance system has their logo etched at the nanometer scale. " +
            "Cross them, and your tanks become expensive paperweights."),

        new("Kingdom of Ashenmoor",      // #3 — What if a caliphate modernized like Meiji Japan?
            NationArchetype.Traditionalist, NationTier.Large,
            CityCount: 8, ArmyCount: 6, StartingTreasury: 4500f,
            new Color(0.80f, 0.65f, 0.15f), // Gold
            TerrainPreference.Mountain, MilitaryProfile.Fortified,
            Iron: 20, Oil: 80, Uranium: 15, Electronics: 10, Manpower: 40, Food: 15,
            Stability: 75,
            new[] { NationTrait.FortressDefense, NationTrait.SovereignWealth, NationTrait.OilWeapon },
            "Mountain kingdom sitting on an ocean of oil. $5 trillion sovereign wealth fund. Their fortress cities have never fallen.",
            "The ancient mountain kingdom watched empires rise and fall from behind their walls. When oil was discovered " +
            "beneath their deserts, they didn't just sell it — they invested the proceeds into a sovereign wealth fund " +
            "so vast it could buy most nations outright. Ashenmoor plays defense because they don't need to attack. " +
            "Time is on their side, and they can wait centuries."),

        new("Volkren Collective",        // #4 — What if Imperial Germany won and kept industrializing?
            NationArchetype.Industrial, NationTier.Large,
            CityCount: 10, ArmyCount: 7, StartingTreasury: 4000f,
            new Color(0.65f, 0.30f, 0.75f), // Purple
            TerrainPreference.Plains, MilitaryProfile.TankHeavy,
            Iron: 80, Oil: 25, Uranium: 15, Electronics: 40, Manpower: 70, Food: 50,
            Stability: 80,
            new[] { NationTrait.ArmoredBlitz, NationTrait.IndustrialBase, NationTrait.RareEarthMonopoly },
            "The world's factory. 54% of global steel. Their tanks outnumber everyone's. Shipyards build faster than anyone can sink.",
            "The Collective doesn't innovate — they scale. Every good idea from every nation gets reverse-engineered, " +
            "mass-produced, and deployed at 10x volume. Their shipyards launch more tonnage in a month than Thalassian builds in a year. " +
            "Their weakness? They import 80% of their oil through a single strait. " +
            "Block that, and the machine stops."),

        new("Thalassian Dominion",       // #5 — What if maritime Venice became a nuclear island empire?
            NationArchetype.Naval, NationTier.Large,
            CityCount: 8, ArmyCount: 6, StartingTreasury: 3500f,
            new Color(0.15f, 0.60f, 0.70f), // Teal
            TerrainPreference.Island, MilitaryProfile.NavalDominant,
            Iron: 25, Oil: 15, Uranium: 5, Electronics: 30, Manpower: 25, Food: 30,
            Stability: 85,
            new[] { NationTrait.NavalSupremacy, NationTrait.SpyMaster },
            "Island naval empire. Unconquered since founding. Their submarine-launched nukes and spy network make them untouchable.",
            "An archipelago of island fortresses connected by the world's most powerful navy. " +
            "Thalassian diplomats know everyone's secrets because Thalassian submarines tap everyone's cables. " +
            "Their empire was built on trade and maintained by intelligence. " +
            "Small army, but you have to GET to their islands first — and nobody has, in 400 years."),

        // ══════════════════════════════════════════════════════════
        //  7 SMALL NATIONS — The Survivors
        //  Each has one defining advantage and a reason to exist
        // ══════════════════════════════════════════════════════════

        new("Selvara",                   // #6 — What if a city-state split the atom first?
            NationArchetype.FreeState, NationTier.Small,
            CityCount: 4, ArmyCount: 2, StartingTreasury: 800f,
            new Color(0.90f, 0.55f, 0.15f), // Orange
            TerrainPreference.Mixed, MilitaryProfile.NuclearSmall,
            Iron: 10, Oil: 5, Uranium: 20, Electronics: 15, Manpower: 15, Food: 10,
            Stability: 75,
            new[] { NationTrait.NuclearDeterrent, NationTrait.NuclearAmbiguity },
            "4 provinces. 1 nuclear weapon. The most dangerous small nation in history. Everyone wants you dead or allied.",
            "A free state born from a disputed border region, populated by refugees from three different empires. " +
            "Selvara's scientists split the atom before anyone else — a desperate gamble that transformed a doomed buffer state " +
            "into the world's most dangerous small nation. The bomb hasn't been used. The bomb doesn't need to be used. " +
            "Everyone knows it's there. That's enough. For now."),

        new("Free City of Orinth",       // #7 — What if Carthage survived and became Singapore?
            NationArchetype.TradeCity, NationTier.Small,
            CityCount: 3, ArmyCount: 1, StartingTreasury: 2000f,
            new Color(0.90f, 0.75f, 0.30f), // Bright Gold
            TerrainPreference.Coastal, MilitaryProfile.TokenForce,
            Iron: 5, Oil: 5, Uranium: 0, Electronics: 20, Manpower: 5, Food: 5,
            Stability: 90,
            new[] { NationTrait.TradeEmpire, NationTrait.CorporateDiplomacy, NationTrait.SovereignWealth },
            "Richest city per capita in the world. No army to speak of. If trade stops, Orinth dies in weeks.",
            "Orinth has existed as a free port since before recorded history. Empires conquered the land around it " +
            "but never Orinth itself — because every empire needed somewhere to trade. The city has no natural resources, " +
            "no farmland, no strategic depth. What it has is a deep harbor, a reputation for neutrality, " +
            "and the fact that 40% of global trade passes through its docks. Destroying Orinth would hurt the destroyer more than the destroyed."),

        new("Kaelith Tribes",            // #8 — What if the Mongol clans never unified but never submitted?
            NationArchetype.Guerrilla, NationTier.Small,
            CityCount: 5, ArmyCount: 2, StartingTreasury: 400f,
            new Color(0.75f, 0.60f, 0.40f), // Sandy Brown
            TerrainPreference.Desert, MilitaryProfile.GuerrillaLight,
            Iron: 10, Oil: 15, Uranium: 5, Electronics: 0, Manpower: 25, Food: 10,
            Stability: 50,
            new[] { NationTrait.GuerrillaResistance, NationTrait.UnsiegeableDesert },
            "Dirt poor. Impossible to conquer. Invading armies die of thirst before they find anyone to fight.",
            "The Kaelith have never had a king, a capital, or a standing army. They don't need them. " +
            "Every tribe is autonomous. Every adult is a fighter. Their desert is so vast and so hostile " +
            "that three superpowers have tried to conquer it and three superpowers have given up. " +
            "The cost of occupation exceeds the value of the land by 500:1. The Kaelith know this. They're patient."),

        new("Duskhollow Pact",           // #9 — What if the Vatican's spy network went secular?
            NationArchetype.Intelligence, NationTier.Small,
            CityCount: 4, ArmyCount: 2, StartingTreasury: 1500f,
            new Color(0.35f, 0.45f, 0.35f), // Dark Forest Green
            TerrainPreference.Forest, MilitaryProfile.BalancedSmall,
            Iron: 15, Oil: 5, Uranium: 5, Electronics: 10, Manpower: 20, Food: 20,
            Stability: 95,
            new[] { NationTrait.SpyMaster, NationTrait.NeutralBroker },
            "They know everything about everyone. Spy depth +2 in all nations from turn 1. Attack them and the world knows YOUR secrets.",
            "Hidden in ancient forests, Duskhollow appears on maps as a minor nation of foresters and scholars. " +
            "In reality, it's the world's most sophisticated intelligence apparatus wearing the mask of a country. " +
            "Every embassy in the world has a Duskhollow 'cultural attache.' Every undersea cable passes through their relay stations. " +
            "They've been neutral for two centuries — not because they're peaceful, but because every nation fears what Duskhollow would reveal if attacked."),

        new("Ironmarch Remnant",         // #10 — What if Rome shrank but never died?
            NationArchetype.Remnant, NationTier.Small,
            CityCount: 3, ArmyCount: 2, StartingTreasury: 1000f,
            new Color(0.55f, 0.35f, 0.25f), // Brown
            TerrainPreference.Mountain, MilitaryProfile.BalancedSmall,
            Iron: 30, Oil: 10, Uranium: 10, Electronics: 5, Manpower: 10, Food: 10,
            Stability: 60,
            new[] { NationTrait.RemnantPride, NationTrait.FortressDefense },
            "Once ruled half the world. Now holds 3 mountain cities and a grudge. Their old fortresses still stand. Their pride won't bend.",
            "The Ironmarch Empire once spanned three continents. Legions, aqueducts, roads that still carry traffic a thousand years later. " +
            "Now they hold a mountain pass and three cities, guarding the ruins of their own glory. " +
            "Their officers still train in the old academies. Their walls were built to last millennia and have. " +
            "The empire is gone, but the Remnant fights like it isn't — and that makes them dangerous in defense, " +
            "even if they can never reclaim what was lost."),

        new("Port Serin",                // #11 — What if Crete became a submarine republic?
            NationArchetype.IslandNaval, NationTier.Small,
            CityCount: 3, ArmyCount: 2, StartingTreasury: 1200f,
            new Color(0.30f, 0.70f, 0.85f), // Light Blue
            TerrainPreference.Island, MilitaryProfile.SubmarineFleet,
            Iron: 10, Oil: 10, Uranium: 0, Electronics: 15, Manpower: 10, Food: 15,
            Stability: 80,
            new[] { NationTrait.PorcupineDefense, NationTrait.SubmarineWolf },
            "Island fortress. Anti-ship missiles and submarines make invasion a suicide mission. Controls a key strait.",
            "Port Serin's three islands sit astride the busiest shipping lane in the world. " +
            "Rather than building a surface fleet they can't afford, Serin invested everything in submarines and shore-based missiles. " +
            "Their doctrine is simple: we can't win a war, but we can make sure you lose one. " +
            "Any fleet attempting amphibious assault faces a gauntlet of mines, torpedoes, and missiles. " +
            "The last admiral who tried lost 40% of his ships before reaching shore."),

        new("Ashfall Compact",           // #12 — What if a nation formed around a supervolcano's riches?
            NationArchetype.ResourceCursed, NationTier.Small,
            CityCount: 3, ArmyCount: 1, StartingTreasury: 500f,
            new Color(0.60f, 0.40f, 0.50f), // Dusty Mauve
            TerrainPreference.Desert, MilitaryProfile.Minimal,
            Iron: 15, Oil: 5, Uranium: 80, Electronics: 0, Manpower: 10, Food: 5,
            Stability: 35,
            new[] { NationTrait.ProliferationTarget },
            "43% of the world's uranium. Weakest military. Lowest stability. Everyone is coming for what's under their soil.",
            "The Ashfall wastes were uninhabitable until geologists discovered the largest uranium deposit on the planet. " +
            "Overnight, the scattered mining camps became a nation — or tried to. Six coups in twenty years. " +
            "Every superpower has 'advisors' in the capital. Every intelligence service runs ops in Ashfall. " +
            "The current government knows it's sitting on the most valuable — and most dangerous — ground in the world. " +
            "If they ever stabilize long enough to refine that uranium themselves... the balance of power changes forever."),
    };

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
        string playerRole = "Defense Minister", int focusIndex = 0, int playerNationIndex = 6)
    {
        var rng = new Random(seed);
        int mapW = TerrainGenerator.DefaultWidth;
        int mapH = TerrainGenerator.DefaultHeight;

        // ═══ Generate terrain + rivers ═══
        var (terrain, riverPaths) = TerrainGenerator.GenerateWorld(mapW, mapH, seed);

        // ═══ Find city locations — need enough for all nations ═══
        int totalCitiesNeeded = Templates.Sum(t => t.CityCount) + 20; // extra buffer
        var citySpots = TerrainGenerator.FindCityLocations(terrain, riverPaths, mapW, mapH, seed, totalCitiesNeeded);

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

        int nationCount = Math.Min(NumNations, citySpots.Count);
        playerNationIndex = Math.Clamp(playerNationIndex, 0, nationCount - 1);

        // ═══ Terrain-driven capital placement ═══
        // Score each candidate spot for each nation's preferred terrain, then greedily assign
        var capitalSpots = AssignCapitals(terrain, citySpots, nationCount, mapW, mapH, rng);
        var usedSpots = new HashSet<int>(capitalSpots.Values.Select(s => citySpots.IndexOf(s)));
        var remainingSpots = citySpots.Where((_, i) => !usedSpots.Contains(i)).ToList();

        // ═══ Create nations from templates ═══
        for (int n = 0; n < nationCount; n++)
        {
            var t = Templates[n];
            bool isPlayer = (n == playerNationIndex);
            var spot = capitalSpots[n];

            var nation = new NationData
            {
                Id = $"N_{n}",
                Name = t.Name,
                Archetype = t.Archetype,
                Tier = t.Tier,
                NationColor = t.NationColor,
                IsPlayer = isPlayer,
                CapitalX = spot.x,
                CapitalY = spot.y,
                Treasury = t.StartingTreasury,
                Prestige = t.Tier == NationTier.Large ? 50f + rng.Next(30) : 20f + rng.Next(20),
                Iron = t.Iron, Oil = t.Oil, Uranium = t.Uranium,
                Electronics = t.Electronics, Manpower = t.Manpower, Food = t.Food,
                Stability = t.Stability,
                Traits = new List<NationTrait>(t.Traits),
            };
            world.Nations.Add(nation);
        }

        world.PlayerNationId = $"N_{playerNationIndex}";

        // ═══ Set starting diplomatic dispositions ═══
        SetStartingDiplomacy(world);

        // ═══ Create cities (capital + secondary per nation template) ═══
        int cityIdx = 0;
        for (int n = 0; n < nationCount; n++)
        {
            var nation = world.Nations[n];
            var spot = capitalSpots[n];

            // Capital city — named after the nation or its root
            string capitalName = Templates[n].Name.Split(' ').Last(); // e.g. "Alliance" → use last word
            if (capitalName.Length < 4) capitalName = Templates[n].Name; // fallback for short names
            world.Cities.Add(new CityData
            {
                Id = $"C_{cityIdx}",
                NationId = nation.Id,
                Name = capitalName,
                TileX = spot.x,
                TileY = spot.y,
                IsCapital = true,
                Size = 3,
                HP = 400,
                CityIndex = cityIdx,
            });
            cityIdx++;
        }

        // Assign remaining cities to nearest nation, respecting per-nation limits
        var citiesPerNation = new int[nationCount]; // tracks secondary cities (capital already placed)
        foreach (var spot in remainingSpots)
        {
            int nearestNation = -1;
            float nearestDist = float.MaxValue;
            for (int n = 0; n < nationCount; n++)
            {
                int maxSecondary = Templates[n].CityCount - 1; // minus capital
                if (citiesPerNation[n] >= maxSecondary) continue;
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

        // ═══ Count provinces (archetype is already set from template) ═══
        for (int n = 0; n < nationCount; n++)
        {
            var nation = world.Nations[n];
            nation.ProvinceCount = CountTilesOwned(world, n, mapW, mapH);
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

        // ═══ Spawn armies per nation template ═══
        for (int n = 0; n < nationCount; n++)
        {
            var nation = world.Nations[n];
            var template = Templates[n];
            var nationCities = world.Cities.Where(c => c.NationId == nation.Id).ToList();

            for (int a = 0; a < template.ArmyCount && a < nationCities.Count; a++)
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

                // ═══ Archetype-driven military composition ═══
                PopulateArmyByProfile(army, template.MilProfile, a, isCoastal, rng);

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

    /// <summary>Generate world with only 12 AI nations (no player). Used for custom nation flow.</summary>
    public static WorldData CreateWorldWithoutPlayer(int seed)
    {
        // Create world with 12 AI templates (skip index 6 = Selvara, use first 12 non-player)
        // We generate all 13 preset nations but mark none as player
        var world = CreateWorld(seed, "J. Crawford", "Defense Minister", 0, 0);
        // Unset player flag on all nations
        foreach (var n in world.Nations) n.IsPlayer = false;
        world.PlayerNationId = null;
        return world;
    }

    /// <summary>Add a custom player nation to an existing world at the specified capital location.</summary>
    public static void AddCustomNation(WorldData world, int capitalX, int capitalY,
        string nationName, NationArchetype archetype,
        string playerName, string playerRole, int focusIndex)
    {
        var rng = new Random(world.Seed + 999); // deterministic but different from world gen
        int mapW = world.MapWidth, mapH = world.MapHeight;
        int nationIdx = world.Nations.Count;

        // Derive resources from geography
        var res = AnalyzeGeographyForResources(world.TerrainMap!, capitalX, capitalY, mapW, mapH);

        // Pick a military profile based on chosen archetype
        var milProfile = archetype switch
        {
            NationArchetype.Hegemon => MilitaryProfile.CombinedArms,
            NationArchetype.Commercial => MilitaryProfile.TechDefense,
            NationArchetype.Revolutionary => MilitaryProfile.MassInfantry,
            NationArchetype.Traditionalist => MilitaryProfile.Fortified,
            NationArchetype.Industrial => MilitaryProfile.TankHeavy,
            NationArchetype.Naval => MilitaryProfile.NavalDominant,
            NationArchetype.FreeState => MilitaryProfile.NuclearSmall,
            NationArchetype.Guerrilla => MilitaryProfile.GuerrillaLight,
            NationArchetype.Intelligence => MilitaryProfile.BalancedSmall,
            _ => MilitaryProfile.BalancedSmall,
        };

        // Create nation
        var nation = new NationData
        {
            Id = $"N_{nationIdx}",
            Name = nationName,
            Archetype = archetype,
            Tier = NationTier.Small,
            NationColor = new Color(0.95f, 0.50f, 0.20f), // Custom orange
            IsPlayer = true,
            CapitalX = capitalX,
            CapitalY = capitalY,
            Treasury = res.Treasury,
            Prestige = 25f,
            Iron = res.Iron, Oil = res.Oil, Uranium = res.Uranium,
            Electronics = res.Electronics, Manpower = res.Manpower, Food = res.Food,
            Stability = 70f,
        };
        world.Nations.Add(nation);
        world.PlayerNationId = nation.Id;

        // Create capital city
        int cityIdx = world.Cities.Count;
        world.Cities.Add(new CityData
        {
            Id = $"C_{cityIdx}",
            NationId = nation.Id,
            Name = nationName.Split(' ').Last(),
            TileX = capitalX,
            TileY = capitalY,
            IsCapital = true,
            Size = 3,
            HP = 400,
            CityIndex = cityIdx,
        });

        // Find 3 nearby spots for secondary cities
        var candidates = TerrainGenerator.FindCityLocations(
            world.TerrainMap!, world.RiverPaths.Select(r =>
            {
                var flat = new int[r.Length * 2];
                for (int i = 0; i < r.Length; i++) { flat[i * 2] = (int)r[i].X; flat[i * 2 + 1] = (int)r[i].Y; }
                return flat;
            }).ToList(),
            mapW, mapH, world.Seed + 1000, 30);

        int secondariesPlaced = 0;
        foreach (var spot in candidates)
        {
            if (secondariesPlaced >= 3) break;
            float dx = spot.x - capitalX, dy = spot.y - capitalY;
            float dist = dx * dx + dy * dy;
            if (dist < 15 * 15 || dist > 60 * 60) continue; // too close or too far

            // Check it's not already a city
            bool tooCloseToExisting = world.Cities.Any(c =>
            {
                float cdx = c.TileX - spot.x, cdy = c.TileY - spot.y;
                return cdx * cdx + cdy * cdy < 20 * 20;
            });
            if (tooCloseToExisting) continue;

            cityIdx = world.Cities.Count;
            world.Cities.Add(new CityData
            {
                Id = $"C_{cityIdx}",
                NationId = nation.Id,
                Name = $"{CityPrefixes[rng.Next(CityPrefixes.Length)]} {CitySuffixes[rng.Next(CitySuffixes.Length)]}",
                TileX = spot.x,
                TileY = spot.y,
                IsCapital = false,
                Size = spot.quality > 5f ? 2 : 1,
                HP = spot.quality > 5f ? 200 : 100,
                CityIndex = cityIdx,
            });
            secondariesPlaced++;
        }

        // Re-derive territory and borders for the whole map
        AssignCityTerritory(world, world.TerrainMap!, mapW, mapH);
        DeriveNationOwnership(world, mapW, mapH);
        ComputeNationBorders(world, mapW, mapH);

        // Count provinces
        nation.ProvinceCount = CountTilesOwned(world, nationIdx, mapW, mapH);

        // Spawn 2 armies
        var nationCities = world.Cities.Where(c => c.NationId == nation.Id).ToList();
        for (int a = 0; a < 2 && a < nationCities.Count; a++)
        {
            var city = nationCities[a];
            bool isCoastal = IsNearWater(world.TerrainMap!, city.TileX, city.TileY, mapW, mapH);
            var army = new ArmyData
            {
                Id = $"{nation.Id}_A_{world.Armies.Count}",
                NationId = nation.Id,
                Name = a < ArmyNames.Length ? ArmyNames[a] : $"Army {a + 1}",
                TileX = city.TileX, TileY = city.TileY,
                TargetTileX = city.TileX, TargetTileY = city.TileY,
                PixelX = city.TileX * MapManagerConstants.TileSize + MapManagerConstants.TileSize / 2f,
                PixelY = city.TileY * MapManagerConstants.TileSize + MapManagerConstants.TileSize / 2f,
                CurrentOrder = a == 0 ? MilitaryOrder.BorderWatch : MilitaryOrder.Standby,
                Formation = a == 0 ? FormationType.Circle : FormationType.Spread,
            };
            army.TargetPixelX = army.PixelX;
            army.TargetPixelY = army.PixelY;
            PopulateArmyByProfile(army, milProfile, a, isCoastal, rng);
            if (a == 0) army.GarrisonCityId = city.Id;
            world.Armies.Add(army);
        }

        // Spawn characters
        string[] roles = { "Head of State", "Defense Minister", "Foreign Minister",
                           "Director of Intelligence", "Chief of Staff",
                           "Finance Minister", "Interior Minister", "Opposition Leader" };
        var capital = world.Cities.First(c => c.NationId == nation.Id && c.IsCapital);
        for (int i = 0; i < roles.Length; i++)
        {
            bool isPlayer = roles[i] == playerRole;
            string cName = isPlayer ? playerName :
                $"{FirstNames[rng.Next(FirstNames.Length)]} {LastNames[rng.Next(LastNames.Length)]}";

            float ta = 30f, wa = 20f, bsa = 40f;
            if (roles[i] == "Head of State") { ta = 80; wa = 60; bsa = 70; }
            else if (roles[i] == "Defense Minister") { ta = 40; wa = 20; bsa = 60; }
            else if (roles[i] == "Foreign Minister") { ta = 15; wa = 80; bsa = 30; }
            else if (roles[i] == "Director of Intelligence") { ta = 30; wa = 40; bsa = 95; }
            else if (roles[i] == "Opposition Leader") { ta = 50; wa = 10; bsa = 30; }

            if (isPlayer)
            {
                if (focusIndex == 1) ta = Math.Clamp(ta + 20, 0, 100);
                if (focusIndex == 2) wa = Math.Clamp(wa + 20, 0, 100);
                if (focusIndex == 3) bsa = Math.Clamp(bsa + 20, 0, 100);
            }

            float px = capital.TileX * MapManagerConstants.TileSize + MapManagerConstants.TileSize / 2f;
            float py = capital.TileY * MapManagerConstants.TileSize + MapManagerConstants.TileSize / 2f;

            world.Characters.Add(new CharacterData
            {
                Id = $"{nation.Id}_Char_{i + 1}",
                NationId = nation.Id,
                Name = cName,
                Role = roles[i],
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

        // Set diplomacy — custom nation starts neutral with everyone
        foreach (var other in world.Nations)
        {
            if (other.Id == nation.Id) continue;
            nation.Relations[other.Id] = DiplomaticStatus.Neutral;
            other.Relations[nation.Id] = DiplomaticStatus.Neutral;
        }

        GD.Print($"[WorldGen] Custom nation '{nationName}' placed at ({capitalX},{capitalY}). " +
            $"{secondariesPlaced + 1} cities, 2 armies. Resources: Iron={res.Iron:F0} Oil={res.Oil:F0} " +
            $"Uranium={res.Uranium:F0} Food={res.Food:F0}");
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

    // ═══ Terrain-driven capital placement ═══
    // Score each candidate city spot per nation's terrain preference, then greedily assign
    private static Dictionary<int, (int x, int y, float quality)> AssignCapitals(
        int[,] terrain, List<(int x, int y, float quality)> spots, int nationCount, int w, int h, Random rng)
    {
        var assigned = new Dictionary<int, (int x, int y, float quality)>();
        var usedSpots = new HashSet<int>();
        const int minCapitalSpacing = 40; // tiles between capitals

        // Large nations first (indices 0-5), then small (6-12)
        // Within each tier, randomize order slightly to avoid same placement every seed
        var order = Enumerable.Range(0, nationCount).ToList();
        // Shuffle within tiers
        for (int i = order.Count - 1; i > 0; i--)
        {
            // Keep large (0-5) and small (6+) tiers grouped but shuffled internally
            int tierStart = Templates[i].Tier == NationTier.Large ? 0 : 6;
            int tierEnd = Templates[i].Tier == NationTier.Large ? Math.Min(6, nationCount) : nationCount;
            int j = tierStart + rng.Next(tierEnd - tierStart);
            if (i >= tierStart && i < tierEnd && j >= tierStart && j < tierEnd)
                (order[i], order[j]) = (order[j], order[i]);
        }

        foreach (int n in order)
        {
            var pref = Templates[n].PreferredTerrain;
            float bestScore = float.MinValue;
            int bestIdx = -1;

            for (int s = 0; s < spots.Count; s++)
            {
                if (usedSpots.Contains(s)) continue;

                // Check minimum spacing from already-assigned capitals
                bool tooClose = false;
                foreach (var (_, cap) in assigned)
                {
                    float dx = spots[s].x - cap.x;
                    float dy = spots[s].y - cap.y;
                    if (dx * dx + dy * dy < minCapitalSpacing * minCapitalSpacing)
                    { tooClose = true; break; }
                }
                if (tooClose) continue;

                float score = ScoreSpotForTerrain(terrain, spots[s].x, spots[s].y, pref, w, h);
                score += spots[s].quality * 0.5f; // factor in base desirability
                score += rng.Next(100) * 0.02f;   // small randomness

                if (score > bestScore) { bestScore = score; bestIdx = s; }
            }

            if (bestIdx < 0)
            {
                // Fallback: take any unused spot
                for (int s = 0; s < spots.Count; s++)
                    if (!usedSpots.Contains(s)) { bestIdx = s; break; }
            }

            if (bestIdx >= 0)
            {
                assigned[n] = spots[bestIdx];
                usedSpots.Add(bestIdx);
            }
        }

        return assigned;
    }

    /// <summary>Score a tile location for a nation's terrain preference. Scans a 25-tile radius.</summary>
    private static float ScoreSpotForTerrain(int[,] terrain, int cx, int cy, TerrainPreference pref, int w, int h)
    {
        int coastal = 0, mountain = 0, forest = 0, grass = 0, sand = 0, hills = 0, water = 0;
        int total = 0;
        const int radius = 25;

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx * dx + dy * dy > radius * radius) continue;
                int nx = cx + dx, ny = cy + dy;
                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                total++;

                int t = terrain[nx, ny];
                if (t == (int)TerrainGenerator.Terrain.Mountain) mountain++;
                else if (t == (int)TerrainGenerator.Terrain.Forest) forest++;
                else if (t == (int)TerrainGenerator.Terrain.Grass) grass++;
                else if (t == (int)TerrainGenerator.Terrain.Sand) sand++;
                else if (t == (int)TerrainGenerator.Terrain.Hills) hills++;
                else if (t <= (int)TerrainGenerator.Terrain.Water) water++;

                // Check adjacency for coastal
                if (t > (int)TerrainGenerator.Terrain.Water)
                {
                    if ((nx > 0 && terrain[nx - 1, ny] <= 1) ||
                        (nx < w - 1 && terrain[nx + 1, ny] <= 1) ||
                        (ny > 0 && terrain[nx, ny - 1] <= 1) ||
                        (ny < h - 1 && terrain[nx, ny + 1] <= 1))
                        coastal++;
                }
            }
        }

        if (total == 0) return 0f;
        float coastR = coastal / (float)total;
        float mountR = mountain / (float)total;
        float forestR = forest / (float)total;
        float grassR = grass / (float)total;
        float sandR = sand / (float)total;
        float waterR = water / (float)total;

        return pref switch
        {
            TerrainPreference.Plains   => grassR * 10f + hills * 0.02f - mountR * 3f,
            TerrainPreference.Coastal  => coastR * 12f + grassR * 3f - mountR * 2f,
            TerrainPreference.Mountain => mountR * 10f + hills * 0.05f - waterR * 5f,
            TerrainPreference.Desert   => sandR * 10f + grassR * 1f - waterR * 3f - forestR * 2f,
            TerrainPreference.Forest   => forestR * 10f + grassR * 2f - sandR * 3f,
            TerrainPreference.Island   => waterR * 6f + coastR * 8f - grassR * 2f,
            TerrainPreference.Mixed    => grassR * 3f + forestR * 2f + mountR * 2f + coastR * 2f,
            _ => 0f
        };
    }

    /// <summary>Count tiles owned by a nation.</summary>
    private static int CountTilesOwned(WorldData world, int nationIdx, int w, int h)
    {
        int count = 0;
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if (world.OwnershipMap![x, y] == nationIdx) count++;
        return count;
    }

    // ═══ Archetype-driven army composition ═══
    private static void PopulateArmyByProfile(ArmyData army, MilitaryProfile profile, int armyIndex, bool isCoastal, Random rng)
    {
        switch (profile)
        {
            case MilitaryProfile.CombinedArms: // Hegemon — USA-style balanced + carriers
                army.Composition[UnitType.Infantry] = 800 + rng.Next(200);
                army.Composition[UnitType.Tank] = 100 + rng.Next(40);
                army.Composition[UnitType.Artillery] = 50 + rng.Next(20);
                army.Composition[UnitType.AntiAir] = 30 + rng.Next(15);
                army.Composition[UnitType.Fighter] = 36 + rng.Next(24);
                if (armyIndex == 0) army.Composition[UnitType.Bomber] = 12 + rng.Next(12);
                if (isCoastal) { army.Composition[UnitType.Destroyer] = 8 + rng.Next(6); army.Composition[UnitType.Carrier] = armyIndex < 2 ? 1 : 0; army.Composition[UnitType.Submarine] = 4 + rng.Next(4); }
                break;

            case MilitaryProfile.MassInfantry: // Revolutionary — Russia/USSR-style quantity
                army.Composition[UnitType.Infantry] = 1200 + rng.Next(400);
                army.Composition[UnitType.Tank] = 60 + rng.Next(40);
                army.Composition[UnitType.Artillery] = 80 + rng.Next(30);
                army.Composition[UnitType.AntiAir] = 20 + rng.Next(15);
                if (armyIndex == 0) army.Composition[UnitType.Fighter] = 24 + rng.Next(12);
                if (isCoastal) { army.Composition[UnitType.Submarine] = 6 + rng.Next(4); army.Composition[UnitType.Destroyer] = 4 + rng.Next(4); }
                break;

            case MilitaryProfile.TechDefense: // Commercial — EU-style small but advanced
                army.Composition[UnitType.Infantry] = 300 + rng.Next(100);
                army.Composition[UnitType.Tank] = 30 + rng.Next(20);
                army.Composition[UnitType.Artillery] = 15 + rng.Next(10);
                army.Composition[UnitType.AntiAir] = 25 + rng.Next(10);
                army.Composition[UnitType.Fighter] = 24 + rng.Next(18);
                if (isCoastal) { army.Composition[UnitType.Destroyer] = 6 + rng.Next(4); army.Composition[UnitType.Submarine] = 3 + rng.Next(3); }
                break;

            case MilitaryProfile.Fortified: // Traditionalist — Saudi/Gulf defensive
                army.Composition[UnitType.Infantry] = 600 + rng.Next(200);
                army.Composition[UnitType.Tank] = 50 + rng.Next(20);
                army.Composition[UnitType.Artillery] = 70 + rng.Next(30);
                army.Composition[UnitType.AntiAir] = 40 + rng.Next(20);
                army.Composition[UnitType.Fighter] = 24 + rng.Next(18);
                if (armyIndex == 0) army.Composition[UnitType.Bomber] = 8 + rng.Next(8);
                break;

            case MilitaryProfile.TankHeavy: // Industrial — China-style armor blitz
                army.Composition[UnitType.Infantry] = 700 + rng.Next(200);
                army.Composition[UnitType.Tank] = 180 + rng.Next(60);
                army.Composition[UnitType.Artillery] = 50 + rng.Next(20);
                army.Composition[UnitType.AntiAir] = 20 + rng.Next(15);
                if (armyIndex == 0) army.Composition[UnitType.Fighter] = 20 + rng.Next(12);
                if (isCoastal) { army.Composition[UnitType.Destroyer] = 6 + rng.Next(4); }
                break;

            case MilitaryProfile.NavalDominant: // Naval Empire — British-style sea power
                army.Composition[UnitType.Infantry] = 400 + rng.Next(100);
                army.Composition[UnitType.Tank] = 30 + rng.Next(20);
                army.Composition[UnitType.AntiAir] = 15 + rng.Next(10);
                army.Composition[UnitType.Fighter] = 18 + rng.Next(12);
                army.Composition[UnitType.Destroyer] = 12 + rng.Next(8);
                army.Composition[UnitType.Submarine] = 8 + rng.Next(6);
                if (armyIndex < 2) army.Composition[UnitType.Carrier] = 1;
                army.Composition[UnitType.LandingCraft] = 4 + rng.Next(4);
                break;

            case MilitaryProfile.NuclearSmall: // FreeState — Israel-style tiny + nuke
                army.Composition[UnitType.Infantry] = 200 + rng.Next(80);
                army.Composition[UnitType.Tank] = 25 + rng.Next(15);
                army.Composition[UnitType.Artillery] = 10 + rng.Next(8);
                army.Composition[UnitType.AntiAir] = 12 + rng.Next(8);
                army.Composition[UnitType.Fighter] = 10 + rng.Next(6);
                if (armyIndex == 0) army.Composition[UnitType.NuclearMissile] = 1; // THE bomb
                break;

            case MilitaryProfile.TokenForce: // Trade City — Singapore-style minimal
                army.Composition[UnitType.Infantry] = 80 + rng.Next(40);
                army.Composition[UnitType.AntiAir] = 15 + rng.Next(10);
                if (isCoastal) army.Composition[UnitType.Destroyer] = 2 + rng.Next(3);
                break;

            case MilitaryProfile.GuerrillaLight: // Guerrilla — Afghan-style pure infantry
                army.Composition[UnitType.Infantry] = 400 + rng.Next(200);
                army.Composition[UnitType.Artillery] = 5 + rng.Next(8);
                break;

            case MilitaryProfile.BalancedSmall: // Intelligence/Remnant — small balanced
                army.Composition[UnitType.Infantry] = 200 + rng.Next(100);
                army.Composition[UnitType.Tank] = 15 + rng.Next(15);
                army.Composition[UnitType.Artillery] = 10 + rng.Next(10);
                army.Composition[UnitType.AntiAir] = 8 + rng.Next(8);
                if (armyIndex == 0) army.Composition[UnitType.Fighter] = 6 + rng.Next(6);
                if (isCoastal) army.Composition[UnitType.Submarine] = 2 + rng.Next(3);
                break;

            case MilitaryProfile.SubmarineFleet: // Island Naval — Taiwan/Cuba-style
                army.Composition[UnitType.Infantry] = 150 + rng.Next(80);
                army.Composition[UnitType.AntiAir] = 12 + rng.Next(8);
                army.Composition[UnitType.Submarine] = 6 + rng.Next(4);
                army.Composition[UnitType.Destroyer] = 4 + rng.Next(3);
                if (armyIndex == 0) army.Composition[UnitType.Missile] = 4 + rng.Next(4); // anti-ship missiles
                break;

            case MilitaryProfile.Minimal: // Resource Cursed — weakest
                army.Composition[UnitType.Infantry] = 100 + rng.Next(80);
                army.Composition[UnitType.Tank] = 5 + rng.Next(8);
                break;
        }
    }

    /// <summary>Analyze geography around a point for the custom nation feature.
    /// Returns resource values derived from terrain composition.</summary>
    public static (float Iron, float Oil, float Uranium, float Electronics, float Manpower, float Food, float Treasury)
        AnalyzeGeographyForResources(int[,] terrain, int cx, int cy, int w, int h)
    {
        int coastal = 0, mountain = 0, forest = 0, grass = 0, sand = 0, hills = 0;
        int total = 0;
        const int radius = 50;

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx * dx + dy * dy > radius * radius) continue;
                int nx = cx + dx, ny = cy + dy;
                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                total++;

                int t = terrain[nx, ny];
                if (t == (int)TerrainGenerator.Terrain.Mountain) mountain++;
                else if (t == (int)TerrainGenerator.Terrain.Forest) forest++;
                else if (t == (int)TerrainGenerator.Terrain.Grass) grass++;
                else if (t == (int)TerrainGenerator.Terrain.Sand) sand++;
                else if (t == (int)TerrainGenerator.Terrain.Hills) hills++;
                if (t > (int)TerrainGenerator.Terrain.Water &&
                    ((nx > 0 && terrain[nx - 1, ny] <= 1) || (nx < w - 1 && terrain[nx + 1, ny] <= 1) ||
                     (ny > 0 && terrain[nx, ny - 1] <= 1) || (ny < h - 1 && terrain[nx, ny + 1] <= 1)))
                    coastal++;
            }
        }

        if (total == 0) total = 1;
        float cR = coastal / (float)total;
        float mR = mountain / (float)total;
        float fR = forest / (float)total;
        float gR = grass / (float)total;
        float sR = sand / (float)total;
        float hR = hills / (float)total;

        return (
            Iron:        Math.Clamp(10f + mR * 60f + hR * 40f, 0, 100),
            Oil:         Math.Clamp(5f + sR * 50f + cR * 30f, 0, 100),
            Uranium:     Math.Clamp(5f + mR * 30f + sR * 20f, 0, 100),
            Electronics: Math.Clamp(10f + gR * 20f, 0, 100),
            Manpower:    Math.Clamp(10f + gR * 40f + fR * 30f, 0, 100),
            Food:        Math.Clamp(10f + gR * 50f + fR * 20f + cR * 15f, 0, 100),
            Treasury:    800f + cR * 500f + gR * 300f
        );
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

    // ═══════════════════════════════════════════════════════════════
    //  STARTING DIPLOMACY — Pre-existing relationships between nations
    //  Not everyone starts neutral. History creates friends & enemies.
    // ═══════════════════════════════════════════════════════════════

    private static void SetStartingDiplomacy(WorldData world)
    {
        // Initialize all relations to Neutral
        foreach (var a in world.Nations)
            foreach (var b in world.Nations)
                if (a.Id != b.Id)
                    a.Relations[b.Id] = DiplomaticStatus.Neutral;

        // Helper to set symmetric relations
        void SetRelation(int a, int b, DiplomaticStatus status)
        {
            if (a >= world.Nations.Count || b >= world.Nations.Count) return;
            world.Nations[a].Relations[world.Nations[b].Id] = status;
            world.Nations[b].Relations[world.Nations[a].Id] = status;
        }

        // ── Great power rivalries ──
        // USA Alliance vs Valdria — Cold War rivals
        SetRelation(0, 1, DiplomaticStatus.Hostile);
        // USA Alliance vs Volkren — emerging rivalry over industrial dominance
        SetRelation(0, 4, DiplomaticStatus.Wary);
        // Valdria vs Volkren — ideological neighbors, uneasy respect
        SetRelation(1, 4, DiplomaticStatus.Wary);

        // ── Natural alliances ──
        // USA Alliance + Meridian — democratic trade partners
        SetRelation(0, 2, DiplomaticStatus.Friendly);
        // USA Alliance + Thalassian — naval alliance (Five Eyes equivalent)
        SetRelation(0, 5, DiplomaticStatus.Allied);
        // Meridian + Thalassian — tech + naval cooperation
        SetRelation(2, 5, DiplomaticStatus.Friendly);

        // ── Resource tensions ──
        // Volkren needs Ashenmoor's oil — uneasy trade dependency
        SetRelation(4, 3, DiplomaticStatus.Wary);
        // Meridian needs Ashenmoor's oil too — they pay well
        SetRelation(2, 3, DiplomaticStatus.Friendly);

        // ── Small nation dynamics ──
        // Everyone eyes Ashfall's uranium
        SetRelation(0, 12, DiplomaticStatus.Wary); // USA wants to secure it
        SetRelation(1, 12, DiplomaticStatus.Wary); // Valdria wants to secure it
        SetRelation(4, 12, DiplomaticStatus.Wary); // Volkren wants to secure it

        // Duskhollow is useful to everyone — friendly with trade powers
        SetRelation(9, 2, DiplomaticStatus.Friendly); // Meridian values intel
        SetRelation(9, 5, DiplomaticStatus.Friendly); // Thalassian respects spies

        // Ironmarch wants to reclaim territory — hostile to nearest large power
        SetRelation(10, 4, DiplomaticStatus.Hostile); // Volkren occupies their old lands

        // Port Serin aligned with whoever controls the strait
        SetRelation(11, 5, DiplomaticStatus.Friendly); // Naval solidarity with Thalassian

        // Orinth is friends with everyone (trade)
        SetRelation(7, 2, DiplomaticStatus.Friendly);
        SetRelation(7, 0, DiplomaticStatus.Friendly);

        // Selvara (player) — surrounded, no natural allies
        // Everyone is neutral-to-wary. Player must build alliances.
        SetRelation(6, 0, DiplomaticStatus.Wary);  // USA sees Selvara's nuke as a problem
        SetRelation(6, 1, DiplomaticStatus.Wary);  // Valdria doesn't trust small nuclear states
    }
}

/// <summary>Shared tile size constant.</summary>
public static class MapManagerConstants
{
    public const int TileSize = 32; // 32px tiles for the 600x360 world
}
