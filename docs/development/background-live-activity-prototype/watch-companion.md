# Read-only Watch companion prototype

The prototype adds a normal installed watchOS app with Current run, Skills and Recent runs screens. Current run shows actual recorded run values while the phone is foregrounded, then estimated gains and distance while the phone explicitly reports that it is backgrounded. It uses a distance-to-reap dial, a Watch-only timeline from the phone estimator and the existing pixel artwork. It is enabled only by the existing `EOV_BACKGROUND_PROTOTYPE` build lane and is not a store release.

The phone owns skills, history, saves and rewards. The Watch displays a snapshot and can request Refresh. It never runs an independent simulation, awards progress, changes settings, starts a run or creates a save checkpoint. Empty data remains empty. The Demo badge comes from the game's explicit isolated demo launch flag.

## Transport and cache

`EOVWatchCompanion_Publish` sends the version 1 JSON captured by the managed game. The native phone service validates the whole payload, persists it atomically and sends the latest value through Watch Connectivity application context. Activation is independent of Live Activity authorization and begins only with a valid publication. Payloads over 32 KiB, invalid versions, missing required fields, invalid enums, negative/non-finite counters, duplicate skill/run identities and oversized arrays/strings are rejected as a whole. The Watch retains its last valid snapshot.

Refresh uses reachable `sendMessage` with command `requestSnapshot`. The native phone service replies with its cached bytes marked `cached=true` and queues a flag consumed on the next actual Unity frame. Only managed publication creates a new timestamp. An immediate cached reply never represents newly simulated or awarded progress. When the phone cannot be reached, the app keeps its saved data and reports that state.

Both caches retain the original UTC Unix `generatedAt`. On Watch relaunch, already-received context is treated as cached. The UI shows connection/cache status and the original date/time. Current-run values are snapshot values; the forecast is explicitly an estimate. Its ring samples estimated current distance and the effective reap limit from bounded estimator keyframes. A UI-only periodic view updates the dial and estimated gains; it freezes at the timeline horizon and marks the estimate ended. It does not continuously run the game on watchOS.

## Generated native targets

The prototype Unity postprocess copies shared snapshot decoding and the phone session into UnityFramework, then creates a single-target watchOS SwiftUI application named `EchoesWatchCompanion`. Its bundle identifier is the phone identifier plus `.watchkitapp`. `WKApplication` and `WKCompanionAppBundleIdentifier` identify a dependent companion. The app is embedded before Unity's symbol postprocessing to avoid a packaging dependency cycle. Unity's public copy-phase API finds an existing matching phase when postprocessing repeats.

Watch deployment target is watchOS 11; the existing Live Activity's iOS 16.2 fallback remains separate. Simulator builds use a Watch simulator SDK on the Watch target and an iPhone simulator SDK on the phone targets. Do not globally override the entire mixed-platform project with `-sdk iphonesimulator`.

See `watch-companion-native-evidence.json` for the final source revision, build and device observations. Direct rendering or cache import must be distinguished there from actual Watch Connectivity delivery.

## Observed prototype results

The full Unity phone app delivered real snapshots over the paired simulators. Phone and Watch cache bytes matched exactly, including six real skills and ten real saved runs. After the phone simulator shut down and the Watch process relaunched, the Watch retained the same original timestamp and cache bytes. Native publication accepted actual managed JSON and rejected malformed, unsupported-version and oversized payloads without replacing that cache.

The native Refresh boundary was exercised with a disposable same-bundle test host invoking the existing store method once after activation. The phone returned a cached acknowledgement and the next real Unity frame published a new snapshot 37 ms after the request was queued. The test host was then replaced with the exact production Watch app. This proves the request/consume/publication path; a manual Refresh tap was not verified.

Simulator coordinate input failed even on the Simulator's own Home toolbar, while its accessibility action worked. Screen navigation could not be verified through that input path. The three direct screen captures and screen clip use unchanged production store/view code in a disposable standalone app, importing the exact snapshot previously received over real WC. Entry-point differences and hashes are recorded in `watch-companion-harness-provenance.json`; these captures are not represented as navigation proof.

