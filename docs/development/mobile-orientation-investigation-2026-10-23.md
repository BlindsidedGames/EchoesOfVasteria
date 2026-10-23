# Intermittent black world after mobile rotation

**Status: deferred at the user's request on 23 October 2026. Unresolved.**

The physical-device fault has not been reproduced in a controlled test. No production camera or rendering change was made during this investigation. Resume from the evidence below when more device information or a reproduction is available; do not treat passing synthetic tests as proof that the fault is fixed.

## Reported behavior

On an iPhone 17 Pro, reported as running the latest iOS, changing from landscape to portrait can leave the compact play area black. The run continues and task progress indicators remain visible and update. Expanding the play area reveals the world below a fixed black band at the top. Opening Settings restores the image.

The user confirmed that the black area stays fixed on the screen rather than moving with the character. Neither the user nor the investigation established reliable reproduction steps. The exact physical-device iOS version and TestFlight build number were not supplied. Do not substitute the simulator's OS version for the user's version.

Original screenshots were copied to the local evidence directory under these names:

- `reported-landscape.png`: original `codex-clipboard-611e348e-1c41-4cde-9a87-7428c12b2757.png`, 2622 Ã— 1206.
- `reported-compact-black.png`: original `codex-clipboard-f099855b-dc13-4d10-a1cf-0272b38d4976.png`, 1206 Ã— 2622.
- `reported-expanded-black-band.png`: original `codex-clipboard-a0791c4c-f396-4d26-be60-5b2daa47fc46.png`, 1206 Ã— 2622.

## Baseline and code ownership

Investigated main commit: `76ee448897b03163459abb896a249ff728fcf7c1`. Editor and the native diagnostic player used Unity `6000.6.0f1`; URP package was `17.6.0`. The Mac independently checked the retained build 174 IPA's Unity framework and game data and found the same Unity version. Record these as historical facts, not assumptions about future builds.

Relevant source:

| File | Responsibility / observation |
| --- | --- |
| `Assets/Scripts/AspectRatioBox.cs` | Only production writer of `Camera.rect` found. Updates in `LateUpdate` and `RenderPipelineManager.beginContextRendering`, retaining the last usable viewport during invalid screen dimensions. Writes only when the desired rectangle differs. |
| `Assets/Scripts/UI/Toolkit/ToolkitResponsiveLayout.cs` | Owns portrait detection, safe-area conversion, compact/expanded world rectangles and focus state. Camera pixel coordinates start at the bottom; UI coordinates start at the top. |
| `Assets/Scripts/UI/Toolkit/ToolkitRunScreen.cs` | Owns the actual expand-button sequence, including closing menus before enabling focus. A diagnostic that merely calls `SetFocusView(true)` does not necessarily reproduce that sequence. |
| `Assets/Scripts/UI/TownWindowManager.cs` | Opening Settings changes menu/focus state. No explicit camera or graphics reset was found in that action. |
| `Assets/Scripts/UI/Toolkit/ToolkitWorldScreen.cs` | Projects world labels and bars into UI Toolkit. Their continued visibility does not prove the world framebuffer is being drawn. |
| `Assets/Scripts/UI/Toolkit/ToolkitBackdropScreen.cs` | Uses a landscape backdrop blur. Hidden elements can retain the blur style and affect the UI composition path. |
| `Assets/Scripts/CameraClampExtension.cs` | Controls world camera position/map bounds; the screen-fixed black region is less consistent with this than with viewport/composition state. |
| `Assets/Scenes/Main.unity` | Main base camera plus UI overlay camera; both have `AspectRatioBox` and both are tagged `MainCamera`. Main camera has post-processing enabled; overlay does not. |

The overlay camera's serialized legacy Canvas consumer is disabled by the Toolkit cutover. This is not sufficient evidence to remove the camera or change its tag. No additional production `RenderTexture` manager or `PixelPerfectCamera` was found as the obvious owner of the fault. Loading-screen and Settings code did not reveal a direct explanation.

The existing render-boundary reconciliation predates this investigation (commit `0954b64f8`). Checking only its rectangle values cannot detect a black or vertically misplaced rendered image.

## Strongest clue: viewport geometry

The expanded screenshot has black rows 0â€“409; the world begins at row 410. At width 1206, the expanded layout reserves approximately `92 Ã— 1206 / 360 + 102 = 410.2` physical pixels at the bottom for the HUD and safe area. That intended bottom reservation appears at the top in the faulty screenshot.

The compact viewport should be `1206 Ã— 9 / 16 = 678.375` pixels high at the top. Its expected region is black in the supplied compact image. A vertically misplaced viewport could place that world image behind the opaque lower UI, explaining both screenshots.

**This is an inference from image geometry, not a captured runtime diagnosis.** No camera state was collected while the physical device was broken.

## What was tested

Local evidence root: `C:/Users/mattr/Documents/Repositories/EchoesOfVasteria/Artifacts/ViewportRotation20261023/`. This directory is gitignored. The report itself is in the versionable `docs/development` tree; it has not been committed by this documentation task.

