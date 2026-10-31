# Historical reach is not a run boundary

The estimator could predict an early death immediately beyond the furthest calibration run. Its zero-damage fallback manufactured incoming damage when authored enemy pressure exceeded the pressure at that historical distance. That made short, undamaged history act like a soft distance limit even when the actual reap cap was much higher.

Removed that fallback. Recorded damage still scales with authored enemy pressure, regeneration still applies, and actual combat deaths still restart normally. Reaping remains tied to `RunDistanceRules.EffectiveCap` from the player's saved maximum and active modifiers. No save or native wire format changes are needed. Zero-damage history now remains zero-damage evidence; survival beyond recorded play remains an experimental estimate.

The Mines regression starts at 2,300 m with history ending at 2,400 m and an actual 38,000 m cap. Before the fix it predicted 15 deaths. After the fix its first run ending is a reap at exactly 38,000 m, the cap increases to 38,380 m, and the remaining 30 seconds advance 300 m into the next run. The input checkpoint remains unchanged.

Unity 6000.6.0f1 EditMode: [pre-fix regression failure](before.xml), [post-fix estimator suite, 14/14 passed](after.xml). The suite also covers real combat death, cap modifiers, successive reap increases, restart countdowns, NPC gates and presentation timeline resets. These are managed owner tests; no new phone build was uploaded for this fix. TestFlight 1.5.9 (188) predates it.
