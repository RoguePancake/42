# WARSHIP — Game Characters & Campaign Structure
## Design Specification

> **Created:** 2026-03-27
> **Status:** Design — replaces the old Authority Meter and Bloc systems

---

## The Game in One Sentence

A fictional military war game played on the real-world map with real nations. Every country is playable. Each level is a different global crisis scenario. Win by completing the mission objective using warfare, diplomacy, and intelligence.

---

## Every Nation Is Playable

No blocs. No abstract superpowers. **Real countries.** The player picks any nation on Earth at scenario start.

Pick the USA and you're a superpower with the biggest military on the planet. Pick Iceland and you're a frozen island with 400,000 people and no army. Both are valid. The difficulty IS your choice of nation.

### Nation Scale (approximate tiers for AI behavior and scenario balancing)

| Tier | Examples | Starting Military | Starting Economy |
|------|----------|-------------------|-----------------|
| **Superpower** | USA, China, Russia | Massive — hundreds of units, nukes, carriers, global reach | $5,000M+ treasury, strong production |
| **Major Power** | UK, France, Germany, Japan, India, Brazil | Large — dozens of units, regional dominance | $2,000-4,000M treasury |
| **Regional Power** | Turkey, Iran, South Korea, Australia, Egypt, Poland | Moderate — enough to defend, limited offense | $500-2,000M treasury |
| **Small Nation** | Norway, Ireland, New Zealand, Portugal, Greece | Small — a handful of units, rely on alliances or terrain | $100-500M treasury |
| **Micro State** | Iceland, Luxembourg, Malta, Fiji, Bhutan | Minimal to none — you start with almost nothing | <$100M treasury |

**The game does not gatekeep.** You want to conquer the world as New Zealand? Go ahead. That's not a bug — that's the hardest difficulty setting in the game.

### What Every Nation Needs (Data)

Each playable nation requires:
- **Real name and capital** (geographic coordinates)
- **Real cities** (5-50 depending on size, with coordinates)
- **Real borders** (polygon for territory overlay)
- **Starting military** (scaled to tier — superpowers get carriers, micro states get a militia)
- **Starting economy** (treasury + resource production based on real geography)
- **Leader name** (real-style name appropriate to the nation)
- **2-3 generals** (named, assigned to starting formations)
- **AI personality** (how this nation behaves when AI-controlled — see below)

### AI Personality by Nation Character

Every nation the player doesn't pick is AI-controlled. AI behavior is driven by national character — not identical bots with different flags.

| Personality Type | Nations | Military Behavior |
|-----------------|---------|-------------------|
| **Expansionist** | USA, Russia, China, Turkey, Iran | Actively seeks territory. Builds military. Will invade weaker neighbors. |
| **Defensive** | EU nations, Japan, South Korea, Australia | Builds strong defense. Won't attack first. Forms coalitions when threatened. |
| **Opportunist** | India, Brazil, Egypt, Indonesia, Pakistan | Waits for others to weaken, then strikes. Flexible alliances. |
| **Survivor** | Small/micro nations | Seeks alliances with nearest major power. Will bandwagon with the winner. Avoids direct conflict. |
| **Wildcard** | North Korea, Israel | Unpredictable. May nuke, may ally, may do nothing. High risk to engage, high risk to ignore. |

---

## Campaign Structure

The game is a series of **scenarios** — fictional "what if" crises on the real-world map. Each scenario has unique starting conditions, unique pressures, and a specific win condition. The core military systems (units, combat, production, supply, nukes) carry across all scenarios.

**The player picks their nation at the start of each scenario.** The scenario defines the crisis. The nation choice defines the difficulty.

### Level 1 — FIRST STRIKE
> Your country just became the world's first nuclear power. Every other nation wants your weapon. Take everything from them instead.

| | |
|---|---|
| **Setup** | Player's chosen nation starts with 1 nuclear warhead and a small military. All other nations are at normal strength. No one else has nukes — yet. |
| **Win Condition** | World conquest — control all capitals |
| **Scaling** | Pick USA = easy mode (big military + nuke). Pick Fiji = nightmare mode (nothing + nuke on a tiny island). |
| **Key Tension** | You have the bomb but need time to build an army. Use deterrence to buy time, build up, then strike. Superpowers are already researching their own nukes. Clock is ticking. |

### Level 2 — ASH AND BLOOD
> A supervolcano has erupted. Global crops are failing. Borders are collapsing. Fight for what's left.

| | |
|---|---|
| **Setup** | All nations start weakened. Food production halved globally. Northern hemisphere nations hit hardest (ash cloud). Southern nations have a temporary advantage. |
| **Win Condition** | Last nation standing |
| **Scaling** | Pick Australia = advantage (southern, isolated, food). Pick Finland = death sentence (dark, cold, no food, Russia next door). |
| **Key Tension** | Everyone is desperate. No one can sustain a long war. Grab fertile territory or starve. |

### Level 3 — THE SIEGE
> The whole world just declared war on you. Every nation. All of them. At once.

