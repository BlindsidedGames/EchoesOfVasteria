## Echoes of Vasteria (EoV) — New Employee Systems Overview

This document consolidates the EoV systems design details. Where something is referenced but not described in detail, it is listed as **Known-but-Undocumented** so a new employee can quickly identify what to locate in the project/wiki.

---

# 1) Product Snapshot

| Field | Value |
|-------|-------|
| **Title** | Echoes of Vasteria (EoV) |
| **Platform** | Steam (released) |
| **Art/Camera** | Pixel art; top-down/isometric presentation |
| **Genre** | Incremental/idle RPG with multiple gathering/progression systems and combat-driven unlocks |
| **Current Version** | **v1.4.3** |

---

# 2) Core Economy: Resource Tiers and Where They Appear

## 2.1 The Tier Chain (Canonical)

EoV's primary tier chain is:

**Eznorb → Nori → Dlog → Erif → Lirium → Copium → Idle → Vastium**

This tiering applies across multiple item families that represent different acquisition methods and processing states.

**Key File:** `Assets/Resources/Gear/Cores/` contains ScriptableObject assets for each tier.

## 2.2 Tiered Item Families (Confirmed)

The tier chain is used across these four families, each with 8 variants:

| Family | Acquisition | Example IDs (Eznorb) |
|--------|-------------|---------------------|
| **Mining Chunks** | Mining tasks | resourceID: 9 |
| **Looting Cores** | Chest/Looting tasks | resourceID: 58 |
| **Enemy Crystals** | Combat drops | resourceID: 17 |
| **Crafted Ingots** | Smelting at Forge | resourceID: 25 |

**Additional Tiered Resources:**
- **Ores** (Mining task variants): Eznorb Ore, Nori Ore, etc.
- **Chests** (Looting task variants): Wooden Chest, Metal Chest, Golden Chest, Crystal Chest, Lirium Chest, Copium Chest, Idle Chest, Vastium Chest

**Key Files:**
- `Assets/Resources/Resource Items/` — All resource ScriptableObjects
- `Assets/Scripts/Upgrades/Resource.cs` — Resource class definition
- `Assets/Scripts/Upgrades/ResourceManager.cs` — Tier upgrade logic

## 2.3 Base (Non-tiered) Resources (Confirmed)

| Resource | ID | Primary Source |
|----------|-----|----------------|
| Slime | 1 | Slime enemies |
| Stick | 2 | Woodcutting |
| Stone | 3 | Mining |
| Bone | 4 | Skeleton enemies |
| Feather | 5 | Woodcutting, Chickens |
| Log | 6 | Woodcutting |
| Leather | 68 | Cows, Pigs |
| Egg | 67 | Chickens, Farming |
| Mutton | 65 | Sheep |
| Chicken | 66 | Chickens |
| Pork | — | Pigs |
| Steak | 69 | Cows (cooked) |

**Total catalogued resources:** 69+

---

# 3) Gathering & Life Skills

## 3.1 Skill System Architecture

**Key Files:**
- `Assets/Scripts/Skills/Skill.cs` — Skill ScriptableObject definition
- `Assets/Scripts/Skills/Milestones/MilestoneDefinition.cs` — Milestone unlocks
- `Assets/Scriptables/Skills/` — Skill asset files

**Skills Defined:**
- Farming, Fishing, Mining, Woodcutting, Combat, Looting

**Skill Properties:**
```csharp
public class Skill : SerializedScriptableObject
{
    public string skillName;
    public Sprite skillIcon;
    public float xpForFirstLevel = 10f;
    public float xpLevelMultiplier = 1.5f;
    public float taskSpeedPerLevel = 0.01f;
    public List<MilestoneDefinition> milestones = new();
}
```

**Milestone System:**
- Milestones unlock at specific skill levels
- Each milestone has passive/active effects with multiple tiers
- Two tier modes: **Manual** (fixed tiers) and **Infinite** (scaling tiers)
- Milestones include "Echo" variants that spawn temporary helper echoes

## 3.2 Woodcutting

- **Availability:** Always available from the start.
- **Products:** Stick, Log, Feather
- **Key File:** `Assets/Scripts/Tasks/WoodcuttingTask.cs`

