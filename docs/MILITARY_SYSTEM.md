# WARSHIP — Military Command & Production System
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
| Leader prestige | Nation Prestige scales effectiveness |
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

---

## PART 11: UNIT STATS & COMBAT FORMULA

Replace binary alive/dead with granular stats. Each unit has HP (0-100), BaseAttack, BaseDefense, Speed, Range, and Domain.

### Unit Stat Table

| Unit | HP | Atk | Def | Speed | Range | Domain |
|------|-----|-----|-----|-------|-------|--------|
| Infantry | 100 | 15 | 20 | 2 | 0 | Land |
| Mech Infantry | 100 | 20 | 22 | 3 | 0 | Land |
| Tank | 100 | 40 | 30 | 4 | 0 | Land |
| Artillery | 80 | 35 | 8 | 1 | 2 | Land |
| Anti-Air | 80 | 10/45 | 15 | 2 | 1 | Land |
| Mobile Radar | 40 | 0 | 5 | 3 | 0 | Land |
| Destroyer | 100 | 30 | 25 | 5 | 1 | Sea |
| Cruiser | 120 | 45 | 35 | 4 | 2 | Sea |
| Submarine | 80 | 50 | 15 | 4 | 1 | Sea |
| Carrier | 150 | 10 | 40 | 3 | 0 | Sea |
| Transport | 80 | 0 | 10 | 4 | 0 | Sea |
| Fighter | 80 | 35 | 20 | 12 | 0 | Air |
| Bomber | 100 | 55 | 10 | 8 | 0 | Air |
| Recon Plane | 50 | 5 | 5 | 14 | 0 | Air |
| Nuclear Warhead | 1 | 999 | 0 | 6 | 0 | Special |

Anti-Air: 10 Atk vs ground, 45 Atk vs air units.

### Combat Resolution Formula

```
Damage = (AttackerAtk * AttackerHP/100) * CombinedModifier / (DefenderDef * DefenderHP/100 + 1)
```

Both sides take damage simultaneously. Attacker takes counter-damage at 50% rate. CombinedModifier is the product of all applicable bonuses (terrain, morale, experience, combined arms, doctrine, weather, supply, encirclement).

This replaces the RNG coin-flip in MilitaryEngine.cs.

---

## PART 12: RETREAT & ROUTING

Units retreat instead of dying when:
- HP drops below 30 → **Shattered** state
- Morale drops below 20 → **Routed** state

### Retreat Rules
- Unit stops fighting, moves 2 tiles toward nearest friendly city
- Retreating units have -50% defense (vulnerable during withdrawal)
- If no retreat path (encircled) → unit surrenders (destroyed)
- Recovery: 5 HP/turn in supply, 10 HP/turn in city with Military Hospital

### Player Decision
Chase retreating enemies (risk overextension) or consolidate? Encircle to prevent retreat (costlier but decisive)?

---

## PART 13: RANGED COMBAT PHASE

Combat resolves in two phases per tick:

**Phase 1 — Ranged:** Units with Range > 0 (Artillery, Cruisers, Anti-Air) fire at enemies within range. Artillery cannot move+fire same turn. Targets chosen by lowest HP first (focus fire). Artillery behind your front line shoots over friendlies.

**Phase 2 — Melee:** Adjacent units exchange damage using the combat formula.

### Artillery Mechanics
- Devastating from range, nearly defenseless up close (Def=8)
- Counter-battery: enemy artillery can target your artillery if in range
- Multiple artillery coordinating = concentrated fire (damage stacks)
- Bonus vs fortified positions: +25% siege damage

### Shore Bombardment
Cruisers use Range 2 to hit coastal tiles before amphibious landings — this is the pre-assault softening.

---

## PART 14: AIR SUPERIORITY ZONES

Fighters on **Intercept** order create an Air Superiority Zone — 3-tile radius circle.

### Effects
- Enemy bombers entering the zone get intercepted (fighters +50% Atk vs bombers)
- Enemy transports cannot Airlift into contested zones
- Friendly ground units in your air zone get +10% defense (Close Air Support)
- Contested zones: fighters fight each other first, winner controls zone

### Sortie Fuel
Aircraft operate 4 turns away from an Airfield or Carrier, then must return to rebase or crash. Prevents infinite air coverage. Carriers become crucial for projecting air power over oceans.

