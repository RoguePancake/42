using Godot;
using System.Linq;
using Warship.Data;
using Warship.Core;
using Warship.Engines;
using Warship.Events;
using Warship.World;
using Warship.UI.HUD;

namespace Warship.UI.Map;

/// <summary>
/// Map orchestrator — owns the rendering stack, chunk system, and build overlay.
///
/// Architecture:
///   - ChunkManager (pure C#) manages chunk data loading/unloading
///   - ZoomController tracks LOD state transitions
///   - Three rendering layers switch based on LOD:
///     * Macro LOD: MacroRenderer (strategic overview)
///     * Hybrid LOD: MacroRenderer + TerrainChunkRenderer blend
///     * Micro LOD: TerrainChunkRenderer + MicroRenderer (full detail)
///   - TerritoryBorderRenderer draws borders/cities at all LODs
///   - ArmySwarmRenderer draws armies with its own 4-level LOD
///   - BuildOverlay handles structure/road/wall placement UI
///
/// Engines (pure C#, created here, communicate via EventBus):
///   - BuildEngine — validates and commits builds
///   - RoadEngine — auto-connects road segments
///   - PathfindingEngine — A* pathfinding across chunks
///   - MovementEngine — army path following
/// </summary>
public partial class MapManager : Node2D
{
    public const int TileSize = MapManagerConstants.TileSize;
    public int MapWidth => _world?.MapWidth ?? TerrainGenerator.DefaultWidth;
    public int MapHeight => _world?.MapHeight ?? TerrainGenerator.DefaultHeight;

    private WorldData? _world;
    private bool _ready;

    // ── Chunk system (source of truth) ──
    private ChunkManager? _chunkManager;

    // ── Rendering layers ──
    private TerrainChunkRenderer? _terrain;
    private TerritoryBorderRenderer? _territory;
    private ArmySwarmRenderer? _armies;
    private MacroRenderer? _macroRenderer;
    private MicroRenderer? _microRenderer;
    private BuildOverlay? _buildOverlay;

    // ── Zoom controller ──
    private ZoomController? _zoomController;
    private LodLevel _currentLod = LodLevel.Macro;

    // ── Pure C# engines ──
    private BuildEngine? _buildEngine;
    private RoadEngine? _roadEngine;
    private PathfindingEngine? _pathfinding;
    private MovementEngine? _movementEngine;

    // ── UI refs ──
    private DossierPanel? _dossier;

    // ── Chunk streaming state ──
    private int _lastCamChunkX = -999;
    private int _lastCamChunkY = -999;

    public override void _Ready()
    {
        GD.Print("[MapManager] Initializing world engine...");

        // Create chunk manager
        _chunkManager = new ChunkManager();

        // Create rendering layers (bottom to top)
        _macroRenderer = new MacroRenderer { Name = "MacroLayer" };
        _terrain = new TerrainChunkRenderer { Name = "TerrainLayer" };
        _microRenderer = new MicroRenderer { Name = "MicroOverlay" };
        _territory = new TerritoryBorderRenderer { Name = "TerritoryLayer" };
        _armies = new ArmySwarmRenderer { Name = "ArmyLayer" };
        _buildOverlay = new BuildOverlay { Name = "BuildOverlay" };

        AddChild(_macroRenderer);
        AddChild(_terrain);
        AddChild(_microRenderer);
        AddChild(_territory);
        AddChild(_armies);
        AddChild(_buildOverlay);

        // ZoomController is added by parent scene or we create it
        _zoomController = GetNodeOrNull<ZoomController>("../ZoomController");
        if (_zoomController == null)
        {
            _zoomController = new ZoomController { Name = "ZoomController" };
            GetParent()?.CallDeferred("add_child", _zoomController);
        }

        // Subscribe to events
        EventBus.Instance?.Subscribe<WorldReadyEvent>(OnWorldReady);
        EventBus.Instance?.Subscribe<LodChangedEvent>(OnLodChanged);
        EventBus.Instance?.Subscribe<BuildCompletedEvent>(OnBuildCompleted);
        EventBus.Instance?.Subscribe<RoadBuiltEvent>(OnRoadBuilt);
        EventBus.Instance?.Subscribe<WallBuiltEvent>(OnWallBuilt);

        _dossier = GetNodeOrNull<DossierPanel>("/root/Main/UILayer/DossierPanel");

        // Start with macro view visible
        SetLodVisibility(LodLevel.Macro);

        GD.Print("[MapManager] World engine layers created.");
    }

