# Screenshot Studio

Open **Tools → Screenshots → Studio** in Unity. This tool captures the real game and its current UI; it does not resize a screenshot or rebuild the interface as artwork. Video is outside its scope.

## Create and reuse a scenario

1. Choose **New scenario from a copied save slot** and select a slot folder containing `snapshot.bin`. The tool copies it before loading. Alternatively, in a running game in the Editor, choose **New scenario from current progress**. This serializes a copy of the loaded data without asking the live game to save.
2. Choose an empty scenario folder. The default is under `Artifacts/ScreenshotStudio/scenarios`, which is gitignored. The folder contains a hashed `progress.bin` and an editable `scenario.json`.
3. Set a name, map (empty means town), window, map seed, warm-up frame count and colour theme. Use a save with the relevant map and lessons already unlocked. The tool does not grant equipment, resources or unlocks.
4. Select **Load / reload isolated scenario**. The tool loads the actual Loading/Main scenes, prepares the saved progress in a new private directory and stages the map through `GameManager.BeginAdventure`. It waits for generation, advances the recipe's frames at 60 fps, then freezes progression and movement.
5. Compose the screen using the real menus. Select the intended item/tab and scroll position, then **Save current composition / scroll positions**. The checkpoint records sprite poses, their ancestor transforms, selected buttons and scroll positions.
6. Select a device preset and **Apply viewport**, or capture directly. Adventure shots can use compact or expanded portrait. Menu shots stay in the normal layout; expanding closes menus in the real game, so the tool rejects that combination.
7. Use **Capture selected** or **Capture all store sizes**. Each batch gets its own timestamped output folder with PNGs, `manifest.json` and a clickable `index.html` review sheet.
8. End the private session. Previously open saved scenes, viewport and colour preference are restored. Loading another scenario from the Studio can perform this restart automatically.

**Create the six-shot template set** duplicates the selected progress snapshot into recipes for River adventure, Forge, Fields, Cauldron, Inventory and the later Haloween map. These are starting points: choose appropriate progress, frames and composition once for each. The later-map recipe requires that map to be unlocked. Both portrait views are captured automatically for adventure recipes when the batch option is enabled.

If new code or content changes the generated object hierarchy, a saved composition may no longer resolve. The tool reports the missing object rather than silently placing a different one. Clear the saved composition, reload, re-stage and save it again.

## Capture presets

Specifications checked on 23 October 2026. Recheck the linked platform documentation before a later release.

| Preset | Pixels | Purpose |
| --- | --- | --- |
| Steam landscape | 2560 × 1440 | 16:9, above Steam's 1920 × 1080 minimum |
| Google phone | 1080 × 1920 | 9:16 portrait |
| Google tablet | 1920 × 1080 | 16:9 landscape for the 7-inch and 10-inch listing slots |
| iPhone medium | 1206 × 2622 | Apple's Dynamic Island medium / 6.3-inch size |
| iPhone large | 1320 × 2868 | Apple's Dynamic Island large / 6.9-inch size |
| iPad landscape | 2752 × 2064 | Accepted 13-inch iPad landscape size |