### Player Decision
Limited fighters — cover your capital (defense) or push air cover forward with invasion? Carriers extend range but are expensive and vulnerable without escorts.

---

## PART 15: NAVAL ENGAGEMENT DEPTH

### Submarine Warfare
- Submarines are **invisible** until they attack
- Destroyers have **Detect** ability (range 2 tiles) — reveals subs
- Without destroyer escort, fleets are vulnerable to sub ambush
- Subs excel at convoy raiding (see Part 25)

### Capital Ships
- Cruisers: shore bombardment (Range 2 vs coastal tiles)
- Carriers: mobile airfield, carry 4 fighters, don't fight directly
- Transports: carry 8 land units — if sunk, ALL loaded units lost

### Sea Zone Control
Ocean divided into named Sea Zones (English Channel, North Atlantic, Indian Ocean, etc.). Nation with most naval strength controls the zone. Control required for:
- Trade route income through that zone
- Safe transport passage
- Blockade enforcement

### Player Decision
UK is surrounded by water — Channel and North Sea control is existential. Submarines (cheap, invisible, great raiders) vs surface fleet (expensive, visible, controls zones)?

---

## PART 16: STRATEGIC BOMBING

Bombers with **Strategic Bombing** order target enemy facilities, not just units.

### Bombing Mechanics
1. Bomber must enter enemy air zone (risk interception)
2. If bomber survives, deals 25-40 damage to target facility
3. Facilities have HP = 100. At 0 HP → **disabled** (stops producing)
4. Repair: costs resources + 2 turns. Facilities never permanently destroyed.
5. Bomber returns to home airfield next turn

### Priority Targets
- **Oil Refinery** → disable fuel = vehicles grounded
- **Tank Factory** → stop armor reinforcements
- **Shipyard** → stop naval production
- **Airfield** → fighters can't rebase here

### Player Decision
Bombers cost $400M and are slow. Bomb oil refineries (cripple mechanized forces) or tank factories (prevent reinforcements)? Both ideal, but you probably only have 2-3 bombers.

---

## PART 17: VETERANCY / EXPERIENCE

Units gain XP from surviving combat. Four tiers:

| Tier | Combats Survived | Bonus |
|------|-----------------|-------|
| Green | 0 | None |
| Regular | 3 | +10% Attack, +10% Defense |
| Veteran | 8 | +20% Atk, +20% Def, +1 Speed |
| Elite | 15 | +30% Atk, +30% Def, +1 Speed, -10% damage taken |

XP gained per combat round survived (1 XP per tick of dealing/receiving damage).

### Player Decision
Your elite 3rd Tank Division has been through 20 battles. Pull them back to recover, or keep them fighting where their bonus matters most? Losing veterans hurts far more than losing fresh recruits.

---

## PART 18: MORALE SYSTEM

Each unit has Morale (0-100). Morale multiplies combat effectiveness:

```
EffectiveAttack = BaseAttack * (Morale / 100)
EffectiveDefense = BaseDefense * (Morale / 100)
```

### Morale Changes

| Event | Change |
|-------|--------|
| Win a combat round | +5 |
| Lose a combat round | -10 |
| Adjacent friendly destroyed | -15 |
| Out of supply | -5/turn |
| Nation leader assassinated | **-30 to ALL units** |
| In city with hospital | +10/turn |
| In friendly territory | +3/turn |
| In enemy territory | -1/turn |
| Garrison in own city | +5/turn |
| Nuclear detonation nearby | -40 |
| War weariness (nation) | -1 to -5/turn |

### Morale Thresholds
- Morale < 50 → **Wavering** (visible status icon)
- Morale < 20 → **Routed** (auto-retreat, see Part 12)

### Key Interaction
Assassinating a rival leader via PoliticalEngine's "eliminate" action causes -30 morale to ALL their units. A covert op can swing an entire war.

---

## PART 19: SUPPLY CHAINS

Replace vague "be near a city" with concrete supply mechanics.

### Supply Sources
Any friendly city with a Fuel Depot OR any city Size 2+ projects supply.

### Supply Range
- 5 tiles from supply source (default)
- Fuel Depot extends by +3 tiles
- Supply traces through friendly/uncontested territory ONLY — cannot pass through enemy tiles

