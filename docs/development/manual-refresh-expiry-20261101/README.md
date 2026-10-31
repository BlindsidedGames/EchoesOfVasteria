# Manual Refresh and estimate expiry validation

The core native allowance, endpoint, stale Refresh and paired Watch request paths pass. The 42mm endpoint dial clipping is now resolved by `dd3d800fc`: fresh 42mm and 46mm native captures keep the full distance and health line inside the circle. See the [clipped 42mm before](captures/watch42-after-limit-dial.png), [42mm after](captures/watch42-limit-dial-dd3d800-distance.png), and [46mm after](captures/watch46-limit-dial-dd3d800-distance.png).

Final production source: `dd3d800fc` (includes `a0da19366`, `91b0d309c`, `b22cea9ea`, `0f5ad93a4`). The combined native/transport proof below uses `a0da19366`; the final Watch-only view adjustment is separately rebuilt and captured on `dd3d800fc`. Work is confined to the existing Mac validation checkout and disposable native bundles. No production Swift/C# edits, player-save writes, gameplay settlement, release, version/build changes, or store mutations occurred here. Root owns `gallery.html`.

## Executable proof

| Owner | Result | Boundary |
| --- | --- | --- |
| Existing decoder/sampler runner | 21 pass | Native wire validation, legacy frames, interpolation/reap/restart behavior |
| Watch presentation runner | 15 pass | 11 existing contracts plus four allowance/expiry regressions, including eight hours after departure |
| Receipt runner | 9 pass | Encoding, source rollover, origin/end/map binding, skew, disabled background data, legacy source omission |
| Activity content runner | 9 pass | All preloaded scalar selection, status/Refresh retirement, live-total guard, invalid/missing endpoint, legacy allowance omission |
| Generated Xcode graph | Pass | Fresh host membership/linkage and repeated migration from actual old framework membership |

The four presentation regressions fail against baseline `6ee0ef926` for the intended reasons and pass after the repair. The early-coverage fixture removes its separate phase-2 player-input stop, so it specifically tests coverage expiry; waiting/restart remain separate sampler/presentation contracts. Logs are in `proof/`. Pure tests do not establish system rendering or transport delivery.

Xcode 26.4.1/Swift 6.3.1 compile the exact production host bridge, intent, attributes, decoder, phone session, widget extension and Objective-C caller, plus the actual Watch Store sources. Deployment targets are iOS 16.2/watchOS 11; simulator runtime is 26.4. These are minimum deployment builds, not execution on old OS runtimes. The independent Watch Preview also builds and supplies the fixed-clock 42/46mm fixture captures. The final embedded build compiles the host, transport framework, widget and Watch companion together.

The real production Objective-C C entry point publishes from the separate QA framework into host-owned Swift classes through its unchanged runtime lookup. UnityFramework is absent in the QA process. The C# packaging driver executes the unmodified production postprocessor with Unity 6000.6.0f1's actual PBXProject implementation and project template. Only editor configuration is supplied and unrelated portrait generation is omitted. The generated graph puts PhoneSession and the activity owner together in the host, links WatchConnectivity, leaves the ObjC caller in UnityFramework, removes an actual seeded legacy PhoneSession framework membership, and restores the same memberships after repeated processing. This is native module/generated-project validation, not a full Unity export or game build.

## Passive endpoint on the system Lock Screen

The host consistently rebases the controlled synthetic high-rate fixture to the real clock in its own cache. These 540000 gains are deliberate QA data, not measured gameplay. The bridge publishes once. The host does not update activity content on a timer or request background execution time; its short readback task suspends normally after backgrounding.

