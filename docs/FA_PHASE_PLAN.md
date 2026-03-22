# FULL AUTHORITY (FA) 
## Phase Implementation Plan (New Master Roadmap)

Since shifting the vision from a classic 4X war sim (the original 30-phase plan) to a **Political Thriller**, the implementation roadmap has been completely rewritten.

All previous map, rendering, and architecture phases (1-14) successfully laid the technical groundwork (EventBus, WorldStateManager, UI, procedural tiles). Moving forward, we follow the **FA Phases**:

---

## 🟢 COMPLETE: Phase FA-1 (The Paradigm Shift)
- **Data:** Added `CharacterData` to `Models.cs` containing TA (Territory), WA (World), BSA (Behind the Scenes), and FAI (Full Authority Index).
- **Spawning:** The engine drops physical VIP Characters onto the world map replacing generic nations.
- **HUD:** The UI reflects Player Character metrics and live FAI progression.

---

## 🟡 NEXT: Phase FA-2 (The Dossier & Intelligence UI)
- **Goal:** Allow the player to interact with their rivals.
- **Implementation:** 
  1. Clicking a Character piece on the map opens the **Dossier Panel**.
  2. The panel displays their Role, Allegiance, and estimated Authority scores (hidden behind an intelligence "fog of war").
  3. Includes an empty **Action Menu** (e.g., "Investigate", "Bribe", "Threaten") for future use.

## 🟢 COMPLETE: Phase FA-3 (The Power Play Engine)
- **Goal:** Make the Authority meters shift based on decisions.
- **Implementation:**
  1. Build the `PoliticalEngine` to process actions via the `EventBus`.
  2. Implement functional actions: e.g., "Fund Domestic Militia" (Costs Treasury, increases TA, lowers WA). 
  3. Add Risk factors: Sometimes actions backfire and drop BSA instead.

## 🟢 COMPLETE: Phase FA-4 (The Turn Engine & Rival AI)
- **Goal:** Advance time and make the world alive.
- **Implementation:**
  1. Build the `TurnEngine` to handle End Turn logic (calculating monthly income and passive decay of Authority).
  2. Implement **Rival AI**: Other characters actively take hidden actions each turn to increase their own Authority, creating a race to 100 FAI.

## 🟢 COMPLETE: Phase FA-5 (The Crisis & News System)
- **Goal:** Random geopolitical tension.
- **Implementation:**
  1. Build a News Feed panel that pops up at the start of a turn.
  2. Generate crises (e.g., "Border Skirmish with China", "Data Leak").
  3. Force the player to make a choice that directly impacts TA, WA, or BSA.

## 🟢 COMPLETE: Phase FA-6 (Swarm Military Control)
- **Goal:** Command massive global forces in real-time.
- **Implementation:** 
  1. Spawns 3,000 troops globally (500 per nation) rendered natively without performance loss.
  2. Built `MilitaryEngine` to control troops as a continuous swarm.
  3. Added the **Army Command Panel** to assign global orders (Stage, Attack, Border Watch, Patrol).
  4. Players drop markers on the map to funnel entirely 500-troop armies across the world.

## 🟡 NEXT: Phase FA-7 (Combat & Military Conquest)
- **Goal:** Make swarms capable of fighting and taking territory, heavily influenced by leader assassinations.
- **Implementation:**
  1. Swarms from different nations colliding will destroy each other (Combat simulation).
  2. If a nation's troops capture an enemy city, ownership changes.
  3. **High-Value Targets:** Assassinating a rival nation's Leader (VIP) plummets that nation's Authority and troop strength, making them significantly easier to conquer. *Leaders are physical targets.*

## ⚪ Phase FA-8 (The Ascension to Full Authority)
- **Goal:** The Win condition.
- **Implementation:**
  1. Reaching an FAI of 100 triggers a victory sequence (e.g., seizing direct global control).
  2. Option to "Continue Simulation" as the unchallenged dictator attempting to maximize World Authority globally.
