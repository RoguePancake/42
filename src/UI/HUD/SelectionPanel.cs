using Godot;
using System.Linq;
using Warship.Core;
using Warship.Data;
using Warship.Events;
using Warship.World;

namespace Warship.UI.HUD;

/// <summary>
/// Right-side panel showing info about whatever is selected (squad, building, or tile).
/// Also shows action buttons: spawn squad from camp, set patrol, etc.
/// </summary>
public partial class SelectionPanel : Control
{
    private WorldData? _world;
    private int _selectedSquadId = -1;
    private int _selectedBuildingId = -1;
    private int _selectedTileX = -1, _selectedTileY = -1;
    private bool _patrolMode;
    private int _patrolSquadId = -1;

    public override void _Ready()
    {
        EventBus.Instance?.Subscribe<WorldReadyEvent>(_ =>
            _world = WorldStateManager.Instance?.World);
        EventBus.Instance?.Subscribe<SelectSquadEvent>(ev =>
        {
            _selectedSquadId = ev.SquadId; _selectedBuildingId = -1; QueueRedraw();
        });
        EventBus.Instance?.Subscribe<SelectBuildingEvent>(ev =>
        {
            _selectedBuildingId = ev.BuildingId; _selectedSquadId = -1; QueueRedraw();
        });
        EventBus.Instance?.Subscribe<SelectTileEvent>(ev =>
        {
            _selectedTileX = ev.TileX; _selectedTileY = ev.TileY;
            _selectedSquadId = -1; _selectedBuildingId = -1;

            // If in patrol mode, set patrol endpoint
            if (_patrolMode && _patrolSquadId >= 0)
            {
                var squad = _world?.Squads.FirstOrDefault(s => s.Id == _patrolSquadId);
                if (squad != null)
                {
                    EventBus.Instance?.Publish(new SetPatrolEvent(
                        _patrolSquadId, squad.TileX, squad.TileY, ev.TileX, ev.TileY));
                }
                _patrolMode = false;
                _patrolSquadId = -1;
            }
            QueueRedraw();
        });
        EventBus.Instance?.Subscribe<DeselectAllEvent>(_ =>
        {
            _selectedSquadId = -1; _selectedBuildingId = -1;
            _selectedTileX = -1; _selectedTileY = -1; QueueRedraw();
        });

        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (_world == null) return;
        if (e is not InputEventKey key || !key.Pressed) return;

        // P = set patrol for selected squad
        if (key.Keycode == Key.P && _selectedSquadId >= 0)
        {
            _patrolMode = true;
            _patrolSquadId = _selectedSquadId;
            GD.Print("[SelectionPanel] Patrol mode: click a tile to set patrol endpoint.");
            GetViewport().SetInputAsHandled();
            QueueRedraw();
            return;
        }

        // T = spawn troops from selected camp
        if (key.Keycode == Key.T && _selectedBuildingId >= 0)
        {
            var bld = _world.Buildings.FirstOrDefault(b => b.Id == _selectedBuildingId);
            if (bld is { Type: BuildingType.TroopCamp, GarrisonCount: > 0 })
            {
                int count = System.Math.Min(50, bld.GarrisonCount);
                EventBus.Instance?.Publish(new SpawnSquadEvent(bld.Id, count));
            }
            GetViewport().SetInputAsHandled();
            QueueRedraw();
        }
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_world == null) return;

        var vpSize = GetViewportRect().Size;
        float panelW = 200;
        float panelX = vpSize.X - panelW;
        float startY = 40;

        // Only draw if something is selected
        bool hasSelection = _selectedSquadId >= 0 || _selectedBuildingId >= 0 || _selectedTileX >= 0;
        if (!hasSelection && !_patrolMode) return;

        var font = ThemeDB.FallbackFont;
        float y = startY;

        // Panel background
        DrawRect(new Rect2(panelX, startY, panelW, vpSize.Y - startY), new Color(0.05f, 0.05f, 0.15f, 0.8f));

        if (_patrolMode)
        {
            DrawString(font, new Vector2(panelX + 10, y + 18), "PATROL MODE",
                HorizontalAlignment.Left, -1, 13, Colors.Cyan);
            DrawString(font, new Vector2(panelX + 10, y + 36), "Click tile to set endpoint",
                HorizontalAlignment.Left, -1, 10, Colors.LightGray);
            return;
        }

