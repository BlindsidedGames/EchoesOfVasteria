# Active late-game profiling fixtures

These files are diagnostic helpers outside `Assets`. They are not included in the
shipping game. Copy them only into an isolated profiling checkout. Never point
SaveManager at the real player root, and never commit generated saves or recordings.

`LateGameStressFixture.Build` reads a **copied** Save1 via SaveManager and writes two
new, explicitly synthetic Save1 roots under a new Artifacts destination. Inputs:
`EOV_STRESS_SOURCE` and `EOV_STRESS_DESTINATION`. It does not edit source saves.

Both states have 1e12 finite units per resource and stew, skill/mastery/Eva levels
1000, all authored quest gates, highest tier-7 Vastium core, and legitimate maximum
quality tier-7/five-affix equipped items. A normal modular character has helmet,
chest/trousers and boots visibility enabled. This configures the top-tier appearance;
town alone does not instantiate the player's world model. Rendering coverage must
be demonstrated separately by an actual map or equipment preview. All finite authored skill unlocks are below
level 1000 (current last manual tier is 400). Level-based infinite systems have no
absolute maximum: these are high-progression stress cases, not an invented cap.
Tasting Turmoil's highest passive and active bonuses are selected using the normal
milestone save records. No config, rarity odds, stop logic, sampling or throughput
is changed. Fast Craft Mode is off so full analytics remains enabled.

- `eternal`: resource collection counts 10000 and buff counts 3000 (authored maxima),
  seven Eternal Boon counts of 1e6, all finite collection tiers complete.
- `ordinary`: ordinary collections at 50% of the same maximum, Eternal counts zero;
  exercises ongoing card grants, collection tier/pool rebuilds, and normal transition.

`LateGameStressScenario.Run(output, stage, inventory, finish)` is a player-only
coroutine for an isolated observer (EOV_STANDALONE_PROFILE or EOV_MEMORY_PROFILE).
It validates loaded state and resources, uses the highest core and ordinary public
forge/tasting methods, and records per-second actual crafts, tastings, cards, stew,
resource consumption, pending telemetry, farm receipts and memory. Normal stops and
manual public restarts are logged; it never overrides upgrade/rarity stop outcomes.
Maximum quality gear prevents normal random rolls from being better while keeping
legitimate affix counts, slot guarantees and roll bounds.

Current authored throughput is 20 crafts/s and 100 tastes/s. The configured forge
cap is 1000/s, but there are currently no authored callers adding a craft-speed
bonus. The fixture does not inject a bonus to pretend 1000/s is attained.

Farm state has all six garden and six orchard beds, level 60, and ample seeds.
Normal paid, journalled Plant/Water/SetRepeat APIs create each batch. A documented
300-second initial growth age is applied with the normal pure TickFields command
as a synthetic starting condition; subsequent clocks, durations and automatic
harvest/replant logic are unchanged.

The default active run has five 120-second windows: hidden, Forge, Cauldron Tasting,
Cauldron Collection, and Fields. Both production systems continue throughout all
windows. A brief setup/settle band precedes them, with recovery afterward. The legacy
stage label `stress-idle-control` does not prove idleness: normal farm commands can
restart tasting. Exclude that band from idle comparisons and use real counters. Inventories
occur outside the sustained timing windows. Per-window real counter deltas must
be positive. EOV_STRESS_MODE selects eternal/ordinary. EOV_STRESS_PHASE_SECONDS is
only a smoke-test duration override; 3-second smoke windows are not sustained data.

Windows invokes the helper from the isolated StandaloneMemoryProbe, preallocates
frame storage, uses a separate company/product identity and rootPathOverride, and
samples process counters externally. Native integration must retain its own
container guard, kernel footprint sampler and clean frame measurement windows.
No physical iPhone or shipping performance claim follows from Simulator results.


## Map entry and generation checks in the Editor

`MapGenerationBaseline.Run(output)` runs via the CLI `run_script` command outside
Play Mode. It exercises three fixed seeds on each of the six authored map configs,
including segment clearing and regeneration. Compare hashes against the previous
runtime in an isolated checkout; the hash covers terrain, decoration tiles,
transforms and prefab sprite flips. It does not claim deterministic task/enemy
placement, which uses the game's shared Unity random stream across frames.

`MapEntryProbe` belongs only in an isolated profiling checkout. Set the company to
`Blindsided Profiling`, copy a fixture to `<output>/isolated-progress`, set
`EOV_EDITOR_MAP_PROBE=1`, `EOV_PLAYER_PROFILE=<output>` and `EOV_PROFILE_SOURCE=<label>`
in that Editor process, then enter Play from the normal Loading scene. It uses
normal map entry for Farmlands, restart, River, Woods, Mines, Halloween and restart.
The observer records scans, elapsed time, frame count and largest frame, checks
loading visibility, generated tasks where authored and resumed movement, then
exits Play. Use a fresh output/fixture copy per pass. Clear the environment variables
afterward. Never install this observer in the main project or use the player's save
root. Editor allocations include Editor overhead and are not device memory figures.
