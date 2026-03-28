# [ARCHIVED] WARSHIP — 30-Phase Plan
## Zero to Playable Test Game

> **ARCHIVED 2026-03-24:** This plan is superseded by `docs/ROADMAP.md`.
> Phases 1-14 were completed then replaced by the real-world map pivot.
> Phases 15-30 were partially reimplemented under the Full Authority plan.
> Kept for historical reference only.

Each phase produces something VISIBLE or TESTABLE.
If you can't see it or click it, it's not done.

---

## BLOCK A: "TILES ON SCREEN" — Phases 1-6

### Phase 1 — Project Skeleton
```
CREATE:
  warship-godot/
  project.godot         (C# enabled, .NET 6+)
  Warship.csproj        (add Newtonsoft.Json NuGet)
  CLAUDE.md             (copy from starter kit)
  docs/                 (copy all doc files)
  All src/ directories  (empty, with .gitkeep)
  All scenes/ dirs
  All assets/ dirs
  .gitignore            (Godot + C# + .NET + .import)

GIT:
  git init
  git add -A
  git commit -m "Phase 1: project skeleton"
  gh repo create warship-godot --private --source=. --push
  git checkout -b develop && git push -u origin develop
  git checkout -b feature/block-a && git push -u origin feature/block-a

VERIFY:
  Open in Godot 4 editor. Build succeeds (no errors).
  Empty gray window when you hit F5.
```

### Phase 2 — Empty Main Scene + Dark Background
```
CREATE:
  scenes/Main.tscn
    Root: Node2D named "Main"
    Child: ColorRect named "Background" (full screen, color #0a0a1a)
    Child: Camera2D named "MainCamera" (current = true)

  Set Main.tscn as main scene in project.godot.

VERIFY:
  F5 → dark screen appears. No errors in output.
```

### Phase 3 — Terrain Tileset (32×32 pixel art)
```
CREATE:
  assets/tilesets/terrain.png — a tile atlas with 8 terrain types
  
  Layout: 8 tiles in a row, each 32×32 pixels.
  Generate this programmatically with a C# EditorScript or by creating
  the pixel art directly. Each tile should look SNES-quality:

  Tile 0: DEEP WATER  — Dark blue (#1c3a6e) with wave lines
  Tile 1: WATER       — Medium blue (#2864a8) with foam hints  
  Tile 2: SAND/BEACH  — Warm tan (#d4b870) with pebble dots
  Tile 3: GRASS       — Rich green (#48a848) with flower dots and grass tufts
  Tile 4: FOREST      — Dark green (#286828) with tree shapes (trunk + canopy)
  Tile 5: HILLS       — Green-brown with curved hill silhouette
  Tile 6: MOUNTAIN    — Gray-brown (#8a7560) with peaked triangle + snow cap
  Tile 7: SNOW        — White-blue (#e0e8f0) with sparkle dots

  Each tile needs enough pixel detail to look good when tiled together.
  Use at least 3-4 shades per tile for depth.

  Also create the TileSet resource in Godot:
    assets/tilesets/terrain.tres
    Physics layer (for blocking movement on water/mountains)
    Terrain sets for auto-tiling (optional but nice)

VERIFY:
  Open terrain.png in Godot. 8 distinct, recognizable terrain tiles visible.
  Each looks like proper SNES pixel art, not programmer art.
```

### Phase 4 — TileMapLayer + Hardcoded Test Map
```
CREATE:
  Add to Main.tscn:
    TileMapLayer named "TerrainLayer"
    Assign terrain.tres tileset

  src/UI/Map/MapManager.cs (partial class : Node2D)
    _Ready():
      Get TerrainLayer reference
      Fill a 40×30 grid with hardcoded terrain:
        - Water border around edges (3 tiles deep)
        - Grass in the middle
        - Mountain cluster in upper right
        - Forest band across middle
        - Sand patch in lower area
        - Small lake (water) in center-left

VERIFY:
  F5 → You see a colorful tile map filling the screen.
  Water around edges, green grass, brown mountains, dark forests.
  It looks like a tiny SNES overworld.
```

### Phase 5 — Camera: Zoom + Pan
```
CREATE:
  src/UI/Map/MapCamera.cs (partial class : Camera2D)
    Fields:
      float _zoom = 1.0f (range 0.3 to 4.0)
      float _zoomSpeed = 0.1f
      float _panSpeed = 400f
      bool _dragging = false
      Vector2 _dragStart

    _UnhandledInput(InputEvent e):
      Mouse scroll up → zoom in (decrease zoom value, Zoom = new Vector2)
      Mouse scroll down → zoom out
      Middle mouse down → start drag
      Middle mouse up → stop drag
      Mouse motion while dragging → pan camera

    _Process(double delta):
      WASD keys → pan camera (scaled by zoom level)
      Clamp camera position to map bounds

    Zoom should be SMOOTH (lerp toward target zoom)

VERIFY:
  F5 → scroll wheel zooms in/out smoothly.
  WASD moves the camera. Middle-click drag pans.
  Can zoom in to see individual tile pixels.
  Can zoom out to see the whole map.
```

