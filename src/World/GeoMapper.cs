using System;

namespace Warship.World;

/// <summary>
/// Converts between geographic coordinates (lat/lon), slippy map tile coordinates,
/// and game grid coordinates. Uses Web Mercator projection (EPSG:3857).
/// </summary>
public static class GeoMapper
{
    /// <summary>
    /// Convert latitude/longitude to slippy map tile X/Y at a given zoom level.
    /// Returns fractional tile coordinates (not floored).
    /// </summary>
    public static (double tileX, double tileY) LatLonToTileFrac(double lat, double lon, int zoom)
    {
        int n = 1 << zoom;
        double x = (lon + 180.0) / 360.0 * n;
        double latRad = lat * Math.PI / 180.0;
        double y = (1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * n;
        return (x, y);
    }

    /// <summary>
    /// Convert latitude/longitude to integer slippy map tile X/Y.
    /// </summary>
    public static (int tileX, int tileY) LatLonToTile(double lat, double lon, int zoom)
    {
        var (fx, fy) = LatLonToTileFrac(lat, lon, zoom);
        return ((int)Math.Floor(fx), (int)Math.Floor(fy));
    }

    /// <summary>
    /// Convert slippy map tile X/Y back to the top-left corner lat/lon.
    /// </summary>
    public static (double lat, double lon) TileToLatLon(int tileX, int tileY, int zoom)
    {
        int n = 1 << zoom;
        double lon = (double)tileX / n * 360.0 - 180.0;
        double latRad = Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * tileY / n)));
        double lat = latRad * 180.0 / Math.PI;
        return (lat, lon);
    }

    /// <summary>
    /// Given a geographic bounding box and zoom level, compute the tile range needed.
    /// Returns (minTileX, minTileY, maxTileX, maxTileY) inclusive.
    /// </summary>
    public static (int minX, int minY, int maxX, int maxY) BoundingBoxToTileRange(
        double latMin, double lonMin, double latMax, double lonMax, int zoom)
    {
        // Top-left corner (max lat, min lon)
        var (tlX, tlY) = LatLonToTile(latMax, lonMin, zoom);
        // Bottom-right corner (min lat, max lon)
        var (brX, brY) = LatLonToTile(latMin, lonMax, zoom);

        return (tlX, tlY, brX, brY);
    }

    /// <summary>
    /// Convert a lat/lon coordinate to a pixel position within the stitched map image.
    /// The stitched image covers tiles from (minTileX, minTileY) to (maxTileX, maxTileY).
    /// Each tile is 256x256 pixels in the source.
    /// </summary>
    public static (double pixelX, double pixelY) LatLonToStitchedPixel(
        double lat, double lon, int zoom,
        int minTileX, int minTileY, int tilePixelSize = 256)
    {
        var (fx, fy) = LatLonToTileFrac(lat, lon, zoom);
        double px = (fx - minTileX) * tilePixelSize;
        double py = (fy - minTileY) * tilePixelSize;
        return (px, py);
    }

    /// <summary>
    /// Convert a lat/lon coordinate to game grid coordinates (tileX, tileY in the game's grid).
    /// Uses the configured map dimensions and bounding box.
    /// </summary>
    public static (int gridX, int gridY) LatLonToGameGrid(
        double lat, double lon,
        double latMin, double lonMin, double latMax, double lonMax,
        int gameMapWidth, int gameMapHeight)
    {
        // Normalize to 0..1 within the bounding box
        double normX = (lon - lonMin) / (lonMax - lonMin);
        // Latitude is inverted (higher lat = lower Y in screen coords)
        double normY = (latMax - lat) / (latMax - latMin);

        int gx = (int)Math.Floor(normX * gameMapWidth);
        int gy = (int)Math.Floor(normY * gameMapHeight);

        // Clamp
        gx = Math.Clamp(gx, 0, gameMapWidth - 1);
        gy = Math.Clamp(gy, 0, gameMapHeight - 1);

        return (gx, gy);
    }

    /// <summary>
    /// Convert game grid coordinates back to approximate lat/lon (center of cell).
    /// </summary>
    public static (double lat, double lon) GameGridToLatLon(
        int gridX, int gridY,
        double latMin, double lonMin, double latMax, double lonMax,
        int gameMapWidth, int gameMapHeight)
    {
        double normX = (gridX + 0.5) / gameMapWidth;
        double normY = (gridY + 0.5) / gameMapHeight;

        double lon = lonMin + normX * (lonMax - lonMin);
        double lat = latMax - normY * (latMax - latMin);

        return (lat, lon);
    }
}
