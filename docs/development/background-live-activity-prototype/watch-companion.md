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
