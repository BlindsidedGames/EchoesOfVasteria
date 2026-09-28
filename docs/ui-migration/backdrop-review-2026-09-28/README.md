# Window backdrop and Dad-o-cado review — 28 September 2026

Original references: [Skills](../baseline-2026-09-27/captures/101-viewport-1280-OpenSkills.png) and [Dad-o-cado](../baseline-2026-09-27/captures/96-dad-joke.png).

- Replaced tiled wood with 60% black, full-viewport dimming above the HUD and below town menus. The backdrop blocks underlying HUD input. Controls retain safe-area constraints.
- Restored Dad-o-cado's original town anchor and a content-sized, sliced speech bubble above-left. The bubble tail points at him; no modal or dimmer. Existing joke cycling remains. Right-click/Escape, opening a menu or starting a run hides the bubble.
- Run-summary dimming now extends outside the HUD safe area too.

Reviewed actual Editor captures for Skills, Forge, Cauldron, town and Dad-o-cado at 1280x720. The files named `1280-skills-safearea.png` and `1280-run-summary.png` deliberately capture a 2340x1080 viewport with 16:9 safe-area controls. Both backdrop bounds equal the full 936x432 panel bounds. See [checks](checks-1280.txt) and [final checks](checks-final.txt). The final speech-bubble reference is [here](1280-dad-long-final.png).

This is a backdrop/conversation correction, not another claim of full-game visual acceptance. Device notch/touch behavior was not physically tested. The new button retains the shared dark styling; its placement matches the original town button.

Validation used a disposable Library save directory and separate Editor product identity; Steam, UGS and feedback networking were disabled. Seven temporary guarded files were restored byte-for-byte (restoration.json), and normal product identity restored. Test harnesses and private save data stay in ignored Library. CLI requests during assembly reload timed out three times (including the final guard-restoration reload); retries succeeded after startup. A harness called before GameManager initialization was retried after readiness. These were tool/startup failures, not UI runtime exceptions.


## Dad-o-cado dismissal follow-up

The bubble now has a three-second unscaled countdown along its bottom edge, automatic expiry, and click/tap dismissal. Choosing another joke restarts the countdown. The timer runs independently of paused or accelerated gameplay. Checks are recorded in `checks-countdown.txt`; the visible progress reference is `1280-countdown-middle.png`. A dispatched UI Toolkit ClickEvent verifies the callback; physical touchscreen input was not tested.

Blur feasibility: Unity 6000.6 supports native `backdrop-filter: blur(...)` for screen-space URP panels, including camera content behind them ([Unity documentation](https://docs.unity3d.com/6000.6/Documentation/Manual/ui-systems/backdrop-filter.html)). The project uses URP quality overrides. Blur has not been enabled or performance-tested in this change.

The initial expiry assertion sampled exactly on the deadline before the next runtime/layout update; checking on a subsequent frame passed. Countdown, expiry, early dismissal and timer restart all passed with gameplay paused. The seven temporary isolation guards were restored byte-for-byte again (`restoration-countdown.json`).


## Enabled backdrop blur

Added a native 3-reference-unit backdrop blur to town-window dimming. The run HUD renders below it; the open window renders above it. Navigation buttons and run progress use matching element blur and 40% opacity because their document must keep the close button and window-specific actions above the backdrop. Existing navigation callbacks are retained. Filter state changes only when a window opens/closes, not on every refresh.

Reviewed Cauldron and Skills with the scene and normal navigation blurred, and verified the close button stays sharp and works. Verified full-viewport coverage at 2340x1080 with constrained safe area, and normal navigation restored after close. See `checks-blur.txt`, `checks-run-blur.txt`, `1280-blur-cauldron.png`, and `1280-run-blur-skills.png`. This is visual/functional validation in Editor; mobile GPU cost has not been profiled.


### Final top-bar behavior (user correction)

Top navigation is exempt from blur and dimming, including the run-progress display. It stays above the full-screen backdrop, enabling direct screen switching. Only the scene/lower HUD are blurred. This supersedes the navigation-blur behavior and captures above. Final reference: `1280-sharp-topbar-cauldron.png`; `checks-topbar.txt` exercises direct Cauldron-to-Stats switching through the real navigation button callback.
