# WARSHIP: Leaders of the Warship — Dev Log
## Update After Every Session

> **Current Direction:** Fictional procedural world, 13 nations (6 large + 7 small),
> real-time gameplay, concrete systems (no authority meters), SNES pixel art.
> All planning lives in **`docs/ROADMAP.md`** — the ONE master document.

---

## Current Status

**Active Milestone:** Pre-M1 (Roadmap complete, code has procedural world + rendering)
**Branch:** claude/pixel-map-research-VA7J1
**Last Session:** 2026-03-28

---

## Session Log

### Session 5 — 2026-03-28 (Master Roadmap & Architecture Consolidation)
**Goal:** Write ONE master roadmap reconciling all docs, archive old plans
**Done:**
- Read and audited ALL planning docs (MILITARY_SYSTEM.md 30 parts, ROADMAP.md, GAME_FLOW.md, GAME_CHARACTERS.md, FULL_AUTHORITY_DESIGN.md, PHASE_PLAN.md, FA_PHASE_PLAN.md)
- Identified contradictions across 7+ documents
- Got 6 directional decisions from user:
  1. Fictional world first (real world later)
  2. 13 nations: 6 large + 7 small (including "United States Alliance")
  3. Real-time gameplay
  4. Concrete systems (no authority meters/FAI)
  5. Both sandbox AND campaign scenarios
  6. Phone-rings interrupt mechanic
- Wrote complete master ROADMAP.md (~1000 lines) incorporating ALL 30 military system parts
- Moved obsolete docs to `docs/Obsolete Ideas/` (PHASE_PLAN.md, FA_PHASE_PLAN.md, FULL_AUTHORITY_DESIGN.md, GAME_FLOW.md, GAME_CHARACTERS.md, DATA_MODELS.md)
**Next:** Milestone 1 — SimulationClock (real-time conversion)

### Session 4 — 2026-03-27 (Procedural World & Army System)
**Goal:** Build fictional procedural world with army swarm rendering
**Done:**
- Rewrote TerrainGenerator.cs for 600x360 tile world (19,200 x 11,520 px)
- Built TerrainChunkRenderer.cs — chunk-baked terrain with pixel detail
- Built ArmySwarmRenderer.cs — pixel-dot armies (1 dot = 10 troops), LOD, formations
- Built TerritoryBorderRenderer.cs — city icons, territory tints, border lines, frustum culling
- Rewrote MapManager.cs as orchestrator for 3 rendering layers
- Rewrote WorldGenerator.cs — procedural nations, city-centric territory (BFS flood-fill)
- Added ArmyData model (composition dictionaries replacing individual UnitData)
- Updated MapCamera.cs zoom for 3x larger map
- Updated WorldStateManager.cs for new world gen signature
- Fixed 15+ compilation errors from UnitData → ArmyData transition
**Issues:** Nation count hardcoded at 6 (needs updating to 13 per roadmap)
**Next:** Master roadmap consolidation

### Session 3 — 2026-03-23 (Real-World Map Integration)
**Goal:** Replace procedural map with real-world OSM tiles
**Done:**
- Created GeoData.cs, TileMapRenderer.cs, WarshipMapBridge.cs
- Real-world nation data (US, China, Russia, EU, India, UK)
- Dual-style renderer (OSM vibrant + TopoMap)
**Note:** Real-world map approach superseded by Session 4's fictional world pivot

### Session 2 — 2026-03-22 (Map Graphics / Economy / News)
**Done:** News ticker, Economy Engine, darker terrain palette

### Session 1 — 2026-03-21 (Project Bootstrap)
**Done:** Project skeleton, Main.tscn, git initialized

---

## Roadmap

All planning lives in **`docs/ROADMAP.md`**. 10 milestones:
```
M1  The Clock Ticks       — Real-time foundation
M2  The Phone Rings       — Interrupt system (signature mechanic)
M3  13 Nations Live       — Full fictional world
M4  The World Breathes    — AI nations act continuously
M5  Your Hands on Wheel   — Full player interaction
M6  The Fog               — Unreliable intelligence
M7  Full Combat           — All 30 military system parts
M8  Campaigns             — 5 scenarios + sandbox mode
M9  Save/Load             — Persistence
M10 Ship It               — Polish, balance, export
```

---

## Decision Log

| # | Decision | Why |
|---|----------|-----|
| 1 | Fresh project at ~/42/ | Clean start |
| 2 | Godot 4 + C# (.NET 8) | Best fit for complex sim |
| 3 | Fictional world first | User directive — real world deferred to post-launch |
| 4 | 13 nations (6 large + 7 small) | User directive — includes "United States Alliance" |
| 5 | Real-time, not turn-based | User directive — SimulationClock replaces TurnEngine |
| 6 | Concrete systems, no FAI | User directive — territory/treasury/armies, not authority meters |
| 7 | Sandbox + Campaign modes | User directive — both available |
| 8 | Phone-rings interrupt mechanic | User directive — signature mechanic with countdown timers |
| 9 | 600x360 tile map at 32px | 3x scale for large troop movements |
| 10 | Army swarm rendering (1 dot = 10 troops) | Pixel art style + handles large armies |
| 11 | City-centric territory (BFS flood-fill) | Capture cities to conquer nations |

---

## Known Bugs

| # | Bug | Fixed? |
|---|-----|--------|
| 1 | `dotnet build` fails outside Godot editor (SDK resolver) | N/A (expected) |
| 2 | Nation count hardcoded at 6 (should be 13) | Pending M3 |

---

## Key Files

```
docs/
├── ROADMAP.md              ← THE plan (one document)
├── MILITARY_SYSTEM.md      ← Detailed military reference (30 parts)
├── DEV_LOG.md              ← This file
└── Obsolete Ideas/         ← Archived old plans
    ├── PHASE_PLAN.md
    ├── FA_PHASE_PLAN.md
    ├── FULL_AUTHORITY_DESIGN.md
    ├── GAME_FLOW.md
    ├── GAME_CHARACTERS.md
    └── DATA_MODELS.md

src/
├── Core/                   ← EventBus, WorldStateManager, TurnEngine, SimRng
├── Data/Models.cs          ← All data models (NationData, ArmyData, CityData...)
├── Engines/                ← Pure C# sim engines
├── Events/GameEvents.cs    ← All event types
├── World/                  ← WorldGenerator, TerrainGenerator
└── UI/Map/                 ← MapManager, TerrainChunkRenderer, ArmySwarmRenderer, TerritoryBorderRenderer
```
