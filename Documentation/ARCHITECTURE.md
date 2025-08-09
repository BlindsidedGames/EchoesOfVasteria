# Architecture

## Core Loop
- The player starts in town. Pressing a `MapGenerationButton` begins a run via `GameManager.StartRun`.
- A `Map` prefab is instantiated with `SegmentedMapGenerator`, `TaskController`, and generation components.
- `TaskController` builds a task list (from editor-assigned objects and/or `ProceduralTaskGenerator`). The `HeroController` automatically moves (A* Pathfinding Project) to the earliest available task and completes it.
- Enemies may engage the hero. Combat uses simple projectile attacks and level-based enemy stats.
- The run ends when the hero dies, the reaper catches up, or the player retreats. Stats are recorded and the player returns to town.

## Key Systems

### GameManager (`Assets/Scripts/GameManager.cs`)
- Singleton orchestrating run lifecycle, map instantiation, camera/UI toggling, and death window flow.
- Tracks current config (`MapGenerationConfig`), connects to `GameplayStatTracker`, and coordinates Steam Rich Presence when available.

### Map Generation
- `SegmentedMapGenerator`: Streams segments as the hero advances, recycles old segments, and moves the A* grid to cover visible chunks.
- `TilemapChunkGenerator`: Produces terrain tiles (water/sand/grass) and decor. Clears decor where tasks spawn to avoid overlaps.
- `ProceduralTaskGenerator`: Emits tasks between [minX, maxX] for each segment using per-terrain spawn rules.

### Tasks (`Assets/Scripts/Tasks/*`)
- `TaskController`: Central coordinator for tasks, owning the ordered `tasks` list and pruning logic. Converts enemies into implicit `KillEnemyTask`s as needed.
- Task implementations: `MiningTask`, `FishingTask`, `FarmingTask`, `WoodcuttingTask`, `TalkToNpcTask`, `OpenChestTask`, etc. Most derive from `BaseTask` and expose a `Target` transform and `IsComplete()`.
- Backtracking: `backtrackingAdditionalWeight` biases selection to tasks behind the hero.

### Hero (`Assets/Scripts/Hero/*`)
- `HeroController`: Drives movement and behavior state machine (Idle/Task/Combat). Calculates effective stats from base values, upgrades, and active buffs; exposes properties like `Damage`, `MoveSpeed`, `Defense`, `AttackRate`, and `MaxHealthValue`.
- `HeroHealth`, `HeroStats`, `HeroAudio`, `EchoController`, `EchoManager`: Health model, permanent stat upgrades, SFX integration, time-limited clone behavior and spawning.

### Enemies (`Assets/Scripts/Enemies/*`)
- `Enemy`: Handles level calculation from world X, target selection (hero or echoes), wandering, animation parameters, and projectile attacks. On death, awards resources via `ResourceManager` and updates trackers.
- `EnemyData`: Scriptable stats including damage, vision/attack ranges, move speed, drop tables, and name.
- `EnemyActivator`: Activates/deactivates enemies near the camera for performance.

### Buffs (`Assets/Scripts/Buffs/*`)
- `BuffManager`: Singleton, persists active timed/distance-based buffs, exposes multipliers used by hero and task logic. Supports auto-cast slots unlocked via quests. Spawns echoes when a buff recipe requests it.
- `BuffRecipe`: Scriptable definition of effects (damage/defense/move/attack speed %, lifesteal %, distance bonuses, instant tasks, echo spawns, etc.).

### Quests (`Assets/Scripts/Quests/*`)
- `QuestManager`: Loads quests, tracks progress (resources, kills, distance, casts, meets), manages UI entries, completion, and rewards (e.g., unlocking buff slots, increasing max distance).
- Integrations: `ResourceManager` (resource goals), `EnemyKillTracker` (kill goals), `GameplayStatTracker` (distance goals), `DiscipleGenerationManager` (NPC generation rates), and Steam achievements.

### Upgrades & Resources (`Assets/Scripts/Upgrades/*`)
- `ResourceManager`: Central wallet for all resources, used by tasks, enemy drops, quests, and UI. UI references under `Assets/Scripts/References/UI/*` display spendable resources and costs.
- `StatUpgrade`, `StatUpgradeController`, `StatUpgradeUIManager`: Purchasing permanent hero stat upgrades.

### UI (`Assets/Scripts/UI/*`)
- Core panels: `MapUI`, task/quest/buff/resource panels, settings and window toggles. Many are thin presenters bound via `*UIReferences` components under `Assets/Scripts/References`.
- `TownWindowManager`: Opens/closes in-town windows when starting/ending runs.

### Audio (`Assets/Scripts/Audio/*`)
- `AudioManager`: Initializes mixers, applies saved volumes, provides helpers for task/combat/hero/chest SFX and looping music.
- `SfxPlayer`: Static helper outputting to the SFX mixer group.

### Stats & Tracking (`Assets/Scripts/Stats/*`)
- `GameplayStatTracker`: Records run metrics (distance, kills, damage dealt/taken, buffs cast, deaths, etc.), surfaces map statistics for lobby buttons, and persists across runs.

### Steam Integration (`Assets/Scripts/Steamworks.NET/*`)
- `SteamManager`, `AchievementManager`, `SteamStatsUpdater`, `RichPresenceManager`, `SteamLanguageLocaleSelector`, `SlimeCombatTracker`: Wrapped behind compile-time defines. Game works without Steam.

## Data Flow & Singletons
- Singletons: `GameManager`, `BuffManager`, `QuestManager`, `AudioManager`, `GameplayStatTracker`, `ResourceManager`, and several UI managers use the common pattern `Instance` with defensive null logging.
- Events: 
  - `GameplayStatTracker.OnRunEnded`, `OnDistanceAdded`
  - `Enemy.OnEngage`
  - `ResourceManager.OnInventoryChanged`
  - `BaseTask.TaskCompleted`
- Save/Load: The `Blindsided.SaveData` layer provides `Oracle` and `StaticReferences` for persistence and global knobs (e.g., volumes, pinned quests), with `OnLoadData` for late application.

## External Packages
- A* Pathfinding Project (`Pathfinding.*`): AIPath, AIDestinationSetter, RVO for movement/avoidance.
- Steamworks.NET: Achievements, stats, and Rich Presence (behind platform defines).
- Sirenix Odin Inspector: Editor attributes in scripts.

## Runtime Sequence (Typical Run)
1. Player clicks a map button -> `GameManager.StartRun(config)`.
2. Instantiate map -> `SegmentedMapGenerator` creates 3 segments, `TaskController.ResetTasks()` builds the task list.
3. `HeroController` ticks, pulling distance into `GameplayStatTracker`, applying `BuffManager` effects.
4. Enemies choose targets; hero engages when in range; resources drop on enemy death.
5. Player retreats, dies, or reaches distance cap (reaper spawns) -> `GameManager` ends the run, shows death window if needed, toggles UI.

## Extensibility
- Add a new Task: create a `BaseTask`-derived MonoBehaviour, add to a segment or prefab; `TaskController` will register and manage it.
- Add a new Enemy: add an `Enemy` with `EnemyData` and (optionally) add to procedural spawns. Drops are driven by `EnemyData.resourceDrops`.
- Add a new Buff: create a `BuffRecipe` asset; reference it in UI or quests. Effects aggregate in `BuffManager`.
- Add a new Quest: create a `QuestData` asset under `Resources/Quests` with requirements and rewards.