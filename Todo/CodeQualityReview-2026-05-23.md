# Code Quality Review - 2026-05-23

Scope: runtime/editor scripts under `Assets/Scripts`, `Assets/Editor`, and `Assets/Tests`. Third-party and package code was scanned for context but not turned into project tasks unless project scripts depend on it directly.

Unity API notes checked against official Unity documentation:
- `Object.FindObjectOfType` / `Object.FindObjectsOfType` are obsolete in current Unity; use `FindFirstObjectByType`, `FindAnyObjectByType`, or `FindObjectsByType` with `FindObjectsSortMode.None` when a scene query is unavoidable. See Unity docs: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Object.FindObjectOfType.html and https://docs.unity.cn/ScriptReference/Object.FindObjectsByType.html.
- `Resources.Load` / `Resources.LoadAll` remain valid APIs, but repeated runtime resource scans are worth isolating behind cache or serialized references in Unity 6000.2.1f1. See Unity docs: https://docs.unity3d.com/ScriptReference/Resources.Load.html and https://docs.unity3d.com/ScriptReference/Resources.LoadAll.html.
- `CinemachineVirtualCamera` should not be introduced; current project code already uses `CinemachineCamera` in gameplay controllers. See Cinemachine docs: https://docs.unity.cn/Packages/com.unity.cinemachine@3.1/api/Unity.Cinemachine.CinemachineVirtualCamera.html.

## P0 - Replace Scene-Wide Lookup Fallbacks With Explicit Runtime References

Several systems still use `FindFirstObjectByType` or `FindObjectsByType` as normal dependency recovery instead of explicit ownership. These are not obsolete, but Unity documents object-finding as a slow scene query, and the pattern makes initialization order fragile.

Evidence:
- `Assets/Scripts/GameManager.cs:500`, `:642`, `:690`, `:816`, `:1232`
- `Assets/Scripts/Buffs/BuffManager.cs:723`
- `Assets/Scripts/Projectile.cs:115`, `:144`, `:186`
- `Assets/Scripts/Tasks/TaskController.cs:681`
- `Assets/Scripts/UI/CollectionsWindowUI.cs:67`, `:739`

Task: Add a small runtime reference/service context for run-scoped systems such as `GameplayStatTracker`, `SkillController`, `TaskbarFlasher`, run UI references, and active map/task controller. Let `GameManager` or the scene bootstrap own assignment, then pass references into projectiles, buffs, task controllers, and UI presenters. Keep one guarded fallback path for diagnostics only.

Performance impact: removes repeated scene scans from combat, buff autocast, UI refresh, and run transitions. Maintainability impact: makes dependencies visible and testable.

Suggested tests: Editor tests for dependency resolution using fake/reference-only components; targeted PlayMode test only if scene lifecycle behavior cannot be covered in EditMode.

## P0 - Refactor TaskController Sorting And Registration Into Dedicated Services

`TaskController` mixes task discovery, enemy task component creation, event subscription, current-task selection, pruning, pooling release decisions, and pathfinder/camera references. `SortTaskListsByProximity` builds a new list and repeatedly scans/removes from it, which is O(n^2) per sort and allocates each call.

Evidence:
- `Assets/Scripts/Tasks/TaskController.cs:242-285` duplicates runtime registration logic.
- `Assets/Scripts/Tasks/TaskController.cs:307-374` duplicates reset-time registration logic.
- `Assets/Scripts/Tasks/TaskController.cs:637-676` hard-codes task type cleanup behavior.
- `Assets/Scripts/Tasks/TaskController.cs:700-770` performs greedy O(n^2) sorting with a fresh list.

Task: Split task registration into a `TaskRegistry` or helper that returns registered task records, move cleanup policy behind task interfaces, and replace sort internals with a reusable comparer or priority selection strategy. Preserve current greedy route behavior unless a gameplay change is explicitly approved.

Performance impact: highest when segments add many tasks and each add requests a sort. Maintainability impact: reduces hidden coupling between task implementations, pooling, hero selection, and stats.

