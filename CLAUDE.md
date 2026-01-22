# Echoes of Vasteria - Claude Context

## Project Overview

**Echoes of Vasteria (EoV)** is an incremental/idle hero management RPG on Steam. The hero auto-runs through procedurally generated maps, completing tasks (farming, fishing, mining, woodcutting, looting) and fighting enemies. Resources feed into crafting, the Cauldron card system, and offline generation via Alter Echoes.

- **Engine:** Unity 6.3 LTS (6000.3.5f1)
- **Platform:** Steam (Steamworks.NET)
- **Version:** 1.4.3
- **Style:** 2D top-down pixel art, A* pathfinding

## Core Systems

### Tier Chain
**Eznorb → Nori → Dlog → Erif → Lirium → Copium → Idle → Vastium**

Used across: Mining Chunks, Looting Cores, Enemy Crystals, Crafted Ingots, Gear Cores.

### Key Formulas

**Defense:** `damage_taken = incoming * (1 - defense/(defense + 25))`

**Movement Speed:** `speed = 3 + 6 * rating/(rating + 25)`

**Stew from Mixing:** `stew = (amountA * valueA + amountB * valueB) / 100`

### Important Conventions

- **Stat quality stored as percentiles [0,1]** not absolute values (for balance resilience)
- **Resources display as whole numbers** (floored)
- **Intentional spelling:** Watermelone, Chillie, Pumking, Funion
- **Quest-gated progression:** Higher tier drops require completing kill quests

## Directory Structure

```
Assets/
├── Scripts/
│   ├── Buffs/           # BuffManager, BuffRecipe, BuffTypes
│   ├── Combat/          # Combat.cs (damage formula)
│   ├── Enemies/         # EnemyData, Enemy
│   ├── Gear/            # CraftingService, GearItem, StatRollMath, EquipmentController
│   ├── Hero/            # HeroController, HeroBase, EchoManager, HeroStatSystem
│   ├── MapGeneration/   # SegmentedMapGenerator, TilemapChunkGenerator
│   ├── NpcGeneration/   # AlterEchoGenerationManager (offline resource gen)
│   ├── Quests/          # QuestData, QuestManager
│   ├── Skills/          # Skill, MilestoneDefinition
│   ├── Tasks/           # FarmingTask, FishingTask, MiningTask, WoodcuttingTask
│   ├── Upgrades/        # Resource, ResourceManager, CauldronManager
│   └── Blindsided/
│       ├── SaveData/    # GameData
│       ├── UGS/         # Leaderboards (UGS)
│       └── Utilities/   # PoolManager, CalcUtils
├── Resources/
│   ├── Enemies/         # EnemyData assets
│   ├── Gear/            # Cores/, Rarity Assets/
│   ├── Quests/          # Per-NPC quest folders
│   ├── Resource Items/  # All resource ScriptableObjects
│   └── Tasks/           # Farming/, Fishing/, Mining/, etc.
├── Scriptables/
│   ├── MapSettings/     # Per-map configuration
│   └── Skills/          # Skill assets + Milestones/
└── Steamworks.NET/      # Steam leaderboards
```

## NPCs

| NPC | System |
|-----|--------|
| Ivan | Forge crafting, Crafting Mastery |
| Eva | Cauldron mixing/tasting |
| Old Timer | Mining ore unlocks |
| Barkley | Woodcutting |
| Flora & Tillman | Farming crop unlocks |
| Gill | Fishing |
| Mildred | Buff/Echo slot unlocks |

## Maps

Farmlands, Woods, River, Beach, Mines, Halloween

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

## Build & Test

```bash
# Unity project - open in Unity Hub
# Main scene: Assets/Scenes/Main.unity
```

## See Also

- [DevStuff/EoV_Context.md](DevStuff/EoV_Context.md) - Full systems documentation
- [DevStuff/Todo.md](DevStuff/Todo.md) - Current development tasks
