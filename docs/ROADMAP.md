# WARSHIP: Leaders of the Warship
## THE Master Roadmap

> **One document. One plan. No simplifying. Every system included.**
> **Last updated:** 2026-03-28

---

## The Game in 60 Seconds

Real-time geopolitical war game. SNES pixel art. Fictional world. You lead a small nation with the world's first nuclear weapon. Thirteen nations compete for dominance — 6 large powers and 7 small states. The world doesn't wait for you.

**Normal state:** Time flows continuously. You manage your nation — move armies, set budgets, deploy spies, negotiate treaties, build facilities. Adjustable speed (1x / 2x / 5x / 10x). Pause anytime with SPACE.

**Then the phone rings.** The simulation generates an **Interrupt** — a time-sensitive decision that slams onto the screen with a countdown timer. Nuclear launch detected? 20 seconds. Coup attempt? 10 seconds. Trade deal expiring? 90 seconds. If you don't decide, a default (usually bad) outcome fires. The world doesn't stop.

**Win conditions (concrete, scenario-dependent):**
- **Military Victory:** Control 60% of all provinces
- **Economic Victory:** Control 50% of global GDP
- **Diplomatic Victory:** 60% of nations allied to you
- **Nuclear Supremacy:** Only nation with nukes + delivery systems
- **Survival:** Still exist at game-time equivalent of turn 200
- **Scenario-specific:** Each campaign level has unique objectives

**Lose conditions:**
- All provinces conquered
- Stability at 0 for 10 consecutive ticks
- Treasury at -5000 for 5 consecutive ticks
- Hostile coup succeeds
- Player character assassinated (if enabled)

**Modes:**
- **Sandbox:** Generate a world, pick a nation, play freely. Any win condition.
- **Campaign:** Curated scenarios with specific starting conditions, objectives, and narrative.

---

## Tech Stack

