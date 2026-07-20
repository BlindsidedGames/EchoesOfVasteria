# Echoes of Vasteria Code and Performance Audit

Date: 2026-05-08

## Scope

This audit is a documentation-only discovery pass over the current Unity project at `C:\Users\mattr\Documents\Unity\Projects\Echoes of Vasteria`. I inspected runtime scripts, tests, README/version files, package metadata, and recent `DevStuff/Changes.md` notes. No scripts, scenes, prefabs, packages, meta files, or `Changes.md` were modified.

The focus was code quality, architecture risk, and performance in hot paths: `Update`, `LateUpdate`, coroutines, UI refresh loops, spawning, pathfinding, combat, map generation, save/load, event systems, and test coverage. Recent notes show substantial work already landed for Forge autocrafting, Cauldron allocations, UITicker, HeroBase cleanup, autosave verification, and A* package warnings, so this report does not treat those older issues as still broken without current evidence.

Unity/API references checked against official documentation:

- Unity 6000.5 deprecates `FindFirstObjectByType`; use `FindAnyObjectByType` when an arbitrary matching object is sufficient: https://docs.unity3d.com/ScriptReference/Object.FindAnyObjectByType.html
- Unity 6000.5 provides `FindObjectsByType` overloads that no longer require the legacy sort-mode argument: https://docs.unity3d.com/ScriptReference/Object.FindObjectsByType.html
- `Tilemap.SetTilesBlock` is the intended batch tile placement API versus individual `SetTile` calls: https://docs.unity3d.com/ScriptReference/Tilemaps.Tilemap.SetTilesBlock.html
- Cinemachine 3 uses the `CinemachineCamera` component: https://docs.unity.cn/Packages/com.unity.cinemachine@3.1/manual/CinemachineCamera.html

## System Map

- Runtime loop: `GameManager` owns run start/return, map instantiation, cleanup, loading overlay, fast-forward, stall monitoring, hero death, and immediate saves.
- Map generation: `SegmentedMapGenerator` coordinates `TilemapChunkGenerator`, `ProceduralTaskGenerator`, `TaskController`, segment pooling, tile writes, task/enemy spawning, and A* grid scans.
- Task flow: `TaskController` builds and prunes task lists, dynamically adds enemy kill tasks, sorts by proximity/backtracking rules, and assigns tasks to heroes/echoes.
- Hero/combat: `HeroBase` drives per-frame movement, animation, task/combat state, distance recording, and rich presence throttling. `HeroCombatController` scans active enemies, estimates DPS/overkill, and pools projectiles.
- Enemy flow: `EnemyActivator` tracks registered enemies, toggles active logic based on camera/combatant proximity, and exposes `ActiveEnemies` for combat target scans.
- UI: Recent systems mix event-driven panels, deprecated-but-retained `UITicker`, coroutines, and direct `Update` loops. Some remaining UI state still polls.
- Persistence: `Oracle` handles autosave and slot metadata. `SaveManager.SaveAsync` and `LoadAsync` use Odin binary snapshots, backup rotation, and lightweight size verification, but the methods currently complete synchronously.
- Tests: Existing tests are mostly EditMode for isolated logic, with one PlayMode stress suite for save files. Test asmdefs are constrained to `UNITY_INCLUDE_TESTS`, and current test scripts are wrapped in `#if UNITY_INCLUDE_TESTS`.

## Top Opportunities

### 1. Replace full A* rescans on every segment shift

Files: `Assets/Scripts/MapGeneration/SegmentedMapGenerator.cs:155`, `Assets/Scripts/MapGeneration/SegmentedMapGenerator.cs:198`, `Assets/Scripts/MapGeneration/SegmentedMapGenerator.cs:289`

Evidence: Segment shifts clear one old segment, create one new segment, wait for colliders, then call `MoveGraph()`. After the first setup, `MoveGraph()` still pauses pathfinding and calls `astar.Scan(gg)` for the entire grid on every shift.

Why it matters: This is likely the highest hitch risk during long runs. A full grid scan touches more map than actually changed and runs on the main thread while pathfinding is paused. The existing comments acknowledge an incremental tracking path, but the live branch uses the full-rescan fallback.

Fix direction: Profile the scan cost first, then replace shift-time full scans with bounded grid updates for the cleared/new segment regions, or a dedicated incremental graph move/update path. Keep the loading/warmup full scan if it is still needed, but avoid rescanning unchanged segments after that. Preserve the existing collider sync sequencing until verified otherwise.

Suggested Editor tests: Extract pure graph-bounds calculation into a small helper and cover first/third segment bounds, trimmed edges, and segment-size edge cases. The runtime graph update itself likely needs Unity performance/profiling validation, but the boundary math can be EditMode.

### 2. Make task ordering linear or near-linear instead of O(n^2)