## 3.3 Farming (Unlock Order by World Distance)

Crops unlock based on **minX** (world X position requirement):

| Order | Crop | minX | XP |
|-------|------|------|-----|
| 1 | Radish | 0 | — |
| 2 | Lettuce | 25 | — |
| 3 | Spud | 50 | — |
| 4 | Wheat | 75 | — |
| 5 | Carrot | 125 | 15 |
| 6 | Pepper | 175 | — |
| 7 | Corn | 200 | — |
| 8 | Cucumber | 225 | — |
| 9 | Funion | 250 | — |
| 10 | Leek | 275 | — |
| 11 | Tomato | 300 | — |
| 12 | Strawberry | 350 | — |
| 13 | Parsnip | 400 | — |
| 14 | Chillie | 425 | — |
| 15 | Turnip | 450 | — |
| 16 | Pumking | 500 | — |
| 17 | Watermelone | 550 | — |

**Note:** Spelling variants (Watermelone, Chillie, Pumking, Funion) are intentional in-game naming.

**Key Files:**
- `Assets/Scripts/Tasks/FarmingTask.cs`
- `Assets/Resources/Tasks/Farming/` — Crop task assets

## 3.4 Fishing (Unlock Order by World Distance)

| Order | Catch | minX | XP | Duration |
|-------|-------|------|-----|----------|
| 1 | Flippy Floppy | 0 | 2 | — |
| 2 | Sir Splashford III | 150 | 5 | 4s |
| 3 | Snapjaw Jr. | 400 | 10 | — |
| 4 | Niblet the Bold | 600 | 8 | — |
| 5 | Flipzoid | 700 | 6 | — |
| 6 | Wigglelittle | 750 | 7 | — |
| 7 | Muddy Muck Muncher | 800 | 12 | — |
| 8 | Bloopicus Maximus | 1200 | 20 | — |

**Key Files:**
- `Assets/Scripts/Tasks/FishingTask.cs`
- `Assets/Resources/Tasks/Fishing/` — Fish task assets

## 3.5 Mining

- **Products:** Stone, Tiered Chunks (Eznorb → Vastium), Tiered Ores
- **Mechanic:** Depletes ore nodes, shows depleted sprite variants
- **Key File:** `Assets/Scripts/Tasks/MiningTask.cs`

---

# 4) Combat System

## 4.1 Damage Formula

**Key File:** `Assets/Scripts/Combat/Combat.cs`

The combat system uses a **simplified armor scaling formula** with diminishing returns:

```csharp
public static float ApplyDefense(float incomingDamage, float defense, DefenseTuning tuning)
{
    float armor = Mathf.Max(0f, defense);
    float n = tuning.N > 0f ? tuning.N : DefaultArmorScalarN; // Default N = 25
    return incomingDamage * (1f - (armor / (armor + n)));
}
```

**Formula:** `damage_taken = incoming × (1 - armor/(armor + 25))`

| Armor | Damage Reduction |
|-------|-----------------|
| 0 | 0% |
| 25 | 50% |
| 50 | 66.7% |
| 100 | 80% |
| ∞ | 100% (asymptotic) |

## 4.2 Enemy Stats Structure

**Key File:** `Assets/Scripts/Enemies/EnemyData.cs`

```csharp
// Combat Stats
maxHealth, damage, defense, attackSpeed, attackRange

// Movement Stats
moveSpeed, visionRange, assistRange, wanderDistance

// Level Scaling (per level bonuses)
healthPerLevel, damagePerLevel, defensePerLevel, distancePerLevel

// Methods
GetDamageForLevel(level) → damage + damagePerLevel * level
GetDefenseForLevel(level) → defense + defensePerLevel * level
GetMaxHealthForLevel(level) → maxHealth + healthPerLevel * level
```

## 4.3 Enemy Types

**Location:** `Assets/Resources/Enemies/`

| Category | Enemies |
|----------|---------|
| **Slimes (Small)** | Blue/Green/Pink/Red/Yellow Minislime |
| **Slimes (Medium)** | Blue/Green/Pink/Red/Yellow Slime |
| **Slimes (Large)** | Blue/Green/Pink/Red/Yellow Maxislime |
| **Skeletons** | Skeleton Swordsman, Skeleton Archer, Skeleton Mage |
| **Farmlands** | Chicken, Cow, Pig, Sheep |

