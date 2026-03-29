# WARSHIP: Leaders of the Warship — Dev Log
## Update After Every Session

> **Current Direction:** Fictional procedural world, 13 nations (6 large + 7 small),
> real-time gameplay, concrete systems (no authority meters), SNES pixel art.
> All planning lives in **`docs/ROADMAP.md`** — the ONE master document.

---

## Current Status

**Active Milestone:** M3 — 13 Nations Live (IN PROGRESS)
**Branch:** claude/overhaul-map-ui-dHnki
**Last Session:** 2026-03-29

---

## Session Log

### Session 8 — 2026-03-29 (Council System + Combat Command Overhaul)
**Goal:** Major UI overhaul — add government council system, proper combat controls, council-aware sidebar
**Done:**
- **New Data Models** (`Models.cs`):
  - `GovernmentType` enum (11 types: FederalCouncil, RevolutionaryCommittee, MerchantSenate, RoyalCourt, CentralCommittee, Admiralty, NationalAssembly, WarCouncil, ShadowCabinet, ImperialCourt, SurvivalCouncil)
  - `AdviserRole` enum (13 roles: 4 core + 9 specialist)
  - `AdviserData` class (name, role, loyalty, competence, hawkishness, current advice)
  - `CouncilData` class (government type, adviser list, display name, archetype mapping, specialist role lookup)
  - `CouncilActionCategory` enum (Domestic, Military, Diplomatic, Intelligence)
  - Added `Council` field to `NationData`
- **New Events** (`GameEvents.cs`):
  - `CouncilActionEvent`, `AdviserOpinionEvent` (council system)
  - `ArmyOrderEvent`, `ArmyFormationEvent`, `ArmySelectedEvent` (combat system)
- **CouncilPanel** (`src/UI/Panels/CouncilPanel.cs`) — full-screen government overlay:
  - Header adapts to government type (e.g., "REVOLUTIONARY COMMITTEE", "ROYAL COURT")
  - Flavor text per government type ("The committee is in permanent session...")
  - Left column: adviser cards with name, role, loyalty/competence bars, current advice
  - Right column: action categories (Domestic/Military/Diplomatic/Intelligence)
  - Actions change by government type (e.g., "Purge Dissidents" for Revolutionary, "Hold Feast" for Royal Court)
  - Toggle with C key or sidebar button
- **CombatCommandPanel** (`src/UI/Panels/CombatCommandPanel.cs`) — replaces MilitaryCommandPanel:
  - Army list with color-coded order status
  - Selected army details: strength, morale, supply, organization
  - Per-army formation buttons (Column/Spread/Wedge/Circle) with stat tooltips
  - Per-army order buttons (Defend/Patrol/Stage/Attack/Retreat)
  - Unit composition breakdown
  - Battle log feed from BattleResolvedEvent
- **LeftSidebar rebuilt** — council-aware:
  - Shows government name + nation archetype in header
  - "Open Council [C]" button
  - Quick actions: Military, Diplomatic, Economic, Intelligence
  - Government-specific special action (varies by type)
- **WorldGenerator** — council generation:
  - `GenerateCouncil()` creates council + 5 advisers per nation
  - Name pools for 5 adviser types (65 unique names)
  - Randomized loyalty, competence, hawkishness per adviser
  - Applied to both `CreateWorld()` and `AddCustomNation()` flows
- **Scene cleanup**:
  - Replaced MilitaryCommandPanel with CombatCommandPanel in Main.tscn
  - Added CouncilPanel to UILayer
  - Deleted MilitaryCommandPanel.cs
- **Updated CLAUDE.md** project structure to reflect actual files

### Session 7 — 2026-03-28 (Map UI Overhaul)
**Goal:** Fix map not rendering — root cause was dead OSM system in scene, procedural map never wired up
**Done:**
- Diagnosed root cause: Main.tscn had TileMapRenderer (OSM real-world tiles) + WarshipMapBridge instead of MapManager + MapCamera. The procedural map system was never in the scene tree.
- Rewrote `scenes/Main.tscn`: removed TileMapRenderer + WarshipMapBridge nodes, added MapManager (Node2D) + MapCamera (Camera2D)
- Deleted `src/UI/Map/TileMapRenderer.cs` (631 lines of dead OSM tile loading code)
- Deleted `src/UI/Map/WarshipMapBridge.cs` (266 lines of dead OSM bridge code)
- Added explicit `MakeCurrent()` to MapCamera._Ready() for reliable camera activation
- Cleaned up stale WarshipMapBridge comment in MilitaryCommandPanel.cs
**Result:** MapManager now in scene tree, creates TerrainChunkRenderer + TerritoryBorderRenderer + ArmySwarmRenderer layers. MapCamera provides WASD/scroll/edge-scroll navigation. World generation → WorldReadyEvent → MapManager.OnWorldReady() → terrain baking pipeline is now properly connected.

### Session 6 — 2026-03-28 (M3: 13 Nations Live)
**Goal:** Replace 6 hardcoded nations with 13 named fictional nations
**Done:**
- Created `docs/NATION_RESEARCH.md` — real-world data grounding all 13 nations
  - Military data from GlobalFirepower 2026, GDP from IMF, resources from EIA
  - Each nation mapped to real-world inspiration (USA→Hegemon, Russia→Revolutionary, etc.)
  - Small nation special mechanics (guerrilla, trade city, intel, island, resource curse)
  - Comparative balance matrix and power ratios
- Updated `Models.cs`:
  - Added `NationTier` enum (Large, Small)
  - Expanded `NationArchetype` with 8 new archetypes (Industrial, Naval, TradeCity, Guerrilla, Intelligence, Remnant, IslandNaval, ResourceCursed)
  - Added resource stockpiles (Iron, Oil, Uranium, Electronics, Manpower, Food)
  - Added Stability and WarWeariness fields
- Rewrote `WorldGenerator.cs`:
  - Defined `NationTemplate` record with all 13 nations' identity, resources, military profiles
  - Terrain-driven capital placement (scores candidate spots by nation's preferred terrain)
  - Per-nation city counts (8-12 large, 2-5 small) and army counts (5-8 large, 1-3 small)
  - 12 archetype-driven military composition profiles (CombinedArms, MassInfantry, TankHeavy, NavalDominant, NuclearSmall, etc.)
  - `AnalyzeGeographyForResources()` for future custom nation feature
  - 13 distinct nation colors
- Updated `CharacterSetupPanel.cs`:
  - Added nation picker dropdown with all 13 nations + tier/archetype labels
  - Description updates on selection (shows cities, armies, treasury)
  - Default: Selvara (#6)
- Updated `WorldStateManager.cs`:
  - `InitializeWorld` now accepts `nationIndex` parameter
- Added nation traits system (22 unique passives like CarrierDoctrine, GuerrillaResistance, NuclearDeterrent)
- Reimagined all 13 nations as alternate-history civilizations with lore blurbs
- Added starting diplomatic dispositions (alliances, rivalries, trade dependencies)
- Built Custom Nation (14th option): name your country, pick archetype, click map to place capital
  - Geography-to-resources formula derives starting resources from terrain around capital
  - AddCustomNation() inserts player nation into existing world, re-derives territory/borders
  - EventBus gets Unsubscribe method; new CustomNationPlacement events
**Pending:** In-editor testing. Engines don't check traits yet (M4+ work).
**Issues:** Cannot verify build (Godot SDK required). Need in-editor test.
**Next:** Custom nation feature, then M4

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
| 2 | Nation count hardcoded at 6 (should be 13) | FIXED (Session 6) |

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
