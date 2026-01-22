# Echoes of Vasteria (EoV) - New Employee Systems Overview

This overview summarizes the runtime systems, progression rules, and data assets that define EoV's core loop, with code-backed references for onboarding.

## Product Snapshot
- Title: Echoes of Vasteria.
- Platform: Steam builds with Steamworks.NET integration.
- Genre: incremental hero management game with auto-running map sessions.
- Presentation: 2D top-down pixel art driven by tilemaps and A* navigation.
- Engine: Unity 6000.2.6f2 in `ProjectSettings/ProjectVersion.txt` (repo guidance still references 6000.2.1f1/6000.1.6f1, confirm target before upgrading).

## Core Loop (Runtime)
EoV runs as a sequence of auto-played maps. Each run spawns a `Map` prefab, `TilemapChunkGenerator` plus `ProceduralTaskGenerator` build terrain and task nodes, and `TaskController` assigns the earliest unfinished task (ordered by world X) while the hero uses A* pathfinding to travel and fight. Tasks grant resources and XP, enemies can drop gear, chests always contain gear, and offline runs continue while the game is closed. See `Assets/Scripts/MapGeneration/*`, `Assets/Scripts/Tasks/*`, and the overview in `README.md`.

## Economy and Tier Chain
The primary tier chain used across resources and gear is:

Eznorb -> Nori -> Dlog -> Erif -> Lirium -> Copium -> Idle -> Vastium

The chain appears in multiple families:
- Mining Chunks: ore tasks in `Assets/Resources/Tasks/Mining/*` drop tiered Chunks.
- Looting Cores: chest tasks in `Assets/Resources/Tasks/Looting/*` drop tiered Cores.
- Enemy Crystals: enemy drops in `Assets/Resources/Enemies/*`.
- Crafted Ingots: forge conversion turns Chunks + Crystals into Ingots, which feed gear crafting (`Assets/Resources/Resource Items/*`, `Assets/Resources/Gear/Cores/*`).
- Gear rarities and cores: `Assets/Resources/Gear/Rarity Assets/*` and `Assets/Resources/Gear/Cores/*` share the same tier names.

## Base Resources and Drops
Baseline resources used early include Stone, Stick, Log, Slime, Bone, Leather, and Feather. Farm animals add Chicken, Egg, Mutton, Pork, and Steak. Resource assets live in `Assets/Resources/Resource Items`.

## Tasks and Life Skills
Task availability and ordering are driven by `TaskData` assets in `Assets/Resources/Tasks`. Tasks can be gated by `requiredQuest`, and each task links to a skill for XP.

### Woodcutting
Woodcutting uses Medium/Large tree variants (oak, birch, spruce). All woodcutting tasks require the Barkley quest "Another Magicool Gift" and drop Stick and Log (`Assets/Resources/Tasks/Woodcutting/*`).

### Farming
Farming tasks unlock via Flora and Tillman quests named "Unlock <Crop>" and follow task ID order. Canonical crop names (player-facing, per resource assets and localization) are:

Radish -> Corn -> Wheat -> Watermelone -> Carrot -> Spud -> Tomato -> Lettuce -> Cucumber -> Leek -> Parsnip -> Pepper -> Chillie -> Pumking -> Strawberry -> Funion -> Turnip

Note: the farming task/prefab for Watermelone is spelled Wartermelone, and some quest IDs use Unlock Chilli / Unlock Pumpking while task/resource assets use Chillie / Pumking. Normalize display text to the player-facing spellings but keep the ID mismatches in mind when wiring data.

Onion appears in localization and `Assets/Scenes/Main.unity`, but there is no task or resource asset yet, so treat it as pending content.

### Fishing
Fishing tasks are Flippy Floppy, Muddy Muck Muncher, Sir Splashford III, Wigglelittle, Snapjaw Jr., Flipzoid, Niblet the Bold, and Bloopicus Maximus. All eight are gated by the single quest "Finishing Touches" (not a sequential chain).

### Mining
Mining tasks are Eznorb Ore and Nori Ore (unlocked by default), then Dlog Ore (quest "Chunky Business"), followed by Unlock Erif, Unlock Lirium, Unlock Copium, Unlock Idle, and Unlock Vastium quests. Each ore task drops Stone plus its tiered Chunks.

### Looting
Looting tasks are Eznorb through Vastium chests. Chests drop Leather plus cores up to their tier. Looting tasks have no required quest gating in their TaskData assets.

## Combat, Enemies, and Drops
Enemy drops are defined in `Assets/Resources/Enemies/*`:
- Skeletons drop Bone plus tiered Crystals; higher-tier crystals require skeleton quest completions.
- Slimes drop Slime, Stone, and tiered Crystals.
- Farm animals drop Chicken/Egg/Feather (Chicken), Mutton (Sheep), Pork (Pig), and Steak (Cow).

Enemies can drop gear, while chests always contain gear (see `README.md`).

## Skeleton Questline (Crystal Tier Unlocks)
Skeleton crystal tiers are gated by a kill quest chain in `Assets/Resources/Quests/Enemies`. The required quest chain does not strictly increase kill counts, so use the required quest links rather than only the numbers.

