# FULL AUTHORITY — Military Command & Production System
## Design Specification

> **Created:** 2026-03-25
> **Status:** Design — not yet implemented

---

## Overview

The military system has two halves:
1. **Command** — selecting units, issuing orders, planning operations
2. **Production** — gathering resources, building facilities, manufacturing units

---

## PART 1: RESOURCES

Six strategic resources. Provinces produce them based on terrain.

| Resource | Source Terrain | Used For |
|----------|--------------|----------|
| Iron | Hills, Mountains | Steel → Vehicles, Ships, Buildings |
| Oil | Sand, specific provinces | Fuel → All mechanized units |
| Uranium | Rare (2-3 tiles worldwide) | Nuclear weapons |
| Electronics | Cities (size 2+) | Radar, Missiles, Advanced units |
| Manpower | All provinces (scales w/ pop) | Infantry, crews for everything |
| Food | Grass, Farms | Keeps manpower flowing, prevents unrest |

### UK Starting Resources
- Iron: LOW (Wales/Scotland)
- Oil: LOW (North Sea — vulnerable)
- Uranium: MINIMAL (1 warhead, that's it)
- Electronics: GOOD (London tech sector)
- Manpower: MODERATE (2M people)
- Food: MODERATE (English farmland)

Key tension: You CANNOT build a full war machine alone. You need trade or conquest.

---

## PART 2: FACILITIES

Built in cities. Each city has limited build slots based on size.

| City Size | Slots |
|-----------|-------|
| Town (1) | 2 |
| City (2) | 4 |
| Capital (3) | 6 |

### Resource Processing Facilities
| Facility | Cost | Build Time | Effect |
|----------|------|------------|--------|
| Steel Mill | $200M + 50 Iron | 3 turns | Iron → Steel |
| Oil Refinery | $300M + 30 Iron | 4 turns | Crude Oil → Fuel |
| Electronics Lab | $250M + 20 Elec | 3 turns | Advanced Components |

### Military Production Facilities
| Facility | Cost | Build Time | Produces |
|----------|------|------------|----------|
| Barracks | $100M | 2 turns | Infantry, Mech Infantry |
| Tank Factory | $400M + 80 Steel | 5 turns | Tanks, Artillery, Anti-Air |
| Shipyard | $500M + 100 Steel | 6 turns | All naval units |
| Airfield | $350M + 50 Steel | 4 turns | Fighters, Bombers, Recon |
| Missile Silo | $800M + 50 Uranium + 30 Elec | 8 turns | Nuclear weapons |

### Support Facilities
| Facility | Cost | Build Time | Effect |
|----------|------|------------|--------|
| Fuel Depot | $150M | 2 turns | +3 tile supply range |
| Radar Station | $200M + 10 Elec | 3 turns | 15-tile fog reveal |
| Military Hospital | $150M | 2 turns | Units heal 20%/turn |
| Fortress | $300M + 60 Steel | 4 turns | City defense +100% |
| Intelligence HQ | $400M + 20 Elec | 5 turns | Enables spy ops |

---

## PART 3: UNIT ROSTER

### Land Units
| Unit | Facility | Cost | Resources | Build Time | Special |
|------|----------|------|-----------|------------|---------|
| Infantry | Barracks | $50M | 10 MP | 1 turn | Garrison cities, ambush in forests |
| Mech Infantry | Barracks | $100M | 10 MP, 5 Steel | 2 turns | Can be airlifted |
| Tank | Tank Factory | $200M | 20 Steel, 10 Fuel | 3 turns | Fast on flat, weak in mountains |
| Artillery | Tank Factory | $150M | 15 Steel | 2 turns | 2-tile range, can't move+fire |
| Anti-Air | Tank Factory | $180M | 15 Steel, 5 Elec | 2 turns | Shoots down aircraft |
| Mobile Radar | Tank Factory | $120M | 10 Elec | 2 turns | Huge fog reveal radius |

### Naval Units
| Unit | Facility | Cost | Resources | Build Time | Special |
|------|----------|------|-----------|------------|---------|
| Destroyer | Shipyard | $300M | 30 Steel, 10 Fuel | 3 turns | Anti-sub, escort |
| Cruiser | Shipyard | $500M | 50 Steel, 15 Fuel | 5 turns | Shore bombardment |
| Submarine | Shipyard | $400M | 40 Steel, 10 Elec | 4 turns | Invisible until attacking |
| Carrier | Shipyard | $800M | 80 Steel, 30 Fuel, 15 Elec | 8 turns | Carries 4 fighters |
| Transport | Shipyard | $200M | 20 Steel, 5 Fuel | 2 turns | Carries 8 land units |

### Air Units
| Unit | Facility | Cost | Resources | Build Time | Special |
|------|----------|------|-----------|------------|---------|
| Fighter | Airfield | $250M | 15 Steel, 10 Fuel | 2 turns | Air superiority |
| Bomber | Airfield | $400M | 25 Steel, 20 Fuel | 4 turns | Devastating vs ground |
| Recon Plane | Airfield | $150M | 10 Steel, 5 Elec | 2 turns | Reveals fog along flight path |

### Special
| Unit | Facility | Cost | Resources | Build Time |
|------|----------|------|-----------|------------|
| Nuclear Warhead | Missile Silo | $2,000M | 100 Uranium, 50 Elec | 12 turns |

---

## PART 4: UPKEEP & SUPPLY

Every unit costs money and fuel per turn.

| Unit | Upkeep/turn | Fuel/turn |
|------|------------|-----------|
| Infantry | $5M | 0 |
| Tank | $20M | 3 |
| Fighter | $25M | 5 |
| Carrier | $50M | 8 |
| Nuke (stored) | $30M | 0 |

### Failure states:
- Treasury $0: Units lose 5% strength/turn
- Fuel empty: Vehicles can't move (sitting ducks)
- Food empty: Infantry desert (lose 1/turn)
- 5 turns at -$5000M: Game over

---

## PART 5: MANPOWER

Population of 2M generates a finite manpower pool.

- Each unit type costs manpower (Infantry=1000, Tank crew=500, Carrier=3000)
- Factories need workers (50-500 per facility)
- Pulling too many civilians tanks stability
- Conscription: doubles pool for 10 turns, but -30 stability, -40% factory output
- Casualties permanently reduce pool
- Conquering cities adds population over time

---

## PART 6: ORDERS SYSTEM

### Movement Orders
| Order | Hotkey | Effect |
|-------|--------|--------|
| Move | Right-click | Pathfind to destination, avoid combat |
| Attack-Move | A + right-click | Move toward target, engage enemies en route |
| Patrol | P + right-click | Loop between current pos and target |
| Waypoints | Shift+right-click | Queue multiple destinations |

### Combat Orders
| Order | Hotkey | Effect |
|-------|--------|--------|
| Attack Target | Right-click enemy | Destroy specific target |
| Bombardment | B + right-click | Artillery/cruisers fire from range |
| Intercept | I + right-click | Fighters patrol airspace |

### Defensive Orders
| Order | Hotkey | Effect |
|-------|--------|--------|
| Hold | H | Stay put, engage in range |
| Garrison | G + click city | Enter city, boost defense |
| Fortify | F | Dig in, +50% defense |
| Blockade | Right-click sea zone | Block trade, intercept transports |

### Special Orders
| Order | How | Effect |
|-------|-----|--------|
| Escort | E + right-click friendly | Follow and protect |
| Shore Bombardment | B + right-click coast | Soften before amphibious assault |
| Airlift | Right-click distant airfield | Instant transport (needs air superiority) |
| Amphibious Assault | Load transport → right-click coast | Beach landing at -30% strength |
| Nuclear Strike | Nuke panel → select → confirm x2 | City destroyed. World reacts. |

---

## PART 7: FORMATIONS

- Ctrl+1-9 saves selected units as numbered group
- Press 1-9 to recall group
- Groups are named ("3rd Fleet", "Channel Wolves")
- Formation Panel in sidebar shows all groups
- Units in a formation receive same orders but pathfind individually

---

## PART 8: COMBAT MODIFIERS

| Factor | Effect |
|--------|--------|
| Forest defense | +30% |
| Hills defense | +20% |
| Mountain defense | +50% |
| City defense | +40% |
| Fortified | +50% |
| Leader authority | Territory Authority scales effectiveness |
| Supply lines | >5 tiles from city = -10% strength/turn |
| Encirclement | Attacker on 3+ sides = defender -25% |
| Amphibious | -30% on landing turn |

---

## PART 9: UPGRADES

| Upgrade | Requires | Cost | Effect |
|---------|----------|------|--------|
| Elite Training | Turn 20+ | $300M | Infantry +25%, unlock Special Forces |
| Advanced Armor | Electronics Lab | $500M | Tanks +20% defense, unlock Heavy Tanks |
| Nuclear Sub Bay | Missile Silo + $1B | $800M | Nuclear Submarines |
| Stealth Hangar | Elec Lab + Turn 30+ | $600M | Stealth Bomber |
| Automated Mill | Electronics Lab | $400M | Steel output +50% |
| Synthetic Fuel | Electronics Lab | $500M | Fuel from coal (no oil needed) |

### Nuclear Escalation Path
1. Start with 1 warhead (story)
2. Build Uranium Mine ($300M, rare tile, 5 turns)
3. Build Nuclear Lab ($600M, 6 turns)
4. Build Missile Silo ($800M, 8 turns)
5. Produce warheads ($2B each, 12 turns each)
6. Late game: Nuclear Sub Bay → mobile hidden launch

Each warhead built escalates global DEFCON.

---

## PART 10: POWER TIMELINE

| Turns | Phase | Your Military |
|-------|-------|--------------|
| 1-5 | Scramble | 1 nuke, handful of infantry, 2 tanks |
| 6-15 | Foundation | First barracks + factory. Steady trickle. |
| 16-30 | Buildup | 3-4 facilities, 30-40 units. First real choices. |
| 31-50 | Regional | Upgraded facilities, 60-80 units. Can project force. |
| 51-80 | Major Power | 100+ units. Carriers, bombers, the works. |
| 81-120 | Superpower | Multiple nukes. Stealth bombers. Nuclear subs. |
| 121-200 | Endgame | World domination or nuclear standoff. |
