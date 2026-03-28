# [ARCHIVED] FULL AUTHORITY (FA)
## Phase Implementation Plan (Expanded Master Roadmap)

> **ARCHIVED 2026-03-24:** This plan has been merged into `docs/ROADMAP.md`.
> All phase statuses and future work are tracked there now.
> Kept for historical reference only.

We are aggressively driving development to complete all phases.

---

### **ACT I - THE FOUNDATION (Phases 1-11) [COMPLETE]**
✅ **Phase FA-1:** Physical VIP Characters & Authority scoring (TA, WA, BSA, FAI).
✅ **Phase FA-2:** The Interactive Dossier & Intel UI.
✅ **Phase FA-3:** The Power Play Engine (Bribes, Investigations, Threats).
✅ **Phase FA-4:** Turn Engine & Rival AI (Passive scoring & time advancement).
✅ **Phase FA-5:** The Crisis & Event Popups (Random Geopolitical events).
✅ **Phase FA-6:** Swarm Military Control Engine (3,000 real-time moving agents).
✅ **Phase FA-7:** Swarm Combat, Capture, & Assassination impact rules.
✅ **Phase FA-8:** The Ascension (Win Condition & Victory Panel).
✅ **Phase FA-9:** The "Full Authority" Main Menu Start Screen & HUD Anchoring.
✅ **Phase FA-10:** Economy Engine. Nations generate Treasury income based on Cities & Provinces.
✅ **Phase FA-11:** News Ticker. Bottom-scrolling global news marquee.

---

### **ACT I.5 - THE REAL-WORLD MAP OVERHAUL (Phases M1-M5) [IN PROGRESS]**

> Vision pivot: Replace the procedural 80×50 SNES-style tile map with a high-fidelity real-world Earth map using OpenStreetMap tiles. Nations overlay on real geography. The map IS the game.

✅ **Phase M-1:** Data Model Updates — Added lon/lat to NationData, CityData, UnitData, CharacterData. Created GeoData.cs with 6 nations using real borders, 80+ real cities, military bases, trade routes.
✅ **Phase M-2:** TileMapRenderer — Dual-style tile renderer (OSM vibrant + OpenTopoMap) with LRU cache (2048 tiles), smooth zoom interpolation, WASD/scroll/drag navigation, frustum culling.
✅ **Phase M-3:** WarshipMapBridge — Event-bus connected bridge drawing government overlay (nation territories, borders, city labels, unit markers, trade routes, command crosshairs). Toggleable with G key.
✅ **Phase M-4:** WorldGenerator Rewrite — Real-world nation placement (Washington D.C., Beijing, Moscow, Brussels, New Delhi, London). Named leaders/generals per nation. Military units at real base locations.
✅ **Phase M-5:** Scene Tree Restructure — Swapped MapManager/MapCamera for TileMapRenderer/WarshipMapBridge in Main.tscn. Fixed all cross-references.

🟡 **Phase M-6:** Tile Download Completion — OSM Standard zoom 0-10 (~15 GB) downloading now. Topo zoom 0-9 (~4 GB) queued after.
🟢 **Phase M-7:** HUD Restyling — Restyle all HUD panels from SNES-blue to dark geopolitical theme with gold (#D4A84B) accents. Add coordinate readout, map mode tabs (F1/F2/G), resource bar.
⚪ **Phase M-8:** Map Mode Tabs UI — Bottom bar with Political/Terrain/Government toggle buttons (currently keyboard-only: F1, F2, G).
⚪ **Phase M-9:** City Detail Overlays — Population indicators, strategic importance markers, port/airfield icons at real city locations.
⚪ **Phase M-10:** Frontline Rendering — Dynamic war frontlines drawn between nation borders when at war. Animated conflict zones.

---

### **ACT II - ECONOMY & SUBVERSION (Phases 12-15)**
🟢 **NEXT Phase FA-12:** Espionage Grid (Fog of War for rival stats/intentions requiring Intel Points to penetrate).
⚪ **Phase FA-13:** Rebellions & Insurgency (Nations with negative Treasury or <10% TA spawn hostile Rebel swarms).
⚪ **Phase FA-14:** National Debt & Bailouts (Borrowing against WA to secure emergency funds).
⚪ **Phase FA-15:** Black Market Trading (Bribing factions for instant arms deals).

---

### **ACT III - GLOBAL ALLIANCES & FALLOUT (Phases 16-25)**
⚪ **Phase FA-16:** United Nations Assembly UI (Proposing embargos and resolutions).
⚪ **Phase FA-17:** Alliance Webs (Defensive pacts between specific Nations).
⚪ **Phase FA-18:** Border Checkpoints & Attrition (Moving through enemy territory damages swarms naturally).
⚪ **Phase FA-19:** City Siege Mechanics (Cities have actual planetary hitpoints before flipping).
⚪ **Phase FA-20:** Media Warfare (Running propaganda campaigns to tank a rival's WA globally).
⚪ **Phase FA-21:** Military Research Facilities (Upgrading swarms to Tanks / Jets / Fleets).
⚪ **Phase FA-22:** Nuclear Silo Construction (The ultimate doomsday weapon).
⚪ **Phase FA-23:** DEFCON System (Global panic meter scaling with massive military movements).
⚪ **Phase FA-24:** WMD Strikes (Nuking a city permanently changes it to radioactive waste, huge WA penalty).
⚪ **Phase FA-25:** Leader Succession (When a leader is eliminated, a new randomly generated protege takes extreme vengeance).

---

### **ACT IV - FULL AUTHORITY (Phases 26-30)**
⚪ **Phase FA-26:** Espionage Agency Bases (Placing literal Spies on the map).
⚪ **Phase FA-27:** True Leader Permadeath (If all succession options run out, the nation entirely collapses into Free States).
⚪ **Phase FA-28:** Procedural Global Objectives (e.g., "The Oil Crisis" or "The Pandemic" affecting all nations for 12 turns).
⚪ **Phase FA-29:** Saving & Loading states.
⚪ **Phase FA-30:** The Final Polish. Sound effects, visceral impacts, massive particle explosions, and deployment of FULL AUTHORITY.