| Quest ID | Requires | Target | Kills | Crystal Unlock | Max Distance Increase |
| --- | --- | --- | --- | --- | --- |
| Watch Them Rattle | Rock and Stone | Skeleton Swordsmen | 100 | Eznorb + Nori | 50 |
| Bonefire Night | Watch Them Rattle | Skeleton Archers | 350 | Erif | 100 |
| Rattle 'n' Roll | Bonefire Night | Skeleton Swordsmen | 200 | Dlog | 50 |
| Shiver in Your Bones | Rattle 'n' Roll | Skeleton Archers | 550 | Lirium | 100 |
| Bone to Pick | Shiver in Your Bones | Skeleton Mages | 850 | Copium | 150 |
| Grind Their Bones | Bone to Pick | Skeleton Mages | 1250 | Idle | 150 |
| Beyond the Bone | Grind Their Bones | Skeleton Mages | 2000 | Vastium | 150 |

Localization keys for these quests live in `Assets/Localization/Tables/Quests/Quests Shared Data.asset` (compact forms like BonefireNight.* and ShiverInYourBones.*).

## Crafting and Forge
Gear crafting uses ingots plus cores. The forge supports ingot conversion and gear rolls, and it records roll quality as a normalized quantile to preserve balance when stat ranges change.

- Ingot conversion: `CoreSO` defines `requiredIngot`, `chunkCostPerIngot`, and `crystalCostPerIngot`. The forge converts Chunks + Crystals into Ingots in `Assets/Scripts/Gear/UI/ForgeWindowUI/ForgeWindowUI.cs`.
- Gear crafting: crafting consumes Ingots plus Cores (`Assets/Resources/Gear/Cores/*`) and rolls affixes by rarity (`Assets/Scripts/Gear/CraftingService.cs`).
- Quality storage: affixes are saved as `quality` in [0,1] and rehydrated through `StatRollMath` (`Assets/Scripts/Gear/EquipmentController.cs`, `Assets/Scripts/Gear/StatRollMath.cs`). Migration `Migration_GearAffixQuality` backfills quality for legacy saves (1.2.18).
- Crafting tier integrity: avoid logic that promotes resources to higher tiers than their recipes allow.

## Player Stats and Diminishing Returns
Defense uses a single global formula in `Assets/Scripts/Combat/Combat.cs`: `damage * (1 - defense/(defense + 25))`. Movement speed uses the same diminishing-returns shape in `HeroStatSystem`: it converts a rating into a speed bonus (base 3, bonus range 6, scalar N=25) before applying buffs. The Move Speed stat is a flat rating, not a percent (`Assets/Resources/Gear/StatDef/Move Speed.asset`).

## Cauldron, Cards, and Infinity (Eternal)
The Cauldron mixes resources into stew, then auto-rolls cards at a configurable rate to grant Alter-Echo resource cards, buff cards, and late-game Infinity (Eternal) cards.

- Mixing: two resources are consumed and converted to stew based on their `baseValue` and `valueMultiplier` (`Assets/Scripts/Upgrades/CauldronManager.cs`).
- Tasting loop: rolls per second and stew per roll are defined in `Assets/Resources/Cauldron/CauldronConfig.asset`. Eva gains XP (50 + 10*(level-1)) and level affects weights.
- Card types:
  - RES:<ResourceName> Alter-Echo cards, grouped by Farming/Fishing/Mining/Woodcutting/Looting/Combat.
  - BUFF:<BuffName> buff cards.
  - Lowest, Eva's Blessing x2, Vast Surge x10, and Nothing outcomes.
- Tiers: resource card tiers boost Alter-Echo generation rates; buff tiers reduce cooldowns and increase buff power; the Buffs group tier is the minimum tier among unlocked buffs.
- Infinity/Eternal: INF:<Stat> cards become eligible only when all normal cards are maxed. Each Infinity stat uses `InfinityCauldronStatSO` and contributes `count^exponent` to hero stats.
- Unlock: the quest "Unlock Cauldron" (ResourcesGathered 50,000) gates Cauldron access.

## Echoes and Alter Echoes
Echoes are clone helpers spawned by milestones. `EchoManager` uses pooling to spawn echoes, enforces per-type caps, and configures whether they fight, perform tasks, or follow the hero when idle (`Assets/Scripts/Hero/EchoManager.cs`). Alter-Echo generation is a separate system in `Assets/Scripts/NpcGeneration/*` that is driven by Cauldron resource card tiers and grouped by skill category.

## Leaderboards
Leaderboards are implemented for both UGS and Steam.

- UGS: Completion Time, Completion Time (Cheaters), Distance Reached, and Tasks (`Assets/Scripts/Blindsided/UGS/UgsLeaderboardsReporter.cs`, `Assets/Scripts/Blindsided/UGS/UgsLeaderboardIds.cs`). Fraud detection routes suspicious completion times to the cheater board and includes game version metadata.
- Steam: Distance, DistanceTravelled (km), Kills, and Tasks (`Assets/Scripts/Steamworks.NET/SteamLeaderboardsReporter.cs`).

## Save Data and Migrations
Save data tracks game version, Cauldron totals, and gear quality. Migration `Migration_GearAffixQuality` (target 1.2.17/1.2.18) backfills affix quality values, and Cauldron totals/card counts live in `GameData` (`Assets/Scripts/Blindsided/SaveData/*`).

## UI Formatting and Readability
Resource counts in the inventory UI are formatted with `CalcUtils.FormatNumber(..., hideDecimal: true)`, which truncates fractional values for readability (`Assets/Scripts/Upgrades/ResourceInventoryUI.cs`, `Assets/Scripts/Blindsided/Utilities/CalcUtils.cs`).