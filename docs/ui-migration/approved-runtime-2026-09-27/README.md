# Approved dark UI: integration and review

The approved HUD/Cauldron visual language is applied in the main Unity project, using the existing game services and navigation definitions. This folder contains actual Editor Game-view captures, not design mockups. Dark mode only. The old screen-space Canvas UI was checked at runtime: zero enabled legacy screen Canvases.

[Open the capture gallery](index.html).

## Review method and reproduction

Unity 6000.6.0f1; main scene; runtime UI Toolkit; reference panel 768 × 432, matching screen height. Broad captures use 1920 × 1080, 1280 × 720, 1024 × 768 and 2340 × 1080. Existing safe-area preferences are retained, so 4:3 captures have substantial vertical margins. VSync off, target 30 FPS; menus paused at time scale 0. Actual mixing, tasting and forge automation tests temporarily use time scale 1. This is UI validation, not a new performance benchmark or a Development Player certification.

Load a disposable copy of developed progression (Eva level 887, Ivan level 480, completed quest history, large resource collections), then open each TownWindowManager route. The private progression fixture is deliberately not committed. Capture after the view has laid out and one further frame has rendered. Inspect native screenshots, correct source, recompile and repeat affected states. Screenshots prefixed `1920-`, `1280-`, `1024-`, `2340-` are broad route captures; `state-` are secondary states; `final-`, `incidental-`, and `synthetic-` are targeted follow-ups. Earlier captures may precede a later correction; see the notes below.

`final-fresh-*` switches to newly constructed GameData in the running isolated session. These test fresh data bindings and locked-state layout, not a cold-start progression walkthrough. The quest manager can retain session-pinned entries, so those screenshots must not be treated as an exact pristine-save baseline.

Run breakdown/summary and reaping prompt screenshots use synthetic amounts and a forced presentation state. They validate layout, not natural death/reaping rewards. The leaderboard uses a local IToolkitLeaderboardSource fixture, including a long display name; no fixture scores were submitted.

## Coverage

| Surface | Evidence and review | Functional evidence / limits |
| --- | --- | --- |
| Navigation | `final-menu-*`, toolbar in every menu | Existing ordered definitions, map routes and progression rules retained; dropdown callbacks exercised |
| In-run HUD | `main-run-hud.png`, `wide-run-hud.png` | Actual Farmlands run, live health/progress; real buff assignment checked separately |
| Buffs / Prospector | `state-buffs`, `state-prospector`, `final-fresh-buffs` | Assign-to-slot and target confirmation passed; icons preserve aspect |
| Cauldron | Broad captures, mixing confirmation, details, Buffs/Eternal substates | Cancel, Mix consumption/reward, Start/Pause, ingredient scrolling passed; actual stew cost comes from manager |
| Forge / inventory | Broad captures and `state-forge-inventory` | Craft, Replace, and automation continuing with window closed passed; inventory reflows rather than clipping last column |
| Settings | `1280-Options`, import/language substates | Real controls retained; save/import/export destructive workflows not executed |
| Statistics | `state-stats-0` through `state-stats-5`, synthetic leaderboard | All six tabs captured; real online results not requested in isolated testing |
| Skills | Developed and fresh-data captures | Original passive/active/milestone content and handlers retained; every milestone combination not exercised |
| Quests / inventory | Completed history and fresh-data captures | Turn-in/pin callbacks retained; not an all-quests completion test |
| Alter Echoes | Broad captures and `state-alter-echoes` | Full rate/storage/card-power text retained, overlapping rows fixed; all generator combinations not exercised |
| Library / Credits | Broad captures, expanded Library chapter | Original text/localization bindings retained; scroll/disclosure presentation checked |
| Run breakdown / summary / death | `run-*` | Synthetic layout captures; actual death/reaping/restart end-to-end remains a separate regression check |
| Intro / notification / meeting / recovery | `incidental-*`, `overlay-meeting-dialogue`, `overlay-recovery` | Original authored dialogue; diagnostic recovery content is synthetic; no real recovery performed |
| Quit / console | `final-quit`, `state-console` | Confirmation/cancel shown; application exit not invoked |
| Loading / world indicators / pinned goals | Source/route inspection, world content in HUD captures | Existing gameplay systems retained; not every transient state independently captured |