The simulator also reported Watch Connectivity reachability after the phone simulator shut down, despite a disconnected pair and the OS red phone icon. Cached timestamps remained correct. Hardware connection behaviour, Watch app icon artwork, manual navigation and distribution remain outside the verified result.

## Earlier full-screen radial revision

Current run now opens on a full-screen time-progress dial with Carl, the map, estimated distance, system countdown and reaps in the centre. The native Back control remains visible. Actual run counters, Refresh and the original cache timestamp sit below the first viewport. A dead or reaped run cannot show an active forecast; town, loading, missing-forecast and expired states use a gray ring.

`watch-companion-radial-v2-evidence.json` records 42 mm and 46 mm native renders using unchanged data received from the game, an actual received dead-run render, and a separate short-deadline timing fixture. In that fixture the same production view and process changed from the active countdown to gray “Forecast ended” without another snapshot or relaunch, closing the earlier foreground deadline verification gap. The fixture modifies only the forecast's two dates and is explicitly labelled in its screenshots and clip. These direct renders do not establish manual navigation or Crown scrolling.

## Distance and resource dial

When the optional `phoneInBackground` field is explicitly true, the current dial uses the distance at the sampled time divided by that run's effective reap limit. It resets when a predicted reap ends the run, stays at zero throughout the restart pause, and resumes at the estimator's actual next-run starting distance. Cap changes are retained at their observed times. There is no forecast countdown.

The large centre value is **Est. resources** gained since this forecast's origin. It starts at zero and uses the estimator's task/kill work totals with the same per-work constants used by the existing phone forecast. It does not include current-run resources or add the previous projection when a newer snapshot arrives. Gains interpolate linearly within each straight movement segment between its observed work-total endpoints; run endings, cap changes and restart pauses are explicit boundaries.

The Watch-only optional `timeline` object has version 1, map ID, UTC origin, horizon seconds, a distance-reaping flag, a truncated flag and at most 128 compact frames. Each frame is `[seconds, distance, effectiveCap, cumulativeEstimatedGain, reapsSinceOrigin, phase]`; phases are moving 0, restarting 1 and waiting for input 2. Duplicate timestamps represent discontinuities; the last frame at the sampled time wins. Positive-duration segments cannot change cap, reap count or phase. The horizon shortens honestly if the frame or 32 KiB snapshot budget is reached. ActivityKit's existing forecast JSON and behaviour remain separate.

Without a compatible timeline, the dial uses only published actual run distance, effective cap and resources, with an **Actual resources** label and no invented reap count. An explicit source flag disables distance-to-reap progress for Hollow and other maps without distance reaps. Older snapshots lacking that flag conservatively omit reap progress. Dead runs remain gray and show their actual resources; expired timelines retain their final estimated gain and show **Estimate ended** below the dial.

The distance-v3 verification is recorded separately in `watch-companion-distance-v3-evidence.json`: 13 focused managed tests, 21 native sampling checks, a rebuilt Unity/iPhone/Watch app, byte-identical paired delivery of a 54-frame real game-produced timeline, both Watch sizes, actual dead fallback, and a clearly labelled managed-estimator fixture clip through two reap resets and expiry. The 128-frame bound case includes six skills and ten history entries in 7,145 bytes. The isolated prototype apps are installed; no store/main changes were made.

## Foreground/background mode

Only `phoneInBackground == true` permits timeline estimates. Foreground `false`, missing/older fields and null fields use actual recorded run distance, effective cap and current-run resources, with the **Actual resources** label. The Watch never infers phone mode from connectivity or snapshot age. The background state comes from the existing game lifecycle after background handling, with an immediate publication at both transitions. Foreground resume immediately clears estimated mode, then a stable run snapshot follows the normal update path; the existing 15-second cadence remains.

The mode-v4 receipts are separate from the historical distance-v3 evidence. No lifetime reap total is introduced: actual mode shows no invented reap count, while background estimates retain reaps since that forecast began.
