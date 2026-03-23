using System.Collections.Generic;
using Godot;

namespace Warship.World;

/// <summary>
/// Real-world geographic data for the 6 playable nations.
/// Coordinates are WGS84 (lat, lon). Used by WorldGenerator in real-map mode
/// to place capitals, cities, and seed territory expansion at correct locations.
/// </summary>
public static class RealWorldData
{
    public record NationGeoData(
        string Name,
        NationArchetypeGeo Archetype,
        double CapitalLat, double CapitalLon,
        string CapitalName,
        Color NationColor,
        CityGeoData[] Cities
    );

    public record CityGeoData(
        string Name,
        double Lat, double Lon,
        int Size // 1=town, 2=city, 3=capital
    );

    public enum NationArchetypeGeo
    {
        Hegemon,        // USA
        Revolutionary,  // China
        Traditionalist, // Russia
        Commercial,     // EU
        Survival,       // India
        FreeState       // UK (player)
    }

    /// <summary>
    /// The 6 nations with real-world capital positions and major cities.
    /// Player starts as UK (FreeState) — the tiny island nation with a nuke.
    /// </summary>
    public static readonly NationGeoData[] Nations = new[]
    {
        // 0: United States — Hegemon
        new NationGeoData(
            "United States", NationArchetypeGeo.Hegemon,
            38.9, -77.0, "Washington D.C.",
            new Color(0.8f, 0.2f, 0.2f),
            new CityGeoData[]
            {
                new("New York", 40.7, -74.0, 2),
                new("Los Angeles", 34.05, -118.24, 2),
                new("Chicago", 41.88, -87.63, 2),
                new("Houston", 29.76, -95.37, 1),
                new("Miami", 25.76, -80.19, 1),
            }
        ),

        // 1: China — Revolutionary
        new NationGeoData(
            "China", NationArchetypeGeo.Revolutionary,
            39.9, 116.4, "Beijing",
            new Color(0.85f, 0.15f, 0.15f),
            new CityGeoData[]
            {
                new("Shanghai", 31.23, 121.47, 2),
                new("Guangzhou", 23.13, 113.26, 2),
                new("Chengdu", 30.57, 104.07, 1),
                new("Wuhan", 30.59, 114.31, 1),
            }
        ),

        // 2: Russia — Traditionalist
        new NationGeoData(
            "Russia", NationArchetypeGeo.Traditionalist,
            55.75, 37.62, "Moscow",
            new Color(0.8f, 0.8f, 0.2f),
            new CityGeoData[]
            {
                new("St. Petersburg", 59.93, 30.32, 2),
                new("Novosibirsk", 55.04, 82.93, 1),
                new("Yekaterinburg", 56.84, 60.60, 1),
                new("Vladivostok", 43.12, 131.87, 1),
            }
        ),

        // 3: European Union — Commercial League
        new NationGeoData(
            "European Union", NationArchetypeGeo.Commercial,
            50.85, 4.35, "Brussels",
            new Color(0.2f, 0.4f, 0.8f),
            new CityGeoData[]
            {
                new("Paris", 48.86, 2.35, 2),
                new("Berlin", 52.52, 13.41, 2),
                new("Rome", 41.90, 12.50, 1),
                new("Madrid", 40.42, -3.70, 1),
                new("Amsterdam", 52.37, 4.90, 1),
            }
        ),

        // 4: India — Survival Accord
        new NationGeoData(
            "India", NationArchetypeGeo.Survival,
            28.61, 77.21, "New Delhi",
            new Color(0.2f, 0.8f, 0.4f),
            new CityGeoData[]
            {
                new("Mumbai", 19.08, 72.88, 2),
                new("Bangalore", 12.97, 77.59, 1),
                new("Kolkata", 22.57, 88.36, 1),
                new("Chennai", 13.08, 80.27, 1),
            }
        ),

        // 5: United Kingdom — FreeState (Player)
        new NationGeoData(
            "United Kingdom", NationArchetypeGeo.FreeState,
            51.51, -0.13, "London",
            new Color(0.8f, 0.5f, 0.1f),
            new CityGeoData[]
            {
                new("Edinburgh", 55.95, -3.19, 1),
                new("Manchester", 53.48, -2.24, 1),
                new("Birmingham", 52.49, -1.90, 1),
            }
        ),
    };

    /// <summary>
    /// Convert all nation data to game grid positions using the configured bounding box.
    /// Returns a list of (nationIndex, gridX, gridY) for capitals.
    /// </summary>
    public static List<(int nationIdx, int gridX, int gridY)> GetCapitalGridPositions(
        MapTileConfig config, int gameMapWidth, int gameMapHeight)
    {
        var result = new List<(int, int, int)>();
        for (int i = 0; i < Nations.Length; i++)
        {
            var nation = Nations[i];
            var (gx, gy) = GeoMapper.LatLonToGameGrid(
                nation.CapitalLat, nation.CapitalLon,
                config.LatMin, config.LonMin, config.LatMax, config.LonMax,
                gameMapWidth, gameMapHeight);
            result.Add((i, gx, gy));
        }
        return result;
    }

    /// <summary>
    /// Get all city grid positions for a nation.
    /// </summary>
    public static List<(string name, int gridX, int gridY, int size)> GetCityGridPositions(
        int nationIdx, MapTileConfig config, int gameMapWidth, int gameMapHeight)
    {
        var nation = Nations[nationIdx];
        var result = new List<(string, int, int, int)>();

        // Capital first
        var (cx, cy) = GeoMapper.LatLonToGameGrid(
            nation.CapitalLat, nation.CapitalLon,
            config.LatMin, config.LonMin, config.LatMax, config.LonMax,
            gameMapWidth, gameMapHeight);
        result.Add((nation.CapitalName, cx, cy, 3));

        // Other cities
        foreach (var city in nation.Cities)
        {
            var (gx, gy) = GeoMapper.LatLonToGameGrid(
                city.Lat, city.Lon,
                config.LatMin, config.LonMin, config.LatMax, config.LonMax,
                gameMapWidth, gameMapHeight);
            result.Add((city.Name, gx, gy, city.Size));
        }

        return result;
    }
}
