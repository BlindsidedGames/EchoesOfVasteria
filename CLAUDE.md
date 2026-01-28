# Echoes of Vasteria - Claude Context

## Project Overview

**Echoes of Vasteria (EoV)** is an incremental/idle hero management RPG on Steam. The hero auto-runs through procedurally generated maps, completing tasks (farming, fishing, mining, woodcutting, looting) and fighting enemies. Resources feed into crafting, the Cauldron card system, and offline generation via Alter Echoes.

- **Engine:** Unity 6.3 LTS (6000.3.5f1)
- **Platform:** Steam (Steamworks.NET)
- **Version:** 1.4.3
- **Style:** 2D top-down pixel art, A* pathfinding
- **Codebase:** 289 C# files across 32 directories

## Core Systems

### Tier Chain
**Eznorb → Nori → Dlog → Erif → Lirium → Copium → Idle → Vastium** (8 tiers, indices 0-7)

Used across: Mining Chunks, Looting Cores, Enemy Crystals, Crafted Ingots, Gear Cores.

### Key Formulas

**Defense:** `damage_taken = incoming * (1 - defense/(defense + 25))`
- Location: `Assets/Scripts/Combat/Combat.cs:28`

**Movement Speed:** `speed = 3 + 6 * rating/(rating + 25)`
- Location: `Assets/Scripts/Hero/Stats/HeroStatSystem.cs:180-186`

**Stew from Mixing:** `stew = (amountA * valueA + amountB * valueB) / 100`
- Location: `Assets/Scripts/Upgrades/CauldronManager.cs:388-389`

### Important Conventions

- **Stat quality stored as percentiles [0,1]** not absolute values (for balance resilience)
- **Resources display as whole numbers** (floored)
- **Intentional spelling:** Watermelone, Chillie, Pumking, Funion
- **Quest-gated progression:** Higher tier drops require completing kill quests

## Architecture Patterns

### Singleton Pattern (3 approaches used)
1. **Generic `Singleton<T>`** - Preferred for MonoBehaviours
2. **Manual Instance property** - Legacy pattern in some managers
3. **Static classes** - For stateless utilities (HeroStatSystem, Combat, TaskWeightService)

### Service Pattern
Stateless service classes: `CraftingService`, `SalvageService`, `TaskWeightService`, `BaseStatService`

### References Pattern (UI Architecture)
MonoBehaviour classes in `References/` hold SerializeField UI references, decoupling logic from scene structure:
- `StatPanel/` (5 files) - Stat panel entry references
- `UI/` (19 files) - General UI element references

### ScriptableObject Configuration
27 SO classes drive gameplay: Skills, Quests, Tasks, Gear, Enemies, Milestones, Cauldron

## Directory Structure

```
Assets/
├── Scripts/
│   ├── (Root - 15 files)     # GameManager, TargetRegistry, HealthBase, Projectile
│   ├── Audio/          (3)   # AudioManager, SfxPlayer
│   ├── Blindsided/    (60)   # Framework: SaveData/, UGS/, Utilities/
│   ├── Buffs/          (4)   # BuffManager, BuffRecipe, BuffTypes
│   ├── Combat/         (1)   # Combat.cs (damage formula)
│   ├── Editor/         (4)   # Editor windows and tools
│   ├── Enemies/        (7)   # EnemyData, Enemy, Health
│   ├── Gear/          (20)   # CraftingService, GearItem, StatRollMath, SO/, UI/
│   ├── Hero/          (15)   # HeroController, HeroBase, EchoManager, Stats/
│   ├── Localization/   (1)   # LocalizationManager
│   ├── MapGeneration/  (9)   # SegmentedMapGenerator, TilemapChunkGenerator
│   ├── NPC/            (4)   # NpcObjectStateController, decorations
│   ├── NpcGeneration/  (6)   # AlterEchoGenerationManager (offline gen)
│   ├── Platform/       (4)   # Mobile platform integration
│   ├── Quests/         (9)   # QuestData, QuestManager
│   ├── References/    (24)   # UI reference containers (StatPanel/, UI/)
│   ├── Skills/        (17)   # Skill, MilestoneDefinition, Milestones/
│   ├── Stats/          (1)   # GameplayStatTracker
│   ├── Steamworks.NET/ (7)   # Steam achievements, leaderboards
│   ├── Tasks/         (16)   # ITask hierarchy, TaskController
│   ├── Tools/          (4)   # Console commands, debug
│   ├── UI/            (31)   # Window managers, panels
│   ├── Upgrades/      (16)   # Resource, ResourceManager, CauldronManager
│   └── Utilities/     (11)   # Singleton base, helpers
├── Resources/
│   ├── Buffs/         (10)   # Buff recipe assets
│   ├── Cauldron/       (1)   # CauldronConfig.asset
│   ├── Enemies/       (22)   # EnemyData assets (Farmlands/, Slimes/)
│   ├── Gear/          (25)   # Cores/, Rarity Assets/, StatDef/
│   ├── Infinity/       (7)   # Infinity stat upgrades
│   ├── Quests/        (97)   # Per-NPC quest folders
│   ├── Resource Items/(69)   # All resource ScriptableObjects
│   ├── StatUpgrades/   (9)   # Stat upgrade assets
│   └── Tasks/         (49)   # Farming/, Fishing/, Mining/, etc.
├── Scriptables/
│   ├── Map/                  # Terrain assets (Grass, Sand, Water)
│   ├── MapSettings/    (6)   # Per-map configuration
│   └── Skills/         (6)   # Skill assets + Effects/ + Milestones/
├── Prefabs/
│   ├── Decor/        (122)   # Environment decoration
│   ├── Projectiles/    (4)   # Combat projectiles
│   ├── Tasks/         (80)   # Task prefabs (Farming/, Mining/, etc.)
│   └── UI/            (51)   # UI panel prefabs
└── Steamworks.NET/           # Steam SDK
```

