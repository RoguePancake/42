# WARSHIP — Game Characters & Campaign Structure
## Design Specification

> **Created:** 2026-03-27
> **Status:** Design — replaces the old Authority Meter system

---

## The Game in One Sentence

A fictional military war game played on the real-world map. Each level is a different global crisis scenario. Win by completing the mission objective using warfare, diplomacy, and intelligence.

---

## Campaign Structure

The game is a series of **scenarios** — fictional "what if" crises on the real-world map. Each scenario has unique starting conditions, unique pressures, and a specific win condition. The core military systems (units, combat, production, supply, nukes) carry across all scenarios.

### Level 1 — FIRST STRIKE
> Your country just became the world's first nuclear power. Five superpowers want your weapon. Take everything from them instead.

| | |
|---|---|
| **Starting Position** | Small nation, 4 provinces, 1 nuclear warhead, minimal military |
| **The World** | 5 superpower blocs at full strength, competing with each other and eyeing you |
| **Win Condition** | World conquest — control all territories |
| **Key Tension** | You have the bomb but not the army. Use deterrence to buy time, build up, then strike. |

### Level 2 — ASH AND BLOOD
> A supervolcano has erupted. Global crops are failing. Borders are collapsing. Fight for what's left.

| | |
|---|---|
| **Starting Position** | Mid-size nation, moderate military, food supplies dwindling |
| **The World** | All nations weakened. Resources scarce. Alliances fracturing. Refugee crises. |
| **Win Condition** | Last nation standing |
| **Key Tension** | Everyone is desperate. No one can sustain a long war. Strike fast or starve. |

### Level 3 — THE SIEGE
> You are the world's dominant superpower. Everyone else just formed a coalition against you. They're coming from every direction.

| | |
|---|---|
| **Starting Position** | Largest military, largest economy, surrounded on all sides |
| **The World** | Every other nation is allied against you. They attack simultaneously. |
| **Win Condition** | Survive — hold your capital and 50% of your territory for 200 turns |
| **Key Tension** | You're powerful but stretched thin. Can't defend everything. What do you sacrifice? |

### Level 4 — DARK WINTER
> A pandemic has collapsed the global economy. Governments are falling. Armies are all that remain.

| | |
|---|---|
| **Starting Position** | Intact military, zero economy, population in freefall |
| **The World** | No trade. No production. Nations running on stockpiles. Coups everywhere. |
| **Win Condition** | Control all 6 capitals |
| **Key Tension** | You can't build anything new. Every unit lost is gone forever. Manage what you have. |

### Level 5 — SECOND SUN
> Someone launched a nuke. Now everyone has them. DEFCON 1. The world is 10 minutes from extinction.

| | |
|---|---|
| **Starting Position** | Full nuclear arsenal, large military, DEFCON 1 from turn 1 |
| **The World** | All nations nuclear-armed. Hair-trigger launch postures. One mistake ends everything. |
| **Win Condition** | Disarm all other nations (conquest or diplomacy) without triggering global nuclear exchange |
| **Key Tension** | You can destroy anyone, but they can destroy you back. Win without ending the world. |

*More scenarios added over time. Each tests different parts of the military system.*

---

## Character System

Characters exist to serve the war game. They are the **human layer on top of the military machine** — providing command bonuses, driving AI behavior, and creating assassination as a viable military tactic.

### What Characters Are NOT
- Not an RPG progression system
- Not a separate gameplay layer with its own meters
- Not sprites walking around the map
- Not a political simulation

### What Characters ARE
- Your identity (and your lose condition — you die, game over)
- Commanders who buff your military formations
- Enemy leaders whose personality drives how their bloc wages war
- High-value targets whose death cripples an enemy's military

---

## Player Leader

You are the head of state. You don't have stats that matter independently — your power IS your nation's military, economy, and intelligence apparatus.

**What your leader provides:**
- **Command authority** — your nation's troops fight at full effectiveness while you're alive
- **Decision-making** — you choose doctrine, authorize nukes, declare wars, assign generals
- **Assassination target** — if an enemy kills you, game over

