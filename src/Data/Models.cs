using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Warship.Data;

// ═══════════════════════════════════════════════════════════════
//  WORLD DATA — The entire game state
// ═══════════════════════════════════════════════════════════════

public class WorldData
{
    public int Seed;
    public int TurnNumber = 1;
    public int Year => TurnNumber / 12;
    public int Month => (TurnNumber % 12) + 1;
    public string? PlayerNationId;

    public int MapWidth;
    public int MapHeight;
    public int[,]? TerrainMap;          // terrain type per cell
    public int[,]? OwnershipMap;        // nation index per cell, -1 = unclaimed
    public int[,]? CityOwnershipMap;    // city index per cell, -1 = no city control

    public List<NationData> Nations = new();
    public List<CityData> Cities = new();
    public List<Vector2[]> RiverPaths = new();
    public List<ArmyData> Armies = new();
    public List<CharacterData> Characters = new();

    // Legacy compatibility — old code referencing Units still compiles
    [System.Obsolete("Use Armies instead. Units is kept for backward compat.")]
    public List<UnitData> Units = new();

    // Pre-computed border polylines per nation (recomputed on territory change)
    public Dictionary<int, List<Vector2[]>> NationBorderLines = new();
}

// ═══════════════════════════════════════════════════════════════
//  TERRAIN
// ═══════════════════════════════════════════════════════════════

public enum TerrainType
{
    DeepWater = 0,
    Water = 1,
    Sand = 2,
    Grass = 3,
    Forest = 4,
    Hills = 5,
    Mountain = 6,
    Snow = 7
}

public static class TerrainRules
{
    public static bool IsPassable(int terrain) => terrain >= 2 && terrain != 6;
    public static bool IsLand(int terrain) => terrain >= 2;

    /// <summary>Movement cost multiplier for army pathfinding.</summary>
    public static float MovementCost(int terrain) => terrain switch
    {
        (int)TerrainType.DeepWater => 999f,  // Impassable for land
        (int)TerrainType.Water => 999f,
        (int)TerrainType.Sand => 1.5f,
        (int)TerrainType.Grass => 1.0f,
        (int)TerrainType.Forest => 2.0f,
        (int)TerrainType.Hills => 2.5f,
        (int)TerrainType.Mountain => 999f,   // Impassable
        (int)TerrainType.Snow => 3.0f,
        _ => 1.0f
    };

    /// <summary>Naval movement cost.</summary>
    public static float NavalMovementCost(int terrain) => terrain switch
    {
        (int)TerrainType.DeepWater => 1.0f,
        (int)TerrainType.Water => 1.0f,
        _ => 999f  // Can't sail on land
    };
}

// ═══════════════════════════════════════════════════════════════
//  NATIONS
// ═══════════════════════════════════════════════════════════════

public enum NationArchetype
{
    Hegemon,          // USA Alliance — military dominant, global projection
    Commercial,       // Meridian Confederation — trade-focused, tech-rich
    Revolutionary,    // Republic of Valdria — ideological, mass manpower
    Traditionalist,   // Kingdom of Ashenmoor — resource-rich, conservative, defensive
    Industrial,       // Volkren Collective — production powerhouse, steel & tanks
    Naval,            // Thalassian Dominion — island/coastal, navy controls sea lanes
    FreeState,        // Selvara (player default) — tiny, 1 nuke, intelligence-focused
    Survival,         // Generic desperate coalition / minor
    TradeCity,        // Free City of Orinth — rich city-state, no army
    Guerrilla,        // Kaelith Tribes — desert fighters, hard to conquer
    Intelligence,     // Duskhollow Pact — spy networks, neutral facade
    Remnant,          // Ironmarch Remnant — former empire, declining
    IslandNaval,      // Port Serin — island base, submarines
    ResourceCursed,   // Ashfall Compact — uranium-rich, unstable
}

public enum NationTier
{
    Large,  // 6 major powers: 8-12 cities, 5-8 armies
    Small,  // 7 minor nations: 2-5 cities, 1-3 armies
}

// ═══════════════════════════════════════════════════════════════
//  NATION TRAITS — Unique passive abilities per nation
//  Each nation gets 2-3 traits. Engines check for traits and
//  apply modifiers. This is what makes nations play differently.
// ═══════════════════════════════════════════════════════════════