### Out of Supply (cumulative per turn)
| Turns Without | Effect |
|---------------|--------|
| 1 | -5% combat effectiveness |
| 2 | -10% effectiveness, morale drops -5/turn |
| 3 | -20% effectiveness, vehicles cannot move |
| 5+ | -5 HP/turn (starvation/attrition) |

### Supply Interdiction
- Bombing/destroying Fuel Depots cuts supply to dependent units
- Submarine blockades cut maritime supply to island nations
- Cutting supply lines is a valid way to win without fighting

### Implementation
BFS flood-fill each turn from all supply sources across friendly tiles (same pattern as ownership map in WorldGenerator). Each tile gets a supply distance value.

### Player Decision
UK is an island. If your navy loses sea lane control, expeditionary forces on the continent starve. The English Channel is the most important strategic position in the early game.

---

## PART 20: CITY SIEGE SYSTEM

Replace instant 30-pixel capture with multi-turn sieges.

### City HP
| City Size | Base HP | With Fortress |
|-----------|---------|---------------|
| Town (1) | 100 | 300 |
| City (2) | 200 | 400 |
| Capital (3) | 400 | 600 |

### Siege Mechanics
- Garrison units inside add their Defense to city defense
- Siege damage per turn = sum of all adjacent attacker Attack values / 4
- Artillery/Cruiser bombardment = **double** siege damage
- When City HP hits 0 → captured, garrison destroyed/surrenders
- Cities under siege cannot produce units or provide supply

### Siege Modifiers
| Condition | Effect |
|-----------|--------|
| Full encirclement (all sides) | +50% siege damage, no garrison retreat |
| Partial encirclement (3+ sides) | +25% siege damage |
| Starvation (cut from supply 3+ turns) | -10 City HP/turn automatically |

### Player Decision
Assault immediately (fast but costly) or starve them out (slow but cheap)? Fortresses make frontal assault expensive — incentivizes bombing facilities and cutting supply first.

---

## PART 21: WEATHER & SEASONS

Game tracks Month via `TurnNumber % 12 + 1`. Weather is deterministic by month — the player can plan around it.

### Seasonal Effects

| Season | Months | Effects |
|--------|--------|---------|
| Clear | Apr-Sep | No modifier |
| Rain/Mud | Oct-Nov, Mar | -20% vehicle speed, -10% attack all |
| Winter | Dec-Feb | -30% vehicle speed, -2 HP/turn unsheltered, +20% defender bonus |

### Regional Variation
- Northern latitudes (Russia, UK, Scandinavia): harsh winters
- Southern latitudes (India, Southern China): monsoon (same as Rain mechanically)
- Deserts: sandstorm events (reduce vision radius by half)

### Storm Events
10% chance per turn: severe weather in a region. -50% all combat, aircraft grounded for 1 turn.

### Player Decision
Time your invasion for summer for maximum advantage. But your enemy expects that too. Attack in winter when they don't expect it, accepting the penalties?

---

## PART 22: COMBAT DOCTRINE

Each nation picks one of 4 doctrines. Set at nation level. Switching costs 3 turns of "reorganization" (-15% effectiveness during transition).

| Doctrine | Bonus | Penalty | Playstyle |
|----------|-------|---------|-----------|
| **Blitzkrieg** | +30% vehicle speed, +20% first-round attack | -15% defense, -10% infantry | Fast offensive |
| **Fortress** | +30% stationary defense, +50% city defense | -20% attack, -15% speed | Defensive turtle |
| **Guerrilla** | +40% in forests/mountains/urban, stealth move, +30% ambush | -20% open terrain, -15% vehicles | Asymmetric |
| **Combined Arms** | +15% when 3+ types together, +10% all stats | No individual unit type bonus | Balanced |

### Default AI Doctrines
- US (Hegemon): Combined Arms
- China (Commercial): Fortress
- Russia (Revolutionary): Blitzkrieg
- EU (Traditionalist): Fortress
- India (Survival): Guerrilla

### Player Decision
UK's starting position (tiny, defensive, 1 nuke) suggests Fortress or Guerrilla early. Scottish forests + Welsh mountains become kill zones with Guerrilla. But Guerrilla is terrible for overseas invasion — need to switch to Blitzkrieg (3 vulnerable turns).

---

## PART 23: DEFCON SYSTEM

Global DEFCON level (5 to 1), visible on HUD as a prominent gauge.

