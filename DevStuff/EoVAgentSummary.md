# EoV Agent Summary

Quick reference for AI agents working on Echoes of Vasteria.

---

## What Is This Game?

**Echoes of Vasteria** is an **incremental/idle RPG** on Steam where a hero auto-runs through procedurally generated 2D maps, completing tasks and fighting enemies. Players don't control movement directly - they manage gear, skills, quests, and resource systems.

- **Engine:** Unity 6.3 LTS (6000.3.5f1)
- **Style:** 2D top-down pixel art with A* pathfinding
- **Codebase:** 289 C# files, 32 directories

---

## The 8-Tier Resource Chain

**Eznorb → Nori → Dlog → Erif → Lirium → Copium → Idle → Vastium** (indices 0-7)

This tier chain applies to: Mining Chunks, Looting Cores, Enemy Crystals, Crafted Ingots, Gear Cores.

---

## 6 Life Skills

| Skill | Primary Drops |
|-------|---------------|
| **Farming** | Crops (17 types, quest-gated) |
| **Fishing** | Fish (8 types) |
| **Mining** | Stone + tiered Chunks |
| **Woodcutting** | Stick, Log, Feather |
| **Looting** | Leather + tiered Cores |
| **Combat** | Enemy-specific drops + Crystals |

---

## Core Formulas

```csharp
// Defense (Combat.cs:28)
damage_taken = incoming * (1 - defense/(defense + 25))

// Movement Speed (HeroStatSystem.cs:180-186)
speed = 3 + 6 * rating/(rating + 25)

// Stew from Mixing (CauldronManager.cs:388-389)
stew = (amountA * valueA + amountB * valueB) / 100
```

---

## Key Systems

### Forge (Ivan)
- Consumes ingots + cores to craft gear
- 4 slots: Weapon, Helmet, Chest, Boots
- Stats stored as **percentiles [0,1]** not absolute values
- Rarity affects affix count

### Cauldron (Eva)
- Mix resources → Stew → Taste for Cards
- Card types: Resource, Buff, Infinity
- Infinity cards unlock when all normal cards maxed (no cap)

### Alter Echoes (Disciples)
- Passive offline resource generation
- Rate: `BestPerMinute * DisciplePercent * CauldronMultiplier`
- Offline capped at 1 hour with 2x multiplier

### Echoes
- Temporary helper clones spawned by milestones/buffs
- Types: Combat, All, TaskOnly, Selective
- Max 10 per type, uses object pooling

---

## NPCs & Their Roles

| NPC | System |
|-----|--------|
| **Ivan** | Forge crafting, Crafting Mastery XP |
| **Eva** | Cauldron mixing/tasting |
| **Old Timer** | Mining ore tier unlocks |
| **Barkley** | Woodcutting quests |
| **Flora & Tillman** | Farming crop unlocks |
| **Gill** | Fishing quests |
| **Mildred** | Buff/Echo slot unlocks |

---

## Quest Requirement Types

| Type | Description |
|------|-------------|
| Resource | Gather specific resources |
| Kill | Kill specific enemies |
| DistanceTravel | Cumulative distance |
| BuffCast | Cast buffs |
| Instant | Auto-complete |
| Meet | Meet NPC |
| CriticalStrike | Land crits |
| ResourcesGathered | Total gathered |
| TasksCompleted | Task count |
| CauldronMix | Mix in cauldron |

---

## Architecture Patterns

### Singletons (3 approaches)
1. `Singleton<T>` generic base - **preferred**
2. Manual `Instance` property - legacy
3. Static classes - stateless utilities

### Service Pattern
Stateless: `CraftingService`, `SalvageService`, `TaskWeightService`, `BaseStatService`

### References Pattern
UI references in `References/` classes decouple logic from scene structure.

### ScriptableObjects
27 SO classes for configuration: Skills, Quests, Tasks, Gear, Enemies, etc.

---

## Key File Locations

| System | Path |
|--------|------|
| Hero | `Scripts/Hero/HeroController.cs`, `HeroBase.cs` |
| Combat | `Scripts/Combat/Combat.cs` |
| Tasks | `Scripts/Tasks/TaskController.cs` |
| Quests | `Scripts/Quests/QuestData.cs`, `QuestManager.cs` |
| Resources | `Scripts/Upgrades/Resource.cs`, `ResourceManager.cs` |
| Skills | `Scripts/Skills/Skill.cs` |
| Cauldron | `Scripts/Upgrades/CauldronManager.cs` |
| Forge | `Scripts/Gear/CraftingService.cs` |
| Save | `Scripts/Blindsided/SaveData/GameData.cs` |
| Map Gen | `Scripts/MapGeneration/SegmentedMapGenerator.cs` |

---

## Resource Locations

| Type | Path |
|------|------|
| Quests | `Resources/Quests/[NPC]/` (97 assets) |
| Resources | `Resources/Resource Items/` (69 assets) |
| Enemies | `Resources/Enemies/` (22 assets) |
| Tasks | `Resources/Tasks/` (49 assets) |
| Gear | `Resources/Gear/` |
| Skills | `Scriptables/Skills/` (6 assets) |
| Maps | `Scriptables/MapSettings/` (6 assets) |

---

## Intentional Spelling

These are **not typos**: Watermelone, Chillie, Pumking, Funion, Haloween

---

## Maps

| Map | Focus | Status |
|-----|-------|--------|
| Farmlands | Farming | Complete |
| Woods | Woodcutting | Complete |
| River | Fishing | Complete |
| Beach | Gathering | Complete |
| Mines | Mining+Looting | Complete |
| Halloween | Seasonal | **BROKEN** |

---

## Known Issues

- `HeroBase.cs` (1545 lines) - oversized, mixes concerns
- `CauldronManager.cs` (1297 lines) - handles too much
- 14 bare `catch {}` blocks in HeroBase.cs
- Halloween map has empty task lists
- Inconsistent singleton patterns

---

## Save System

- **Format:** Odin Binary with custom header
- **Autosave:** Every 30 seconds
- **Backup:** Rotating snapshots (current, prev1, prev2)

---

## Display Conventions

- Resources: whole numbers (floored)
- Stats stored as percentiles for balance resilience
- Task completion thresholds: 10, 100, 1K, 10K, 100K
