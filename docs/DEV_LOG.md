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

**Active Milestone:** 1 — First Playable Build (see `docs/ROADMAP.md`)
**Branch:** main
**Last Session:** 2026-03-24

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

## Roadmap

All milestone tracking and future work lives in **`docs/ROADMAP.md`** — single source of truth.

### Completed Phases (Historical)
- Phases 1-14 (original Warship plan) — all done, then superseded by real-world map
- FA-1 to FA-11 (Full Authority foundation) — all done
- M-1 to M-5 (real-world map overhaul) — all done
- M-6 (tile download) — in progress

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
