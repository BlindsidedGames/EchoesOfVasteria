# Manual Live Activity Refresh — 31 October 2026

**PASS:** two real button taps updated synthetic distance, resources and reaps in the system-hosted Lock Screen activity. The native host woke from normal OS suspension, performed the intent in the background and returned to suspension. The Lock Screen remained visible. This verifies the native mechanism; production Unity integration is untested.

| Capture | Distance / cap | Resources | Reaps | Refresh | Sample time shown |
| --- | --- | --- | --- | --- | --- |
| [Before](liveintent/before.png) | 150 / 200 | +0 | 0 | #0 | 6:57:25 pm |
| [First tap](liveintent/after-first.png) | 150 / 200 | +40 | 2 | #1 | 6:57:45 pm |
| [Later tap after resets](liveintent/after-reset.png) | 25 / 200 | +110 | 6 | #2 | 6:58:21 pm |

Times above are the simulator's displayed sample times. The later snapshot shows a lower segment distance and higher resource/reap totals, so the result is more than a changed refresh counter. [Short clip of both taps](liveintent/refresh-taps.mp4): two excerpts concatenated from raw video seconds 14–20 and 49–56. The time jump is an edit; it is not a continuous 13-second interval. There are no number overlays or speed changes.

## Execution evidence

Successful action: `RefreshSnapshotIntent: LiveActivityIntent`, compiled in both the disposable native host and widget, with `supportedModes = .background` and `authenticationPolicy = .alwaysAllowed`. A regular widget-only `AppIntent` was also measured as a control.

- Activity: `FF6F5437-AE94-4D2E-9BA7-248A6138DAA7`; immutable origin Unix `1793433445.154296`.
- Success executor: bundle `com.blindsidedgames.timeformatlab`, PID **43081**. It is the native host process, not the widget extension.
- Host reported scene phase `background` at Unix `1793433405.564093`. No later `active` scene transition occurred in the measured tap window.
- RunningBoard reported `running-suspended` before the first tap, then `running-active` for intent execution, and `running-suspended` afterward. It repeated this sequence for the second tap. Its CPU role `UserInteractive` during the intent is distinct from visible foreground app state.
- `UIApplication.applicationState` was **2 (background)** immediately before and after each successful action. The actual screenshots and clip retain the Lock Screen UI.
- First sample: Unix `1793433465.934362`, elapsed **20.780066 s**, quantized to 20 s. Second: Unix `1793433501.058375`, elapsed **55.904079 s**, quantized to 55 s. Each action awaited `Activity.update` before returning.
- No debugger, SIGSTOP, app timer, server, push or reward/save mutation was used. These were real system-delivered button actions after normal OS suspension, not direct calls to `perform()`.

[Intent and lifecycle receipts](liveintent/events.jsonl), [RunningBoard excerpts](liveintent/host-process-state-excerpts.txt), [process samples](liveintent/process-samples.json), and [structured outcome](results.json) preserve the measurements. Screenshots were requested after the updates, so the separately rendered elapsed timer can be several seconds ahead of the frozen sample time. Counters remain a snapshot until another tap.

The log also contains three additional intent invocations after the measured screenshot/video window. Their source was not attributed by this capture; they are retained separately in `results.json` and excluded from the two-tap screenshot assertions.

## Widget-extension control

The regular `AppIntent` ran in bundle `com.blindsidedgames.timeformatlab.widget`, PID **40301**, after the actual button tap. `Activity<RefreshLabAttributes>.activities` returned an empty list. The [before](extension/before.png) and [after](extension/after-first.png) cards remained 150/200, +0 resources, reaps 0 and refresh #0. See [control receipt](extension/events.jsonl).

This demonstrates the failure of that specific extension-side activity query. It is not a claim that every extension architecture is impossible. The successful documented route here uses `LiveActivityIntent` in the app process without presenting the app.

## Scope, availability and reproduction

Standalone harness sources are in [Harness](Harness/project.yml); the exact extension-control sources are in [ExtensionControlHarness](ExtensionControlHarness/project.yml). Generate with XcodeGen and build the `TimeFormatLab` scheme with simulator signing disabled. Install only bundle `com.blindsidedgames.timeformatlab` on an owned simulator. The host creates one synthetic activity when explicitly launched into the foreground. Lock and wake the display, allow normal suspension, then tap Refresh. It samples the immutable activity origin and synthetic trajectory at the current time and publishes the resulting content state.

The fixture starts at 150/200 with pace 25/s, reaches its first cap at +2 s, restarts until +4 s, then repeats an 8-second moving segment and 2-second restart. Resources are twice whole elapsed seconds. Reaps are synthetic cycle counts. This is an elapsed-time UI fixture, not the game's authoritative progression/reward calculation. `results.json` keeps visually observed fields, actual intent receipts and independently computed model expectations separate.

Environment: iPhone 17 Pro simulator; runtime name iOS 26.4, reported version **26.4.1**, build **23E254a**; Xcode **26.4.1 (17E202)**, simulator SDK 26.4, harness minimum 26.0. The local AppIntents interface exposes `supportedModes` from iOS 26.0 and has no `allowedExecutionTargets`; Apple's current DocC metadata introduces the latter at **iOS 27.0**. See [availability receipt](api-availability.json). No unavailable target-selection API was used.

Apple's [interactivity guide](https://developer.apple.com/documentation/widgetkit/adding-interactivity-to-widgets-and-live-activities) directs Live Activity actions to `LiveActivityIntent` and describes execution in the app process. Its [runtime guide](https://developer.apple.com/documentation/appintents/configuring-the-runtime-behavior-of-your-app-intents) describes mode selection, including conflicting foreground-default wording. The explicit mode and measured application state are therefore recorded here rather than inferring visible behavior from protocol conformance.

This simulator Lock Screen UI does **not** prove invocation on authenticated/locked physical hardware, Face ID behavior, physical Always-On cadence, Dynamic Island acceptance, terminated-host behavior, or Unity engine startup. No game/Unity build, production integration, version bump, upload or deployment occurred. The older River demo card above the lab is unrelated and was not modified.

The previous `time-format-lab-20261031` evidence and hashes remain unchanged. This directory is the only change in its evidence commit, based on canonical `659ba5e9d05fe60c529d1d7b4e7b3253c278edb4`. [Provenance](provenance.json) and source/artifact hash manifests identify the exact harness and media. Raw broad logs, full videos and build products remain ignored local artifacts.

[Cleanup](cleanup.json) stopped only owned capture processes and the disposable harness. [Preservation](preservation.json) confirms the primary checkout matches its original HEAD/diff and progression files retain their original hashes. Only the already changed Unity Editor Analytics cache differs from the older pre-Unity-build baseline.