### Phase 6 — Procedural Terrain with Perlin Noise
```
CREATE:
  src/World/TerrainGenerator.cs (plain C# class)
    static method: int[,] Generate(int width, int height, int seed)
    
    Uses simplified Perlin noise (implement from scratch or use a simple
    value noise with interpolation — no external libraries):
    
    float Noise(float x, float y, int seed):
      Hash-based pseudo-random per grid point
      Bilinear interpolation between grid points
    
    float FBM(float x, float y, int seed, int octaves=5):
      Layered noise at different frequencies
      Sum: amplitude * Noise(x * frequency, y * frequency)
      amplitude *= 0.5, frequency *= 2 each octave
    
    For each tile (x, y):
      float height = FBM(x * 0.04, y * 0.04, seed)
      float moisture = FBM(x * 0.05 + 100, y * 0.05 + 100, seed)
      
      // Edge falloff — ocean around map edges
      float edgeDist = min distance to any edge / (mapSize * 0.15)
      height *= clamp(edgeDist, 0, 1)
      
      // Height → terrain type
      < 0.20 → DEEP_WATER (0)
      < 0.28 → WATER (1)
      < 0.33 → SAND (2)
      < 0.50 → moisture > 0.55 ? FOREST (4) : GRASS (3)
      < 0.62 → HILLS (5)
      < 0.78 → MOUNTAIN (6)
      >= 0.78 → SNOW (7)
    
    Returns int[width, height] of tile indices.

  Update MapManager.cs:
    _Ready():
      int[,] terrain = TerrainGenerator.Generate(80, 50, 42)
      Loop: set each TileMapLayer cell from the array

VERIFY:
  F5 → organic-looking map with natural coastlines, mountain chains,
  forest patches, grassy plains. Different every seed.
  Change seed in code → different map. Same seed → same map.
  It looks like a real continent, not random noise.
```

---

## BLOCK B: "A LIVING WORLD" — Phases 7-12

### Phase 7 — Nations + Territory Colors
```
CREATE:
  src/Data/Models.cs — initial data models:
    
    public class NationData {
      public string Id;
      public string Name;
      public string Color; // hex
      public int CapitalX, CapitalY;
      public float Stability = 0.7f;
      public float Treasury = 1000;
      public float MilitaryStrength = 0.3f;
      public float GDP = 500;
      public List<string> ProvinceIds = new();
      public bool IsPlayer = false;
    }
    
    public class WorldData {
      public int Seed;
      public int TurnNumber = 1;
      public int[,] TerrainMap;
      public int[,] OwnershipMap; // nation index per tile, -1 = unclaimed
      public List<NationData> Nations = new();
    }

  src/World/WorldGenerator.cs:
    Generate terrain (from Phase 6)
    Place 6 nations on land tiles spread across the map:
      Nation 0: IRONPACT (red #c03030) — Hegemon
      Nation 1: MERIDIAN (blue #3070d0) — Commercial
      Nation 2: VOLKREN (purple #8040b0) — Revolutionary
      Nation 3: ASHWARD (green #30a040) — Traditionalist
      Nation 4: FREEHOLD (orange #d08020) — Survival
      Nation 5: SELVARA (gold #d8b020) — Player's free state
    
    Flood-fill from each capital to claim territory.
    Stop at water tiles and when territories meet.
    Result: OwnershipMap[x,y] = nation index.
    
  Update MapManager.cs:
    After terrain: add a second TileMapLayer or CanvasLayer
    For each land tile: draw a semi-transparent color overlay (nation color, ~20% opacity)
    Draw border lines between different nations (dark, 1-2px)

VERIFY:
  F5 → Terrain map with 6 colored nation territories.
  Visible borders between nations. Ocean has no ownership.
  Player's gold territory is small (4-8 tiles) surrounded by larger nations.
```

### Phase 8 — City Sprites on Map
```
CREATE:
  assets/sprites/cities/capital.png — multi-tile capital sprite
    32×32 or 64×64 pixel art: cluster of buildings with colored roofs,
    walls, a flagpole with a small flag, lit windows (yellow pixels),
    chimney with smoke suggestion. Should look like a Pokemon/FF6 town.
  
  assets/sprites/cities/town.png — smaller town sprite
    32×32: 2-3 buildings, simpler than capital.

  src/UI/Map/CityRenderer.cs (partial class : Node2D)
    On world ready:
      For each nation's capital: place capital.png sprite at tile position
      Modulate sprite color slightly toward nation color
      For 2-3 other towns per nation: place town.png at random owned tiles
    
    Capital should have a small flag sprite child that has nation's color.

VERIFY:
  F5 → Cities visible on the map at nation capitals.
  Each has buildings with the nation's color visible.
  Capitals look bigger/more important than towns.
  Cities look like they belong on an SNES overworld.
```

