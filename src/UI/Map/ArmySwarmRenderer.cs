using Godot;
using System.Collections.Generic;
using Warship.Data;
using Warship.Core;
using Warship.World;

namespace Warship.UI.Map;

/// <summary>
/// Renders armies with a 4-level LOD system based on camera zoom.
///
/// LOD 0 (Strategic, zoom &lt; 0.08x): 1 colored dot per army — see the whole war.
/// LOD 1 (Operational, 0.08x–0.3x): Dot clusters with formation shape, 1 dot = 50 troops.
/// LOD 2 (Tactical, 0.3x–1.0x): Unit-type colored dots, 1 dot = 10 troops.
/// LOD 3 (Close-up, &gt; 1.0x): Individual unit pixel-stamp silhouettes — tanks, ships, planes.
///
/// Performance: frustum-culled, LOD limits draw count.
///   LOD 0: ~200 draws. LOD 1: ~4k. LOD 2: ~20k. LOD 3: ~5k (viewport limited).
/// </summary>
public partial class ArmySwarmRenderer : Node2D
{
    private const int TileSize = MapManagerConstants.TileSize;
    private const float SwarmRadius = 40f;   // Max pixel spread of dots from army center
    private const float JitterSpeed = 3.0f;  // Animation speed of dot movement

    // LOD zoom thresholds
    private const float LodStrategicMax = 0.08f;   // LOD 0 → 1
    private const float LodOperationalMax = 0.30f;  // LOD 1 → 2
    private const float LodTacticalMax = 1.0f;      // LOD 2 → 3

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
        float cameraZoom = camera?.Zoom.X ?? 1f;

        Rect2 viewRect;
        if (camera != null)
        {
            var vpSize = GetViewportRect().Size;
            var zoom = camera.Zoom;
            var camPos = camera.GlobalPosition;
            float halfW = vpSize.X / (2f * zoom.X);
            float halfH = vpSize.Y / (2f * zoom.Y);
            float margin = SwarmRadius * 4 + 50; // extra margin for large stamps at LOD 3
            viewRect = new Rect2(camPos.X - halfW - margin, camPos.Y - halfH - margin,
                                 halfW * 2 + margin * 2, halfH * 2 + margin * 2);
        }
        else
        {
            viewRect = new Rect2(0, 0, _world.MapWidth * TileSize, _world.MapHeight * TileSize);
        }

