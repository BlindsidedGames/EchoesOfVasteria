# UI migration capture tools

These `.cs.txt` files are investigation helpers, deliberately outside `Assets` so they do not compile into the game. Copy a helper into the isolated project's `Library` as `.cs`, then invoke its named `Main` entry with `unity command run_script --file ... --entry Class.Main --project-path ... --json`.

The scripts retain the paths used for the initial capture session. Update the output, project and private snapshot paths together before reproducing elsewhere. The developed save is deliberately not included.

## Isolation required

Create a separate project copy from the current workspace, including its uncommitted assets. Use a distinct company/product identity and a save root inside the copy. Before entering Play, disable Steam initialization, UGS initialization/leaderboard upload, profile cloud access and feedback submission in that disposable copy. Verify `SteamManager.Initialized == false`, `UnityServices.State == Uninitialized`, and the actual SaveManager root before loading synthetic data. Do not copy these guards into the shipping project.

The original session used `EOVUITKIsolation/EOVUITKCapture`, a `Library/UITKDisposableSaves` override, and unconditional early returns in SteamManager.Awake, UgsInitializer.EnsureInitializedAsync, UgsLeaderboardsReporter.TryUploadAllAsync, FeedbackForm.Post, UniqueNameService.SetUniqueAsync and LocalProfile.GetMyDisplayNameAsync, plus disabled leaderboard settings.

## Capture sequence

1. `UIBaselineInventory.Main` records Edit-mode scene/prefab/button inventory.
2. Enter Play in the disposable copy with fresh progression. Capture startup separately, before dismissing Welcome.
3. `UIBaselineCapture.Main` queues fresh, copied developed and synthetic all-quest-completed scenarios.
4. `ExpandBaseline.Main` and `CaptureExtraSurfaces.Main` append scenarios. These older helpers require re-registering the stored session's `Tick` callback and setting `index` to the previous result count minus one after an earlier queue has finished.
5. `CaptureBaselineCorrections.Main` captures inventory through Forge, NPC presentation and fresh quest entries. It resumes its queue itself.
6. `CaptureViewportBaseline.Main` captures 1280x720 and 2340x1080 desktop viewports, then restores 3840x2160. It adds temporary Game-view size presets only to the disposable Editor.
7. Inspect every screenshot. A successful capture is not evidence that the intended screen opened. See `coverage.csv` for rejected/limited cases.

NPC captures intentionally instantiate the real conversation prefab over town; they do not claim natural encounter coverage. Some fixtures retain scroll/tooltip state. Viewport tests do not exercise mobile platform branches or device input. Export/import screenshots activate the panels without putting actual save contents into public evidence.

## Native validation and installation

The `ValidateToolkit*`, `ValidateInstalled*`, `ValidateNavigationLayout`, `ValidateRunProgress` and `ValidateLibraryGates` helpers exercise native views only in the disposable project. Navigation map checks temporarily supply quest completions and start/return from all five maps. `ValidateRunProgress` embeds the exact previous MapUI as a temporary reference component for equivalence checks. No reference component is compiled into Assets.

`InstallNative*` helpers are authoring operations for a clean Edit-mode scene, not capture helpers. Snapshot the current scene/prefab immediately before invoking one. Unity can rewrite unrelated tile animation speeds, driven RectTransforms and old serialization while saving; narrow the resulting change against that immediate snapshot, preserving all earlier workspace work. Navigation's legacy reference import should run before deactivating the source toolbar, or against a disposable baseline copy with the source toolbar enabled and laid out.

## Final main-project checks

`ValidateMainNativeUI`, `ValidateNativeLifecycle`, `ValidateMainForgeEdges` and `ValidateNativeFinishing` run against the saved cutover in the main project, using `EOVUITKIsolation/EOVUITKMainValidation`, a private fixture, a Library-only save root and verified offline guards. Their results are separate from the earlier temporary-project checks. Back up exact current bytes before applying temporary guards and restore those bytes after testing; do not reset guarded files to Git HEAD.

`TemporaryUITKPlayerValidation.cs.txt` is a reproduction harness, not a shipping source file. Its product-name/command-line gates are additional safeguards, not a substitute for the save/cloud guards. It exercises a locally built IL2CPP Development Player with `--uitk-check`, captures cameras/native panels into a temporary RenderTexture while the process is hidden, and exits automatically. It is compiled only for validation and then removed from Assets. Generated builds stay outside the repository. Off-screen captures validate the rendered native content, not visible-window presentation or physical touchscreen input.
