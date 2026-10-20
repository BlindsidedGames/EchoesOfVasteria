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