### Phase 9 — Rivers (Bézier Curves)
```
CREATE:
  src/World/RiverGenerator.cs (plain C# class)
    Generate(int[,] terrain, int seed) → List<Vector2[]>
    
    For 6-10 rivers:
      Find a mountain or hill tile as source
      Trace downhill toward water:
        At each step, move toward lowest neighbor
        Add lateral wobble: perpendicular offset using sin(step * 0.2) * 1.5
        Store each point as pixel coordinates (tileX * 32 + 16, tileY * 32 + 16)
        Stop when reaching water or map edge
      
      Smooth the raw points into a Bézier-friendly path.
      Return the list of point arrays.

  src/UI/Map/RouteRenderer.cs (partial class : Node2D)
    DrawRivers(List<Vector2[]> rivers):
      For each river path:
        Use _Draw() override with DrawPolyline or manual Bézier:
        
        Layer 1: dark brown bank line (width 6px, color #554830)
        Layer 2: blue water line (width 3px, color #2864a8)  
        Layer 3: light highlight (width 1px, color #4890cc)
    
    Call QueueRedraw() to trigger _Draw().

VERIFY:
  F5 → Rivers wind naturally from mountains toward the sea.
  They curve, not straight lines. Multiple rivers visible.
  Brown banks visible on either side of blue water.
  Rivers look hand-drawn, not tile-aligned.
```

### Phase 10 — Unit Sprites: Tank on the Map
```
CREATE:
  assets/sprites/units/tank.png — 32×32 pixel art tank
    Colored body (will be modulated to nation color)
    Visible treads (dark lines on sides)
    Turret with barrel pointing right
    Small shadow underneath
    Should be recognizable as a tank at small sizes.

  src/UI/Map/UnitManager.cs (partial class : Node2D)
    SpawnUnit(string type, int tileX, int tileY, NationData nation):
      Create Sprite2D child
      Load texture
      Position at tile center (tileX * 32 + 16, tileY * 32 + 16)
      Modulate to nation color
      Add to tracking dictionary
    
    On world ready:
      For player nation: spawn 1 tank at capital
      For each AI nation: spawn 1 tank at capital

  Click detection:
    _UnhandledInput: on left click, raycast to find if a unit was clicked
    If unit clicked: draw selection highlight (white rectangle around unit)
    Store selectedUnit reference.

VERIFY:
  F5 → 6 tanks visible on map, one per nation capital.
  Each tank is tinted to its nation's color.
  Click a tank → white selection box appears around it.
  Click elsewhere → selection clears.
```

### Phase 11 — Unit Movement (Click to Move)
```
CREATE:
  Update UnitManager.cs:
    When unit is selected AND right-click on a tile:
      Calculate path from unit position to target (simple: straight line
      for now, A* pathfinding can come later)
      
      Animate movement:
        Use a Tween or _Process loop
        Move sprite from current position to target at 100px/sec
        While moving, unit.IsMoving = true
        On arrival, snap to tile center, IsMoving = false
    
    Movement rules:
      Can't move onto DEEP_WATER or WATER tiles
      Can't move onto MOUNTAIN tiles (for now)
      Can move onto GRASS, FOREST, HILLS, SAND, SNOW
      Max movement: 3 tiles per turn (store moves remaining)

  Add basic UnitData to Models.cs:
    public class UnitData {
      public string Id;
      public string NationId;
      public string Type; // "tank", "soldier", "cannon"
      public int TileX, TileY;
      public float Strength = 1.0f;
      public int MovesRemaining = 3;
      public bool IsSelected = false;
    }

VERIFY:
  F5 → Click your tank to select it.
  Right-click a grass tile → tank WALKS there smoothly (animated, not teleport).
  Tank stops at water. Can't walk through mountains.
  Satisfying to watch it move across the tile map.
```

### Phase 12 — Multiple Unit Types
```
CREATE:
  assets/sprites/units/soldier.png — 32×32 pixel art soldier
    Visible helmet, body, rifle, walking pose
  
  assets/sprites/units/cannon.png — 32×32 pixel art artillery
    Wheeled gun carriage, long barrel

  Update WorldGenerator / UnitManager:
    Player nation gets: 2 tanks, 3 soldiers, 1 cannon (placed near capital)
    Each AI nation gets: 1 tank, 2 soldiers (placed near capital)
    
  Update UnitPanel.cs (partial class : Control):
    When unit selected, show popup with:
      Unit type icon
      Nation name (colored)
      Strength bar (green fill)
      Moves remaining: X/3
      
    SNES blue window style:
      Dark blue background (#18184a)
      Bright border top-left (#e0e0f0)
      Dark border bottom-right (#080828)
      White monospace text

VERIFY:
  F5 → Multiple unit types visible on map.
  Each has distinct sprite. Click any → info popup appears.
  Select and move each type. All animate smoothly.
  You can see the player's small army and the AI nations' forces.
```

