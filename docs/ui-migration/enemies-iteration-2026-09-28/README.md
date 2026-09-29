# Enemies visual iteration — 28 September 2026

Original reference: `../baseline-2026-09-27/captures/40-tabs-4-Statistics_Window-Buttons-enemiesButton.png` (fresh profile, unknown enemies).

Enemies share the accepted Tasks row treatment: 32-unit framed artwork, native sprite PPU, masks filling the frame, bold names, muted IDs, consistent spacing and subtle separators. Known enemies use amber frames; unknown enemies retain neutral frames. Original four stat columns and full-width kill-reveal progress remain. The title explicitly labels Lvl, and the slider is labelled Preview distance. No combat, reveal, progression or distance-scaling formulas changed. Row order only changes when needed, with no measured performance claim.

The isolated developed-profile Editor review exercised all seven sort callbacks, distance zero/half/maximum, native sprite scale, four information columns and unknown-state hiding/progress. `checks-enemies.txt` contains passing checks. Screenshots include `1280-enemies-final.png`, `1280-enemies-unknown.png` and `1280-tasks-regression.png`. The phone-check capture is actually 844x390 despite the inherited 1280 filename prefix. Column bounds passed at that landscape size; this is not physical touch-device coverage.

Unknown-state validation temporarily removed only the presentation controller's kill-tracker reference and restored it; no progression was erased. Test timeScale was 0 and frame cap 30. Seven temporary save/network/feedback guards were restored byte-for-byte (`restoration.json`). No real save or cloud progression was used. The ignored Library scripts contain the capture harnesses.

## Stat icons

Enemy stat values now use the existing health, damage, defense, attack-rate and movement sprites in consistent 16-unit slots at native PPU. Bonus damage keeps a short Bonus label next to the sword; Vision and Kills retain text because they have no established stat icon mapping. Tooltips identify icon-only values. All eight values come directly from the presentation calculations, including unknown and out-of-range states; no formatted English string parsing is used.

`1280-icons-final.png` shows the current pass. `checks-enemy-icons.txt` verifies all eight values are present, distance and sorting callbacks, unknown artwork/progress, and smaller landscape bounds. Temporary guards restored and verified in `restoration-icons.json`. This icon pass increases row height to accommodate two native-scale icon lines.
