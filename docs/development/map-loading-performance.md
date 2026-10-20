# Map-loading performance — 20 October 2026

## Retained changes

- Show loading before teardown/instantiation and reject duplicate entry requests.
- Destroy/deactivate the old map before installing another pathfinder. Disable
  prefab auto-scan; retain the final scan after terrain and collider completion.
- Batch terrain writes/clears per segment. Keep decoration matrix resets.
- Replace fixed frame batches with work budgets: 6 ms during initial loading,
  2 ms while streaming during a run. Resume each budget after yielding.
- Cache terrain/boundary samples for one immutable decoration pass only.
- Sort/select initial tasks once generation completes, then restore normal behavior.
- Reuse at most two inactive stateless scenery instances per pool, sixteen total,
  only when restarting the same map. Full cleanup still applies to town/map changes.
  Stateful gameplay/animation instances are excluded. Discard empty prefab pool
  closures so they cannot retain assets. Restore authored/random sprite flips on reuse.

## Editor comparison

Unity 6000.6.0f1, RTX 4080 SUPER, 1080 × 2340, same isolated Editor, domain reload,
normal Loading/Main startup, copied synthetic high-progression fixture, seed 9173,
60 FPS target. Baseline runtime is e4b924c2a (same runtime as f882be5a0); final is this
change. Seven entries use the normal BeginAdventure path. No real save is modified.

| Map | Before | After | Frames before / after |
| --- | ---: | ---: | ---: |
| Farmlands entry | 2.015 s | 1.374 s | 61 / 43 |
| Farmlands restart | 1.534 s | 1.267 s | 62 / 46 |
| River | 1.157 s | 0.444 s | 56 / 17 |
| Woods | 1.327 s | 0.710 s | 61 / 29 |
| Mines | 1.411 s | 0.781 s | 61 / 26 |
| Spooky entry (Haloween) | 1.241 s | 0.408 s | 61 / 13 |
| Spooky restart | 1.229 s | 0.387 s | 62 / 13 |

Each entry scans twice before and once after. All seven final entries display
loading immediately, finish generation and resume movement. The five entries with
authored gathering tasks have nonempty task lists. Spooky has no authored gathering
tasks; zero there is expected. Final full-pass evidence is in the ignored
`Artifacts/StandaloneMemory/map-editor-verified/` folder; baseline is
`map-editor-before/`.

This is a local Editor comparison, not a physical iPhone benchmark. Runs were
sequential rather than randomized cold-cache trials, and later passes benefit from
Editor/asset warmup. Treat wall-time percentages as indicative; removed scan and
frame counts are directly observed. Editor total allocation includes Editor
resources and is not a device-footprint measurement. A cold first entry still has
a roughly 192 ms largest frame; this does not establish hitch-free loading.

The initial 2 ms budget everywhere regressed Farmlands; it was rejected in favor of
6 ms during the loading overlay and 2 ms for in-run work. Two subsequent candidate
passes completed with faster entries. No texture, sound, density or map artwork
changes are included.

## Validation and review

- Final CLI Play Mode tests: 2 passed, 0 failed. Covers bounded reusable scenery,
  excluding stateful objects, preserving active objects/full release, and one
  initial sort after task registration. XML: `map-loading-tests-final.xml`.
- 18/18 fixed-seed terrain/decor hashes match the original implementation: six
  configs × three seeds, four segments including clear/regenerate, tile matrices
  and sprite flips. This does not assert identical task/enemy positions: those use
  the existing shared Unity random stream across frames.
- All 239 original save/profile files match their pre-work SHA-256 manifest.
- Reviewed same-map retention, cross-map teardown, callback ordering, failed spawn
  attempts, null terrain samples, zero-size segments and stale pooled sprite flips.
- The probe initially rejected Spooky's legitimately empty task list. Corrected
  the observer to inspect authored task availability and reran all seven entries.
- Existing Editor search-index exceptions/Hub IPC warnings occurred outside game
  execution; no new game exception in the completed final pass.

No standalone build was continued after the request to prefer Editor validation.
No Mac coordination, merge or upload. These changes are not in internal build 166;
physical iPhone 13 mini validation remains outstanding.
