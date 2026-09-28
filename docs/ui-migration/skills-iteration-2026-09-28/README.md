# Skills window iteration — 28 September 2026

Selected-skill totals now use an isolated presentation accumulator containing that skill's unlocked milestones, respecting passive versus active replacement. Cross-skill XP remains attributed to the source skill (Combat's Mining XP remains under Combat). Live gameplay aggregation is unchanged. Skill-level task speed is shown only for the selected skill. Global active slots, active milestones and set descriptions remain visible; All bonuses expands the previous complete totals.

Entries use the existing stat/skill/task/echo artwork with aspect-preserving images in a shared 24-unit frame. Task-unlock art is framed too. Thin separators are absolute overlays and add no row height. Milestone names are bold and the quieter next-level label stays on the same line. Simple English increase descriptions become +amount effect, retaining full echo conditions, counts and duration. Unrecognized/custom descriptions retain their original wording.

Validation uses the isolated developed profile, Steam/UGS/feedback disabled, and no real save writes. Captures cover all six tabs at 1280x720, Combat at 2340x1080, active state, and expanded All bonuses. `checks-1280.txt` records source attribution, unchanged live totals after presentation refresh, actual toggle callbacks and image aspect preservation. An initial toggle assertion held the old row after a refresh rebuilt it; the final assertion re-queries the live row.

This focused pass does not establish full localization or physical touch-device coverage. Existing English copy remains English. UI review remains iterative.


## Pixel-scale correction

The fixed 20x20 fit box was incorrect: it independently scaled each sprite, while the shared `.icon` class also contributed an unwanted right margin. Entry art now uses a plain Image at `sprite.rect * 16 / sprite.pixelsPerUnit`, centered inside the previous 21x20 hidden-overflow mask. The new outer frame remains. This applies to both task art and new stat/XP/echo icons. Large art is cropped, not resized to fit.

All bonuses has no arrow. Its standard button padding and centered label are restored; toggling still expands/collapses the full summary. References: `1280-pixels-combat.png`, `1280-pixels-logging.png`, `1280-pixels-farming.png`; checks in `checks-pixels.txt`. Geometry assertions allow one physical pixel of Unity layout rounding at 1280x720 (16 reference units resolves to 16.2 at this scale); source dimensions retain the exact PPU formula. Final checks include centering, mask bounds, native scale and disclosure behavior.

## Border-fill correction and enabled colour previews

The mask now fills 100% of the frame content area, removing the previous inset while retaining native sprite PPU. Final user-selected colours: unlocked entries use warm amber #d0a166; active milestones override that border with muted purple #b38ac9. Locked entries retain their normal border. The earlier preview captures below show the original green unlocked border and three proposed active colours, rather than this final combination.

Actual isolated Editor captures: `1280-borders-A-purple.png`, `1280-borders-B-blue.png`, `1280-borders-C-amber.png`. `checks-borders.txt` verifies all four mask edges, native PPU, activation and return to unlocked styling when disabled. All checks passed. Temporary save/network guards were restored byte-for-byte; see `restoration-borders.json`.

## Checkmark alignment and summary icons

The shared checkmark painter now adds contentRect.position to its path coordinates. Previously it used the inner dimensions with an outer-origin position, shifting ticks up and left by the border width. `checkbox-alignment.png` is an isolated native UI Toolkit render showing checked/unchecked and labelled controls with one- and two-unit borders. Geometry is recorded in `checkbox-alignment.txt`.

Left-side active milestones and bonus summaries now use existing stat, resource, XP, task-speed and echo sprites, including expanded All bonuses. Structured presentation metadata supplies icons without guessing from English text. Shared stat text remains compatible with legacy consumers. Native sprite PPU is retained in consistent masked icon slots; values, wording and source-skill attribution are unchanged.

Final six-tab screenshots: `1280-summary-icons-combat.png`, `1280-summary-icons-mining.png`, `1280-summary-icons-logging.png`, `1280-summary-icons-fishing.png`, `1280-summary-icons-farming.png`, `1280-summary-icons-looting.png`. These show the final amber unlocked / purple active borders. Captured at 1280x720 in the Editor with the disposable developed profile, timeScale 0 and a 30 FPS cap. `checks-summary-icons.txt` verifies icons, wording/values and aspect preservation for every tab; all passed. Temporary guards were restored byte-for-byte (`restoration-summary-icons.json`).