| Level | Trigger | Effects |
|-------|---------|---------|
| DEFCON 5 (Peace) | Default | Normal gameplay |
| DEFCON 4 (Guarded) | Nuclear facility built OR major war | +10% military spending, -10 diplomacy |
| DEFCON 3 (Elevated) | Warhead produced OR 2+ wars active | +20% unit production speed, -20% trade |
| DEFCON 2 (Severe) | Warhead on delivery platform OR 3+ wars | Full mobilization, civilian economy -30%, AI may preemptive strike |
| DEFCON 1 (Critical) | Nuclear strike authorized | Last chance diplomacy. 5-turn countdown before AI considers MAD acceptable |

### De-escalation
DEFCON only drops (improves) if: no wars for 5 consecutive turns AND no nukes under construction. 1 level per 5 peaceful turns.

### Visual
- HUD clock/gauge shows DEFCON level
- DEFCON 2+: subtle red screen tint
- DEFCON 1: klaxon sound, flashing red border

---

## PART 24: NUCLEAR DETERRENCE

AI nations evaluate a Deterrence Score before military action:

```
DeterrenceScore = (NumWarheads * 50) + (HasNukeSub ? 200 : 0) + (DeliveryRange * 10) + (Prestige * 2)
```

### AI Behavior by Deterrence

| Score | AI Response |
|-------|-------------|
| 0-49 | Ignores nuclear threat. May invade freely. |
| 50-99 | Hesitates. 50% chance to abort invasion. Seeks allies first. |
| 100-199 | Will not invade directly. Uses proxy wars, economic pressure, covert ops. |
| 200+ | Full MAD. Treats you as untouchable. Shifts to diplomatic/economic competition. |

### UK Starting Position
- Deterrence = 50 (1 warhead, no second strike, no sub)
- Superpowers hesitate but don't fully back off
- Getting a nuclear submarine → Deterrence jumps to 250+ (game-changing strategic goal)

### Player Decision
Invest in more warheads (visible, provocative, raises DEFCON) or nuclear submarine (expensive, slow, but invisible second-strike that terrifies AI permanently)?

---

## PART 25: CONVOY / TRADE RAIDING

Trade routes (already visualized as green lines) become targetable.

### Mechanics
- Each trade route generates income for both endpoints
- Routes pass through Sea Zones
- **Submarines on Blockade** in a sea zone: 30% chance/turn to disrupt convoy (reduce route income by 25% for 3 turns)
- **Destroyers on Escort** in same zone: each reduces sub intercept by 15%

### The Battle of the Atlantic
UK trade flows through North Atlantic. Enemy sends subs. You send destroyers to escort. Cat-and-mouse gameplay emerges naturally.

### Player Decision
Submarine warfare is UK's asymmetric advantage. Can't match China's army or Russia's tanks, but 5 subs can strangle overseas trade. If they catch your subs though, you lose expensive assets.

---

## PART 26: DECOY & DECEPTION

Tools to exploit the DECEIVED intel state.

### Decoy Units
- Built at Barracks for $20M + 2 Manpower (cheap)
- Appear identical to real units on enemy intel
- Destroyed in 1 hit — revealed as fake
- Enemy spy network depth determines detection: Depth 0=0% detected, Depth 1=20%, Depth 2=50%, Depth 3=80%

### Radio Deception
- Mobile Radar unit special order
- Creates ghost blips showing fake unit concentrations on enemy radar
- Costs $10M/turn, lasts until Radar destroyed or order cancelled

### Dummy Facilities
- Build "Dummy Airfield" or "Dummy Missile Silo"
- 1/10th real cost, appears real on enemy intel
- Draws enemy bombing raids to waste targets

### Player Decision
10 real tanks + 20 decoys = enemy sees 30 tanks on their border. If they shift defenders to meet your "invasion," your real 10 tanks attack somewhere else. Higher Prestige = more convincing deceptions.

---

## PART 27: GENERALS / COMMANDERS

Characters with military roles can be assigned to formations.

### Command Bonus
- General provides +X% to all units in formation, where X = Nation's Prestige / 5
- TA-60 general = +12% to all stats
- Max 3 generals per nation

### Special Abilities by Role
| Role | Bonus |
|------|-------|
| Defense Minister | +15% defense to assigned units |
| Chief of Staff | +15% attack to assigned units |
| Director of Intelligence | +3 tile vision range for assigned units |

