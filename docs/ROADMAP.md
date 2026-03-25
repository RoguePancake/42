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

## MILESTONE 2: CLICK & COMMAND
**Goal:** Player can select units, move them, and give basic orders. Replaces swarm-only control.
**Tag:** `v0.10.0-alpha`
**Design:** See `docs/MILITARY_SYSTEM.md`

| # | Task | Priority | Status | Notes |
|---|------|----------|--------|-------|
| 2.1 | Unit selection (click, box-select, shift+click, double-click) | HIGH | ⚪ | UnitSelectionManager.cs |
| 2.2 | A* pathfinding over tile grid (terrain costs, domain rules) | HIGH | ⚪ | PathfindingEngine.cs — land/sea/air domains |
| 2.3 | Move + Attack-Move + Patrol + Waypoint orders | HIGH | ⚪ | Right-click, A+click, P+click, Shift+click |
| 2.4 | Hold / Garrison / Fortify / Blockade defensive orders | MEDIUM | ⚪ | H, G, F keys + right-click sea zones |
| 2.5 | Order queue per unit (max 8, execute in sequence) | MEDIUM | ⚪ | Visual queue in unit info panel |
| 2.6 | Formations — Ctrl+1-9 save, 1-9 recall, rename | MEDIUM | ⚪ | FormationManager.cs + sidebar panel |
| 2.7 | Improve news feed — categorize, color-code, scannable | MEDIUM | ⚪ | Player needs to quickly read what happened |
| 2.8 | Frontline rendering (animated war zones) | LOW | ⚪ | Visual feedback for active wars |

**Exit criteria:** Player clicks a tank, right-clicks a destination, tank pathfinds there. Can create "3rd Fleet" and recall it with hotkey. Orders feel responsive.

---

## MILESTONE 3: RESOURCES & PRODUCTION
**Goal:** Military-industrial pipeline works. Player builds facilities, manufactures units, manages resources.
**Tag:** `v0.11.0-alpha`
**Design:** See `docs/MILITARY_SYSTEM.md` Parts 1-3

| # | Task | Priority | Status | Notes |
|---|------|----------|--------|-------|
| 3.1 | 6 resource types in data model (Iron, Oil, Uranium, Elec, Manpower, Food) | HIGH | ⚪ | Add to Models.cs, province yields by terrain |
| 3.2 | ResourceEngine.cs — collect resources per province per turn | HIGH | ⚪ | Pure C# engine, Phase 1 of turn pipeline |
| 3.3 | Facility system — build slots per city, construction timers | HIGH | ⚪ | FacilityData model, CityData gets slots |
| 3.4 | Production queues — queue units at facilities, 1 at a time | HIGH | ⚪ | ProductionEngine.cs — runs each turn |
| 3.5 | Expanded unit types (16 unit types across land/sea/air) | HIGH | ⚪ | Update UnitType enum, add UnitDomain |
| 3.6 | National Production Panel UI (click city → see facilities → build) | HIGH | ⚪ | The main build interface |
| 3.7 | City detail overlays (population, slots, port/airfield icons) | MEDIUM | ⚪ | Click or hover on cities |
| 3.8 | Trade for missing resources (buy oil from allies) | MEDIUM | ⚪ | TradeEngine integration |

**Exit criteria:** Click London → build a Barracks (2 turns) → queue Infantry → watch it roll out. Resources tick up each turn. Run out of oil = vehicles grounded.

---

## MILESTONE 4: UPKEEP, SUPPLY & MANPOWER
**Goal:** Running a military has real costs. Player makes hard choices about army size vs economy.
**Tag:** `v0.11.5-alpha`
**Design:** See `docs/MILITARY_SYSTEM.md` Parts 4-5

| # | Task | Priority | Status | Notes |
|---|------|----------|--------|-------|
| 4.1 | Unit upkeep costs per turn (money + fuel) | HIGH | ⚪ | EconomyEngine update |
| 4.2 | Supply lines — units >5 tiles from city lose strength | HIGH | ⚪ | Supply range calc, Fuel Depot extends +3 |
| 4.3 | Manpower pool — finite, scales with population | MEDIUM | ⚪ | Can't spam infinite infantry |
| 4.4 | Conscription mechanic (emergency boost, stability penalty) | MEDIUM | ⚪ | Desperate times button |
| 4.5 | Bankruptcy consequences (units weaken, vehicles stop) | HIGH | ⚪ | Already partial in EconomyEngine |
| 4.6 | Combat modifiers (terrain, fortification, encirclement, morale) | HIGH | ⚪ | Replace simple RNG combat |
| 4.7 | City siege mechanics — cities have HP before flipping | MEDIUM | ⚪ | FA-19. No more instant capture |
| 4.8 | Balance pass — income/upkeep/combat numbers feel right | MEDIUM | ⚪ | Playtest and tune |

