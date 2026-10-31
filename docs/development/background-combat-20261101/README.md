# Background combat estimate

Survival now comes from the player's current combat stats and the map's eligible enemies. Recorded damage no longer decides whether enemies can hurt the player. The one-hour allowance is retained and also supplies the Watch/Live Activity forecast horizon; frame and work limits can shorten that horizon.

## Calculation

The live combat controller and estimator share `Combat.ExpectedDps`: damage, attack rate, expected crits, enemy defence and the enemy-specific kill bonus. The content adapter reads the same central hero-stat calculation without temporary buffs, then applies saved ability expiry and automatic ability cycles. This read-only query neither replaces the live stat cache nor clears its pending changes.

For each eligible enemy, the estimate divides its level-scaled health by combined hero/Echo DPS, then estimates incoming damage over that exposure using its attack rate and the hero's defence. Combat-capable Echoes contribute attacks and an assumed equal share of target interception. Only the intercepted share transfers half damage to the shared health pool. Task-only Echoes do not grant this protection. Bombschrooms use their authored fuse and blast multiplier instead of ordinary attack DPS: a shroom killed before the fuse finishes contributes no blast.

The eligible enemies retain the map's spawn weights, level scaling, terrain validation and opening restrictions. Mean encounter damage is weighted across those entries. Encounter rate is the larger of recorded kills per second and authored enemies per metre multiplied by recorded distance per second. Completed kills alone omit enemies that are still attacking; this density floor avoids treating a struggling character's low kill count as safety.

The estimator integrates incoming damage and regeneration in steps of at most two seconds, ending the outing at zero health or the effective reap cap. Run resets restore health and reset the combat-ability clock; saved manual abilities do not carry into the next run. A forecast does not consume ability state or award inventory. Normal settlement still owns awards, typed kills, history and checkpoint publication.

## Evidence

- `before.xml`: the pre-fix owner-boundary regression ran three cases. Zero and high recorded damage failed; the middle case passed. Identical current threat was producing different deaths solely because history differed.
- `after.xml`: focused estimator, combat-formula and timeline contracts. Includes zero/low completed kills, level growth, crits, defence, Echo interception, the real authored Combat Echo's expiry, next-run clearing, regeneration, read-only stat-cache ownership and the one-hour allowance.
- `continuation.xml`: existing isolated PlayMode continuation tests, covering real cold/warm settlement, repeated foreground notifications, persisted rewards, rebuilt world and companion data. The settlement fixture now supplies a strong real weapon; zero recorded damage no longer makes its naked starting character immortal.
- `profile-replay-kill-rate-only.json` preserves the rejected first model: recorded kill rate alone missed all three early-game deaths in the two-minute observation window.
- `profile-replay.json` invokes the production combat formula with nine recorded early/mid/late QA stat and pace profiles. Mid/late characters survive their observed windows. The early first-death distances over the allowance are approximately 90–92 m, compared with observed 79–84 m: **10–16% optimistic**. It predicts deaths rather than assuming the reap cap is reachable, but does not reproduce exact combat timing.
- `profile-replay.cs.txt` preserves the temporary, read-only Unity replay command. It was removed from the Editor after generating the receipt. Original inputs are the existing local `Artifacts/BackgroundRunExperiment20261031` captures. This is an in-sample replay of recorded pace with current authored enemies, not a new foreground run or an out-of-sample accuracy measurement.

## Limits

Positioning, enemy overlap, projectile travel, individual crit rolls, dice rolls and milestone-spawned Echoes are not simulated. Equal Echo target sharing is an assumption. Averaged exposure smooths bursts and can predict death too late, especially for weak characters. Ability uptime is a forecast input; settlement does not replay every automatic cast's counters, Windfall or gathering-resonance cycle. Skill gains during settlement do not continually retrain movement and combat pace.

The same one-hour/20,000-work bounds, required-player-input gates and estimated-history isolation remain. This is experimental background progression, not proof of exact survival or a reason to grant uncapped offline rewards. No save schema, asset identity, store version or player save was changed. TestFlight 188 is unchanged.

Final verification: **35/35 EditMode cases and 5/5 isolated PlayMode continuation cases passed** on Unity 6000.6.0f1. The final EditMode repeat passed after the shared allowance constant and one-hour presentation horizon were in place. One intervening Editor startup exited before producing a report; the subsequent repeat completed. The temporary replay Editor script and its generated metadata were removed. No scene or authored balance asset was edited.
