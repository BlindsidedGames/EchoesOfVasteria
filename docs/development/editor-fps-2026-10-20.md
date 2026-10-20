# Editor frame pacing investigation - 20 October 2026

The existing save was running at 3840x2160 with a Scene view and Game view present. Preferences were TargetFps=30 and VSyncEnabled=true. Startup assigned the 30 FPS preference directly, unlike the options screen, which releases that limit for desktop landscape VSync. Correct startup to use the same policy without changing saved preferences.

The CPU profile also exposed a terrain-validation gizmo scan over every tile on every repaint, even with all three validation toggles false. Exit before enumerating cells when none of the overlays is enabled. Enabled overlays, map generation, gameplay and player data are unchanged.

## Measurements

Ten-second post-warmup live samples with the same save and Editor windows:

| Condition | FPS | P95 frame time | Main thread average | PlayerLoop average |
| --- | ---: | ---: | ---: | ---: |
| Original cap 30, VSync 1 | 29.7 | 44.63 ms | 33.61 ms | 9.24 ms |
| Original code, uncapped, VSync 0 | 33.4 | 37.50 ms | 29.90 ms | 10.10 ms |
| Original code, uncapped, VSync 1 | 39.5 | 34.21 ms | 25.31 ms | 9.14 ms |
| Fixed startup and gizmo guard, VSync 1 | 51.2 | 27.69 ms | 19.51 ms | 9.31 ms |

Separate 120-frame CPU captures including Editor work recorded the generator gizmo callback at about 6.99 ms before, 0.0028 ms after. Full frame average changed from 24.96 ms to 19.11 ms in those captures. After repair, SceneView.Paint still cost 4.62 ms and GameView.Paint 3.65 ms; game PlayerLoop was 10.41 ms. These nested profiler times are not additive. They explain why this is not solely a save/gameplay bottleneck.

Samples are observational during a live procedural run, not a deterministic replay or a standalone/mobile performance claim. No deep profiling. Recording and temporary probes were disabled/removed afterward. Preferences and unrelated local asset/settings edits were preserved. Editor compilation passed. No broad test rerun was needed for the early-return and startup policy correction.

Ignored evidence: Artifacts/UIRegression/editor-fps.txt, editor-fps-after.txt, editor-frame-breakdown.json, editor-frame-breakdown-after.json.

## Combat echo follow-up

Recorded per-frame combat-echo populations and CPU markers while the existing save ran. With the Game view maximized (other Editor panels hidden), the pre-change 35-second sample contained:

| Active combat echoes | Sample frames | Mean frame time | P95 | PlayerLoop |
| --- | ---: | ---: | ---: | ---: |
| 0 | 413 | 8.25 ms | 12.54 ms | 5.92 ms |
| 4 | 892 | 10.38 ms | 14.20 ms | 7.73 ms |
| 8 | 527 | 12.02 ms | 16.70 ms | 8.95 ms |
| 10 | 266 | 13.14 ms | 18.37 ms | 10.06 ms |

Ten-echo `EchoController.Update` averaged 0.297 ms and `CharacterAppearance.LateUpdate` 0.175 ms; physics simulation stayed below 0.02 ms. Thus the correlation is real, but combat AI alone does not explain the overall cost. Normal Editor layout added substantial repaint/hierarchy overhead: a representative eight-echo capture had 10.10 ms PlayerLoop and 8.69 ms EditorLoop. Its runtime UI repaint cost was 2.43 ms.

Move world icons and bars with UI Toolkit render transforms and DynamicTransform hints rather than changing left/top layout coordinates every frame. Fill widths, slice sizes, visibility, row centering and game rules remain unchanged. Editor compilation passed. Captured landscape and portrait screenshots were inspected; 16 non-row world anchors in each orientation matched the projected source positions within 0.23 panel units. Normal dimensions, time scale, Editor layout and disabled-profiler state were restored afterward.

A subsequent capture with six combat echoes measured 1.49 ms UI repaint, but the enemy/echo mix and procedural map differed. This is evidence of the remaining workload, not a controlled claim of total-FPS improvement from the transform change. Further performance work should measure world UI/rendering and Editor repaint costs, not reduce echo targeting frequency without evidence.

Ignored evidence: combat-frame-profile.csv, combat-max-profile.csv, combat-after-profile.csv, combat-compare.json, combat-ui-transform-breakdown.json, world-alignment.txt and world-ui-1920.png/world-ui-1170.png under Artifacts/UIRegression. Very large outliers adjacent to CLI compilation/profile extraction are diagnostic overhead and are not attributed to combat spawns. Population comparisons are observational, not deterministic replays.

## Controlled follow-up: appearance and world-bar work

The earlier changes did not establish a sufficient real-world FPS improvement. This follow-up isolates two avoidable costs and keeps their claims bounded:

