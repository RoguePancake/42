using Godot;
using System.Collections.Generic;
using Warship.Data;
using Warship.Core;
using Warship.World;

namespace Warship.UI.Map;

/// <summary>
/// Renders armies as pixel-dot swarms. Each army's troop count / 10 = number of dots.
/// At zoom-out: 1px colored dots swarming like ants.
/// At zoom-in: 2-3px dots with unit-type color variation.
/// Frustum-culled: only draws visible armies.
///
/// Performance: uses batched DrawRect calls. ~200 armies * ~100 dots avg = ~20k draws max.
/// </summary>
public partial class ArmySwarmRenderer : Node2D
{
    private const int TileSize = MapManagerConstants.TileSize;
    private const float SwarmRadius = 40f;   // Max pixel spread of dots from army center
    private const float JitterSpeed = 3.0f;  // Animation speed of dot movement

    private WorldData? _world;
    private int _frameSeed;
    private string? _selectedArmyId;

    public string? SelectedArmyId
    {
        get => _selectedArmyId;
        set { _selectedArmyId = value; QueueRedraw(); }
    }

    public override void _Process(double delta)
    {
        if (_world == null)
        {
            _world = WorldStateManager.Instance?.Data;
            if (_world != null) QueueRedraw();
            return;
        }

        _frameSeed++;

        // Interpolate army pixel positions toward targets
        bool needsRedraw = false;
        foreach (var army in _world.Armies)
        {
            if (!army.IsAlive) continue;
            float speed = 200f * (float)delta;
            var current = new Vector2(army.PixelX, army.PixelY);
            var target = new Vector2(army.TargetPixelX, army.TargetPixelY);
            float dist = current.DistanceTo(target);

            if (dist > 1f)
            {
                if (dist <= speed)
                {
                    army.PixelX = target.X;
                    army.PixelY = target.Y;
                }
                else
                {
                    var dir = (target - current).Normalized();
                    army.PixelX += dir.X * speed;
                    army.PixelY += dir.Y * speed;
                }
                needsRedraw = true;
            }
        }

        // Always redraw for jitter animation
        QueueRedraw();
    }

    public override void _Draw()
    {
        _world = WorldStateManager.Instance?.Data;
        if (_world == null || _world.Armies.Count == 0) return;

        // Get camera viewport for frustum culling
        var camera = GetViewport().GetCamera2D();
        Rect2 viewRect;
        if (camera != null)
        {
            var vpSize = GetViewportRect().Size;
            var zoom = camera.Zoom;
            var camPos = camera.GlobalPosition;
            float halfW = vpSize.X / (2f * zoom.X);
            float halfH = vpSize.Y / (2f * zoom.Y);
            viewRect = new Rect2(camPos.X - halfW - SwarmRadius * 2, camPos.Y - halfH - SwarmRadius * 2,
                                 halfW * 2 + SwarmRadius * 4, halfH * 2 + SwarmRadius * 4);
        }
        else
        {
            viewRect = new Rect2(0, 0, _world.MapWidth * TileSize, _world.MapHeight * TileSize);
        }

        float cameraZoom = camera?.Zoom.X ?? 1f;

        foreach (var army in _world.Armies)
        {
            if (!army.IsAlive) continue;

            // Frustum cull
            if (!viewRect.HasPoint(new Vector2(army.PixelX, army.PixelY))) continue;

            int nIdx = int.Parse(army.NationId.Split('_')[1]);
            Color nationColor = _world.Nations[nIdx].NationColor;
            var center = new Vector2(army.PixelX, army.PixelY);

            // Selection highlight
            if (army.Id == _selectedArmyId)
            {
                DrawArc(center, SwarmRadius + 8, 0, Mathf.Pi * 2, 32, Colors.Yellow, 2);
            }

            int dotCount = army.SwarmDotCount;
            // Cap dots for performance
            if (dotCount > 500) dotCount = 500;

            // Dot size based on zoom
            float dotSize;
            if (cameraZoom < 0.3f)
                dotSize = 1f;
            else if (cameraZoom < 0.8f)
                dotSize = 2f;
            else
                dotSize = 3f;

            // Compute spread based on army size and formation
            float spread = SwarmRadius * System.Math.Min(1f, dotCount / 100f);
            if (army.Formation == FormationType.Column) spread *= 0.5f;
            if (army.Formation == FormationType.Wedge) spread *= 0.8f;

            // Draw each dot
            int armyHash = army.Id.GetHashCode();
            foreach (var (type, count) in army.Composition)
            {
                int typeDots = System.Math.Max(1, count / 10);
                if (typeDots > 200) typeDots = 200;
                Color dotColor = GetUnitTypeColor(type, nationColor);

                for (int i = 0; i < typeDots; i++)
                {
                    // Deterministic position with time-based jitter
                    int hash1 = TerrainGenerator.HashInt(armyHash, i * 2 + (int)type * 1000);
                    int hash2 = TerrainGenerator.HashInt(armyHash, i * 2 + 1 + (int)type * 1000);

                    float angle = (hash1 & 0xFFFF) / 65535f * Mathf.Pi * 2f;
                    float radius = (hash2 & 0xFFFF) / 65535f * spread;

                    // Jitter: slight position variation each frame
                    float jitterAngle = angle + _frameSeed * JitterSpeed * 0.01f;
                    float jitterRadius = radius + Mathf.Sin(_frameSeed * 0.05f + i * 0.3f) * 3f;

                    // Formation shaping
                    float fx = Mathf.Cos(jitterAngle) * jitterRadius;
                    float fy = Mathf.Sin(jitterAngle) * jitterRadius;

                    if (army.Formation == FormationType.Wedge)
                    {
                        // Push dots forward (toward target direction)
                        fx *= 0.6f;
                        fy *= 1.2f;
                    }
                    else if (army.Formation == FormationType.Column)
                    {
                        fx *= 0.3f;
                        fy *= 2f;
                    }

                    var dotPos = center + new Vector2(fx, fy);
                    DrawRect(new Rect2(dotPos.X, dotPos.Y, dotSize, dotSize), dotColor);
                }
            }

            // Draw army label at zoom-in
            if (cameraZoom > 0.5f)
            {
                var font = ThemeDB.FallbackFont;
                string label = $"{army.Name} ({army.TotalStrength})";
                DrawString(font, center + new Vector2(-40, -SwarmRadius - 12), label,
                    HorizontalAlignment.Center, 80, 10,
                    army.NationId == _world.PlayerNationId ? Colors.Gold : Colors.White);
            }
        }
    }

    /// <summary>Vary dot color by unit type within the nation's palette.</summary>
    private static Color GetUnitTypeColor(UnitType type, Color nationColor)
    {
        return UnitRules.GetDomain(type) switch
        {
            UnitDomain.Land => nationColor,
            UnitDomain.Naval => nationColor.Lerp(Colors.CornflowerBlue, 0.3f),
            UnitDomain.Air => nationColor.Lightened(0.25f),
            UnitDomain.Special => Colors.OrangeRed,
            _ => nationColor
        };
    }

    /// <summary>Handle click to select army.</summary>
    public ArmyData? GetArmyAtPosition(Vector2 worldPos)
    {
        if (_world == null) return null;
        foreach (var army in _world.Armies)
        {
            if (!army.IsAlive) continue;
            var center = new Vector2(army.PixelX, army.PixelY);
            if (center.DistanceTo(worldPos) < SwarmRadius + 10)
                return army;
        }
        return null;
    }
}
