using Godot;
using Warship.Core;
using Warship.Data;
using Warship.Events;

namespace Warship.UI.Map;

/// <summary>
/// Manages LOD state based on camera zoom level.
/// Publishes LodChangedEvent when the active LOD level transitions.
/// Attached as a sibling to MapCamera — reads zoom, publishes state.
///
/// LOD Thresholds:
///   Macro  (zoom &lt; 0.06)  — Strategic map: colored terrain blocks, nation icons
///   Hybrid (0.06 - 0.25)   — Transitional: simplified tiles, structure icons, road lines
///   Micro  (&gt; 0.25)       — Full detail: textured tiles, buildings, units, roads
/// </summary>
public partial class ZoomController : Node
{
    // Zoom thresholds for LOD transitions
    public const float MacroToHybrid = 0.06f;
    public const float HybridToMicro = 0.25f;

    // Hysteresis: prevent flickering at boundaries
    private const float Hysteresis = 0.008f;

    private LodLevel _currentLod = LodLevel.Macro;
    private Camera2D? _camera;

    public LodLevel CurrentLod => _currentLod;

    /// <summary>Current zoom value from camera.</summary>
    public float CurrentZoom => _camera?.Zoom.X ?? 0.015f;

    public override void _Ready()
    {
        _camera = GetNodeOrNull<Camera2D>("../MapCamera");
        if (_camera == null)
            GD.PrintErr("[ZoomController] No MapCamera found!");
        else
            GD.Print("[ZoomController] Tracking camera zoom for LOD switching.");
    }

    public override void _Process(double delta)
    {
        if (_camera == null) return;

        float zoom = _camera.Zoom.X;
        var newLod = ComputeLod(zoom);

        if (newLod != _currentLod)
        {
            var oldLod = _currentLod;
            _currentLod = newLod;
            EventBus.Instance?.Publish(new LodChangedEvent(oldLod, newLod, zoom));
        }
    }

    private LodLevel ComputeLod(float zoom)
    {
        // Apply hysteresis based on current state to prevent flickering
        return _currentLod switch
        {
            LodLevel.Macro => zoom > MacroToHybrid + Hysteresis ? LodLevel.Hybrid : LodLevel.Macro,
            LodLevel.Hybrid => zoom < MacroToHybrid - Hysteresis ? LodLevel.Macro
                             : zoom > HybridToMicro + Hysteresis ? LodLevel.Micro
                             : LodLevel.Hybrid,
            LodLevel.Micro => zoom < HybridToMicro - Hysteresis ? LodLevel.Hybrid : LodLevel.Micro,
            _ => LodLevel.Macro,
        };
    }

    /// <summary>
    /// Get the chunk load radius for the current LOD.
    /// Macro = fewer chunks loaded (each covers more screen).
    /// Micro = more chunks loaded (zoomed in, less world visible).
    /// </summary>
    public int GetChunkLoadRadius()
    {
        return _currentLod switch
        {
            LodLevel.Macro => 4,
            LodLevel.Hybrid => 5,
            LodLevel.Micro => 6,
            _ => 4,
        };
    }
}
