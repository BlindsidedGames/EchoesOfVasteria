# Forge / Gear Rolling Performance Review

Date: 2026-05-23

Scope: Review the forge and gear rolling system end to end for runtime performance, allocation hotspots, duplicated work, UI refresh cost, data flow, and structural bottlenecks. No runtime code changes are made as part of this review.

## Working Notes

- Review started. Initial pass will map the forge UI flow, crafting service, gear item model, stat rolling math, equipment updates, salvage path, and related save/UI refresh boundaries.
- Forge roll flow currently runs through `ForgeWindowUI.CraftUntilUpgradeCoroutine` -> `CraftingService.Craft` -> rarity/slot/affix roll -> upgrade evaluation/telemetry -> UI preview/resource refresh -> salvage/release.
- Current worktree already had modified forge files before this review. Findings are based on the working tree as inspected, not a clean baseline.

## Findings

### High impact

1. **Autocraft does not use the existing batch craft API.** `CraftingService.CraftBatch` exists, but `ForgeWindowUI.CraftUntilUpgradeCoroutine` still loops over `crafting.Craft(...)` one item at a time. That means every roll still pays for per-craft affordability checks, resource batch enter/exit inside `Craft`, XP event dispatch, telemetry object creation, upgrade score calculation, and resource spend telemetry.

2. **Telemetry queues pooled mutable `GearItem` references if the analytics service is present.** `CraftingService.Craft` records `CraftResult { Item = item }`. During autocraft, `ForgeWindowUI` releases non-stopping items through `GearObjectPool.ReleaseItem(lastCrafted)`. If `ForgeAnalyticsService` / `BackgroundTelemetryProcessor` are added to the active scene, queued telemetry may process that same `GearItem` after it has been reset or reused. Current scene search found `CraftingService` and `SalvageService` in `Assets/Scenes/Main.unity`, but did not find `ForgeAnalyticsService`, `BackgroundTelemetryProcessor`, or `ScoreEvaluationService`, so this appears to be a dormant or partially integrated path rather than active Main-scene behavior today.

3. **Background telemetry reads Unity/object state off the main thread.** `BackgroundTelemetryProcessor` reads `ScriptableObject.name`, `GearItem.affixes`, `StatDefSO` fields, and uses `Mathf`/Unity-backed data from a worker thread. `Mathf` is fine, but Unity object access and mutable save dictionaries are not a clean thread boundary. This risks intermittent issues and makes performance tuning harder because the background worker is not operating on immutable DTOs.

4. **Upgrade evaluation is duplicated per roll.** `CraftingService.Craft` computes upgrade score, absolute score, and `IsPotentialUpgrade` for analytics. The autocraft loop then calls `UpgradeEvaluator.IsPotentialUpgrade` again for the stop condition. `IsPotentialUpgrade` calls `ComputeUpgradeScore`, so the candidate/current comparison is repeated for every roll.

5. **Score lookup repeatedly scans stat definitions.** `UpgradeEvaluator.ComputeUpgradeScore` and `ComputeAbsoluteScore` call `crafting.GetStatByMapping` for each aggregated stat. `GetStatByMapping` linearly scans the `stats` list. With few stats this is small, but it is in the hot path and is repeated across analytics, stop checks, and quality/UI formatting.

6. **Autocraft still creates per-result UI strings and collections at 10 Hz.** The loop throttles visual refreshes, but each visual refresh calls `GearStatTextBuilder.BuildCraftResultSummary`, which allocates `List<string>`, `Dictionary<HeroStatMapping,...>`, `HashSet<HeroStatMapping>`, sorted affix lists, formatted strings, and the joined result string.

7. **Odds refresh allocates and reloads assets.** `RarityOddsCalculator.BuildRarityWeightInfo` calls `AssetCache.GetAll<RaritySO>().OrderBy(...).ToList()` every refresh, then creates new weight and line lists. It is throttled to 5 Hz, but still avoidable and happens during rolling, core changes, XP changes, and resource refresh.

8. **Aggregate/equipped stat text rebuilds allocate heavily.** `UpdateAggregateStatsText` creates entry lists, then `GearStatTextBuilder.BuildAggregateStatsTextSections` creates multiple dictionaries/lists, section lists, splits strings, and joins output. This is mostly event/deferred rather than per roll, but it can spike when equipment changes or the forge opens.

