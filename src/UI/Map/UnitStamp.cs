using Warship.Data;

namespace Warship.UI.Map;

/// <summary>
/// Pixel stamp definitions for each unit type. At close zoom (LOD 3),
/// each individual unit is rendered as a small pixel silhouette instead
/// of a generic dot.
///
/// Scale: 1 pixel = 1 soldier-width at max zoom.
///   Infantry = 1x1 dot, Tank = 3x2 rectangle, Carrier = 20x5 hull.
///   Shapes are simple — rectangles, crosses, lines — the beauty is in the SCALE.
/// </summary>
public static class UnitStamp
{
    // ── Stamp data: relative pixel offsets from unit center ──

    private static readonly (int dx, int dy)[] StampInfantry =
    {
        (0, 0)
    };

    private static readonly (int dx, int dy)[] StampMechInfantry =
    {
        (0, 0), (0, 1)
    };

    // 3x2 rectangle — wide and blocky
    private static readonly (int dx, int dy)[] StampTank =
    {
        (-1, 0), (0, 0), (1, 0),
        (-1, 1), (0, 1), (1, 1)
    };

    // 2x3 tall rectangle — gun barrel
    private static readonly (int dx, int dy)[] StampArtillery =
    {
        (0, -1), (1, -1),
        (0, 0),  (1, 0),
        (0, 1),  (1, 1)
    };

    // 2x2 square with a pip on top
    private static readonly (int dx, int dy)[] StampAntiAir =
    {
        (0, -1),
        (0, 0), (1, 0),
        (0, 1), (1, 1)
    };

    // Diamond shape — radar dish
    private static readonly (int dx, int dy)[] StampMobileRadar =
    {
        (0, -1),
        (-1, 0), (0, 0), (1, 0),
        (0, 1)
    };

    // 5-wide cross — wings + fuselage
    private static readonly (int dx, int dy)[] StampFighter =
    {
                  (0, -2),
                  (0, -1),
        (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0),
                  (0, 1),
                  (0, 2)
    };

    // 7-wide fat cross — heavy bomber
    private static readonly (int dx, int dy)[] StampBomber =
    {
                           (0, -3),
                           (0, -2),
                  (-1, -1),(0, -1),(1, -1),
        (-3, 0), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (3, 0),
                  (-1, 1),(0, 1),(1, 1),
                           (0, 2),
                           (0, 3)
    };

    // 5x3 wide body — cargo plane
    private static readonly (int dx, int dy)[] StampTransport =
    {
        (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0),
        (-1, 1), (0, 1), (1, 1),
        (0, 2)
    };

    // 5-wide cross, thinner — scout
    private static readonly (int dx, int dy)[] StampReconPlane =
    {
                  (0, -1),
        (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0),
                  (0, 1)
    };

    // 8x3 long oval — warship hull
    private static readonly (int dx, int dy)[] StampDestroyer =
    {
              (-3, -1), (-2, -1), (-1, -1), (0, -1), (1, -1), (2, -1),
        (-4, 0), (-3, 0), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (3, 0),
              (-3, 1), (-2, 1), (-1, 1), (0, 1), (1, 1), (2, 1)
    };

    // 12x4 long rectangle — heavy cruiser
    private static readonly (int dx, int dy)[] StampCruiser =
    {
              (-5, -1), (-4, -1), (-3, -1), (-2, -1), (-1, -1), (0, -1), (1, -1), (2, -1), (3, -1), (4, -1),
        (-6, 0), (-5, 0), (-4, 0), (-3, 0), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (3, 0), (4, 0), (5, 0),
        (-6, 1), (-5, 1), (-4, 1), (-3, 1), (-2, 1), (-1, 1), (0, 1), (1, 1), (2, 1), (3, 1), (4, 1), (5, 1),
              (-5, 2), (-4, 2), (-3, 2), (-2, 2), (-1, 2), (0, 2), (1, 2), (2, 2), (3, 2), (4, 2)
    };

