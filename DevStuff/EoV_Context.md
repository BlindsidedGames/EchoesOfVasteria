# Echoes of Vasteria (EoV) — Consolidated Context Document

This document provides complete context for AI assistance with the EoV codebase.

---

## Product Snapshot

| Field | Value |
|-------|-------|
| **Title** | Echoes of Vasteria (EoV) |
| **Platform** | Steam (Steamworks.NET integration) |
| **Engine** | Unity 6.3 LTS (6000.3.5f1) |
| **Genre** | Incremental/idle hero management RPG with auto-running map sessions |
| **Presentation** | 2D top-down pixel art, tilemaps, A* navigation |
| **Current Version** | v1.4.3 |

---

## Codebase Statistics

| Metric | Count |
|--------|-------|
| C# Files | 289 |
| Directories | 32 |
| ScriptableObject Classes | 27 |
| Singleton/Manager Classes | 19+ |
| Quest Assets | 97 |
| Resource Assets | 69 |
| Prefabs | 257 |
| Milestone Effect Types | 9 |

---

## Core Loop (Runtime)

EoV runs as auto-played map sessions:
1. `Map` prefab spawns with `TilemapChunkGenerator` + `ProceduralTaskGenerator` building terrain and task nodes
2. `TaskController` assigns the earliest unfinished task (ordered by world X)
3. Hero uses A* pathfinding to travel and fight
4. Tasks grant resources and XP; enemies can drop gear; chests always contain gear
5. Offline runs continue while game is closed

**Key Files:** `Assets/Scripts/MapGeneration/*`, `Assets/Scripts/Tasks/*`

---

## Economy: Tier Chain

**Primary tier chain (8 tiers):**

**Eznorb → Nori → Dlog → Erif → Lirium → Copium → Idle → Vastium**

### Tiered Item Families

| Family | Acquisition | Location |
|--------|-------------|----------|
| **Mining Chunks** | Mining tasks | `Assets/Resources/Tasks/Mining/*` |
| **Looting Cores** | Chest tasks | `Assets/Resources/Tasks/Looting/*` |
| **Enemy Crystals** | Combat drops | `Assets/Resources/Enemies/*` |
| **Crafted Ingots** | Forge smelting | `Assets/Resources/Resource Items/*` |
| **Gear Cores** | Crafting ingredient | `Assets/Resources/Gear/Cores/*` |

**Key Files:**
- `Assets/Resources/Resource Items/` — All resource ScriptableObjects
- `Assets/Scripts/Upgrades/Resource.cs` — Resource class
- `Assets/Scripts/Upgrades/ResourceManager.cs` — Tier upgrade logic

### Base (Non-tiered) Resources

| Resource | ID | Source |
|----------|-----|--------|
| Slime | 1 | Slime enemies |
| Stick | 2 | Woodcutting |
| Stone | 3 | Mining |
| Bone | 4 | Skeleton enemies |
| Feather | 5 | Woodcutting, Chickens |
| Log | 6 | Woodcutting |
| Mutton | 65 | Sheep |
| Chicken | 66 | Chickens |
| Egg | 67 | Chickens |
| Leather | 68 | Cows, Pigs |
| Steak | 69 | Cows |
| Pork | — | Pigs |

---

## Life Skills

**Skills:** Farming, Fishing, Mining, Woodcutting, Combat, Looting

**Skill Structure (`Assets/Scripts/Skills/Skill.cs`):**
```csharp
public class Skill : SerializedScriptableObject
{
    public string skillName;
    public float xpForFirstLevel = 10f;
    public float xpLevelMultiplier = 1.5f;
    public float taskSpeedPerLevel = 0.01f;
    public List<MilestoneDefinition> milestones = new();
}
```

**Milestones:** Unlock at skill levels, have passive/active effects, can spawn Echoes.

