namespace Warship.World;

/// <summary>
/// Configuration for real-world map tile sources.
///
/// FREE tile sources that show roads, rivers, and railways:
///
/// 1. OpenStreetMap Standard - Best all-rounder
///    URL: https://tile.openstreetmap.org/{z}/{x}/{y}.png
///    License: ODbL (data) + CC-BY-SA 2.0 (tiles)
///    Shows: Roads, rivers, railways, cities, terrain coloring
///    Note: tile.openstreetmap.org has strict usage policy - for production,
///          self-host tiles or use a permissive provider.
///
/// 2. OpenTopoMap - Beautiful topographic style
///    URL: https://tile.opentopomap.org/{z}/{x}/{y}.png
///    License: CC-BY-SA
///    Shows: Elevation contours, rivers, roads, railways
///
/// 3. Stamen/Stadia Terrain - Artistic terrain visualization
///    URL: https://tiles.stadiamaps.com/tiles/stamen_terrain/{z}/{x}/{y}.png
///    License: CC-BY 3.0 (needs free API key for Stadia)
///    Shows: Terrain shading, roads, water features
///
/// 4. CartoDB Voyager - Clean modern style
///    URL: https://basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}.png
///    License: CC-BY 3.0
///    Shows: Roads, rivers, place names, clean colors
///
/// 5. OpenRailwayMap (overlay) - Railway detail overlay
///    URL: https://tiles.openrailwaymap.org/standard/{z}/{x}/{y}.png
///    License: CC-BY-SA 2.0
///    Shows: Transparent overlay with railway lines only
///    Use: Composite on top of a base map for railway detail
///
/// ZOOM LEVEL GUIDE (for full world map):
///   z=3: Continental outlines only (8x8 = 64 tiles)
///   z=4: Country shapes, major rivers (16x16 = 256 tiles)
///   z=5: Countries clear, major cities, large rivers (32x32 = 1024 tiles) ★ RECOMMENDED
///   z=6: Medium cities, visible roads, railways (64x64 = 4096 tiles)
///   z=7: Detailed roads, all railways (128x128 = 16384 tiles, ~1GB)
///
/// For a strategy game, z=5 gives the best balance of detail vs file size.
/// At z=5 the full world is 32x32 tiles = 8192x8192 pixels.
/// </summary>
public class MapTileConfig
{
    /// <summary>Tile server URL template. Use {z}, {x}, {y} placeholders.</summary>
    public string TileUrlTemplate { get; set; } = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";

    /// <summary>Optional overlay URL (e.g., OpenRailwayMap for train lines).</summary>
    public string? OverlayUrlTemplate { get; set; }

    /// <summary>Zoom level. 5 recommended for full world strategy game.</summary>
    public int Zoom { get; set; } = 5;

    /// <summary>Source tile size in pixels (standard OSM = 256).</summary>
    public int SourceTileSize { get; set; } = 256;

    /// <summary>Bounding box - minimum latitude (south edge).</summary>
    public double LatMin { get; set; } = -56.0;  // Exclude Antarctica

    /// <summary>Bounding box - maximum latitude (north edge).</summary>
    public double LatMax { get; set; } = 72.0;   // Include Scandinavia/Russia

    /// <summary>Bounding box - minimum longitude (west edge).</summary>
    public double LonMin { get; set; } = -180.0;

    /// <summary>Bounding box - maximum longitude (east edge).</summary>
    public double LonMax { get; set; } = 180.0;

    /// <summary>User-Agent header for tile requests (required by OSM policy).</summary>
    public string UserAgent { get; set; } = "WARSHIP-Game/1.0 (github.com/warship-game)";

    /// <summary>Delay between tile downloads in milliseconds (respect server policies).</summary>
    public int DownloadDelayMs { get; set; } = 100;

    /// <summary>Attribution text (REQUIRED by license for display in-game).</summary>
    public string Attribution => TileUrlTemplate.Contains("openstreetmap")
        ? "© OpenStreetMap contributors"
        : TileUrlTemplate.Contains("opentopomap")
            ? "© OpenTopoMap (CC-BY-SA)"
            : TileUrlTemplate.Contains("stadiamaps")
                ? "Map tiles by Stamen Design (CC-BY 3.0), hosted by Stadia Maps"
                : TileUrlTemplate.Contains("cartocdn")
                    ? "© CARTO (CC-BY 3.0)"
                    : "Map data © OpenStreetMap contributors";

    /// <summary>Local directory for cached tiles (relative to user:// in Godot).</summary>
    public string CacheDir { get; set; } = "map_cache";

    /// <summary>Pre-bundled stitched map image path (res:// asset).</summary>
    public string BundledMapPath { get; set; } = "res://assets/tilesets/real_map.png";

    // ── Preset Configs ──

    public static MapTileConfig OpenStreetMap() => new()
    {
        TileUrlTemplate = "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
        Zoom = 5
    };

    public static MapTileConfig OpenTopoMap() => new()
    {
        TileUrlTemplate = "https://tile.opentopomap.org/{z}/{x}/{y}.png",
        Zoom = 5
    };

    public static MapTileConfig CartoVoyager() => new()
    {
        TileUrlTemplate = "https://basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}.png",
        Zoom = 5
    };

    public static MapTileConfig WithRailwayOverlay(MapTileConfig baseConfig)
    {
        baseConfig.OverlayUrlTemplate = "https://tiles.openrailwaymap.org/standard/{z}/{x}/{y}.png";
        return baseConfig;
    }
}