**What your leader does NOT do:**
- Walk around the map (you're running a country, not adventuring)
- Have RPG stats that level up
- Compete on a career ladder

You're already at the top. The challenge is staying there while conquering everything below you.

---

## Your Generals (2-3 per nation)

Generals are commanders assigned to military formations. They provide concrete battlefield bonuses.

| Role | Formation Bonus | Risk |
|------|----------------|------|
| **Field Marshal** | +15% attack to assigned units | Killed if formation destroyed — all units lose bonus + morale hit |
| **Admiral** | +15% defense to assigned naval group | Same — fleet loses cohesion |
| **Air Commander** | +3 tile air superiority range | Same — air coverage collapses |

### Mechanics
- Assign a general to a formation (Ctrl+1-9 groups from MILITARY_SYSTEM.md Part 7)
- Formation receives the general's bonus as long as they're assigned
- If the formation is destroyed in combat, the general dies
- Losing a general: formation loses bonus immediately + **-20 morale** to all surviving units in that formation
- Generals can be reassigned between formations (takes 1 turn to transfer)
- **You cannot create new generals.** Generals are generated at game start per scenario. Lose them and they're gone.

### Player Decision
Your best general makes your main army devastating — but if that army gets wiped, you lose both the troops AND the irreplaceable commander. Split your generals across formations (safer, weaker) or concentrate them (riskier, stronger)?

---

## Enemy Bloc Leaders

Each of the 5 superpower blocs is led by a specific character. Their personality determines how their nation wages war. This is not flavor text — it directly controls AI behavior.

### Leader Personality → AI Doctrine

| Bloc | Leader | Personality | Military Behavior |
|------|--------|-------------|-------------------|
| **Hegemon** (US) | President Mitchell | Cold pragmatist. Calculates everything. | Combined Arms doctrine. Won't attack unless odds favor him 2:1. Will use economic pressure first. Escalates methodically. |
| **Revolutionary** (Russia) | Chairman Volkov | Paranoid and aggressive. Sees threats everywhere. | Blitzkrieg doctrine. Attacks early and often. May bluff a nuclear program. Unpredictable timing. |
| **Commercial League** (China) | Director Wei | Patient. Everything is leverage. | Fortress doctrine. Builds up slowly, massive economy. Tries to buy your allies. When ready, overwhelming force. |
| **Traditionalist** (EU) | Chancellor Muller | Principled but cautious. Avoids risk. | Fortress doctrine. Strong defense, weak offense. Will sanction you rather than fight. Coalition builder. |
| **Survival Accord** (India) | PM Sharma | Desperate pragmatist. Will do anything to survive. | Guerrilla doctrine. Unpredictable alliances. Will betray anyone if survival demands it. |

### Assassinating Enemy Leaders

This is a core military tactic, not a political minigame.

**How it works:**
1. Build an Intelligence HQ (facility from MILITARY_SYSTEM.md Part 2)
2. Deploy spy network against target nation (takes turns to build depth)
3. At sufficient depth, assassination becomes available as an interrupt choice
4. Success chance based on spy network depth vs target's counter-intelligence

**What happens when an enemy leader dies:**
- **-30 morale to ALL units** in that nation (immediate, devastating)
- **Military effectiveness drops 40%** for 5 turns (command confusion)
- **AI behavior becomes erratic** for 10 turns (successor is weaker, makes mistakes)
- **Successor takes over** — randomly generated, lower skill, different personality quirks
- Other blocs may see weakness and attack the leaderless nation

**What happens when YOU die:**
- Game over. Restart scenario or load save.

### Player Decision
Assassination is the single most powerful move in the game — it can swing an entire war. But it requires massive investment in intelligence (money and time that could go to military), and if you fail, the target nation knows you tried and may retaliate. Do you spend 15 turns building a spy network to kill Volkov, or spend those resources building 10 more tanks?

---

## Enemy Generals

Each bloc has 2-3 generals assigned to their major army groups. Same mechanics as your generals.

**Why they matter:**
- An enemy general makes their formation significantly stronger
- Killing a general (by destroying their formation) weakens that entire front
- Intel can reveal which formations have generals (priority targets)
- A general-less army group is noticeably weaker — focus fire there

**Flipping generals (advanced, scenario-dependent):**
- In some scenarios, enemy generals can be bribed to defect
- A defecting general brings intel about enemy positions, not troops
- Requires high spy network depth + significant treasury investment
- Creates an interrupt: "General Kuznetsov offers to defect. He wants $500M and asylum. He knows where Volkov's reserves are hidden."

---

## What This Replaces

The old Authority system (TA/WA/BSA/FAI meters) is removed. In its place:

| Old System | Replaced By |
|------------|-------------|
| Territory Authority meter | Your actual territory — provinces you control |
| World Authority meter | Your actual diplomatic state — alliances, reputation |
| Behind-the-Scenes Authority meter | Your spy network depth against each nation |
| Full Authority Index (win condition) | Scenario-specific win conditions |
| 8 VIPs per nation with authority scores | 1 leader + 2-3 generals per nation with military relevance |
| Political action buttons (bribe/threaten) | Interrupt-driven decisions that emerge from the simulation |
| Career ladder / role selection | You're the leader. Period. Scenario defines your starting position. |

---

## How Characters Interact With Military Systems

Characters aren't a separate system. They're hooks into the military engine:

| Military System (from MILITARY_SYSTEM.md) | Character Connection |
|------------------------------------------|---------------------|
| **Part 7: Formations** | Generals are assigned to formations |
| **Part 17: Veterancy** | Veteran units with a good general are devastating |
| **Part 18: Morale** | Leader death = -30 morale to ALL units |
| **Part 22: Doctrine** | Leader personality determines national doctrine |
| **Part 24: Nuclear Deterrence** | Player leader authorizes launches |
| **Part 26: Deception** | Enemy leader personality affects how they fall for deception |
| **Part 27: Generals** | Direct implementation — command bonuses on formations |
| **Part 30: System Interactions** | Assassination → morale collapse → retreat → easy siege |

---

## Summary

The character system is **small by design**. This is a military war game with scenario-based campaigns. Characters give the war machine a human face — your generals strengthen your armies, enemy leaders drive how blocs fight, and assassination is a weapon of war.

Everything else is tanks, planes, ships, nukes, and the map.