- Translucent actors now retain eight sprites as data for the existing composite shader, without building eight unused SpriteRenderers. Opaque actors still lazily create their layered renderers; transparency changes, pooling and equipment updates preserve the original behavior. Appearance properties are reapplied when source frame, outfit/tool, tint, facing or sorting changes, rather than on every unchanged frame.
- World health/duration bars no longer call Background again for an unchanged sprite. That helper temporarily resets the slice scale to 1; restoring its actual scale caused avoidable repaint invalidation every frame.

Evidence:

1. Original and current appearance components ran side by side in one process against the same source sprite/profile. Seven paired rounds of 20,000 unchanged-frame updates: original 72.50-73.13 ms, improved 28.76-31.67 ms (median 72.80 versus 28.87 ms; about 60% less CPU in this operation). This is not a total-game FPS percentage.
2. A 20-pose opaque/translucent/mirrored render comparison was byte-for-byte identical (zero different pixels). SpriteRenderer count fell from 190 to 110; ten translucent actors account for the 80 removed renderers. The existing Play Mode character appearance test passed, including GPU transparency checks, pooling, equipment changes and facing. A separate skill-tool test selection returned zero tests and is not counted as passing.
3. Same frozen scene, old/new world panel code alternated twice. World render-data rebuild time: 1.074/1.122 ms old, 0.653/0.678 ms new. Average full frame: 16.085/16.568 ms old, 15.325/16.019 ms new. This stationary-scene A/B isolates redundant redraws; it does not predict the exact gain during every moving fight.
4. Final projection checks in landscape and portrait and their screenshots were inspected. All before/after experiments used a copied save root. Original progression and preferences are restored for normal Editor use.

A candidate to remove the apparently empty UI camera was rejected: it changed output pixels slightly. The camera remains enabled. The moving-world appearance A/B also had changing combat populations, so it is retained as diagnostic data rather than a claimed whole-game speedup.

Evidence in ignored Artifacts/UIRegression: appearance-before.png, appearance-after.png, appearance-ab-result.json, appearance-tests-after.json, actor-runtime-ab.txt, subsystem-cost.txt, world-ab.txt, world-alignment.txt. No gameplay timing, targeting, spawn limits or art were reduced.

## 20 October: frame-spike investigation and checkpoint policy

A quiet, non-deep Editor capture with a copied save and ten combat echoes recorded task-completion frames of 58–80 ms. Subsystem markers isolated 43–57 ms inside `Hero.TaskTick`, not combat targeting or Steam rich presence. An isolated completion receipt cost 44.5–58.3 ms. A phase breakdown measured snapshot contribution 0.53 ms, farm proposal 0.05 ms, detached full-save clone 4.94 ms, serialization 1.40 ms, waiting for disk commit 36.90 ms, and metadata publication 0.36 ms. These are measurements on this Windows Editor/save, not iPhone timings.

Matt clarified the desired policy: interval checkpoints and return to town, not per-task saves. The attempted asynchronous per-task receipt implementation was discarded. Task credits instead apply atomically to the in-memory farm/inventory and retain their replay receipt there. The existing checkpoint writer captures immutable bytes and writes in the background. Return-to-town now explicitly requests that writer; its previous `EventHandler.SaveData()` only gathered runtime contributions into memory. Existing mobile-background, quit and manual-save safeguards remain.

The ordinary autosave interval remains 60 seconds, with the existing initial 30-second delay. This cadence is a product choice, not an industry-wide mandated number. Abrupt termination may lose progress since the last successful checkpoint, as expected for this policy.

### Source review

This review uses primary sources from several organizations, rather than treating one Unity page as a complete persistence design:

