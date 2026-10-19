# Internal startup diagnostic build

Add `EOV_STARTUP_DIAGNOSTICS` to the build's extra scripting defines (or the iOS
scripting define symbols). Do not enable it for general releases.

This build retains the normal save, networking and Game Center behaviour. It
adds startup phase labels and stage-based progress, plus a separate Game Center
status. The percentage identifies stages; it is not a time estimate.

Startup checkpoints are written to `startup-diagnostics.json` in the app's
persistent data directory, separate from save slots. They contain the app
version, UTC time, phase, Game Center status, Unity allocated memory and whether
startup completed. They contain no save contents and are not uploaded. Failure
to read or write diagnostics does not prevent startup.

Ask the affected tester to launch twice and screenshot the loading screen on the
second launch. It shows the last incomplete launch's checkpoint even if the
current attempt stops earlier. A checkpoint shows the last reached stage, not a
proven cause: a native callback or memory termination can happen asynchronously.
A user closing the app during startup also leaves an incomplete checkpoint.

The diagnostic overlay remains visible through town initialization and applying
the loaded progress. Successful startup marks the checkpoint complete and hides
the overlay. Removing the define restores the normal loading screen and removes
diagnostic calls from call sites; no player-save migration is required.