Activity `D36CDD1E-5E5C-40AB-AA59-0578C599B079` had staleDate `1793488678.675851`. Before it, the activity was active with approximately 536357 current gains and a valid terminal payload of 540000 gains, 212/212m, status `limitReached`. The elapsed clock stops at 1:00:00. The dimmed Lock Screen retained its prior scalar render past the deadline. Normal Device → Lock/wake rendered the preloaded endpoint and removed Refresh without foregrounding the app or pressing Refresh. Only after capturing that result was the host foregrounded for readback: at `1793488719.579575`, the same ID was stale. This proves normal-wake rendering, not exact redraw timing while dimmed. See [before](captures/limit-before-deadline.png), [dimmed after deadline](captures/limit-after-deadline-before-wake.png), [normal wake](captures/limit-after-normal-wake.png), and the paired `passive-limit-*` receipts.

The early-coverage case separately renders [Estimate ended](captures/early-estimate-ended.png), with its 60-second endpoint gains of 9000 and the declared 1h maximum retained. It does not claim the full allowance was used. Its elapsed clock remains the allowance-bounded departure clock, distinct from the frozen forecast scalars.

## Real Refresh and native receiver

A CUA tap on the actual production Lock Screen Refresh button after the endpoint ran the production LiveActivityIntent. At `1793488933.054339`, the native bridge updated activity `AD78CB65-8368-4515-955B-98E811F9011F` to `limitReached`, 540000 gains, revision 2. The application stayed background before/after (2/2). The [rendered card](captures/actual-refresh-after-deadline.png) then removed Refresh. This is actual input proof; it is separate from the controlled callback probe.

The controlled probe invokes the real WCSession delegate method, without a replacement handler or transport mock. Foreground requests return queued cache; the real ObjC consume entry point consumes that queue exactly once, while foreground Activity Refresh preserves content. For the already-stale branch, activity `34BA16DE-91FA-474A-8420-FFFC030DF515` is stale before the background request, remains the same ID/source/origin, and advances Refresh revision 1 → 2 while retaining the endpoint. The reply is estimated, its receipt matches, snapshot bytes are unchanged, no Unity refresh is queued, and UnityFramework remains absent. See `receipts/stale-*`, `callback-foreground-guard.json`, and `proof/stale-owner.log`. Immediate content readback can precede ActivityKit's local notification; subsequent foreground readback supplies the observed update. The callback probe alone does not claim radio delivery.

## Actual paired Watch delivery

`Harness/project.yml` embeds a QA Watch app using the unchanged production App/Store/views and existing native art, with bundle `com.blindsidedgames.echoes.manualexpiryqa.watchkitapp` and WKCompanionAppBundleIdentifier `com.blindsidedgames.echoes.manualexpiryqa`. Install the embedded app on the owned paired Watch as well as the host on its paired phone. This matching embedded configuration matters: the unrelated standalone Store QA bundle initially reports installed=0. The matched phone reports installed=1, and the Watch reports reachable=1 and receives the production context.

CUA Device → Gestures → Double Tap exercises the real Refresh primary action. With the phone foreground, [the Watch displays queued](captures/paired-watch-foreground-refresh.png). A later capture after the 20-second deadline [no longer displays queued](captures/paired-watch-after-queue-timeout.png). With the phone on Home in the background, the second real request reaches the native receiver. At `1793489342.608306`, the phone updates existing activity `671F5296-FD15-4078-B74A-69B1238CE051` to 540000 gains/status limitReached/revision 1 while app state stays 2/2, then replies estimated. The native Watch retains its endpoint without a queued label. The phone cache SHA256 stays `dc179541104f516a0f24e5f1a23ae22f74c9039e86344436cf9acb8cbf1a7fa7`, generatedAt stays `1793485674.3257132`, and input bytes equal cache bytes. Paired receipts verify the same activity/source/origin and no UnityFramework. No scripted callback probe runs in this scenario. See [Watch result](captures/paired-watch-background-refresh.png), `proof/paired-*`, and `receipts/paired-*`.

This proves the two real foreground/background delivery branches on these simulators. A forced late/out-of-order Watch reply interleaved with a newer context, cold process wake, physical-device behavior, and full spoken accessibility output were not independently exercised.