**Milestone Effect Types (9 classes in `Assets/Scripts/Skills/Milestones/`):**
| Effect Class | Purpose |
|--------------|---------|
| MilestoneStatEffectDefinition | Flat or percent stat bonuses |
| MilestoneResourceBonusEffectDefinition | Resource drop multipliers |
| MilestoneExperienceBonusEffectDefinition | XP multipliers (per-skill or global) |
| MilestoneSpawnEchoEffectDefinition | Spawn echoes with duration/probability |
| MilestoneChanceEffectDefinition | Proc chance effects |
| MilestoneCauldronTasteBonusEffectDefinition | Cauldron-specific effects |

### Woodcutting
- **Always available** (requires Barkley quest "Another Magicool Gift")
- **Drops:** Stick, Log, Feather
- Uses Medium/Large tree variants (oak, birch, spruce)

### Farming (Unlock Order by Task ID)

**Canonical order:** Radish → Corn → Wheat → Watermelone → Carrot → Spud → Tomato → Lettuce → Cucumber → Leek → Parsnip → Pepper → Chillie → Pumking → Strawberry → Funion → Turnip

**Note:** Spelling variants are intentional (Watermelone, Chillie, Pumking, Funion). Task prefab uses "Wartermelone". Quest IDs use "Unlock Chilli" / "Unlock Pumpking" while assets use Chillie/Pumking.

Unlocked via Flora and Tillman "Unlock <Crop>" quests.

### Fishing

**All fish gated by single quest "Finishing Touches":**
Flippy Floppy, Muddy Muck Muncher, Sir Splashford III, Wigglelittle, Snapjaw Jr., Flipzoid, Niblet the Bold, Bloopicus Maximus

### Mining

**Unlock progression:**
- Eznorb Ore, Nori Ore (default)
- Dlog Ore (quest "Chunky Business")
- Higher tiers via Old Timer quests: Unlock Erif → Lirium → Copium → Idle → Vastium

Each ore drops Stone + tiered Chunks.

### Looting
Eznorb through Vastium chests. Chests drop Leather + cores up to tier. No quest gating.

---

## Combat System

### Damage Formula (`Assets/Scripts/Combat/Combat.cs`)

```csharp
damage_taken = incoming * (1 - defense / (defense + 25))
```

| Defense | Damage Reduction |
|---------|-----------------|
| 0 | 0% |
| 25 | 50% |
| 50 | 66.7% |
| 100 | 80% |

**Note:** Defense stacks additively with no DR. The DR occurs in the damage formula, not stat accumulation.

### Enemy Stats (`Assets/Scripts/Enemies/EnemyData.cs`)

```csharp
// Combat: maxHealth, damage, defense, attackSpeed, attackRange
// Movement: moveSpeed, visionRange, assistRange, wanderDistance
// Scaling: healthPerLevel, damagePerLevel, defensePerLevel, distancePerLevel
GetDamageForLevel(level) → damage + damagePerLevel * level
```

### Enemy Types (`Assets/Resources/Enemies/`)

| Category | Enemies |
|----------|---------|
| **Slimes** | Blue/Green/Pink/Red/Yellow in Small/Medium/Large |
| **Skeletons** | Swordsman, Archer, Mage |
| **Farmlands** | Chicken, Cow, Pig, Sheep |

### Drop System (`Assets/Scripts/Upgrades/DropResolver.cs`)

1. First roll by weight
2. Additional rolls use `additionalLootChances`
3. Amount biased low: `t *= t`
4. Filtered by `minX`/`maxX` world position
5. Quest requirements can gate drops

**Enemies can drop gear; chests always contain gear.**

---

## Quest System

### Architecture (`Assets/Scripts/Quests/QuestData.cs`)

```csharp
public class QuestData : ScriptableObject
{
    public string questId;
    public LocalizedString questName, description;
    public string npcId;
    public List<QuestData> requiredQuests;
    public List<Requirement> requirements;
    public List<Reward> rewards;
    public int unlockBuffSlots, unlockAutoBuffSlots;
    public float maxDistanceIncrease;
    public float disciplePercentReward;
}
```

**Requirement Types:** Resource, Kill, DistanceRun, DistanceTravel, BuffCast, Instant, Meet, CriticalStrike, ResourcesGathered, TasksCompleted, CauldronMix