| Check | Result and limits |
| --- | --- |
| Render-boundary rectangle test | Passed. Confirms viewport reconciliation after a late change; does not prove correct pixels. |
| Rendered Editor test | Passed using actual `ScreenCapture` images. Exercises base/overlay stack enabled and disabled, three cycles, landscape/portrait, compact/expanded/compact, and menu closed/open/closed: 108 states, 81 sampled world points each. |
| Windows native IL2CPP test player | Same rendered test passed. Uses 1564 Ã— 720 and 720 Ã— 1564 to fit the desktop; Editor uses the reported 2622 Ã— 1206 and 1206 Ã— 2622 dimensions. Neither is an iPhone GPU test. |
| Final Editor group | `final-editor.xml`: 3 passed, including rendered rotation, render-boundary reconciliation and the separate desktop input regression. |
| Backdrop composition probe | `composition.xml`: passed. `AnyOverlayPanelHasBackdropFilter` remains true for a hidden element retaining a filter, then becomes false when its style is cleared. This identifies a composition-path change, not the fault's cause. Temporary probe logging was removed. |
| iPhone 13 mini simulator | Exact-main native Metal build showed correct compact placement. Fresh-player dialogue and the creator keyboard invalidated expanded-view coverage. |
| iPhone 17 Pro simulator, iOS 26.4.1 | Compact view validated at 1206 Ã— 2622. Expected pixel rectangle: y 1943.625, height 678.375. An OS screenshot with all Toolkit UI hidden contained world pixels in top rows 0â€“677, not at the bottom. No blackout reproduced. |
| Native Settings / post-processing checks | Settings open/close and temporary post-processing off/restore completed without logged Error/Exception/Assert. Since the fault was not active, this does not establish a remedy. |
| Native expanded view | **Inconclusive.** Production state reset `FocusView` to false. A final same-binary attempt including `CloseAllWindows` also remained compact. These screenshots must not be labelled expanded validation. |

The final synthetic fixture uses the game's `Resources/Characters/Composite` material, a white sprite tinted green and a global `Light2D`. It enables post-processing and creates a small blurred menu from the actual Toolkit panel settings. It samples inside the expected world viewport from the displayed frame. It does not simulate all gameplay, tutorial or mobile surface timing.

Test source: `Assets/Tests/PlayMode/Tests/ViewportRotationPlayModeTests.cs`. `Tests.asmdef` gained URP runtime and URP 2D references for this fixture. These changes were left uncommitted; preserve or review them when resuming.

### Failed experiments that were test faults

Do not count these as reproductions of the game bug:

- `rendered-stack.xml`: offscreen RenderView readback was vertically flipped relative to the displayed screen. Replaced with real `ScreenCapture` output.
- `sprite-stack.xml`: the deliberately visible gray test menu overlapped sampled world pixels because panel settings scaled its dimensions. Corrected to constant-pixel sizing.
- `native-windows-setup.xml`: a test-only shader was stripped from the native player. Switched to the game's resource-backed material.
- `native-windows-menu-overlap.xml`: an 80-pixel test bar overlapped the smaller native viewport. Reduced the artificial bar to eight pixels.
- Earlier retained Mac simulator apps lacked the current render-boundary hook. They were unsuitable as evidence for this main commit, so an exact-main diagnostic was built.
- Native observer build initially used APIs that were obsolete or unavailable in Unity 6000.6; these observer-only compile errors were corrected. No production source change was needed.

Retained intermediate XML, logs and images explain those failures. `native-windows.xml`, `final-editor.xml`, `validation.json` and this report describe the final results.

## Native evidence and isolation

Mac task ID: `01a14f26-9556-7747-9ad0-00063705c0ef`, historically titled `â€¢ Deploy Echoes internal TestFlight` (used here for diagnostics, not a release).

Mac root: `/Users/matthewrushworth/Documents/Codex/2026-10-19/task/orientation-20261023/`.

- `Echoes/`: isolated checkout of the exact main commit.
- `Echoes/Assets/OrientationProbe/OrientationProbe.cs`: observer guarded by `EOV_ORIENTATION_PROBE`.
- `evidence/run/`: first simulator run.
- `evidence/run-clean/` and `evidence/run-final/`: iPhone 17 Pro runs, state captures and OS screenshots.
- `evidence/release174-engine-provenance.json`: engine provenance check.
- `evidence/orientation-result.json`: 50 final iPhone 17 Pro state summaries, control states and pixel bounds.
- `evidence/artifact-index.json` and `evidence/final-preservation.json`: retained files and preservation checks.
- `evidence/run-clean/r2-compact-hidden-ui.png` and `evidence/run-final/v3-expanded-hidden-ui.png`: OS screenshots; despite the latter filename, focus remained false, so it is not valid expanded coverage.
- Raw `camera-trace.jsonl`, native export/build logs and observer build attempts are also retained under `evidence/`.

