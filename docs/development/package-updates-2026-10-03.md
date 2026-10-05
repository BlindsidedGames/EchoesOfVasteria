# Package updates — 2026-10-03

Unity Editor remains **6000.6.0f1**. Work was performed on `fix/editor-startup-terrain-catalogue`, initially `6357a40d12df30cb94e1719c42a48ded94cdcf4a`; authenticated remote read matched this commit.

The manual A* update is present locally: HEAD 5.4.6 → working tree 5.4.7. Odin version is 4.0.2.4 in HEAD and the working tree, with local vendor reimport changes. Those pre-existing changes are preserved and excluded from this task’s commit. No manual update is missing from this Mac checkout that can be established from the supplied request.

## Updated dependencies

| Package | Before | After |
|---|---|---|
| `com.unity.2d.animation` | 16.0.0 | 16.0.1 |
| `com.unity.2d.psdimporter` | 15.0.0 | 15.0.1 |
| `com.unity.2d.tilemap.extras` | 9.0.0 | 9.0.1 |
| `com.unity.ide.rider` | 3.0.39 | 3.1.0 |
| `com.unity.ide.visualstudio` | 2.0.27 | 2.0.28 |
| `com.unity.localization` | 1.5.12 | 1.5.13 |
| `com.unity.project-auditor-rules` | 1.0.3 | 3.0.0 |
| `com.unity.services.cloudsave` | 3.4.0 | 3.4.1 |
| `com.unity.services.leaderboards` | 2.3.3 | 2.3.4 |
| `com.unity.addressables` | 2.11.2 | 4.1.0 |
| `com.unity.scriptablebuildpipeline` | 3.0.3 | 4.0.0 |
| `com.unity.services.authentication` | 3.7.4 | 3.8.0 |
| `com.unity.services.wire` | 1.5.0 | 1.6.0 |
| `com.unity.splines` | 2.9.0 | 2.9.1 |
| Steamworks.NET | 2024.8.0 / SDK 1.60 | 2025.164.1 / SDK 1.64 |

