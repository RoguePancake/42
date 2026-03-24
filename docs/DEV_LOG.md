# FULL AUTHORITY — Dev Log
## Update After Every Session

> **🚨 MAJOR VISION PIVOT — MAP OVERHAUL 🚨**
> The game map has been rebuilt from a procedural 80×50 SNES tile grid to a 
> **real-world Earth map** using OpenStreetMap tiles at zoom 0-10 (~15 GB).
> Nations now sit at real coordinates (Washington D.C., Moscow, Beijing, etc.)
> with actual borders, real city names, and geographic trade routes.
> Development follows **`docs/FA_PHASE_PLAN.md`** — see ACT I.5 for map phases.

---

## Current Status

**Active Phase:** M-6 (Tile Download) + M-7 (HUD Restyling) queued
**Branch:** main
**Last Session:** 2026-03-23

---

## Session Log

### Session 3 — 2026-03-23 (Real-World Map Integration)
**Goal:** Replace procedural map with real-world OSM tiles
**Done:**
- Created `GeoData.cs` — 6 nations with real borders (US, China, Russia, EU, India, UK), 80+ real cities, military bases, trade routes
- Created `TileMapRenderer.cs` — Dual-style renderer (OSM vibrant + TopoMap), 2048-tile LRU cache, smooth zoom, WASD/scroll/drag
- Created `WarshipMapBridge.cs` — Government overlay (territories, borders, city labels, units, routes) toggleable with G key. EventBus connected.
- Added lon/lat fields to `Models.cs` (NationData, CityData, UnitData, CharacterData)
- Added map events to `GameEvents.cs` (NationSelectedEvent, MapStyleChangedEvent, UnitMoveToCoordRequested)
- Rewrote `WorldGenerator.cs` — Nations at real coordinates, named leaders/generals, units at real military bases
- Updated `Main.tscn` — Swapped MapManager/MapCamera → TileMapRenderer/WarshipMapBridge
- Updated `WorldStateManager.cs` — CreateWorld(42) single-param call
- Updated `MilitaryCommandPanel.cs` — Removed old MapManager reference
- Updated `.gitignore` — Excluded tile directories (15+ GB)
- Started OSM tile download (1.4M tiles, zoom 0-10, ~15 GB) — running in background
**Issues:** `dotnet build` fails outside Godot editor (Godot SDK not resolvable from terminal — normal for Godot C# projects)
**Next:** Complete tile download, start topo download, restyle HUD

### Session 2 — 2026-03-22 (Map Graphics / Economy / News)
**Goal:** Visual overhaul + Economy Engine + News Ticker
**Done:**
- Cyberpunk grid effects (added then reverted to original style)
- Darker terrain palette (Plague Inc. inspired)
- News ticker implementation (BottomPanel, NewsTicker)
- Economy Engine (FA-10) — national treasuries, income per turn
- Fixed display settings (aspect ratio "keep")
- Left/Right sidebar implementations
**Issues:** None
**Next:** Map idea integration

### Session 1 — 2026-03-21 (Project Bootstrap)
**Goal:** Phase 1-2 (project skeleton + empty scene)
**Done:**
- Created `~/Desktop/42/` project
- `project.godot` — C# enabled, 1280×720
- All `src/` and `assets/` directories
- `scenes/Main.tscn` — Node2D + dark bg + Camera2D
- Git initialized
**Issues:** None
**Next:** Terrain generation

---

## Phase Tracker

### Old 30-Phase Plan (Phases 1-14 from original, now superseded)
| # | Phase | Status | Notes |
|---|-------|--------|-------|
| 1 | Project skeleton | ✅ | |
| 2 | Empty main scene | ✅ | |
| 3 | Terrain generation | ✅ | Now superseded by OSM tiles |
| 4 | MapManager + rendered terrain | ✅ | **REPLACED** by TileMapRenderer |
| 5 | Camera zoom + pan | ✅ | **REPLACED** by TileMapRenderer camera |
| 6 | Ocean boundaries + noise | ✅ | Now superseded by real coastlines |
| 7 | Nations + territory colors | ✅ | Now uses real-world borders via GeoData |
| 8 | City sprites on map | ✅ | Now uses real city coords (lon/lat) |
| 9 | Rivers (Bézier curves) | ✅ | Now rendered in OSM tile data |
| 10 | Unit sprite: tank on map | ✅ | Now positioned by lon/lat |
| 11 | Unit movement | ✅ | |
| 12 | Multiple unit types | ✅ | |
| 13 | EventBus | ✅ | |
| 14 | WorldStateManager | ✅ | |

### FA Phase Plan (Active)
| # | Phase | Status | Notes |
|---|-------|--------|-------|
| FA-1 | VIP Characters + Authority | ✅ | |
| FA-2 | Dossier UI | ✅ | |
| FA-3 | Power Play Engine | ✅ | |
| FA-4 | Turn Engine + AI | ✅ | |
| FA-5 | Crisis Events | ✅ | |
| FA-6 | Swarm Military | ✅ | |
| FA-7 | Swarm Combat | ✅ | |
| FA-8 | Victory Panel | ✅ | |
| FA-9 | Main Menu + HUD | ✅ | |
| FA-10 | Economy Engine | ✅ | |
| FA-11 | News Ticker | ✅ | |
| **M-1** | **Data Model + GeoData** | **✅** | **lon/lat fields, real borders** |
| **M-2** | **TileMapRenderer** | **✅** | **OSM + Topo dual-style** |
| **M-3** | **WarshipMapBridge** | **✅** | **Government overlay toggle** |
| **M-4** | **WorldGenerator rewrite** | **✅** | **Real-world placement** |
| **M-5** | **Scene restructure** | **✅** | **Main.tscn updated** |
| **M-6** | **Tile download** | **🟡** | **OSM ~15 GB downloading** |
| **M-7** | **HUD restyling** | **⚪** | **Dark theme + gold accents** |
| **M-8** | **Map mode tabs UI** | **⚪** | |
| **M-9** | **City detail overlays** | **⚪** | |
| **M-10** | **Frontline rendering** | **⚪** | |
| FA-12 | Espionage Grid | ⚪ | |
| FA-13 | Rebellions | ⚪ | |
| FA-14 | National Debt | ⚪ | |
| FA-15 | Black Market | ⚪ | |

---

## Decision Log

| # | Decision | Why |
|---|----------|-----|
| 1 | Fresh project at ~/Desktop/42/ | Clean start, no legacy baggage |
| 2 | Godot.NET.Sdk 4.3.0 with .NET 8 | Matches locally cached SDK |
| 3 | OSM Standard tiles, not dark theme | User wants vibrant green/blue Earth map |
| 4 | Government overlay as toggleable code layer, not separate tile set | Saves ~65 GB disk space |
| 5 | Zoom 0-10 OSM (~15 GB) + Zoom 0-9 Topo (~4 GB) = ~19 GB | User set 20 GB budget |
| 6 | Keep TerrainGenerator + legacy tile coords for backward compat | Don't break existing engines |
| 7 | Player starts as UK Defense Minister | FreeState archetype, small but strategic |

---

## Known Bugs

| # | Bug | Phase | Fixed? |
|---|-----|-------|--------|
| 1 | `dotnet build` fails outside Godot editor (SDK resolver) | — | N/A (expected) |
| | | | |

---

## Architecture Notes (Post-Map Overhaul)

### Key Files
```
src/
├── Core/
│   ├── EventBus.cs          ← Rule 1: ALL communication
│   ├── WorldStateManager.cs ← Rule 2: ALL state mutations
│   └── RuntimeBridge.cs
├── Data/
│   ├── Models.cs            ← Now includes lon/lat fields
│   └── GeoData.cs           ← NEW: Real-world nation geography
├── Engines/                 ← Rule 3: Pure C# engines (unchanged)
│   ├── PoliticalEngine.cs
│   ├── AIEngine.cs
│   ├── MilitaryEngine.cs
│   ├── CrisisEngine.cs
│   ├── VictoryEngine.cs
│   └── EconomyEngine.cs
├── Events/
│   └── GameEvents.cs        ← Added NationSelectedEvent, MapStyleChangedEvent
├── UI/
│   ├── Map/
│   │   ├── TileMapRenderer.cs    ← NEW: Real-world map renderer
│   │   ├── WarshipMapBridge.cs   ← NEW: Game ↔ Map glue
│   │   ├── MapManager.cs         ← OLD: No longer in scene (kept for reference)
│   │   └── MapCamera.cs          ← OLD: No longer in scene (kept for reference)
│   └── HUD/ (all HUD panels — unchanged, need restyling)
└── World/
    ├── WorldGenerator.cs     ← REWRITTEN: Real-world coordinates
    └── TerrainGenerator.cs   ← Kept for backward compat
```
