# Tutorial pointer and echo regressions — 20 October 2026

The standalone report exposed missing interaction coverage in the performance work.

- Keep Settings/Statistics in the left navigation group in landscape, including after switching out of portrait. The prior branch explicitly moved these into the right-hand container, overlapping return controls.
- Queue concurrent NPC meetings in every orientation. Previously only portrait queued them; landscape cards could cover earlier actions. Active meetings now sort above ordinary navigation/window panels, below intro/system dialogs.
- Interrupt an unfinished task when replacing it or clearing it. Previously SetTask only released its claim, leaving the fishing animator in its held casting state. Completed-task animations retain their existing completion path.

Three focused Play Mode regressions fail against f298c60eb and pass after repair.
They cover the real Echo/Fishing animator path, landscape navigation placement after portrait resize, and queued Meet/Next/Close pointer events. Meeting checks include Panel.Pick and EventSystem raycasts at 5120x2054, 1170x2532 and 1920x1080. The full game was also loaded using a copied save root; landscape rendering was inspected in Artifacts/UIRegression/landscape-fixed.png. Original player saves were not used by these checks.

Evidence: ignored Artifacts/UIRegression/before.xml and after.xml. A separate existing shoreline test filter returned zero tests and is not counted as passing. This focused pass is not a claim that every tutorial or UI surface has been exhaustively tested.

## Completed fishing locomotion follow-up

The Editor reproduced a second path: an echo moving to woodcutting remained in `Fishing_Idle_Down`. Retarget cleanup alone did not handle completed fishing. The shared Animator had no movement exits from its three fishing states, and a fast catch could pulse `CatchFish` before the initial cast reached the held state.

Add immediate movement-to-Run transitions to all three fishing states and allow the initial cast to consume an early catch trigger. The regression fails against the previous controller. After repair, a direct Play mode Animator probe passes all three states. The CLI test runner subsequently reported zero selected tests, so those attempts are not counted as successful test runs. A normal run is additionally checked using an isolated copy of the player save; see ignored `Artifacts/UIRegression/fishing-live-result.json` for observed movement and fishing transitions.

Live Editor result: 1,329 sampled frames, 10,769 moving-echo samples, 61 fishing-pose samples and seven fishing-to-Run transitions. Longest simultaneous moving/fishing observation was one transition frame, with no sustained stuck pose. Graph probe and live run used current controller; all player save I/O remained in the copied root.
