using Godot;
using System.Collections.Generic;
using Warship.Data;
using Warship.Core;
using Warship.World;

namespace Warship.UI.Map;

/// <summary>
/// Renders armies with a 4-level LOD system based on camera zoom.
///
/// LOD 0 (Strategic, zoom &lt; 0.08): 1 colored dot per army.
/// LOD 1 (Operational, 0.08-0.3): Dot clusters, 1 dot = 50 troops.
/// LOD 2 (Tactical, 0.3-1.0): Unit-type colored dots, 1 dot = 10 troops.
/// LOD 3 (Close-up, &gt; 1.0): Individual pixel-stamp silhouettes.
///
/// Frustum-culled, animated with frame jitter.
/// </summary>
public partial class ArmySwarmRenderer : Node2D
{
    private const int TileSize = MapManagerConstants.TileSize;
    private const float SwarmRadius = 40f;
    private const float JitterSpeed = 3.0f;

    private const float LodStrategicMax = 0.08f;
    private const float LodOperationalMax = 0.30f;
    private const float LodTacticalMax = 1.0f;

    private WorldData? _world;
    private int _frame;
    private string? _selectedArmyId;

    public string? SelectedArmyId
    {
        get => _selectedArmyId;
        set { _selectedArmyId = value; QueueRedraw(); }
    }

    public void Initialize(WorldData world)
    {
        _world = world;
        GD.Print($"[ArmySwarm] Initialized with {world.Armies.Count} armies");
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_world == null)
        {
            _world = WorldStateManager.Instance?.Data;
            if (_world != null) QueueRedraw();
            return;
        }

        _frame++;

        // Interpolate army positions toward targets
        float speed = 200f * (float)delta;
        foreach (var army in _world.Armies)
        {
            if (!army.IsAlive) continue;
            var cur = new Vector2(army.PixelX, army.PixelY);
            var tgt = new Vector2(army.TargetPixelX, army.TargetPixelY);
            float dist = cur.DistanceTo(tgt);

            if (dist > 1f)
            {
                if (dist <= speed)
                {
                    army.PixelX = tgt.X;
                    army.PixelY = tgt.Y;
                }
                else
                {
                    var dir = (tgt - cur).Normalized();
                    army.PixelX += dir.X * speed;
                    army.PixelY += dir.Y * speed;
                }
            }
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_world == null || _world.Armies.Count == 0) return;

        var camera = GetViewport().GetCamera2D();
        float zoom = camera?.Zoom.X ?? 1f;
        var viewRect = GetViewRect(camera);

        int lod = zoom < LodStrategicMax ? 0
                : zoom < LodOperationalMax ? 1
                : zoom < LodTacticalMax ? 2
                : 3;