9. **Single manual craft forces immediate save-event serialization.** `OnCraftClicked` calls `EventHandler.SaveData()` after each manual craft. `EventHandler.SaveData()` is debounced to once per frame and invokes save subscribers rather than directly writing to disk, but those subscribers still rebuild in-memory snapshots such as equipment/resource state. The first-equipment path also calls `EquipmentController.Equip`, which invokes `SaveState` immediately after load.

10. **The pool reduces object allocation but complicates ownership.** `GearObjectPool` works for transient discarded items, but `GearItem` is also the live object handed to equipment, UI preview, telemetry, and salvage. That mixed ownership makes it easy to reuse an item while another subsystem still references it.

## Quick Wins

- Use the already-created `ScoreEvaluationService.Evaluate` or a similar single-pass result in `CraftingService.Craft` and return/pass that to the autocraft loop so upgrade score, absolute score, quality, and `IsUpgrade` are not recomputed separately.
- Add a cached `Dictionary<HeroStatMapping, StatDefSO>` inside `CraftingService` during `Awake` and use it for stat lookup instead of scanning the stat list per score contribution.
- Cache sorted rarity lists and per-core rarity weight rows for `RarityOddsCalculator`, invalidating only when Ivan level/config/core weights change.
- Avoid building craft result summary text unless the forge window is actually open and the visual refresh interval has elapsed. If the window is closed, keep only the stopping item and attention state.
- Replace `BuildAffixStatSet` per stopping candidate with a reusable scratch set or compact bitmask keyed by `HeroStatMapping`/stat index. This only matters when `LockAutocraftStatSet` is enabled.
- Stop using pooled `GearItem` references inside queued telemetry. Snapshot the fields needed for telemetry before release, even if broader telemetry redesign waits.
- Either wire `ScoreEvaluationService`/analytics intentionally or remove/defer the unused service path. Right now there is optimization-oriented code that is not present in `Main.unity`, which makes performance behavior harder to reason about.
- Remove or gate `Debug.Log` calls from hot/commonly clicked UI paths if they show up in profiling builds.

## Larger Redesign Options

### Option A: Dedicated roll engine with immutable roll result DTOs

Move rarity/slot/affix rolling into a pure service that returns a compact `GearRollResult` struct/DTO: core id, slot id, rarity id/tier, affix stat ids, values, precomputed upgrade score, absolute score, and stop flags. Only materialize a `GearItem` when the roll is shown to the player or equipped. This removes most transient object pooling pressure and makes telemetry thread-safe.

Tradeoff: Requires refactoring UI, salvage, telemetry, and equipment boundaries, but gives the biggest long-term performance and maintainability gain.

### Option B: True batch simulation for autocraft

Create an autocraft-specific batch method that spends resources once per batch, rolls N candidates in a tight loop, evaluates stop conditions, accumulates salvage/resource deltas, and emits one UI/resource event per frame. It should not call the single manual `Craft` method for each candidate.

Tradeoff: More code paths to keep behavior-identical, but the speed ceiling improves dramatically because per-item UI/events/telemetry can be collapsed.

### Option C: Telemetry aggregation instead of per-craft event replay

During autocraft, aggregate counters directly: crafts by core/rarity/slot, upgrade counts, score aggregates, best scores, salvage totals. Flush one aggregate object at the end of each frame/session instead of queuing each craft result.

Tradeoff: Detailed per-item telemetry is intentionally reduced or reconstructed from aggregates. This is a beneficial behavior change only if the design accepts aggregate stats as sufficient for fast rolling.

### Option D: Separate runtime gear model from UI/save model

Use stable ids and fixed-size affix arrays for runtime rolling/evaluation, then convert to `GearItem` only at persistence/UI boundaries. Equipment could store computed stat totals alongside item display data, making stat reads O(1) and avoiding repeated affix scans.

Tradeoff: Larger migration, but it clarifies ownership and removes the current pooled-object ambiguity.

## Questions / Verification Gaps

- No profiler capture has been run yet. These findings are static-review findings and should be verified with Unity Profiler/Deep Profile disabled first, then allocation call stacks if needed.
- Scene/prefab text search did not find active `ForgeAnalyticsService`, `BackgroundTelemetryProcessor`, or `ScoreEvaluationService` references. This should still be confirmed in Unity if scene composition is generated or loaded additively.
- Save cadence check: `EventHandler.SaveData()` is an in-memory event dispatch debounced once per frame; `Oracle.SaveToFile` does disk persistence via `SaveManager.SaveAsync(...).GetAwaiter().GetResult()`. Manual craft calls do not appear to write directly to disk, but they can still trigger save snapshot work.
