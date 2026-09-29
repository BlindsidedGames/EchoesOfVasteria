# Tasks visual iteration — 28 September 2026

Original reference: `../baseline-2026-09-27/captures/40-tabs-5-Statistics_Window-Buttons-tasksButton.png`. This was captured with a fresh profile, so task names/art remain unknown. The files named inventory-Tasks_Button are not valid Tasks panel references.

Tasks retain the original name, ID, completions, time, XP, spawn chance/weight, improvement threshold, distance constraints and four sorting modes. Known-task art now uses native sprite.rect * 16 / pixelsPerUnit sizing inside a full-inner-frame mask. Names are bold, IDs muted, and aligned rows have subtle separators. Task-only styling leaves enemy rows unchanged. The checkbox is labelled Boost because TaskWeightService applies a spawn-weight bonus; it does not enable/disable the task. Known frames are amber, boosted frames purple. Unknown tasks retain neutral frames, hidden artwork and no boost control. Existing weight calculations and progression are unchanged.

Rows are retained during refresh. Reordering only moves a row when its position differs, instead of moving every row unconditionally; no measured performance gain is claimed.

Actual isolated Editor captures use a disposable developed profile, timeScale 0, 30 FPS cap, at 1280x720 and 844x390 landscape. `1280-tasks-final.png` is the final desktop view. The `1280-tasks-phone-check.png` filename is inherited from the harness; its actual dimensions are 844x390. `checks-tasks.txt` verifies all sort callbacks, native PPU/mask geometry, the boost callback/weight change, restoration of its original test preference, and control bounds. All checks passed.

The unknown-state capture temporarily removed only the presentation controller's tracker reference, exercised the existing null-tracker path, and restored the reference. It is a presentation fixture, not a new-save lifecycle test. `checks-unknown.txt` verifies neutral borders, no revealed artwork and hidden boost controls. Physical touch input, translations and the other statistics tabs were not comprehensively tested in this pass.

Save/network/feedback guards were removed and all seven original files hash-verified (`restoration.json`). Test harnesses and disposable saves remain ignored under Library.