## 4.4 Drop System

**Key File:** `Assets/Scripts/Upgrades/DropResolver.cs`

Drop mechanics:
1. First roll selects from available drops by **weight**
2. Additional rolls use `additionalLootChances` (0-1 probabilities)
3. Amount determined with **bias towards lower values**: `t *= t` (squaring)
4. World position filtering via `minX` and `maxX`
5. Quest requirements can gate specific drops

---

# 5) Quest System and Combat-Gated Tier Unlocks

## 5.1 Quest Architecture

**Key Files:**
- `Assets/Scripts/Quests/QuestData.cs` — Quest definition
- `Assets/Scripts/Quests/QuestManager.cs` — Quest tracking

```csharp
public class QuestData : ScriptableObject
{
    public string questId;
    public LocalizedString questName;
    public LocalizedString description;
    public string npcId;
    public bool autoPin;
    public List<QuestData> requiredQuests;    // Quest dependencies
    public List<Requirement> requirements;    // What player must do
    public List<Reward> rewards;              // What player receives
    public int unlockBuffSlots;
    public int unlockAutoBuffSlots;
    public float maxDistanceIncrease;
    public float disciplePercentReward;
}
```

**Requirement Types:**
- Resource, Kill, DistanceRun, DistanceTravel, BuffCast, Instant, Meet, CriticalStrike, ResourcesGathered, TasksCompleted, CauldronMix

## 5.2 Skeleton Questline (Combat-Gated Tier Progression)

**Location:** `Assets/Resources/Quests/Enemies/`

| Quest | Enemy Type | Kill Count | Reward |
|-------|-----------|------------|--------|
| Watch Them Rattle | Skeleton Swordsmen | 100 | +50 max distance |
| Bonefire Night | Skeleton Archers | 350 | +100 max distance |
| Shiver in Your Bones | Skeleton Archers | 550 | +100 max distance |
| Rattle 'n' Roll | Skeleton Swordsmen | — | — |
| Bone to Pick | Skeleton Mages | 850 | +150 max distance |
| Grind Their Bones | Skeleton Mages | 1250 | +150 max distance |
| Beyond the Bone | Skeleton Mages | 2000 | +150 max distance |

**Tier Unlock Mechanic:** Each skeleton quest unlocks the ability for skeletons to drop the *next higher crystal/ingot tier*. Drops are gated by both quest completion AND world distance (minX).

## 5.3 Ore/Ingot Unlock Quests

**Location:** `Assets/Resources/Quests/Old Timer/Ores/`

The Old Timer NPC provides quests to unlock higher tier ores:
- Unlock Erif
- Unlock Lirium
- Unlock Copium
- Unlock Idle
- Unlock Vastium

---

# 6) NPCs and Their Roles

| NPC | Role | Location | Key Systems |
|-----|------|----------|-------------|
| **Ivan** | Craftsman | Forge | Gear crafting, Crafting Mastery XP |
| **Eva** | Witch | Cauldron | Stew mixing, Card tasting, Eva leveling |
| **Old Timer** | Miner | Mining | Ore unlocks, Mining progression |
| **Barkley** | Lumberjack | Woods | Building quests, Woodcutting |
| **Flora & Tillman** | Farmers | Farmlands | Crop unlocks |
| **Gill** | Fisherman | River/Water | Fishing quests |
| **Mildred** | — | Town | Buff slot unlocks, Echo cast unlocks |
| **Wren** | — | (Planned) | (Quest folder exists but empty) |

**Key Files:**
- `Assets/Prefabs/Tasks/NPC/` — NPC meeting task prefabs
- `Assets/Resources/Quests/[NPC Name]/` — Per-NPC quest folders
- `Assets/Art/NPCS/` — NPC artwork

---

# 7) Maps and Zones

**Location:** `Assets/Scriptables/MapSettings/`