**Exit criteria:** Building 50 tanks feels powerful but drains treasury. Overextended armies wither. Lean forces with good supply beat bloated ones.

---

## MILESTONE 5: WAR ROOM & OPERATIONS
**Goal:** Player can plan multi-phase invasions and see strategic targets at a glance.
**Tag:** `v0.12.0-beta`
**Design:** See `docs/MILITARY_SYSTEM.md` Parts 6-7

| # | Task | Priority | Status | Notes |
|---|------|----------|--------|-------|
| 5.1 | War Room panel — strategic targets, chokepoints, enemy capitals | HIGH | ⚪ | Click target → deploy forces |
| 5.2 | Invasion Planner — multi-phase operations (blockade → air → land) | HIGH | ⚪ | OperationData model, auto-generate orders |
| 5.3 | Bombardment orders (artillery + cruiser ranged attacks) | MEDIUM | ⚪ | Pre-combat softening |
| 5.4 | Amphibious assault (transports → beach landing at -30%) | MEDIUM | ⚪ | D-Day style operations |
| 5.5 | Airlift (mech infantry → distant airfield, needs air superiority) | LOW | ⚪ | Rapid deployment |
| 5.6 | Alliance webs — defensive pacts, coalition warfare | HIGH | ⚪ | FA-17. Allies join wars |
| 5.7 | Espionage grid — fog of war, DECEIVED state for fake intel | HIGH | ⚪ | FA-12. Intel integration with military |
| 5.8 | Rebellions & insurgency (low TA → rebel swarms) | MEDIUM | ⚪ | FA-13 |

**Exit criteria:** Player plans "Operation Iron Dawn" with naval blockade phase, air superiority phase, and ground assault phase. Executes it over 10 turns. Feels like a real commander.

---

## MILESTONE 6: ENDGAME & ESCALATION
**Goal:** Nuclear weapons, upgrades, DEFCON, and leader succession create dramatic late-game tension.
**Tag:** `v0.13.0-beta`
**Design:** See `docs/MILITARY_SYSTEM.md` Part 9

| # | Task | Priority | Status | Notes |
|---|------|----------|--------|-------|
| 6.1 | Facility upgrades (Elite Training, Advanced Armor, Stealth Hangar) | HIGH | ⚪ | Unlock advanced unit variants |
| 6.2 | Nuclear escalation path (Uranium Mine → Lab → Silo → Warheads) | HIGH | ⚪ | FA-22. The game's signature mechanic |
| 6.3 | Nuclear Sub Bay upgrade — mobile hidden launch platforms | HIGH | ⚪ | Late-game strategic terror |
| 6.4 | DEFCON system — global panic meter, each nuke built raises it | MEDIUM | ⚪ | FA-23 |
| 6.5 | WMD strikes — nuke a city, permanent consequences | HIGH | ⚪ | FA-24. Double-confirm, world reacts |
| 6.6 | Leader succession — assassination creates vengeful successor | MEDIUM | ⚪ | FA-25 |
| 6.7 | UN Assembly — embargoes, resolutions, diplomatic pressure | MEDIUM | ⚪ | FA-16 |

**Exit criteria:** Late game is dramatically different from early game. Building your second nuke makes the whole world panic. Nuclear subs prowl the oceans as hidden second-strike capability.

---

## MILESTONE 7: POLISH & SHIP
**Goal:** Feature-complete, polished, ready for wider release.
**Tag:** `v1.0.0`

| # | Task | Priority | Status | Notes |
|---|------|----------|--------|-------|
| 7.1 | Save & load game state | HIGH | ⚪ | FA-29. Critical for real play sessions |
| 7.2 | Espionage agency bases — spies on the map | LOW | ⚪ | FA-26 |
| 7.3 | True leader permadeath — nation collapse | LOW | ⚪ | FA-27 |
| 7.4 | Procedural global objectives ("Oil Crisis", "Pandemic") | MEDIUM | ⚪ | FA-28 |
| 7.5 | Black market trading — shadow arms deals | LOW | ⚪ | FA-15 |
| 7.6 | Media warfare — propaganda campaigns | LOW | ⚪ | FA-20 |
| 7.7 | Sound effects + music + animations | MEDIUM | ⚪ | FA-30 |
| 7.8 | Tutorial hints — first 3 turns explain controls | MEDIUM | ⚪ | New players need onboarding |
| 7.9 | Opening speech scene ("First Fire" narrative) | LOW | ⚪ | Dramatic intro from GAME_FLOW.md |
| 7.10 | Final balance, difficulty levels, performance optimization | HIGH | ⚪ | |

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
