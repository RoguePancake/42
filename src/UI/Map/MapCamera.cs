using System;
using Godot;
using Warship.World;

namespace Warship.UI.Map;

/// <summary>
/// Zoomable camera. WASD/arrows pan, scroll wheel zooms, middle-click drags.
/// Zoomed out = Minecraft map overview. Zoomed in = individual tile detail.
/// Clamped to map bounds.
/// </summary>
public partial class MapCamera : Camera2D
{
    private float _targetZoom = 0.5f;
    private const float ZoomMin = 0.1f;    // see most of the map
    private const float ZoomMax = 4.0f;    // individual tile detail
    private const float ZoomSmooth = 8.0f;
    private const float PanSpeed = 400f;

    private bool _dragging;
    private float _mapPixelW;
    private float _mapPixelH;

    public override void _Ready()
    {
        _mapPixelW = TerrainGenerator.DefaultWidth * TerrainGenerator.TileSize;
        _mapPixelH = TerrainGenerator.DefaultHeight * TerrainGenerator.TileSize;

        Position = new Vector2(_mapPixelW / 2f, _mapPixelH / 2f);
        Zoom = new Vector2(_targetZoom, _targetZoom);
        MakeCurrent();
        GD.Print("[MapCamera] Ready.");
    }

    /// <summary>Center camera on a tile position.</summary>
    public void CenterOnTile(int tileX, int tileY)
    {
        float px = tileX * TerrainGenerator.TileSize + TerrainGenerator.TileSize / 2f;
        float py = tileY * TerrainGenerator.TileSize + TerrainGenerator.TileSize / 2f;
        Position = new Vector2(px, py);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp && mb.Pressed)
                _targetZoom = Math.Clamp(_targetZoom * 1.15f, ZoomMin, ZoomMax);
            else if (mb.ButtonIndex == MouseButton.WheelDown && mb.Pressed)
                _targetZoom = Math.Clamp(_targetZoom / 1.15f, ZoomMin, ZoomMax);
            else if (mb.ButtonIndex == MouseButton.Middle)
                _dragging = mb.Pressed;
        }

        if (e is InputEventMouseMotion mm && _dragging)
            Position -= mm.Relative / Zoom.X;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        float z = Zoom.X;

        // Keyboard pan
        var pan = Vector2.Zero;
        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up)) pan.Y -= 1;
        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down)) pan.Y += 1;
        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left)) pan.X -= 1;
        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right)) pan.X += 1;
        if (pan != Vector2.Zero)
            Position += pan.Normalized() * PanSpeed * dt / z;

        // Smooth zoom
        float newZoom = Mathf.Lerp(z, _targetZoom, ZoomSmooth * dt);
        Zoom = new Vector2(newZoom, newZoom);

        // Clamp to map bounds
        float halfW = GetViewportRect().Size.X / (2f * newZoom);
        float halfH = GetViewportRect().Size.Y / (2f * newZoom);
        Position = new Vector2(
            Math.Clamp(Position.X, halfW, _mapPixelW - halfW),
            Math.Clamp(Position.Y, halfH, _mapPixelH - halfH));
    }

    /// <summary>Convert a screen pixel position to a tile coordinate.</summary>
    public Vector2I ScreenToTile(Vector2 screenPos)
    {
        var worldPos = GetCanvasTransform().AffineInverse() * screenPos;
        int tx = (int)(worldPos.X / TerrainGenerator.TileSize);
        int ty = (int)(worldPos.Y / TerrainGenerator.TileSize);
        return new Vector2I(tx, ty);
    }
}
