# FULL AUTHORITY — Game Update Roadmap
## Single Source of Truth for Development

> **Last updated:** 2026-03-24
> **Game:** Full Authority (formerly "Warship: Leaders of the Warship")
> **Engine:** Godot 4 + C# | **Platform:** Desktop | **Style:** Real-world geopolitical thriller

---

## What's Built (Baseline)

Everything below is **coded and functional** as of Session 3 (2026-03-23):

| System | Status | Key Files |
|--------|--------|-----------|
| Core architecture | Done | EventBus, WorldStateManager, RuntimeBridge |
| Data models | Done | Models.cs (6 classes), GeoData.cs (6 nations, 80+ cities) |
| Economy engine | Done | Income, upkeep, bankruptcy |
| Military engine | Done | Swarm movement, combat, city capture |
| Political engine | Done | 7 action types, authority shifts |
| AI engine | Done | Rival decision-making each turn |
| Crisis engine | Done | Random events with player choices |
| Victory engine | Done | FAI >= 90 triggers win |
| Real-world map | Done | TileMapRenderer (OSM + Topo), WarshipMapBridge |
| World generator | Done | Real coordinates, named leaders, military bases |
| Main menu | Done | Title screen, character setup |
| HUD | Done | TopBar, EndTurn, DossierPanel, MilitaryCommandPanel |
| Crisis/Victory UI | Done | Modal panels for events and win state |
| News ticker | Done | Scrolling event feed |
| Notifications | Done | Slide-in alerts |

**The game runs end-to-end:** Menu → Setup → Map → Turns → Economy/Combat/Politics/Crisis → Victory.

---

## The Goal

Ship a testable build at each milestone. Every milestone = someone can download it, play it, and give feedback. No milestone depends on "finishing everything first."

---

## MILESTONE 1: FIRST PLAYABLE BUILD
**Goal:** Hand someone the game and say "play 30 turns, tell me what's broken."
**Tag:** `v0.9.0-alpha`

All core systems work. This milestone is about making the existing game **stable, visible, and testable** — not adding new features.

| # | Task | Priority | Status | Notes |
|---|------|----------|--------|-------|
| 1.1 | Complete OSM tile download (zoom 0-10) | HIGH | 🟡 In Progress | ~15 GB, running on MacBook |
| 1.2 | Complete Topo tile download (zoom 0-9) | MEDIUM | ⚪ Queued | ~4 GB, after OSM finishes |
| 1.3 | Fallback for missing tiles | HIGH | ⚪ | Solid color or "loading" texture so game doesn't break without tiles |
| 1.4 | HUD restyle — dark theme + gold accents | MEDIUM | ⚪ | Match Plague Inc / DEFCON aesthetic |
| 1.5 | Map mode tab bar (F1/F2/G as clickable buttons) | LOW | ⚪ | Currently keyboard-only |
| 1.6 | Smoke test: play 50 turns without crash | HIGH | ⚪ | Fix any stability issues found |
| 1.7 | Bundle test build (export template) | HIGH | ⚪ | Verify someone else can run it |

**Exit criteria:** Game launches, map renders, turns advance, no crashes for 50 turns, economy/combat/politics all visibly working.

---

## MILESTONE 2: CORE LOOP POLISH
**Goal:** The core gameplay loop feels good. Player understands what's happening and has meaningful choices each turn.
**Tag:** `v0.10.0-alpha`

| # | Task | Priority | Status | Notes |
|---|------|----------|--------|-------|
| 2.1 | City detail overlays (population, strategic value, port/airfield icons) | MEDIUM | ⚪ | Click or hover on cities |
| 2.2 | Frontline rendering (animated war zones between nations) | MEDIUM | ⚪ | Visual feedback for wars |
| 2.3 | Espionage grid — fog of war for rival stats | HIGH | ⚪ | FA-12. Without this, player sees too much |
| 2.4 | Improve news feed — categorize, color-code, make scannable | MEDIUM | ⚪ | Player needs to quickly read what happened |
| 2.5 | Player unit commands — direct control (click unit → right-click target) | HIGH | ⚪ | Currently swarm-only; player needs agency |
| 2.6 | Tutorial hints — first 3 turns explain controls | LOW | ⚪ | Tooltip or overlay text |
| 2.7 | Balance pass — income/upkeep/combat numbers feel right | MEDIUM | ⚪ | Playtest and tune |

