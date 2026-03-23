# FULL AUTHORITY — Dev Log
## Update After Every Session

> **🚨 MAJOR VISION PIVOT 🚨**
> The game has shifted from "Warship" (a 4X conqueror) to **Full Authority** (a Political Thriller). 
> The old 30-phase roadmap below is now OBSOLETE. 
> Development now firmly follows the new **`docs/FA_PHASE_PLAN.md`** roadmap to track progress towards the 100 FAI win condition!
> The technical foundation (Map, Tiles, Core Engines) from Phases 1-17 remains in place.

---

## Current Status

**Phase:** FA-12 (complete)
**Branch:** claude/continue-phases-Hhcpz
**Tag:** N/A
**Last Session:** 2026-03-23

---

## Session Log

### Session 1 — 2026-03-21
**Goal:** Phase 1-2 (project skeleton + empty scene)
**Done:**
- Created fresh `~/Desktop/42/` project from scratch
- `project.godot` — C# enabled, 1280×720, Main.tscn as main scene
- `Warship.csproj` — .NET 8, Godot.NET.Sdk 4.3.0
- All `src/` directories: Core, Engines, Player, AI, Data, Events, World, UI
- All `assets/` directories: tilesets, sprites, fonts, themes, audio
- Copied all design docs from rootfiles
- `scenes/Main.tscn` — Node2D + dark ColorRect (#0a0a1a) + Camera2D
- `dotnet build` — ✅ 0 errors
- Git initialized, committed as `f710dab`
**Issues:** None
**Next:** Phase 3 — Terrain tileset (32×32 pixel art)

### Session — 2026-03-23
**Goal:** Phase FA-12 — Espionage Grid (Fog of War)
**Done:**
- Added `IntelLevel` enum and `IntelRecord` class to Models.cs
- Added `IntelRecords` list to `WorldData`
- Added `IntelChangedEvent` to GameEvents.cs
- Created `IntelligenceEngine.cs` — passive BSA-based intel accumulation, decay, fog helpers
- Wired IntelligenceEngine into Main.tscn scene tree
- Modified `PoliticalEngine.cs`: "investigate" grants +15 intel pts on target, "review_intel" grants +5 to all rivals
- Modified `DossierPanel.cs`: rival stats now fogged (???/~25%/~30%/exact) based on intel level, color-coded intel indicator, desaturated progress bars at low intel
- Modified `AIEngine.cs`: AI uses fogged values for decision-making, prioritizes investigation when intel is low
- Modified `TopBar.cs`: shows intel coverage count (e.g., "Intel: 2/5")
- Updated FA_PHASE_PLAN.md and DEV_LOG.md
**Key Design:** BSA 40 = breakeven. Below 40 = intel decays. Above 40 = accumulates. "Investigate" is the active espionage tool (+15 pts). Five fog tiers: Unknown/Rumor/Observed/Confirmed/Complete.
**Issues:** No .NET SDK available to verify build
**Next:** Phase FA-13 — Rebellions & Insurgency

---

## Phase Tracker

| # | Phase | Status | Date |
|---|-------|--------|------|
| 1 | Project skeleton | ✅ | 2026-03-21 |
| 2 | Empty main scene | ✅ | 2026-03-21 |
| 3 | Terrain generation (procedural)  | ✅ | — |
| 4 | MapManager + rendered terrain    | ✅ | — |
| 5 | Camera zoom + pan | ✅ | — |
| 6 | Ocean boundaries + noise layers  | ✅ | — |
| 7 | Nations + territory colors | ✅ | — |
| 8 | City sprites on map | ✅ | — |
| 9 | Rivers (Bézier curves) | ✅ | — |
| 10 | Unit sprite: tank on map | ✅ | — |
| 11 | Unit movement (click to move) | ✅ | — |
| 12 | Multiple unit types | ✅ | — |
| 13 | EventBus decoupled structure | ✅ | — |
| 14 | WorldStateManager | ✅ | — |
| 15 | TurnEngine + WSC | ⬜ | — |
| 16 | ResourceEngine + EconomicEngine | ⬜ | — |
| 17 | HUD: Top bar (SNES style) | ✅ | — |
| 18 | End Turn button + controls | ⬜ | — |
| 19 | Title screen + opening speech | ⬜ | — |
| 20 | Basic AI movement | ⬜ | — |
| 21 | Basic combat | ⬜ | — |
| 22 | War declaration + peace | ⬜ | — |
| 23 | News feed (bottom panel) | ⬜ | — |
| 24 | Nation panel (click territory) | ⬜ | — |
| 25 | Budget panel + faction bars | ⬜ | — |
| 26 | Fog of war | ⬜ | — |
| 27 | Basic trade routes | ⬜ | — |
| 28 | DiplomacyEngine + relations | ⬜ | — |
| 29 | Simple intel + spy networks | ⬜ | — |
| 30 | ★ CHECKPOINT: Playable test game v0.1.0 | ⬜ | — |

---

## Decision Log

| # | Decision | Why |
|---|----------|-----|
| 1 | Fresh project at ~/Desktop/42/ | Clean start, no legacy baggage |
| 2 | Godot.NET.Sdk 4.3.0 with .NET 8 | Matches locally cached SDK |

---

## Known Bugs

| # | Bug | Phase | Fixed? |
|---|-----|-------|--------|
| | | | |
