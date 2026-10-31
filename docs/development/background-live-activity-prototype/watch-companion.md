# Read-only Watch companion prototype

The prototype adds a normal installed watchOS app with Current run, Skills and Recent runs screens. It uses the same pixel artwork and forecast view as the existing Live Activity. It is enabled only by the existing `EOV_BACKGROUND_PROTOTYPE` build lane and is not a store release.

The phone owns skills, history, saves and rewards. The Watch displays a snapshot and can request Refresh. It never runs an independent simulation, awards progress, changes settings, starts a run or creates a save checkpoint. Empty data remains empty. The Demo badge comes from the game's explicit isolated demo launch flag.

## Transport and cache

`EOVWatchCompanion_Publish` sends the version 1 JSON captured by the managed game. The native phone service validates the whole payload, persists it atomically and sends the latest value through Watch Connectivity application context. Activation is independent of Live Activity authorization and begins only with a valid publication. Payloads over 32 KiB, invalid versions, missing required fields, invalid enums, negative/non-finite counters, duplicate skill/run identities and oversized arrays/strings are rejected as a whole. The Watch retains its last valid snapshot.

Refresh uses reachable `sendMessage` with command `requestSnapshot`. The native phone service replies with its cached bytes marked `cached=true` and queues a flag consumed on the next actual Unity frame. Only managed publication creates a new timestamp. An immediate cached reply never represents newly simulated or awarded progress. When the phone cannot be reached, the app keeps its saved data and reports that state.

Both caches retain the original UTC Unix `generatedAt`. On Watch relaunch, already-received context is treated as cached. The UI shows connection/cache status and the original date/time. Current-run values are snapshot values; the forecast is explicitly an estimate. Its ring uses system date-driven progress, expires with one deadline task, and rechecks expiry when the app becomes active. It does not continuously run the game on watchOS.

## Generated native targets

The prototype Unity postprocess copies shared snapshot decoding and the phone session into UnityFramework, then creates a single-target watchOS SwiftUI application named `EchoesWatchCompanion`. Its bundle identifier is the phone identifier plus `.watchkitapp`. `WKApplication` and `WKCompanionAppBundleIdentifier` identify a dependent companion. The app is embedded before Unity's symbol postprocessing to avoid a packaging dependency cycle. Unity's public copy-phase API finds an existing matching phase when postprocessing repeats.

Watch deployment target is watchOS 11; the existing Live Activity's iOS 16.2 fallback remains separate. Simulator builds use a Watch simulator SDK on the Watch target and an iPhone simulator SDK on the phone targets. Do not globally override the entire mixed-platform project with `-sdk iphonesimulator`.

See `watch-companion-native-evidence.json` for the final source revision, build and device observations. Direct rendering or cache import must be distinguished there from actual Watch Connectivity delivery.

## Observed prototype results

The full Unity phone app delivered real snapshots over the paired simulators. Phone and Watch cache bytes matched exactly, including six real skills and ten real saved runs. After the phone simulator shut down and the Watch process relaunched, the Watch retained the same original timestamp and cache bytes. Native publication accepted actual managed JSON and rejected malformed, unsupported-version and oversized payloads without replacing that cache.

The native Refresh boundary was exercised with a disposable same-bundle test host invoking the existing store method once after activation. The phone returned a cached acknowledgement and the next real Unity frame published a new snapshot 37 ms after the request was queued. The test host was then replaced with the exact production Watch app. This proves the request/consume/publication path; a manual Refresh tap was not verified.

Simulator coordinate input failed even on the Simulator's own Home toolbar, while its accessibility action worked. Screen navigation could not be verified through that input path. The three direct screen captures and 30-second clip use unchanged production store/view code in a disposable standalone app, importing the exact snapshot previously received over real WC. Entry-point differences and hashes are recorded in `watch-companion-harness-provenance.json`; these captures are not represented as navigation proof.

The simulator also reported Watch Connectivity reachability after the phone simulator shut down, despite a disconnected pair and the OS red phone icon. Cached timestamps remained correct. Hardware connection behaviour, Watch app icon artwork, manual navigation and distribution remain outside the verified result.
