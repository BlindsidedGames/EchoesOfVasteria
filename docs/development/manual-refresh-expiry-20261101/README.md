# Manual Refresh and estimate expiry validation

Validation in progress. Production source checkpoint: `b22cea9ea` (includes `91b0d309c`). Work is confined to the existing Mac validation checkout and disposable native bundles. No production source edits, player-save writes, gameplay settlement, release, version/build changes, or store mutations occur here.

## Current proof

- The 11 existing Watch presentation contracts pass. The four new allowance/expiry regressions fail against baseline `6ee0ef926` and pass against the checkpoint. Early coverage's fixture removes the separate phase-2 player-input stop so it tests coverage expiry specifically.
- Existing native wire/sampler checks: 21 pass, including legacy frames and decoder compatibility. Ten separate receipt contracts pass: current source, rollover, origin, endpoint, map, clock skew, opt-out, foreground data and timeline-only legacy acknowledgement.
- Actual Watch Store and Preview targets compile for watchOS 11. The phone host and unchanged production widget, bridge, intent and phone session compile for iOS 16.2. The disposable transport framework compiles the exact production Objective-C caller and its separate snapshot Swift module; the host receives publication through that real C/ObjC lookup path.
- The production C# postprocessor executes using Unity 6000.6.0f1's real PBXProject library and project template. Its editor configuration is supplied by the harness; unrelated portrait generation is omitted. The generated graph places PhoneSession and activity owner together in the host, links WatchConnectivity, leaves the real ObjC caller in UnityFramework, removes an actual seeded legacy PhoneSession framework membership, and has the same memberships after repeated processing. This is generated-project validation, not a full Unity export/build.

## Passive endpoint on the system Lock Screen

The disposable host consistently rebases the controlled synthetic high-rate fixture to the real clock. No Unity or player cache is involved. The bridge publishes once. The host does not update activity content on a timer; its short readback task suspends normally after backgrounding and requests no extra execution time.

Activity `D36CDD1E-5E5C-40AB-AA59-0578C599B079` had staleDate `1793488678.675851`. Before that deadline it was active, with approximately 536357 current gains and a valid terminal payload of 540000 gains, 212/212m, status `limitReached`. The native elapsed clock stops at 1:00:00. The dimmed Lock Screen retained the previous scalar render after the deadline. Normal Device → Lock/wake rendered the preloaded endpoint and removed Refresh, without foregrounding the app or pressing Refresh. Only after capturing that result was the host foregrounded for readback: at `1793488719.579575`, the same activity ID was `stale`. This proves normal-wake rendering, not an exact redraw time while dimmed. See [before](captures/limit-before-deadline.png), [dimmed after deadline](captures/limit-after-deadline-before-wake.png), [normal wake](captures/limit-after-normal-wake.png) and [native readback](receipts/passive-limit-after-render.json).

## Reproduction

`Harness/project.yml` compiles the production sources directly. Run its `prepare.py`, generate with XcodeGen, and build scheme EchoesManualExpiryQA with signing disabled for an owned simulator. Launch with `--scenario=limit` or `--scenario=early`; both deadlines are 25 seconds away, with different reasons for stopping estimates. `--scenario=probe` also invokes the real WCSession delegate owner on backgrounding; that is owner-boundary evidence, not radio transport delivery.

Compile `check-receipt.swift` alongside the production snapshot and allowance sources, passing the Integration/Fixtures directory. Existing presentation and wire/sampler runners remain their contracts' primary owners. `Harness/PackageReadback.csproj` requires `-p:UnityXcodeDll=<Unity iOS support DLL>` and arguments `<repo> <Unity project template> <isolated output>`. Run `Harness/check-package.py <isolated output>` to assert the generated graph.

Further native Refresh and layout results will be added before final handoff. No full Unity build or paired WatchConnectivity radio delivery is claimed.