**Exit criteria:** A new player can figure out the game in 5 minutes. Wars are visible on the map. Intel creates real uncertainty. Turns feel meaningful.

---

## MILESTONE 3: DEPTH & SYSTEMS
**Goal:** The game has enough systems interacting that emergent stories happen. Replayable.
**Tag:** `v0.11.0-beta`

| # | Task | Priority | Status | Notes |
|---|------|----------|--------|-------|
| 3.1 | Rebellions & insurgency | HIGH | ⚪ | FA-13. Nations with low TA spawn rebel swarms |
| 3.2 | National debt & bailouts | MEDIUM | ⚪ | FA-14. Borrowing, economic pressure |
| 3.3 | Black market trading | LOW | ⚪ | FA-15. Shadow arms deals |
| 3.4 | UN Assembly UI — embargoes, resolutions | MEDIUM | ⚪ | FA-16. Diplomatic layer |
| 3.5 | Alliance webs — defensive pacts | HIGH | ⚪ | FA-17. Coalition warfare |
| 3.6 | Border attrition — moving through enemy territory costs strength | LOW | ⚪ | FA-18 |
| 3.7 | City siege mechanics — cities have HP before flipping | MEDIUM | ⚪ | FA-19 |
| 3.8 | Media warfare — propaganda campaigns | LOW | ⚪ | FA-20 |

**Exit criteria:** Multiple paths to victory feel viable. AI nations form alliances and betray each other. Economic and military strategies both work. 30+ turns consistently produce interesting stories.

---

## MILESTONE 4: ENDGAME & ESCALATION
**Goal:** Nuclear weapons, DEFCON, and leader succession create dramatic late-game tension.
**Tag:** `v0.12.0-beta`

| # | Task | Priority | Status | Notes |
|---|------|----------|--------|-------|
| 4.1 | Military research facilities — upgrade units to tanks/jets/fleets | HIGH | ⚪ | FA-21 |
| 4.2 | Nuclear silo construction | HIGH | ⚪ | FA-22. The game's signature mechanic |
| 4.3 | DEFCON system — global panic meter | MEDIUM | ⚪ | FA-23 |
| 4.4 | WMD strikes — nuke a city, permanent consequences | HIGH | ⚪ | FA-24 |
| 4.5 | Leader succession — assassination creates vengeful successor | MEDIUM | ⚪ | FA-25 |

**Exit criteria:** Late game is dramatically different from early game. Nuclear weapons change everything. DEFCON creates tension even without war.

---

## MILESTONE 5: POLISH & SHIP
**Goal:** Feature-complete, polished, ready for wider release.
**Tag:** `v1.0.0`

| # | Task | Priority | Status | Notes |
|---|------|----------|--------|-------|
| 5.1 | Espionage agency bases — spies on the map | LOW | ⚪ | FA-26 |
| 5.2 | True leader permadeath — nation collapse | LOW | ⚪ | FA-27 |
| 5.3 | Procedural global objectives ("Oil Crisis", "Pandemic") | MEDIUM | ⚪ | FA-28 |
| 5.4 | Save & load game state | HIGH | ⚪ | FA-29. Critical for real play sessions |
| 5.5 | Sound effects + music + animations | MEDIUM | ⚪ | FA-30 |
| 5.6 | Final balance, difficulty levels, performance optimization | HIGH | ⚪ | |
| 5.7 | Opening speech scene ("First Fire" narrative) | LOW | ⚪ | Dramatic intro from GAME_FLOW.md |

**Exit criteria:** Someone can play a full game (200 turns), save mid-game, come back, and have a satisfying experience start to finish.

---

## Archived Plans

These documents are **historical only** — kept for reference but no longer active:

| Document | What it was | Why archived |
|----------|-------------|--------------|
| `docs/PHASE_PLAN.md` | Original 30-phase plan (SNES pixel art, procedural map) | Superseded by FA plan + real-world map pivot |
| `docs/FA_PHASE_PLAN.md` | Full Authority phase plan (FA-1 to FA-30 + M-1 to M-10) | Merged into this roadmap |

---

## How to Use This Document

1. **Each session:** Check which milestone you're on. Pick the highest-priority incomplete task.
2. **After each session:** Update status columns. Add notes about what you learned.
3. **Don't skip milestones.** Each one produces a testable build. Test it before moving on.
4. **Add tasks freely** within a milestone if you discover work needed. Don't add new milestones without good reason.

---

*One roadmap. Clear milestones. Ship early, ship often.*