| What | Tech |
|------|------|
| Engine | Godot 4 (.NET/C# build) |
| Language | C# (.NET 8+) |
| Serialization | Newtonsoft.Json |
| Map | Chunk-baked Sprite2D terrain, 32×32 pixel tiles |
| Armies | Pixel-dot swarm rendering (1 dot = 10 troops) |
| Territory | City-centric BFS flood-fill ownership |
| UI | Control nodes, SNES blue theme |
| RNG | SimRng (seeded, deterministic) |

---

## What's Already Built

| System | Status | Key Files |
|--------|--------|-----------|
| EventBus (typed pub/sub) | Working | `Core/EventBus.cs` |
| WorldStateManager (state + deltas) | Working | `Core/WorldStateManager.cs` |
| WorldSimulationCore (engine orchestrator) | Working | `Core/WorldSimulationCore.cs` |
| TurnEngine (13-phase pipeline) | Working | `Core/TurnEngine.cs` |
| SimRng (seeded deterministic RNG) | Working | `Core/SimRng.cs` |
| Procedural terrain generator (600×360) | Working | `World/TerrainGenerator.cs` |
| Procedural world generator (nations, cities, armies) | Working | `World/WorldGenerator.cs` |
| Data models (Nations, Cities, Armies, Characters) | Working | `Data/Models.cs` |
| Game events (typed event records) | Working | `Events/GameEvents.cs` |
| Chunk-based terrain renderer | Working | `UI/Map/TerrainChunkRenderer.cs` |
| Army swarm renderer (pixel dots + formations) | Working | `UI/Map/ArmySwarmRenderer.cs` |
| Territory border renderer | Working | `UI/Map/TerritoryBorderRenderer.cs` |
| MapManager (orchestrator for 3 layers) | Working | `UI/Map/MapManager.cs` |
| MapCamera (zoom/pan/edge scroll) | Working | `UI/Map/MapCamera.cs` |
| Character setup panel | Working | `UI/Menus/CharacterSetupPanel.cs` |
| Political engine (bribes, threats) | Working | `Engines/PoliticalEngine.cs` |
| Military engine (movement, combat) | Working | `Engines/MilitaryEngine.cs` |
| AI engine (rival behavior) | Working | `Engines/AIEngine.cs` |
| Economy engine (treasury, income) | Working | `Engines/EconomyEngine.cs` |
| HUD (top bar, sidebars, news ticker) | Working | `UI/HUD/*.cs` |
| Main menu | Working | `UI/Menus/MainMenu.cs` |

---

## Architecture Rules — NEVER Break These

**1. EventBus Only** — No engine calls another engine. Publish events, subscribe, react.
**2. WorldStateManager for All Writes** — Never mutate data directly. Use deltas.
**3. Pure C# Engines** — Sim engines = plain C#. Zero Godot API. Only Core singletons are Nodes.
**4. SimRng for ALL Randomness** — Deterministic replays require seeded RNG everywhere.

---

## The Fictional World — 13 Nations

Procedurally generated fictional world. 600×360 tile map at 32px = 19,200 × 11,520 pixel world. 5-7 continents with mountain ranges, rivers, biomes. Geography creates strategic challenges — coastal nations depend on trade, landlocked nations have defensible borders, island nations need naval power.

**Real-world map is a FUTURE addition (post-launch).** This game starts fictional.

### 6 Large Nations

| # | Name | Archetype | Personality | Starting Advantage |
|---|------|-----------|-------------|-------------------|
| 1 | **United States Alliance** | Hegemon (military dominant) | Expansionist | Largest army, highest GDP, 2 carriers, advanced tech |
| 2 | **Republic of Valdria** | Revolutionary (aggressive ideologues) | Aggressive | Fast unit production, high manpower, ideological zeal (+morale) |
| 3 | **Meridian Confederation** | Commercial League (trade focused) | Opportunist | Richest treasury, most trade routes, economic leverage |
| 4 | **Kingdom of Ashenmoor** | Traditionalist (conservative) | Defensive | Fortified cities, mountain terrain, high defense bonuses |
| 5 | **Volkren Collective** | Industrial Powerhouse | Expansionist | Highest production capacity, steel/iron rich, tank-heavy |
| 6 | **Thalassian Dominion** | Naval Empire | Opportunist | Island/coastal, strongest navy, controls sea lanes |

### 7 Small Nations

| # | Name | Terrain | Starting Condition |
|---|------|---------|-------------------|
| 7 | **Selvara** (Player default) | Mixed — 4 provinces | World's first nuclear weapon. Tiny but dangerous. |
| 8 | **Free City of Orinth** | Coastal city-state | Trade hub, rich but no army. Vulnerable. |
| 9 | **Kaelith Tribes** | Desert/steppe | Guerrilla fighters, spread thin, hard to conquer |
| 10 | **Duskhollow Pact** | Forest/swamp | Intelligence specialists, spy network depth +2 start |
| 11 | **Ironmarch Remnant** | Mountains | Former empire fragment, good defense, low manpower |
| 12 | **Port Serin** | Island chain | Naval base, submarine advantage, isolated |
| 13 | **Ashfall Compact** | Volcanic/barren | Uranium-rich, nuke potential, harsh terrain |

### Nation Generation Rules
- Large nations: 8-12 cities each, 5+ armies, 15-25 provinces
- Small nations: 2-5 cities each, 1-3 armies, 4-8 provinces
- City spacing: minimum 30 tiles between cities
- Territory: BFS flood-fill from each city up to its `ControlRadius`
- Borders: Pre-computed polyline segments, recomputed only on territory change
- Archetypes derived from terrain composition (coastal → naval, mountains → defensive, plains → military, etc.)
- Player (Selvara) always spawns in a defensible but resource-poor position surrounded by large nations

### Nation Data Structure
Each nation tracks (concrete, no abstract meters):
- **Territory:** Which provinces/cities they control (tile-level ownership map)
- **Treasury:** Actual gold amount, income/expenses per tick
- **Military:** Actual armies with unit compositions (Dictionary<UnitType, int>)
- **Resources:** Actual stockpiles of Iron, Oil, Uranium, Electronics, Manpower, Food
- **Diplomacy:** Actual relationships with every other nation (war, alliance, trade agreement, embargo, neutral)
- **Spy Networks:** Actual depth per target nation (0-5 scale, each level = more intel revealed)
- **War Weariness:** 0-100 scale tracking public tolerance for conflict
- **Stability:** Actual unrest level from economic/military/political factors
- **DEFCON contribution:** Actions that raise/lower global tension

**No authority meters. No FAI. Everything is concrete and measurable.**

---

## Real-Time Clock System

Replaces TurnEngine's button-press model. Time flows continuously.

### SimulationClock.cs (New Autoload Node)
- Replaces END TURN button with continuous ticking
- Speed settings: Pause (0x), Normal (1x), Fast (2x), Faster (5x), Max (10x)
- SPACE bar toggles pause/unpause
- Each "tick" runs the full simulation pipeline once
- Tick rate at 1x = 1 tick per 2 real seconds (tunable)
- At 10x = 10 ticks per 2 real seconds = 5 ticks/sec
- All existing engines keep working — they fire on clock ticks instead of button presses

### Simulation Pipeline Per Tick
```
 1. ResourceCollection    → Province yields based on terrain + facilities
 2. EconomicProcessing    → GDP, treasury, income/expenses
 3. TradeProcessing       → Route income, tariffs, convoy movement
 4. ProductionProcessing  → Facility/unit build progress advances
 5. UnrestProcessing      → Stability checks, rebellion risk
 6. PoliticsProcessing    → Faction influence, coup risk
 7. DiplomacyProcessing   → Relation drift, treaty timers, AI proposals
 8. AIDecision            → AI nations issue orders (move, build, declare war, trade)
 9. PlayerActionExecution → Queued player commands resolve
10. MilitaryMovement      → Armies move toward targets
11. CombatResolution      → Battles resolve where armies meet
12. IntelligenceProcessing → Spy ops advance, intel updates, counter-intel
13. InterruptEvaluation   → Check trigger conditions, fire interrupts
14. WarWearinessUpdate    → WW changes from combat/peace
15. DEFCONUpdate          → Global tension recalculation
16. NewsAggregation       → Format significant events into feed
17. StateCommit           → Commit all deltas, advance tick counter
```

### New Events
- `SimSpeedChangedEvent { float Speed }`
- `SimPausedEvent { bool IsPaused }`
- `TickCompletedEvent { int TickNumber }`

### HUD Changes
- Turn counter becomes elapsed time display + current speed
- Speed control bar replaces END TURN button (Pause / 1x / 2x / 5x / 10x)

---

## The Phone Rings — Interrupt System

The core innovation. Time-sensitive decisions with countdown timers. This IS the game's signature mechanic.

### InterruptEngine.cs (Pure C# Engine)
- Evaluates world state every tick against trigger conditions
- Generates `InterruptEvent`s with priority, timer, choices, default outcome
- Multiple interrupts queue by priority — highest priority displays first
- Player works through the queue

### InterruptPanel.cs (UI)
- Slides in from the side of the screen
- Shows: title, description, countdown bar, 2-4 choice buttons
- Auto-resolves with default outcome on timeout
- CRITICAL interrupts: red border, alarm sound, auto-pauses simulation
- URGENT interrupts: orange border, doesn't pause, 30-45s timer
- ROUTINE interrupts: blue border, doesn't pause, 60-90s timer

### Timer Durations (The timer length IS the design)
| Interrupt Type | Timer | Priority |
|---------------|-------|----------|
| ICBM launch detected | 20 seconds | CRITICAL |
| Assassination window — spy has target alone | 15 seconds | CRITICAL |
| Coup plotters approaching your residence | 10 seconds | CRITICAL |
| Nuclear facility detected in enemy nation | 30 seconds | URGENT |
| Enemy fleet approaching your waters | 45 seconds | URGENT |
| General offers to defect — wants asylum NOW | 30 seconds | URGENT |
| Military flashpoint — border skirmish escalating | 45 seconds | URGENT |
| Anti-war protests erupting (War Weariness > 50) | 60 seconds | URGENT |
| Economic crisis — currency collapse | 60 seconds | URGENT |
| Trade agreement expiring — renew or renegotiate? | 90 seconds | ROUTINE |
| Diplomatic offer from foreign nation | 90 seconds | ROUTINE |
| Intelligence report — enemy troop movements | 60 seconds | ROUTINE |
| Ceasefire proposal | 75 seconds | ROUTINE |

### Interrupt Events
- `InterruptTriggeredEvent { string Id, string Title, string Description, float TimerSeconds, InterruptChoice[] Choices, int DefaultChoiceIndex, InterruptPriority Priority }`
- `InterruptResolvedEvent { string Id, int ChoiceIndex, bool WasTimeout }`

### Integration with Existing Systems
- Replaces/absorbs CrisisEngine.cs — same concept, better execution
- InterruptEngine reads world state to decide triggers (e.g., War Weariness > 50 → protest interrupt)
- Each choice maps to concrete game effects (delta applications via WorldStateManager)
- AI nations have their own interrupt-like decision moments (handled internally by AIDecisionEngine)

---

## Military Production System

### Part 1: Strategic Resources

Six resources. Provinces produce them based on terrain. You CANNOT build a full war machine alone — trade or conquest required.

| Resource | Source Terrain | Used For |
|----------|--------------|----------|
| Iron | Hills, Mountains | Steel → Vehicles, Ships, Buildings |
| Oil | Sand, specific provinces | Fuel → All mechanized units |
| Uranium | Rare (2-3 tiles worldwide) | Nuclear weapons |
| Electronics | Cities (size 2+) | Radar, Missiles, Advanced units |
| Manpower | All provinces (scales w/ pop) | Infantry, crews for everything |
| Food | Grass, Farms | Keeps manpower flowing, prevents unrest |

Resource scarcity drives gameplay: small nations like Selvara have limited Iron, minimal Uranium (just enough for 1 warhead), moderate Electronics and Food. Must trade for Oil and Iron or conquer resource-rich provinces.

### Part 2: Facilities

Built in cities. Each city has limited build slots based on size.

| City Size | Slots |
|-----------|-------|
| Town (1) | 2 |
| City (2) | 4 |
| Capital (3) | 6 |

#### Resource Processing Facilities
| Facility | Cost | Build Time | Effect |
|----------|------|------------|--------|
| Steel Mill | $200M + 50 Iron | 3 ticks | Iron → Steel (required for vehicles/ships) |
| Oil Refinery | $300M + 30 Iron | 4 ticks | Crude Oil → Fuel (required for mechanized) |
| Electronics Lab | $250M + 20 Elec | 3 ticks | Advanced Components (required for missiles/radar) |

#### Military Production Facilities
| Facility | Cost | Build Time | Produces |
|----------|------|------------|----------|
| Barracks | $100M | 2 ticks | Infantry, Mech Infantry |
| Tank Factory | $400M + 80 Steel | 5 ticks | Tanks, Artillery, Anti-Air |
| Shipyard | $500M + 100 Steel | 6 ticks | All naval units |
| Airfield | $350M + 50 Steel | 4 ticks | Fighters, Bombers, Recon Planes |
| Missile Silo | $800M + 50 Uranium + 30 Elec | 8 ticks | Nuclear weapons |

#### Support Facilities
| Facility | Cost | Build Time | Effect |
|----------|------|------------|--------|
| Fuel Depot | $150M | 2 ticks | +3 tile supply range from this city |
| Radar Station | $200M + 10 Elec | 3 ticks | 15-tile fog reveal radius |
| Military Hospital | $150M | 2 ticks | Units in range heal 20%/tick |
| Fortress | $300M + 60 Steel | 4 ticks | City defense +100% |
| Intelligence HQ | $400M + 20 Elec | 5 ticks | Enables spy operations from this city |

### Part 3: Unit Roster (16 Unit Types)

#### Land Units
| Unit | Facility | Cost | Resources | Build Time | HP | Attack | Defense | Move | Special |
|------|----------|------|-----------|------------|-----|--------|---------|------|---------|
| Infantry | Barracks | $50M | 10 MP | 1 tick | 100 | 30 | 40 | 2 | Garrison cities, ambush in forests (+25% def) |
| Mech Infantry | Barracks | $100M | 10 MP, 5 Steel | 2 ticks | 120 | 40 | 35 | 3 | Can be airlifted, fast in plains |
| Tank | Tank Factory | $200M | 20 Steel, 10 Fuel | 3 ticks | 200 | 70 | 50 | 3 | Fast on flat, weak in mountains (-30%) |
| Artillery | Tank Factory | $150M | 15 Steel | 2 ticks | 80 | 60 | 15 | 1 | 2-tile range, can't move+fire same tick |
| Anti-Air | Tank Factory | $180M | 15 Steel, 5 Elec | 2 ticks | 100 | 20 | 30 | 2 | Shoots down aircraft, negates air bonuses |
| Mobile Radar | Tank Factory | $120M | 10 Elec | 2 ticks | 60 | 0 | 10 | 2 | Huge fog reveal radius, radio deception capable |

#### Naval Units
| Unit | Facility | Cost | Resources | Build Time | HP | Attack | Defense | Move | Special |
|------|----------|------|-----------|------------|-----|--------|---------|------|---------|
| Destroyer | Shipyard | $300M | 30 Steel, 10 Fuel | 3 ticks | 150 | 45 | 40 | 4 | Anti-sub specialist, escort duty |
| Cruiser | Shipyard | $500M | 50 Steel, 15 Fuel | 5 ticks | 250 | 65 | 55 | 3 | Shore bombardment (2-tile inland range) |
| Submarine | Shipyard | $400M | 40 Steel, 10 Elec | 4 ticks | 120 | 55 | 25 | 3 | Invisible until attacking, convoy raiding |
| Carrier | Shipyard | $800M | 80 Steel, 30 Fuel, 15 Elec | 8 ticks | 300 | 20 | 60 | 2 | Carries 4 fighters, mobile airfield |
| Transport | Shipyard | $200M | 20 Steel, 5 Fuel | 2 ticks | 80 | 0 | 20 | 3 | Carries 8 land units across water |

#### Air Units
| Unit | Facility | Cost | Resources | Build Time | HP | Attack | Defense | Move | Special |
|------|----------|------|-----------|------------|-----|--------|---------|------|---------|
| Fighter | Airfield | $250M | 20 Steel, 10 Fuel | 2 ticks | 80 | 50 | 30 | 6 | Air superiority, intercept bombers |
| Bomber | Airfield | $350M | 30 Steel, 15 Fuel | 3 ticks | 100 | 70 | 15 | 4 | Strategic bombing, area damage |
| Recon Plane | Airfield | $150M | 10 Steel, 5 Elec | 1 tick | 40 | 0 | 10 | 8 | Massive fog reveal, spots subs |

#### Special Units
| Unit | Facility | Cost | Resources | Build Time | HP | Attack | Defense | Move | Special |
|------|----------|------|-----------|------------|-----|--------|---------|------|---------|
| Nuclear Submarine | Shipyard + Silo | $1000M | 80 Steel, 30 Elec, 10 Uranium | 10 ticks | 200 | 40 | 30 | 3 | Second-strike capability, invisible, carries 2 warheads |
| ICBM | Missile Silo | $500M | 20 Uranium, 20 Elec | 5 ticks | N/A | N/A | N/A | N/A | Launches from silo, hits anywhere, nuclear devastation |

### Part 4: Upkeep & Supply

Every unit costs money and fuel per tick:
- **Monetary upkeep:** ~10% of build cost per tick (Infantry $5M, Tank $20M, Carrier $80M)
- **Fuel consumption:** Mechanized/naval/air units consume fuel when moving. Stationary = half fuel cost.
- **Supply range:** Units within supply range of a friendly city or Fuel Depot operate normally. Beyond range:
  - Tick 1-3: No healing
  - Tick 4-6: -10% combat effectiveness
  - Tick 7+: -25% combat, -5 HP/tick attrition
- **Out of fuel:** Mechanized units can't move. Aircraft forced to land (nearest friendly airfield) or crash. Ships drift.

### Part 5: Manpower System

- Each nation has a manpower pool (scales with population/provinces)
- Infantry costs manpower to build AND to reinforce
- Vehicles/ships/aircraft need crew (smaller manpower cost)
- Manpower regenerates slowly per tick from controlled provinces
- Heavy casualties → manpower shortage → can't reinforce → army weakens
- Capturing populous provinces = manpower boost
- Losing provinces = manpower drain

### Unit Rendering (Already Built)
- Army swarm renderer: 1 pixel dot = 10 troops
- LOD: 1px dots at zoom < 0.3, 2px at 0.3-0.8, 3px at > 0.8
- Formation shapes: Column (marching), Wedge (attacking), Spread (default), Circle (garrison)
- Color variation by domain: land=nation color, naval=blue-shifted, air=lightened, special=orange-red
- Army labels shown at zoom > 0.5

---

## Military Orders & Formations

### Part 6: Orders

Each army can be given one order at a time:

| Order | Effect | Valid For |
|-------|--------|-----------|
| Move | Move toward target tile at army move speed | All |
| Attack Move | Move toward target, engage enemies encountered | All |
| Hold | Stay in position, do not pursue | All |
| Patrol | Move between two waypoints, engage contacts | All |
| Garrison | Enter a city, +25% defense, can't be flanked | Land |
| Bombard | Artillery/Cruiser fires at target within range | Artillery, Cruiser |
| Air Patrol | Fighters circle zone, intercept enemy air | Fighter |
| Strategic Bomb | Bomber targets enemy facility/city | Bomber |
| Blockade | Submarine/Destroyer disrupts trade route in sea zone | Naval |
| Escort | Destroyer protects convoy/carrier in sea zone | Destroyer |
| Airlift | Transport Mech Infantry to target airfield | Mech Infantry + Airfield |
| Amphibious Assault | Land units load onto Transport, assault coastal tile | Land + Transport |
| Nuclear Strike | Launch warhead at target | ICBM, Nuclear Sub |

### Part 7: Formations

Armies are organized into formations. Each formation has:
- A **composition** (Dictionary<UnitType, int>) — how many of each unit type
- A **formation type** affecting combat behavior
- An optional **general** providing command bonuses

| Formation | Shape | Bonus | Penalty |
|-----------|-------|-------|---------|
| Column | Narrow, long | +20% move speed | -10% defense (flanking exposure) |
| Wedge | Forward-leaning triangle | +15% attack | -5% defense |
| Spread | Wide, dispersed | Reduced area damage taken | -10% attack (less concentration) |
| Circle | Defensive ring | +20% defense | Cannot move while in formation |
| Line | Classic battle line | Balanced, no bonus/penalty | — |

### Part 8: Combat Modifiers

Terrain affects combat:
| Terrain | Attack Modifier | Defense Modifier |
|---------|----------------|-----------------|
| Plains | +0% | +0% |
| Forest | -10% | +25% (ambush) |
| Hills | -5% | +15% |
| Mountains | -20% | +40% |
| River Crossing | -25% | +0% |
| Urban/City | -15% | +30% |
| Desert | +5% (open sightlines) | -5% |
| Snow | -10% | -10% |

Weather modifiers (applied on top):
| Condition | Effect |
|-----------|--------|
| Rain | -10% attack all, -20% air effectiveness |
| Snow/Blizzard | -15% attack, -30% move speed, double fuel consumption |
| Fog | -20% ranged attack, +20% ambush chance |
| Night | -15% attack, +25% submarine/covert effectiveness |

### Part 9: Unit Upgrades

Units can be upgraded at facilities (costs resources + time):
- Infantry → Mech Infantry (at Barracks, +$50M, +5 Steel)
- Fighter Mk1 → Fighter Mk2 (at Airfield, +$100M, +5 Elec, +10 Attack, +5 Defense)
- Tank → Heavy Tank (at Tank Factory, +$150M, +15 Steel, +30 Attack, +20 HP)
- Destroyer → Guided Missile Destroyer (at Shipyard, +$200M, +10 Elec, anti-air capability)

Upgrades take time (unit is unavailable during upgrade). Strategic decision: upgrade existing or build new?

### Part 10: Nuclear Escalation Ladder

Nukes are not just weapons — they reshape the entire game:

1. **Research:** Build Missile Silo ($800M + Uranium + Electronics, 8 ticks)
2. **Production:** Build warhead ($500M + 20 Uranium + 20 Elec, 5 ticks)
3. **Delivery:** Warhead goes onto ICBM (silo-based, vulnerable) or Nuclear Sub (mobile, invisible)
4. **Authorization:** Player must explicitly authorize launch (CRITICAL interrupt with 10-second confirm)
5. **Launch:** 3-tick flight time for ICBM. Sub-launched = 1-tick (closer range).
6. **Impact:** Target city destroyed permanently. 5-tile radius devastation. Fallout zone for 20 ticks.
7. **Aftermath:** All nations condemn you (-50 relations with everyone). DEFCON jumps to 1. Other nuclear nations may retaliate (MAD).

---

## Combat Resolution

### Part 11: The Combat Formula

When two armies occupy the same tile (or adjacent for ranged), combat resolves:

```
Damage = (AttackerStrength × AttackStat / DefenderDefenseStat)
         × TerrainModifier
         × WeatherModifier
         × MoraleModifier
         × FormationModifier
         × CombinedArmsBonus
         × VeterancyBonus
         × GeneralCommandBonus
         × SupplyModifier
```

- Both sides deal damage simultaneously
- Damage is distributed across unit types in the army (proportional to count)
- Individual "units" within the army have HP — when HP reaches 0, that unit is destroyed
- Destroyed units reduce army dot count in real-time (visible on map)

### Part 12: Retreat & Routing

Armies have morale (0-100). Combat and events affect morale:

| Morale Level | Effect |
|-------------|--------|
| 75-100 | Full effectiveness |
| 50-74 | -10% combat stats |
| 25-49 | -25% combat stats, may retreat |
| 1-24 | -50% combat stats, will retreat |
| 0 | **ROUT** — army flees at max speed toward nearest friendly city, cannot receive orders for 5 ticks |

**Retreat triggers:**
- Morale drops below 25: 50% chance to retreat per tick
- Morale drops below 10: automatic retreat
- Army loses 50%+ strength in a single combat: automatic retreat
- General killed: -30 morale instantly, high rout chance

**Rout behavior:**
- Routed army moves at 1.5x speed toward nearest friendly city
- Cannot attack, defend at 50% effectiveness
- Regains control after reaching friendly territory or after 5 ticks
- Other armies that SEE a friendly rout: -5 morale (panic spreads)

### Part 13: Ranged Combat

Artillery and Cruisers attack from range without entering the target tile:

- **Artillery:** 2-tile range, must be stationary, can't fire and move same tick
- **Cruiser shore bombardment:** 2-tile inland range from coastal water tile
- **Ranged units fire FIRST** in combat resolution (before melee contact)
- Ranged damage is reduced by 50% against units in Fortress or City defense
- Counter-battery: if two artillery units are in range of each other, they exchange fire

### Part 14: Air Superiority Zones

Aircraft operate in "air zones" — a zone covers a radius around the airfield or carrier they're based at.

- **Fighter patrol zone:** 8-tile radius from base. Intercepts enemy air in this zone.
- **Bomber operating range:** 12-tile radius from base.
- **Air superiority:** If you have more fighters in a zone than the enemy → you have air superiority. Grants:
  - +10% attack to your ground units in the zone (close air support)
  - Enemy bombers in the zone are intercepted (fighter vs bomber combat before bombing resolves)
  - Enemy recon planes in the zone are shot down
- **Anti-Air units negate air bonuses** for the tile they're on (not the whole zone)
- **Carriers are mobile airfields** — their fighter zone moves with them

### Part 15: Naval Engagement

Naval combat has depth layers:

1. **Surface:** Destroyers, Cruisers, Carriers exchange fire
2. **Subsurface:** Submarines attack from below — invisible until they fire
3. **Anti-sub warfare:** Destroyers have sonar — detect subs within 3 tiles. Once detected, sub loses stealth.
4. **Carrier strikes:** Carrier launches fighters to attack enemy surface fleet (carrier stays back)

**Sea zone control:** Controlling sea zones (having the most naval power) grants:
- Trade route protection (convoys pass safely)
- Amphibious assault capability (can land troops on enemy coast)
- Shore bombardment access (cruisers can hit inland targets)
- Blockade capability (enemy trade routes through this zone disrupted)

### Part 16: Strategic Bombing

Bombers target enemy infrastructure:

| Target | Effect | Repair Time |
|--------|--------|-------------|
| Factory/Mill | Production halted | 5 ticks |
| Airfield | Aircraft can't launch | 3 ticks |
| Shipyard | Ship production halted | 5 ticks |
| Oil Refinery | Fuel production halted | 4 ticks |
| Radar Station | Fog reveal disabled | 2 ticks |
| City center | -10% city production, civilian casualties (War Weariness +5 for BOTH sides) |

Bombing defended cities: Anti-Air shoots at bombers first. Fighter escort protects bombers. Unescorted bombers against fighters = heavy bomber losses.

---

## Advanced Military Systems

### Part 17: Veterancy

Units that survive combat gain experience:

| Level | XP Required | Bonus |
|-------|------------|-------|
| Green | 0 | No bonus |
| Regular | 50 XP | +5% attack/defense |
| Veteran | 150 XP | +10% attack/defense, +1 move |
| Elite | 300 XP | +20% attack/defense, +1 move, +10 morale floor |
| Legendary | 500 XP | +30% attack/defense, +2 move, +20 morale floor, name generated |

XP gained:
- Surviving a battle: +20 XP
- Winning a battle: +10 XP bonus
- Destroying an enemy unit: +15 XP
- Capturing a city: +25 XP

Legendary units get procedurally generated names and are visible in the news feed. Losing a Legendary unit = -10 national morale, +5 War Weariness.

### Part 18: Morale System

Each army has a morale value (0-100) affecting combat effectiveness:

**Morale increases:**
| Event | Change |
|-------|--------|
| Winning a battle | +10 |
| Capturing a city | +15 |
| In friendly territory | +2/tick |
| Well supplied | +1/tick |
| General present | +5 (passive) |
| National victory (any army wins big) | +5 to all armies |

**Morale decreases:**
| Event | Change |
|-------|--------|
| Losing a battle | -15 |
| Heavy casualties (30%+ in one fight) | -20 |
| Out of supply | -5/tick |
| In enemy territory (no supply) | -3/tick |
| Friendly army routed nearby | -5 |
| General killed | -30 |
| Nuclear weapon detonated nearby | -25 |
| War Weariness > 50 nationally | -1/tick |

### Part 19: Supply Chains

Supply flows from cities through connected territory:

- **Supply source:** Any friendly city with a Fuel Depot or the Capital
- **Supply range:** Fuel Depot adds +3 tile range. Capital has base 5-tile range.
- **Supply path:** Must trace through friendly-controlled tiles (no crossing enemy territory)
- **Naval supply:** Transport ships can extend supply across water
- **Supply cut:** If enemy captures a province in your supply chain, units beyond that point lose supply
- **Attrition:** Unsupplied units take damage and lose combat effectiveness (see Part 4)

Strategic gameplay: cutting enemy supply lines (via naval blockade, province capture, or bombing Fuel Depots) can win without direct combat.

### Part 20: City Sieges

Armies don't instantly capture cities. Siege mechanics:

1. **Encirclement:** Attacker must have armies on 3+ sides of a city (or 2+ for coastal cities)
2. **Siege begins:** City defense HP starts depleting based on attacker strength vs. garrison defense
3. **Garrison:** City has inherent defense + any garrisoned army. Fortress doubles defense HP.
4. **Bombardment:** Artillery/Cruisers accelerate siege (damage city defense without assaulting)
5. **Assault:** Attacker can choose to storm the city (faster but high casualties) or starve it out
6. **Starvation:** Encircled city without supply: -10 defense HP/tick, garrison morale -5/tick
7. **Fall:** When city defense HP reaches 0, city is captured. Garrison army is destroyed or surrenders.
8. **Capture effects:** Capturing all cities of a nation = that nation is conquered. Territory flips. Remaining armies of conquered nation become guerrillas (weakened, no resupply, fight on for 10 ticks then disband).

### Part 21: Weather & Seasons

The world has a seasonal cycle (every ~50 ticks = 1 season):

| Season | Effects |
|--------|---------|
| Spring | Normal conditions. Rivers flood (+1 tile width, harder to cross). |
| Summer | +10% movement speed. Desert heat: -5 HP/tick to non-desert units in desert. |
| Autumn | Rain common: -10% attack, -20% air effectiveness. Harvests: +20% food production. |
| Winter | Snow in northern/mountain tiles: -30% movement, double fuel, -15% attack. Frozen rivers: crossable without penalty. |

**Weather events** (random per region per tick):
- Clear, Cloudy, Rain, Storm, Fog, Blizzard (winter only)
- Storms ground aircraft and prevent naval movement
- Fog enables surprise attacks (+20% ambush bonus)

Historical lesson: invading in winter is catastrophic. The game makes you feel why.

### Part 22: Combat Doctrine

Each nation can adopt ONE doctrine that shapes their military identity:

| Doctrine | Bonuses | Penalties | Best For |
|----------|---------|-----------|----------|
| Blitzkrieg | +20% tank/mech speed, +15% first-strike | -10% defense, -15% siege | Fast conquest, open terrain |
| Fortress Defense | +25% defense all units, +30% city defense | -15% attack, -20% move speed | Small nations, holding territory |
| Naval Supremacy | +20% all naval stats, +15% carrier capacity | -10% land combat | Island/coastal nations |
| Air Doctrine | +20% air stats, +25% bombing damage | -10% ground combat | Tech-advanced nations |
| Guerrilla Warfare | +30% forest/mountain defense, +20% ambush | -15% open terrain combat | Small nations, rough terrain |
| Combined Arms | +10% to all combined arms bonuses (stacks) | No pure-type bonuses | Balanced, large armies |

Changing doctrine takes 10 ticks (military reorganization) and costs morale.

### Part 23: DEFCON System

Global tension level visible on HUD. Affects all nations.

| Level | Trigger | Effects |
|-------|---------|---------|
| DEFCON 5 (Peace) | Default start | Normal gameplay |
| DEFCON 4 (Guarded) | Nuclear facility built OR major war starts | +10% military spending globally, -10 diplomacy |
| DEFCON 3 (Elevated) | Warhead produced OR 2+ wars active | +20% unit production speed, -20% trade income |
| DEFCON 2 (Severe) | Warhead on delivery platform OR 3+ wars | Full mobilization, civilian economy -30%, AI may preemptive strike |
| DEFCON 1 (Critical) | Nuclear strike authorized or launched | Last chance diplomacy. 5-tick countdown before AI considers MAD acceptable |

**De-escalation:** DEFCON only drops if: no wars for 5 consecutive ticks AND no nukes under construction. 1 level per 5 peaceful ticks.

**Visual feedback:**
- HUD gauge shows DEFCON level prominently
- DEFCON 2+: subtle red screen tint
- DEFCON 1: klaxon sound, flashing red border on entire screen

### Part 24: Nuclear Deterrence AI

AI nations evaluate a Deterrence Score before military action against you:

```
DeterrenceScore = (NumWarheads × 50) + (HasNukeSub ? 200 : 0)
                + (DeliveryRange × 10) + (MilitaryReputation × 2)
```

| Score | AI Response |
|-------|-------------|
| 0-49 | Ignores nuclear threat. May invade freely. |
| 50-99 | Hesitates. 50% chance to abort invasion. Seeks allies first. |
| 100-199 | Will NOT invade directly. Uses proxy wars, economic pressure, covert ops instead. |
| 200+ | Full MAD. Treats you as untouchable. Shifts entirely to diplomatic/economic competition. |

**Selvara starting position:**
- Deterrence = 50 (1 warhead, no second strike, no sub)
- Large nations hesitate but don't fully back off
- Getting a Nuclear Submarine → Deterrence jumps to 250+ (game-changing strategic goal)

**Player strategic choice:** Invest in more visible warheads (provocative, raises DEFCON, but shows strength) or build a Nuclear Submarine (expensive, slow, but invisible second-strike capability that terrifies AI permanently)?

---

## Intelligence & Deception

### Part 25: Convoy / Trade Raiding

Trade routes (visualized as green lines on map) are targetable:

- Each trade route generates income for both endpoint nations
- Routes pass through sea zones
- **Submarines on Blockade order** in a sea zone: 30% chance/tick to disrupt convoy (reduce route income by 25% for 3 ticks)
- **Destroyers on Escort order** in same zone: each reduces sub intercept chance by 15%
- Multiple subs in same zone stack disruption chance (diminishing returns)

**Emergent gameplay:** Enemy trade flows through ocean between continents. You send subs to strangle it. They send destroyers to escort. Cat-and-mouse naval warfare.

**Strategic asymmetry for small nations:** Can't match a large nation's army or tank count, but 5 subs can strangle their overseas trade. If they catch your subs though, you lose expensive irreplaceable assets.

### Part 26: Decoy & Deception

Tools to exploit the DECEIVED intel state — making the enemy see what you want:

**Decoy Units:**
- Built at Barracks for $20M + 2 Manpower (cheap)
- Appear identical to real units on enemy intel
- Destroyed in 1 hit — revealed as fake
- Enemy spy network depth determines detection: Depth 0 = 0% detected, Depth 1 = 20%, Depth 2 = 50%, Depth 3 = 80%

**Radio Deception:**
- Mobile Radar unit special order
- Creates ghost blips showing fake unit concentrations on enemy radar
- Costs $10M/tick, lasts until Radar destroyed or order cancelled

**Dummy Facilities:**
- Build "Dummy Airfield" or "Dummy Missile Silo"
- 1/10th real cost, appears real on enemy intel
- Draws enemy bombing raids to waste targets

**Example play:** 10 real tanks + 20 decoys = enemy sees 30 tanks on their border. They shift defenders to meet your "invasion." Your real 10 tanks attack somewhere else.

### Part 27: Generals / Commanders

Characters with military roles assigned to formations:

**Command Bonus:**
- General provides +X% to all units in their formation, where X = General's Military Skill / 5
- Skill-60 general = +12% to all combat stats
- Max 3 generals per nation

**Special Abilities by Role:**
| Role | Bonus |
|------|-------|
| Defense Minister | +15% defense to assigned formation |
| Chief of Staff | +15% attack to assigned formation |
| Director of Intelligence | +3 tile vision range for assigned formation |

**Risk:**
- Generals are physically on the map (part of the formation they command)
- If general is killed (assassination or formation destroyed) → formation loses bonus + -20 morale hit to all armies nationally
- Player character can personally command a formation (risky but powerful — if you die, game over)

**Player choice:** Director of Intelligence gives amazing vision but low command bonus. Defense Minister gives great defense but you risk your own character on the front line.

---

## War Weariness & Combined Arms

### Part 28: Combined Arms Bonuses

Different unit types fighting together get synergy bonuses:

| Combination | Bonus | Reason |
|-------------|-------|--------|
| Infantry + Tank (same tile) | +15% attack both | Infantry screens, tanks punch through |
| Artillery adjacent to friendlies in combat | +20% attack to engaged friendlies | Fire support |
| Fighter overhead (air zone covers battle) | +10% attack to ground units | Close air support |
| Destroyer + Carrier (same zone) | +20% defense to carrier | Escort screen |
| Anti-Air + any ground (same tile) | Negates enemy air bonus | Air denial |
| Submarine + Cruiser (same zone) | +15% attack both | Surface + subsurface coordination |
| Mech Infantry + Tank (same tile) | +20% speed bonus | Mechanized assault |
| **Infantry + Artillery + Tank (all three)** | **+25% ALL stats** | **Full combined arms** |

**Cap:** Maximum +40% from all combined arms sources combined.

**Design intent:** 3 tanks + 3 infantry + 2 artillery fights better than 8 tanks. Mixed armies are stronger but harder to coordinate (move at slowest unit speed). Rewards thoughtful composition over unit spam.

### Part 29: War Weariness

Nations accumulate War Weariness (WW, 0-100) during conflict:

**WW Increases:**
| Event | Change |
|-------|--------|
| Per tick at war | +2 |
| Per additional simultaneous war | +1 |
| Friendly unit destroyed | +5 |
| City captured by enemy | +10 |
| Nuclear weapon detonated (anywhere in world) | +20 |

**WW Decreases:**
| Event | Change |
|-------|--------|
| Per tick at peace | -3 |
| Capture enemy city | -5 (victory euphoria) |
| Enemy nation surrenders | -10 |

**WW Effects:**
| War Weariness | Effects |
|---------------|---------|
| 0-25 | None. Public supports the war. |
| 26-50 | -10% production speed, -5 morale to all armies |
| 51-75 | -20% production, -10 morale, protest interrupts fire |
| 76-100 | -30% production, -20 morale, military coup risk (CRITICAL interrupt) |

**AI behavior:** AI nations actively seek peace when WW > 75. Prevents eternal wars.

**Crisis integration:** WW > 50 triggers "Anti-War Protests" interrupt (URGENT, 60s):
1. Crack down (stability -10, WW -5)
2. Address concerns (military spending -5%, WW -15)
3. Offer ceasefire (opens peace negotiation, WW -20)

### Part 30: How Systems Interconnect

These aren't isolated — they chain into emergent gameplay:

1. **Assassination → Morale collapse → Mass retreat → Easy siege**
   Kill the enemy leader, their army morale drops -30, units rout, cities fall.

2. **Sub blockade → Supply cut → Attrition → Win without fighting**
   Strangle supply lines via convoy raiding, enemy vehicles freeze, troops starve.

3. **Bomber raids → Factory disabled → No reinforcements + Nuke deterrence prevents retaliation**
   Air superiority lets you dismantle their war machine while nukes keep them from escalating.

4. **War weariness → Protest interrupts → Stability drops → Military weakens → Enemy exploits opening**
   Long wars destroy you from within before the enemy army does.

5. **Decoys + intelligence depth → Enemy wastes resources → Real attack elsewhere**
   Intelligence warfare as force multiplier for small nations.

6. **DEFCON rises → Economy penalty → Must choose military or economy → Deterrence shifts AI to covert ops**
   The nuclear shadow shapes all decisions.

7. **Winter invasion → Supply strain → Morale crash → Guerrilla defenders shred you**
   Timing and terrain matter as much as raw numbers.

8. **Combined arms + veteran units + good general → Small force beats large one**
   Quality over quantity is a viable strategy for small nations.

Every decision ripples across multiple systems. This creates emergent storytelling.

---

## Fog of War & Intelligence

Three states for all foreign data:

| State | Meaning | Visual |
|-------|---------|--------|
| UNKNOWN | No information. "???" displayed. | Dark/black fog on map. |
| ESTIMATED | Rough guess, could be wrong. Confidence percentage shown. | Dim fog, unit silhouettes. |
| KNOWN | Verified by spies or direct observation. Accurate. | Clear visibility. |
| **DECEIVED** | Enemy counter-intelligence fed you false data. Looks like KNOWN but is WRONG. | **No visual indicator** — that's the point. |

### What You See
- **Your territory:** Always clear, full info
- **Adjacent tiles:** Clear terrain, can see armies (but not composition)
- **Unit vision radius:** Each army reveals tiles around it (radius based on unit types, Recon Plane = huge radius)
- **Radar Stations:** Reveal 15-tile radius
- **Everything else:** UNKNOWN unless revealed by spies

### Spy Networks
- Deploy agents to target nations from Intelligence HQ
- Network depth builds over time:

| Depth | Time to Reach | What You Learn |
|-------|--------------|----------------|
| 0 (None) | — | Nothing. UNKNOWN for all data. |
| 1 (Shallow) | 5 ticks | Treasury range, army COUNT (not composition), active wars |
| 2 (Moderate) | 10 ticks | Army composition estimates, facility locations, diplomatic agreements |
| 3 (Deep) | 15 ticks | Exact army data, production queues, leader intentions, nuke status |
| 4 (Embedded) | 25 ticks | Sabotage capability, assassination prep, policy leaks |
| 5 (Compromised) | 40 ticks | Full control — can trigger false flag, feed false intel to them |

- Each level costs more to maintain ($50M, $100M, $200M, $400M, $800M per tick)
- Counter-intelligence: enemy spies can detect your network. If caught:
  - Network depth resets to 0
  - Diplomatic relations -20
  - Risk of DECEIVED state: enemy feeds you false data that looks like Depth 3 intel

### The DECEIVED State
The most dangerous mechanic in the game:
- Enemy counter-intelligence feeds your spies false information
- You see confident data (looks like KNOWN) that is completely fabricated
- No UI indicator that you're being deceived
- False troop counts, fake facility locations, wrong diplomatic info
- Only way to detect: cross-reference with Recon Plane observations, or reach Depth 5 in their network
- Player must learn to question their own intel

---

## Campaign Scenarios & Sandbox Mode

### Sandbox Mode
- Generate a procedural world (or load a preset map)
- Pick ANY of the 13 nations to play
- All systems active from the start
- Choose your win condition (or play open-ended)
- Adjustable difficulty: AI aggression, starting resources, interrupt timer length
- Seed-based generation for shareable worlds

### Campaign Mode — 5 Scenarios

Each scenario has unique starting conditions, narrative, objectives, and constraints.

#### Scenario 1: FIRST FIRE
> *"You built the bomb. Now everyone knows."*

- **Play as:** Selvara (small nation, 4 provinces)
- **Starting condition:** World's first nuclear warhead. No delivery system. All large nations aware.
- **Objective:** Survive 100 ticks without being conquered. Build a credible nuclear deterrent.
- **Special rules:** United States Alliance and Volkren Collective both demand you disarm. Refuse and face economic sanctions. Accept and lose your only leverage.
- **Key interrupts:** "Disarmament ultimatum" (45s), "Spy caught in your nuke facility" (30s), "Alliance offers protection in exchange for warhead" (60s)
- **Win:** Still independent at tick 100 with nuclear deterrent intact (Deterrence Score > 100)
- **Lose:** Conquered, disarmed by force, or accepted a deal that makes you a puppet state

#### Scenario 2: ASH AND BLOOD
> *"The United States Alliance has declared war. You have 3 minutes."*

- **Play as:** Selvara
- **Starting condition:** USA declares war at tick 0. Their army is 10x your size. Your allies are weak.
- **Objective:** Survive the invasion and force a peace deal.
- **Special rules:** Other large nations are neutral but can be diplomatically pulled in. Guerrilla Warfare doctrine available from start. Mountains on your border provide defensive advantage.
- **Key interrupts:** "Enemy fleet spotted — 45 seconds to scramble defense" (45s), "Civilian evacuation — which city to abandon?" (30s), "Foreign nation offers alliance if you cede 1 province" (60s)
- **Win:** Force USA to accept ceasefire (their War Weariness > 75 or you capture 1 of their cities)
- **Lose:** All 4 provinces captured

#### Scenario 3: THE SIEGE
> *"They've surrounded you. No trade. No allies. Just walls and will."*

- **Play as:** Ironmarch Remnant (mountain fortress nation)
- **Starting condition:** 3 large nations have declared embargo. No trade routes. Limited resources.
- **Objective:** Break the siege through any means — military breakout, diplomatic wedge, or covert ops.
- **Special rules:** Fortress Defense doctrine at start. Rich in Iron but no Oil or Electronics. Must capture or trade for them.
- **Key interrupts:** "Food reserves critical — ration or risk unrest?" (60s), "Enemy offers surrender terms" (45s), "Underground tunnel to smuggle resources discovered" (30s)
- **Win:** Re-establish 3+ trade routes and break embargo from at least 2 nations
- **Lose:** Stability reaches 0, or starvation (Food at 0 for 10 ticks)

#### Scenario 4: DARK WINTER
> *"Nuclear winter. Half the world is dead. Now it gets hard."*

- **Play as:** Any surviving nation (player picks from 3 options)
- **Starting condition:** Nuclear war already happened. 4 nations destroyed. Fallout zones cover 30% of map. Remaining nations scrambled.
- **Objective:** Rebuild civilization. Establish the largest functioning state.
- **Special rules:** Radiation tiles damage units. Food scarce. Manpower critically low. Electronics production halved. Surviving nations desperate — diplomacy volatile.
- **Key interrupts:** "Radiation spreading toward your capital — evacuate?" (45s), "Refugees from destroyed nation — accept or turn away?" (60s), "Warlord controls the last Uranium deposit — negotiate or fight?" (45s)
- **Win:** Control 5+ functioning cities and have positive food/resource balance for 20 consecutive ticks
- **Lose:** Last city falls or population drops to 0

#### Scenario 5: SECOND SUN
> *"Every nation has the bomb. Only you can stop what comes next."*

- **Play as:** Meridian Confederation (economic powerhouse)
- **Starting condition:** All 6 large nations have nuclear weapons. DEFCON at 3. Arms race spiraling.
- **Objective:** Prevent nuclear war AND achieve economic dominance.
- **Special rules:** Economic leverage is your weapon. Trade embargoes, sanctions, market manipulation. Military is secondary. Must balance deterrence with de-escalation.
- **Key interrupts:** "DEFCON rising — emergency summit?" (30s), "Nation threatens first strike — intervene?" (20s), "Arms control treaty proposed — sign?" (60s), "Black market nuke sale detected" (45s)
- **Win:** DEFCON returns to 5, you control 50% of global GDP, and no nukes were launched
- **Lose:** Any nuclear weapon detonates, or your GDP drops below 20% of global total

---

## Build Milestones — The Actual Plan

Each milestone ends with a testable build you can hand to someone and say "play this."

### MILESTONE 1: The Clock Ticks
**Goal:** Convert from turn-based to real-time.

**Build:**
- `SimulationClock.cs` — New Autoload Node. Runs simulation pipeline on a timer. Adjustable speed (1x/2x/5x/10x). SPACE toggles pause.
- Convert `EndTurnButton` → speed control bar
- All existing engines fire on clock ticks instead of button presses
- HUD shows elapsed time + current speed

**New files:** `src/Core/SimulationClock.cs`
**Modified:** `Core/TurnEngine.cs` (becomes tick-driven), `UI/HUD/TopBar.cs`, `project.godot` (new autoload)

**Test:** Launch → time advances → economy ticks → AI moves → crises fire on their own → pause/unpause works → speed changes work.

---

### MILESTONE 2: The Phone Rings
**Goal:** Interrupt system with countdown timers.

**Build:**
- `InterruptEngine.cs` — Pure C# engine. Evaluates trigger conditions each tick.
- `InterruptPanel.cs` — UI panel with slide-in, countdown bar, choice buttons.
- `InterruptQueue` — Priority queue in WorldStateManager.
- Absorb/replace `CrisisEngine.cs`

**New files:** `src/Engines/InterruptEngine.cs`, `src/UI/Panels/InterruptPanel.cs`
**Modified:** `Data/Models.cs` (InterruptData), `Events/GameEvents.cs` (InterruptTriggeredEvent, InterruptResolvedEvent)

**Starter interrupts:** 6 hand-crafted interrupts covering CRITICAL/URGENT/ROUTINE priorities.

**Test:** Play 2 min → interrupts fire → countdown visible → picking choice applies effects → ignoring applies default → CRITICAL auto-pauses sim.

---

### MILESTONE 3: 13 Nations Live
**Goal:** Update world generation for 13 fictional nations with full data.

**Build:**
- Update `WorldGenerator.cs` — 6 large nations (8-12 cities, 5+ armies) + 7 small nations (2-5 cities, 1-3 armies)
- Named nations with archetypes (see nation table above)
- Each nation gets starting resources, treasury, military composition per archetype
- Terrain-driven placement: coastal nations near coast, mountain nations in mountains, etc.
- Player picks from any of 13 nations in setup (Selvara is default)

**Modified:** `World/WorldGenerator.cs`, `Data/Models.cs` (NationTier enum, resource fields), `UI/Menus/CharacterSetupPanel.cs`

**Test:** Generate world → 13 nations visible with correct placement → borders/territory look right → each nation has appropriate starting military → player can pick any nation.

---

### MILESTONE 4: The World Breathes
**Goal:** AI nations act continuously. The world plays itself.

**Build:**
- Expand `AIEngine.cs` — AI nations move units, start wars, form alliances, trade, build facilities every tick
- `DiplomacyData` — Relations matrix (war, alliance, trade, embargo, neutral) between all 13 nations
- `NewsEngine.cs` — Aggregates events into scrolling news feed
- Map shows AI movement, trade convoys, border shifts in real-time

**Modified:** `Engines/AIEngine.cs`, `Data/Models.cs` (DiplomacyData, RelationType), `Events/GameEvents.cs` (WarDeclaredEvent, AllianceFormedEvent, TradeAgreementEvent)

**Test:** Launch → don't touch anything → watch AI go to war, sign deals, move armies → news feed reports it → world plays itself.

---

### MILESTONE 5: Your Hands on the Wheel
**Goal:** Full player interaction in real-time.

**Build:**
- **Council system (DONE):** Government body changes by nation archetype (National Assembly, Royal Court, Politburo, etc.). 5 advisers per nation (Military, Economic, Intelligence, Diplomatic + specialist). Council actions: Domestic, Military, Diplomatic, Intelligence — available actions vary by government type.
  - `src/UI/Panels/CouncilPanel.cs` — full-screen council overlay, toggle with C key
  - `src/Data/Models.cs` — GovernmentType, AdviserData, CouncilData
- **Combat command (DONE):** Per-army tactical orders (Defend/Patrol/Stage/Attack/Retreat), formation selection (Column/Spread/Wedge/Circle), army list with status, composition view, battle log.
  - `src/UI/Panels/CombatCommandPanel.cs` — replaces old MilitaryCommandPanel
- **Council-aware sidebar (DONE):** LeftSidebar shows government name, quick actions, government-specific special action.
- **Budget panel:** Allocate spending across Military / Infrastructure / Intelligence / Diplomacy. Changes take effect over time.
- **Diplomacy panel:** Click nation → Propose alliance, Declare war, Offer trade, Demand tribute, Embargo.
- **Production panel:** Select city → view facilities → queue construction.
- **Resource display:** Show all 6 resource stockpiles + income/consumption rates.
- **Council engine:** Wire `CouncilActionEvent` to actual game state changes (tax rate → treasury, martial law → stability, etc.)

**New files:** `src/UI/Panels/CouncilPanel.cs` (DONE), `src/UI/Panels/CombatCommandPanel.cs` (DONE), `src/UI/Panels/BudgetPanel.cs`, `src/UI/Panels/DiplomacyPanel.cs`, `src/UI/Panels/ProductionPanel.cs`
**Modified:** `UI/Map/MapManager.cs` (army selection/ordering), `Engines/ResourceEngine.cs`, `Engines/EconomicEngine.cs`

**Test:** Full gameplay loop — manage nation in real-time, issue military orders, adjust budget, do diplomacy, build units, respond to interrupts. Playable for 10+ minutes.

---

### MILESTONE 6: The Fog
**Goal:** Intelligence is unreliable. The map lies to you.

**Build:**
- `IntelEngine.cs` — Manages UNKNOWN/ESTIMATED/KNOWN/DECEIVED states for all foreign data.
- Fog of War rendering layer — player territory clear, spy networks reveal areas, everything else dark.
- Spy network system: deploy agents, build depth over ticks, costs money per level.
- Counter-intelligence: enemy detects spies, feeds false data (DECEIVED state).
- Decoy units, dummy facilities, radio deception (Part 26).
- Recon Plane fog reveal.

**New files:** `src/Engines/IntelEngine.cs`, `src/UI/Map/FogOfWarRenderer.cs`
**Modified:** `Data/Models.cs` (IntelState enum, SpyNetwork data), `UI/Map/TerritoryBorderRenderer.cs` (fog overlay)

**Test:** Foreign nations show "???" → deploy spies → gradually see data → some turns out wrong → get deceived at least once → Recon Planes reveal map areas.

---

### MILESTONE 7: Full Combat
**Goal:** Complete military system with all 30 parts implemented.

**Build (in sub-milestones):**

**7a: Production Pipeline**
- 6 strategic resources on provinces
- Facility construction in cities (build slots)
- Unit production from facilities (16 unit types)
- Upkeep, fuel, supply range, manpower

**7b: Core Combat**
- Combat formula (Part 11)
- Retreat & routing (Part 12)
- Ranged combat — artillery/cruiser range attacks (Part 13)
- Morale system (Part 18)

**7c: Air & Naval**
- Air superiority zones — fighters, bombers, carrier operations (Part 14)
- Naval engagement — surface/subsurface/anti-sub (Part 15)
- Strategic bombing — facility destruction (Part 16)
- Convoy/trade raiding — subs vs destroyers (Part 25)

**7d: Advanced Combat**
- Combined arms bonuses (Part 28)
- Veterancy system (Part 17)
- Supply chains and attrition (Part 19)
- City sieges — encirclement, bombardment, starvation, assault (Part 20)
- Weather & seasons (Part 21)
- Combat doctrine — 6 national doctrines (Part 22)

**7e: Nuclear**
- DEFCON system (Part 23)
- Nuclear deterrence AI (Part 24)
- Nuclear escalation ladder (Part 10)
- Warhead production, delivery, launch, impact, fallout, MAD

**7f: Commanders**
- General assignment to formations (Part 27)
- Command bonuses, special abilities by role
- General death consequences
- Player character leading formations (risky)

**Modified:** Almost everything. `Engines/MilitaryEngine.cs` becomes the largest engine. `Data/Models.cs` gets resource/facility/production data. New combat resolution pipeline.

**Test:** Build armies → research and produce units → assign formations → fight battles → see morale/routing/supply effects → air superiority matters → naval combat works → nukes change everything.

---

### MILESTONE 8: Campaigns
**Goal:** 5 playable campaign scenarios + sandbox mode.

**Build:**
- `ScenarioLoader.cs` — Loads pre-configured world states for each scenario
- `ScenarioData/` — JSON files defining each scenario's starting conditions, objectives, special rules
- Campaign selection screen in main menu
- Scenario-specific win/loss condition checking
- Sandbox mode: world generation + nation picker + difficulty settings

**New files:** `src/Core/ScenarioLoader.cs`, `src/Data/ScenarioData/*.json`, `src/UI/Screens/CampaignScreen.cs`
**Modified:** `UI/Menus/MainMenu.cs`, `Engines/VictoryEngine.cs`

**Test:** Pick campaign → scenario loads with correct conditions → play through → hit win or lose condition → scenario ends with result screen. Sandbox generates random world and plays freely.

---

### MILESTONE 9: Save/Load
**Goal:** Save and load game state. Autosave. Multiple slots.

**Build:**
- `SaveSystem.cs` — Serializes entire WorldData (clock state, interrupt queue, diplomacy, fog, everything) to JSON via Newtonsoft.Json
- Autosave every N real-time minutes (configurable)
- Save slots (main menu + pause menu)
- Includes RNG seed + tick count for deterministic replay
- Scenario progress persistence

**New files:** `src/Core/SaveSystem.cs`, `src/UI/Panels/SaveLoadPanel.cs`

**Test:** Play 5 min → save → keep playing → load → state identical. Autosave works. Multiple slots.

---

### MILESTONE 10: Ship It
**Goal:** Polish, balance, and package.

**Build:**
- **Balance pass:** Tune interrupt frequencies, timer durations, AI aggression, economy rates, combat numbers
- **Sound:** Phone ring SFX for interrupts. Ambient music shifts with DEFCON. Battle sounds. UI clicks.
- **Visual polish:** Screen shake on explosions. Flash on battles. Panel transitions. DEFCON color tinting.
- **Tutorial:** First scenario acts as tutorial — guided interrupts teach mechanics
- **Settings:** Volume, speed default, interrupt timer difficulty (longer = easier)
- **Export:** Godot export for Windows/Mac/Linux

**Test:** Hand build to someone who's never seen it. They can start, understand, play 20 min, experience interrupts, manage nation, win or lose. No crashes. No softlocks.

---

## Priority Order

```
M1  The Clock Ticks       ← Real-time foundation. Everything builds on this.
M2  The Phone Rings       ← The signature mechanic. This IS the game.
M3  13 Nations Live       ← The fictional world with all nations.
M4  The World Breathes    ← AI makes the world alive.
M5  Your Hands on Wheel   ← Player agency. Now it's a game.
M6  The Fog               ← Unreliable intel. Now it's a GOOD game.
M7  Full Combat           ← All 30 military parts. Now it's DEEP.
M8  Campaigns             ← Scenarios + sandbox. Now it has CONTENT.
M9  Save/Load             ← Quality of life.
M10 Ship It               ← Polish and distribute.
```

---

## Documents This Replaces

All previous planning documents are moved to `docs/Obsolete Ideas/`:
- `PHASE_PLAN.md` — Original 30-phase procedural plan (already archived)
- `FULL_AUTHORITY_DESIGN.md` — Authority meter system (replaced by concrete systems)
- `GAME_FLOW.md` — Player experience spec (incorporated here, UK/bloc references removed)
- `GAME_CHARACTERS.md` — Character/scenario spec (incorporated here, adapted for fictional world)

The ONLY current planning document is this file: `docs/ROADMAP.md`

`docs/MILITARY_SYSTEM.md` is retained as the detailed military reference — all 30 parts are summarized here but the full doc has additional detail.

---

*This is THE plan. One document. No simplifying. Every system from every doc included.*
*Last updated: 2026-03-28*
