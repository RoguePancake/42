# Full Authority (FA) — Core Design Shift

## Vision 
The game has evolved from a traditional war simulator ("Warship") into a deep, geopolitical political thriller named **Full Authority**. 

### 1. The Core Loop
The primary gameplay loop isn't just about conquering lands—it's about consolidating power. 
You begin the game not necessarily as the absolute dictator, but as a VIP on the political ladder (e.g., a Defense Minister, a General, an Intelligence Chief). 

Your objective is to reach a **Full Authority Index (FAI) of 100**.

### 2. The Three Authority Meters
Every VIP in the game is tracked across three massive metrics:

- **Territory Authority (TA):** "How firmly do you control your own domain?"
  - Military readiness, law enforcement, administrative control in specific regions.
- **World Authority (WA):** "How much does the world respect, fear, or depend on you?"
  - Diplomatic weight, military projection (nukes), economic leverage globally.
- **Behind the Scenes Authority (BSA):** "What do you ACTUALLY control?"
  - Blackmail, secret alliances, shadow networks, assassination capability, kompromat.

The combination of these forms the **FAI**. 
Depending on the regime (Democracy vs Junta), the ladder to 100 FAI is entirely different.

### 3. VIP Characters on the Map (High Value Targets)
Characters (VIPs) physically exist on the world map! `CharacterData` represents real targets. They can be selected, tracked, assassinated, or moved. The player themselves is a physical entity on the board.
Because VIPs act as the anchors of a nation's Authority, **eliminating rival leaders directly weakens their country's stability**. Assassinating a foreign President or General will plummet their Territory Authority, making their country vastly easier to conquer by force. VIPs are not just stats—they are physical lynchpins of power that can be captured, bribed, threatened, or wiped from the map entirely.

### 4. Real World Focus
Moving forward, procedurally generated blank-slate nations have been replaced by real-world equivalents (USA, China, Russia, EU, India, UK) to anchor the complex diplomacy systems in immediate reality.