        foreach (var army in _world.Armies)
        {
            if (!army.IsAlive) continue;

            var center = new Vector2(army.PixelX, army.PixelY);
            if (!viewRect.HasPoint(center)) continue;

            int nIdx = MapManager.GetNationIndex(_world, army.NationId);
            Color natColor = nIdx >= 0 ? _world.Nations[nIdx].NationColor : Godot.Colors.Gray;

            // Selection highlight
            if (army.Id == _selectedArmyId)
            {
                float r = lod == 0 ? 12f : SwarmRadius + 8;
                DrawArc(center, r, 0, Mathf.Pi * 2, 32, Godot.Colors.Yellow, 2);
            }

            switch (lod)
            {
                case 0: DrawStrategic(army, center, natColor); break;
                case 1: DrawOperational(army, center, natColor); break;
                case 2: DrawTactical(army, center, natColor, zoom); break;
                case 3: DrawCloseUp(army, center, natColor); break;
            }

            DrawLabel(army, center, natColor, zoom, lod);
        }
    }

    // ═══ LOD 0: Strategic — 1 dot per army ═══

    private void DrawStrategic(ArmyData army, Vector2 center, Color nationColor)
    {
        float size = System.Math.Clamp(army.TotalStrength / 200f, 3f, 8f);
        Color c = DomainColor(army.PrimaryDomain, nationColor);
        DrawRect(new Rect2(center.X - size / 2, center.Y - size / 2, size, size), c);
    }

    // ═══ LOD 1: Operational — dot clusters, 1 dot = 50 troops ═══

    private void DrawOperational(ArmyData army, Vector2 center, Color nationColor)
    {
        int dots = System.Math.Clamp(army.TotalStrength / 50, 1, 200);
        float spread = SwarmRadius * System.Math.Min(1f, dots / 40f);
        FormationSpread(army.Formation, ref spread);

        int hash = army.Id.GetHashCode();
        Color c = DomainColor(army.PrimaryDomain, nationColor);

        for (int i = 0; i < dots; i++)
        {
            var pos = DotPosition(hash, i, 0, spread, army.Formation, center);
            DrawRect(new Rect2(pos.X, pos.Y, 2f, 2f), c);
        }
    }

    // ═══ LOD 2: Tactical — unit-type colored dots, 1 dot = 10 troops ═══

    private void DrawTactical(ArmyData army, Vector2 center, Color nationColor, float zoom)
    {
        float dotSize = zoom < 0.5f ? 1f : 2f;
        float spread = SwarmRadius * System.Math.Min(1f, army.SwarmDotCount / 100f);
        FormationSpread(army.Formation, ref spread);

        int hash = army.Id.GetHashCode();

        foreach (var (type, count) in army.Composition)
        {
            int dots = System.Math.Clamp(count / 10, 1, 200);
            Color c = TypeColor(type, nationColor);

            for (int i = 0; i < dots; i++)
            {
                var pos = DotPosition(hash, i, (int)type * 1000, spread, army.Formation, center);
                DrawRect(new Rect2(pos.X, pos.Y, dotSize, dotSize), c);
            }
        }
    }

    // ═══ LOD 3: Close-up — pixel-stamp silhouettes ═══

    private void DrawCloseUp(ArmyData army, Vector2 center, Color nationColor)
    {
        int maxUnits = System.Math.Min(army.TotalStrength, 2000);
        float spread = SwarmRadius * 2f;
        FormationSpread(army.Formation, ref spread);

        int hash = army.Id.GetHashCode();
        int idx = 0;

        foreach (var (type, count) in army.Composition)
        {
            int render = System.Math.Min(count, maxUnits - idx);
            if (render <= 0) break;

            Color c = TypeColor(type, nationColor);
            var stamp = UnitStamp.GetStamp(type);

            for (int i = 0; i < render; i++)
            {
                var pos = DotPosition(hash, idx, 0, spread, army.Formation, center);
                foreach (var (dx, dy) in stamp)
                    DrawRect(new Rect2(pos.X + dx, pos.Y + dy, 1f, 1f), c);

                idx++;
                if (idx >= maxUnits) return;
            }
        }
    }

    // ═══ Shared helpers ═══

    private Vector2 DotPosition(int hash, int index, int typeOffset,
        float spread, FormationType formation, Vector2 center)
    {
        int h1 = TerrainGenerator.HashInt(hash, index * 2 + typeOffset);
        int h2 = TerrainGenerator.HashInt(hash, index * 2 + 1 + typeOffset);

        float angle = (h1 & 0xFFFF) / 65535f * Mathf.Pi * 2f;
        float radius = (h2 & 0xFFFF) / 65535f * spread;

        // Jitter animation
        float jAngle = angle + _frame * JitterSpeed * 0.01f;
        float jRadius = radius + Mathf.Sin(_frame * 0.05f + index * 0.3f) * 3f;

        float fx = Mathf.Cos(jAngle) * jRadius;
        float fy = Mathf.Sin(jAngle) * jRadius;

        // Formation shaping
        if (formation == FormationType.Wedge) { fx *= 0.6f; fy *= 1.2f; }
        else if (formation == FormationType.Column) { fx *= 0.3f; fy *= 2f; }

        return center + new Vector2(fx, fy);
    }

    private static void FormationSpread(FormationType f, ref float spread)
    {
        if (f == FormationType.Column) spread *= 0.5f;
        if (f == FormationType.Wedge) spread *= 0.8f;
    }

    private void DrawLabel(ArmyData army, Vector2 center, Color nationColor, float zoom, int lod)
    {
        bool show = army.Id == _selectedArmyId
            || (lod == 1 && army.TotalStrength > 500)
            || (lod >= 2 && zoom > 0.4f);

        if (!show) return;

        var font = ThemeDB.FallbackFont;
        string label = $"{army.Name} ({army.TotalStrength})";
        int fontSize = lod >= 3 ? 12 : 10;
        float yOff = lod == 0 ? -16 : -SwarmRadius - 12;

        bool isPlayer = _world != null && army.NationId == _world.PlayerNationId;
        DrawString(font, center + new Vector2(-40, yOff), label,
            HorizontalAlignment.Center, 120, fontSize,
            isPlayer ? Godot.Colors.Gold : Godot.Colors.White);
    }

    private static Color DomainColor(UnitDomain domain, Color nat) => domain switch
    {
        UnitDomain.Naval => nat.Lerp(Godot.Colors.CornflowerBlue, 0.3f),
        UnitDomain.Air => nat.Lightened(0.25f),
        UnitDomain.Special => Godot.Colors.OrangeRed,
        _ => nat
    };

    private static Color TypeColor(UnitType type, Color nat)
        => DomainColor(UnitRules.GetDomain(type), nat);

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

    private Rect2 GetViewRect(Camera2D? camera)
    {
        if (camera != null)
        {
            var vpSize = GetViewportRect().Size;
            var camPos = camera.GlobalPosition;
            float halfW = vpSize.X / (2f * camera.Zoom.X);
            float halfH = vpSize.Y / (2f * camera.Zoom.Y);
            float margin = SwarmRadius * 4 + 50;
            return new Rect2(
                camPos.X - halfW - margin, camPos.Y - halfH - margin,
                halfW * 2 + margin * 2, halfH * 2 + margin * 2);
        }
        return new Rect2(0, 0, 192000, 115200);
    }
}