Files: `Assets/Scripts/Tasks/TaskController.cs:142`, `Assets/Scripts/Tasks/TaskController.cs:172`, `Assets/Scripts/Tasks/TaskController.cs:700`

Evidence: Sorting is debounced, which is good, but `SortTaskListsByProximity()` allocates a new `List<(Vector3, MonoBehaviour, ITask)>`, repeatedly scans all remaining pairs, uses `Vector3.Distance`, and removes by index until empty. `GameManager` forces a resort after initial generation.

Why it matters: Segment generation can add many tasks at once. The current greedy selection is O(n^2), and each sort also rebuilds `tasks`, `taskObjects`, and `taskMap`. It is probably fine at small task counts but gets risky as task density, warmup segments, or echoes increase.

Fix direction: If left-to-right progression is the dominant rule, preserve an x-sorted list from generation and apply backtracking/max-backtrack weighting during selection rather than rebuilding a greedy route. If the greedy behavior must remain, reuse scratch buffers and compare squared distances where possible. Consider maintaining separate task metadata so repeated `task.Target`/`transform` lookups are not needed during sorting.

Suggested Editor tests: Cover exact task order parity for normal ordering, backtrackingAdditionalWeight, maxBacktrackDistance, taskRemovalDistance, null task removal, and echo skill filtering. Wrap any new tests in `#if UNITY_INCLUDE_TESTS` / `#endif`.

### 3. Reduce enemy/echo scan cost with spatial or event-indexed targeting

Files: `Assets/Scripts/Enemies/EnemyActivator.cs:45`, `Assets/Scripts/Enemies/EnemyActivator.cs:57`, `Assets/Scripts/Enemies/EnemyActivator.cs:82`, `Assets/Scripts/Hero/HeroCombatController.cs:384`, `Assets/Scripts/Hero/HeroCombatController.cs:430`, `Assets/Scripts/Hero/HeroCombatController.cs:569`

Evidence: `EnemyActivator.LateUpdate()` iterates every registered enemy every frame, checks every combat echo for offscreen proximity, and uses `List.Contains`/`Remove` to maintain `activeEnemies`. Hero combat then scans `EnemyActivator.ActiveEnemies` for nearest targets; the time-aware echo path may also estimate combined DPS across the main hero plus all combat echoes.

Why it matters: This is a multiplicative runtime path: enemies times echoes times active heroes. It will get more expensive exactly when the game is busiest: dense combat, many echoes, or higher enemy spawn density.

Fix direction: Keep the current active enemy registry, but back it with a `HashSet<Enemy>` for membership updates and use a spatial index or x-bucketed active range for target selection. At minimum, throttle offscreen echo proximity checks and use squared distances consistently. Longer term, push engagement/target changes by event so heroes do not all rescan the same list.

Suggested Editor tests: Extract enemy activation classification into a pure helper and test camera bounds, padding, hero proximity, echo proximity, engaged enemies, inactive/null enemies, and membership transitions.

### 4. Remove per-frame allocation in BuffManager cooldown ticking

Files: `Assets/Scripts/Buffs/BuffManager.cs:173`, `Assets/Scripts/Buffs/BuffManager.cs:226`, `Assets/Scripts/Buffs/BuffManager.cs:245`, `Assets/Scripts/Buffs/BuffManager.cs:721`

Evidence: `BuffManager.Update()` calls `Tick(Time.deltaTime)` every frame. `TickCooldowns()` allocates `new List<BuffRecipe>(cooldowns.Keys)` whenever any cooldown exists. `AutoCastBuffs()` also runs every frame while ticking and calls `CanActivate()` for each auto slot.

Why it matters: This is a classic small-but-steady GC source. The project has already spent effort removing UI/tasting GC, so this stands out as an easy remaining hot-path cleanup.

Fix direction: Use a reusable scratch list for cooldown keys, or collect expired recipes into a cached list and remove after enumeration. Consider throttling `AutoCastBuffs()` to a short interval or making cooldown expiration event-driven if exact per-frame casting is not required. Avoid double `CanActivate()` work when `AutoCastBuffs()` immediately calls `PurchaseBuff()`.

Suggested Editor tests: Cover cooldown decrement/removal without relying on frames by calling `Tick(delta)` directly. Add tests for auto-cast eligibility, cooldown expiry, and distance-percent duration rules.

### 5. Make save methods truly asynchronous or rename and schedule them honestly

Files: `Assets/Scripts/Blindsided/SaveData/SaveManager.cs:50`, `Assets/Scripts/Blindsided/SaveData/SaveManager.cs:64`, `Assets/Scripts/Blindsided/SaveData/SaveManager.cs:79`, `Assets/Scripts/Blindsided/Oracle.cs:260`, `Assets/Scripts/Blindsided/Oracle.cs:309`