### Skeleton Questline (Crystal Tier Unlocks)

| Quest | Requires | Target | Kills | Crystal Unlock | Max Distance |
|-------|----------|--------|-------|----------------|--------------|
| Watch Them Rattle | Rock and Stone | Swordsmen | 100 | Eznorb + Nori | +50 |
| Bonefire Night | Watch Them Rattle | Archers | 350 | Erif | +100 |
| Rattle 'n' Roll | Bonefire Night | Swordsmen | 200 | Dlog | +50 |
| Shiver in Your Bones | Rattle 'n' Roll | Archers | 550 | Lirium | +100 |
| Bone to Pick | Shiver in Your Bones | Mages | 850 | Copium | +150 |
| Grind Their Bones | Bone to Pick | Mages | 1250 | Idle | +150 |
| Beyond the Bone | Grind Their Bones | Mages | 2000 | Vastium | +150 |

**Location:** `Assets/Resources/Quests/Enemies/`
**Localization:** `Assets/Localization/Tables/Quests/Quests Shared Data.asset`

---

## NPCs

| NPC | Role | Key Systems |
|-----|------|-------------|
| **Ivan** | Craftsman | Gear crafting, Crafting Mastery XP |
| **Eva** | Witch | Cauldron (mixing, tasting, leveling) |
| **Old Timer** | Miner | Ore unlock quests |
| **Barkley** | Lumberjack | Woodcutting, building quests |
| **Flora & Tillman** | Farmers | Crop unlocks |
| **Gill** | Fisherman | Fishing quests |
| **Mildred** | — | Buff slot unlocks, Echo cast unlocks |

**Files:** `Assets/Prefabs/Tasks/NPC/`, `Assets/Resources/Quests/[NPC]/`, `Assets/Art/NPCS/`

---

## Maps

| Map | File |
|-----|------|
| Farmlands | `Assets/Scriptables/MapSettings/Farmlands.asset` |
| Woods | `Assets/Scriptables/MapSettings/Woods.asset` |
| River | `Assets/Scriptables/MapSettings/River.asset` |
| Beach | `Assets/Scriptables/MapSettings/Beach.asset` |
| Mines | `Assets/Scriptables/MapSettings/Mines.asset` |
| Halloween | `Assets/Scriptables/MapSettings/Haloween.asset` |

**Generation:** `Assets/Scripts/MapGeneration/SegmentedMapGenerator.cs`

---

## Forge System

### Crafting (`Assets/Scripts/Gear/CraftingService.cs`)

1. Consume ingots + core resources
2. Roll rarity (weighted, level-scaled)
3. Roll slot with smart protection (recent slots penalized 25%)
4. Generate affixes by rarity
5. Award Ivan XP

### Ingot Conversion

`CoreSO` defines `requiredIngot`, `chunkCostPerIngot`, `crystalCostPerIngot`. Chunks + Crystals → Ingots.

### Stat Quality as Percentiles

Items store stat quality as **normalized [0,1] percentile**, not absolute values. Conversion via `StatRollMath.RemapRoll(percentile)`.

**Why:** Balance patches can adjust ranges while items retain relative quality.

**Migration:** `Migration_GearAffixQuality` (target 1.2.17/1.2.18) backfills quality.

### Equipment Slots

**Slots:** Weapon, Helmet, Chest, Boots

**Guaranteed First Affix:**
- Boots → Move Speed
- Chest → Defense
- Helmet → Max Health
- Weapon → Damage

### Ivan Mastery

- XP per tier: `[2, 3, 5, 8, 13, 21, 34, 55]`
- Bonus +3 XP per rarity step above core tier
- Level curve: `xpForFirstLevel * Mathf.Pow(level, 1.25)`

---

## Cauldron System

**Unlock:** Quest "Unlock Cauldron" (ResourcesGathered 50,000)

### Mixing (Resources → Stew)

```csharp
points = amountA * a.baseValue * a.valueMultiplier + amountB * b.baseValue * b.valueMultiplier
stewGained = points / 100.0
```

