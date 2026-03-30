using Godot;
using System;
using Warship.Core;
using Warship.Data;
using Warship.World;

namespace Warship.UI.HUD;

/// <summary>
/// Minimap in the bottom-right corner. Shows the entire world as a small
/// baked texture with a white rectangle showing the camera viewport.
/// Click on the minimap to jump the camera there.
///
/// Baked once when the world loads. Buildings and squads drawn as colored dots
/// on top each frame.
/// </summary>
public partial class Minimap : Control
{
    private const int MinimapSize = 180; // pixel size of the minimap square

    private WorldData? _world;
    private ImageTexture? _terrainTexture;
    private float _scaleX, _scaleY; // world tiles → minimap pixels

    public override void _Ready()
    {
        EventBus.Instance?.Subscribe<WorldReadyEvent>(_ =>
        {
            _world = WorldStateManager.Instance?.World;
            if (_world != null) BakeTerrainTexture();
        });

        CustomMinimumSize = new Vector2(MinimapSize, MinimapSize);
        MouseFilter = MouseFilterEnum.Stop; // capture clicks
    }

    private void BakeTerrainTexture()
    {
        if (_world?.TerrainMap == null) return;

        int w = _world.MapWidth, h = _world.MapHeight;
        _scaleX = (float)MinimapSize / w;
        _scaleY = (float)MinimapSize / h;

        // Bake terrain into a small image (1 pixel per tile would be 512x512,
        // so we downsample to MinimapSize x MinimapSize)
        var img = Image.CreateEmpty(MinimapSize, MinimapSize, false, Image.Format.Rgba8);

        for (int py = 0; py < MinimapSize; py++)
        {
            for (int px = 0; px < MinimapSize; px++)
            {
                int tx = (int)(px / _scaleX);
                int ty = (int)(py / _scaleY);
                tx = Math.Clamp(tx, 0, w - 1);
                ty = Math.Clamp(ty, 0, h - 1);

                int terrain = _world.TerrainMap[tx + ty * w];
                img.SetPixel(px, py, TerrainInfo.GetColor(terrain));
            }
        }

        _terrainTexture = ImageTexture.CreateFromImage(img);
        GD.Print("[Minimap] Baked terrain texture.");
    }

    public override void _GuiInput(InputEvent e)
    {
        if (_world == null) return;

        // Click on minimap → move camera
        if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            JumpCameraTo(mb.Position);
            AcceptEvent();
        }
        // Drag on minimap
        if (e is InputEventMouseMotion mm && Input.IsMouseButtonPressed(MouseButton.Left))
        {
            JumpCameraTo(mm.Position);
            AcceptEvent();
        }
    }

    private void JumpCameraTo(Vector2 localPos)
    {
        if (_world == null || _scaleX <= 0) return;

        // Convert minimap pixel to world tile
        var vpSize = GetViewportRect().Size;
        float mmX = localPos.X - (vpSize.X - MinimapSize - 10);
        float mmY = localPos.Y - (vpSize.Y - MinimapSize - 10);

        int tileX = (int)(mmX / _scaleX);
        int tileY = (int)(mmY / _scaleY);
        tileX = Math.Clamp(tileX, 0, _world.MapWidth - 1);
        tileY = Math.Clamp(tileY, 0, _world.MapHeight - 1);

        // Find camera and move it
        var camera = GetViewport().GetCamera2D();
        if (camera is UI.Map.MapCamera mc)
            mc.CenterOnTile(tileX, tileY);
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_world == null || _terrainTexture == null) return;

        var vpSize = GetViewportRect().Size;
        float mmX = vpSize.X - MinimapSize - 10;
        float mmY = vpSize.Y - MinimapSize - 10;

        // Background border
        DrawRect(new Rect2(mmX - 2, mmY - 2, MinimapSize + 4, MinimapSize + 4),
            new Color(0.15f, 0.15f, 0.25f, 0.9f));

        // Terrain texture
        DrawTextureRect(_terrainTexture, new Rect2(mmX, mmY, MinimapSize, MinimapSize), false);

        // Draw buildings as colored dots
        foreach (var bld in _world.Buildings)
        {
            float px = mmX + bld.TileX * _scaleX;
            float py = mmY + bld.TileY * _scaleY;
            Color c = BuildingInfo.GetColor(bld.Type);
            DrawRect(new Rect2(px - 1, py - 1, 3, 3), c);
        }

        // Draw squads as bright dots
        foreach (var squad in _world.Squads)
        {
            if (!squad.IsAlive) continue;
            float px = mmX + squad.TileX * _scaleX;
            float py = mmY + squad.TileY * _scaleY;
            DrawRect(new Rect2(px - 1, py - 1, 3, 3), Colors.LimeGreen);
        }

        // Draw player as gold dot
        float playerPx = mmX + _world.Player.TileX * _scaleX;
        float playerPy = mmY + _world.Player.TileY * _scaleY;
        DrawRect(new Rect2(playerPx - 2, playerPy - 2, 4, 4), Colors.Gold);

        // Draw camera viewport rectangle
        var camera = GetViewport().GetCamera2D();
        if (camera != null)
        {
            float zoom = camera.Zoom.X;
            float camHalfW = vpSize.X / (2f * zoom);
            float camHalfH = vpSize.Y / (2f * zoom);
            var camPos = camera.GlobalPosition;

            float ts = TerrainGenerator.TileSize;
            float rx = mmX + (camPos.X - camHalfW) / ts * _scaleX;
            float ry = mmY + (camPos.Y - camHalfH) / ts * _scaleY;
            float rw = camHalfW * 2 / ts * _scaleX;
            float rh = camHalfH * 2 / ts * _scaleY;

            DrawRect(new Rect2(rx, ry, rw, rh), new Color(1f, 1f, 1f, 0.6f), false, 1.5f);
        }

        // Label
        var font = ThemeDB.FallbackFont;
        DrawString(font, new Vector2(mmX + 4, mmY - 4), "MAP",
            HorizontalAlignment.Left, -1, 10, Colors.White);
    }
}