    public override void _ExitTree()
    {
        EventBus.Instance?.Unsubscribe<WorldReadyEvent>(OnWorldReady);
        EventBus.Instance?.Unsubscribe<LodChangedEvent>(OnLodChanged);
        EventBus.Instance?.Unsubscribe<BuildCompletedEvent>(OnBuildCompleted);
        EventBus.Instance?.Unsubscribe<RoadBuiltEvent>(OnRoadBuilt);
        EventBus.Instance?.Unsubscribe<WallBuiltEvent>(OnWallBuilt);
    }

    private void OnWorldReady(WorldReadyEvent ev)
    {
        _world = WorldStateManager.Instance?.Data;
        if (_world?.TerrainMap == null)
        {
            GD.PrintErr("[MapManager] WorldReady but no terrain data!");
            return;
        }

        GD.Print($"[MapManager] World ready — {_world.MapWidth}x{_world.MapHeight} tiles");

        // Initialize chunk manager from world data
        _chunkManager!.Initialize(_world);
        int cx = _chunkManager.ChunksX, cy = _chunkManager.ChunksY;
        GD.Print($"[MapManager] Chunk grid: {cx}x{cy} ({cx * cy} total chunks, {ChunkData.Size}x{ChunkData.Size} tiles each)");

        // Initialize renderers
        _terrain?.Initialize(_world);
        _territory?.Initialize(_world);
        _armies?.Initialize(_world);
        _macroRenderer?.Initialize(_chunkManager, _world);
        _microRenderer?.Initialize(_chunkManager, _world);

        // Create pure C# engines
        _pathfinding = new PathfindingEngine(_chunkManager);
        _buildEngine = new BuildEngine(_chunkManager, _world);
        _roadEngine = new RoadEngine(_chunkManager);
        _movementEngine = new MovementEngine(_chunkManager, _pathfinding, _world);

        // Wire build overlay to engine
        _buildOverlay?.SetBuildEngine(_buildEngine);

        _ready = true;
        _lastCamChunkX = -999; // Force initial chunk load

        GD.Print("[MapManager] All systems initialized. World engine online.");
    }

    public override void _Process(double delta)
    {
        // Fallback late init
        if (!_ready)
        {
            var data = WorldStateManager.Instance?.Data;
            if (data?.TerrainMap != null)
            {
                _world = data;
                GD.Print("[MapManager] Late init — picking up world data from WSM.");
                OnWorldReady(new WorldReadyEvent(data.Seed, data.PlayerNationId ?? ""));
            }
            return;
        }

        // Stream chunks based on camera position
        UpdateChunkStreaming();

        // Process army movement
        _movementEngine?.ProcessTick((float)delta);
        _movementEngine?.UpdateArmyChunkAssignments();
    }

