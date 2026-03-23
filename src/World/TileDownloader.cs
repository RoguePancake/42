using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Warship.World;

/// <summary>
/// Downloads slippy map tiles from OSM-compatible tile servers and caches them locally.
/// Pure C# — no Godot dependency. Can be used at build time or runtime.
///
/// Usage:
///   var config = MapTileConfig.OpenStreetMap();
///   var result = await TileDownloader.DownloadTilesAsync(config, "/path/to/cache");
///   // result.TotalTiles, result.Downloaded, result.Cached
/// </summary>
public static class TileDownloader
{
    public class DownloadResult
    {
        public int TotalTiles { get; set; }
        public int Downloaded { get; set; }
        public int Cached { get; set; }
        public int Failed { get; set; }
        public int MinTileX { get; set; }
        public int MinTileY { get; set; }
        public int MaxTileX { get; set; }
        public int MaxTileY { get; set; }
        public string CacheDir { get; set; } = "";
    }

    /// <summary>
    /// Download all tiles needed for the configured bounding box and zoom level.
    /// Skips tiles that are already cached on disk.
    /// </summary>
    public static async Task<DownloadResult> DownloadTilesAsync(
        MapTileConfig config, string cacheBasePath, Action<string>? log = null)
    {
        var (minX, minY, maxX, maxY) = GeoMapper.BoundingBoxToTileRange(
            config.LatMin, config.LonMin, config.LatMax, config.LonMax, config.Zoom);

        var result = new DownloadResult
        {
            MinTileX = minX, MinTileY = minY,
            MaxTileX = maxX, MaxTileY = maxY,
            CacheDir = cacheBasePath
        };

        int tilesWide = maxX - minX + 1;
        int tilesHigh = maxY - minY + 1;
        result.TotalTiles = tilesWide * tilesHigh;

        log?.Invoke($"[TileDownloader] Zoom {config.Zoom}: {tilesWide}x{tilesHigh} = {result.TotalTiles} tiles");
        log?.Invoke($"[TileDownloader] Tile range: X=[{minX}..{maxX}], Y=[{minY}..{maxY}]");

        // Ensure cache directory exists
        string tileDir = Path.Combine(cacheBasePath, $"z{config.Zoom}");
        Directory.CreateDirectory(tileDir);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", config.UserAgent);
        client.Timeout = TimeSpan.FromSeconds(30);

        // Download base tiles
        await DownloadTileSet(config.TileUrlTemplate, config.Zoom, minX, minY, maxX, maxY,
            tileDir, "base", client, config.DownloadDelayMs, result, log);

        // Download overlay tiles if configured
        if (!string.IsNullOrEmpty(config.OverlayUrlTemplate))
        {
            string overlayDir = Path.Combine(cacheBasePath, $"z{config.Zoom}_overlay");
            Directory.CreateDirectory(overlayDir);
            await DownloadTileSet(config.OverlayUrlTemplate, config.Zoom, minX, minY, maxX, maxY,
                overlayDir, "overlay", client, config.DownloadDelayMs, result, log);
        }

        log?.Invoke($"[TileDownloader] Done: {result.Downloaded} downloaded, {result.Cached} cached, {result.Failed} failed");
        return result;
    }

    private static async Task DownloadTileSet(
        string urlTemplate, int zoom, int minX, int minY, int maxX, int maxY,
        string dir, string label, HttpClient client, int delayMs,
        DownloadResult result, Action<string>? log)
    {
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                string filename = $"{x}_{y}.png";
                string filePath = Path.Combine(dir, filename);

                if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
                {
                    result.Cached++;
                    continue;
                }

                string url = urlTemplate
                    .Replace("{z}", zoom.ToString())
                    .Replace("{x}", x.ToString())
                    .Replace("{y}", y.ToString());

                // Some servers use {s} for subdomains (a, b, c)
                url = url.Replace("{s}", ((x + y) % 3) switch { 0 => "a", 1 => "b", _ => "c" });

                try
                {
                    byte[] data = await client.GetByteArrayAsync(url);
                    await File.WriteAllBytesAsync(filePath, data);
                    result.Downloaded++;

                    if (result.Downloaded % 50 == 0)
                        log?.Invoke($"[TileDownloader] {label}: {result.Downloaded + result.Cached}/{result.TotalTiles}...");

                    if (delayMs > 0)
                        await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    log?.Invoke($"[TileDownloader] Failed {label} tile {x},{y}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Get the file path for a specific cached tile.
    /// </summary>
    public static string GetTilePath(string cacheBasePath, int zoom, int x, int y, bool overlay = false)
    {
        string dir = overlay
            ? Path.Combine(cacheBasePath, $"z{zoom}_overlay")
            : Path.Combine(cacheBasePath, $"z{zoom}");
        return Path.Combine(dir, $"{x}_{y}.png");
    }
}
