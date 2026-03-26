# FULL AUTHORITY — The Phone Rings
## Consolidated Roadmap

> **One document. One plan. Every milestone ends with a testable build.**

---

## The Game

Real-time geopolitical thriller. You're not a god — you're a political operator climbing to power in a world that won't wait for you.

**Normal state:** Time flows. You manage your nation — move units, set budgets, deploy spies, read intel. Adjustable speed (1x, 2x, 5x, 10x). Pause anytime.

**Then the phone rings.** The simulation generates an **Interrupt** — a time-sensitive decision that demands your attention. A panel slams in with a countdown timer and 2-4 choices. If you don't pick, a default (usually bad) outcome fires. The world doesn't stop while you decide.

The timer length IS the design:
- Nuclear launch detected → 20 seconds
- Assassination window → 15 seconds
- Coup attempt on you → 10 seconds
- Military flashpoint → 45 seconds
- Economic crisis → 60 seconds
- Diplomatic offer → 90 seconds

**Win** by reaching Full Authority Index 100 through any combination of Territory Authority, World Authority, and Behind-the-Scenes Authority.

**Lose** if your FAI hits 0, your nation is conquered, or you're assassinated.

---

## What's Already Built

| System | Status | Files |
|--------|--------|-------|
| EventBus (typed pub/sub) | Working | `Core/EventBus.cs` |
| WorldStateManager (state + deltas) | Working | `Core/WorldStateManager.cs` |
| World generator (6 real nations, 80+ cities) | Working | `World/WorldGenerator.cs`, `Data/GeoData.cs` |
| Data models (Nations, Cities, Units, Characters, Authority) | Working | `Data/Models.cs` |
| Character setup (pick nation/role) | Working | `UI/Menus/CharacterSetupPanel.cs` |
| Political engine (bribes, threats, investigations) | Working | `Engines/PoliticalEngine.cs` |
| Crisis engine (random events with choices) | Working | `Engines/CrisisEngine.cs` |
| Military engine (swarm movement, combat, assassination) | Working | `Engines/MilitaryEngine.cs` |
| AI engine (rival nation behavior) | Working | `Engines/AIEngine.cs` |
| Economy engine (treasury, income) | Working | `Engines/EconomyEngine.cs` |
| Victory engine (win conditions) | Working | `Engines/VictoryEngine.cs` |
| Real-world map renderer (OSM tiles, dual style) | Working | `UI/Map/TileMapRenderer.cs` |
| Government overlay (borders, cities, units) | Working | `UI/Map/WarshipMapBridge.cs` |
| HUD (top bar, sidebars, news ticker, dossier) | Working | `UI/HUD/*.cs` |
| HotZone minimaps (pinnable 24×24 tile views) | Working | `UI/HUD/HotZoneMap.cs`, `UI/HUD/HotZoneManager.cs` |
| View switcher (Map/Intel/WarRoom/Economy tabs) | Working | `UI/HUD/MainViewSwitcher.cs` |
| Main menu | Working | `UI/Menus/MainMenu.cs` |
| Game events (typed event records) | Working | `Events/GameEvents.cs` |

---

## The Plan: 8 Milestones

Each milestone = a testable build. You can hand it to someone and say "play this."

---

### MILESTONE 1: The Clock Ticks
**Goal:** Convert from turn-based to real-time. Time flows, simulation ticks automatically.

**What changes:**
- `SimulationClock.cs` — New Node that replaces END TURN button. Runs the simulation pipeline on a timer. Adjustable speed (1x/2x/5x/10x). Pause/unpause with SPACE.
- `EndTurnButton.cs` → becomes speed control bar (Pause / 1x / 2x / 5x / 10x)
- All engines that listen to `TurnAdvancedEvent` keep working — they just fire on clock ticks instead of button presses
- HUD shows elapsed time + current speed instead of "Turn 1"

**New events:**
- `SimSpeedChangedEvent(float Speed)` — UI updates speed display
- `SimPausedEvent(bool IsPaused)` — freeze/unfreeze rendering

**Test:** Launch game → time advances automatically → economy ticks → AI moves units → crises fire on their own → player can pause/unpause with SPACE and change speed.