| Map | Setting File |
|-----|--------------|
| Farmlands | Farmlands.asset |
| Woods | Woods.asset |
| River | River.asset |
| Beach | Beach.asset |
| Mines | Mines.asset |
| Halloween | Haloween.asset |

**Map Generation:**
- `Assets/Scripts/MapGeneration/SegmentedMapGenerator.cs`
- Tilemap assets in `Assets/Scriptables/Map/`

---

# 8) The Forge System

## 8.1 Crafting Process

**Key Files:**
- `Assets/Scripts/Gear/CraftingService.cs` — Main crafting logic
- `Assets/Scripts/Gear/SO/CraftingConfigSO.cs` — Configuration
- `Assets/Scripts/Gear/EquipmentController.cs` — Equipment management

**Crafting Flow:**
1. Consume ingots and core resources
2. Roll rarity using weighted distribution
3. Roll equipment slot with smart protection
4. Generate affixes based on rarity tier
5. Award Ivan XP (Crafting Mastery)

## 8.2 Rarity System

- 8 rarity tiers (indexed 0-7)
- Each rarity defines: affix count, floor percent, weight multiplier
- Level scaling: `baseWeight + core.GetRarityWeightPerLevel(r) * level`

## 8.3 Stat Quality as Percentiles (Confirmed)

**Key insight:** Items store stat quality as **normalized percentiles [0,1]** rather than absolute values.

**Why this matters:**
- Balance patches can adjust min/max stat ranges
- Items retain their **relative quality** (e.g., "90th percentile roll")
- Conversion: `value = RemapRoll(percentile)` using the stat's rollCurve

**Stat Definition Structure (`StatDefSO`):**
```csharp
- minRoll, maxRoll: Value range bounds
- rollCurve: AnimationCurve mapping quantile [0,1] → value
- rarityBands: Per-rarity quantile ranges with withinTierCurve
- floorPercent: Minimum roll quality for rarity
```

## 8.4 Equipment Slots

Default slots: **Weapon, Helmet, Chest, Boots**

**Guaranteed First Affix by Slot:**
- Boots → Move Speed
- Chest → Defense
- Helmet → Max Health
- Weapon → Damage

## 8.5 Ivan Mastery (Crafting XP)

- XP per craft based on core tier: `[2, 3, 5, 8, 13, 21, 34, 55]`
- Bonus for rolling rarity above core tier: +3 XP per step
- Level curve: `xpForFirstLevel * Mathf.Pow(currentLevel, 1.25)`

---

# 9) Cauldron and Cards System

## 9.1 Overview

**Key Files:**
- `Assets/Scripts/Upgrades/CauldronManager.cs`
- `Assets/Scripts/Upgrades/CauldronConfig.cs`

The Cauldron system has three main functions:
1. **Mixing** — Convert resources to Stew (currency)
2. **Tasting** — Spend Stew to roll cards
3. **Eva Leveling** — XP from tasting improves roll weights

## 9.2 Mixing (Resources → Stew)

```csharp
double points = amountA * a.baseValue * a.valueMultiplier
              + amountB * b.baseValue * b.valueMultiplier;
double stewGained = points / 100.0;
```

## 9.3 Tasting (Stew → Cards)

**Card Types:**
| Type | Prefix | Effect |
|------|--------|--------|
| Resource Cards | `RES:` | Unlock/upgrade Alter Echo disciples |
| Buff Cards | `BUFF:` | Upgrade buff power/cooldown |
| Infinity Cards | `INF:` | Eternal stat bonuses (no cap) |
| Lowest Card | — | Fills weakest card |
| Eva's Blessing | — | 2 random cards |
| Vast Surge | — | 10 random cards |

**Tier Thresholds:**
- Resource tiers: `[1, 5, 20, 50, 100, 200, 350, 500]`
- Buff tiers: `[1, 3, 10, 25, 50, 100, 200, 300]`

**Per-Tier Bonuses:**
- Resource Power: `[10, 25, 50, 75, 120, 180, 250, 400]%`
- Buff Cooldown Reduction: `[5, 10, 15, 25, 40, 60, 80, 100]%`
- Buff Power Bonus: `[0, 0, 5, 10, 15, 20, 25, 30]%`

## 9.4 Infinity (Eternal Boons)