public enum NationTrait
{
    // Military
    CarrierDoctrine,     // +50% carrier attack power, can project air power from sea
    MassConscription,    // +30% infantry count from manpower, faster reinforcement
    FortressDefense,     // +40% defense in own territory, cities take longer to siege
    ArmoredBlitz,        // +25% tank speed and attack, -10% defense (offense-focused)
    NavalSupremacy,      // +30% all naval combat, controls sea trade routes in range
    NuclearDeterrent,    // AI gets -40% war willingness, 1-shot city/army destroyer
    SubmarineWolf,       // Submarines invisible until attacking, +50% sub attack

    // Economic
    TradeEmpire,         // +100% trade route income, can embargo without declaring war
    IndustrialBase,      // +30% production speed, -15% unit cost
    RareEarthMonopoly,   // Can restrict electronics supply to enemies, +50% electronics income
    OilWeapon,           // Can flood/restrict oil to crash/spike prices globally
    SovereignWealth,     // Treasury generates 2% interest per tick, immune to bank runs

    // Intelligence / Diplomatic
    SpyMaster,           // Start with spy depth +2 in all nations, +50% covert op success
    NeutralBroker,       // Can sell intel to all sides, attacking you costs -30 prestige
    GuerrillaResistance, // Units in home territory get ×2 defense, invaders take attrition
    RemnantPride,        // +20% morale but -10% diplomacy (won't accept unfavorable deals)
    NuclearAmbiguity,    // Enemies unsure if you have nukes — intel shows "POSSIBLE NUCLEAR"
    ProliferationTarget, // Everyone runs covert ops against you, but your uranium is ×3 value

    // Survival / Niche
    PorcupineDefense,    // Shore missiles + mines make naval invasion ×3 cost
    UnsiegeableDesert,   // Armies in desert/sand terrain regenerate, enemies take ×2 attrition
    CorporateDiplomacy,  // Can bribe nations directly with treasury, cheaper alliance costs
}

// ═══════════════════════════════════════════════════════════════
//  DIPLOMATIC DISPOSITION — Starting relations between nations
//  Not all nations begin neutral. History creates friends & enemies.
// ═══════════════════════════════════════════════════════════════

public enum DiplomaticStatus
{
    Allied,     // Military alliance, shared intelligence, trade bonus
    Friendly,   // Positive relations, open trade, will defend if asked
    Neutral,    // Default — no obligations
    Wary,       // Suspicious, reduced trade, border patrols
    Hostile,    // Active rivalry, sanctions, may declare war
    AtWar,      // Open conflict
}

public class NationData
{
    public string Id = "";
    public string Name = "";
    public NationArchetype Archetype;
    public NationTier Tier = NationTier.Large;
    public Color NationColor;
    public bool IsPlayer = false;

    // Capital tile position
    public int CapitalX, CapitalY;
    public int ProvinceCount;      // tiles owned

    // Stats
    public float Treasury = 1000f;
    public float Prestige = 30f;
    public bool IsAlive = true;

    // Resource stockpiles (0-100 scale, replenished by territory each tick)
    public float Iron;
    public float Oil;
    public float Uranium;
    public float Electronics;
    public float Manpower;
    public float Food;

    // Stability & war weariness
    public float Stability = 80f;      // 0-100, below 20 = rebellion risk
    public float WarWeariness;         // 0-100, accumulates during wars

    // Traits — unique passive abilities (set from template)
    public List<NationTrait> Traits = new();

    // Government council — advisers, government type
    public CouncilData Council = new();

    // Diplomacy — relations with other nations (key = nation Id)
    public Dictionary<string, DiplomaticStatus> Relations = new();

    // Military Command
    public MilitaryOrder GlobalMilitaryOrder = MilitaryOrder.BorderWatch;
    public int CommandTargetX = -1;
    public int CommandTargetY = -1;

    // Geographic coordinates for map bridge (lon/lat)
    public float CommandTargetLon = float.NaN;
    public float CommandTargetLat = float.NaN;

    // Border polygon for territory rendering (lon/lat pairs)
    public float[][]? BorderPolygon;
}

