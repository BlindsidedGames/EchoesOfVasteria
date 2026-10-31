# Background outings and Live Activities prototype

The approved fallback is main commit `80d98debce913eae3dc9cad1c3d78d037f8412e4`.
Experimental work is on `Matt/background-live-activity-prototype`; it is not a store release.

## Run it

The estimator is off in normal builds. Enable `EOV_BACKGROUND_PROTOTYPE` for a prototype build, or use the explicit `-eov-background-prototype` launch argument for a desktop evaluation. Native ActivityKit integration additionally requires the prototype iOS build define and its editor postprocessor.

On iOS the native host reads this explicit launch argument because IL2CPP does not expose simulator argv through `System.Environment`.

The `-eov-background-demo` argument creates a synthetic River demonstration only in an app whose bundle identifier ends in `.prototype` (or the existing protected Editor screenshot session). It supplies ten fictional calibration samples, starts at 30 metres and gives a 200-metre reap cap. This demonstrates integration; it is not evidence of estimation accuracy. Prototype builds suppress authenticated online-service bootstrap.

## Ownership

- `BackgroundRunEstimator` calculates distance, work, approximate health, deaths, reaps and restart time without generating a scene or changing inventory.
- `BackgroundRunContent` reads authored map/category/task/enemy/drop rules and applies projected work through existing inventory, skill, typed-kill and statistics owners. Drop slots and propagation bonuses use the existing resolver. Tier rolls still occur per resource award.
- `GameplayStatTracker` retains at most ten real calibration samples per map. Estimated or abandoned runs do not train the model; estimated history is labelled.
- `GameManager.Background` coordinates cold and warm resume, rebuilds the world at the projected distance, and supplies the trajectory included in the companion snapshot.
- `Oracle.Background` commits rewards, active run and timestamp together. A rejected or partial publication blocks further saves and requires checkpoint recovery; it cannot silently apply the payment again.

Mobile backgrounding freezes foreground work until settlement and regeneration finish. Desktop loss of focus continues live and earns no second estimated payment. Reopening a stopped desktop session uses the checkpoint. An undiscovered map with no evidence receives no invented pace. Required NPC/tutorial interactions and queued town returns stop the projection.

## Live Activity

The Swift WidgetKit extension and ActivityKit bridge display a current snapshot on the Lock Screen and Dynamic Island. While the game is awake, automatic local publication shows actual run distance, effective cap and work labelled "this run". Background snapshots use the existing Watch trajectory sampler and show estimated distance/current effective cap, completed tasks, kills, resources and reaps since departure. The elapsed clock uses the immutable departure origin and continues through Refresh taps without game execution. The labelled gold distance bar stays fixed between publications. There is no future-endpoint arrow or forecast countdown.

The compact dark-teal card uses the saved character's appearance, with the original pixel Hero for legacy saves, alongside the game's pickaxe, sword and Carl artwork. Its resource icon is the exact `Chest_Anim_0` sprite used by the in-game run summary, cropped without resampling; provenance is in `Assets/Editor/iOSLiveActivity/Art/SourceSprites.json`. Refresh is an accessible icon control at the top right. The small supplemental family retains a compact layout.

The main card and expanded Dynamic Island share one evenly spaced row of pickaxe/tasks, sword/kills, chest/resources and Carl/reaps. The four values use matching text and icon sizes, with descriptive metric names retained for accessibility. The [four-metric visual revision](four-metrics-20261031/README.md) records the actual simulator Lock Screen check.

Optional `portraitLayers` carry up to eight ordered bundled idle asset names. The managed character catalog and equipment appearance resolver own outfit selection, visibility and ordering. The iOS postprocessor exports available 64-pixel idle frames as 2x assets; native code composites them at the same zoom as the game HUD. Invalid or missing portrait data falls back without discarding progress. Encoded attributes and content state are checked together against a 3,900-byte budget, leaving room below ActivityKit's 4 KB limit.

Manual Refresh changes a presentation revision for a short two-point portrait bob between nearby resting poses. A positive reap gain within the same departure changes a separate revision for a small Carl sway. These are brief native content-update animations, with no timer or repeating work, and are disabled for reduced luminance and Reduce Motion. The [saved-character simulator receipt](character-portrait-20261031/README.md) records matching saved hair/clothing and equipped helmet, and measured portrait movement; positive-reap Carl movement was not observed in that pass.

