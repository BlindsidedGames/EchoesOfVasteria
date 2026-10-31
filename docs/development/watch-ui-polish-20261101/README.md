# Watch ring and phone Refresh validation

Production source tested: `4d7519865f422100d3f613aea1a1478c5f32cabb`, containing the scoped Watch ring and Live Activity Refresh changes. This directory owns native validation only. The root thread owns production fixes and the shortfalls review. No release, version change or full Unity export was performed.

## Watch geometry

The current production views build in the existing isolated Watch Preview target. Fresh native 42mm and 46mm Expedition captures show a fully rounded upper track/arc; the inset stroke remains within the existing dial frame. Compare [42mm after](captures/expedition-42-after.png) and [46mm after](captures/expedition-46-after.png) with the earlier [42mm](../watch-integration-20261101/captures/01-42-final.png) and [46mm](../watch-integration-20261101/captures/01-46-final.png) integration captures. All use the same live fixture. Native render time can select a different authored character frame; no screenshot retouching was used.

## Actual phone widget

The disposable `EchoesUIPolish` host compiles the exact current production widget template, ActivityAttributes, Refresh intent, native bridge and decoder unchanged. It stages one controlled trajectory from the prior actual Unity-captured helmet snapshot, rebased consistently to the real clock, in its own app cache. The system owns the Live Activity rendering. The unique QA bundle `com.blindsidedgames.echoes.uipolish` does not replace the existing Unity prototype or build 188. The original ended card remains visible above the new QA card in the Lock Screen captures.

The host and widget extension compile with Xcode 26.4.1/iOS Simulator SDK26.4. [Portrait system capture](captures/live-activity-portrait-after.png) shows the production Refresh control: visible 36-point square with18-point icon, inside the explicit48-point rectangular label target. Supplemental small-family source uses32/16 inside44. Expanded Dynamic Island source uses the regular target. These dimensions are source/layout facts, not measured successful edge hits.

The native Rotate action was exercised. The phone's Lock Screen remains in its portrait UI and is shown sideways when the physical simulator is rotated; [rotated capture](captures/live-activity-rotated-lockscreen.png) records that result. It does not establish a landscape card layout. A narrow-phone/small-family and expanded Dynamic Island render remain unverified in this pass.

## Critical input limit

Actual near-corner taps outside the visible36-point square but inside the intended48-point target were attempted using CUA. Coordinate input failed **before delivery**, including `windowNotFoundAtPosition((1168.0, 765.0))`, then after native Center `((986.0, 795.0))`. A half-scale non-actionable control coordinate also failed `((811.0, 429.0))`. No Refresh intent log was produced. The system Live Activity button was absent from the exposed iOS accessibility tree, so no AX replacement for the edge tap was available. This is not a failed product hit test and not a passing hit test: delivery was blocked by the automation backend. The reported accidental unlock/open-app behavior therefore still needs direct device/system acceptance. Dynamic Island expansion also requires coordinate input and was not claimed.

Native Simulator menus/toolbar input do work after recovery: bind Simulator, get a full AX state, perform the exposed **Raise** action on the standard window, then select **Window → Echoes Live Activity Prototype 20261031** using fresh AX indices. Fetch the next AX state before subsequent actions. Before Raise, menu selections returned stale element IDs. Raise restores menu selection and screenshots; it does not repair coordinate taps. **Device → Lock** successfully shows the system Lock Screen; the bezel Sleep/Wake button alone did not provide reliable lock-state evidence.

## Reproduction and preservation

Stage art with `python3 docs/development/watch-ui-polish-20261101/Harness/prepare.py`, generate the project with XcodeGen using `Harness/project.yml`, then build scheme `EchoesUIPolish` for the owned iPhone simulator with signing disabled. The harness has no mocked Refresh action or direct call to `perform()`: any successful Refresh would run the production intent and native bridge through the system. Its initialization is QA-only; it makes no Unity or save calls. Generated projects, assets and build products are ignored. The original captured JSON is retained for reproducibility, not presented as a new gameplay-accuracy measurement.

The primary checkout and old release worktree remain read-only. Decoder and native phone bridge were not edited. Other projects' simulators were not selected or changed. No new tests, dependencies or production testing seams were added.

## Actual Watch Refresh response

The actual Store target was rebuilt with the current ring fix, then its isolated QA cache was staged using the existing `Integration/Tools/store-cache.py`. CUA selected **Window → Echoes Watch Prototype 42mm**, then **Device → Gestures → Double Tap**. The native gesture activated the production Refresh button's primary action. After its visual feedback settled, [the after capture](captures/watch-store-after-refresh.png) changed from [Cached with age](captures/watch-store-before-refresh.png) to **Cached · iPhone unavailable**, while retaining48/212m,90% health,11tasks,1kill and16resources. This is the real store's unreachable branch, not Preview Refresh state substitution. It does not establish paired message delivery or phone recomputation.

The42mm status line visually truncates the unavailable message; the source supplies the complete meaning in its accessibility label. Spoken accessibility output was not verified. One bounded coordinate attempt to open the integrated Watch menu failed before delivery with `windowNotFoundAtPosition((59.0, 163.0))`. The Watch app's content was not exposed by the Simulator AX tree, so menu→another selected screen→dial remains unverified. No further coordinate retries or test seams were added.

## Final state

Watch Preview, actual Watch Store, and the isolated host plus exact production widget extension all finish with BUILD SUCCEEDED. The final generated Xcode project was rebuilt after its app-icon build setting was made explicit in the harness. The disposable phone host was uninstalled after capture and its log stream stopped; only its own QA activity/cache were removed. The phone orientation was restored to Portrait. The old Unity activity/build188 were retained, and the Watch remains on the owned Store QA app. Primary user binary-diff SHA256 still matches `7d45e5d114159fefba437f0b4df201af1939c4ffc59e6c29725a468ee1c68ec2`. No production source changed in this validation follow-up.
