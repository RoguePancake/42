using Godot;
using Warship.World;

namespace Warship.UI.Map;

/// <summary>
/// Camera controller: WASD/arrow pan, scroll zoom, middle-click drag, edge scroll.
/// Clamped to map bounds. Smooth zoom interpolation.
/// </summary>
public partial class MapCamera : Camera2D
{
    // Zoom: 0.007x = entire world, 4.0x = individual soldiers
    private float _targetZoom = 0.015f;
    private const float ZoomMin = 0.007f;
    private const float ZoomMax = 4.0f;
    private const float ZoomStep = 0.15f;
    private const float ZoomSmoothing = 8.0f;

    // Pan
    private const float PanSpeed = 500f;
    private const float EdgeScrollSpeed = 400f;
    private const float EdgeMargin = 30f;

    // UI panel insets (don't edge-scroll over UI)
    private const float InsetTop = 64f;
    private const float InsetBottom = 200f;
    private const float InsetLeft = 250f;
    private const float InsetRight = 250f;

    // Middle-click drag
    private bool _dragging;

    // Map size in pixels
    private float _mapPixelW;
    private float _mapPixelH;

    public override void _Ready()
    {
        _mapPixelW = TerrainGenerator.DefaultWidth * MapManagerConstants.TileSize;
        _mapPixelH = TerrainGenerator.DefaultHeight * MapManagerConstants.TileSize;

        // Start centered, zoomed out
        Position = new Vector2(_mapPixelW / 2f, _mapPixelH / 2f);
        Zoom = new Vector2(_targetZoom, _targetZoom);
        MakeCurrent();

        GD.Print("[MapCamera] Ready — WASD/arrows pan, scroll zoom, middle-drag");
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp && mb.Pressed)
                _targetZoom = Mathf.Clamp(_targetZoom * 1.15f, ZoomMin, ZoomMax);
            else if (mb.ButtonIndex == MouseButton.WheelDown && mb.Pressed)
                _targetZoom = Mathf.Clamp(_targetZoom / 1.15f, ZoomMin, ZoomMax);
            else if (mb.ButtonIndex == MouseButton.Middle)
                _dragging = mb.Pressed;
        }

        if (e is InputEventMouseMotion mm && _dragging)
        {
            Position -= mm.Relative / Zoom.X;
        }
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

        // Edge scroll (only when not dragging, mouse in map area)
        if (!_dragging)
        {
            var mouse = GetViewport().GetMousePosition();
            var vpSize = GetViewportRect().Size;
            var edge = Vector2.Zero;

            bool inMap = mouse.X > InsetLeft && mouse.X < vpSize.X - InsetRight
                      && mouse.Y > InsetTop && mouse.Y < vpSize.Y - InsetBottom;

            if (inMap)
            {
                if (mouse.X < InsetLeft + EdgeMargin) edge.X -= 1;
                if (mouse.X > vpSize.X - InsetRight - EdgeMargin) edge.X += 1;
                if (mouse.Y < InsetTop + EdgeMargin) edge.Y -= 1;
                if (mouse.Y > vpSize.Y - InsetBottom - EdgeMargin) edge.Y += 1;
            }

            if (edge != Vector2.Zero)
                Position += edge.Normalized() * EdgeScrollSpeed * dt / z;
        }

        // Smooth zoom
        float newZoom = Mathf.Lerp(z, _targetZoom, ZoomSmoothing * dt);
        Zoom = new Vector2(newZoom, newZoom);

        // Clamp to map bounds
        float halfW = GetViewportRect().Size.X / (2f * newZoom);
        float halfH = GetViewportRect().Size.Y / (2f * newZoom);
        Position = new Vector2(
            Mathf.Clamp(Position.X, halfW, _mapPixelW - halfW),
            Mathf.Clamp(Position.Y, halfH, _mapPixelH - halfH));
    }
}
