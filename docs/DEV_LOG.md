# WARSHIP — Dev Log
## Update After Every Session

---

## Current Status

**Phase:** 2 (complete)
**Branch:** main
**Tag:** N/A
**Last Session:** 2026-03-21

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

---

## Phase Tracker

| # | Phase | Status | Date |
|---|-------|--------|------|
| 1 | Project skeleton | ✅ | 2026-03-21 |
| 2 | Empty main scene | ✅ | 2026-03-21 |
| 3 | Terrain tileset (32px pixel art) | ⬜ | — |
| 4 | TileMapLayer + hardcoded test map | ⬜ | — |
| 5 | Camera zoom + pan | ⬜ | — |
| 6 | Procedural terrain (Perlin noise) | ⬜ | — |
| 7 | Nations + territory colors | ⬜ | — |
| 8 | City sprites on map | ⬜ | — |
| 9 | Rivers (Bézier curves) | ⬜ | — |
| 10 | Unit sprite: tank on map | ⬜ | — |
| 11 | Unit movement (click to move) | ⬜ | — |
| 12 | Multiple unit types | ⬜ | — |
| 13 | SimRng + EventBus + ISimEngine | ⬜ | — |
| 14 | WorldStateManager | ⬜ | — |
| 15 | TurnEngine + WSC | ⬜ | — |
| 16 | ResourceEngine + EconomicEngine | ⬜ | — |
| 17 | HUD: Top bar (SNES style) | ⬜ | — |
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
