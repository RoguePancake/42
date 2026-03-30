using Godot;
using Warship.Core;
using Warship.Data;
using Warship.Events;
using Warship.World;

namespace Warship.UI.Map;

/// <summary>
/// Build mode UI overlay. Shows grid snapping, placement preview,
/// and terrain validation feedback when the player is placing structures/roads/walls.
///
/// Activated by pressing B (toggle build mode) or through the council panel.
/// Left-click places, right-click cancels, scroll changes type.
/// </summary>
public partial class BuildOverlay : Node2D
{
    private const int TileSize = MapManagerConstants.TileSize;

    private bool _buildModeActive;
    private StructureType _selectedType = StructureType.Barracks;
    private BuildCategory _buildCategory = BuildCategory.Structure;
    private RoadType _selectedRoadType = RoadType.Dirt;
    private WallType _selectedWallType = WallType.Sandbag;
    private DirectionMask _selectedWallFacing = DirectionMask.N;

    // Road dragging state
    private bool _roadDragging;
    private int _roadStartX, _roadStartY;

    private WorldData? _world;
    private Warship.Engines.BuildEngine? _buildEngine;

    private enum BuildCategory { Structure, Road, Wall }

    // Current cursor position in tile coords
    private int _cursorTileX, _cursorTileY;
    private bool _placementValid;

    public bool IsBuildMode => _buildModeActive;

    public override void _Ready()
    {
        EventBus.Instance?.Subscribe<WorldReadyEvent>(OnWorldReady);
        EventBus.Instance?.Subscribe<BuildCompletedEvent>(OnBuildCompleted);
        EventBus.Instance?.Subscribe<BuildFailedEvent>(OnBuildFailed);
    }

    public override void _ExitTree()
    {
        EventBus.Instance?.Unsubscribe<WorldReadyEvent>(OnWorldReady);
        EventBus.Instance?.Unsubscribe<BuildCompletedEvent>(OnBuildCompleted);
        EventBus.Instance?.Unsubscribe<BuildFailedEvent>(OnBuildFailed);
    }

    private void OnWorldReady(WorldReadyEvent ev)
    {
        _world = WorldStateManager.Instance?.Data;
    }

    public void SetBuildEngine(Warship.Engines.BuildEngine engine)
    {
        _buildEngine = engine;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Toggle build mode with B key
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.B)
            {
                _buildModeActive = !_buildModeActive;
                EventBus.Instance?.Publish(new NotificationEvent(
                    _buildModeActive ? "Build mode ON" : "Build mode OFF", "info"));
                QueueRedraw();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (!_buildModeActive) return;

            // Switch build category
            if (key.Keycode == Key.Key1) { _buildCategory = BuildCategory.Structure; QueueRedraw(); }
            if (key.Keycode == Key.Key2) { _buildCategory = BuildCategory.Road; QueueRedraw(); }
            if (key.Keycode == Key.Key3) { _buildCategory = BuildCategory.Wall; QueueRedraw(); }

            // Cycle type with Tab
            if (key.Keycode == Key.Tab)
            {
                CycleSelectedType();
                QueueRedraw();
                GetViewport().SetInputAsHandled();
            }

            // Rotate wall facing with R
            if (key.Keycode == Key.R && _buildCategory == BuildCategory.Wall)
            {
                _selectedWallFacing = RotateFacing(_selectedWallFacing);
                QueueRedraw();
                GetViewport().SetInputAsHandled();
            }

            // Cancel with Escape
            if (key.Keycode == Key.Escape)
            {
                _buildModeActive = false;
                _roadDragging = false;
                QueueRedraw();
            }
        }

        if (!_buildModeActive) return;

        // Track cursor position
        if (@event is InputEventMouseMotion)
        {
            UpdateCursorPosition();
            QueueRedraw();
        }

        // Click to place
        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            UpdateCursorPosition();

