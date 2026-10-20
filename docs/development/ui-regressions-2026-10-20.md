# Tutorial pointer and echo regressions — 20 October 2026

The standalone report exposed missing interaction coverage in the performance work.

- Keep Settings/Statistics in the left navigation group in landscape, including after switching out of portrait. The prior branch explicitly moved these into the right-hand container, overlapping return controls.
- Queue concurrent NPC meetings in every orientation. Previously only portrait queued them; landscape cards could cover earlier actions. Active meetings now sort above ordinary navigation/window panels, below intro/system dialogs.
- Interrupt an unfinished task when replacing it or clearing it. Previously SetTask only released its claim, leaving the fishing animator in its held casting state. Completed-task animations retain their existing completion path.

Three focused Play Mode regressions fail against f298c60eb and pass after repair.
They cover the real Echo/Fishing animator path, landscape navigation placement after portrait resize, and queued Meet/Next/Close pointer events. Meeting checks include Panel.Pick and EventSystem raycasts at 5120x2054, 1170x2532 and 1920x1080. The full game was also loaded using a copied save root; landscape rendering was inspected in Artifacts/UIRegression/landscape-fixed.png. Original player saves were not used by these checks.

Evidence: ignored Artifacts/UIRegression/before.xml and after.xml. A separate existing shoreline test filter returned zero tests and is not counted as passing. This focused pass is not a claim that every tutorial or UI surface has been exhaustively tested.