        // Determine LOD level
        int lod;
        if (cameraZoom < LodStrategicMax)
            lod = 0;
        else if (cameraZoom < LodOperationalMax)
            lod = 1;
        else if (cameraZoom < LodTacticalMax)
            lod = 2;
        else
            lod = 3;

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
                float highlightRadius = lod == 0 ? 12f : SwarmRadius + 8;
                DrawArc(center, highlightRadius, 0, Mathf.Pi * 2, 32, Colors.Yellow, 2);
            }

            switch (lod)
            {
                case 0:
                    DrawLod0Strategic(army, center, nationColor);
                    break;
                case 1:
                    DrawLod1Operational(army, center, nationColor);
                    break;
                case 2:
                    DrawLod2Tactical(army, center, nationColor, cameraZoom);
                    break;
                case 3:
                    DrawLod3CloseUp(army, center, nationColor);
                    break;
            }

            // Draw army label
            DrawArmyLabel(army, center, nationColor, cameraZoom, lod);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  LOD 0: Strategic — 1 dot per army, color-coded by domain
    // ═══════════════════════════════════════════════════════════════

    private void DrawLod0Strategic(ArmyData army, Vector2 center, Color nationColor)
    {
        // Single colored dot, size indicates army strength
        float size = System.Math.Clamp(army.TotalStrength / 200f, 3f, 8f);
        Color dotColor = GetDomainColor(army.PrimaryDomain, nationColor);
        DrawRect(new Rect2(center.X - size / 2, center.Y - size / 2, size, size), dotColor);
    }

    // ═══════════════════════════════════════════════════════════════
    //  LOD 1: Operational — dot clusters, 1 dot per 50 troops
    // ═══════════════════════════════════════════════════════════════

    private void DrawLod1Operational(ArmyData army, Vector2 center, Color nationColor)
    {
        int dotCount = System.Math.Max(1, army.TotalStrength / 50);
        if (dotCount > 200) dotCount = 200;

        float spread = SwarmRadius * System.Math.Min(1f, dotCount / 40f);
        ApplyFormationSpread(army.Formation, ref spread);

        int armyHash = army.Id.GetHashCode();
        Color dotColor = GetDomainColor(army.PrimaryDomain, nationColor);

        for (int i = 0; i < dotCount; i++)
        {
            var pos = GetDotPosition(armyHash, i, 0, spread, army.Formation, center);
            DrawRect(new Rect2(pos.X, pos.Y, 2f, 2f), dotColor);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  LOD 2: Tactical — unit-type colored dots, 1 dot per 10 troops
    // ═══════════════════════════════════════════════════════════════

    private void DrawLod2Tactical(ArmyData army, Vector2 center, Color nationColor, float cameraZoom)
    {
        float dotSize = cameraZoom < 0.5f ? 1f : 2f;

        float spread = SwarmRadius * System.Math.Min(1f, army.SwarmDotCount / 100f);
        ApplyFormationSpread(army.Formation, ref spread);

        int armyHash = army.Id.GetHashCode();

        foreach (var (type, count) in army.Composition)
        {
            int typeDots = System.Math.Max(1, count / 10);
            if (typeDots > 200) typeDots = 200;
            Color dotColor = GetUnitTypeColor(type, nationColor);

            for (int i = 0; i < typeDots; i++)
            {
                var pos = GetDotPosition(armyHash, i, (int)type * 1000, spread, army.Formation, center);
                DrawRect(new Rect2(pos.X, pos.Y, dotSize, dotSize), dotColor);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  LOD 3: Close-Up — individual unit pixel-stamp silhouettes
    // ═══════════════════════════════════════════════════════════════

    private void DrawLod3CloseUp(ArmyData army, Vector2 center, Color nationColor)
    {
        // At close zoom, render every unit with its stamp shape.
        // Cap total units rendered per army for performance.
        int totalUnits = System.Math.Min(army.TotalStrength, 2000);

        float spread = SwarmRadius * 2f; // Larger spread at close zoom
        ApplyFormationSpread(army.Formation, ref spread);

        int armyHash = army.Id.GetHashCode();
        int unitIdx = 0;

        foreach (var (type, count) in army.Composition)
        {
            int renderCount = System.Math.Min(count, totalUnits - unitIdx);
            if (renderCount <= 0) break;

            Color unitColor = GetUnitTypeColor(type, nationColor);
            var stamp = UnitStamp.GetStamp(type);

            for (int i = 0; i < renderCount; i++)
            {
                var pos = GetDotPosition(armyHash, unitIdx, 0, spread, army.Formation, center);

                // Draw each pixel of the stamp
                foreach (var (dx, dy) in stamp)
                {
                    DrawRect(new Rect2(pos.X + dx, pos.Y + dy, 1f, 1f), unitColor);
                }

                unitIdx++;
                if (unitIdx >= totalUnits) break;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Shared helpers
    // ═══════════════════════════════════════════════════════════════

    private Vector2 GetDotPosition(int armyHash, int index, int typeOffset,
        float spread, FormationType formation, Vector2 center)
    {
        int hash1 = TerrainGenerator.HashInt(armyHash, index * 2 + typeOffset);
        int hash2 = TerrainGenerator.HashInt(armyHash, index * 2 + 1 + typeOffset);

        float angle = (hash1 & 0xFFFF) / 65535f * Mathf.Pi * 2f;
        float radius = (hash2 & 0xFFFF) / 65535f * spread;

        // Jitter: slight position variation each frame
        float jitterAngle = angle + _frameSeed * JitterSpeed * 0.01f;
        float jitterRadius = radius + Mathf.Sin(_frameSeed * 0.05f + index * 0.3f) * 3f;

        // Formation shaping
        float fx = Mathf.Cos(jitterAngle) * jitterRadius;
        float fy = Mathf.Sin(jitterAngle) * jitterRadius;

        if (formation == FormationType.Wedge)
        {
            fx *= 0.6f;
            fy *= 1.2f;
        }
        else if (formation == FormationType.Column)
        {
            fx *= 0.3f;
            fy *= 2f;
        }

        return center + new Vector2(fx, fy);
    }

    private static void ApplyFormationSpread(FormationType formation, ref float spread)
    {
        if (formation == FormationType.Column) spread *= 0.5f;
        if (formation == FormationType.Wedge) spread *= 0.8f;
    }

    private void DrawArmyLabel(ArmyData army, Vector2 center, Color nationColor,
        float cameraZoom, int lod)
    {
        // LOD 0: show name only for selected army
        // LOD 1: show name + strength for large armies
        // LOD 2-3: show name + strength when zoomed enough
        bool showLabel = false;
        if (army.Id == _selectedArmyId)
            showLabel = true;
        else if (lod == 0)
            showLabel = false;
        else if (lod == 1 && army.TotalStrength > 500)
            showLabel = true;
        else if (lod >= 2 && cameraZoom > 0.4f)
            showLabel = true;

        if (!showLabel) return;

        var font = ThemeDB.FallbackFont;
        string label = $"{army.Name} ({army.TotalStrength})";
        int fontSize = lod >= 3 ? 12 : 10;
        float yOffset = lod == 0 ? -16 : -SwarmRadius - 12;

        DrawString(font, center + new Vector2(-40, yOffset), label,
            HorizontalAlignment.Center, 120, fontSize,
            army.NationId == _world!.PlayerNationId ? Colors.Gold : Colors.White);
    }

    /// <summary>Color by unit domain (for LOD 0-1 where individual types aren't shown).</summary>
    private static Color GetDomainColor(UnitDomain domain, Color nationColor)
    {
        return domain switch
        {
            UnitDomain.Land => nationColor,
            UnitDomain.Naval => nationColor.Lerp(Colors.CornflowerBlue, 0.3f),
            UnitDomain.Air => nationColor.Lightened(0.25f),
            UnitDomain.Special => Colors.OrangeRed,
            _ => nationColor
        };
    }

    /// <summary>Color by specific unit type (for LOD 2-3).</summary>
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
