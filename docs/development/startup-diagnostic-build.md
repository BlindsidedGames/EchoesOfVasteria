# Internal startup diagnostic build

Enable `EOV_STARTUP_DIAGNOSTICS` only for internal builds. The startup menu offers:

1. Normal startup: original services and UI.
2. Pause before town activation: load scene assets, then wait for an explicit Continue before Awake/OnEnable.
3. Skip Game Center sign-in: suppress authentication requests and achievement reporting. This does not remove the native SDK.
4. Skip online services: skip UGS initialization/sign-in. Game Center remains independent.
5. Skip main UI Toolkit screens: keep scene/game initialization, omit main Toolkit screen construction. The independent diagnostic overlay remains available.
6. Minimal: combine checks 3, 4 and 5. A successful combined check alone cannot identify which subsystem matters.

Close and reopen the app between checks so initialized services cannot leak into the next comparison. No reinstall or new build is required. Each check uses the existing selected save; diagnostics never reset or replace it. Ordinary gameplay save behavior remains enabled.

The independent overlay shows elapsed startup time, the latest operation, Game Center status, memory warnings and managed exceptions. On iOS it also reports native physical footprint and current memory headroom using `task_info` and `os_proc_available_memory`. A worker samples every 500 ms, including while the main thread is blocked. Peaks are sampled values, not guaranteed instantaneous maxima. The main-thread gap is an observation, not proof of a deadlock; backgrounding the app can also increase it.

`startup-diagnostics.json` preserves the last attempt and `startup-results.json` the latest result per mode. Both live beside, not inside, save slots. Writes use temporary files and replacement; failure to write diagnostics never blocks gameplay. The next launch displays the last incomplete attempt before a new selection overwrites it. No diagnostic records are transmitted.

The native sampler cannot identify a crash cause by itself, intercept native crashes, or replace Apple crash/jetsam reports. A last operation is a breadcrumb, not proof that operation caused termination. Simulator headroom is not the physical device's budget.

For isolated automation, pass `-eovDiagnosticMode=Normal` (or another enum name from `StartupDiagnostics.TestMode`) to bypass the selector. Verify normal, paused activation and omission modes with disposable simulator data before upload. Keep the release save format and authentication behavior unchanged when the define is absent.