---

## BLOCK C: "IT'S A GAME ENGINE" — Phases 13-18

### Phase 13 — SimRng + EventBus + ISimEngine
```
CREATE:
  src/Core/SimRng.cs (plain C#):
    Static seeded RNG. SetSeed(int), NextFloat(), Range(int,int),
    Chance(float probability) → bool, Pick<T>(List<T>), Shuffle<T>(List<T>)

  src/Core/ISimEngine.cs (plain C#):
    interface ISimEngine:
      string EngineName { get; }
      int Priority { get; }
      void Initialize(WorldStateManager state, EventBus bus);
      void OnTurnPhase(TurnContext ctx, TurnPhase phase);
      void Shutdown();
    
    enum TurnPhase { TurnOpen, Resource, Economy, Trade, Unrest, Politics,
      Diplomacy, AIDecision, PlayerAction, Military, Intel, Events, News, TurnClose }
    
    class TurnContext { int TurnNumber; int Year; int Month; }

  src/Core/EventBus.cs (partial class : Node, Autoload):
    Dictionary<Type, List<Delegate>> _subs
    Subscribe<T>(Action<T>), Unsubscribe<T>(Action<T>), Publish<T>(T evt)
    Error isolation: try/catch each subscriber, log, continue.
    static Instance set in _Ready()

  src/Events/GameEvents.cs:
    record TurnAdvancedEvent(int Turn, int Year, int Month);
    record WorldReadyEvent(int Seed, string PlayerNationId);
    record UnitMovedEvent(string UnitId, int FromX, int FromY, int ToX, int ToY);
    record BattleResolvedEvent(string AttackerId, string DefenderId, 
      int TileX, int TileY, bool AttackerWon, float AttackerLoss, float DefenderLoss);
    record WarDeclaredEvent(string AggressorId, string DefenderId);
    record PeaceSignedEvent(string NationAId, string NationBId);
    record ResourcesCollectedEvent(string NationId, float Income);
    record NationStatsChangedEvent(string NationId);

VERIFY:
  EventBus autoload works. Test: subscribe to a dummy event, publish it,
  handler fires. GD.Print confirms. SimRng: seed 42, generate 5 floats,
  reseed 42, same 5 floats.
```

### Phase 14 — WorldStateManager
```
CREATE:
  src/Core/WorldStateManager.cs (partial class : Node, Autoload):
    private WorldData _world;
    static Instance;
    
    InitializeWorld(WorldData world, string playerId)
    GetWorld() → WorldData (reference for now, deep clone later)
    GetNation(string id) → NationData
    GetPlayerNation() → NationData
    
    ApplyNationDelta(string nationId, NationDelta delta):
      Find nation, apply each non-null delta field:
        if (delta.TreasuryDelta.HasValue) nation.Treasury += delta.TreasuryDelta.Value;
        if (delta.StabilityDelta.HasValue) nation.Stability = Clamp(nation.Stability + delta.StabilityDelta.Value, 0, 1);
        // etc for each field
      Publish NationStatsChangedEvent
    
    public class NationDelta {
      public string NationId;
      public float? TreasuryDelta;
      public float? StabilityDelta;
      public float? GDPDelta;
      public float? MilitaryDelta;
      public float? InfamyDelta;
      public float? PrestigeDelta;
      public float? WarExhaustionDelta;
      public float? LegitimacyDelta;
    }

VERIFY:
  WSM autoload works. Create nation with Treasury=1000.
  ApplyNationDelta(TreasuryDelta=-200). GetNation shows Treasury=800.
```

### Phase 15 — TurnEngine + WorldSimulationCore
```
CREATE:
  src/Core/TurnEngine.cs (plain C#):
    List<ISimEngine> _engines (sorted by Priority)
    RegisterEngine(ISimEngine e)
    
    ExecuteTurn(WorldStateManager state, EventBus bus):
      Create TurnContext
      For each phase in TurnPhase enum:
        For each engine:
          engine.OnTurnPhase(ctx, phase)
      Increment turn number
      bus.Publish(new TurnAdvancedEvent(...))
  
  src/Core/WorldSimulationCore.cs (partial class : Node, Autoload):
    TurnEngine _turnEngine
    static Instance
    
    _Ready(): create TurnEngine, get refs to EventBus + WSM
    RegisterEngine(ISimEngine e): add to TurnEngine
    AdvanceTurn(): _turnEngine.ExecuteTurn(wsm, bus)

VERIFY:
  All 3 autoloads boot in order. Console shows:
  [EventBus] Online.
  [WSM] Online.
  [WSC] Online.
  Call AdvanceTurn() → TurnAdvancedEvent fires. Turn number increments.
```

