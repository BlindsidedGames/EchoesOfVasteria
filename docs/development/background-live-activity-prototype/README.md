# Background outings and Live Activities prototype

The approved fallback is main commit `80d98debce913eae3dc9cad1c3d78d037f8412e4`.
Experimental work is on `background-live-activity-prototype`; it is not a store release.

## Run it

The estimator is off in normal builds. Enable `EOV_BACKGROUND_PROTOTYPE` for a prototype build, or use the explicit `-eov-background-prototype` launch argument for a desktop evaluation. Native ActivityKit integration additionally requires the prototype iOS build define and its editor postprocessor.

The `-eov-background-demo` argument creates a synthetic River demonstration only in an app whose bundle identifier ends in `.prototype` (or the existing protected Editor screenshot session). It supplies ten fictional calibration samples, starts at 30 metres and gives a 200-metre reap cap. This demonstrates integration; it is not evidence of estimation accuracy. Prototype builds suppress authenticated online-service bootstrap.

## Ownership

- `BackgroundRunEstimator` calculates distance, work, approximate health, deaths, reaps and restart time without generating a scene or changing inventory.
- `BackgroundRunContent` reads authored map/category/task/enemy/drop rules and applies projected work through existing inventory, skill, typed-kill and statistics owners. Drop slots and propagation bonuses use the existing resolver. Tier rolls still occur per resource award.
- `GameplayStatTracker` retains at most ten real calibration samples per map. Estimated or abandoned runs do not train the model; estimated history is labelled.
- `GameManager.Background` coordinates cold and warm resume, rebuilds the world at the projected distance, and publishes the same forecast to the native bridge.
- `Oracle.Background` commits rewards, active run and timestamp together. A rejected or partial publication blocks further saves and requires checkpoint recovery; it cannot silently apply the payment again.

Mobile backgrounding freezes foreground work until settlement and regeneration finish. Desktop loss of focus continues live and earns no second estimated payment. Reopening a stopped desktop session uses the checkpoint. An undiscovered map with no evidence receives no invented pace. Required NPC/tutorial interactions and queued town returns stop the projection.

## Live Activity

The Swift WidgetKit extension and ActivityKit bridge display estimated map progress on the Lock Screen and Dynamic Island. A foreground snapshot supplies a forecast window; system timer/progress views continue without game execution. There is no server or APNs dependency. Actual rewards are applied only by the game on resume.

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
