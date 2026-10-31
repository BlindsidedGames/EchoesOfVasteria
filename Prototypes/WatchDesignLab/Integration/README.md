# Production Watch view verification

`EchoesWatchPreview` compiles the production views, presentation model and decoder with an isolated fixture adapter. `EchoesWatchStore` compiles the actual production App and WatchConnectivity store in a unique standalone simulator bundle. Neither target is a replacement for the shipping companion.

Run `python3 Prototypes/WatchDesignLab/Integration/Tools/prepare.py` to stage the complete source character catalog and production static art. It writes only Artifacts plus this harness's fixture resources. The production Unity postprocessor owns the shipping export; the Python staging path is native QA only. Generate the Xcode project with `xcodegen generate --spec Prototypes/WatchDesignLab/Integration/project.yml` and build either scheme for the owned Watch simulator. Generated project/build products are not tracked.

Preview arguments select `--screen=automatic|dial|health|resources|away|skills|history|comparison`, a named `--state=` fixture and optional `--text=large`. These arguments do not exist in the production app. The clock is frozen relative to each fixture origin for repeatable views; hero animation uses native render time. Preview Refresh drives presentation states only and is not a WatchConnectivity pass.

The Foundation executable `check-presentation.swift` owns the new Watch projection boundary. Its risks are mixing phone totals with gains, selecting stale/wrong-map estimates, and animating cached data as current. Existing decoder/managed tests cannot catch those view projection mistakes. It adds no test-only production seam; production calls the same presentation type. It deliberately does not replay the sampler's existing interpolation tests.

See `docs/development/watch-integration-20261101` for current captures, contracts and handoff limits.
