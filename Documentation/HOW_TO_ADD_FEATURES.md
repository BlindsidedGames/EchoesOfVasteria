# How To Add Common Features

## New Task Type
1. Create a MonoBehaviour that implements `ITask` (or derive from `BaseTask`).
2. Expose a `Transform Target` and implement `IsComplete()`; raise `TaskCompleted` when done.
3. Place your component under a segment/prefab in the map. If added at runtime, call `TaskController.AddRuntimeTaskObject`.
4. Optionally add spawn rules to `ProceduralTaskGenerator`/terrain settings so it’s generated procedurally.

## New Enemy
1. Create an `EnemyData` ScriptableObject with base stats, attack range/speed, and drop table.
2. Create a prefab with `Enemy`, `AIPath`, `AIDestinationSetter`, `RVOController`, and a `Health` component.
3. Assign your `EnemyData` to the `Enemy` and set the projectile prefab/origin.
4. Add your enemy to procedural spawns or place in scenes; `TaskController` will attach a `KillEnemyTask` automatically.

## New Buff
1. Create a `BuffRecipe` asset and choose effects (percent bonuses, lifesteal, instant tasks, distance modifiers, echo spawns).
2. Reference the recipe in UI or quests. `BuffManager` aggregates effects and exposes multipliers.
3. For auto-cast, ensure quests unlock slots (`unlockBuffSlots`, `unlockAutoBuffSlots`).

## New Quest
1. Create a `QuestData` under `Resources/Quests` with requirements (Resource/Kill/Distance/Instant/Meet/BuffCast) and rewards.
2. Set dependencies in `requiredQuests` and optional NPC gating via `npcId`.
3. UI: `QuestUIManager` auto-creates entries; pinning is handled by `PinnedQuestUIManager`.

## New Permanent Stat Upgrade
1. Create `StatUpgrade` assets and list them in the `StatUpgradeController`.
2. Wire UI via `StatUpgradeUIManager` and `*UIReferences`.

## Add SFX/Music
1. Put clips in the project and reference them in `AudioManager` arrays.
2. Use `AudioManager.Instance.Play*` helpers for tasks/combat/hero events.

## Extend Map Generation
1. Update `TilemapChunkGenerator` decor and task settings per terrain type.
2. Adjust `SegmentedMapGenerator.segmentSize` and ensure the A* grid dimensions update appropriately.

## Tips
- Prefer using existing singletons via `Instance` accessors; log if null.
- For save data, write to `Blindsided.Oracle.saveData` via provided records and rely on `OnLoadData` hooks to apply on load.
- Keep code in `Assets/Scripts` and avoid modifying third-party packages under `Packages/` unless vendoring fixes intentionally.