### Tasting (Stew → Cards)

Auto-rolls at configurable rate from `CauldronConfig.asset`.

**Card Types:**
| Type | Prefix | Effect |
|------|--------|--------|
| Resource | `RES:` | Alter Echo generation boost |
| Buff | `BUFF:` | Buff power/cooldown |
| Infinity | `INF:` | Eternal stat bonuses (no cap) |
| Lowest | — | Fills weakest card |
| Eva's Blessing | — | 2 random |
| Vast Surge | — | 10 random |

**Tier Thresholds:**
- Resource: `[1, 5, 20, 50, 100, 200, 350, 500]`
- Buff: `[1, 3, 10, 25, 50, 100, 200, 300]`

**Per-Tier Bonuses:**
- Resource Power: `[10, 25, 50, 75, 120, 180, 250, 400]%`
- Buff Cooldown Reduction: `[5, 10, 15, 25, 40, 60, 80, 100]%`
- Buff Power: `[0, 0, 5, 10, 15, 20, 25, 30]%`

### Eva Leveling

XP = stew spent. Next level = `50 + 10*(level-1)`. Level affects tasting weights.

### Infinity (Eternal)

Unlocks when all normal cards maxed. Uses `count^exponent` scaling. No cap.

---

## Alter Echo System (Disciples)

**Files:** `Assets/Scripts/NpcGeneration/AlterEcho*.cs`

Passive offline resource generation. Each unlocked resource gets a disciple.

**Rate:** `BestPerMinute * DisciplePercent * CauldronMultiplier`

**Offline:** Capped at 1 hour, 2x multiplier.

**Storage:**
```csharp
public Dictionary<string, DiscipleGenerationRecord> Disciples = new();
public float DisciplePercent = 0.01f;
```

---

## Buff System

### Architecture (`Assets/Scripts/Buffs/BuffManager.cs`, `BuffRecipe.cs`)

**Effect Types (15):**
MoveSpeedPercent, DamagePercent, DefensePercent, AttackSpeedPercent, TaskSpeedPercent, HealthRegenPercent, CritChancePercent, CritDamagePercent, MaxDistancePercent, MaxDistanceIncrease, InstantTasks, TimeScalePercent, ResourceMultiplier, ExperienceBonusFraction, DistanceDurationPercent

**Duration Types:**
1. **Time** — Seconds, affected by power multiplier
2. **DistancePercent** — Expires at `LongestRun × duration`

### Auto-Buff

- 5 slots (unlocked via quests)
- Auto-casts when: tasting inactive, run in progress, not loading
- Saved to `oracle.saveData.AutoBuffSlots`

### Cooldowns

- Start **after** buff expires
- Exception: MaxDistance buffs start cooldown during active
- Reduced by Cauldron tier
- **Reset on death**
- Long runs don't charge cooldowns on retreat

---

## Player Stats

### Movement Speed (`Assets/Scripts/Hero/Stats/HeroStatSystem.cs`)

```csharp
const float BaseMoveSpeed = 3f;
const float MoveBonusRange = 6f;
const float MovementScalarN = 25f;

speed = BaseMoveSpeed + MoveBonusRange * (rating / (rating + MovementScalarN))
```

**Formula:** `Speed(r) = 3 + 6 × r/(r+25)`

| Rating | Speed |
|--------|-------|
| 0 | 3.0 |
| 25 | 6.0 |
| 50 | 7.0 |
| 100 | 7.8 |

**Note:** Move Speed is a flat rating, not percent.

---

## Echo System

### What Are Echoes?

Temporary helper clones spawned by milestones/buffs.

**Files:** `Assets/Scripts/Hero/EchoController.cs`, `EchoManager.cs`, `EchoType.cs`

**Properties:**
- Configurable lifetime with optional extension
- Subject to caps (default 10 per type)
- Use A* pathfinding
- Duration UI bars (green → yellow → red)

### Echo Types