    /// <summary>
    /// Stream chunks in/out based on camera viewport.
    /// Both the terrain renderer and chunk-based renderers get updated.
    /// </summary>
    private void UpdateChunkStreaming()
    {
        if (_chunkManager == null || _zoomController == null) return;

        var camera = GetViewport().GetCamera2D();
        if (camera == null) return;

        var camPos = camera.GlobalPosition;
        int camTileX = (int)(camPos.X / TileSize);
        int camTileY = (int)(camPos.Y / TileSize);
        int camChunkX = camTileX / ChunkData.Size;
        int camChunkY = camTileY / ChunkData.Size;

        // Only re-evaluate when camera moves to a new chunk
        if (camChunkX == _lastCamChunkX && camChunkY == _lastCamChunkY)
            return;
        _lastCamChunkX = camChunkX;
        _lastCamChunkY = camChunkY;

        int loadRadius = _zoomController.GetChunkLoadRadius();
        int unloadRadius = loadRadius + 3;

        var newChunks = _chunkManager.UpdateLoadedChunks(
            camTileX, camTileY, loadRadius, unloadRadius, 8);

        // Bake new chunks into renderers
        foreach (var coord in newChunks)
        {
            var loadedChunks = _chunkManager.GetLoadedChunks();
            if (!loadedChunks.TryGetValue(coord, out var chunk)) continue;

            _macroRenderer?.BakeChunk(coord, chunk);

            if (_currentLod == LodLevel.Micro)
                _microRenderer?.BakeChunkOverlay(coord, chunk);
        }

        // Unload chunks from renderers that are no longer in the loaded set
        var loaded = _chunkManager.GetLoadedChunks();

        // Check macro renderer for stale chunks (periodic, not every frame)
        // This is handled by the chunk manager's unload logic
    }

    private void OnLodChanged(LodChangedEvent ev)
    {
        _currentLod = ev.NewLod;
        SetLodVisibility(ev.NewLod);
        GD.Print($"[MapManager] LOD: {ev.OldLod} → {ev.NewLod} (zoom={ev.Zoom:F4})");

        // When entering micro, bake overlays for all loaded chunks
        if (ev.NewLod == LodLevel.Micro && _chunkManager != null)
        {
            foreach (var (coord, chunk) in _chunkManager.GetLoadedChunks())
                _microRenderer?.BakeChunkOverlay(coord, chunk);
        }

        // When leaving micro, clear micro overlays to save memory
        if (ev.OldLod == LodLevel.Micro)
            _microRenderer?.ClearAll();
    }

    /// <summary>
    /// Show/hide rendering layers based on current LOD level.
    /// The world data doesn't change — only the visual representation.
    /// </summary>
    private void SetLodVisibility(LodLevel lod)
    {
        switch (lod)
        {
            case LodLevel.Macro:
                if (_macroRenderer != null) _macroRenderer.Visible = true;
                if (_terrain != null) _terrain.Visible = false;
                if (_microRenderer != null) _microRenderer.Visible = false;
                break;

            case LodLevel.Hybrid:
                if (_macroRenderer != null) _macroRenderer.Visible = true;
                if (_terrain != null) _terrain.Visible = true;
                if (_microRenderer != null) _microRenderer.Visible = false;
                break;

            case LodLevel.Micro:
                if (_macroRenderer != null) _macroRenderer.Visible = false;
                if (_terrain != null) _terrain.Visible = true;
                if (_microRenderer != null) _microRenderer.Visible = true;
                break;
        }

        // These are always visible
        if (_territory != null) _territory.Visible = true;
        if (_armies != null) _armies.Visible = true;
        if (_buildOverlay != null) _buildOverlay.Visible = true;
    }

    // ── Build/road/wall event handlers: invalidate chunk renderers ──

    private void OnBuildCompleted(BuildCompletedEvent ev)
    {
        var coord = ChunkManager.WorldToChunkCoord(ev.TileX, ev.TileY);
        _microRenderer?.InvalidateChunk(coord);
        // Re-bake macro chunk too
        if (_chunkManager != null)
        {
            var chunks = _chunkManager.GetLoadedChunks();
            if (chunks.TryGetValue(coord, out var chunk))
                _macroRenderer?.BakeChunk(coord, chunk);
        }
    }

    private void OnRoadBuilt(RoadBuiltEvent ev)
    {
        InvalidateChunksAround(ev.FromX, ev.FromY);
        InvalidateChunksAround(ev.ToX, ev.ToY);
    }