            if (mb.ButtonIndex == MouseButton.Left)
            {
                ExecutePlacement();
                GetViewport().SetInputAsHandled();
            }
            else if (mb.ButtonIndex == MouseButton.Right)
            {
                if (_roadDragging)
                {
                    _roadDragging = false;
                }
                else
                {
                    _buildModeActive = false;
                }
                QueueRedraw();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_buildModeActive)
        {
            UpdateCursorPosition();
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (!_buildModeActive) return;

        // Draw placement preview at cursor
        var tilePos = new Vector2(_cursorTileX * TileSize, _cursorTileY * TileSize);

        // Validation color
        Color previewColor = _placementValid
            ? new Color(0.2f, 0.8f, 0.2f, 0.4f)   // Green = valid
            : new Color(0.8f, 0.2f, 0.2f, 0.4f);   // Red = invalid

        // Grid highlight
        DrawRect(new Rect2(tilePos, new Vector2(TileSize, TileSize)), previewColor);

        // Border
        Color borderColor = _placementValid
            ? new Color(0.2f, 1.0f, 0.2f, 0.8f)
            : new Color(1.0f, 0.2f, 0.2f, 0.8f);
        DrawRect(new Rect2(tilePos, new Vector2(TileSize, TileSize)), borderColor, false, 2);

        // Type label
        var font = ThemeDB.FallbackFont;
        string label = _buildCategory switch
        {
            BuildCategory.Structure => _selectedType.ToString(),
            BuildCategory.Road => $"Road ({_selectedRoadType})",
            BuildCategory.Wall => $"Wall ({_selectedWallType}) → {_selectedWallFacing}",
            _ => "Build"
        };
        DrawString(font, tilePos + new Vector2(0, -8), label,
            HorizontalAlignment.Left, 200, 10, Colors.White);

        // Road drag preview
        if (_roadDragging && _buildCategory == BuildCategory.Road)
        {
            var startPos = new Vector2(
                _roadStartX * TileSize + TileSize / 2f,
                _roadStartY * TileSize + TileSize / 2f);
            var endPos = new Vector2(
                _cursorTileX * TileSize + TileSize / 2f,
                _cursorTileY * TileSize + TileSize / 2f);
            DrawLine(startPos, endPos, new Color(0.8f, 0.8f, 0.3f, 0.7f), 3);
        }

        // Wall facing preview
        if (_buildCategory == BuildCategory.Wall)
        {
            DrawWallFacingPreview(tilePos);
        }
    }

    private void DrawWallFacingPreview(Vector2 tilePos)
    {
        Color wallPreview = new(0.8f, 0.8f, 0.2f, 0.7f);
        float w = 3;

        if ((_selectedWallFacing & DirectionMask.N) != 0)
            DrawLine(tilePos, tilePos + new Vector2(TileSize, 0), wallPreview, w);
        if ((_selectedWallFacing & DirectionMask.S) != 0)
            DrawLine(tilePos + new Vector2(0, TileSize), tilePos + new Vector2(TileSize, TileSize), wallPreview, w);
        if ((_selectedWallFacing & DirectionMask.W) != 0)
            DrawLine(tilePos, tilePos + new Vector2(0, TileSize), wallPreview, w);
        if ((_selectedWallFacing & DirectionMask.E) != 0)
            DrawLine(tilePos + new Vector2(TileSize, 0), tilePos + new Vector2(TileSize, TileSize), wallPreview, w);
    }

    private void UpdateCursorPosition()
    {
        var worldPos = GetGlobalMousePosition();
        _cursorTileX = (int)(worldPos.X / TileSize);
        _cursorTileY = (int)(worldPos.Y / TileSize);

        // Validate current position
        _placementValid = _buildCategory switch
        {
            BuildCategory.Structure => _buildEngine?.ValidatePlacement(
                _selectedType, _cursorTileX, _cursorTileY,
                _world?.PlayerNationId ?? "") == null,
            BuildCategory.Road => ValidateRoadPlacement(),
            BuildCategory.Wall => ValidateWallPlacement(),
            _ => false,
        };
    }