```csharp
public enum EchoType
{
    Combat,      // Combat only
    All,         // Fight + any task
    TaskOnly,    // No combat
    Selective    // Listed skills only
}
```

**Pooling:** `PoolManager.Get(gm.EchoPrefab)`

---

## Hero Controller

**Files:** `Assets/Scripts/Hero/HeroController.cs`, `HeroBase.cs`

**HeroController (Singleton):**
- Initialize `HeroStatSystem`
- Auto-buff visual feedback
- Animation sync

**HeroBase:**
- Combat (targeting, projectiles, damage)
- Movement (A* pathfinding)
- Stats (gear, upgrades, buffs)
- Tasks (tracking, completion)
- Skills (selection, XP)

---

## Leaderboards

### UGS (`Assets/Scripts/Blindsided/UGS/UgsLeaderboardsReporter.cs`)
- Completion Time (+ Cheaters variant)
- Distance Reached
- Tasks

Fraud detection routes suspicious times to cheater board. Includes version metadata.

### Steam (`Assets/Scripts/Steamworks.NET/SteamLeaderboardsReporter.cs`)
- Distance
- DistanceTravelled (km)
- Kills
- Tasks

---

## Technical Architecture

### Design Patterns

**Singleton (3 approaches):**
1. `Singleton<T>` generic base class — preferred for MonoBehaviours
2. Manual `Instance` property — legacy pattern in some managers
3. Static classes — for stateless utilities (HeroStatSystem, Combat, TaskWeightService)

**Service Pattern:**
Stateless services: `CraftingService`, `SalvageService`, `TaskWeightService`, `BaseStatService`

**References Pattern (UI):**
MonoBehaviour classes in `References/` hold SerializeField UI references, decoupling logic from scene structure. 24 reference classes total.

**ScriptableObject Configuration:**
27 SO classes drive gameplay configuration. Key types:
- `Skill` — XP curve, milestones, resource unlocks
- `QuestData` — Requirements, rewards, chains
- `TaskData` — Drops, skill mapping, weights
- `EnemyData` — Stats, drops, scaling
- `MapGenerationConfig` — Terrain, tasks, enemies per map

### Object Pooling (`Assets/Scripts/Blindsided/Utilities/Pooling/PoolManager.cs`)

- Prefab-based pools (by instance ID)
- Named pools (procedural GameObjects)
- Playable Graph cleanup
- Warning at >100 inactive

**Pooled:** Echoes, Enemies, Projectiles, Floating Text, UI

### Save Data (`Assets/Scripts/Blindsided/SaveData/GameData.cs`)

**Save Format:** Odin Binary serialization with custom header (schema version, timestamp, build ID)
**Autosave:** Every 30 seconds
**Backup:** Maintains `snapshot.bin`, `snapshot.prev1.bin`, `snapshot.prev2.bin` rotation

**Key Persisted Fields:**
| Category | Fields |
|----------|--------|
| Session | PlayTime, OfflineTime, TimeScale, DateQuitString |
| Skills | SkillData (level, XP, milestones per skill) |
| Resources | Resources (amounts, earned flags, best rates) |
| Quests | Quests (progress, completion), PinnedQuests |
| Gear | EquipmentBySlot, CraftingMasteryLevel/XP |
| Cauldron | CauldronStew, CauldronCardCounts, EvaLevel/XP |
| Disciples | Disciples (offline generation records) |
| Stats | General (lifetime stats), ForgeStats, MapStats |
| Preferences | SavedPreferences (UI settings) |

**Migrations:**
| Version | Migration | Purpose |
|---------|-----------|---------|
| 1.2.12 | Migration_DuckHelmetSanitation | Fix duck helmet affixes |
| 1.2.17 | Migration_CauldronOverflowRedistribution | Cap card tiers, redistribute |
| 1.2.17 | Migration_GearAffixQuality | Populate percentile quality |

---

## UI Conventions

- Resources: whole numbers (`CalcUtils.FormatNumber(..., hideDecimal: true)`)
- Forge numbers: clean formatting
- Task completion thresholds: 10, 100, 1K, 10K, 100K for bonus multipliers

