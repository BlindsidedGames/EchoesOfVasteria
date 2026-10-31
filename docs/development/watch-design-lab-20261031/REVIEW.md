# Watch design review

The design lab is separate from TestFlight 188. It uses native SwiftUI, the unchanged shipping snapshot decoder, controlled preview data and the game's original sprite pixels. No screenshot here is a shipped feature or a physical-device test.

## First native pass

Inspected both contact sheets containing all ten actual 42 mm and 46 mm simulator captures. This pass was rejected for the following specific issues:

| Design | Required revision |
| --- | --- |
| Expedition dial | Enlarge the undersized character and tighten the empty centre. Do not present cached health as a background health estimate. |
| Living trail | Fix the clipped water strip and replace the plain platform-like slab with a coherent riverbank using authored pixels. |
| Combat watch | Use a plain Health or Combat title and reduce unused vertical space. Retain last-phone health and age. |
| Harvest counter | Use Run loot or Resources, balance the gap above the rates action, and check compact large values. |
| Since you left | Remove the duplicated elapsed reading. Fit all four gain tiles and distance inside the small Watch viewport. |
| Run goal | Use the saved character or meaningful target art. Keep units clear and round remaining distance upward. |
| Skill book | Reduce wasted space and oversized cells so more than one row is useful in the first viewport. |
| Run ribbon | Use Recent runs, include units in the selected value, and keep controls and legend in view. |
| Run comparison | Preserve the readable stacked cards, but fit the comparison delta and context beneath them. |
| Map dossier | Tie map selection to the map heading; replace the disconnected oversized chevron. Distinguish recent records and qualify estimates. |

Across the set, fix the system-clock safe area and content compression instead of shrinking everything. Keep large-type layouts scrollable. The next native captures must be reviewed before accepting final screenshots.

An independent visual pass also caught inconsistent displayed arithmetic: 145/min and 137/min were paired with a +7/min delta. Related values must use one visible precision. Likewise, 2.6k latest versus 2,550 m recent best makes equal values appear contradictory. The goal screen needs its own target-led composition, not a recoloured copy of the expedition ring.

Changing the Dynamic Type environment alone does not scale fixed point-size fonts. The large-text capture must visibly exercise scaled text and a usable scrolling layout before it can count as accessibility evidence.

## Fourth native pass

Inspected all ten layouts in both revision-4 contact sheets. The clock collisions and normal-size clipping are resolved. The larger personalized character, compact gain cells, six-skill grid, explicit chart units, leading comparison delta and consistent map distances are substantial improvements. The visible comparison arithmetic now agrees: 145/min minus 138/min produces approximately +7/min.

Remaining refinements: show actual water in the river vignette, match the character and Carl's sprite pixel scale, remove the duplicate health age, and compare a target-led linear goal layout against the second circular dial. The goal caption currently touches the segmented arc. Large-value formatting should use uppercase K/M/B so millions cannot be mistaken for the metres unit. Native state, large-text, interaction and animation evidence is still required before final acceptance.

## Data checks

Distance progress uses the current effective cap, and disappears on maps without distance reaping. Background gain counters use the timeline origin and stop at expiry. Health is the last phone reading. Rates use matching totals and time. Skill data contains levels, not XP percentages. History contains the last ten runs overall, so map summaries are recent samples, not lifetime records. The local goal belongs only to the lab session and awards nothing.

The separate background-distance bug was repaired and tested on the prototype branch in `0ae560c76`; its pre/post evidence is under `../background-live-activity-prototype/history-reach-fix-20261031/`. It does not alter this lab's snapshot format or retroactively update TestFlight 188.

## Final native pass

Reviewed all ten at 42 mm and 46 mm, plus large-text and large-value variants. The final revision fixes the small heart's intrinsic sizing, aligns the trail actors on the grass lip, keeps Carl's wings intact, renders partial goal segments exactly, separates the comparison rate unit, and removes the misleading New run message after estimate expiry. Reaped fixtures now agree with their distance cap and zero health. Skill pinning moves the selected skill first; estimated duration is labelled away time.

All normal-size primary layouts fit the small Watch viewport. Larger text intentionally scrolls; a screenshot of its first viewport alone does not verify that lower controls are reachable. Interaction verification is recorded separately from visual acceptance. Controlled offline, no-data, no-reap, terminal, restart, expiry and beyond-history cases are available beside the main captures.

### Recommended direction

Use Expedition dial as the normal Run page and Since you left when the phone is backgrounded. Keep the animated trail as a selectable Run presentation. Put the skill book under Progress and recent runs under Journal, with comparisons and map records as details. The health, resource and local-goal screens are focused alternatives, not a requirement to swipe through ten permanent pages.

The full Watch app can animate its authored sprites while active. Wrist-down and Reduce Motion should stay still. The Watch can advance a phone-supplied estimate locally; it cannot make a suspended iPhone generate new gameplay snapshots. These prototypes use controlled fixtures and do not validate production WatchConnectivity, physical wrist-down behaviour, haptics or phone battery use.

### Gallery verification

The gallery includes all 20 final raw captures, the corresponding 20 first captures, and controlled state variants. The builder checks that every linked native image and clip exists; its JavaScript passes Node's syntax check. `open_in_codex` opened the HTML in the Codex browser panel. Browser Use rejected binding that local file URL under its URL policy, so the gallery's dropdowns were inspected in source, not exercised through browser automation. Native screenshot inspection above was performed directly on the PNG files.

### Native interaction evidence

The Mac simulator's standard Device → Gestures → Double Tap action opened the real goal editor and changed its target from 100 to 500. The same supported gesture scrolled the large-text Skillbook to Farming and Looting. These are native view events, not launch-argument screenshots. Ordinary coordinate touch and Crown rotation could not be verified because the simulator input route returned windowNotFoundAtPosition. Other controls remain implemented but unverified through ordinary input. The app's Refresh action is a local fixture demonstration, not proof of suspended-phone delivery.

The final native proof is in [PROOF.md](PROOF.md). The gallery includes six recordings: foreground animation at both sizes, the Always On transition and still state, static phone health while backgrounded, and the native goal editor interaction. Static variable-frame-rate viewing copies retain the last captured frame; raw originals and exact transforms are documented in [video-provenance.json](video-provenance.json). Root also inspected extracted foreground frames and the goal-change / large-text-scroll screenshots.

The estimator repair and lab have been combined on the review branch. The canonical background prototype branch independently retains the same distance fix. Neither change has been uploaded to TestFlight or merged to main.