### Phase 16 — ResourceEngine + EconomicEngine
```
CREATE:
  src/Engines/ResourceEngine.cs:
    Priority: 1, Phase: Resource
    For each nation:
      income = nation.ProvinceCount * 50 * (1 + nation.GDP / 1000)
      Apply TreasuryDelta = +income
      Publish ResourcesCollectedEvent

  src/Engines/EconomicEngine.cs:
    Priority: 2, Phase: Economy
    For each nation:
      expenses = nation.MilitaryStrength * 300 + nation.ProvinceCount * 20
      Apply TreasuryDelta = -expenses
      If treasury < 0: Apply StabilityDelta = -0.02
      Recalculate GDP based on provinces and treasury trend
  
  Register both in GameBootstrapper.

VERIFY:
  Advance 5 turns. Print player treasury each turn.
  Should see income coming in and expenses going out.
  Net positive if economy is healthy. Numbers change each turn.
```

### Phase 17 — HUD: Top Bar (SNES Style)
```
CREATE:
  src/UI/HUD/TopBar.cs (partial class : Control)
    SNES blue window background:
      StyleBoxFlat: bg_color #18184a
      Border: 2px, top+left #e0e0f0, bottom+right #080828
      Inner border: 1px, top+left #8080b0, bottom+right #282858
    
    Labels (monospace pixel font, white):
      "☢ WARSHIP"  (gold #f8d830)
      "│"
      Nation name (nation color)
      "│"
      "💰 [treasury]g" (gold)
      "│"
      Stability bar (green fill on dark bg, 60px wide, 8px tall)
      "│"
      "T[turn] Y[year]"
      "│"
      "☢×[nukes]"  (gold if > 0, gray if 0)
    
    Subscribe to TurnAdvancedEvent → update all values.
    Subscribe to NationStatsChangedEvent → update stats.
    
    Position: top of screen, full width, 32px tall.

VERIFY:
  F5 → SNES blue bar across top of screen.
  Shows nation name, treasury, stability bar, turn number.
  Advance turn → numbers update. Treasury changes. Turn increments.
  Looks like a Final Fantasy menu bar.
```

### Phase 18 — End Turn Button + Turn Controls
```
CREATE:
  Add to TopBar or as separate Control:
    Button "▶ END TURN" — SNES styled:
      Normal: blue bg with beveled border
      Hover: slightly lighter
      Pressed: inverted bevel
      Gold text (#f8d830), bold monospace
    
    On click: WorldSimulationCore.Instance.AdvanceTurn()
    
  Also handle keyboard:
    SPACE or ENTER → end turn
    
  Show brief turn transition:
    On TurnAdvancedEvent:
      Flash "Turn [N]" text briefly in center of screen
      Or: brief dim overlay that fades out (100ms)

VERIFY:
  F5 → Click END TURN → turn advances, HUD updates, flash appears.
  Press SPACE → same thing. Can rapidly advance turns.
  Economy runs each turn. Treasury changes visibly.
```

---

## BLOCK D: "THINGS HAPPEN" — Phases 19-24

### Phase 19 — Title Screen + Opening Speech
```
CREATE:
  scenes/TitleScreen.tscn:
    Dark background (#0a0a18)
    "WARSHIP" text — large, gold (#f8d830), centered, pixel font
    Subtitle: "Leaders of the Warship" — smaller, white
    Menu (SNES blue windows):
      "NEW GAME"  ← clickable
      "LOAD GAME" ← grayed out (future)
      "QUIT"      ← exits app
    
    Press NEW GAME → transition to speech screen.
  
  scenes/SpeechScreen.tscn:
    Black background
    Opening speech text (from GAME_FLOW.md) revealed line by line
    Each line fades in over 0.5 seconds
    Press SPACE or click to skip to next line / skip all
    Music: tense ambient if available, silence if not
    
    After speech completes: load GameWorld scene with camera zoomed
    out, then slowly zoom to player's capital.
  
  Set TitleScreen.tscn as project main scene.
  GameBootstrapper moves to GameWorld.tscn.

VERIFY:
  F5 → Title screen appears. Looks like a real SNES game menu.
  Click NEW GAME → opening speech plays, text appears dramatically.
  After speech → map loads, camera finds your nation.
```