## Corrections driven by the review

- Shared dark palette, thin borders and button states; removed light runtime variants and detached navigation shadows.
- Restored prototype HUD layout, aspect-fitting art, fixed-size buff controls, border progress and external automation indicator.
- Cauldron uses actual resource values and skill-modified stew cost; Eternal entries show the real Infinity name/value, not a fabricated tier.
- Restored card-gain visual feedback and corrected modal cleanup before UI construction.
- Settings slider tracks were accidentally affected by a broad scrollbar selector. Scoped the selector to vertical scrollers and recaptured actual sliders.
- Inventory columns now fit the available companion width; Alter Echo rows expand to their content instead of overlapping.
- Statistics graph colors remain distinguishable against dark backgrounds. Leaderboard score formatting no longer exposes TMP markup in literal text labels.
- NPC portrait frames respect authored animation padding without changing width/height proportions.
- Run summaries hide the breakdown-only rate legend and retain an outside-close hint.
- Notification roots receive the same surface treatment as nested surfaces.
- Compact Forge uses stacked toggles, narrower side columns and a scrolling work area. 4:3 Cauldron ingredients use three columns to avoid splitting names into fragments; tasting retains a scrollable area when height is limited.

A gameplay check found that ForgeAnalyticsService was absent in the running main scene: crafting worked but counters did not advance. CraftingService now ensures the analytics service exists independently of the Forge window. The repeated actual crafting checks pass after that correction.

## Recorded checks

- `functional-actions.txt`: twelve passing gameplay/action checks, including 15 repeated Cauldron/Forge open-close cycles. This is not a long-session memory leak measurement.
- `capture-routes.txt`, `alternate-routes.txt`: route availability and HUD configuration.
- `interaction-review.txt`, `final-review.txt`, `run-review.txt`, `incidental-review.txt`: attached sprite Images use ScaleToFit; this check alone does not certify visual quality.
- `overlay-review.txt`: no active legacy screen-space Canvases.
- Some first overlay capture attempts changed the view in the same frame as requesting ScreenCapture. `overlay-intro-clean.png` and `overlay-notification.png` are superseded by `incidental-intro.png` and `incidental-notification.png`; do not use those earlier files as evidence.

## Remaining validation limits

Physical mobile touch/keyboard/safe-area testing, controller-only navigation, every locale at enlarged text, standalone Player rendering, real cloud responses, save recovery/import/export, offline reward end-to-end processing, and natural death/reaping cycles are not certified by this pass. The migration still contains hard-coded copy alongside existing localization bindings; full localization extraction is separate unfinished work. Some secondary screens retain inherited inline dimensions, although shared palette/control styling is centralized.

The capture/review loop found and fixed concrete defects. It does not justify claiming every gameplay path or device has been exhaustively verified.

## Safety

Testing uses a distinct product/company identity, Library-only saves, and temporary Steam/UGS/feedback guards. Real save directories are never used by the fixture. Temporary guards are restored from byte-exact backups after validation; restoration status is recorded separately. Test harnesses and private fixture data remain in ignored Library/temp directories. No generated builds are committed.

Targeted `small-*` captures supersede the initial 4:3 Cauldron/Forge layout. `small-review.txt` also checks actual tasting-scroll reachability and the card-gain highlight class. A Pipeline command timed out during domain/startup work; subsequent runtime checks reported no gameplay exceptions.

The final action check was repeated after converting Forge to a scrollable work area: all twelve recorded checks still passed. The last visual follow-up also scopes native scrollbar input styling so default white arrow buttons and field backgrounds do not leak into dark panels.

Isolation cleanup completed: all seven guarded sources were restored byte-for-byte, original company/product identity restored, Editor left out of Play mode. See `isolation-restoration.json`. Existing unrelated workspace changes were preserved.

Final restored-project compilation: no errors, two lifecycle-analyzer warnings. `ToolkitConsoleScreen.Instance` is cleared in OnDisable; ForgeSession removes its load subscription in OnDestroy (the analyzer asks for OnDisable). These are recorded rather than described as a warning-free build. Editor verified stopped, with the original company/product identity.