## Layout outcomes and limits

- Native wide and [narrow phone portrait](captures/phone-narrow-portrait.png) cards fit the elapsed time, allowance, gains and control. Actual Refresh input succeeded on the wide phone.
- A real long press expands the portrait Dynamic Island. [Before Refresh](captures/island-expanded-before-refresh.png) and [after the paired Watch Refresh](captures/island-expanded-after-watch-refresh.png) show the production content and terminal control retirement.
- The system Lock Screen stays in portrait when the physical simulator rotates; [the narrow rotated capture](captures/phone-narrow-rotated-lockscreen.png) is not a landscape card pass. Safari runs in actual landscape, but a bounded long press on its side Island did not expand the activity; [that system limitation is captured](captures/phone-landscape-system-island-unavailable.png). Both phones were restored to Portrait.
- Watch 42/46mm away screens show a capped 1:00:00/Limit reached or the earlier Estimate ended as appropriate. Fixed-clock Preview evidence is distinct from the actual Store/transport evidence. The fresh 42/46mm dial captures on `dd3d800fc` resolve the earlier distance/health clipping.
- The supplemental small-family source compiles. A mirrored system pill appeared on Watch, but a complete small-family card was not accepted as rendered proof in this pass.

## Final Watch dial follow-up

Both existing Watch Preview and actual Store targets rebuild successfully against exact production commit `dd3d800fca01dd624e16ce0e47d2c6a0677674a8`. Fresh native Preview captures use the same high-rate fixture at offset 3600, screen dial. Full `212 / 212 m` and the heart/`Last 90%` line fit inside the circle on both 42mm and 46mm; the original clipped image is preserved for comparison. These are native renders of the exact production view, with a fixture clock rather than transport substitution claims.

A real coordinate tap on each dial was attempted to inspect resource/time focus. Both failed before input delivery: 42mm `windowNotFoundAtPosition((127.0, 270.0))`, 46mm `windowNotFoundAtPosition((1151.0, 336.0))`. No dial button is exposed in the Simulator accessibility tree, so no AX replacement was available. Resource/time focus therefore remains unverified; this is an automation delivery limit, not a failed product hit test. No test-only production hook or injected focus was added. No transport/content suite repetition was needed for this four-line view adjustment.

The temporary Preview apps were removed from both owned Watches after capture, and the existing 42mm Store QA app was restored. The primary dirty diff SHA256 remains unchanged. Root's gallery is untouched.

## Reproduction and preservation

Run `Harness/prepare.py`, then XcodeGen with `Harness/project.yml`. Build scheme EchoesManualExpiryQA for the owned paired phone simulator with signing disabled, **without a global `-sdk` override**, so the embedded Watch target selects watchsimulator. Install the phone app and its embedded Watch app on the paired devices. Launch using `simctl launch ... <bundle> -- --scenario=limit` or `early`. Confirm the displayed scenario after installation; simulator app restoration can initially launch with defaults. `probe` adds the controlled owner callback checks, while `probe-existing` reuses an existing cached activity without republishing it.

Compile `check-receipt.swift` with the production snapshot and allowance sources, passing Integration/Fixtures. Compile `check-content.swift` with production ActivityAttributes for the iOS simulator and run with simctl spawn. Existing presentation and decoder/sampler runners retain their respective primary contracts. `Harness/PackageReadback.csproj` requires `-p:UnityXcodeDll=<Unity iOS support DLL>` and arguments `<repo> <Unity project template> <isolated output>`; then run `Harness/check-package.py <isolated output>`.

Version remains 1.5.9 (188). The primary checkout stays read-only with its two pre-existing dirty localization/font assets. No player-save path is used. Generated projects, staged art and build products are ignored. Only disposable QA bundles/caches/activities are removed during cleanup; the original game/activities and other apps remain outside the cleanup scope.
