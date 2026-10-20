# Active late-game profiling fixtures

These files are diagnostic helpers outside `Assets`. They are not included in the
shipping game. Copy them only into an isolated profiling checkout. Never point
SaveManager at the real player root, and never commit generated saves or recordings.

`LateGameStressFixture.Build` reads a **copied** Save1 via SaveManager and writes two
new, explicitly synthetic Save1 roots under a new Artifacts destination. Inputs:
`EOV_STRESS_SOURCE` and `EOV_STRESS_DESTINATION`. It does not edit source saves.

Both states have 1e12 finite units per resource and stew, skill/mastery/Eva levels
1000, all authored quest gates, highest tier-7 Vastium core, and legitimate maximum
quality tier-7/five-affix equipped items. All finite authored skill unlocks are below
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
windows. A brief idle control precedes them, with recovery afterward. Inventories
occur outside the sustained timing windows. Per-window real counter deltas must
be positive. EOV_STRESS_MODE selects eternal/ordinary. EOV_STRESS_PHASE_SECONDS is
only a smoke-test duration override; 3-second smoke windows are not sustained data.

Windows invokes the helper from the isolated StandaloneMemoryProbe, preallocates
frame storage, uses a separate company/product identity and rootPathOverride, and
samples process counters externally. Native integration must retain its own
container guard, kernel footprint sampler and clean frame measurement windows.
No physical iPhone or shipping performance claim follows from Simulator results.