Sources: [Apple screenshot specifications](https://developer.apple.com/help/app-store-connect/reference/app-information/screenshot-specifications/), [Google Play preview assets](https://support.google.com/googleplay/android-developer/answer/9866151?hl=en), [Steam graphical assets](https://partner.steamgames.com/doc/store/assets).

Google's large-screen guidance supports 16:9 landscape, dimensions between 1080 and 7680 pixels, and at least four screenshots demonstrating the tablet experience. Reuse the composition across both tablet slots when the interface is the same; do not stretch phone captures. iPad uses its own aspect ratio and therefore needs a separate render. Apple screenshots must not contain transparency.

The editor renders the responsive interface at the preset size. It is not a native-device simulator: platform-specific integrations and safe-area behavior still need device verification before publishing. These files are raw captures, not automatically uploaded store assets.

## Repeatability and boundaries

A scenario stores progression plus a seeded run recipe and a visual checkpoint. It is not a general-purpose serializer for every coroutine, physics contact or enemy AI state. Starting the recipe rebuilds the scene through the normal game; it does not resume a whole Unity process snapshot. Screenshot sessions use an isolated task-placement random stream and synchronous generation so frame-budget timing cannot change spawn ordering. Normal gameplay retains its existing RNG and asynchronous generation. Fixed warm-up frames and restored transforms make the composition reusable, but changes to generation or asset hierarchies can require re-staging. Do not promise bit-for-bit reproduction across game or Unity upgrades.

Normal simulation time, actor movement, animation, farming growth and buff autocasting pause during capture. UI layout and the camera remain live so each size uses the normal responsive layout and map boundaries. One-frame and 30-frame controls let you adjust the staged moment. Save the composition after advancing if the new frame should become the checkpoint.

The exporter checks the requested pixel dimensions and portrait focus state. Blocking dialogs and tutorials prevent export. The returning-player choice is dismissed through its ordinary **Skip lessons** button in the private session; other introduction prompts must be completed or dismissed normally. A forced compact state cannot be exported under an expanded filename.

The manifest records the scenario, commit, Unity version, progress hash, seed, warm-up frames, theme, language, each target size, viewport and PNG hash. Keep the raw images and manifest alongside any later marketing treatments.

## Isolation and cleanup

- Private save roots are created under `Library/ScreenshotStudio/<unique-id>`. Original snapshots are read or copied, never overwritten. All three private slots receive the same copied starting progress, so the editor's selected-slot preference cannot accidentally select a real slot.
- Offline elapsed time is reset on the private starting copy. The original progress payload remains unchanged and is verified by hash on each load.
- Capture sessions suppress Steam and online-service initialization. Shop ownership and promotion-cache paths use the private root; promotion network fetches and store startup are disabled in this session.
- Source map assets temporarily use the recipe seed. Original seed settings are retained in Editor session state and restored after generation or on exit.
- Private saves and outputs are retained for inspection. They may contain copied player progress or names; review before sharing or committing them.
- Save scene edits before launching the sandbox. The tool refuses a dirty scene rather than discarding it. Do not modify source code while a capture batch is running; a domain reload interrupts its in-memory capture queue.

## Command-line capture

Set `EOV_SCREENSHOT_SCENARIO` to a prepared scenario folder and run Unity with:

```text
-batchmode -projectPath <project> -executeMethod TimelessEchoes.Editor.Capture.ScreenshotBatch.Run -logFile <log>
```

Do not add `-quit`: the batch command enters Play mode, captures, restores the editor state and exits itself. It has a four-minute bound and returns a nonzero exit on failure. `Artifacts/ScreenshotStudio/captures/last-batch.txt` points to the most recent completed batch. Use the same Unity version and source content when comparing repeated captures.

## Validation and known layout finding

On 23 October 2026, the health-width regression failed with the original auto-sized HUD (about 77 logical pixels locked versus 161 unlocked) and passed after reserving the five-slot width. The final selected Play Mode group passed both this check and the existing right-click dismissal regression in landscape/portrait. The Editor RNG test also passed, checking ordinary play uses Unity's original stream while capture placement ignores unrelated draws.

The real-scene capture exercise exported seven Forge sizes and eleven adventure view/size combinations. A separate exercise recorded a checkpoint, moved and restored the hero, stopped Play mode, reloaded the same scenario and captured the eleven adventure views again. Both private sessions had UGS uninitialized and Steam integration off. The local evidence and review page are in `Artifacts/ScreenshotStudio/`; these are validation compositions, not approved marketing images.

iPad captures use the 13-inch landscape preset (2752 × 2064). The Forge keeps its landscape columns at this size; portrait still uses its existing pages. The world fills the iPad display instead of letterboxing. This crops the visible width while preserving the full 16:9 run activity area used for Echo task eligibility. The existing maximum width of 32:9 is unchanged. Narrow desktop windows use the same crop behavior. Menus and HUD controls use the full safe height instead of inheriting a 16:9 letterbox inset; their control sizes and spacing stay unchanged.

A composition checkpoint preserves visual poses, selected controls and scrolling, while `progress.bin` remains the starting progression snapshot. If you change progression while staging (for example planting crops or equipping different gear), create a new progress snapshot from that staged session before using it as a new baseline. Transient results such as an uncommitted Forge item are not a general-purpose saved game state.