    private void OnWallBuilt(WallBuiltEvent ev)
    {
        InvalidateChunksAround(ev.TileX, ev.TileY);
    }

    private void InvalidateChunksAround(int tileX, int tileY)
    {
        var coord = ChunkManager.WorldToChunkCoord(tileX, tileY);
        if (_chunkManager == null) return;
        var chunks = _chunkManager.GetLoadedChunks();
        if (chunks.TryGetValue(coord, out var chunk))
        {
            _macroRenderer?.BakeChunk(coord, chunk);
            if (_currentLod == LodLevel.Micro)
                _microRenderer?.BakeChunkOverlay(coord, chunk);
        }
    }

    // ── Input handling ──

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_world == null || !_ready) return;

        // Don't handle map clicks when build mode is active
        if (_buildOverlay != null && _buildOverlay.IsBuildMode) return;

        if (@event is InputEventMouseButton mb && mb.Pressed)
        {
            var worldPos = GetGlobalMousePosition();
            var tile = PixelToTile(worldPos);

            if (mb.ButtonIndex == MouseButton.Left)
                HandleLeftClick(worldPos, tile);
            else if (mb.ButtonIndex == MouseButton.Right)
                HandleRightClick(tile);
        }
    }

    private void HandleLeftClick(Vector2 worldPos, Vector2I tile)
    {
        if (_world == null || _armies == null) return;

        // Check character click
        var character = _world.Characters.FirstOrDefault(c =>
            c.TileX == tile.X && c.TileY == tile.Y);
        if (character != null)
        {
            _dossier?.ShowCharacter(character);
            _armies.SelectedArmyId = null;
            GetViewport().SetInputAsHandled();
            return;
        }

        // Check army click
        var army = _armies.GetArmyAtPosition(worldPos);
        if (army != null)
        {
            _armies.SelectedArmyId = army.Id;
            GD.Print($"[Map] Selected: {army.Name} ({army.TotalStrength} troops)");
        }
        else
        {
            _armies.SelectedArmyId = null;
        }
        GetViewport().SetInputAsHandled();
    }

    private void HandleRightClick(Vector2I tile)
    {
        if (_world == null || _armies == null) return;

        if (_armies.SelectedArmyId != null)
        {
            // Move selected army via pathfinding
            var army = _world.Armies.FirstOrDefault(a => a.Id == _armies.SelectedArmyId);
            if (army != null)
            {
                // Use ArmyMoveRequested which triggers pathfinding in MovementEngine
                EventBus.Instance?.Publish(new ArmyMoveRequested(
                    army.Id, tile.X, tile.Y));
                GD.Print($"[Map] {army.Name} → ({tile.X}, {tile.Y}) via pathfinding");
            }
        }
        else if (_world.PlayerNationId != null)
        {
            // Set global command target
            var player = GetNationById(_world, _world.PlayerNationId);
            if (player != null)
            {
                player.CommandTargetX = tile.X;
                player.CommandTargetY = tile.Y;
                _territory?.ForceRedraw();
            }
        }
        GetViewport().SetInputAsHandled();
    }

    // ── Helpers ──

    public static Vector2I PixelToTile(Vector2 pixel)
    {
        return new Vector2I(
            (int)(pixel.X / TileSize),
            (int)(pixel.Y / TileSize));
    }

    public static Vector2 TileToPixel(int tx, int ty)
    {
        return new Vector2(
            tx * TileSize + TileSize / 2f,
            ty * TileSize + TileSize / 2f);
    }

    public static NationData? GetNationById(WorldData world, string nationId)
    {
        return world.Nations.FirstOrDefault(n => n.Id == nationId);
    }

    public static int GetNationIndex(WorldData world, string nationId)
    {
        for (int i = 0; i < world.Nations.Count; i++)
            if (world.Nations[i].Id == nationId) return i;
        return -1;
    }
}
