# Profiling evidence — 26 September 2026

Start with [the report](../../PerformanceProfile.md), [key Editor comparisons](KeyResults.md), [map/progression matrix](MapMatrix.md), [all Editor windows](ScenarioResults.md), and [rendered Player windows](player-rendered/ScenarioResults.md).

This folder contains 249 Editor capture windows and 36 rendered Development Player windows. Some are transition, synthetic stress, probe or housekeeping captures. The report identifies rejected/contaminated trials; counts do not imply independent replications or exhaustive coverage.

## Files and metrics

- `frame-captures.zip` in each capture directory: losslessly archived `<scenario>.csv` per-frame observations. Times named `unscaledDelta_ms`/`wall_ms` are ms; Unity timing recorder values are ns; GC/memory recorder values are bytes. Raw first two boundary samples are preserved.
- `<scenario>-setup.json`: synchronous setup duration plus initial state, Editor only. Fixture loading is part of setup and not pure generation cost.
- `<scenario>-meta.json`: before/after progression, resolution, VSync/cap/scale, automation, memory, objects, GC and (Editor) global event subscriptions.
- `summary.csv/json`: derived percentiles, throughput and available metrics. First two boundary frames excluded; raw maximum remains separately available. `ScenarioResults.md` is a compact view, not every metric.
- `throughput-observations.json` and `craft-throughput.csv`: live tracker / successful-craft observations. Do not use stale Editor save totals for craft or run throughput. Partial craft-observer window is flagged.
- `timeline-*`, `map-cpu*`, `layout-subtree-*`, event, pool, save and offline files: targeted experiments. Earlier empty `cpu-*` timelines are not evidence. Initial premature offline probes and the first pause-backlog tasting sample are excluded in the report.
- `player/`: rejected hidden-window pilot, never representative rendered performance. `player-rendered/`: successful visible Player pass. Round 1 welcome overlay contaminates clean town/UI comparisons; use rounds 2–3.
- `instrumentation/*.cs.txt`: inactive records of temporary scripts. They are not Unity assets and are not auto-executable. Inspect before adapting; scripts contain run-specific paths, assumptions and queue insertion indices.
- `cleanup-*`, `real-save-integrity.txt`, `progression-restored.txt`, `editor-resumed.txt`: restoration evidence. `restore-error.txt` is an initial helper failure resolved as described in `restore-error-resolved.txt` and the report.

Main Thread/PlayerLoop can contain waits and Editor overhead. FrameTimingManager CPU/render/GPU data is present for the rendered Player and can lag the observed frame. All-zero GPU/render/draw/batch counters are unavailable, not zero cost. No Deep Profile was used; focused Profiler timelines have additional overhead. No memory snapshot ownership graph or GPU pass capture was taken.

## Recalculate tables

From the repository root, with Python 3:

```powershell
python docs/profiling/2026-09-26/analyse.py
python docs/profiling/2026-09-26/analyse.py docs/profiling/2026-09-26/player-rendered
python docs/profiling/2026-09-26/keytables.py
```

The analyser reads frame-captures.zip directly; extracting the CSVs is optional. Every archived entry was checked byte-for-byte by SHA-256 before the loose CSV was removed. No extra Python packages are required. Percentiles use linear interpolation per capture. Rates are per real second, including Slipstream. Follow the report when interpreting synthetic states, partial observations, cold/transition windows and instrumented probes.

## Reproduce safely

1. Use the same dirty source state, Unity 6000.6.0f1 and asset catalogue. The base commit alone is insufficient. Record current source state, hardware, rendering preferences and editor layout. `workspace-status-delta.json` shows this audit added only its documentation paths relative to the initial status.
2. Preserve the real saves and live state before profiling. This audit's full Odin baseline remains private at `Library/PerformanceAudit/original-live.bin`; its hash is in `isolation.json`. It is intentionally excluded from these captures. Public metadata documents distinguishing progression and settings but does not reconstruct every resource/card value without that local baseline.
3. Use a disposable project/product and private SaveManager root, disable Steam/UGS initialization/submission, and suppress normal autosave before loading copied or synthetic data. The autosave supplement additionally blocks feedback requests and restores slot-card PlayerPrefs. Do not run these scripts against unguarded real progression.
4. Adapt `Session.cs.txt`, then the fixture scripts (`Extend`, `Forge`, `ExtraAudit`) and the observers. Follow the report's scenario procedures; warm 5 s and capture 15–20 s ordinarily, three repetitions for key pairs. Do not blindly run scripts in alphabetical order or reuse their queue indices. Cloned config changes are synthetic controls, not progression facts.
5. For Player validation, copy Assets/Packages/ProjectSettings to a disposable external project, apply only that copy's audit runner and online guards, change product identity, enable frame timing, and build a Windows Mono Development Player. `PlayerAuditRendered.cs.txt` and the builder capture the measured setup. Final visible-build guards were unconditional Steam Awake / UGS initializer / UGS upload early returns; `PreparePlayer.ps1.txt` records an earlier conditional-guard iteration; `final-player-network-guards.patch.txt` records the final diff against original source. Launch visibly, confirm nonzero rendering/timing, and set caps/VSync after each LoadData. The hidden pilot is rejected.
6. Restore and verify hashes/state after removing every callback and runtime clone. Advance disposable clocks before restoring a long-paused original state so unscaled time cannot grant an audit-duration catch-up. Remove temporary executable sources. The saved instrumentation is evidence, not a production feature.

Map terrain uses an independently randomised System.Random seed; setting Unity.Random to 260926 did not make these runs deterministic. Capture matching settings and repeat runs; use a proper deterministic terrain/encounter fixture before exact combat/task correctness claims.

The external project/build is under `%LOCALAPPDATA%/Temp/EOV-Profile-20260926`, outside version control. It retains audit-only compiled instrumentation and network guards for local reproducibility, and must not be shipped or used as a normal game build. The original project has no active audit instrumentation.
