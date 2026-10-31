# Background allowance and native UI

The managed allowance remains one hour. The former 20,000-task/kill ceiling is removed from the estimator and native timeline decoder. The producer sends `backgroundLimitSeconds` from the existing policy constant; no save schema or version changes are required.

Settlement prepares weighted task/enemy choices once per projected step, rather than rebuilding them for every event. Eligibility is refreshed each step, at most two seconds apart; completion-tier or combat eligibility changes within a step take effect on the next step. Every completion still resolves its own loot, bonuses, XP and typed statistics. Existing inventory batching coalesces inventory-change notifications without suppressing resource-added events or tier rolls.

Managed verification on Unity 6000.6.0f1: **36 EditMode cases and six isolated PlayMode continuation cases passed**. The new high-rate allowance case failed before the repair: an eight-hour absence at 100 tasks and 50 kills per second credited only 133.33 seconds instead of 3600. Its final lifecycle case credits 360,000 tasks and 180,000 kills, persists the full hour and counts, grants typed resources, and rejects duplicate foreground payouts. It took **12.276 seconds on this desktop**, including world restoration. This is a stress fixture, not mobile performance or real-game pace validation. Phone profiling remains necessary.

The first large settlement did complete but failed the Editor screenshot-session watchdog. A headless probe also logged an Editor localization exception and exceeded its 300-second timeout; it is not passing evidence. The fixture's unrestricted skill asset lookup was replaced by a scoped authored-asset lookup. The subsequent optimized D3D11 run and the full six-case suite completed successfully. [Managed receipts](managed-results/) retain the intended pre-fix failure and final passing runs. No new arbitrary event or reward ceiling replaces the old one.

The native wire now accepts optional `backgroundLimitSeconds` as presentation metadata and carries it through the real Live Activity bridge into optional ContentState. Both phone and Watch use one shared formatter for the transmitted allowance. Missing or malformed optional values are omitted without dropping valid progress. The maximum is never inferred from a trajectory or forecast end, which may be shorter than the background policy.

Since you left shows elapsed time and the allowance together. The phone elapsed header adds the maximum alongside its native timer, falling back to a second line when width is constrained. Supplemental small-family source has a compact version. Compact Dynamic Island remains non-interactive; its expanded region retains the existing Refresh button. No new decorative animation, background timer, server or scheduling path was added.

The Expedition dial spends available height beyond the existing 42mm layout on a larger ring and a lower information quartet. Geometry determines the space; no device-name check is used. The 42mm ring remains 100 points. The corrected inset stroke and existing Refresh targets remain intact. Native [42mm](captures/watch-42-dial.png) and [46mm](captures/watch-46-dial.png) renders show the resulting layout. Away, legacy and 405k-gain cases are captured at both sizes.

## Focused contracts

The new native boundary executable initially failed against the incoming decoder because a valid 540k/hour trajectory was rejected by its 20k counter ceiling. After removal of those ceilings, 10 focused checks pass: the high-rate payload samples/presents 405k task/kill/resource gains at 45 minutes; a one-hour allowance survives a five-minute estimate; legacy/invalid values stay absent; and a different transmitted value is retained. Finite, nonnegative, integral, monotonic and machine-representable count validation remains. This is wire/presentation coverage, not a replay of the managed estimator. See [native-contracts.txt](native-contracts.txt).

The test-audit gate is at the native wire boundary: existing managed tests cannot catch native rejection or accidental metadata substitution. The pre-fix failure is genuine decode rejection, not a compilation error. No production testing seam or transport mock was added. Phone/Watch native builds and controlled system UI evidence are recorded separately; the fixture adapter is not live gameplay accuracy evidence.

## Checkpoint scope

The current Watch Preview and isolated production widget/bridge host compile. Watch screenshots use the current production views with the separate fixture adapter; its QA-only clock offset supports the high-rate case. Controlled fixtures transmit 3600 seconds, while the away estimate deliberately ends after 300 seconds, making policy/trajectory separation visible.

The `Harness` reuses the existing isolated native host structure with a new unique bundle `com.blindsidedgames.echoes.allowanceqa`, the real production widget/intent/bridge/decoder, and its own cache. Actual phone captures and final build/compatibility receipts are complete; see [PROOF.md](PROOF.md) for observed results, preserved state and input limits. No full Unity export, version/build change, main merge or release was performed. The root thread owns managed policy, tests and the new gallery.