// ═══════════════════════════════════════════════════════════════
//  CITIES — Territory anchors. Capture cities to conquer nations.
// ═══════════════════════════════════════════════════════════════

public class CityData
{
    public string Id = "";
    public string NationId = "";
    public string Name = "City";
    public int TileX, TileY;
    public float Longitude, Latitude;  // Geographic coords for map bridge
    public bool IsCapital;
    public int Size = 1;  // 1=town, 2=city, 3=capital

    public int HP = 100;
    public int MaxHP => Size switch { 3 => 400, 2 => 200, _ => 100 };

    /// <summary>Control radius in tiles. Bigger cities own more land.</summary>
    public int ControlRadius => Size switch { 3 => 35, 2 => 25, _ => 15 };

    /// <summary>Index in WorldData.Cities list (set during world gen).</summary>
    public int CityIndex;

    // Siege state
    public string? BesiegingNationId;  // null = not under siege
    public int SiegeTurns;
}

// ═══════════════════════════════════════════════════════════════
//  UNIT TYPES — Everything that fights
// ═══════════════════════════════════════════════════════════════

public enum UnitType
{
    // Land
    Infantry,
    MechInfantry,
    Tank,
    Artillery,
    AntiAir,
    MobileRadar,

    // Air
    Fighter,
    Bomber,
    Transport,
    ReconPlane,

    // Naval
    Destroyer,
    Cruiser,
    Carrier,
    Submarine,
    LandingCraft,

    // Special
    Missile,
    NuclearMissile
}

public enum UnitDomain { Land, Air, Naval, Special }

public static class UnitRules
{
    public static UnitDomain GetDomain(UnitType type) => type switch
    {
        UnitType.Infantry => UnitDomain.Land,
        UnitType.MechInfantry => UnitDomain.Land,
        UnitType.Tank => UnitDomain.Land,
        UnitType.Artillery => UnitDomain.Land,
        UnitType.AntiAir => UnitDomain.Land,
        UnitType.MobileRadar => UnitDomain.Land,
        UnitType.Fighter => UnitDomain.Air,
        UnitType.Bomber => UnitDomain.Air,
        UnitType.Transport => UnitDomain.Air,
        UnitType.ReconPlane => UnitDomain.Air,
        UnitType.Destroyer => UnitDomain.Naval,
        UnitType.Cruiser => UnitDomain.Naval,
        UnitType.Carrier => UnitDomain.Naval,
        UnitType.Submarine => UnitDomain.Naval,
        UnitType.LandingCraft => UnitDomain.Naval,
        UnitType.Missile => UnitDomain.Special,
        UnitType.NuclearMissile => UnitDomain.Special,
        _ => UnitDomain.Land
    };

    /// <summary>Combat strength per unit of this type.</summary>
    public static float AttackPower(UnitType type) => type switch
    {
        UnitType.Infantry => 1f,
        UnitType.MechInfantry => 1.5f,
        UnitType.Tank => 5f,
        UnitType.Artillery => 4f,
        UnitType.AntiAir => 2f,
        UnitType.MobileRadar => 0f,
        UnitType.Fighter => 6f,
        UnitType.Bomber => 8f,
        UnitType.ReconPlane => 0f,
        UnitType.Destroyer => 7f,
        UnitType.Cruiser => 9f,
        UnitType.Carrier => 3f,
        UnitType.Submarine => 6f,
        UnitType.Missile => 15f,
        UnitType.NuclearMissile => 100f,
        _ => 1f
    };

    /// <summary>Defense strength per unit.</summary>
    public static float DefensePower(UnitType type) => type switch
    {
        UnitType.Infantry => 1.5f,
        UnitType.MechInfantry => 1.2f,
        UnitType.Tank => 4f,
        UnitType.Artillery => 1f,
        UnitType.AntiAir => 3f,
        UnitType.MobileRadar => 0.5f,
        UnitType.Fighter => 3f,
        UnitType.Bomber => 1.5f,
        UnitType.ReconPlane => 0.5f,
        UnitType.Destroyer => 5f,
        UnitType.Cruiser => 7f,
        UnitType.Carrier => 8f,
        UnitType.Submarine => 2f,
        _ => 1f
    };

