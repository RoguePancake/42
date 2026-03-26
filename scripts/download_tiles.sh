#!/bin/bash
# Download OSM Standard tiles for zoom levels 0-5 (~50MB)
# Usage: bash scripts/download_tiles.sh [max_zoom]
# Default max_zoom=5. For more detail: bash scripts/download_tiles.sh 7

MAX_ZOOM=${1:-5}
TILE_DIR="assets/map/tiles"
USER_AGENT="FullAuthority/1.0 (game project)"
DELAY=0.1  # Be polite to OSM servers

echo "=== Full Authority Tile Downloader ==="
echo "Downloading OSM Standard tiles zoom 0-$MAX_ZOOM"
echo "Output: $TILE_DIR/"
echo ""

total=0
downloaded=0
skipped=0

for z in $(seq 0 $MAX_ZOOM); do
    max_xy=$(( (1 << z) - 1 ))
    for x in $(seq 0 $max_xy); do
        for y in $(seq 0 $max_xy); do
            total=$((total + 1))
            dir="$TILE_DIR/$z/$x"
            file="$dir/$y.png"

            if [ -f "$file" ]; then
                skipped=$((skipped + 1))
                continue
            fi

            mkdir -p "$dir"
            url="https://tile.openstreetmap.org/$z/$x/$y.png"

            if curl -sf -H "User-Agent: $USER_AGENT" -o "$file" "$url"; then
                downloaded=$((downloaded + 1))
                printf "\r  Zoom %d: downloaded %d tiles..." "$z" "$downloaded"
            else
                echo ""
                echo "  WARN: Failed $url"
            fi

            sleep $DELAY
        done
    done
    count=$(( (1 << z) * (1 << z) ))
    echo ""
    echo "  Zoom $z complete ($count tiles)"
done

echo ""
echo "=== Done ==="
echo "Total: $total tiles | Downloaded: $downloaded | Skipped: $skipped"
echo "Size: $(du -sh $TILE_DIR 2>/dev/null | cut -f1)"
