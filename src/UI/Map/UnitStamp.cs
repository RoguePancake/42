using System.Collections.Generic;
using Warship.Data;

namespace Warship.UI.Map;

/// <summary>
/// Pixel-art silhouette definitions for each unit type.
/// At LOD 3 (close zoom), each unit is rendered as a small pixel stamp
/// instead of a generic dot.
///
/// Scale: 1 pixel = 1 soldier-width at max zoom.
/// Shapes are simple — rectangles, crosses, lines.
/// </summary>
public static class UnitStamp
{
    // Infantry: 1x1 dot
    private static readonly (int dx, int dy)[] Infantry = { (0, 0) };

    // MechInfantry: 1x2 vertical
    private static readonly (int dx, int dy)[] MechInfantry = { (0, 0), (0, 1) };

    // Tank: 3x2 rectangle
    private static readonly (int dx, int dy)[] Tank =
    {
        (-1, 0), (0, 0), (1, 0),
        (-1, 1), (0, 1), (1, 1),
    };

    // Artillery: 2x3 tall
    private static readonly (int dx, int dy)[] Artillery =
    {
        (0, -1), (1, -1),
        (0, 0),  (1, 0),
        (0, 1),  (1, 1),
    };

    // AntiAir: 2x2 with pip
    private static readonly (int dx, int dy)[] AntiAir =
    {
                 (0, -1),
        (0, 0),  (1, 0),
        (0, 1),  (1, 1),
    };

    // MobileRadar: diamond
    private static readonly (int dx, int dy)[] MobileRadar =
    {
                  (0, -1),
        (-1, 0),  (0, 0), (1, 0),
                  (0, 1),
    };

    // Fighter: cross (5-wide wings + fuselage)
    private static readonly (int dx, int dy)[] Fighter =
    {
                           (0, -2),
                           (0, -1),
        (-2, 0), (-1, 0),  (0, 0), (1, 0), (2, 0),
                           (0, 1),
                           (0, 2),
    };

    // Bomber: fat cross (7-wide)
    private static readonly (int dx, int dy)[] Bomber =
    {
                                    (0, -3),
                                    (0, -2),
                  (-1, -1),         (0, -1), (1, -1),
        (-3, 0), (-2, 0), (-1, 0), (0, 0),  (1, 0), (2, 0), (3, 0),
                  (-1, 1),         (0, 1),  (1, 1),
                                    (0, 2),
                                    (0, 3),
    };

    // Transport: 5x3 cargo plane
    private static readonly (int dx, int dy)[] Transport =
    {
        (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0),
                 (-1, 1), (0, 1), (1, 1),
                          (0, 2),
    };

    // ReconPlane: thin cross (5-wide)
    private static readonly (int dx, int dy)[] ReconPlane =
    {
                           (0, -1),
        (-2, 0), (-1, 0),  (0, 0), (1, 0), (2, 0),
                           (0, 1),
    };

    // Destroyer: 8x3 hull
    private static readonly (int dx, int dy)[] Destroyer =
    {
              (-3, -1), (-2, -1), (-1, -1), (0, -1), (1, -1), (2, -1),
        (-4, 0), (-3, 0), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (3, 0),
              (-3, 1), (-2, 1), (-1, 1), (0, 1), (1, 1), (2, 1),
    };

    // Cruiser: 12x4 heavy warship
    private static readonly (int dx, int dy)[] Cruiser =
    {
              (-5, -1), (-4, -1), (-3, -1), (-2, -1), (-1, -1), (0, -1), (1, -1), (2, -1), (3, -1), (4, -1),
        (-6, 0), (-5, 0), (-4, 0), (-3, 0), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0), (3, 0), (4, 0), (5, 0),
        (-6, 1), (-5, 1), (-4, 1), (-3, 1), (-2, 1), (-1, 1), (0, 1), (1, 1), (2, 1), (3, 1), (4, 1), (5, 1),
              (-5, 2), (-4, 2), (-3, 2), (-2, 2), (-1, 2), (0, 2), (1, 2), (2, 2), (3, 2), (4, 2),
    };

    // Carrier: 20x5 (built procedurally)
    private static readonly (int dx, int dy)[] Carrier;

    // Submarine: 6x2 thin
    private static readonly (int dx, int dy)[] Submarine =
    {
        (-3, 0), (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0),
                 (-2, 1), (-1, 1), (0, 1), (1, 1),
    };

    // LandingCraft: 5x3
    private static readonly (int dx, int dy)[] LandingCraft =
    {
        (-2, 0), (-1, 0), (0, 0), (1, 0), (2, 0),
        (-2, 1), (-1, 1), (0, 1), (1, 1), (2, 1),
                 (-1, 2), (0, 2), (1, 2),
    };

    // Missile: 1x4 vertical
    private static readonly (int dx, int dy)[] Missile =
    {
        (0, -2), (0, -1), (0, 0), (0, 1),
    };

    // NuclearMissile: 2x6 thick ICBM
    private static readonly (int dx, int dy)[] NuclearMissile =
    {
        (0, -3), (1, -3),
        (0, -2), (1, -2),
        (0, -1), (1, -1),
        (0, 0),  (1, 0),
        (0, 1),  (1, 1),
        (0, 2),  (1, 2),
    };

    static UnitStamp()
    {
        // Build carrier hull: 20x5, tapered bow/stern
        var hull = new List<(int, int)>();
        for (int x = -10; x < 10; x++)
            for (int y = -2; y <= 2; y++)
            {
                if ((x == -10 || x == 9) && (y == -2 || y == 2)) continue;
                hull.Add((x, y));
            }
        Carrier = hull.ToArray();
    }

    public static (int dx, int dy)[] GetStamp(UnitType type) => type switch
    {
        UnitType.Infantry => Infantry,
        UnitType.MechInfantry => MechInfantry,
        UnitType.Tank => Tank,
        UnitType.Artillery => Artillery,
        UnitType.AntiAir => AntiAir,
        UnitType.MobileRadar => MobileRadar,
        UnitType.Fighter => Fighter,
        UnitType.Bomber => Bomber,
        UnitType.Transport => Transport,
        UnitType.ReconPlane => ReconPlane,
        UnitType.Destroyer => Destroyer,
        UnitType.Cruiser => Cruiser,
        UnitType.Carrier => Carrier,
        UnitType.Submarine => Submarine,
        UnitType.LandingCraft => LandingCraft,
        UnitType.Missile => Missile,
        UnitType.NuclearMissile => NuclearMissile,
        _ => Infantry,
    };

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
        _ => (1, 1),
    };
}