- Unlocks when **all** regular cards are maxed
- Uses exponential scaling: `value = CardCount ^ exponent`
- No cap — infinite progression
- Affects hero stats directly

---

# 10) Alter Echo System (Disciples)

**Key Files:**
- `Assets/Scripts/NpcGeneration/AlterEchoGenerationManager.cs`
- `Assets/Scripts/NpcGeneration/AlterEchoGenerator.cs`
- `Assets/Scripts/NpcGeneration/AlterEcho.cs`

**What It Is:** Passive offline resource generation system. Each unlocked resource gets a "disciple" that generates resources over time.

**Generation Rate:**
```csharp
rate = BestPerMinute * DisciplePercent * CauldronMultiplier
```

**Offline Progress:**
- Capped by `OfflineTimeCap` (default 1 hour)
- Multiplied by `OfflineTimeScaleMultiplier` (default 2x)

**Data Storage:**
```csharp
public Dictionary<string, DiscipleGenerationRecord> Disciples = new();
public float DisciplePercent = 0.01f; // Quest reward accumulation
```

---

# 11) Buff System

## 11.1 Architecture

**Key Files:**
- `Assets/Scripts/Buffs/BuffManager.cs`
- `Assets/Scripts/Buffs/BuffRecipe.cs`
- `Assets/Scripts/Buffs/BuffTypes.cs`

**Buff Effect Types (15):**
- MoveSpeedPercent, DamagePercent, DefensePercent
- AttackSpeedPercent, TaskSpeedPercent
- HealthRegenPercent, CritChancePercent, CritDamagePercent
- MaxDistancePercent, MaxDistanceIncrease
- InstantTasks, TimeScalePercent
- ResourceMultiplier, ExperienceBonusFraction
- DistanceDurationPercent

## 11.2 Duration Types

1. **Time** — Duration in seconds, affected by power multiplier
2. **DistancePercent** — Expires at `(LongestRun × duration)` distance

## 11.3 Auto-Buff System

- Up to 5 auto-cast slots (unlocked via quests)
- Toggle per-slot via `ToggleSlotAutoCast(slot)`
- Auto-casts when: tasting inactive, run in progress, not loading
- Saved to `oracle.saveData.AutoBuffSlots`

## 11.4 Cooldown Mechanics

- Starts **after** buff expires (not concurrent)
- Exception: MaxDistance buffs start cooldown **during** active
- Reduced by Cauldron tier: `cooldown *= Max(0, 1 - reductionPercent/100)`
- **Reset on death** (all cooldowns clear)
- **Smart retreat logic:** Long runs don't charge cooldowns

---

# 12) Player Stats and Diminishing Returns

## 12.1 Movement Speed

**Key File:** `Assets/Scripts/Hero/Stats/HeroStatSystem.cs`

```csharp
const float BaseMoveSpeed = 3f;      // Minimum speed
const float MoveBonusRange = 6f;     // Max additional speed
const float MovementScalarN = 25f;   // DR curve strength

var scale01 = movementRating / (movementRating + MovementScalarN);
var finalSpeed = BaseMoveSpeed + MoveBonusRange * scale01;
```

**Formula:** `Speed(r) = 3 + 6 × r/(r+25)`

| Rating | Speed | % of Max Bonus |
|--------|-------|----------------|
| 0 | 3.0 | 0% |
| 25 | 6.0 | 50% |
| 50 | 7.0 | 66.7% |
| 100 | 7.8 | 80% |
| ∞ | 9.0 | 100% |

## 12.2 Defense

**Note:** Defense stat itself has **NO diminishing returns** in accumulation. It's purely additive from base + gear + infinity + buffs.

The diminishing returns occur in the **damage formula** (see Section 4.1), not in stat stacking.

---

# 13) Echo System (Temporary Helper Clones)

## 13.1 What Are Echoes?

**Key Files:**
- `Assets/Scripts/Hero/EchoController.cs`
- `Assets/Scripts/Hero/EchoManager.cs`
- `Assets/Scripts/Hero/EchoType.cs`