Suggested tests: EditMode tests for registration, duplicate prevention, completion cleanup policy, max backtrack filtering, and sorting order for fixed positions.

## P1 - Consolidate Procedural Task Generation Paths

`ProceduralTaskGenerator` has synchronous and coroutine generation paths with copied enemy/NPC validation and spawn code. Changes to spawn validation, terrain rules, or task spacing must be made in both paths.

Evidence:
- `Assets/Scripts/Tasks/ProceduralTaskGenerator.cs:262-344` synchronous enemy spawn path.
- `Assets/Scripts/Tasks/ProceduralTaskGenerator.cs:605-689` async enemy spawn path.
- `Assets/Scripts/Tasks/ProceduralTaskGenerator.cs:346-360` and `:691-735` duplicate NPC handling.
- `Assets/Scripts/Tasks/ProceduralTaskGenerator.cs:922-943` evaluates the weighted filter and weight twice per entry.

Task: Extract shared `TrySpawnEnemy`, `TrySpawnNpc`, and weighted-pick helpers that both sync and async flows call. Have the async path yield between shared operations rather than carrying separate logic. Cache filtered weights during `PickEntry` so filters and weights are evaluated once per call.

Performance impact: lowers allocations and duplicate predicate/weight work during segment generation. Maintainability impact: reduces risk of sync/async spawn behavior drifting.

Suggested tests: EditMode tests for spawn eligibility, NPC spacing replacement, terrain filtering, and weighted selection with deterministic input.

## P1 - Clarify Pool Lifecycle Contracts

`PoolManager` currently re-enables every disabled `ITask` component under pooled prefabs on get, cleans PlayableDirector graphs on release/destroy, and silently destroys objects without markers. That makes pooling behavior global and surprising for non-task pooled objects.

Evidence:
- `Assets/Scripts/Blindsided/Utilities/Pooling/PoolManager.cs:146-149` adds markers on get.
- `Assets/Scripts/Blindsided/Utilities/Pooling/PoolManager.cs:216-227` re-enables child task components on prefab get.
- `Assets/Scripts/Blindsided/Utilities/Pooling/PoolManager.cs:286-295` repeats task re-enable behavior for named pools.
- `Assets/Scripts/Blindsided/Utilities/Pooling/PoolManager.cs:178-190` destroys unmarked objects.
- `Assets/Scripts/GameManager.cs:1164-1178` releases all pooled map children before destroying the map.

Task: Introduce an optional `IPooledLifecycle` interface or `PooledObjectCallbacks` component with `OnBeforePoolRelease` / `OnAfterPoolGet`. Move task re-enable and PlayableDirector cleanup to lifecycle-aware components. Keep marker handling in `PoolManager`, but avoid project-specific task rules inside the generic pool.

Performance impact: avoids repeated child component scans for pooled prefabs that do not need task reset behavior. Maintainability impact: makes pooling side effects local to the prefab/system that needs them.

Suggested tests: EditMode tests around double release, unmarked release behavior, lifecycle callback order, and task component reset.

## P1 - Break Up Oversized Feature Controllers Along Existing Presenter/Service Boundaries

Some feature files are too large to reason about safely and already contain signs of partial extraction. The worst examples are `ForgeWindowUI` at 2533 lines, `CauldronManager` at 1595 lines, `GameManager` at 1256 lines, and `CollectionsWindowUI` at 902 lines.

Evidence:
- `Assets/Scripts/Gear/UI/ForgeWindowUI/ForgeWindowUI.cs:22-130` owns UI references, services, selection state, runtime maps, and cache fields.
- `Assets/Scripts/Gear/UI/ForgeWindowUI/ForgeWindowUI.cs:1153-1185` owns autocraft batching/turbo rules inside the UI class.
- `Assets/Scripts/Gear/UI/ForgeWindowUI/ForgeWindowUI.cs:2481-2529` repeats conversion button state blocks.
- `Assets/Scripts/Upgrades/CauldronManager.cs:64-78` already uses extracted services but still owns many counters, caches, and batching details.
- `Assets/Scripts/UI/CollectionsWindowUI.cs:120-162` rebuilds data, filtering, sorting, and instantiation in the view.