### Risk
- Generals are physically on the map (already in CharacterData)
- If general is killed (assassinated or unit destroyed) → formation loses bonus + -20 morale hit
- Player character can personally command a formation (risky but powerful)

### Player Decision
Director of Intelligence gives amazing vision but low TA (low command bonus). Defense Minister gives great defense but you risk your own character on the front line.

---

## PART 28: COMBINED ARMS BONUSES

Different unit types fighting together get synergy bonuses.

### Synergy Pairs

| Combination | Bonus | Reason |
|-------------|-------|--------|
| Infantry + Tank (same tile) | +15% attack both | Infantry screens, tanks punch |
| Artillery adjacent to friendlies in combat | +20% to engaged friendlies | Fire support |
| Fighter overhead (air zone covers battle) | +10% attack to ground | Close air support |
| Destroyer + Carrier (same zone) | +20% defense to carrier | Escort screen |
| Anti-Air + any ground (same tile) | Negates enemy air bonus | Air denial |
| Submarine + Cruiser (same zone) | +15% attack both | Surface + subsurface |
| Mech Infantry + Tank (same tile) | +20% speed bonus | Mechanized assault |
| **Infantry + Artillery + Tank** (all three) | **+25% ALL stats** | Full combined arms |

Maximum +40% from all combined arms sources combined (cap).

### Player Decision
3 tanks + 3 infantry + 2 artillery fights better than 8 tanks. Mixed armies are stronger but harder to coordinate (move at slowest unit speed). Rewards thoughtful composition over spam.

---

## PART 29: WAR WEARINESS

Nations accumulate War Weariness (WW, 0-100) during conflict.

### WW Increases
| Event | Change |
|-------|--------|
| Per turn at war | +2 |
| Per additional simultaneous war | +1 |
| Friendly unit destroyed | +5 |
| City captured by enemy | +10 |
| Nuclear weapon detonated (anywhere) | +20 |

### WW Decreases
| Event | Change |
|-------|--------|
| Per turn at peace | -3 |
| Capture enemy city | -5 (victory euphoria) |
| Enemy nation surrenders | -10 |

### WW Effects

| War Weariness | Effects |
|---------------|---------|
| 0-25 | None. Public supports the war. |
| 26-50 | -10% production speed, -5 morale to all units |
| 51-75 | -20% production, -10 morale, -1 TA/turn, protest crises |
| 76-100 | -30% production, -20 morale, -2 TA/turn, military coup risk |

### AI Behavior
AI nations actively seek peace when WW > 75. Prevents eternal wars.

### Crisis Integration
WW > 50 triggers "Anti-War Protests" crisis:
1. Crack down (-10 WA, -5 WW)
2. Address concerns (-5 TA, -15 WW)
3. Offer ceasefire (opens peace negotiation, -20 WW)

### Player Decision
Winning against Russia but it's been 30 turns. WW at 60 — production slowing, morale dropping, protests erupting. Push for Moscow (risky, could trigger DEFCON 1) or accept favorable peace now?

---

## PART 30: HOW SYSTEMS INTERCONNECT

These aren't isolated features — they chain together into emergent gameplay:

1. **Assassination → Morale collapse → Mass retreat → Easy siege**
   Kill the enemy leader, their army crumbles (-30 morale), units rout, cities fall.

2. **Sub blockade → Supply cut → Attrition → Win without fighting**
   Strangle supply lines, enemy vehicles freeze, troops starve.

3. **Bomber raids → Factory disabled → No reinforcements + Nuke deterrence prevents retaliation**
   Air superiority lets you dismantle their war machine while nukes keep them from escalating.

4. **War weariness → Crisis events → TA drops → Military weakens → Enemy exploits opening**
   Long wars destroy you from within before the enemy does.

5. **Decoys + high Prestige → Enemy wastes resources → Real attack elsewhere**
   Intelligence warfare as force multiplier.

6. **DEFCON rises → Economy penalty → Must choose military or economy → Deterrence shifts AI to covert ops**
   The nuclear shadow shapes all decisions.

7. **Winter invasion → Supply strain → Morale crash → Guerrilla defenders shred you**
   Timing and terrain matter as much as raw numbers.

8. **Combined arms + veteran units + good general → Small force beats large one**
   Quality over quantity is a viable strategy for the UK's tiny military.

Every decision ripples across multiple systems. This creates the "one more turn" emergent storytelling that defines great strategy games.
