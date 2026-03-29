# WARSHIP: Leaders of the Warship
## CLAUDE.md — Read This First Every Session

---

## The Game in 30 Seconds

SNES-style pixel art grand strategy. Player leads a tiny free state with 4
provinces and the world's first nuclear weapon. Five AI superpower blocs are
coming. Survive through war, trade, diplomacy, covert ops. Godot 4 + C#.

---

## Three Rules — NEVER Break These

**1. EventBus Only** — No engine calls another engine. Publish events, subscribe, react.
**2. WorldStateManager for All Writes** — Never mutate data directly. Use deltas.  
**3. Pure C# Engines** — Sim engines = plain C#. Zero Godot API. Only Core singletons are Nodes.

---

## Tech Stack

| What | Tech |
|------|------|
| Engine | Godot 4 (.NET/C# build) |
| Language | C# (.NET 6+) |
| Serialization | Newtonsoft.Json |
| Map | TileMapLayer, 32×32 pixel tiles |
| Sprites | AnimatedSprite2D |
| UI | Control nodes, custom SNES theme |
| RNG | Custom SimRng (seeded, deterministic) |

---

## Project Structure

```
warship-godot/
├── project.godot
├── Warship.csproj
├── CLAUDE.md                    ← THIS FILE
│
├── docs/
│   ├── PHASE_PLAN.md            ← 30-phase build plan
│   ├── DATA_MODELS.md           ← All C# class definitions
│   ├── GAME_FLOW.md             ← Player experience spec
│   └── DEV_LOG.md               ← Session tracking (update every session)
│
├── src/
│   ├── Core/
│   │   ├── EventBus.cs          ← Autoload #1. Typed pub/sub.
│   │   ├── WorldStateManager.cs ← Autoload #2. State + deltas.
│   │   ├── WorldSimulationCore.cs ← Autoload #3. Engine orchestrator.
│   │   ├── TurnEngine.cs        ← Pure C#. Turn pipeline.
│   │   ├── ISimEngine.cs        ← Interface + TurnPhase enum.
│   │   └── SimRng.cs            ← Seeded RNG.
│   │
│   ├── Engines/                 ← Pure C# simulation engines
│   │   ├── ResourceEngine.cs
│   │   ├── EconomicEngine.cs
│   │   ├── TradeEngine.cs
│   │   ├── PoliticalEngine.cs
│   │   ├── DiplomacyEngine.cs
│   │   ├── MilitaryEngine.cs
│   │   ├── IntelligenceEngine.cs
│   │   ├── AIDecisionEngine.cs
│   │   ├── EventsEngine.cs
│   │   └── NewsEngine.cs
│   │
│   ├── Player/
│   │   └── PlayerEngine.cs
│   │
│   ├── AI/
│   │   └── UtilityEvaluator.cs
│   │
│   ├── Data/
│   │   └── Models.cs            ← ALL data models in one file initially
│   │
│   ├── Events/
│   │   └── GameEvents.cs        ← ALL event types
│   │
│   ├── World/
│   │   ├── WorldGenerator.cs
│   │   ├── TerrainGenerator.cs
│   │   └── GameBootstrapper.cs
│   │
│   └── UI/
│       ├── Map/
│       │   ├── MapManager.cs          ← 3-layer renderer orchestrator
│       │   ├── MapCamera.cs           ← Zoom, pan, edge scroll, clamped
│       │   ├── TerrainChunkRenderer.cs ← Chunk-based terrain streaming
│       │   ├── TerritoryBorderRenderer.cs ← Territory tints + borders
│       │   ├── ArmySwarmRenderer.cs   ← 4-level LOD army rendering
│       │   └── UnitStamp.cs           ← Pixel-art unit silhouettes
│       ├── HUD/
│       │   ├── TopBar.cs             ← Turn counter, player stats
│       │   ├── BottomPanel.cs        ← Economy + authority meters
│       │   ├── LeftSidebar.cs        ← Council-aware action menu
│       │   ├── RightSidebar.cs       ← Intel & diplomacy panel
│       │   ├── MainViewSwitcher.cs   ← Map/Intel/WarRoom/Economy tabs
│       │   ├── SpeedControlBar.cs    ← Sim speed controls
│       │   ├── NotificationManager.cs ← Toast notifications
│       │   ├── NewsTicker.cs         ← News headline ticker
│       │   ├── DossierPanel.cs       ← Character detail panel
│       │   ├── VictoryPanel.cs       ← Win/lose screen
│       │   ├── HotZoneManager.cs     ← Mini-map pin system
│       │   └── CrisisPanel.cs        ← Crisis event modal
│       ├── Panels/
│       │   ├── CouncilPanel.cs       ← Government body (changes by type)
│       │   ├── CombatCommandPanel.cs ← Tactical army command interface
│       │   └── InterruptPanel.cs     ← "The Phone Rings" modal
│       ├── Menus/
│       │   ├── MainMenu.cs           ← Title screen
│       │   └── CharacterSetupPanel.cs ← Nation picker + custom nation
│       └── Screens/
│           └── (future scenario screens)
│
├── scenes/
│   ├── Main.tscn                ← Main scene, GameBootstrapper here
│   ├── TitleScreen.tscn
│   ├── GameWorld.tscn           ← Map + HUD + panels
│   └── SpeechScreen.tscn
│
├── assets/
│   ├── tilesets/
│   │   └── terrain.png          ← 32×32 tile atlas
│   ├── sprites/
│   │   ├── units/               ← tank.png, soldier.png, etc.
│   │   └── cities/              ← city_small.png, city_capital.png
│   ├── fonts/
│   │   └── pixel.ttf            ← Monospace pixel font
│   ├── themes/
│   │   └── snes_blue.tres       ← SNES window style theme
│   └── audio/
│
└── tests/
```

---

## Autoload Boot Order (project.godot)

```
1. EventBus           → sets Instance, prints "[EventBus] Online."
2. WorldStateManager  → sets Instance, prints "[WSM] Online."
3. WorldSimulationCore → gets refs to EB + WSM, creates TurnEngine
```

Then Main.tscn loads → GameBootstrapper runs → world generates → UI renders.

---

## Turn Pipeline (13 Phases)

```
 0  TurnOpen          → Begin transaction
 1  ResourceCollection → Province yields
 2  EconomicProcessing → GDP, treasury
 3  TradeProcessing    → Routes, tariffs
 4  Unrest             → Rebellion checks
 5  Politics           → Factions, coups
 6  Diplomacy          → Relations, alliances
 7  AIDecision         → AI nations act
 8  PlayerAction       → Player commands execute
 9  Military           → Battles resolve
10  Intelligence       → Spy ops
11  Events             → Random events
12  News               → Format dispatches
13  TurnClose          → Commit, advance turn
```

---

## Coding Rules

```csharp
// USE                          // NEVER USE
GD.Print("msg")                 Debug.Log("msg")
SimRng.NextFloat()              Random.value / new Random()
Math.Clamp(v, min, max)         Mathf.Clamp()
_state.ApplyNationDelta(...)    nation.Treasury -= 100
_bus.Publish(new XEvent{})      otherEngine.DoThing()
```

- Engines: plain C# class implementing ISimEngine. No `partial`, no `: Node`.
- UI nodes: `partial class : Control` or `: Node2D`. Subscribe in _Ready, unsub in _ExitTree.
- Newtonsoft.Json for all serialization.
- SimRng for ALL randomness (deterministic replays).

---

## Session Startup

1. Read this file
2. `git branch --show-current` — know where you are
3. Check docs/DEV_LOG.md — know what phase you're on
4. Read the phase spec in docs/PHASE_PLAN.md
5. Build it. Test it. Commit it. Update DEV_LOG.md.

---

## Design Reference (Quick)

**Player:** Tiny free state, 4 provinces, 1 nuke. Surrounded by 5 superpower blocs.

**Blocs:** Hegemon (military dominant), Revolutionary (aggressive ideologues),
Commercial League (trade focused), Traditionalists (conservative), Survival
Accord (desperate coalition).

**Map:** SNES pixel art. 32px tiles. Grass with flowers, forests with trees,
mountains with snow caps, animated water. Multi-tile cities with flags and smoke.
Winding Bézier rivers. Curved trade routes with moving convoys.

**UI:** Classic blue SNES menu windows (FF6/Chrono Trigger style). Slide-in
panels. HP-bar style stat displays. Monospace pixel font.

**Core mechanic:** Intelligence is unreliable. Enemy numbers are estimates with
confidence ranges. The DECEIVED fog state shows wrong info that looks correct.

**Win:** Military (60% provinces), Economic (50% GDP), Diplomatic (60% allied),
Nuclear Supremacy, or Survival (exist at turn 200).

**Lose:** All provinces conquered, stability at 0 for 10 turns, treasury at
-5000 for 5 turns, or hostile coup.

For full design details: `docs/GAME_FLOW.md`
