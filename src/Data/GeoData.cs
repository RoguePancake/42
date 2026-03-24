using Godot;

namespace Warship.Data;

/// <summary>
/// Real-world geographic data for nations, cities, and military bases.
/// All coordinates in (longitude, latitude) — standard geographic order.
/// 
/// These are simplified polygons for game purposes. Real country borders
/// have thousands of points — we keep ~20-40 points per nation for performance.
/// </summary>
public static class GeoData
{
    public class NationGeoProfile
    {
        public string Name = "";
        public NationArchetype Archetype;
        public float CapitalLon, CapitalLat;
        public string CapitalName = "";
        public Color NationColor;
        public (string Name, float Lon, float Lat, int Size)[] Cities = System.Array.Empty<(string, float, float, int)>();
        public float[][] Border = System.Array.Empty<float[]>();
        public (float Lon, float Lat)[] MilitaryBases = System.Array.Empty<(float, float)>();
    }

    public static readonly NationGeoProfile[] Nations = new[]
    {
        // ═══ UNITED STATES — Hegemon ═══
        new NationGeoProfile
        {
            Name = "United States",
            Archetype = NationArchetype.Hegemon,
            CapitalLon = -77.04f, CapitalLat = 38.90f,
            CapitalName = "Washington D.C.",
            NationColor = new Color(0.22f, 0.45f, 0.78f), // Steel blue
            Cities = new[]
            {
                ("New York", -74.00f, 40.71f, 3),
                ("Los Angeles", -118.24f, 34.05f, 3),
                ("Chicago", -87.63f, 41.88f, 2),
                ("Houston", -95.37f, 29.76f, 2),
                ("Phoenix", -112.07f, 33.45f, 2),
                ("Philadelphia", -75.16f, 39.95f, 2),
                ("San Antonio", -98.49f, 29.42f, 1),
                ("San Diego", -117.16f, 32.72f, 1),
                ("Dallas", -96.80f, 32.78f, 2),
                ("San Francisco", -122.42f, 37.77f, 2),
                ("Seattle", -122.33f, 47.61f, 2),
                ("Denver", -104.99f, 39.74f, 1),
                ("Miami", -80.19f, 25.76f, 2),
                ("Atlanta", -84.39f, 33.75f, 2),
                ("Boston", -71.06f, 42.36f, 2),
            },
            // Simplified US continental outline
            Border = new float[][]
            {
                new[] { -124.7f, 48.4f }, new[] { -123.0f, 48.4f }, new[] { -117.0f, 49.0f },
                new[] { -104.0f, 49.0f }, new[] { -95.2f, 49.0f }, new[] { -84.0f, 46.5f },
                new[] { -82.5f, 42.0f }, new[] { -79.0f, 43.0f }, new[] { -76.0f, 44.0f },
                new[] { -67.0f, 45.0f }, new[] { -67.0f, 44.5f }, new[] { -70.0f, 43.0f },
                new[] { -71.0f, 41.0f }, new[] { -74.0f, 40.5f }, new[] { -75.5f, 39.0f },
                new[] { -75.5f, 35.5f }, new[] { -77.0f, 34.5f }, new[] { -81.0f, 31.0f },
                new[] { -81.5f, 25.0f }, new[] { -82.5f, 27.5f }, new[] { -85.0f, 29.5f },
                new[] { -89.0f, 30.0f }, new[] { -94.0f, 29.5f }, new[] { -97.0f, 26.0f },
                new[] { -100.0f, 28.5f }, new[] { -104.0f, 32.0f }, new[] { -107.0f, 32.0f },
                new[] { -111.0f, 31.5f }, new[] { -114.5f, 32.5f }, new[] { -117.5f, 32.5f },
                new[] { -120.0f, 34.0f }, new[] { -121.0f, 36.5f }, new[] { -123.0f, 38.5f },
                new[] { -124.0f, 40.5f }, new[] { -124.5f, 42.0f }, new[] { -124.0f, 46.0f },
            },
            MilitaryBases = new[]
            {
                (-77.04f, 38.90f),    // Pentagon
                (-118.24f, 34.05f),   // LA area
                (-73.96f, 40.77f),    // NYC area
                (-122.42f, 37.77f),   // San Francisco
                (-157.97f, 21.35f),   // Pearl Harbor
                (-97.74f, 30.27f),    // Fort Hood area
            }
        },

        // ═══ CHINA — Commercial League ═══
        new NationGeoProfile
        {
            Name = "China",
            Archetype = NationArchetype.Commercial,
            CapitalLon = 116.41f, CapitalLat = 39.90f,
            CapitalName = "Beijing",
            NationColor = new Color(0.85f, 0.22f, 0.22f), // Chinese red
            Cities = new[]
            {
                ("Shanghai", 121.47f, 31.23f, 3),
                ("Guangzhou", 113.26f, 23.13f, 2),
                ("Shenzhen", 114.06f, 22.54f, 2),
                ("Chengdu", 104.07f, 30.57f, 2),
                ("Wuhan", 114.31f, 30.59f, 2),
                ("Nanjing", 118.80f, 32.06f, 2),
                ("Hangzhou", 120.15f, 30.27f, 2),
                ("Chongqing", 106.55f, 29.56f, 2),
                ("Xi'an", 108.94f, 34.26f, 2),
                ("Harbin", 126.65f, 45.75f, 1),
                ("Hong Kong", 114.17f, 22.28f, 2),
                ("Taipei", 121.56f, 25.03f, 2),
            },
            Border = new float[][]
            {
                new[] { 73.5f, 39.0f }, new[] { 79.0f, 37.0f }, new[] { 87.0f, 49.0f },
                new[] { 97.0f, 48.0f }, new[] { 108.0f, 53.5f }, new[] { 119.0f, 53.5f },
                new[] { 127.0f, 50.0f }, new[] { 131.0f, 48.0f }, new[] { 135.0f, 48.5f },
                new[] { 134.5f, 42.5f }, new[] { 129.0f, 41.0f }, new[] { 125.0f, 40.0f },
                new[] { 121.0f, 39.0f }, new[] { 119.0f, 34.5f }, new[] { 121.0f, 30.0f },
                new[] { 121.5f, 28.0f }, new[] { 118.0f, 24.5f }, new[] { 114.0f, 22.5f },
                new[] { 110.0f, 20.0f }, new[] { 108.0f, 21.0f }, new[] { 106.0f, 22.5f },
                new[] { 98.0f, 21.5f }, new[] { 97.0f, 24.0f }, new[] { 92.0f, 27.0f },
                new[] { 87.0f, 27.5f }, new[] { 79.0f, 32.0f }, new[] { 74.0f, 36.5f },
            },
            MilitaryBases = new[]
            {
                (116.41f, 39.90f),    // Beijing military district
                (121.47f, 31.23f),    // Shanghai naval
                (110.35f, 18.25f),    // Hainan naval base
                (114.06f, 22.54f),    // Shenzhen
            }
        },

        // ═══ RUSSIA — Revolutionary ═══
        new NationGeoProfile
        {
            Name = "Russia",
            Archetype = NationArchetype.Revolutionary,
            CapitalLon = 37.62f, CapitalLat = 55.76f,
            CapitalName = "Moscow",
            NationColor = new Color(0.75f, 0.68f, 0.20f), // Amber/gold
            Cities = new[]
            {
                ("St. Petersburg", 30.32f, 59.93f, 3),
                ("Novosibirsk", 82.92f, 55.01f, 2),
                ("Yekaterinburg", 60.60f, 56.84f, 2),
                ("Kazan", 49.11f, 55.79f, 2),
                ("Nizhny Novgorod", 43.94f, 56.30f, 1),
                ("Chelyabinsk", 61.40f, 55.15f, 1),
                ("Samara", 50.15f, 53.19f, 1),
                ("Vladivostok", 131.89f, 43.12f, 2),
                ("Murmansk", 33.08f, 68.97f, 1),
                ("Volgograd", 44.51f, 48.72f, 1),
            },
            Border = new float[][]
            {
                new[] { 27.0f, 57.0f }, new[] { 28.0f, 60.0f }, new[] { 30.0f, 62.0f },
                new[] { 33.0f, 69.0f }, new[] { 40.0f, 68.0f }, new[] { 45.0f, 68.5f },
                new[] { 60.0f, 69.5f }, new[] { 69.0f, 73.5f }, new[] { 87.0f, 73.0f },
                new[] { 105.0f, 74.0f }, new[] { 120.0f, 73.0f }, new[] { 140.0f, 72.0f },
                new[] { 160.0f, 69.5f }, new[] { 170.0f, 65.0f }, new[] { 180.0f, 66.0f },
                new[] { 170.0f, 60.0f }, new[] { 155.0f, 55.0f }, new[] { 142.0f, 54.0f },
                new[] { 135.0f, 48.5f }, new[] { 131.0f, 48.0f }, new[] { 127.0f, 50.0f },
                new[] { 119.0f, 53.5f }, new[] { 108.0f, 53.5f }, new[] { 97.0f, 48.0f },
                new[] { 87.0f, 49.0f }, new[] { 73.5f, 53.0f }, new[] { 68.0f, 55.0f },
                new[] { 60.0f, 56.0f }, new[] { 55.0f, 54.0f }, new[] { 50.0f, 52.0f },
                new[] { 46.0f, 48.0f }, new[] { 40.0f, 46.0f }, new[] { 38.0f, 47.0f },
                new[] { 36.0f, 50.0f }, new[] { 30.0f, 52.0f }, new[] { 28.0f, 54.0f },
            },
            MilitaryBases = new[]
            {
                (37.62f, 55.76f),     // Moscow
                (30.32f, 59.93f),     // St. Petersburg
                (33.08f, 68.97f),     // Murmansk (Northern Fleet)
                (131.89f, 43.12f),    // Vladivostok (Pacific Fleet)
                (44.51f, 48.72f),     // Volgograd
            }
        },

        // ═══ EUROPEAN UNION — Traditionalist ═══
        new NationGeoProfile
        {
            Name = "European Union",
            Archetype = NationArchetype.Traditionalist,
            CapitalLon = 4.35f, CapitalLat = 50.85f,
            CapitalName = "Brussels",
            NationColor = new Color(0.18f, 0.40f, 0.72f), // EU blue
            Cities = new[]
            {
                ("Paris", 2.35f, 48.86f, 3),
                ("Berlin", 13.41f, 52.52f, 3),
                ("Rome", 12.50f, 41.90f, 2),
                ("Madrid", -3.70f, 40.42f, 2),
                ("Amsterdam", 4.90f, 52.37f, 2),
                ("Vienna", 16.37f, 48.21f, 2),
                ("Munich", 11.58f, 48.14f, 2),
                ("Milan", 9.19f, 45.46f, 2),
                ("Barcelona", 2.17f, 41.39f, 2),
                ("Warsaw", 21.01f, 52.23f, 2),
                ("Prague", 14.42f, 50.08f, 2),
                ("Dublin", -6.26f, 53.35f, 1),
                ("Lisbon", -9.14f, 38.72f, 1),
                ("Athens", 23.73f, 37.98f, 1),
                ("Stockholm", 18.07f, 59.33f, 2),
                ("Helsinki", 24.94f, 60.17f, 1),
                ("Copenhagen", 12.57f, 55.68f, 1),
            },
            // Simplified Western Europe outline
            Border = new float[][]
            {
                new[] { -9.5f, 36.5f }, new[] { -5.5f, 36.0f }, new[] { -1.5f, 36.5f },
                new[] { 3.0f, 37.0f }, new[] { 5.0f, 43.5f }, new[] { 7.5f, 44.0f },
                new[] { 11.0f, 37.0f }, new[] { 15.5f, 38.0f }, new[] { 18.5f, 40.0f },
                new[] { 20.0f, 40.0f }, new[] { 24.0f, 35.0f }, new[] { 26.0f, 41.0f },
                new[] { 29.0f, 41.5f }, new[] { 28.0f, 45.0f }, new[] { 24.0f, 48.0f },
                new[] { 24.0f, 51.0f }, new[] { 23.5f, 54.0f }, new[] { 28.0f, 56.0f },
                new[] { 28.0f, 60.0f }, new[] { 25.0f, 60.5f }, new[] { 26.0f, 65.5f },
                new[] { 25.0f, 69.0f }, new[] { 18.0f, 70.0f }, new[] { 14.0f, 65.0f },
                new[] { 12.0f, 60.0f }, new[] { 8.0f, 58.0f }, new[] { 7.0f, 55.0f },
                new[] { 14.0f, 54.0f }, new[] { 10.0f, 54.0f }, new[] { 5.0f, 53.5f },
                new[] { 3.0f, 51.5f }, new[] { -5.0f, 50.0f }, new[] { -8.0f, 52.0f },
                new[] { -10.0f, 54.0f }, new[] { -6.0f, 55.0f }, new[] { -8.0f, 57.5f },
                new[] { -5.0f, 58.5f }, new[] { -3.0f, 56.0f }, new[] { 1.0f, 52.0f },
                new[] { -1.0f, 50.0f }, new[] { -5.0f, 48.0f }, new[] { -4.0f, 43.0f },
                new[] { -8.0f, 43.0f }, new[] { -9.5f, 42.0f }, new[] { -9.0f, 38.5f },
            },
            MilitaryBases = new[]
            {
                (2.35f, 48.86f),      // Paris
                (13.41f, 52.52f),     // Berlin
                (12.50f, 41.90f),     // Rome
                (-3.70f, 40.42f),     // Madrid
            }
        },

        // ═══ INDIA — Survival Accord ═══
        new NationGeoProfile
        {
            Name = "India",
            Archetype = NationArchetype.Survival,
            CapitalLon = 77.21f, CapitalLat = 28.61f,
            CapitalName = "New Delhi",
            NationColor = new Color(0.25f, 0.72f, 0.40f), // Indian green
            Cities = new[]
            {
                ("Mumbai", 72.88f, 19.08f, 3),
                ("Kolkata", 88.36f, 22.57f, 2),
                ("Chennai", 80.27f, 13.08f, 2),
                ("Bangalore", 77.59f, 12.97f, 2),
                ("Hyderabad", 78.47f, 17.39f, 2),
                ("Ahmedabad", 72.57f, 23.02f, 2),
                ("Pune", 73.86f, 18.52f, 1),
                ("Jaipur", 75.79f, 26.91f, 1),
                ("Lucknow", 80.95f, 26.85f, 1),
                ("Kanpur", 80.35f, 26.45f, 1),
            },
            Border = new float[][]
            {
                new[] { 68.0f, 23.5f }, new[] { 71.0f, 21.0f }, new[] { 72.5f, 19.0f },
                new[] { 74.0f, 15.0f }, new[] { 74.5f, 12.0f }, new[] { 77.0f, 8.0f },
                new[] { 80.0f, 9.0f }, new[] { 80.0f, 13.0f }, new[] { 82.0f, 16.0f },
                new[] { 87.0f, 21.5f }, new[] { 89.0f, 22.0f }, new[] { 92.0f, 21.5f },
                new[] { 97.0f, 28.0f }, new[] { 92.0f, 27.0f }, new[] { 88.0f, 28.0f },
                new[] { 85.0f, 28.5f }, new[] { 81.0f, 30.0f }, new[] { 79.0f, 32.0f },
                new[] { 76.0f, 34.5f }, new[] { 74.5f, 35.5f }, new[] { 71.0f, 34.0f },
                new[] { 69.0f, 29.0f }, new[] { 68.0f, 25.5f },
            },
            MilitaryBases = new[]
            {
                (77.21f, 28.61f),     // New Delhi
                (72.88f, 19.08f),     // Mumbai
                (88.36f, 22.57f),     // Kolkata
                (80.27f, 13.08f),     // Chennai (Eastern Naval Command)
            }
        },

        // ═══ UNITED KINGDOM — FreeState (Player) ═══
        new NationGeoProfile
        {
            Name = "United Kingdom",
            Archetype = NationArchetype.FreeState,
            CapitalLon = -0.12f, CapitalLat = 51.51f,
            CapitalName = "London",
            NationColor = new Color(0.75f, 0.45f, 0.15f), // British orange/gold
            Cities = new[]
            {
                ("Manchester", -2.24f, 53.48f, 2),
                ("Birmingham", -1.90f, 52.49f, 2),
                ("Edinburgh", -3.19f, 55.95f, 2),
                ("Glasgow", -4.25f, 55.86f, 2),
                ("Liverpool", -2.99f, 53.41f, 1),
                ("Bristol", -2.59f, 51.45f, 1),
                ("Cardiff", -3.18f, 51.48f, 1),
                ("Belfast", -5.93f, 54.60f, 1),
            },
            Border = new float[][]
            {
                new[] { -5.7f, 50.0f }, new[] { -3.0f, 50.3f }, new[] { 1.5f, 51.0f },
                new[] { 1.8f, 52.5f }, new[] { 0.5f, 53.0f }, new[] { -0.5f, 54.5f },
                new[] { -1.5f, 55.0f }, new[] { -2.0f, 56.0f }, new[] { -3.0f, 57.0f },
                new[] { -5.0f, 58.5f }, new[] { -6.5f, 58.0f }, new[] { -5.5f, 56.5f },
                new[] { -7.5f, 57.5f }, new[] { -8.0f, 54.5f }, new[] { -6.0f, 54.0f },
                new[] { -5.0f, 53.5f }, new[] { -4.5f, 52.5f }, new[] { -5.0f, 51.5f },
                new[] { -4.2f, 51.0f }, new[] { -5.5f, 50.2f },
            },
            MilitaryBases = new[]
            {
                (-0.12f, 51.51f),     // London
                (-1.08f, 50.80f),     // Portsmouth (Royal Navy)
                (-5.04f, 50.15f),     // Plymouth (Royal Navy)
                (-3.19f, 55.95f),     // Edinburgh (RAF)
            }
        },
    };