        // ── Squad selected ──
        if (_selectedSquadId >= 0)
        {
            var squad = _world.Squads.FirstOrDefault(s => s.Id == _selectedSquadId);
            if (squad == null) return;

            DrawString(font, new Vector2(panelX + 10, y + 18), squad.Name,
                HorizontalAlignment.Left, -1, 13, Colors.Gold);
            y += 26;

            DrawInfoLine(font, panelX, ref y, "Troops", squad.Count.ToString(), Colors.White);
            DrawInfoLine(font, panelX, ref y, "Morale", $"{squad.Morale:F0}%", Colors.LimeGreen);
            DrawInfoLine(font, panelX, ref y, "Order", squad.Order.ToString(), Colors.LightBlue);
            DrawInfoLine(font, panelX, ref y, "Position", $"({squad.TileX}, {squad.TileY})", Colors.LightGray);

            y += 10;
            DrawString(font, new Vector2(panelX + 10, y + 14), "Right-click: Move",
                HorizontalAlignment.Left, -1, 10, Colors.Yellow);
            y += 16;
            DrawString(font, new Vector2(panelX + 10, y + 14), "P: Set Patrol",
                HorizontalAlignment.Left, -1, 10, Colors.Cyan);
            return;
        }

        // ── Building selected ──
        if (_selectedBuildingId >= 0)
        {
            var bld = _world.Buildings.FirstOrDefault(b => b.Id == _selectedBuildingId);
            if (bld == null) return;

            DrawString(font, new Vector2(panelX + 10, y + 18),
                BuildingInfo.DisplayName(bld.Type),
                HorizontalAlignment.Left, -1, 13, Colors.Gold);
            y += 26;

            DrawInfoLine(font, panelX, ref y, "Health", $"{bld.Health}%", Colors.LimeGreen);
            DrawInfoLine(font, panelX, ref y, "Position", $"({bld.TileX}, {bld.TileY})", Colors.LightGray);

            if (bld.Type == BuildingType.TroopCamp)
            {
                DrawInfoLine(font, panelX, ref y, "Garrison", $"{bld.GarrisonCount}/{bld.GarrisonCap}", Colors.White);
                y += 10;
                if (bld.GarrisonCount > 0)
                {
                    DrawString(font, new Vector2(panelX + 10, y + 14), "T: Deploy 50 troops",
                        HorizontalAlignment.Left, -1, 10, Colors.Yellow);
                }
            }
            return;
        }

        // ── Tile selected ──
        if (_selectedTileX >= 0)
        {
            int terrain = _world.TerrainMap[_selectedTileX + _selectedTileY * _world.MapWidth];
            string terrainName = ((Terrain)terrain).ToString();

            DrawString(font, new Vector2(panelX + 10, y + 18), "TILE INFO",
                HorizontalAlignment.Left, -1, 13, Colors.White);
            y += 26;

            DrawInfoLine(font, panelX, ref y, "Terrain", terrainName, TerrainInfo.GetColor(terrain).Lightened(0.3f));
            DrawInfoLine(font, panelX, ref y, "Position", $"({_selectedTileX}, {_selectedTileY})", Colors.LightGray);
            DrawInfoLine(font, panelX, ref y, "Buildable", TerrainInfo.IsBuildable(terrain) ? "Yes" : "No",
                TerrainInfo.IsBuildable(terrain) ? Colors.LimeGreen : Colors.IndianRed);
            DrawInfoLine(font, panelX, ref y, "Passable", TerrainInfo.IsPassable(terrain) ? "Yes" : "No",
                TerrainInfo.IsPassable(terrain) ? Colors.LimeGreen : Colors.IndianRed);
        }
    }

    private void DrawInfoLine(Font font, float panelX, ref float y, string label, string value, Color valueColor)
    {
        DrawString(font, new Vector2(panelX + 10, y + 14), $"{label}:",
            HorizontalAlignment.Left, -1, 10, Colors.Gray);
        DrawString(font, new Vector2(panelX + 80, y + 14), value,
            HorizontalAlignment.Left, -1, 10, valueColor);
        y += 16;
    }
}
