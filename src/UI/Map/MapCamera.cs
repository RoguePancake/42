using Godot;

namespace Warship.UI.Map;

/// <summary>
/// Camera with WASD pan, mouse scroll zoom, middle-click drag,
/// arrow keys, and edge scrolling. Clamped to map bounds.
/// </summary>
public partial class MapCamera : Camera2D
{
    // Zoom settings
    private float _targetZoom = 1.0f;
    private const float ZoomMin = 0.3f;
    private const float ZoomMax = 4.0f;
    private const float ZoomSpeed = 0.15f;
    private const float ZoomSmoothing = 8.0f;

    // Pan settings
    private const float PanSpeed = 500f;
    private const float EdgeScrollSpeed = 400f;
    private const float EdgeScrollMargin = 30f; // pixels from screen edge

    // Drag state
    private bool _dragging = false;
    private Vector2 _dragStart;

    // Map bounds (in pixels)
    private float _mapWidth;
    private float _mapHeight;

    public override void _Ready()
    {
        _mapWidth = MapManager.MapWidth * MapManager.TileSize;
        _mapHeight = MapManager.MapHeight * MapManager.TileSize;

        // Center camera on map
        Position = new Vector2(_mapWidth / 2f, _mapHeight / 2f);
        _targetZoom = 0.4f; // Start zoomed out further for 64px tiles

        GD.Print("[MapCamera] Ready — WASD/arrows to pan, scroll to zoom, middle-click drag");
    }

    public override void _UnhandledInput(InputEvent e)
    {
        // Mouse scroll → zoom
        if (e is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp && mb.Pressed)
            {
                _targetZoom = Mathf.Clamp(_targetZoom + ZoomSpeed, ZoomMin, ZoomMax);
            }
            else if (mb.ButtonIndex == MouseButton.WheelDown && mb.Pressed)
            {
                _targetZoom = Mathf.Clamp(_targetZoom - ZoomSpeed, ZoomMin, ZoomMax);
            }
            // Middle mouse → start/stop drag
            else if (mb.ButtonIndex == MouseButton.Middle)
            {
                _dragging = mb.Pressed;
                if (_dragging)
                    _dragStart = mb.GlobalPosition;
            }
        }

        // Mouse drag → pan
        if (e is InputEventMouseMotion mm && _dragging)
        {
            float currentZoom = Zoom.X;
            Position -= (mm.Relative / currentZoom);
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        float currentZoom = Zoom.X;

        // ─── Keyboard Pan (WASD + Arrows) ───
        var panDir = Vector2.Zero;

        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
            panDir.Y -= 1;
        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
            panDir.Y += 1;
        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
            panDir.X -= 1;
        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
            panDir.X += 1;

        if (panDir != Vector2.Zero)
            Position += panDir.Normalized() * PanSpeed * dt / currentZoom;

        // ─── Edge Scroll (mouse near screen edges) ───
        if (!_dragging)
        {
            var mousePos = GetViewport().GetMousePosition();
            var viewportSize = GetViewportRect().Size;
            var edgeDir = Vector2.Zero;

            if (mousePos.X < EdgeScrollMargin) edgeDir.X -= 1;
            if (mousePos.X > viewportSize.X - EdgeScrollMargin) edgeDir.X += 1;
            if (mousePos.Y < EdgeScrollMargin) edgeDir.Y -= 1;
            if (mousePos.Y > viewportSize.Y - EdgeScrollMargin) edgeDir.Y += 1;

            if (edgeDir != Vector2.Zero)
                Position += edgeDir.Normalized() * EdgeScrollSpeed * dt / currentZoom;
        }

        // ─── Smooth Zoom ───
        float newZoom = Mathf.Lerp(currentZoom, _targetZoom, ZoomSmoothing * dt);
        Zoom = new Vector2(newZoom, newZoom);

        // ─── Clamp to Map Bounds ───
        float halfViewW = GetViewportRect().Size.X / (2f * newZoom);
        float halfViewH = GetViewportRect().Size.Y / (2f * newZoom);

        Position = new Vector2(
            Mathf.Clamp(Position.X, halfViewW, _mapWidth - halfViewW),
            Mathf.Clamp(Position.Y, halfViewH, _mapHeight - halfViewH)
        );
    }
}
