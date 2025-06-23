
# Timeless Echoes — MVP Game Design Document

---

## 1. High‑Concept Pitch
*Top‑down 2D pixel‑art action game where you command **up to five heroes** at once.  
Swap control instantly (keys `1–5`), steer the active hero with **WASD + mouse aim**, and let the others auto‑battle.  
Clear escalating waves and bosses inside the **Echo Chamber** arena to earn XP and levels — laying the mechanical groundwork for the future open‑world version of **Timeless Echoes**.*

---

## 2. Target MVP Scope

| Area | MVP Feature |
|------|-------------|
| **Platform** | Windows & macOS standalone (≤ 150 MB) |
| **Session Length** | ~10‑minute arena runs (first‑time players typically end at wave 7‑10) |
| **Heroes** | 4 archetypes<br>• **Knight** – melee cleave<br>• **Archer** – ranged arrow<br>• **Mage** – ranged AoE fireball<br>• **Cleric** – melee heal‑strike |
| **Controls** | WASD movement • mouse aim • number keys **1‑5** to swap hero |
| **Combat** | One telegraphed basic attack per hero:<br>• **Melee** – circular warning, slight delay, hit<br>• **Ranged** – projectile toward mouse/auto target<br>Active hero deals **×2 damage** |
| **Enemies** | 3 mob types (Grunt, Fastling, Brute) + 1 boss (**Cyclops** with slam + laser telegraphs) |
| **Progression** | Damage‑based XP ➜ level curve; each level **+2 Max HP**, **+1 Base Damage** (automatic) |
| **UI** | Character cards showing HP bar, XP bar, active border • Wave counter & banner |
| **Camera** | Cinemachine Camera with 2D Framing Transposer |
| **Art & Audio** | 16×16 / 24×24 pixel sprites, simple slash & projectile VFX, single chiptune loop, SFX for actions |
| **Persistence** | None (self‑contained arena demo) |

---

## 3. Core Game Loop

1. **Enter Echo Chamber** arena.  
2. Countdown → **Wave Start**; spawner releases pattern for the current wave.  
3. Player **moves**/**aims** active hero, **switches** heroes, while AI heroes and enemies auto‑fight.  
4. Every **5 waves** a boss appears; red telegraphs cue dodge & hero‑switch moments.  
5. Heroes earn XP, **level up**, and grow stronger.  
6. After each wave a 3‑second breather; HP does **not** auto‑heal → sustained attrition.  
7. Repeat until all heroes fall → **Game Over** screen shows best wave & stats.

---

## 4. Open‑World Vision (Post‑MVP)

| Pillar | Concept |
|--------|---------|
| **Seamless Overworld** | Hand‑crafted regions unlocked after beating the arena trial |
| **Dynamic Encounter Zones** | Roaming monsters reuse the telegraph & XP systems |
| **Idle Outposts** | Leave heroes gathering resources while controlling another elsewhere |
| **Narrative Arcs** | Quests that branch but always reinforce five‑hero combat core |

_All MVP code and assets are built to transition directly into these overworld features without rewrites._

---

## 5. Milestone Roadmap (6 Weeks)

| Week | Goal | Key Deliverables |
|------|------|------------------|
| **1** | **Foundation** | Repo setup · Input · `PartyManager` · 1 hero prototype · HP UI |
| **2** | **Combat Core** | Telegraphed melee & projectile attacks · enemy vision gizmos · double‑damage flag |
| **3** | **XP & Levels** | `LevelSystem` · stat bumps · XP UI · basic SFX |
| **4** | **Enemy Variety** | Fast & Brute variants · `WaveSpawner` · wave counter UI |
| **5** | **Boss & Polish** | Cyclops boss with slam & laser telegraphs · level‑up popups · basic VFX · Game‑Over flow |
| **6** | **Content + Demo** | Final art pass · balance tuning · main menu · itch.io/Steam demo build |

---

## 6. Stretch Goals (Post‑MVP)

1. **Overworld Region #1: Verdant Isles**  
2. **Zone Persistence** – heroes continue fighting off‑screen  
3. **Waypoint Stones** (fast travel / save)  
4. **Procedural Side‑Events** (ambushes, merchants)  
5. **Simple Gear Modifiers** (+% damage, burn, slow)

---

## 7. Definition of “Done” (MVP)

* Playtesters download a zip, launch, and reach at least Wave 3.  
* In‑game help screen explains controls without external docs.  
* Telegraphs, level‑ups, Game Over, and restart all function without errors.  
* No critical exceptions in the log; stable 60 FPS on mid‑range laptop.  
* Build size under 150 MB.

---

## 8. Immediate Next Step

Create a task board for **Week 1 – Foundation** and implement:

* `PlayerMovement`  
* `PartyManager` (hot‑swap, follow, Cinemachine)  
* Prototype **Knight** hero  
* Dummy **Grunt** enemy  
* HP bar & damage flow  

_Once tested, proceed to Week 2 milestones._