**Echoes are temporary helper clones** of the hero spawned through skill milestones or buff effects. They:
- Have configurable lifetimes
- Can be restricted to specific skills or combat-only
- Participate in combat, gathering, or both
- Are subject to population caps (default 10 per type)
- Use A* pathfinding like the hero
- Display duration UI bars (green → yellow → red)

## 13.2 Echo Types

```csharp
public enum EchoType
{
    Combat,      // Combat only, ignores tasks
    All,         // Can fight and complete any task
    TaskOnly,    // Never engages in combat
    Selective    // Only performs listed skills
}
```

## 13.3 Pooling

Echoes use object pooling for performance:
- **Key File:** `Assets/Scripts/Blindsided/Utilities/Pooling/PoolManager.cs`
- Spawned via `PoolManager.Get(gm.EchoPrefab)`
- Lifetime managed with optional extension for finishing current task/combat

---

# 14) Hero Controller Architecture

**Key Files:**
- `Assets/Scripts/Hero/HeroController.cs` — Singleton concrete implementation
- `Assets/Scripts/Hero/HeroBase.cs` — Base class with core functionality

**HeroController Responsibilities:**
1. Singleton management (static `Instance`)
2. Initialize centralized `HeroStatSystem`
3. Auto-buff visual feedback (animator sync)
4. Animation synchronization (movement, attacks, sprite flipping)

**HeroBase Responsibilities:**
- Combat: Targeting, projectile firing, damage application
- Movement: A* pathfinding, task navigation, idle wandering
- Stat System: Gear bonuses, upgrade application, buff multipliers
- Task Interaction: Current task tracking, completion
- Skills: Combat skill selection, XP granting
- Equipment: Damage, defense, health bonuses from gear

---

# 15) Leaderboard System

**Key Files:**
- `Assets/Scripts/Blindsided/UGS/LeaderboardClient.cs`
- `Assets/Scripts/Blindsided/UGS/UgsLeaderboardsReporter.cs`
- `Assets/Scripts/Blindsided/UGS/UgsLeaderboardIds.cs`

**Leaderboard Types:**
1. **Completion Time** — Seconds to complete (lower is better)
2. **Distance Reached** — Map distance progression
3. **Tasks Completed** — Total task count

**Features:**
- Async submission via Unity Gaming Services
- Metadata includes version info
- Separate cheater variant boards
- Avoids resubmitting unless better score

---

# 16) Technical Architecture Notes

## 16.1 Object Pooling

**Key File:** `Assets/Scripts/Blindsided/Utilities/Pooling/PoolManager.cs`

Uses Unity's `ObjectPool<T>` with:
- Prefab-based pools (by instance ID)
- Named pools (for procedural GameObjects)
- Playable Graph cleanup before pooling
- Warning threshold at >100 inactive instances

**Pooled Objects:**
- Echoes, Enemies, Projectiles, Floating Text, UI Elements

## 16.2 Save Data

**Key File:** `Assets/Scripts/Blindsided/SaveData/GameData.cs`

Key persisted data:
- `Disciples` — Alter Echo generation records
- `CauldronTotals` — Lifetime tasting stats
- `AutoBuffSlots` — Per-slot auto-cast state
- `CraftingMasteryLevel`, `CraftingMasteryXP` — Ivan progression
- `CauldronEvaLevel`, `CauldronEvaXp` — Eva progression

---

# 17) UI/UX Conventions

- Resources display as **whole numbers** (floored, no decimals)
- Forge numbers formatted cleanly
- Graph tooltips widened for readability
- Task completion thresholds: 10, 100, 1,000, 10,000, 100,000 for bonus multipliers

---

# 18) Content/Data Conventions

- **Stable content keys** used throughout (spreadsheets/JSON/ScriptableObjects)
- Quest keys follow pattern: `QuestName.*` with variants for name/description/reward
- Localization via Unity's `LocalizedString`
- Any renames require migration handling

---

# 19) What a New Employee Should Be Able to Explain After Day 1

