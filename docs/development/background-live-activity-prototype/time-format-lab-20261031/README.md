# ActivityKit elapsed formatter experiment — 31 October 2026

Both app-defined `DiscreteFormatStyle` variants compiled, but failed in the actual system-hosted Lock Screen renderer. The activity displayed a skeleton rather than counters, and `WidgetRenderer_Activities` logged `Errors.noType` naming `LabAttributedStyle` or `LabStringStyle`. Apple's native timer/progress controls and its built-in `TimeDataSource` stopwatch worked while the host process was stopped.

| Trial | Observed result | Evidence |
| --- | --- | --- |
| Native timer and date progress | Timer advanced from `0:03` to `0:30`. The short bar filled and remained full past the synthetic restart boundary. | [3 seconds](control/lockscreen-t03.png), [30 seconds](control/lockscreen-t30.png), [short clip](control/control-short.mp4) |
| Custom AttributedString | Placeholder at +3 and +8 seconds; archive could not resolve the custom type. | [3 seconds](attributed/lockscreen-t03.png), [8 seconds](attributed/lockscreen-t08.png), [exact errors](attributed/decode-errors.txt) |
| Custom String | Placeholder at +3 and +6 seconds; same failure class for the String type. | [3 seconds](string/lockscreen-t03.png), [6 seconds](string/lockscreen-t06.png), [exact errors](string/decode-errors.txt) |
| Built-in TimeDataSource | Stopwatch advanced `00:03.15` to `00:06.14`; clock advanced `6:44:38 pm` to `6:44:41 pm`. | [3 seconds](system/lockscreen-t03.png), [6 seconds](system/lockscreen-t06.png) |

The system control distinguishes an app-defined formatter decoding failure from a general failure of the `Text(TimeDataSource, format:)` route. This result covers these two implementations on this simulator runtime; it does not establish behavior on every OS release or physical device.

## Method and provenance

- Disposable SwiftUI host and real ActivityKit extension, built with Xcode 26.4.1 (17E202), simulator SDK 26.4. iPhone 17 Pro simulator, runtime named iOS 26.4, reported version **26.4.1**, build **23E254a**. Harness deployment target 26.0.
- One activity per trial, created in the foreground with a fixed future origin. No `Activity.update`, app timer, network request, push token, or background task. Launched with `simctl`, with no debugger attached.
- After the host reported the background scene phase, the runner validated its executable path and sent **only that harness PID** SIGSTOP. Every checkpoint recorded a stopped `Ts` state with constant CPU time: control `0:01.69`, attributed `0:01.64`, String `0:01.74`, system `0:01.74`.
- The system rendered the native controls without host execution. The failed custom variants never rendered their computed distance, resource, or restart values. A successful model calculation or compilation is not counted as renderer success.
- Screenshots are full simulator captures. The older River demo activity above the lab card is unrelated to this trial. The native control's early screenshot includes the lab permission prompt; the card remains visible. Later screenshots are after permission was accepted. Custom placeholders persisted after permission was accepted and the display was awakened.
- Screenshot timestamps are taken immediately before invoking `simctl screenshot`; they are request timestamps, not exact pixel acquisition times. The system stopwatch therefore includes capture latency. The clip is an approximately 13-second excerpt, beginning about one second before the synthetic origin. Use the onscreen timer for clip timing.
- `results.json` separates actual process/timestamp measurements and visually transcribed output from **model expectations**. Expected distance/resource/restart values were not observed in the failed custom activities. Raw runner checkpoints include these expectations; the packaged measurements separate them.
- The attributed capture process had already loaded the earlier checkpoint list, so it captured +0/+3/+8/+15/+30/+45. The retained runner reflects the final shorter +0/+3/+6 custom schedule. This changes capture duration, not the harness or failed formatter source. Only key images are included; `screenshotIncluded` identifies omitted raw captures.
- Source hashes match the final built harness sources. `provenance.json` records build and binary hashes. The harness has no Unity source dependency. No production code integration, app version change, game launch, upload, or deployment occurred for this experiment.

## Limits and outcome

The native date progress bar represents one segment. It saturates at its cap and does not restart automatically. The custom model describes a restart at +2 seconds and movement again at +4, but that behavior is **unverified in the system renderer** because the custom view failed decoding.

The observed native control cards report `Reduced luminance: no`. Simulator dimming behavior does not prove physical Always-On refresh cadence. Dynamic Island views exist in the source, but this experiment independently captured only the Lock Screen. There is no physical phone, Watch, Always-On, or Dynamic Island acceptance claim.

An earlier setup attempt failed to background the harness in time and is excluded. Raw videos, broad process logs, generated Xcode projects, build products and downloaded DocC JSON remain in ignored local artifacts. Only exact relevant error excerpts are committed.

The final cleanup resumed and terminated only the disposable harness. The primary game checkout's HEAD and diff hash match their baseline. All 227 original files other than an already changed Unity Editor Analytics cache match their original hashes; progression files were preserved. See [preservation](preservation.json) and [cleanup](cleanup.json).

## Reproduction

Copy `Harness` to a disposable output directory, generate its Xcode project with `xcodegen generate --spec Harness/project.yml`, and build the `TimeFormatLab` scheme for an owned iOS 26 simulator with `CODE_SIGNING_ALLOWED=NO`. Install only `com.blindsidedgames.timeformatlab`. Launch it with `--trial control`, `attributed`, `string`, or `system`.

`run-trial.py` is the captured experiment script, with the original simulator UUID. Run a copy outside this evidence directory after selecting an owned simulator and adapting that UUID. It deliberately refuses to overwrite a trial directory. It requires operator lock/wake interaction after launch, and leaves its host stopped for capture verification. Resume or terminate **only its validated harness PID** afterward. A setup failure can leave capture processes running; clean up those owned processes before another trial. Do not run it unattended against a user's active game session.

API references: [AttributedString initializer](https://developer.apple.com/documentation/swiftui/text/init(_:format:)-3lqg1), [String initializer](https://developer.apple.com/documentation/swiftui/text/init(_:format:)-8sfgg), [DiscreteFormatStyle](https://developer.apple.com/documentation/foundation/discreteformatstyle), [TimeDataSource](https://developer.apple.com/documentation/swiftui/timedatasource).
