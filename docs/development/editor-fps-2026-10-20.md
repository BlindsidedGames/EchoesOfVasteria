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