---

### MILESTONE 2: The Phone Rings
**Goal:** Interrupt system. Time-sensitive decisions slam onto the screen with countdown timers.

**What changes:**
- `InterruptEngine.cs` — Pure C# engine. Each tick, evaluates world state against trigger conditions. Generates `InterruptEvent`s with priority, timer duration, choices, and default outcome.
- `InterruptPanel.cs` — UI panel. Slides in from the side. Shows title, description, countdown bar, choice buttons. Auto-resolves on timeout.
- `InterruptQueue` — Priority queue in WorldStateManager. Multiple interrupts stack. Higher priority interrupts display first. Player works through the queue.

**New events:**
- `InterruptTriggeredEvent(string Id, string Title, string Description, float TimerSeconds, InterruptChoice[] Choices, int DefaultChoiceIndex, InterruptPriority Priority)`
- `InterruptResolvedEvent(string Id, int ChoiceIndex, bool WasTimeout)`

**Interrupt priority levels:**
- `CRITICAL` (nuclear, coup, assassination) — auto-pauses sim, red border, alarm sound
- `URGENT` (military, political crisis) — doesn't pause, orange border, 30-45s timer
- `ROUTINE` (trade deal, diplomatic offer) — doesn't pause, blue border, 60-90s timer

**Starter interrupts (expand later):**
- "Enemy fleet approaching your waters" (URGENT, 45s)
- "General offers to defect — wants asylum NOW" (URGENT, 30s)
- "Assassination window — your spy has the target alone" (CRITICAL, 15s)
- "Trade agreement expiring — renew or renegotiate?" (ROUTINE, 90s)
- "ICBM launch detected" (CRITICAL, 20s)
- "Coup plotters approaching your residence" (CRITICAL, 10s)

**Replaces:** `CrisisEngine.cs` gets merged into `InterruptEngine.cs`. Same concept, better execution.

**Test:** Play for 2 minutes → interrupts fire → countdown visible → picking a choice applies effects → ignoring it applies the default → CRITICAL interrupts auto-pause the sim.

---

### MILESTONE 3: The World Breathes
**Goal:** AI nations act continuously. You see them moving, trading, fighting each other — not just reacting to you.

**What changes:**
- `AIEngine.cs` — Expand to make AI nations do things every tick: move units, start wars with each other, form alliances, trade. The world is alive whether you interact or not.
- `DiplomacyData` added to `Models.cs` — Relations matrix between all nations. Wars, alliances, trade agreements, embargoes.
- `NewsEngine.cs` — Aggregates significant events from the last N ticks into a scrolling news feed. "Russia invades EU border province." "China-India trade agreement signed."
- Map shows AI unit movement in real-time, trade convoys flowing, borders shifting.

**New events:**
- `WarDeclaredEvent(string AggressorId, string DefenderId)`
- `AllianceFormedEvent(string NationA, string NationB)`
- `TradeAgreementEvent(string NationA, string NationB, float Value)`
- `TerritoryChangedEvent(string CityId, string OldOwnerId, string NewOwnerId)`

**Test:** Launch game → don't touch anything → watch AI nations go to war with each other, sign deals, move units. The world plays itself. News feed reports what's happening.

---

### MILESTONE 4: Your Hands on the Wheel
**Goal:** Player can actually DO things in real-time. Issue orders, manage budget, interact with the map.

**What changes:**
- **Unit orders panel** — Select unit → right-click destination → unit moves in real-time. Order types: Move, Attack, Patrol, Hold.
- **Budget panel** — Allocate spending: Military / Infrastructure / Intelligence / Diplomacy. Changes take effect over time, not instantly.
- **Diplomacy panel** — Click a nation → Propose alliance, Declare war, Offer trade, Demand tribute. AI responds based on relations + authority.
- **Power plays** — Bribe, Threaten, Investigate actions from the existing PoliticalEngine, now available anytime (not just on turn boundaries).

**Test:** Full gameplay loop — manage your nation in real-time, issue military orders, adjust budget, do diplomacy, respond to interrupts. You can play for 10 minutes and things happen.

---

### MILESTONE 5: The Fog
**Goal:** You don't know everything. Intelligence is unreliable. The map lies to you.

