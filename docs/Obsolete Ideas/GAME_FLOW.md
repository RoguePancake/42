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

Camera starts zoomed out showing the full world — real Earth geography.
All 6 nations visible with colored territory overlays on the vibrant OSM map.
Pause 1 second.

Camera smoothly zooms toward player's territory (UK — orange/gold overlay over Britain).
Zoom takes 2-3 seconds, real coastlines and cities becoming visible.
Settles on London.

Brief overlay text:
```
UNITED KINGDOM — FREE STATE
Capital: London
Major Cities: Manchester, Birmingham, Edinburgh, Glasgow...
Military: Minimal
Nuclear Arsenal: 1 warhead
```

Text fades after 3 seconds. Player has control.

---

## 5. FIRST TURN — WHAT THE PLAYER SEES

### The Map
- UK territory clearly visible (orange/gold overlay on real Britain geography)
- 5 surrounding powers with colored territory overlays:
  - United States (steel blue), China (red), Russia (amber), EU (blue), India (green)
- Vibrant OSM base map — green land, blue oceans, roads, rivers, cities all from tile data
- City labels at real coordinates (★ London, Manchester, Edinburgh...)
- Green trade route lines following real shipping lanes
- Government overlay can be toggled with G key
- F1 = Vibrant map / F2 = Topographic / G = Government overlay on/off

### The HUD (Top Bar)
```
Turn 1 | M1 Y0 | Treasury: $800M | Defense Minister Gen. Crawford | TA: 30% WA: 20% BSA: 40% [FAI: 30%]
```

### Their Units
- Tank groups at London and Portsmouth
- Ships patrolling around the coast

### The News Feed (Bottom)
```
📰 TURN 1
  EVENT  The world holds its breath. Your nuclear test was detected.
  DIPLO  United States requests emergency diplomatic contact.
  DIPLO  China expresses "deep concern."
  DIPLO  Russia congratulates your "scientific achievement."
  DIPLO  European Union calls for immediate disarmament.
  INTEL  India: unusual troop movements near Kashmir border.
```

### Available Actions
- Click units to select → right-click to set command target
- Click rival territory → nation info panel opens
- Use ARMY COMMAND panel to set military orders
- F1/F2/G to switch map modes
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

> **Post-Map Overhaul:** The visual style has shifted from SNES pixel art to
> a high-fidelity real-world geopolitical map aesthetic.

- **Base Map:** Real-world OpenStreetMap tiles — vibrant green land, blue oceans, visible roads/railways/rivers
- **Territory Overlays:** Semi-transparent nation colors over the real map tiles (toggle with G)
- **Nation Borders:** Glowing colored lines tracing real-world border polygons
- **Cities:** Text labels at real coordinates (capitals have ★ star, gold color)
- **Units:** Colored markers at lon/lat positions over the real map
- **Trade Routes:** Green lines following real shipping lanes (Transatlantic, Suez, Pacific, etc.)
- **Map Modes:**
  - F1 = Standard (vibrant OSM)
  - F2 = Topographic (elevation contours, terrain shading)
  - G = Toggle government overlay (borders + territory colors)
- **HUD:** Dark theme panels overlaid on map. Moving toward gold (#D4A84B) accent colors.
- **Target aesthetic:** Plague Inc. / DEFCON / Supreme Ruler — a geopolitical thriller, not retro pixel art

---

*This is what we're building toward. Phase by phase.*