Evidence: `SaveAsync()` serializes with Odin binary, writes/flushed files, rotates backups, and writes metadata before returning `Task.FromResult(true)`. `LoadAsync()` also performs sync file reads/deserialization before returning a completed task. Autosave calls this through `Oracle.SaveToFile()` every 30 seconds.

Why it matters: Recent work removed the full deserialization verification GC spike, but serialization and disk IO still happen synchronously on the calling thread. As save data grows, autosave and immediate post-run/quest saves can still hitch.

Fix direction: Either rename to synchronous APIs and explicitly schedule saves at safe points, or move file IO and serialization to `Task.Run` with a snapshot/copy boundary that avoids mutating live game data off-thread. Keep Unity API calls such as `Application.version` and `Application.persistentDataPath` on the main thread. Coalesce redundant immediate saves after quest/run transitions.

Suggested Editor tests: Extend the existing save stress tests with a pure EditMode test around slot rotation/fallback if possible. Keep PlayMode only for scenarios that require `Application.persistentDataPath` or frame timing.

### 6. Convert remaining polling UI to event-driven dirty flags

Files: `Assets/Scripts/UI/QuestButtonIndicator.cs:21`, `Assets/Scripts/Quests/QuestManager.cs:641`, `Assets/Scripts/UI/TownWindowManager.cs:296`, `Assets/Scripts/UI/SettingsPanelUI.cs:573`, `Assets/Scripts/UI/Core/EventDrivenStatsPanelUI.cs:37`

Evidence: The project now has event-driven UI base classes, but `QuestButtonIndicator` still calls `HasQuestsReadyForTurnIn()` every `Update`. `TownWindowManager` polls right-click every 0.05 seconds in a coroutine. `SettingsPanelUI` refreshes save-slot info every second. `QuestButtonIndicator` already subscribes to quest hand-in and load events, so the frame poll is partially redundant.

Why it matters: Each individual poll is small, but UI polling accumulates across always-active panels and can trigger avoidable UGUI work. It also makes state ownership harder to reason about because UI state is both event-updated and polled.

Fix direction: Add a quest readiness changed event from `QuestManager` when `ReadyForTurnIn` changes, then remove `QuestButtonIndicator.Update()`. Use Input System performed/canceled callbacks for right-click close if practical. For save slot metadata, refresh on open, slot switch, delete, import/export, and save completion rather than every second.

Suggested Editor tests: Test quest readiness event emission when progress crosses ready/not-ready boundaries and verify the indicator can update from events without polling.

### 7. Tighten projectile hit-path component lookups

Files: `Assets/Scripts/Projectile.cs:82`, `Assets/Scripts/Projectile.cs:109`, `Assets/Scripts/Projectile.cs:115`, `Assets/Scripts/Projectile.cs:126`, `Assets/Scripts/Projectile.cs:179`, `Assets/Scripts/Projectile.cs:190`, `Assets/Scripts/Hero/HeroCombatController.cs:610`

Evidence: Projectiles are pooled and cache `IHasHealth`/`IDamageable` in `Init()`, but the hit branch still does several `GetComponent` calls and fallback `FindAnyObjectByType` calls for skill/stat services. Movement uses `Vector2.Distance` after already moving toward the target.

Why it matters: Projectile hit code is bursty under high attack speed and echo-heavy combat. Pooling avoids instantiate churn, but component lookups and scene searches in a burst path can still create CPU spikes.

Fix direction: Pass stable service references or cached singleton references through `Projectile.Init()` or a projectile context. Cache `ProjectileHitSfx` and avoid repeated `GetComponent` on the same projectile. Compare squared remaining distance instead of `Vector2.Distance`. Keep defensive fallback lookups outside the hot path where possible.

Suggested Editor tests: Add tests around projectile init context and damage routing with fake health/damageable components. Use PlayMode only if transform movement over frames must be verified.

### 8. Precompute enemy sprite variant pools for spawn reuse

Files: `Assets/Scripts/Enemies/Enemy.cs:191`, `Assets/Scripts/Enemies/Enemy.cs:200`, `Assets/Scripts/Enemies/Enemy.cs:205`, `Assets/Scripts/Enemies/Enemy.cs:287`, `Assets/Scripts/Enemies/Enemy.cs:636`, `Assets/Scripts/Enemies/Enemy.cs:708`

Evidence: `InitForSpawn()` calls `ApplyRandomSpriteLibrary()`. That method builds `validRegular` and `validWeighted` with LINQ `Where(...).ToList()` and then calls `GetComponentsInChildren<SpriteResolver>(true)` when applying a variant.

Why it matters: Enemy pooling means spawn reuse is a hot path. The logic is not per-frame, but segment generation can spawn many enemies in a short batch, making repeated LINQ and component traversal noticeable.

