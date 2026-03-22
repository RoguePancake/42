# WARSHIP — Game Flow
## What the Player Experiences

---

## 1. TITLE SCREEN

Dark screen (#0a0a18). Pixel art style.

```
        ☢ W A R S H I P ☢
      Leaders of the Warship

         ▸ NEW GAME
           LOAD GAME
           SETTINGS
           QUIT
```

SNES blue window frames around menu items. Gold text (#f8d830) for title.
Cursor (▸) moves with arrow keys or mouse. Enter or click selects.

---

## 2. GAME SETUP (Future — skip for test game)

For v0.1.0 test game, skip this screen. Hardcode:
- Scenario: FIRST FIRE
- World: Fictional (procedural)
- Difficulty: Standard
- Seed: 42 (or random)

---

## 3. OPENING SPEECH — FIRST FIRE

Black screen. Text appears one line at a time, each fading in over 0.5s.
Player clicks or presses SPACE to advance.

```
We did it.

In a concrete bunker underneath a wheat field
in our smallest province...

...three physicists and an engineer did what every
superpower on earth spent billions trying to do.

They built the weapon that ends wars.
Or starts the last one.

We are a free state. Four provinces.
Two million people.

An army that couldn't hold a single border
against any of the five blocs that carve
this world between them.

On any other day, we are nobody.

That was yesterday.

Today, every satellite in orbit is pointed at us.

Every spy network is activating assets
in our capital.

Every ambassador is requesting
an emergency meeting.

Every general is drawing up invasion plans —
and every diplomat is drawing up alliance offers.

Because we have the bomb.
The only one.
And everyone knows it.

Five blocs. Five offers.
Five threats wearing different masks.

And me. Standing in the rubble of a test site,
holding the future of the world in my hands...

...with about ten minutes before someone
decides to take it from me.

Let them come.

They'll find out that the smallest nation
in the world just became the most dangerous.
```

After final line: pause 2 seconds, then fade to white, then fade to map.

---

## 4. MAP REVEAL

Camera starts zoomed out showing the full world.
All 6 nations visible with colored territories.
Pause 1 second.

Camera smoothly zooms toward player's territory (gold/yellow).
Zoom takes 2-3 seconds.
Settles on player's capital city.

Brief overlay text:
```
SELVARA — FREE STATE
Population: 2,100,000
Provinces: 4
Military: Minimal
Nuclear Arsenal: 1 warhead
```

Text fades after 3 seconds. Player has control.

---

## 5. FIRST TURN — WHAT THE PLAYER SEES

### The Map
- Their 4 provinces are clearly visible (gold territory tint)
- 5 surrounding blocs in red, blue, purple, green, orange
- Cities visible at capitals with flags
- Rivers winding through terrain
- Green trade route lines between some nations
- Fog covering distant territory they haven't explored

### The HUD (Top Bar)
```
☢ WARSHIP │ SELVARA │ 💰 800g │ [STAB ████░░] 74% │ T1 Y100 │ ☢×1 │ ▶ END TURN
```

### Their Units
- 2 tanks near capital
- 3 soldiers spread across provinces
- 1 artillery at capital

### The News Feed (Bottom)
```
📰 TURN 1
  EVENT  The world holds its breath. Your nuclear test was detected.
  DIPLO  IRONPACT requests emergency diplomatic contact.
  DIPLO  MERIDIAN League expresses "deep concern."
  DIPLO  VOLKREN Front congratulates your "scientific achievement."
  DIPLO  ASHWARD Pact calls for immediate disarmament.
  INTEL  FREEHOLD Accord: unusual troop movements near your border.
```

### Available Actions
- Click units to select → right-click to move
- Click rival territory → nation info panel opens
- Press B → budget panel opens
- Click END TURN → advance to turn 2

---

## 6. TYPICAL TURN (Turns 2-30)

Each turn the player:

1. **Reads the news** — what happened last turn
   - Battles, diplomacy changes, economic events
   - AI nations doing things (wars, alliances, trade)

2. **Checks their stats** — top bar HUD
   - Treasury going up or down?
   - Stability stable?
   - Anyone at war with them?

3. **Issues orders**
   - Move units to strategic positions
   - Adjust budget if needed
   - Check on spy networks
   - React to diplomatic events

4. **Clicks END TURN**

5. **Watches the world react**
   - AI units move
   - Battles resolve (flash effects on map)
   - Trade convoys flow along routes
   - News feed updates

---

## 7. COMBAT FLOW

When a player unit enters an enemy tile (while at war):

1. Battle resolves next turn (MilitaryEngine Phase 9)
2. Visual: red flash on tile, floating "BATTLE" text
3. Numbers compare: attacker strength vs defender + terrain
4. Result:
   - Winner stays on tile, loses some strength
   - Loser retreats to adjacent owned tile OR is destroyed
5. News: "BATTLE: Your 1st Tank Division defeats IRONPACT forces at (12,8)"

---

## 8. WAR FLOW

1. **Declaration:** Click rival nation → DECLARE WAR
2. **Invasion:** Move units into their territory
3. **Battles:** Each tile contact = battle next turn
4. **Progress:** Capture tiles, push toward their capital
5. **Victory:** Capture their capital → they'll accept peace terms
6. **Peace:** Click OFFER PEACE → demands based on how much you won
7. **Aftermath:** Conquered tiles change to your color

---

## 9. ECONOMY FLOW

Each turn automatically:
- Provinces produce income based on terrain + development
- Military costs upkeep
- Net treasury change shown in HUD

Player controls:
- Budget allocation (Military/Infra/Research/Social)
- Trade happens automatically between friendly nations
- War disrupts trade (routes disappear during war)

---

## 10. INTEL FLOW

Without spy network: enemy stats show "???"

With spy network:
1. Click nation → ESTABLISH SPY NETWORK (200g)
2. Wait 5 turns → Depth 1 (see treasury, stability)
3. Wait 10 turns → Depth 2 (see military strength)
4. Wait 15 turns → Depth 3 (see relations with others)

Makes you choose: spend gold on intel or military?

---

## 11. FOG OF WAR

- Your territory: always clear
- Adjacent to your territory: clear
- Where your units are: clear (5 tile radius)
- Where units WERE: explored (partially fogged, terrain visible)
- Never explored: fully dark

Moving units acts as exploration. You must actively scout
to see what's happening on the other side of the world.

---

## VISUAL STYLE REFERENCE

Everything should look like it belongs on a Super Nintendo:

- **Map tiles:** Bright, colorful, 32×32 pixels with detail (grass has flowers, forests have tree shapes, water has wave hints)
- **Sprites:** Chunky, recognizable at small sizes (tanks with treads, soldiers with helmets, cities with buildings)
- **UI windows:** Dark blue (#18184a) with beveled borders — bright white/blue top-left edge, dark bottom-right edge
- **Text:** White monospace pixel font on dark backgrounds
- **Stat bars:** Colored fills (green/red/gold) on dark backgrounds with 1px dark border
- **Colors:** Bold and saturated, not muted. This is a 16-bit game, not a modern dark-mode app.

---

*This is what we're building toward. Phase by phase.*