    /// <summary>Tiles moved per turn.</summary>
    public static int Speed(UnitType type) => type switch
    {
        UnitType.Infantry => 2,
        UnitType.MechInfantry => 3,
        UnitType.Tank => 4,
        UnitType.Artillery => 1,
        UnitType.MobileRadar => 2,
        UnitType.Fighter => 8,
        UnitType.Bomber => 6,
        UnitType.ReconPlane => 10,
        UnitType.Destroyer => 5,
        UnitType.Cruiser => 4,
        UnitType.Carrier => 3,
        UnitType.Submarine => 4,
        UnitType.Missile => 12,
        _ => 2
    };
}

// ═══════════════════════════════════════════════════════════════
//  ARMIES — Groups of units that move and fight together.
//  LOD 0: 1 dot per army. LOD 1: 1 dot per 50. LOD 2: 1 dot per 10.
//  LOD 3: every unit rendered with pixel stamp silhouettes.
// ═══════════════════════════════════════════════════════════════

public enum MilitaryOrder
{
    Standby,
    BorderWatch,
    Patrol,
    Stage,
    Attack
}

public enum FormationType
{
    Column,    // Moving in line
    Spread,    // Defensive spread
    Wedge,     // Attack formation
    Circle     // Staging/garrison
}

public class ArmyData
{
    public string Id = "";
    public string NationId = "";
    public string Name = "Army";

    // Composition: UnitType → count of that unit
    public Dictionary<UnitType, int> Composition = new();

    /// <summary>Total troop/vehicle/ship count across all types.</summary>
    public int TotalStrength => Composition.Values.Sum();

    /// <summary>Number of swarm dots to render (1 dot per 10 troops).</summary>
    public int SwarmDotCount => System.Math.Max(1, TotalStrength / 10);

    // Position (tile grid)
    public int TileX, TileY;
    public int TargetTileX, TargetTileY;

    // Smooth pixel position for rendering
    public float PixelX, PixelY;
    public float TargetPixelX, TargetPixelY;

    // State
    public MilitaryOrder CurrentOrder = MilitaryOrder.Standby;
    public FormationType Formation = FormationType.Spread;
    public float Morale = 100f;     // 0-100
    public float Supply = 100f;     // 0-100
    public float Organization = 100f; // 0-100, how well-organized (drops in combat)
    public bool IsAlive = true;

    // Garrison assignment (null = field army)
    public string? GarrisonCityId;

    // Pathfinding
    public List<(int x, int y)>? CurrentPath;
    public int PathIndex;

    /// <summary>Primary domain based on majority composition.</summary>
    public UnitDomain PrimaryDomain
    {
        get
        {
            int land = 0, naval = 0, air = 0;
            foreach (var (type, count) in Composition)
            {
                var domain = UnitRules.GetDomain(type);
                if (domain == UnitDomain.Land) land += count;
                else if (domain == UnitDomain.Naval) naval += count;
                else if (domain == UnitDomain.Air) air += count;
            }
            if (naval > land && naval > air) return UnitDomain.Naval;
            if (air > land && air > naval) return UnitDomain.Air;
            return UnitDomain.Land;
        }
    }

    /// <summary>Total attack strength weighted by unit types.</summary>
    public float TotalAttackPower => Composition.Sum(kv => kv.Value * UnitRules.AttackPower(kv.Key));

    /// <summary>Total defense strength weighted by unit types.</summary>
    public float TotalDefensePower => Composition.Sum(kv => kv.Value * UnitRules.DefensePower(kv.Key));

    /// <summary>Movement speed = slowest unit in the army.</summary>
    public int MoveSpeed => Composition.Count == 0 ? 2 :
        Composition.Keys.Min(t => UnitRules.Speed(t));
}

// ═══════════════════════════════════════════════════════════════
//  LEGACY UNIT DATA — Kept for backward compatibility
// ═══════════════════════════════════════════════════════════════

[System.Obsolete("Use ArmyData instead")]
public class UnitData
{
    public string Id = "";
    public string NationId = "";
    public UnitType Type;
    public int TileX, TileY;
    public float Strength = 1.0f;
    public bool IsAlive = true;
    public MilitaryOrder CurrentOrder = MilitaryOrder.Standby;
    public float PixelX, PixelY;
    public float TargetPixelX, TargetPixelY;
    public float Longitude, Latitude;
    public float TargetLongitude, TargetLatitude;
}

