# Saved character portrait and brief native updates

The actual Unity-generated simulator app and its Live Activity now use the same saved character appearance. The card retains the four-metric row, elapsed clock, distance bar and Refresh control. This is the local prototype at **1.5.9 (187)**, with no version change or upload.

| Saved outfit | Actual game | Actual Lock Screen |
| --- | --- | --- |
| Purple hair, blue shirt, black trousers; equipped helmet hidden | [Game](purple-hair-game.png) | [Card](purple-hair-lockscreen.png) |
| Same outfit with equipped gold helmet visible | [Game](gold-helmet-game.png) | [Card](gold-helmet-lockscreen.png) |

The purple hair and gold helmet both fit inside the existing portrait crop. The descriptor contains six ordered named layers from the real saved profile and equipment owner. These images were captured from the actual Unity app and native Lock Screen, without the demonstration launch argument. The snapshot reports `demo: false`; this remains an isolated QA save, including calibration retained from earlier prototype work.

## Observed animation

[Seven-second continuous recording of one helmet portrait Refresh](helmet-refresh-bob.mp4). The recording is trimmed at the ends, scaled to 720 pixels wide and encoded at 30 fps; no interior cuts, overlays or timing changes were applied.

The persistent portrait moves through intermediate positions rather than merely changing a revision number. A 60 fps extraction from the source recording measured the gold helmet's top edge at native pixel rows **1944 → 1942 → 1940 → 1938** over successive frames, a six-pixel movement (two points on this 3x display) over roughly 0.35 seconds. `helmet-motion-measurements.txt` records the image-region measurements. The first identity-only transition stayed stationary and was replaced by this data-driven bob between two adjacent resting poses. The earlier stationary-transition recording is excluded from this final evidence.

Both actual Refresh taps ran in PID 11712 with application state **2 before and after**, Reduce Motion **off**, the same activity/source and unchanged elapsed origin. The native host logged combined encoded attributes/state at **675 and 674 bytes**. The portrait revisions advanced from 1 to 2, giving opposite directions between the two nearby poses. See `native-receipt.txt`.

Carl's small horizontal/tilt transition is configured only when projected reaps increase within the same source and background departure. **Its positive-reap animation was not observed.** A controlled near-cap checkpoint reaped during cold restoration before the Lock Screen interval; after restarting through the real game UI, the measured background trajectory stopped before another reap. No reap counter was injected. Numeric counter changes and revision logs alone are not treated as motion proof.

## Build, fixture and scope

- Source `394415b6e` combines the native portrait implementation with the managed descriptor and asset exporter. Final native animation source is `9c86cb6d4`.
- Unity 6000.6.0f1 scripts-only export succeeded in 50.43 seconds with no errors. Of 198 catalog parts, 179 have valid idle frames and were exported; optional parts with no idle image are skipped. Idle tools intentionally have no image, so no artificial weapon pose is added.
- Native Xcode builds succeeded against simulator SDK 26.4. Actual rendering was checked on an iPhone 17 Pro simulator, iOS 26.4.1. `receipt.json` records native source/generated source and binary hashes.
- The existing continuation PlayMode suite passed **5/5**, including saved profile replacement, equipment visibility and no progress award. Native cached-wire checks passed **25/25**, including old cache fallback, ordered layers and malformed optional portraits without dropping progress. Two intermediate managed fixture failures were corrected: JsonUtility null-array normalization and an unauthored hair colour. The production appearance behavior was unchanged by those corrections.
- Only the owned simulator prototype fixture was edited through the normal SaveManager, while the app was stopped. Original owned data was backed up locally to `Artifacts/LiveActivityCharacter20261031/prototype-before-portrait.tar.gz`. The temporary guarded Editor utility was removed after use. The final helmet QA save is retained for the visible Mac preview; no production progression file was changed.
- The primary checkout retains its original HEAD and user diff. Original player-directory checks distinguish progression from generated test reports and Editor Analytics cache changes.
- This pass does not verify physical hardware, Face ID/locked-device authorization, real Always-On cadence, older runtimes, Dynamic Island or small-family rendering, or Watch presentation. Reduced-luminance and Reduce Motion suppression is implemented; the measured bob used the bright Lock Screen with Reduce Motion off.

Apple documents [content-update animation constraints](https://developer.apple.com/documentation/widgetkit/animating-data-updates-in-widgets-and-live-activities) and the [combined 4 KB ActivityKit payload limit](https://developer.apple.com/documentation/activitykit/displaying-live-data-with-live-activities). There is no server, App Group, runtime image transport or periodic background rendering in this implementation.
