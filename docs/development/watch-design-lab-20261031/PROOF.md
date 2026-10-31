# Native Watch lab evidence and limits

Ten distinct native SwiftUI designs were built and iterated on the owned Series 11 42mm and 46mm watchOS 26.4 simulators. The initial, intermediate and final PNGs are real simulator captures. Normal final screenshots are 374×446 and 416×496 pixels. Contact sheets are labeled derivatives. The independent coordinator inspected all 20 final normal screens and accepted the final layout refinements.

## What was verified

- The isolated Watch-only target builds with Xcode 26.4.1 and no signing. Its bundle is `com.blindsidedgames.echoes.watch.designlab`; the shipping Watch app and shared snapshot decoder are unchanged.
- All 18 fixture files decode through the shipping decoder. The executable validation also checks frozen restart counters, the controlled +1% reap cap, frozen terminal/expired gains, no distance reap, and current 5200m beyond retained Mines best 2550m with the real 38000m effective cap. This is presentation-contract evidence; the coordinator separately owns the managed estimator regression/fix.
- Every design has native initial/final captures at both sizes. The second and third capture batches preserve actual rejected fit revisions. The revision log records observed defects and changes. Large text genuinely increases glyph sizes through `ScaledMetric`; dense content scrolls instead of claiming every large-type field fits one viewport.
- Large values, no snapshot, town, offline, no distance reap, restart, expiry, ended runs, no retained history and beyond-history examples were captured where relevant at both sizes. Maps expose only IDs known from the current run or retained records; a controlled known-current map with no retained records stays honest.
- **Actual native control event:** Simulator Device→Gestures→DoubleTap opened Choose goal. Further DoubleTap changed the resource preset from 100 to 500 (and an additional gesture reached 1K). The native editor screenshots and18s goal clip show the control and quantity change. These are real Watch gesture actions, not launch-argument substitutions or injected state changes.
- **Actual large-text scrolling:** the same standard gesture scrolled the 42mm Skillbook to reveal Farming/Looting and the large Resources screen to expose the full Run averages action. See `07-42-large-scrolled.png` and `04-42-large-scrolled.png` beside their initial large-text captures.
- Real foreground recordings on both sizes show the saved character's authored running sequence and Carl's finite authored arrival, then his held final pose. Frames1.5s and2s visibly differ in the hero pose while Carl rests. The native Always On checkbox changed0→1, produced the dimmed static layout, and was restored to0. The background Health recording uses static last-phone values and a static portrait.

## Native videos

`video-provenance.json` records source dimensions, native frame counts/durations, raw SHA256 hashes, original paths and presentation transforms. Every original is retained under `clips/raw`.

Foreground and Always On transition clips are unmodified native recordings. The goal clip is a codec-copy excerpt of its first 18s; its original 77.5s recording remains available. Simulator video is variable-frame-rate: the dimmed observation emitted only one native frame during a requested 4s recording; background Health emitted startup frames and no later changes during the requested 6s observation. Their viewing copies hold the last recorded frame for the observation interval. No motion interpolation or invented UI frames were added. These viewing holds are distinct from unmodified camera output.

## Limits

Ordinary coordinate taps and simulated Crown wheel input could not be verified: the desktop input tool returned `windowNotFoundAtPosition`, including on a fresh Simulator binding and a dedicated fullscreen space. Simulator accessibility/menu actions worked, including actual DoubleTap control activation and scrolling. Ordinary touch, Crown rotation, pin taps, chart selection, receipt navigation and rate-toggle taps therefore remain implemented/source-reviewed but are **not** claimed as native interaction passes. No further input scaffolding was added to simulate success.

Reduce Motion, inactive scene and reduced-luminance guards are present in the native actor components. The controlled phone-background fixture and native Always On state were observed; the OS Reduce Motion setting was not separately toggled. Physical hardware and real WatchConnectivity transport were not tested. Refresh is a local 450ms fixture interaction, and source clocks are deliberately frozen for repeatable presentation captures. Controlled history/scenario values are identified in `FixtureProvenance.json`; they are not real player records or estimator measurements.

No TestFlight upload, production version change, main-branch merge, Android/Steam operation or player-save mutation was performed. Goals and pins are lab-process state only. The primary checkout, shipped 188 artifacts and prior isolated release worktree were preserved.