// ═══════════════════════════════════════════════════════════════
//  INTERRUPTS — "The Phone Rings" timed decision system
// ═══════════════════════════════════════════════════════════════

public enum InterruptPriority
{
    Routine,    // Blue border, no pause, 60-90s
    Urgent,     // Orange border, no pause, 30-45s
    Critical    // Red border, auto-pauses sim, 10-20s
}

public class InterruptChoice
{
    public string Label = "";
    public string EffectDescription = "";
}

public class InterruptData
{
    public string Id = "";
    public string Title = "";
    public string Description = "";
    public InterruptPriority Priority;
    public float TimerSeconds;
    public InterruptChoice[] Choices = System.Array.Empty<InterruptChoice>();
    public int DefaultChoiceIndex;  // applied on timeout
}

// ═══════════════════════════════════════════════════════════════
//  GOVERNMENT & COUNCIL — Player's governing body
//  Name and composition changes by nation archetype.
// ═══════════════════════════════════════════════════════════════

public enum GovernmentType
{
    FederalCouncil,         // Hegemon — military-political cabinet
    RevolutionaryCommittee, // Revolutionary — politburo of ideologues
    MerchantSenate,         // Commercial / TradeCity — profit-driven board
    RoyalCourt,             // Traditionalist — hereditary advisers
    CentralCommittee,       // Industrial — technocratic planners
    Admiralty,              // Naval / IslandNaval — naval command
    NationalAssembly,       // FreeState — elected representatives
    WarCouncil,             // Guerrilla — tribal elders and war chiefs
    ShadowCabinet,          // Intelligence — spymasters in the dark
    ImperialCourt,          // Remnant — faded glory bureaucrats
    SurvivalCouncil,        // ResourceCursed / Survival — desperate pragmatists
}

public enum AdviserRole
{
    Military,       // Always present — war, defense, conscription
    Economic,       // Always present — treasury, trade, production
    Intelligence,   // Always present — spies, counter-intel, covert ops
    Diplomatic,     // Always present — treaties, alliances, sanctions
    // Specialist slots vary by government type:
    FleetAdmiral,       // Naval/IslandNaval — naval operations
    PartyCommissar,     // Revolutionary — ideological enforcement
    ChiefEngineer,      // Industrial — production optimization
    CourtChamberlain,   // Traditionalist/Remnant — court intrigue
    TradeGuildmaster,   // Commercial/TradeCity — trade route management
    TribalElder,        // Guerrilla — morale and guerrilla tactics
    Spymaster,          // Intelligence — deep cover operations
    NuclearOfficer,     // FreeState (Selvara) — nuclear deterrent
    ResourceWarden,     // ResourceCursed — resource extraction
}

/// <summary>Council action categories the player can take through their government.</summary>
public enum CouncilActionCategory
{
    Domestic,       // Tax, infrastructure, martial law, elections, suppress dissent
    Military,       // Defense budget, authorize ops, conscription, nuclear auth
    Diplomatic,     // Treaties, declare war, sanctions, request aid
    Intelligence,   // Spy missions, counter-intel, assassinations
}

public class AdviserData
{
    public string Id = "";
    public string Name = "";
    public AdviserRole Role;
    public string NationId = "";

    // Personality affects recommendations
    public float Hawkishness = 0.5f;    // 0=dove, 1=hawk — military aggression
    public float Loyalty = 0.8f;        // 0=traitor, 1=devoted — coup risk
    public float Competence = 0.7f;     // 0=incompetent, 1=genius — action success modifier

    // Current opinion on proposed actions (set by AI each tick)
    public string CurrentAdvice = "";   // e.g., "I recommend caution, sir."
    public bool ApprovesCurrentProposal = true;
}

public class CouncilData
{
    public GovernmentType Type;
    public List<AdviserData> Advisers = new();

    // Policy state (modified by CouncilEngine)
    public float TaxRate = 0.20f;           // 0.0–1.0, affects income and stability
    public float DefenseBudgetPct = 0.30f;  // 0.0–1.0, fraction of income to military
    public bool MartialLawActive = false;
    public bool ConscriptionActive = false;
    public bool NuclearAuthGranted = false;