Registry versions were selected from [official Unity package metadata](https://packages.unity.com/com.unity.addressables), filtered to stable numeric releases supporting Unity 6000.6 or earlier. Tarballs came from the metadata’s official download URLs and matched the supplied SHA-1 checksums. Previously transitive Addressables, Scriptable Build Pipeline, Authentication, Wire and Splines are explicitly pinned so the selected updates remain reproducible. UPM corrected Profiling Core and Settings Manager dependency depths from 2 to 1.

Steamworks.NET was obtained from the [official stable Unity release](https://github.com/rlabrecque/Steamworks.NET/releases/tag/2025.164.1). Existing asset GUIDs and the game’s custom SteamManager were preserved. The official release removes `isteammusicremote.cs`; there are no game callers, and its now-missing native API is not retained as a broken wrapper. The new Mac native library includes arm64 and x86_64.

## Retained versions and limits

- [A* 5.4.7](https://arongranberg.com/astar/documentation/stable/changelog.html), [Odin 4.0.2.4](https://odininspector.com/patch-notes), [Quantum Console 2.6.7](https://assetstore.unity.com/packages/tools/utilities/quantum-console-211046), [MPUIKit 1.2.3](https://assetstore.unity.com/packages/tools/gui/modern-procedural-ui-kit-163041), and [Better Rule Tiles 1.5.0](https://docs.vinark.dev/better-rule-tiles/Changelog) match the official stable releases inspected.

- 2D Aseprite 6.0.0, Common 15.0.0 and SpriteShape 16.0.0 remain the compatible stable versions. Newer releases of these and the other 2D packages require Unity 6000.7; no Editor upgrade was performed.

- Built-in Cinemachine, URP/ShaderGraph, Timeline, uGUI, Burst, Collections, Mathematics, Test Framework and platform modules remain matched to Unity 6000.6. Registry versions numerically below these bundled versions are not upgrades.

- Input System 1.20.0, Collab Proxy 2.13.6, Recorder 5.1.7, Analytics 6.3.0, Cloud Code 2.10.4, Visual Scripting 1.9.12 and the Linux SDK/toolchain are already on their inspected stable versions. Other locked registry dependencies have no compatible newer stable release in the metadata inspected.

- Existing `com.unity.pipeline` 0.8.0-exp.1 has no stable release in the inspected registry. It is retained, with no new prerelease added and no feature removed.

- VoxelBusters Essential Kit/CoreLibrary native framework remnants and Rainbow Folders metadata remnants have no complete versioned managed package in this checkout. They were preserved. A reliable upgrade requires the complete authoritative package and verified existing entitlement; no purchases, new legal acceptance, account changes or credentials were made. Mobile builds were not validated.

## Validation evidence

The checked-in `tools/testing/run.py` creates disposable snapshots, unique game/preference identities, offline entitlement reads and an OS sandbox denying real save/preference writes and external IP connections. Registry packages are embedded in the disposable snapshots with recorded file hashes. The authored Editor was already open; it was not relaunched or placed in Play. It resolved the new packages and reported ready, compiling=false, Main loaded clean.

- Initial protected baseline setup failed before Unity launch because the outer filesystem sandbox denied creation of the test-only native namespace. The approved retry used the unchanged protected harness.

- Original dependency baseline: **185 EditMode + 56 PlayMode passed, zero failed/skipped**. Evidence: `/tmp/eov-package-baseline-20261003b/summary.json`.

- Updated dependency snapshot: **185 EditMode + 56 PlayMode passed, zero failed/skipped**. Evidence: `/tmp/eov-package-final-20261003/summary.json`.

- Final UPM-normalized manifest/lock snapshot: **185 EditMode + 56 PlayMode passed, zero failed/skipped**. Exact authored manifest/lock and all owned Steamworks bytes match `inputs.json`. Evidence: `/tmp/eov-package-final-lock-20261003/summary.json`.

- Updated Mac Mono build: Unity `Succeeded`, process exit 0, 223,708,283 bytes. It logged 7 scene repair errors and 10 warnings; the original dependency baseline build also succeeded with the same 7 error messages and 10 warnings. These diagnostics predate the updates; no unrelated Main scene rewrite was performed. Evidence: `/tmp/eov-package-final-20261003/build-proof/build-report.json`.

- Original Mac Mono baseline build: **Succeeded**, process exit 0, identical 7 scene repair errors and 10 warnings. Evidence: `/tmp/eov-package-baseline-20261003b/build-proof/build-report.json`; exact error comparison: `/tmp/eov-package-evidence-20261003/build-comparison.json`.

- A first build preparation was refused because a test-only preference file had reappeared after test cleanup. The retry used a fresh disposable build identity; no real preferences were removed.

- Not run: player launch/Metal visual smoke, Steam production sessions, live cloud calls, Windows/Linux/mobile/AOT builds. Mac build success is not evidence for those checks.

A normal authored-project harness run can use the resolved new registry cache: each updated package has exactly one current cache entry; no duplicate stale version blocks snapshot preparation.

Registry inventory, download metadata, scoped vendor file hashes, resolver records and original statuses are in `/tmp/eov-package-evidence-20261003/`, `/tmp/eov-package-metadata/` and `/tmp/eov-package-registry-inventory.json`.

Before farming source work resumed, SHA-256 comparison of every original Unity input outside manifests/lock/Steamworks found **zero unrelated changes**. No saves were copied into snapshots. No authored scene or farming/UI file was edited. Git/index and Unity validation slots are coordinated in `/tmp/eov-unity-coordination.json`.

Persistent evidence archive: `/Users/matthewrushworth/Projects/Echoes Package Update Evidence/2026-10-03/`. It contains the inventories, original/final manifest and lockfile records, file hashes, logs, NUnit XML, build reports and `EOVPackageVerification.app`; no Unity entitlement bytes, real player saves, full project Library or account credentials were archived.