### Phase 20 — Basic AI Movement
```
CREATE:
  src/Engines/AIDecisionEngine.cs:
    Priority: 7, Phase: AIDecision
    
    For each non-player nation:
      Simple behavior for now:
        Pick one random military unit
        Pick a random adjacent owned tile
        Move unit there
        (This just makes the map feel ALIVE — units moving around)
    
    Publish UnitMovedEvent for each AI unit move.
  
  UnitManager subscribes to UnitMovedEvent:
    Animate AI unit movements on map (same smooth walk as player units)

VERIFY:
  F5 → advance several turns.
  AI nations' tanks and soldiers visibly move around their territory.
  The map feels alive — things are happening even when you do nothing.
```

### Phase 21 — Basic Combat
```
CREATE:
  src/Engines/MilitaryEngine.cs:
    Priority: 9, Phase: Military
    
    Check all tiles: if two units from nations at war occupy same tile:
      Attacker strength = unit.Strength * (0.9 + SimRng.NextFloat() * 0.2)
      Defender strength = unit.Strength * (0.9 + SimRng.NextFloat() * 0.2)
        + terrain bonus (forest +10%, mountain +20%, fort +10% per level)
      
      If attacker > defender:
        Defender loses (difference * 0.3) strength
        If defender strength < 0.1: unit destroyed, attacker occupies tile
      Else:
        Attacker loses (difference * 0.3) strength
        Attacker pushed back to previous tile
      
      Publish BattleResolvedEvent
  
  Visual:
    On BattleResolvedEvent:
      Flash red/orange at battle tile for 0.5 seconds
      Show floating text: "BATTLE" with damage numbers
      Losing unit sprite blinks and fades if destroyed

VERIFY:
  Use debug: declare war between player and AI nation.
  Move player tank into AI tank's tile.
  Next turn → battle resolves. Damage visible. Winner stays.
  Loser retreats or dies with visual feedback.
```

### Phase 22 — War Declaration + Peace
```
CREATE:
  Add to NationPanel.cs (or create WarPanel.cs):
    When viewing an AI nation:
      Button: "DECLARE WAR" (red tint)
      Click → WarDeclaredEvent published
      Both nations' AtWarWith lists updated
      HUD shows war indicator
    
    When at war with a nation:
      Button: "OFFER PEACE" (green tint)  
      Click → PeaceSignedEvent published
      AtWarWith lists cleared
      
  src/Engines/DiplomacyEngine.cs:
    Priority: 6, Phase: Diplomacy
    Track AtWarWith per nation
    On WarDeclaredEvent: update both nations' war status
    On PeaceSignedEvent: clear war status
    
  Update MilitaryEngine:
    Only resolve combat between nations that are AtWarWith each other.
    Units from non-warring nations can share tiles peacefully.

VERIFY:
  F5 → Click a rival nation. Click DECLARE WAR.
  War icon appears. Now you can move units into their territory.
  Combat resolves when units meet. Click OFFER PEACE → war ends.
```

### Phase 23 — News Feed (Bottom Panel)
```
CREATE:
  src/Engines/NewsEngine.cs:
    Priority: 12, Phase: News
    Collect all events from this turn
    Format into NewsItem objects:
      struct NewsItem { string Headline; string Category; Color CategoryColor; int Turn; }
    Categories:
      WAR (red), TRADE (green), DIPLO (blue), EVENT (orange), INTEL (purple)
    
    Store current turn news + last 5 turns of history.

  src/UI/HUD/BottomPanel.cs (partial class : Control):
    SNES blue window, bottom of screen, 100px tall
    Tab buttons: "📰 NEWS" | "📋 ORDERS" 
    
    NEWS tab:
      Scrollable list of NewsItem entries
      Color-coded category label on left
      Headline text on right
      Most recent at top
    
    ORDERS tab (placeholder for now):
      Text: "No orders queued. Select a unit or click a command."

VERIFY:
  F5 → Bottom panel visible with SNES styling.
  Advance turns → news items appear: "Turn 3: Resources collected",
  "IRONPACT forces moved", "Battle at tile (12, 8)".
  Declare war → "WAR: SELVARA declares war on IRONPACT" appears in red.
```

### Phase 24 — Nation Panel (Click Territory)
```
CREATE:
  src/UI/Panels/NationPanel.cs (partial class : Control):
    Slides in from right side when clicking a nation's territory on the map.
    
    SNES blue window style. Width: 220px.
    
    Content:
      Nation name (colored, bold)
      Leader: "[Generated Name]"
      ─────────────────
      STATS (HP bar style):
        STR [====----] 45%
        STAB [======--] 72%
        GOLD: 1,240g
        GDP:  580/turn
        PRES: 34
      ─────────────────
      RELATIONS WITH YOU:
        [colored bar -100 to +100]
        "Neutral" / "Friendly" / "Hostile"
      ─────────────────
      STATUS: AT PEACE / AT WAR
      ─────────────────
      ACTIONS:
        [DECLARE WAR]   (if at peace)
        [OFFER PEACE]   (if at war)
        [PROPOSE TRADE]
        [CLOSE]
    
    Click map tile:
      If tile is owned by a nation != player → open NationPanel for that nation
      If tile is owned by player → open player info or unit panel
      If tile is water/unclaimed → close any panel

VERIFY:
  F5 → Click on red IRONPACT territory.
  Panel slides in from right. Shows their stats as SNES-style bars.
  Shows relation with you. Action buttons visible.
  Click DECLARE WAR → war starts, panel updates to show AT WAR status.
  Click somewhere else → panel closes.
```