---

## Key File Reference

| System | Files |
|--------|-------|
| Resources | `Assets/Scripts/Upgrades/Resource.cs`, `ResourceManager.cs` |
| Skills | `Assets/Scripts/Skills/Skill.cs`, `MilestoneDefinition.cs` |
| Tasks | `Assets/Scripts/Tasks/[Skill]Task.cs` |
| Combat | `Assets/Scripts/Combat/Combat.cs` |
| Enemies | `Assets/Scripts/Enemies/EnemyData.cs`, `Enemy.cs` |
| Quests | `Assets/Scripts/Quests/QuestData.cs`, `QuestManager.cs` |
| Forge | `Assets/Scripts/Gear/CraftingService.cs`, `GearItem.cs`, `StatRollMath.cs` |
| Cauldron | `Assets/Scripts/Upgrades/CauldronManager.cs`, `CauldronConfig.cs` |
| Buffs | `Assets/Scripts/Buffs/BuffManager.cs`, `BuffRecipe.cs` |
| Echoes | `Assets/Scripts/Hero/EchoController.cs`, `EchoManager.cs` |
| Hero | `Assets/Scripts/Hero/HeroController.cs`, `HeroBase.cs`, `HeroStatSystem.cs` |
| Pooling | `Assets/Scripts/Blindsided/Utilities/Pooling/PoolManager.cs` |
| Save | `Assets/Scripts/Blindsided/SaveData/GameData.cs` |
| Leaderboards | `Assets/Scripts/Blindsided/UGS/UgsLeaderboardsReporter.cs` |
| Map Gen | `Assets/Scripts/MapGeneration/SegmentedMapGenerator.cs` |

---

## Map Generation System

**Flow:** `SegmentedMapGenerator` → `TilemapChunkGenerator` → `ProceduralTaskGenerator`

**Segment System:**
- 3-segment queue (64x18 tiles default)
- Oldest segment recycled when hero reaches 3rd segment
- Async coroutines spread work across frames

**Per-Map Configuration (`Assets/Scriptables/MapSettings/*.asset`):**
| Setting | Purpose |
|---------|---------|
| Terrain layers | Bottom/middle/top terrain assets |
| Task weights | Per-skill category weights |
| Enemy list | EnemyData assets to spawn |
| Enemy density | Enemies per horizontal unit |
| NPC tasks | Fixed NPC spawn positions |

**Map Status:**
| Map | Focus | Status |
|-----|-------|--------|
| Farmlands | Farming | Complete |
| Woods | Woodcutting | Complete |
| River | Fishing | Complete |
| Beach | Gathering | Complete (no enemies) |
| Mines | Mining+Looting | Complete |
| **Halloween** | Seasonal | **BROKEN - empty task lists** |

---

## Known Technical Debt

### Critical
| Issue | Location |
|-------|----------|
| Halloween map has empty task lists | `Haloween.asset` |

### Major (Large Classes)
| File | Lines | Issue |
|------|-------|-------|
| HeroBase.cs | 1545 | Mixes movement, combat, tasks, stats, UI |
| CauldronManager.cs | 1297 | Handles mixing, tasting, cards, Eva, infinity |
| ProceduralTaskGenerator.cs | 983 | Tasks, enemies, NPCs, terrain validation |
| SettingsPanelUI.cs | 1130 | Too many UI responsibilities |

### Medium
- 14 bare `catch {}` blocks in HeroBase.cs silently swallow exceptions
- Progress calculation duplicated across 4 quest files
- Inconsistent singleton patterns (3 approaches)
- Some References classes contain logic (should be data-only)
- No max collection sizes in ForgeStats (potential memory bloat)
- Pooling has no max size limit

### Minor
- 15 files cluttering Scripts/ root directory
- Empty Migration/ directory exists
- "Haloween" spelling (should be "Halloween")
- Health.cs should be renamed EnemyHealth.cs
- DistanceRun quest requirement type exists but unused
- Namespace inconsistency in References/ classes