The final simulator framework UUID was `2B8A6FA1-C03A-389D-8F80-5D3729909542`, SHA-256 `d2822de633eccf28d6f38d34c61c4f66bce961c5a1a94202d9879f63f03169f2`. The helper requested `BuildOptions.None` and an Xcode Release configuration, but the simulator engine banner reported Development. This is an additional limit on equivalence to the installed release. The 50 recorded snapshots had no command errors or logged Error/Exception/Assert; main-camera post-processing was verified true / false / true.

The observer logged screen/safe-area sizes, focus/town state, camera rectangles and pixel rectangles, camera stack, projections, render target descriptors and relevant panel state. File-driven commands exercised orientation, compact/expanded requests, real Settings open/close, UI hiding and temporary post-processing toggles. OS `simctl` screenshots were the primary native image evidence.

Private bundle ID: `com.blindsidedgames.echoesofvasteria.orientationprobe20261023`. The build used a guarded private container and disposable save root. Only a previously created disposable test profile was copied into the second private container to avoid the creator keyboard. Real player saves and the existing running simulator were left alone. The dedicated app and simulator were shut down at the end. No upload or release occurred.

## Related engine issue, not a confirmed match

[Unity UUM-153520](https://issuetracker.unity.com/issues/25116/camera-renders-at-the-wrong-viewport-position-when-post-processing-is-enabled-with-a-positive-viewport-rect-y-value-in-the-urp-2d-renderer) describes URP 2D with post-processing rendering a positive-Y viewport at the bottom instead of the top. At investigation time it reproduced in 6000.3.16f1 and 6000.3.25f1, but not 6000.6.2f1; the reported affected stream was 6000.3. Our runtime was 6000.6.0f1.

The visual similarity makes this useful context, but the version and platform differences prevent claiming it is our bug or that an engine upgrade fixes it.

URP source inspection found that post-processing, camera stacking and non-default viewports can select intermediate render targets. Backbuffer and intermediate texture coordinate origins differ, and Toolkit backdrop filters can change the final composition path. No package-cache edits, permanent post-processing disable, engine upgrade or speculative camera-reset workaround was applied. The default volume includes a vignette, so disabling post-processing would also change appearance.

## Separate desktop bug already fixed locally

The earlier unclickable-menu bug is distinct and was confirmed fixed by the user. Right-click dismissal previously destroyed the receiving panel on button-down, before the UI input system received button-up. A trace showed stale right-button state and soft capture on the navigation panel, explaining why inner controls neither hovered nor scrolled.

Local changes move dismissal to right-button release in `LateUpdate` in `TownWindowManager.cs` and `ToolkitRunScreen.cs`. `SharedScrollingPlayModeTests.cs` exercises the actual input path in both orientations: right-click close, reopen, then left-click Saves. It failed before the fix and passed afterward. Evidence is in `Artifacts/FieldsInputLock20261023/`, including before/after XML, `stuck-v2-snapshot.log`, `observations.md` and `fix.patch`. The local corrected player was tested by the user. Keep these changes separate from any future mobile rendering hypothesis.

## Resume when more evidence is available

1. Record the exact app version/build and iOS version. Ask what map and menu were active, whether portrait was compact or expanded beforehand, and whether the app had resumed from the background. These are unknowns, not established triggers.
2. If the fault returns, preserve the broken state long enough to capture a screenshot or recording before opening Settings. Useful comparisons are compact versus expanded, and whether opening Settings or merely closing a menu restores it. Do not make the user repeat extensive blind tests.
3. Capture physical-device camera rectangles, screen/safe-area sizes and the actual presented image together. If rectangles are correct but pixels are misplaced, focus on render targets/final composition. If rectangles are wrong, trace layout/focus state first.
4. If native expanded coverage is needed, begin from a verified tutorial-complete disposable profile and exercise the actual expand button's sequence. Verify `FocusView == true` and expanded `WorldPixels` before counting any screenshot as coverage. Do not repeat invalid fresh-profile runs.
5. Test any suspected workaround against an active failure and normal landscape/portrait transitions. Preserve map bounds, UI input, tutorial pointers and saves. Do not merge a camera reset or disable post-processing solely because it sounds plausible.

The diagnostic did not intentionally change user saves. A late Mac preservation check found one exception requiring attribution: `Saves/Save2/meta.json` in the original root changed at 04:41:27 UTC (embedded metadata timestamp 04:41:22, build ID 1.4.3), before the first diagnostic launch at 04:44:03 UTC. The private diagnostic app was 1.4.13 and used the verified Simulator `orientation-save` root. The source of the metadata change is unproven; a running Steam process was observed, but external sync or user activity is only a possibility. Original snapshot payloads were unchanged. The metadata file was not restored or edited by the investigator; timestamps and hash evidence were retained on the Mac. Its preservation guard reported all 23 prior checkouts, all 524 art/quality/save-source inputs, and 161 of 162 original-root file hashes unchanged. Both owned simulators were shut down; the pre-existing user simulator remained booted.

Nine pre-existing dirty files were preserved by hash; Unity-generated global render settings and QualitySettings changes were restored where identified. Local preservation records are under `Artifacts/ReleaseShop20261023/` and `Artifacts/ShopMerge20261023/`. The repository already had unrelated local changes. No commit, merge or deployment was performed for this investigation or this handoff.