---

## BLOCK E: "PLAYABLE STRATEGY" — Phases 25-30

### Phase 25 — Budget Panel + Faction Bars
```
CREATE:
  src/UI/Panels/BudgetPanel.cs:
    SNES window. Opens from HUD button or keyboard shortcut (B).
    
    4 sliders (SNES styled — blue track, gold handle):
      MILITARY:  [====|------] 35%
      INFRA:     [===|-------] 25%
      RESEARCH:  [===|-------] 25%
      SOCIAL:    [==|--------] 15%
    
    Sliders must sum to 100%. When one moves up, others adjust down.
    
    Effects preview text:
      "MIL 35%: Army growth +0.01/turn"
      "INFRA 25%: Development +0.005/turn"
      "RESEARCH 25%: Tech progress: 12/100"
      "SOCIAL 15%: Stability +0.01/turn"
    
    [APPLY] button saves budget to nation data.
  
  Faction display (below budget or separate panel):
    5 colored bars:
      MIL  [=====---] 62  (red)
      MER  [=======-] 78  (green)  
      NAT  [===-----] 38  (orange)
      PRG  [=====---] 55  (blue)
      REL  [====----] 48  (purple)
    
    Dominant faction marked with ▸ arrow.

VERIFY:
  F5 → Press B → budget panel opens. Sliders work.
  Adjust military up → see army growth text change.
  Factions visible with colored bars. One marked as dominant.
  Apply budget → numbers take effect on next turn.
```

### Phase 26 — Fog of War
```
CREATE:
  src/UI/Map/FogOfWarRenderer.cs:
    Separate CanvasLayer above terrain, below units.
    
    For each tile on map:
      If owned by player nation OR adjacent to owned tile: CLEAR (no fog)
      If within 5 tiles of any player unit: VISIBLE (no fog)
      If explored (unit was here before): FOGGED (dark overlay, 50% opacity)
      Otherwise: UNKNOWN (dark overlay, 85% opacity)
    
    Store explored tiles in a HashSet<(int,int)>.
    Update each turn and when units move.
    
    Draw: for each fogged/unknown tile, draw a dark ColorRect at tile position.
    
    AI units in FOGGED areas: visible but dimmed.
    AI units in UNKNOWN areas: invisible.

VERIFY:
  F5 → Your territory is clear. Distant lands are dark.
  Move a unit → fog clears around it, revealing terrain and enemy units.
  Territory you explored stays partially visible (fogged).
  You can't see AI units in unexplored territory.
```

### Phase 27 — Basic Trade Routes
```
CREATE:
  src/Engines/TradeEngine.cs:
    Priority: 3, Phase: Trade
    
    Simplified for test game:
      For each pair of non-warring nations with relations > -20:
        tradeValue = (nationA.GDP + nationB.GDP) * 0.01
        Both nations: TreasuryDelta += tradeValue / 12 (monthly)
    
    Generate visual route paths:
      For each trade pair:
        Start: nationA capital pixel position
        End: nationB capital pixel position
        Generate 3-5 waypoints with perpendicular sine wobble
        Store as Vector2 array

  Update RouteRenderer.cs:
    DrawTradeRoutes(List<TradeRoute> routes):
      For each route:
        Draw Bézier curve (green #48c848 if healthy)
        Animate 3 convoy dots moving along the curve each _Process tick

VERIFY:
  F5 → Green winding lines connect nations that are trading.
  Small dots move along the routes (convoys).
  Declare war → route between those nations disappears.
  Peace → route reappears.
  Routes look organic, not straight lines.
```

### Phase 28 — DiplomacyEngine + Relations
```
CREATE:
  src/Engines/DiplomacyEngine.cs:
    Priority: 6, Phase: Diplomacy
    
    For each nation pair:
      Track relations (-100 to +100), starting at 0
      Natural drift toward 0 at -0.5/turn
      Modifiers:
        At war: -50 flat
        Shared border: -5
        Trade active: +2/turn
        Same culture/religion: +10
    
    AI war decisions (upgrade from Phase 20):
      If relations < -60 AND military stronger than neighbor → consider war
      Probability: (|relations| - 50) * 0.5 % per turn
      Publish WarDeclaredEvent if triggered
    
    AI peace decisions:
      If at war AND war exhaustion > 70 → offer peace

  Update NationPanel:
    Show relation bar: red (-100) to green (+100) gradient
    Show current modifiers as text list

VERIFY:
  F5 → Run 30 turns. Some AI nations declare war on each other.
  News feed shows: "IRONPACT declares war on ASHWARD!"
  Later: "IRONPACT and ASHWARD sign peace treaty."
  Relations panel shows changing values with modifier explanations.
```