1. **The economic tier chain** (Eznorb → Vastium) spans 4 item families (chunks/cores/crystals/ingots).
2. **Life-skill unlock chains** are gated by world distance (minX), with Woodcutting always available.
3. **Combat quests gate tier progression** — Skeleton questline unlocks higher tier drops.
4. **The Forge stores stat quality as percentiles** for balance resilience across patches.
5. **Movement Speed uses diminishing returns** (`r/(r+25)` formula); Defense does not stack with DR.
6. **Cauldron/Cards** provide permanent progression via Stew → Tasting → Cards → Tier bonuses.
7. **Echoes are pooled temporary hero clones** with type-based behavior restrictions.
8. **Alter Echo (Disciples)** provide passive offline resource generation.

---

# 20) Key File Reference

| System | Primary Files |
|--------|---------------|
| Resources | `Assets/Scripts/Upgrades/Resource.cs`, `ResourceManager.cs` |
| Skills | `Assets/Scripts/Skills/Skill.cs`, `MilestoneDefinition.cs` |
| Tasks | `Assets/Scripts/Tasks/[Skill]Task.cs` |
| Combat | `Assets/Scripts/Combat/Combat.cs` |
| Enemies | `Assets/Scripts/Enemies/EnemyData.cs`, `Enemy.cs` |
| Quests | `Assets/Scripts/Quests/QuestData.cs`, `QuestManager.cs` |
| Forge/Gear | `Assets/Scripts/Gear/CraftingService.cs`, `GearItem.cs` |
| Cauldron | `Assets/Scripts/Upgrades/CauldronManager.cs` |
| Buffs | `Assets/Scripts/Buffs/BuffManager.cs`, `BuffRecipe.cs` |
| Echoes | `Assets/Scripts/Hero/EchoController.cs`, `EchoManager.cs` |
| Hero | `Assets/Scripts/Hero/HeroController.cs`, `HeroBase.cs` |
| Stats | `Assets/Scripts/Hero/Stats/HeroStatSystem.cs` |
| Pooling | `Assets/Scripts/Blindsided/Utilities/Pooling/PoolManager.cs` |
| Save Data | `Assets/Scripts/Blindsided/SaveData/GameData.cs` |
| Leaderboards | `Assets/Scripts/Blindsided/UGS/LeaderboardClient.cs` |

---

# 21) Gaps to Fill (Internal Wiki/Docs Targets)

The following should be documented from canonical sources:

- [ ] Core gameplay loop in minute-to-minute terms
- [ ] Complete damage formula with crit, elemental, etc.
- [ ] Full equipment affix list and rarity distributions
- [ ] Save schema migrations (especially Forge percentile change)
- [ ] Economy sinks (where resources are spent)
- [ ] Live balancing approach (data-driven vs code-driven)
- [ ] Telemetry/analytics if any

---

## Exploration Notes (Raw Findings)

### Corrections Made to Original Document:

1. **Version:** Changed from "v1.2.x → v1.3" to **v1.4.3** (current bundleVersion)

2. **Farming Order:** The original order was incorrect. Actual order is by minX world distance:
   - Original claimed: Radish → Corn → Wheat → Watermelone → Carrot...
   - Actual: Radish → Lettuce → Spud → Wheat → Carrot → Pepper → Corn...

3. **Fishing Order:** The original order was also incorrect:
   - Original claimed: Flippy Floppy → Muddy Muck Muncher → Sir Splashford III...
   - Actual: Flippy Floppy → Sir Splashford III → Snapjaw Jr. → Niblet the Bold...

4. **Skeleton Kill Counts:** The original list was incomplete/incorrect:
   - Original: 100, 200, 350, 550, 850, 1250, 2000
   - Actual varies by enemy type (Swordsmen: 100, Archers: 350/550, Mages: 850/1250/2000)

5. **Defense DR:** Original claimed Defense uses same DR as Movement Speed — **incorrect**. Defense has no built-in DR; the DR occurs in the damage formula.

6. **Altar vs Alter:** There is no "Altar" system. It's called "Alter Echo" (disciples for passive generation).

7. **Additional base resources found:** Egg, Chicken, Mutton, Pork, Steak (not in original list)

8. **NPCs discovered:** Ivan, Eva, Old Timer, Barkley, Flora, Tillman, Gill, Mildred, AvoDude, Wren (planned)

9. **Maps discovered:** Farmlands, Woods, River, Beach, Mines, Halloween (6 total)