- [Epic: saving and loading games](https://dev.epicgames.com/documentation/en-us/unreal-engine/saving-and-loading-your-game-in-unreal-engine) recommends asynchronous saving during active gameplay to avoid hitches; synchronous writes fit paused/menu contexts. This is cross-engine corroboration, not an Unreal API recommendation for Unity.
- [Microsoft: FileStream](https://learn.microsoft.com/en-us/dotnet/api/system.io.filestream) explains asynchronous resource-intensive I/O to avoid blocking the UI thread and distinguishes managed buffering from flushing underlying buffers. Our ordinary checkpoints already serialize access through one writer and flush their temp snapshot before publishing it.
- [Unity: JSON serialization](https://docs.unity.com/en-us/engine/6000.5/manual/scripting/compilation-and-code-reload/script-serialization/json-serialization) cautions against modifying data while another thread serializes it. Our save format remains Odin binary, but the same ownership concern applies: keep runtime mutation on the main thread and hand immutable captured bytes to background I/O, rather than handing it the live save graph.
- [Apple: preparing for background](https://developer.apple.com/documentation/uikit/preparing-your-ui-to-run-in-the-background) says to preserve user data and close files when deactivating; suspension/termination cannot be treated as a guaranteed orderly quit. Keep the mobile background checkpoint.
- [Apple: extending background execution](https://developer.apple.com/documentation/uikit/extending-your-app-s-background-execution-time) describes bounded background execution and expiration handling. It does not justify unlimited background work. No new background capability is introduced here; native iPhone lifecycle behavior still needs device validation.
- [Apple: atomic data-writing option](https://developer.apple.com/documentation/foundation/nsdata/writingoptions) documents writing an auxiliary file before replacing the destination; [Microsoft: File.Replace](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace) describes replacement and backup behavior. Our existing format uses verified, uniquely named generations and same-directory publication instead of overwriting the sole good save. Do not remove checksums, schema checks, old generations, authority checks or conflict handling to improve an FPS number.
- [Unity: managed-memory GC](https://docs.unity.com/en-us/engine/6000.6/manual/analysis/performance-memory/managed-memory/garbage-collector) explains cumulative allocation pressure. The in-memory credit path avoids serializing/deserializing the entire save merely to change a seed receipt.

Additional measured work: `FieldsWorldView.Refresh` repeatedly searched static town hierarchies and cost about 5 ms at each refresh even while adventuring. Cache the authored legacy farm renderers once, but continue suppressing them after quest visibility changes. Navigation full rescans were another 11–15 ms spike; the subsequent equivalence check and asynchronous scan change are described below.

### Subsequent audit and validation

The global read-only subagent audit identified automatic repeating-bed harvests as a second per-action durable save path. Those now use the same in-memory progression policy. Before preparing an in-memory farm command, ResourceManager captures its current amounts, discovery state, tiers and ledger statistics; this prevents a harvest from replacing live gains with an older save-tree amount. Mutable farm/resource records are detached for validation, then published together. Manual economic commands retain their explicit durable transaction path.

Measured task-credit application after removing per-task I/O: 0.454 ms first sample, then 0.129 and 0.118 ms (versus 44–58 ms before). This isolated operation result is not a whole-frame FPS claim.

The async full-navigation-scan experiment compared all 13,680 grid nodes: positions, walkability, penalties, tags and all eight connection flags matched the synchronous scan. Synchronous scan measured 11.246 ms; the largest asynchronous step measured 7.876 ms over four steps. Sampled additional allocation during this controlled experiment was 1,204,764 bytes. Initial map loading retains its one synchronous scan. Runtime segment shifts use A*'s complete asynchronous scan, not a strip-only update that could leave altered task colliders stale.

Task visibility lookup comparison on the live map had zero result differences. In alternating 5,000-call samples, the original per-candidate virtual-camera lookup took 9.814/9.885 ms, while a per-frame resolved output camera took 1.303/1.307 ms. The Unity Mono per-thread allocation counter returned zero even for a separate known-allocating probe, so its byte readings are not reliable and are excluded from the evidence.
The final credit path, including capture of live resource records, measured 0.728 ms cold and 0.326/0.312 ms warm. This supersedes the earlier 0.118–0.454 ms samples taken before that safeguard. Automatic harvest failures now retry at most once a second instead of allocating a new rejected proposal every frame; successful harvests retain their original immediate cadence.

Animation updates also recreated two bound delegates for each actor each frame. Caching those callbacks per actor reduced the measured Mono heap growth across 10,000 calls from 2,568,192 bytes to zero sampled growth (4.477 ms before, 3.219 ms after). This uses Mono heap-size sampling, not the unreliable per-thread counter; it is an isolated operation measurement and not an FPS percentage.

Six selected Play Mode checks passed after the final change: task credit/checkpoint behavior, automatic harvest/checkpoint behavior, cached legacy-art suppression, town-return progression, echo fishing-to-walking transition, and appearance/pooling consistency. Restoring the original FarmService made the task-credit regression fail for the intended reason: it found three packs on disk before a checkpoint, where the test expected zero. Restoring the fixed service returned all six checks to passing. The broader 62-test fixture previously had six other UI/graph/scrolling expectation failures plus a temporary-directory cleanup race; the cleanup race was fixed and its town-return test passed in the focused run. The remaining six were not established against pristine HEAD, so this is not a clean full-suite claim.
Final unattended 30-second Editor sample (timeline profiler off, preallocated timing array, no CLI calls during sampling): 2,025 frames, ten combat echoes throughout, 3840x2160, VSync 1, target frame rate -1, ordinary Editor layout. Mean 14.828 ms / 67.4 FPS, P95 20.640 ms, P99 26.490 ms, twelve frames above 30 ms, maximum 35.425 ms. The procedural population/workload differs from earlier captures, so this does not establish a before/after total-FPS percentage or eliminate all stutters. A prior timeline capture after the task fix still identified GC/incremental GC as a remaining outlier source. Further frame-drop work must profile that remaining workload rather than attribute every drop to combat logic.

The live iPhone 15 Pro simulator also regenerated its rounded screen mesh correctly (101 vertices, screen-mesh dirty flag cleared). The unchanged rounding algorithm is now invoked only for a dirty/replaced mesh. Simulator window discovery no longer traverses all loaded Editor objects ten times per second. The temporary simulator was closed and desktop resolution restored after inspection.