| | |
|---|---|
| **Setup** | Player starts as their chosen nation at 150% normal military strength. Every other nation is allied against them and attacks immediately. |
| **Win Condition** | Survive — hold your capital for 200 turns |
| **Scaling** | Pick Russia = hard but possible (huge territory, nuclear arsenal, winter). Pick Belgium = surrounded on all sides, good luck. |
| **Key Tension** | You're powerful but the entire world is coming. Triage — what do you defend, what do you sacrifice? |

### Level 4 — DARK WINTER
> A pandemic has collapsed the global economy. Governments are falling. Only the military remains.

| | |
|---|---|
| **Setup** | All nations start with intact military but zero economy. No production. No new units. What you start with is all you get. |
| **Win Condition** | Control all capitals on your continent (or all island capitals for island nations) |
| **Scaling** | Pick China = massive army but lots of capitals to capture. Pick Japan = small army but only need a few island capitals. |
| **Key Tension** | Every unit is irreplaceable. Every bullet matters. Pure tactical warfare. |

### Level 5 — SECOND SUN
> Someone launched a nuke. Now everyone has them. DEFCON 1. The world is minutes from extinction.

| | |
|---|---|
| **Setup** | All nations nuclear-armed. DEFCON 1 from turn 1. Any nation can launch at any time. |
| **Win Condition** | Disarm all other nations without triggering global nuclear exchange |
| **Scaling** | Pick USA = massive arsenal, strong conventional military. Pick Switzerland = neutral but surrounded by nukes aimed in every direction. |
| **Key Tension** | You can destroy anyone but they destroy you back. Military conquest while avoiding armageddon. |

*More scenarios added over time. Each tests different parts of the military system. Player nation choice makes every scenario replayable at different difficulties.*

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
- Enemy leaders whose personality drives how their nation wages war
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
- **Micro states may start with 0-1 generals.** Another reason they're harder.

### Player Decision
Your best general makes your main army devastating — but if that army gets wiped, you lose both the troops AND the irreplaceable commander. Split your generals across formations (safer, weaker) or concentrate them (riskier, stronger)?

---

## Enemy Leaders

Every AI nation has a leader. Their personality determines how their nation wages war. This is not flavor text — it directly controls AI behavior.

### Leader Personality Drives Everything

Each leader has a personality type (mapped from their nation's character — see AI Personality table above). The personality controls:
- **Doctrine choice** (Blitzkrieg, Fortress, Guerrilla, Combined Arms)
- **Aggression level** (how easily they declare war)
- **Alliance behavior** (do they honor alliances or backstab?)
- **Nuclear posture** (will they launch? bluff? disarm?)
- **Negotiation style** (what deals they accept, what they demand)

Leaders are not interchangeable. Fighting Russia feels different from fighting Japan because their leaders think differently.

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
- Other nations may see weakness and attack the leaderless nation

**What happens when YOU die:**
- Game over. Restart scenario or load save.

### Player Decision
Assassination is the single most powerful move in the game — it can swing an entire war. But it requires massive investment in intelligence (money and time that could go to military), and if you fail, the target nation knows you tried and may retaliate. Do you spend 15 turns building a spy network or spend those resources building 10 more tanks?

---

## Enemy Generals

Every nation has 2-3 generals (scaled by tier — superpowers may have 4-5). Same mechanics as your generals.

**Why they matter:**
- An enemy general makes their formation significantly stronger
- Killing a general (by destroying their formation) weakens that entire front
- Intel can reveal which formations have generals (priority targets)
- A general-less army group is noticeably weaker — focus fire there

**Flipping generals (advanced, scenario-dependent):**
- In some scenarios, enemy generals can be bribed to defect
- A defecting general brings intel about enemy positions, not troops
- Requires high spy network depth + significant treasury investment
- Creates an interrupt: "General Petrov offers to defect. He wants $500M and asylum. He knows where Russia's reserves are hidden."

---

## What This Replaces

The old Authority system and Bloc system are both removed. In their place:

| Old System | Replaced By |
|------------|-------------|
| 5 superpower blocs (Hegemon, Revolutionary, etc.) | Real individual nations — every country on Earth |
| Bloc archetypes driving AI | National character / personality types driving AI |
| Territory Authority meter | Your actual territory — land you control on the map |
| World Authority meter | Your actual diplomatic state — alliances, reputation |
| Behind-the-Scenes Authority meter | Your spy network depth against each nation |
| Full Authority Index (win condition) | Scenario-specific win conditions |
| 8 VIPs per nation with authority scores | 1 leader + 2-3 generals per nation with military relevance |
| Political action buttons (bribe/threaten) | Interrupt-driven decisions that emerge from the simulation |
| Career ladder / role selection | You're the leader. Scenario + nation choice defines your challenge. |
| Fixed player nation (UK) | Any nation. Pick your home country. Pick the hardest challenge you can find. |

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

The character system is **small by design**. This is a military war game with scenario-based campaigns on the real-world map. Every nation is playable — your country choice IS your difficulty setting. Characters give the war machine a human face — your generals strengthen your armies, enemy leaders drive how nations fight, and assassination is a weapon of war.

Everything else is tanks, planes, ships, nukes, and the map.