Trajectory rows now optionally append cumulative task and kill gains to the existing six columns. The native sampler reads both six- and eight-column caches; older caches show unavailable work counts rather than invented zeros. Counts interpolate within existing movement segments and remain unchanged during restart pauses. No second estimator or new gameplay owner is introduced. The standalone Watch presentation is unchanged apart from compatible decoding. The native wire checks can be run with `swiftc Assets/Editor/iOSLiveActivity/Companion/EOVWatchSnapshot.swift Tests/Native/LiveActivityTimeline/main.swift -o /tmp/eov-timeline-checks && /tmp/eov-timeline-checks`.

On iOS 17 and later, **Refresh** runs a `LiveActivityIntent` in the native app process, samples the cached trajectory at the current time, and updates the existing activity. It does not request foreground presentation, invoke Unity, save, settle progress, or award rewards. Actual settlement remains exclusively in the managed resume path. iOS 26 explicitly requests background intent mode; iOS 17–25 use `openAppWhenRun = false`. iOS 16.2 displays an instruction to open Echoes instead. These availability gates compile against the simulator SDK; physical-device and older-runtime behavior require separate validation.

Each forecast has a fresh source ID. A button must match the current activity ID, persisted valid-source marker and cached run/map/trajectory. End and opt-out invalidate that marker immediately. Publication, Refresh and End share a queue so stale taps cannot start an activity or overwrite a later retirement. Expired trajectories are not extrapolated. Cached data and native state are presentation inputs, never authoritative rewards.

The generated main executable contains the intent metadata, activity owner and snapshot reader; UnityFramework retains its Objective-C transport and Watch service. Full Unity initialization and normal foreground settlement remain owned by Unity's lifecycle. The button's native code does not provide a periodic suspended refresh mechanism. There is no server, APNs dependency, keepalive or opportunistic background scheduler.

Prototype iOS resume waits until UIKit reports actual foreground activity before entering managed settlement. This handles the simulator's queued resume notification during locking. A foreground bounce shorter than two seconds restores the paused run without rebuilding its world.

The artwork comes from the game. There is no explanatory footer. Apple permits short animation on content updates, not an indefinitely looping sprite while suspended; the icons remain still between updates.

## Deliberate prototype limits

- A settlement covers at most one hour and 20,000 work events. These are evaluation bounds, not an approved production offline policy.
- Pace and combat exposure are estimates. Health prediction uses recent damage and authored enemy pressure; survival beyond recorded reach is unvalidated.
- Old history lacks active-time and Echo-performer measurements. New real runs record those small counters. Changing equipment, preferences or balance can make older pace less representative.
- Active saved reward/reap-distance buffs expire, and the temporary shop boost consumes its budget. Automatic buff recasting, combat-Echo positioning, gathering resonance and Windfall cycles are not yet reproduced. They need accuracy work before production payouts.
- Resource/XP levels update through existing owners during settlement, but the precomputed distance/health forecast does not recalculate pace for every new milestone. The widget resource total is an approximation; the settlement resolves actual eligible drop events.
- Fields have their existing foreground-only growth policy. This experiment does not add offline farm automation.

## Validation

Windows Unity 6000.6.0f1:

- Nine estimator cases plus four retained-history cases passed.
- Four continuation PlayMode tests passed, including actual isolated cold settlement, warm background/foreground settlement, save persistence, repeated foreground notifications without duplicate rewards, and world regeneration at the projected distance.
- The lifecycle case first exposed an empty hero-stat snapshot on cold load and an already-loading guard preventing warm regeneration; both were repaired and the same case passed.
- 379 protected local save-file hashes remained unchanged.

Mac simulator evidence and exact native build inputs are recorded alongside the native implementation. Native harness checks and the full Unity app checks must be reported separately. Simulator success does not establish physical-device battery behaviour or forecast accuracy.

The [2026-10-31 Unity integration receipt](refresh-integration-20261031/README.md) records actual Lock Screen Refresh taps, unchanged save/cache/settlement hashes during those taps, one subsequent managed resume settlement, and the final numeric elapsed-clock preview. That local simulator evidence was captured at version 1.5.9 (187). The [updated internal TestFlight build 188 receipt](testflight-188-device-2026-10-31.md) records the subsequent device build and Apple upload; tester availability is verified separately.
