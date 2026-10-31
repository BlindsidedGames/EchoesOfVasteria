# Native results and limits

Source checkpoint: `fbae88173eb3e00b6e95e2b766eb835466a1bfce`, followed by the final self-contained-decoder cleanup and proof commit. `validation.json` records the exact final native source hashes. The root thread owns the managed allowance producer, high-rate settlement measurement, combined review and gallery.

## Observed native UI

- Current production Expedition and Since you left views render at 42mm and 46mm. Both away views fit Elapsed, the timer, 1h max and all gains/distance. Legacy payloads omit the maximum. The controlled high-rate case displays 405k gains at 45 minutes without rejection or overflow.
- The 42mm dial keeps its 100-point diameter and existing quartet placement. The 46mm viewport yields 117.5 points from available height, with the quartet lower. The inset ring remains fully rounded. Root independently inspected and accepted both dial captures and the 46mm away capture from the source checkpoint.
- Actual system-hosted Lock Screen cards on the owned iPhone 17 Pro simulator and a new isolated iPhone 13 mini simulator show Elapsed and 1h max alongside the existing Refresh button. The wider device's older ended Unity card remains above the new QA card; it was not replaced or modified.
- `phone-content-state.json` and `narrow-content-state.json` are read back from the real ActivityKit activity after the production bridge publishes it. Each contains backgroundLimitSeconds 3600 while forecastEnd minus backgroundOrigin is 900 seconds. This proves native propagation of the independent allowance, not a fresh Unity export or a live gameplay source.
- A second narrow-device run removes only the optional allowance input. Its real ContentState omits that key, and `phone-narrow-legacy.png` shows the current widget omitting the maximum. It does not substitute the 900-second trajectory duration.
- `phone-compact-island.png` shows the current system compact Island with the saved character and estimated distance. No compact Refresh button was added. The dial’s estimated time-focus caption also includes the transmitted maximum; legacy away-time and foreground run-time captions are preserved. That caption compiles but was not interactively selected because coordinate input is unavailable.

All captures are native Simulator screenshots without retouching. Watch rendering uses the existing isolated fixture adapter and its frozen presentation clock. Phone rendering uses the current production widget, intent, bridge, attributes and decoder in a unique native QA host; its one controlled trajectory is rebased consistently to real time in that bundle's own cache. These fixtures demonstrate presentation and transport boundaries, not estimator accuracy or earned inventory.

## Builds and checks

Current-source Watch Preview and actual Store targets both finish BUILD SUCCEEDED at the unchanged watchOS 11 minimum. The isolated native phone host and production widget finish BUILD SUCCEEDED with an explicit iOS 16.2 deployment target, matching the shipping widget minimum. Xcode 26.4.1 and simulator SDK 26.4 were used.

Ten focused native allowance/high-rate checks pass. The incoming decoder failed the controlled high-rate case at runtime after successful compilation; `native-before.txt` records the intended negative result. The new coverage owns wire acceptance and presentation policy. Existing compatibility checks also pass: 21 native wire/sampling cases and 11 Watch presentation cases. Their results are retained separately. No duplicate managed simulation or new production testing seam was introduced.

The changed editor packaging file typechecks against the installed Unity 6000.6 SDK and existing character runtime assembly. It copies the shared formatting file into the native export and includes it in phone/widget/Watch UI targets. The decoder remains a standalone Foundation file, so old native decoder/lab build commands do not acquire a helper dependency. A fresh full Unity iOS export was not run.

## Input and layout limits

Simulator native AX window/menu actions work after raising the window. Device→Lock once locks/dims and a second invocation wakes the Lock Screen; this yielded the actual system captures. New QA Live Activity permission prompts were accepted only for the disposable bundle.

A single attempt to hold the compact Island through CUA coordinate input was rejected before delivery: `windowNotFoundAtPosition((864.0, 199.0))`. No repeated coordinate-debugging loop or alternate input injection was used. Expanded Island rendering/Refresh input, supplemental small-family rendering, physical-device near-edge taps/unlock behavior, and true landscape card layout remain unverified. Their source compiles, and the elapsed header uses horizontal fitting with a vertical fallback, but compilation is not visual/input acceptance. The previous actual Watch Store unavailable-Refresh proof remains separate; it was not repeated or re-labelled as paired delivery.

No new server, background scheduling or automatic 15-second update mechanism was added. Timer rendering and the supplied maximum do not imply that suspended Unity runs every 15 seconds.

## Preserved state and cleanup

The QA phone bundle was uninstalled from both owned devices. The newly created narrow simulator was shut down; existing devices and other projects' simulators were preserved. Both phone orientations remained Portrait. The old release worktree and production 188 artifacts were read-only throughout. No main merge, version change, upload or deployment occurred.

The primary user checkout still has its original two asset changes, with binary-diff SHA256 `7d45e5d114159fefba437f0b4df201af1939c4ffc59e6c29725a468ee1c68ec2`. No player save or progression was modified. Temporary build products and staged catalogs remain ignored local artifacts. The 12 linked captures and source/artifact hashes in `manifest.json` and `validation.json` are sufficient for root review without treating limited native acceptance as a release result.