## NPCs

| NPC | ID | Quest Folder | System |
|-----|-----|--------------|--------|
| Ivan | Ivan1 | - | Forge crafting, Crafting Mastery |
| Eva | Witch1 | Eva/ (4) | Cauldron mixing/tasting |
| Old Timer | OldTimer1 | Old Timer/ (7) | Mining ore unlocks |
| Barkley | Barkley1 | Barkley/ (9) | Woodcutting |
| Flora & Tillman | Farmers1 | Flora and Tillman/ (16) | Farming crop unlocks |
| Gill | - | Gill/ (1) | Fishing |
| Mildred | - | Mildred/ (10) | Buff/Echo slot unlocks |

## Maps

| Map | Focus | Status |
|-----|-------|--------|
| Farmlands | Farming | Complete |
| Woods | Woodcutting | Complete |
| River | Fishing | Complete |
| Beach | Gathering | Complete (no enemies) |
| Mines | Mining+Looting | Complete |
| Halloween | Seasonal | **BROKEN** - empty task lists |

## Quest Requirement Types

| Type | Enum | Description |
|------|------|-------------|
| Resource | 0 | Gather specific resources |
| Kill | 1 | Kill specific enemies |
| DistanceRun | 2 | Longest single run distance (unused) |
| DistanceTravel | 3 | Cumulative distance traveled |
| BuffCast | 4 | Cast buffs |
| Instant | 5 | Auto-complete (tutorials) |
| Meet | 6 | Meet specific NPC |
| CriticalStrike | 7 | Land critical hits |
| ResourcesGathered | 8 | Total resources gathered |
| TasksCompleted | 9 | Complete task count |
| CauldronMix | 10 | Mix in cauldron |

## Common Tasks

### Adding a new resource
1. Create ScriptableObject in `Assets/Resources/Resource Items/`
2. Set `resourceID`, `baseValue`, `valueMultiplier`, `CauldronCategory`
3. Add to relevant drop tables in enemy/task assets

### Adding a new quest
1. Create QuestData in `Assets/Resources/Quests/[NPC]/`
2. Set `requiredQuests` for chain dependencies
3. Define `requirements` (Kill, Resource, etc.) and `rewards`

### Modifying gear stats
1. Edit `StatDefSO` in `Assets/Resources/Gear/StatDef/`
2. Adjust `minRoll`, `maxRoll`, `rollCurve`
3. Existing items auto-convert via percentile storage

### Adding a new map
1. Create MapGenerationConfig in `Assets/Scriptables/MapSettings/`
2. Configure terrain layers, task weights, enemy list
3. **Ensure task category arrays are not empty**

## Key Files Reference

| System | Primary Files |
|--------|---------------|
| Hero | `Hero/HeroController.cs`, `Hero/HeroBase.cs` (1545 lines) |
| Stats | `Hero/Stats/HeroStatSystem.cs` (static cache) |
| Combat | `Combat/Combat.cs` (damage formula) |
| Tasks | `Tasks/TaskController.cs`, `Tasks/BaseTask.cs` |
| Enemies | `Enemies/Enemy.cs`, `Enemies/EnemyData.cs` |
| Quests | `Quests/QuestData.cs`, `Quests/QuestManager.cs` |
| Resources | `Upgrades/Resource.cs`, `Upgrades/ResourceManager.cs` |
| Skills | `Skills/Skill.cs`, `Skills/MilestoneDefinition.cs` |
| Cauldron | `Upgrades/CauldronManager.cs` (1297 lines) |
| Forge | `Gear/CraftingService.cs`, `Gear/StatRollMath.cs` |
| Save | `Blindsided/SaveData/GameData.cs`, `SaveManager.cs` |
| Pooling | `Blindsided/Utilities/Pooling/PoolManager.cs` |
| Map Gen | `MapGeneration/SegmentedMapGenerator.cs` |

## Build & Test

```bash
# Unity project - open in Unity Hub
# Main scene: Assets/Scenes/Main.unity
```

## Change Log

After making changes to the codebase, append a summary to `DevStuff/Changes.md`. Each entry should be a single line describing what was added, changed, fixed, or removed.

## See Also

- [DevStuff/EoV_Context.md](DevStuff/EoV_Context.md) - Full systems documentation
- [DevStuff/CodeReviewTodo.md](DevStuff/CodeReviewTodo.md) - Code review findings
- [DevStuff/Todo.md](DevStuff/Todo.md) - Current development tasks