    private bool ValidateRoadPlacement()
    {
        if (_world == null || _world.TerrainMap == null) return false;
        if (_cursorTileX < 0 || _cursorTileX >= _world.MapWidth ||
            _cursorTileY < 0 || _cursorTileY >= _world.MapHeight) return false;
        return TerrainRules.IsLand(_world.TerrainMap[_cursorTileX, _cursorTileY]);
    }

    private bool ValidateWallPlacement()
    {
        return ValidateRoadPlacement(); // Same basic validation
    }

    private void ExecutePlacement()
    {
        if (_world?.PlayerNationId == null) return;

        switch (_buildCategory)
        {
            case BuildCategory.Structure:
                EventBus.Instance?.Publish(new BuildRequestEvent(
                    _selectedType, _cursorTileX, _cursorTileY, _world.PlayerNationId));
                break;

            case BuildCategory.Road:
                if (!_roadDragging)
                {
                    // Start road drag
                    _roadDragging = true;
                    _roadStartX = _cursorTileX;
                    _roadStartY = _cursorTileY;
                }
                else
                {
                    // Build road segments along the path
                    BuildRoadPath(_roadStartX, _roadStartY, _cursorTileX, _cursorTileY);
                    _roadDragging = false;
                }
                break;

            case BuildCategory.Wall:
                EventBus.Instance?.Publish(new WallBuildRequestEvent(
                    _cursorTileX, _cursorTileY, _selectedWallFacing, _selectedWallType));
                break;
        }
    }

    /// <summary>Build road segments along a Manhattan path between two points.</summary>
    private void BuildRoadPath(int fromX, int fromY, int toX, int toY)
    {
        int cx = fromX, cy = fromY;

        // First move horizontally, then vertically
        while (cx != toX)
        {
            int nx = cx + (toX > cx ? 1 : -1);
            EventBus.Instance?.Publish(new RoadBuildRequestEvent(cx, cy, nx, cy, _selectedRoadType));
            cx = nx;
        }
        while (cy != toY)
        {
            int ny = cy + (toY > cy ? 1 : -1);
            EventBus.Instance?.Publish(new RoadBuildRequestEvent(cx, cy, cx, ny, _selectedRoadType));
            cy = ny;
        }
    }

    private void CycleSelectedType()
    {
        switch (_buildCategory)
        {
            case BuildCategory.Structure:
                var types = System.Enum.GetValues<StructureType>();
                int idx = System.Array.IndexOf(types, _selectedType);
                _selectedType = types[(idx + 1) % types.Length];
                break;

            case BuildCategory.Road:
                var roadTypes = System.Enum.GetValues<RoadType>();
                int rIdx = System.Array.IndexOf(roadTypes, _selectedRoadType);
                _selectedRoadType = roadTypes[(rIdx + 1) % roadTypes.Length];
                break;

            case BuildCategory.Wall:
                var wallTypes = System.Enum.GetValues<WallType>();
                int wIdx = System.Array.IndexOf(wallTypes, _selectedWallType);
                _selectedWallType = wallTypes[(wIdx + 1) % wallTypes.Length];
                break;
        }
    }

    private static DirectionMask RotateFacing(DirectionMask current) => current switch
    {
        DirectionMask.N => DirectionMask.E,
        DirectionMask.E => DirectionMask.S,
        DirectionMask.S => DirectionMask.W,
        DirectionMask.W => DirectionMask.N,
        _ => DirectionMask.N,
    };

    private void OnBuildCompleted(BuildCompletedEvent ev)
    {
        EventBus.Instance?.Publish(new NotificationEvent(
            $"Built {ev.Type} at ({ev.TileX}, {ev.TileY})", "success"));
    }

    private void OnBuildFailed(BuildFailedEvent ev)
    {
        EventBus.Instance?.Publish(new NotificationEvent(
            $"Cannot build: {ev.Reason}", "warning"));
    }
}