    // ═══ TRADE ROUTES — Real shipping lanes ═══
    public static readonly (float FromLon, float FromLat, float ToLon, float ToLat, string Name)[] TradeRoutes = new[]
    {
        // Transatlantic
        (-74.0f, 40.7f, -0.12f, 51.5f, "Transatlantic Express"),
        (-74.0f, 40.7f, 2.35f, 48.9f, "New York - Paris"),

        // Europe internal
        (-0.12f, 51.5f, 2.35f, 48.9f, "London - Paris"),
        (-0.12f, 51.5f, 13.41f, 52.5f, "London - Berlin"),
        (2.35f, 48.9f, 12.50f, 41.9f, "Paris - Rome"),

        // Suez route
        (12.50f, 41.9f, 32.3f, 31.2f, "Mediterranean - Suez"),
        (43.0f, 14.5f, 72.9f, 19.1f, "Red Sea - Mumbai"),

        // Pacific routes
        (121.5f, 31.2f, -122.4f, 37.8f, "Shanghai - San Francisco"),
        (121.5f, 31.2f, 139.7f, 35.7f, "Shanghai - Tokyo"),

        // Indian Ocean
        (72.9f, 19.1f, 114.2f, 22.5f, "Mumbai - Hong Kong"),
        (72.9f, 19.1f, 55.3f, 25.3f, "Mumbai - Dubai"),

        // Russia overland
        (37.6f, 55.8f, 131.9f, 43.1f, "Trans-Siberian"),
    };
}