Task: Create separate tasks per feature: move Forge autocraft into a service/controller, extract Forge conversion button state rendering, move Cauldron session stats/card batching into focused collaborators, and move Collections data projection out of the MonoBehaviour. Keep each extraction behavior-preserving and test it before moving the next section.

Performance impact: lets hot paths such as autocraft and tasting be tested and optimized without UI side effects. Maintainability impact: lowers merge risk and makes feature-specific bugs easier to isolate.

Suggested tests: EditMode tests for Forge autocraft batching decisions, conversion button state, Cauldron stat snapshots, and Collections item projection.

## P2 - Finish The Polling-To-Events Migration For UI

The project already has an event-driven UI base class, but several panels still poll on `Update` or short coroutines. Some polling is justified for time displays, but inventory/unlock/tooltip state can usually be event-driven with a targeted periodic fallback.

Evidence:
- `Assets/Scripts/UI/UITicker.cs:7-18` says the ticker is deprecated for UI updates.
- `Assets/Scripts/UI/Core/EventDrivenStatsPanelUI.cs:6-15` documents the desired event-driven pattern.
- `Assets/Scripts/UI/CollectionsWindowUI.cs:292-325` hashes unlocks on a polling coroutine.
- `Assets/Scripts/UI/CollectionsWindowUI.cs:710-720` rebuilds tooltip text at 5 Hz while visible.
- `Assets/Scripts/UI/SettingsPanelUI.cs:565-579` polls save-slot info every second.
- `Assets/Scripts/GameManager.cs:441-448` refreshes run buttons at 10 Hz.

Task: Convert non-time-sensitive UI refreshes to events from `ResourceManager`, `QuestManager`, save-slot changes, and run-state changes. Keep the periodic base class only for genuinely time-sensitive visuals.

Performance impact: fewer background UI refreshes and asset-cache scans when windows are open. Maintainability impact: makes UI updates traceable to state changes.

Suggested tests: EditMode tests with fake events to verify panels mark dirty and refresh only when visible.

## P2 - Reduce Per-Frame Allocation And Runtime Component Fetching In Hot Paths

Several hot paths are already mostly cached, but there are still repeated allocations and component fetches inside `Update` or frequent loops.

Evidence:
- `Assets/Scripts/Buffs/BuffManager.cs:173-176` ticks every frame; `:245-254` allocates a new list from cooldown keys each tick.
- `Assets/Scripts/Projectile.cs:82-196` runs movement and hit resolution every frame; `:126`, `:147`, `:190` fetch `ProjectileHitSfx` on hit.
- `Assets/Scripts/MapGeneration/CloudSpawner.cs:111` fetches a `SpriteRenderer` from a cloud transform during reset.
- `Assets/Scripts/Tasks/TaskController.cs:700-770` allocates sort pairs and uses distance square roots during sort.

Task: Add scratch lists or safe in-place cooldown update, cache projectile SFX in `Awake`/pool lifecycle, store cloud sprite renderer in the cloud record, and use squared distance where exact distance is not required. Verify behavior with focused tests before changing timing-sensitive systems.

Performance impact: reduces GC pressure in combat, buffs, and segment/map UI loops. Maintainability impact: makes hot-path assumptions explicit.

Suggested tests: EditMode tests for cooldown expiry/removal and projectile hit resolution; profiler confirmation after implementation.

## P2 - Standardize Serialized Field Visibility And Data Object Exceptions

Most runtime MonoBehaviours follow `[SerializeField] private`, but some runtime controllers expose serialized fields publicly. ScriptableObject data classes intentionally use public fields in many places, so this should be scoped to MonoBehaviours and runtime controllers only.