    // 20x5 massive hull — aircraft carrier with deck stripe
    private static readonly (int dx, int dy)[] StampCarrier;

    // 6x2 thin rectangle — submarine
    private static readonly (int dx, int dy)[] StampSubmarine =
    {
        (-3, 0), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0),
        (-2, 1), (-1, 1), (0, 1), (1, 1)
    };

    // 5x3 rectangle — landing craft
    private static readonly (int dx, int dy)[] StampLandingCraft =
    {
        (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0),
        (-2, 1), (-1, 1), (0, 1), (1, 1), (2, 1),
        (-1, 2), (0, 2), (1, 2)
    };

    // 1x4 vertical line — arrow/missile
    private static readonly (int dx, int dy)[] StampMissile =
    {
        (0, -2), (0, -1), (0, 0), (0, 1)
    };

    // 2x6 thick vertical — ICBM
    private static readonly (int dx, int dy)[] StampNuclearMissile =
    {
        (0, -3), (1, -3),
        (0, -2), (1, -2),
        (0, -1), (1, -1),
        (0, 0),  (1, 0),
        (0, 1),  (1, 1),
        (0, 2),  (1, 2)
    };

    static UnitStamp()
    {
        // Build carrier stamp procedurally — 20x5 hull
        var list = new System.Collections.Generic.List<(int, int)>();
        for (int x = -10; x < 10; x++)
        {
            for (int y = -2; y <= 2; y++)
            {
                // Taper bow and stern
                if ((x == -10 || x == 9) && (y == -2 || y == 2)) continue;
                list.Add((x, y));
            }
        }
        StampCarrier = list.ToArray();
    }

    /// <summary>
    /// Get the pixel stamp for a unit type. Returns array of (dx, dy)
    /// offsets relative to the unit's center pixel.
    /// </summary>
    public static (int dx, int dy)[] GetStamp(UnitType type) => type switch
    {
        UnitType.Infantry => StampInfantry,
        UnitType.MechInfantry => StampMechInfantry,
        UnitType.Tank => StampTank,
        UnitType.Artillery => StampArtillery,
        UnitType.AntiAir => StampAntiAir,
        UnitType.MobileRadar => StampMobileRadar,
        UnitType.Fighter => StampFighter,
        UnitType.Bomber => StampBomber,
        UnitType.Transport => StampTransport,
        UnitType.ReconPlane => StampReconPlane,
        UnitType.Destroyer => StampDestroyer,
        UnitType.Cruiser => StampCruiser,
        UnitType.Carrier => StampCarrier,
        UnitType.Submarine => StampSubmarine,
        UnitType.LandingCraft => StampLandingCraft,
        UnitType.Missile => StampMissile,
        UnitType.NuclearMissile => StampNuclearMissile,
        _ => StampInfantry
    };

    /// <summary>
    /// Bounding box size in pixels for a unit type (for click detection).
    /// </summary>
    public static (int w, int h) GetSize(UnitType type) => type switch
    {
        UnitType.Infantry => (1, 1),
        UnitType.MechInfantry => (1, 2),
        UnitType.Tank => (3, 2),
        UnitType.Artillery => (2, 3),
        UnitType.AntiAir => (2, 3),
        UnitType.MobileRadar => (3, 3),
        UnitType.Fighter => (5, 5),
        UnitType.Bomber => (7, 7),
        UnitType.Transport => (5, 3),
        UnitType.ReconPlane => (5, 3),
        UnitType.Destroyer => (9, 3),
        UnitType.Cruiser => (13, 4),
        UnitType.Carrier => (20, 5),
        UnitType.Submarine => (6, 2),
        UnitType.LandingCraft => (5, 3),
        UnitType.Missile => (1, 4),
        UnitType.NuclearMissile => (2, 6),
        _ => (1, 1)
    };
}