Fix direction: Cache valid sprite library variants and resolver arrays in `Awake`/`OnValidate` or when serialized lists change. Roll against cached weight totals without creating lists. Reset `SpriteRenderer.flipX` when reusing pooled decor/enemies that randomize flip state.

Suggested Editor tests: Cover weighted selection eligibility and fallback behavior through a pure selector helper. Confirm no null/zero-weight entries are selected.

### 9. Replace broad hierarchy searches in run cleanup and one-off toggles where references are knowable

Files: `Assets/Scripts/GameManager.cs:1140`, `Assets/Scripts/GameManager.cs:1167`, `Assets/Scripts/GameManager.cs:1177`, `Assets/Scripts/GameManager.cs:1229`, `Assets/Scripts/GameManager.cs:1242`

Evidence: Cleanup uses `CurrentMap.GetComponentsInChildren<PooledObject>(true)` before destroying the map. `DestroyAllEchoes()` uses `FindObjectsByType` correctly for Unity 6000, but it is still a broad scene search. `EnableMildred()` walks all transforms under the map looking for a hard-coded name.

Why it matters: These are transition paths, not per-frame paths, but they happen around run start/return where hitching is visible. They also increase coupling to hierarchy names and make pooling ownership harder to verify.

Fix direction: Let `SegmentedMapGenerator` or a map-owned registry release known segment roots and spawned objects. Store a serialized `Mildred` reference or a small `NamedMapActor` registry on the map prefab instead of scanning transforms by string. Keep the current `FindObjectsByType<T>(FindObjectsInactive.Include)` overload for rare global cleanup until a stronger owner registry exists.

Suggested Editor tests: Test a map registry helper resolves optional actors without scene-name scans and that cleanup releases registered pooled roots exactly once.

### 10. Align project version documentation and API migration expectations

Files: `README.md:3`, `README.md:18`, `ProjectSettings/ProjectVersion.txt:1`, `AGENTS.md:27`, `CameraClampExtension.cs:19`, `GameManager.cs:1234`

Current state: `ProjectSettings/ProjectVersion.txt`, README setup guidance, and repository instructions now identify Unity `6000.5.1f1` as the supported editor. The project uses Cinemachine 3 and current scripts use `CinemachineCamera`, matching the intended API direction. `GameManager` uses the Unity 6000.5 `FindObjectsByType<T>(FindObjectsInactive)` overload.

Why it matters: Version drift can hide API warnings and package compatibility decisions. Unity 6000.x also changes object-find API expectations, so audit and test baselines should target the actual editor version used by the project.

Fix direction: Keep documentation, generated project files, CI, and test expectations aligned with `ProjectSettings/ProjectVersion.txt`. Continue avoiding `CinemachineVirtualCamera` and prefer `CinemachineCamera`.

Suggested Editor tests: No gameplay test needed. Add a lightweight editor validation test only if the team wants automated checks for package/editor version metadata.

## Suggested First Three Tasks

1. Replace the full segment-shift A* rescan with a measured incremental graph update path.
   This has the largest likely runtime payoff and directly targets run hitches.

2. Refactor `TaskController` ordering into a tested, allocation-conscious helper.
   This is contained, testable in EditMode, and reduces risk around procedural task density and echoes.

3. Clean up the remaining per-frame GC/polling paths: `BuffManager.TickCooldowns()` and `QuestButtonIndicator.Update()`.
   These are small, low-risk changes that should keep recent GC improvements from regressing.

## Test Strategy

- Prefer EditMode tests when logic can be extracted from MonoBehaviours: task sorting, graph bounds math, cooldown ticking, quest readiness events, save slot naming/rotation, weighted selection, and UI dirty-state helpers.
- Use PlayMode tests only when frame timing, coroutines, transforms, pooling activation, A* components, or Unity physics/collider updates are essential.
- Wrap new test script contents in `#if UNITY_INCLUDE_TESTS` / `#endif`, matching the current `Assets/Tests` pattern.
- Run Unity tests through the MCP test tooling, not directly in the editor.
- Add performance probes before and after high-risk changes. Good first markers: segment shift duration, `astar.Scan`/graph update time, task sort duration/count, active enemy count, combat echo count, projectile hit burst count, autosave duration, and GC alloc per frame during a long run.

## Open Questions

- How large can task density, enemy density, and combat echo count get in intended late-game saves?
- Is the greedy proximity task route required for gameplay feel, or is mostly left-to-right ordering acceptable with backtracking exceptions?
- Is the A* full rescan masking stale-node bugs that an incremental update must explicitly handle?
- Are autosaves allowed to lag by one coalesced request, or must every immediate save complete synchronously before gameplay continues?
- Should the codebase standardize on one UI pattern now that `EventDrivenStatsPanelUI` exists, or keep mixed polling for low-frequency panels?