### Phase 29 — Simple Intel + Spy Network Start
```
CREATE:
  src/Engines/IntelligenceEngine.cs:
    Priority: 10, Phase: Intel
    
    Simplified for test game:
      Player can establish spy network in any nation.
      Network grows from depth 0 to depth 3 (1 depth per 5 turns).
      Cost: 50 gold/turn while active.
      
      At depth 1+: see target's treasury and stability (exact).
      At depth 2+: see target's military strength.
      At depth 3: see their relations with other nations.
      
      Without spy network: you see "???" for most stats.
    
  Update NationPanel:
    If no spy network: show "STR: ???" "GOLD: ???"
    If spy network depth 1: show treasury and stability
    If depth 2+: show military strength
    Mark intel source: "[SPY LV.2]" in corner
    
  Add to NationPanel actions:
    [ESTABLISH SPY NETWORK] → costs 200 gold upfront
    Shows: "Network building... Depth 0/3"

VERIFY:
  F5 → Click a rival nation. Stats show "???" for most things.
  Click ESTABLISH SPY NETWORK → gold deducted, network starts.
  After 5 turns → depth 1, treasury/stability now visible.
  After 10 turns → depth 2, military strength appears.
  You're making decisions based on INCOMPLETE INFORMATION.
```

### Phase 30 — ★ CHECKPOINT: Playable Test Game
```
INTEGRATION:
  Wire everything together for a complete game loop:
  
  1. Title screen → New Game
  2. Opening speech (First Fire text)
  3. World generates: 6 nations, terrain, cities, units, rivers, routes
  4. Camera zooms to player capital
  5. Player can:
     - Pan/zoom the map
     - Select and move units (click + right-click)
     - Open nation panels (click territory)
     - Declare war / offer peace
     - Adjust budget
     - Establish spy networks
     - Read news feed
     - End turn
  6. AI nations:
     - Move units each turn
     - Declare war when relations are bad
     - Offer peace when exhausted
     - Trade with friendly nations
  7. Economy runs:
     - Resources collected each turn
     - Expenses paid
     - Trade income flows
     - Treasury shown in HUD
  8. Combat works:
     - Units entering enemy tiles trigger battles
     - Terrain affects combat
     - Winners stay, losers retreat/die
     - Visual battle effects
  9. Fog of war:
     - Unexplored territory is dark
     - Units reveal area as they move
     - Enemy units hidden in fog
  10. Victory check (simplified):
      - Control 60% of land tiles → win
      - Lose all your tiles → lose
      - Reach turn 100 → score tallied

GIT:
  git add -A
  git commit -m "Phase 30: Playable test game - v0.1.0"
  git push
  git checkout develop
  git merge feature/block-a
  git push
  git tag v0.1.0
  git push --tags

VERIFY:
  Complete play test:
  ✅ Title screen shows, looks good
  ✅ Opening speech plays dramatically
  ✅ Map loads with organic terrain, rivers, cities, units
  ✅ Camera zoom and pan work smoothly
  ✅ Select unit → move it → it walks smoothly
  ✅ Can't walk on water or mountains
  ✅ AI units move every turn (map feels alive)
  ✅ Declare war → combat works → units fight
  ✅ Economy runs → treasury changes each turn
  ✅ Trade routes visible as winding lines with convoys
  ✅ Fog of war hides unexplored areas
  ✅ Spy networks reveal enemy info gradually
  ✅ News feed reports events each turn
  ✅ Budget panel works
  ✅ The game is PLAYABLE for 30+ turns without crashing
  ✅ It looks like an SNES game, not programmer art
  
  🎉 TAG v0.1.0 — FIRST PLAYABLE
```

---

## After Phase 30

The game is playable. From here, each session adds depth:

**Session A:** Politics (factions, legitimacy, coups)
**Session B:** Advanced diplomacy (alliances, trade dependency)  
**Session C:** Advanced military (combined arms, supply lines, siege)
**Session D:** Naval + air units on map
**Session E:** Nuclear program + First Fire scenario
**Session F:** Fallen Star scenario + game modes
**Session G:** Full fog of intelligence (confidence ranges, deception)
**Session H:** War room advisors + newspaper system
**Session I:** Sound + music + animations + polish
**Session J:** Balance + difficulty + release

But that's future. Right now: get to Phase 30.

---

*30 phases. Zero to playable. Let's go.*