**What changes:**
- `IntelEngine.cs` — Manages what the player knows vs. reality. Three states for all foreign data: UNKNOWN (???), ESTIMATED (could be wrong), KNOWN (verified by spies).
- `FogOfWar` layer on map — Your territory is clear. Spy networks reveal areas. Everything else is dark or estimated.
- **Spy networks** — Deploy agents to nations. Over time they reveal: troop positions, treasury, alliances, intentions. But intel can be WRONG — counter-intelligence feeds you bad data.
- **The DECEIVED state** — Enemy counter-intelligence can feed your spies false information. You see confident data that is completely fabricated. No UI indicator that you're being deceived.
- Interrupt: "Intelligence report: Russia moving 50,000 troops to your border" — but is it real?

**Test:** Start game → foreign nations show "???" for most stats → deploy spies → gradually see more data → some of it turns out wrong → get deceived at least once.

---

### MILESTONE 6: The Bomb
**Goal:** Nuclear weapons. The ultimate trump card and the thing that makes your tiny nation matter.

**What changes:**
- `NuclearEngine.cs` — Manages warhead count, silo construction, launch authorization, fallout.
- **Nuclear deterrence** — Having nukes prevents AI from attacking you directly (unless they think you won't use them, or they get nukes too).
- **Launch sequence** — CRITICAL interrupt: "Authorize nuclear strike?" → Target selection → 3-tick countdown (can be aborted) → Devastation.
- **Consequences** — Nuking a city: destroys it permanently, massive WA penalty globally, all nations condemn you, but the target nation's TA collapses. MAD if multiple nations have nukes.
- **DEFCON meter** — Global tension indicator (5=peace, 1=imminent nuclear war). Rises with military buildups, wars, threats. At DEFCON 1, AI nations may launch preemptively.
- **Arms race** — AI nations research nukes over time. Once 2+ nations have them, the game becomes a knife-edge balance of terror.

**Test:** Play through to the point where you threaten nuclear strikes → AI backs down OR calls your bluff → launch a nuke → see the devastation and global reaction → other nations start building nukes → MAD scenario.

---

### MILESTONE 7: Save the World (or Load It)
**Goal:** Save/load game state. Autosave. Multiple save slots.

**What changes:**
- `SaveSystem.cs` — Serializes entire WorldData (including SimulationClock state, interrupt queue, diplomacy matrix, fog state) to JSON via Newtonsoft.Json.
- Autosave every N real-time minutes (configurable).
- Save slots in the main menu. Load from title screen or in-game pause menu.
- Save file includes RNG seed + tick count for deterministic replay capability.

**Test:** Play 5 minutes → save → keep playing → load → game state is identical to save point. Autosave works. Multiple slots work.

---

### MILESTONE 8: Ship It
**Goal:** Polish, balance, and package for playtest distribution.

**What changes:**
- **Balance pass** — Tune interrupt frequencies, timer durations, AI aggression, economy rates, authority gain/loss amounts. The game should feel tense but not overwhelming.
- **Sound design** — Phone ring SFX for interrupts. Ambient music shifts with DEFCON level. UI click sounds. Explosion/battle audio.
- **Visual polish** — Screen shake on explosions. Flash effects on battles. Smooth panel transitions. DEFCON color tinting on the whole screen.
- **Tutorial interrupt** — First 2 minutes of gameplay: guided interrupts that teach the core mechanics. "This is your first crisis. You have 45 seconds to decide..."
- **Settings** — Volume, game speed default, interrupt timer difficulty (longer timers = easier).
- **Export** — Godot export for Windows/Mac/Linux. Playtest-ready build.

**Test:** Hand the build to someone who's never seen it. They should be able to: start the game, understand the premise, play for 20 minutes, experience interrupts, manage their nation, and either win or lose. No crashes. No softlocks.

---

## What Got Cut (and Why)

These features from the old FA plan are **deferred** — not cancelled, just not needed for a testable game:

| Old Phase | Feature | Why Deferred |
|-----------|---------|-------------|
| FA-13 | Rebellions & Insurgency | Cool but not core. Add after M8 if game needs more internal threats. |
| FA-14 | National Debt & Bailouts | Economy works fine without this layer. Add for depth later. |
| FA-15 | Black Market Trading | Nice flavor. Not needed for core loop. |
| FA-16 | United Nations Assembly | Complex UI for a system that can be simulated simpler. Post-launch. |
| FA-18 | Border Checkpoints & Attrition | Terrain/movement detail. Post-launch. |
| FA-19 | City Siege Mechanics | Cities already flip via combat. Siege adds realism but not fun yet. |
| FA-20 | Media Warfare / Propaganda | Great thematic fit. Add after core is solid. |
| FA-21 | Military Research | Unit upgrades. Post-launch depth. |
| FA-25 | Leader Succession | Interesting edge case. Not core. |
| FA-26 | Espionage Agency Bases | Physical spies on map. Fog system covers intel for now. |
| FA-27 | True Leader Permadeath | Nation collapse mechanic. Post-launch. |
| FA-28 | Procedural Global Objectives | "Oil Crisis" / "Pandemic" — great for replayability. Post-launch. |
| M-8 | Map Mode Tabs UI | Keyboard shortcuts (F1/F2/G) work fine for now. |
| M-9 | City Detail Overlays | Nice visual. Not blocking gameplay. |
| M-10 | Frontline Rendering | Cool visual. Not blocking gameplay. |

---

## Architecture Notes

### SimulationClock replaces TurnEngine
The 13-phase pipeline concept stays. But instead of `EndTurnButton` → run all phases → wait, the `SimulationClock` runs phases on a timer:

```
Every tick (based on speed setting):
  1. ResourceCollection
  2. EconomicProcessing
  3. DiplomacyProcessing
  4. AIDecision
  5. MilitaryMovement
  6. CombatResolution
  7. IntelligenceProcessing
  8. InterruptEvaluation    ← NEW: checks conditions, fires interrupts
  9. NewsAggregation
  10. StateCommit
```

### InterruptEngine is the core innovation
This is what makes Full Authority different from every other grand strategy game. The interrupt system creates **asymmetric time pressure** — you're a political operator, not an omniscient deity. Some decisions can't wait. Some information is wrong. The phone rings whether you're ready or not.

### EventBus stays the backbone
Nothing changes architecturally. New engines publish new events. UI subscribes. No engine calls another engine. WorldStateManager is the single source of truth.

---

## Priority Order

```
M1  The Clock Ticks       ← Real-time foundation. Everything else builds on this.
M2  The Phone Rings       ← The unique mechanic. This IS the game.
M3  The World Breathes    ← AI makes the world feel alive.
M4  Your Hands on Wheel   ← Player agency. Now it's a game.
M5  The Fog               ← Unreliable intel. Now it's a GOOD game.
M6  The Bomb              ← Nuclear stakes. Now it's a TENSE game.
M7  Save/Load             ← Quality of life.
M8  Ship It               ← Polish and distribute.
```

---

## Design Documents

| Document | What it covers |
|----------|---------------|
| `docs/MILITARY_SYSTEM.md` | **30-part military warfare spec.** Resources, facilities, 16 unit types, production, orders, formations. Plus deep combat systems: unit stats/HP, combat formula, retreat/routing, ranged phases, air superiority zones, naval depth, strategic bombing, veterancy, morale, supply chains, city sieges, weather/seasons, combat doctrine, DEFCON, nuclear deterrence, convoy raiding, decoys/deception, generals, combined arms, war weariness, and system interaction web. |
| `docs/GAME_FLOW.md` | Player experience spec — what you see and do each turn |

Military systems integrate across milestones:
- **M4 (Your Hands on Wheel):** Unit selection, orders, movement, basic combat with the new formula
- **M5 (The Fog):** Intel depth, DECEIVED state, decoy units, recon planes
- **M6 (The Bomb):** DEFCON system, nuclear deterrence AI, WMD strikes, war weariness
- **M8 (Ship It):** Combat doctrine, combined arms, veterancy, weather, strategic bombing, naval depth

---

*This replaces PHASE_PLAN.md, FA_PHASE_PLAN.md, and the phase tracker in DEV_LOG.md.*
*Last updated: 2026-03-26*
