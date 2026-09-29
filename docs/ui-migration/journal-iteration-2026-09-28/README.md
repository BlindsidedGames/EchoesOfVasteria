# Map journal iteration - 28 September 2026

General statistics now offers Overall plus the five original map-art selectors. Recent performance and four recent runs sit beside the selected map's complete lifetime statistics. Run rows expand the existing complete run breakdown on click and collapse on a second click. Graphs remains available separately for the full retained history; an embedded trend graph is not part of this first iteration.

Rates use summed resource/task/kill totals divided by summed saved run duration in real minutes, not an unweighted average of individual run rates. The sample count explicitly identifies matches within the globally retained last 50 runs. No lifetime time/run counters were invented or backfilled. Existing historical map counters and recent history are displayed as stored, even if they disagree (the disposable developed profile has Farmlands lifetime deaths=0 but 11 deaths in its retained history).

Found here lists positive-weight resource drops from configured positive-weight task categories and enemy pools. It is a configured pool catalogue, not a guarantee that each entry is currently available at a particular skill level/distance or terrain. Discovery checks are account-wide resource receipts/enemy kills; no per-map discovery history is claimed. Unknown entries conceal names and artwork. Previews show 12 entries per group with Show all / Show fewer controls. Clicking an entry shows its name, including on touch; desktop hover is supplemental. No new progression fields or gameplay rules were added.

Native artwork dimensions use sprite.rect * 16 / pixelsPerUnit inside full masks. Stats keep shared spacing, muted labels and separators limited to their content. No nested decorative panels. UI history and collection construction run on selection/history changes; live totals update without rebuilding the whole journal on every distance event. Closing releases journal visual references.

## Validation

`1280-journal-final.png` is the 1280x720 Farmlands capture. Per-map, Overall, expanded-detail and smaller landscape captures are alongside it. The phone-check image is 844x390 despite its filename prefix. `checks-journal.txt` passed map-selector callbacks, all original lifetime labels/values, filtered sample counts for each map, native assigned sprite dimensions, empty-history state, run detail open/close, collection expansion/collapse and click detail, and smaller-screen label/value bounds. A first PPU harness version incorrectly inspected zero layout sizes of collapsed elements; the final check uses explicitly assigned width and height.

The empty-history check temporarily cleared and restored the disposable in-memory run list in a try/finally block. Validation used the isolated developed profile, offline guards, timeScale 0, VSync off and 30 FPS. Guarded source files were restored byte-for-byte (restoration.json), normal project identity restored and Play mode stopped. Real saves and cloud progression were untouched. Harnesses remain ignored under Library.

Remaining: physical mobile touch, text scaling/localization, portrait readability, and a fresh-save end-to-end discovery journey were not validated. This is a first visual iteration for review, not approval of the final layout.

## Sidebar revision

`sidebar-journal-final.png` supersedes the initial top-selector capture. Selectors now occupy the left edge. Lifetime is on the left and Recent runs on the right; the Recent performance rates/summary section and Found here explanatory line were removed at the user's request. Discovery scope remains available on the count tooltip. All five map, lifetime/sample, native artwork, run-detail, collection, empty-history and smaller-landscape checks passed again (checks-sidebar.txt). The preview was stopped and all guards restored byte-for-byte (restoration-sidebar.json).

## Full-height Recent runs revision

`columns-journal-final.png` supersedes the sidebar capture. Lifetime and Found here share the left scroll column, with Enemies above Resources. Recent runs occupies a separate full-height right column and lists every retained matching run rather than four. Run details expand immediately below their own row. Both columns scroll independently.

The first visual check exposed a scroll-wrapper sizing error; the column class was moved to the wrapper in source and the same correction applied live for recapture. Map/lifetime/sample/native-dimension checks, all retained runs, enemies-before-resources order, click expansion and empty states passed (checks-columns.txt). A separate runtime check moved the right scroll offset to 120 and verified that the left offset remained unchanged, then restored the right offset. Guard restoration is recorded in restoration-columns.json.
