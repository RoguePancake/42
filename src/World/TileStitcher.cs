using System;
using System.IO;
using Godot;

namespace Warship.World;

/// <summary>
/// Stitches downloaded map tiles into a single Godot ImageTexture for rendering.
/// Supports compositing a transparent overlay (e.g., OpenRailwayMap) on top.
/// </summary>
public static class TileStitcher
{
    /// <summary>
    /// Stitch all cached tiles for the given config into a single Image.
    /// Returns null if tiles are not yet downloaded.
    /// </summary>
    public static Image? StitchTiles(MapTileConfig config, string cacheBasePath)
    {
        var (minX, minY, maxX, maxY) = GeoMapper.BoundingBoxToTileRange(
            config.LatMin, config.LonMin, config.LatMax, config.LonMax, config.Zoom);

        int tilesWide = maxX - minX + 1;
        int tilesHigh = maxY - minY + 1;
        int srcTile = config.SourceTileSize;

        int totalWidth = tilesWide * srcTile;
        int totalHeight = tilesHigh * srcTile;

        GD.Print($"[TileStitcher] Stitching {tilesWide}x{tilesHigh} tiles → {totalWidth}x{totalHeight}px");

        var stitched = Image.CreateEmpty(totalWidth, totalHeight, false, Image.Format.Rgba8);

        // Fill with ocean blue as fallback
        stitched.Fill(new Color(0.12f, 0.32f, 0.58f));

        int loaded = 0;
        int missing = 0;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                string tilePath = TileDownloader.GetTilePath(cacheBasePath, config.Zoom, x, y);

                if (!File.Exists(tilePath))
                {
                    missing++;
                    continue;
                }

                var tileImg = Image.LoadFromFile(tilePath);
                if (tileImg == null)
                {
                    missing++;
                    continue;
                }

                // Convert to RGBA8 if needed
                if (tileImg.GetFormat() != Image.Format.Rgba8)
                    tileImg.Convert(Image.Format.Rgba8);

                // Blit tile into the stitched image
                int destX = (x - minX) * srcTile;
                int destY = (y - minY) * srcTile;

                stitched.BlitRect(tileImg, new Rect2I(0, 0, srcTile, srcTile), new Vector2I(destX, destY));
                loaded++;
            }
        }

        GD.Print($"[TileStitcher] Base: {loaded} loaded, {missing} missing");

        // Composite overlay if configured
        if (!string.IsNullOrEmpty(config.OverlayUrlTemplate))
        {
            CompositeOverlay(stitched, config, cacheBasePath, minX, minY, maxX, maxY, srcTile);
        }

        return stitched;
    }

    /// <summary>
    /// Composite transparent overlay tiles (e.g., railway lines) on top of the base image.
    /// </summary>
    private static void CompositeOverlay(Image stitched, MapTileConfig config,
        string cacheBasePath, int minX, int minY, int maxX, int maxY, int srcTile)
    {
        int loaded = 0;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                string tilePath = TileDownloader.GetTilePath(cacheBasePath, config.Zoom, x, y, overlay: true);

                if (!File.Exists(tilePath))
                    continue;

                var overlayImg = Image.LoadFromFile(tilePath);
                if (overlayImg == null)
                    continue;

                if (overlayImg.GetFormat() != Image.Format.Rgba8)
                    overlayImg.Convert(Image.Format.Rgba8);

                // Alpha-blend overlay onto stitched image
                int destX = (x - minX) * srcTile;
                int destY = (y - minY) * srcTile;

                stitched.BlitRect(overlayImg, new Rect2I(0, 0, srcTile, srcTile), new Vector2I(destX, destY));
                loaded++;
            }
        }

        GD.Print($"[TileStitcher] Overlay: {loaded} tiles composited");
    }

    /// <summary>
    /// Stitch tiles and scale to match game grid dimensions.
    /// Returns a texture ready for MapManager rendering.
    /// </summary>
    public static ImageTexture? StitchAndScale(MapTileConfig config, string cacheBasePath,
        int gamePixelWidth, int gamePixelHeight)
    {
        var img = StitchTiles(config, cacheBasePath);
        if (img == null) return null;

        // Scale to match game's pixel dimensions
        if (img.GetWidth() != gamePixelWidth || img.GetHeight() != gamePixelHeight)
        {
            GD.Print($"[TileStitcher] Scaling {img.GetWidth()}x{img.GetHeight()} → {gamePixelWidth}x{gamePixelHeight}");
            img.Resize(gamePixelWidth, gamePixelHeight, Image.Interpolation.Bilinear);
        }

        return ImageTexture.CreateFromImage(img);
    }

    /// <summary>
    /// Try to load a pre-bundled stitched map from res:// assets.
    /// This is the production path — tiles are pre-stitched at build time.
    /// </summary>
    public static ImageTexture? LoadBundled(string resourcePath)
    {
        if (!Godot.FileAccess.FileExists(resourcePath))
        {
            GD.Print($"[TileStitcher] No bundled map at {resourcePath}");
            return null;
        }

        var img = Image.LoadFromFile(resourcePath);
        if (img == null)
        {
            GD.Print($"[TileStitcher] Failed to load bundled map");
            return null;
        }

        GD.Print($"[TileStitcher] Loaded bundled map: {img.GetWidth()}x{img.GetHeight()}");
        return ImageTexture.CreateFromImage(img);
    }

    /// <summary>
    /// Save stitched image to disk (for build-time pre-bundling).
    /// </summary>
    public static void SaveStitchedImage(Image img, string outputPath)
    {
        var error = img.SavePng(outputPath);
        if (error == Error.Ok)
            GD.Print($"[TileStitcher] Saved stitched map to {outputPath}");
        else
            GD.PrintErr($"[TileStitcher] Failed to save: {error}");
    }
}