    /// <summary>Display name for this government body, based on type.</summary>
    public string DisplayName => Type switch
    {
        GovernmentType.FederalCouncil => "Federal Council",
        GovernmentType.RevolutionaryCommittee => "Revolutionary Committee",
        GovernmentType.MerchantSenate => "Merchant Senate",
        GovernmentType.RoyalCourt => "Royal Court",
        GovernmentType.CentralCommittee => "Central Committee",
        GovernmentType.Admiralty => "The Admiralty",
        GovernmentType.NationalAssembly => "National Assembly",
        GovernmentType.WarCouncil => "War Council",
        GovernmentType.ShadowCabinet => "Shadow Cabinet",
        GovernmentType.ImperialCourt => "Imperial Court",
        GovernmentType.SurvivalCouncil => "Survival Council",
        _ => "Council"
    };

    /// <summary>Maps archetype to government type.</summary>
    public static GovernmentType FromArchetype(NationArchetype archetype) => archetype switch
    {
        NationArchetype.Hegemon => GovernmentType.FederalCouncil,
        NationArchetype.Revolutionary => GovernmentType.RevolutionaryCommittee,
        NationArchetype.Commercial => GovernmentType.MerchantSenate,
        NationArchetype.TradeCity => GovernmentType.MerchantSenate,
        NationArchetype.Traditionalist => GovernmentType.RoyalCourt,
        NationArchetype.Industrial => GovernmentType.CentralCommittee,
        NationArchetype.Naval => GovernmentType.Admiralty,
        NationArchetype.IslandNaval => GovernmentType.Admiralty,
        NationArchetype.FreeState => GovernmentType.NationalAssembly,
        NationArchetype.Guerrilla => GovernmentType.WarCouncil,
        NationArchetype.Intelligence => GovernmentType.ShadowCabinet,
        NationArchetype.Remnant => GovernmentType.ImperialCourt,
        NationArchetype.ResourceCursed => GovernmentType.SurvivalCouncil,
        NationArchetype.Survival => GovernmentType.SurvivalCouncil,
        _ => GovernmentType.NationalAssembly
    };

    /// <summary>Returns specialist adviser roles for this government type.</summary>
    public static AdviserRole[] GetSpecialistRoles(GovernmentType type) => type switch
    {
        GovernmentType.FederalCouncil => new[] { AdviserRole.NuclearOfficer },
        GovernmentType.RevolutionaryCommittee => new[] { AdviserRole.PartyCommissar },
        GovernmentType.MerchantSenate => new[] { AdviserRole.TradeGuildmaster },
        GovernmentType.RoyalCourt => new[] { AdviserRole.CourtChamberlain },
        GovernmentType.CentralCommittee => new[] { AdviserRole.ChiefEngineer },
        GovernmentType.Admiralty => new[] { AdviserRole.FleetAdmiral },
        GovernmentType.NationalAssembly => new[] { AdviserRole.NuclearOfficer },
        GovernmentType.WarCouncil => new[] { AdviserRole.TribalElder },
        GovernmentType.ShadowCabinet => new[] { AdviserRole.Spymaster },
        GovernmentType.ImperialCourt => new[] { AdviserRole.CourtChamberlain },
        GovernmentType.SurvivalCouncil => new[] { AdviserRole.ResourceWarden },
        _ => System.Array.Empty<AdviserRole>()
    };
}

// ═══════════════════════════════════════════════════════════════
//  CHARACTERS — VIPs on the map
// ═══════════════════════════════════════════════════════════════

public class CharacterData
{
    public string Id = "";
    public string NationId = "";
    public string Name = "";
    public string Role = "Official";
    public bool IsPlayer = false;

    // Position on map
    public int TileX, TileY;
    public float PixelX, PixelY;
    public float TargetPixelX, TargetPixelY;
    public bool IsMoving = false;

    // Authority Meters (0.0 to 100.0)
    public float TerritoryAuthority = 30f;
    public float WorldAuthority = 20f;
    public float BehindTheScenesAuthority = 40f;

    public float FullAuthorityIndex => (TerritoryAuthority + WorldAuthority + BehindTheScenesAuthority) / 3f;
}