Evidence:
- `Assets/Scripts/Tasks/TaskController.cs:25-36` uses `[SerializeField] public` for hero/backtracking/pruning fields.
- `Assets/Scripts/GameManager.cs:83` exposes `mapUIInstance`.
- `Assets/Scripts/PixelGridSnap.cs:8` exposes `internalHeightPx`.
- Many ScriptableObjects such as `Assets/Scripts/Enemies/EnemyData.cs` and `Assets/Scripts/MapGeneration/MapGenerationConfig.cs` use public fields as authoring data and should be treated separately.

Task: Convert runtime MonoBehaviour serialized public fields to `[SerializeField] private` with properties or explicit methods where other systems need access. Leave ScriptableObject data containers alone unless a separate data-encapsulation task is approved.

Performance impact: neutral. Maintainability impact: reduces accidental mutation of runtime state.

Suggested tests: compile validation plus existing tests; add tests only where accessors carry behavior.

## P3 - Normalize Resource Loading Boundaries

The project has `AssetCache`, but a few direct `Resources.Load` calls remain in gameplay systems. Some are one-time fallback defaults, but they still encode string paths and make missing assets a runtime concern.

Evidence:
- `Assets/Scripts/Blindsided/BuildModeConfig.cs:36` loads config by resource path.
- `Assets/Scripts/Gear/MeetIvanReward.cs:24-26` loads specific gear assets by string path.
- `Assets/Scripts/Upgrades/BaseStatService.cs:89-91` loads stats from resources with fallback to full scan.
- `Assets/Scripts/Utilities/AssetCache.cs:8-116` already centralizes cached resource loading.

Task: Replace direct gameplay `Resources.Load` strings with serialized references, `AssetCache.Get`, or a small typed registry. Keep bootstrapping/config cases documented if they intentionally use Resources.

Performance impact: reduces runtime asset lookup and full resource scans. Maintainability impact: catches missing assets earlier via inspector references or registry validation.

Suggested tests: EditMode validation test for required registry/config assets.

## P3 - Remove Compatibility Branches That No Longer Apply To Unity 6000

The project targets Unity 6000.2.1f1, but at least one local script keeps pre-Unity-6 compatibility code for obsolete object-finding APIs.

Evidence:
- `Assets/Scripts/GameManager.cs:1229-1235` uses `FindObjectsByType` for Unity 6000 and falls back to obsolete `Object.FindObjectsOfType` otherwise.

Task: Remove pre-Unity-6 fallback branches in project-owned code where the target version is fixed. This keeps obsolete APIs out of the codebase and reduces warning noise.

Performance impact: small but positive when the modern overload uses `FindObjectsSortMode.None`. Maintainability impact: aligns code with the documented Unity target.

Suggested tests: compile validation.

## P3 - Clean Up Encoding And Formatting Artifacts Opportunistically

A few files contain mojibake characters from prior encoding conversions and some indentation inconsistency. This is low risk but should be cleaned only when touching nearby logic to avoid noisy diffs.

Evidence:
- `Assets/Scripts/Blindsided/Utilities/Pooling/PoolManager.cs:172` contains a corrupted dash sequence in a comment.
- `Assets/Scripts/Gear/UI/ForgeWindowUI/ForgeWindowUI.cs:1160` contains a corrupted less-than-or-equal symbol in a comment.
- `Assets/Scripts/Upgrades/CauldronManager.cs:1503` contains a corrupted multiplication symbol in a comment.

Task: Normalize comments to ASCII and run a line-ending/whitespace check on touched files. Do not reformat unrelated files wholesale.

Performance impact: none. Maintainability impact: reduces reviewer noise and keeps docs/comments readable.

Suggested tests: `git diff --check` after edits.

## Review Notes

- I did not recommend editing `Assets/Scenes/Main.unity`.
- I did not create `.meta` files.
- I did not treat third-party package internals as project cleanup tasks.
- The existing dirty worktree includes many unrelated modifications, including package and scene changes, so implementation tasks should be scoped carefully before touching any currently modified files.
