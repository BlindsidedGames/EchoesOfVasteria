# Visual parity correction pass — 27 September 2026

The first native cutover passed functional checks but was not acceptable visual parity. The user's allowance for standardization permits small alignment changes, not replacement art, altered border density or redesigned screens. This pass corrects identified regressions against the retained original scene and baseline captures.

## Corrections

| Finding | Evidence | Correction |
| --- | --- | --- |
| Forge used a broad comparison row below crafting, and conversions wrapped 2+1 | Original `101-viewport-1280-OpenForge.png` versus first native `player/1280-Forge.png` | Restore gear/crafting/comparison columns, compact three-wide conversions, framed portrait, Forge Information heading and framed expandable sections |
| Forge selector borders were too heavy | Original selectors have `Image.pixelsPerUnitMultiplier=2`; native resolved slice scale was 1 | Shared Background API accepts the authored multiplier; Forge selector scale is 0.5 |
| Cauldron six-column grid wrapped after five | At 2340×1080: grid width 161.2 reference units, each margin resolved to 1.2; sixth card moved to next row although height assumed six columns | Shared indexed `ToolkitGrid` for Cauldron and resource inventory; geometry checks cover every cell |
| Text appeared thinner than the original | Original Noto Sans ExtraBold material has UNDERLAY_ON, offsets 1/-1, softness 1, dilate 1; shared native text lacked the equivalent treatment | Consolidate the treatment already used by the native book into shared text roles and remaining affected screens |
| Town scenery leaked around the backdrop | Original BG offsets deliberately extend well beyond SafeArea; native BG was limited to SafeArea | Backdrop fills the viewport while interactive controls retain safe-area layout |
| Run HUD merged originally separate frames, used `UI_Frames_4` instead of `UI_Frames_14`, shrank the portrait and moved the death prompt to centre | Original `91-run-hud-settled.png` and `93-run-death.png` | Restore original frame roles, masked portrait, separate buff/stat panels, compact controls and left-side death prompt |
| Prospector target change separator was corrupted | Source literal contained mojibake | Use a Unicode bullet escape |

An asset-only audit initially suggested restoring several `UI_Frames_5` outer frames. Checking `Library/UITKCutoverBackup/Main.unity` showed these Images were disabled before cutover. That attempted change was rejected and removed. `authored-frame-visibility.csv` records this distinction. Serialized references alone are not visual acceptance evidence.

## Capture setup

Main connected Editor, saved native cutover, development fixture loaded into memory from a private copy. Temporary isolation disables Steam/UGS/name/feedback callbacks, uses product `EOVUITKSpacingValidation` and save root `Library/UITKSpacingDisposableSaves`. Captures request 1280×720 and 2340×1080 Game View presets, VSync off, target 30 FPS, time scale 0. Tasting is stopped before opening each window. Intro is dismissed only in the disposable profile. Run captures briefly advance the isolated simulation to let the camera settle, then freeze it again. These are Editor viewport checks, not a mobile build, performance measurement, or approval of every UI state.

Captures wait between opening, capturing and changing resolution. The earlier attempt that resized before the last screenshot completed was rejected and overwritten. The fixture and the old synthetic baseline differ in progression values and selections; compare artwork and composition, not numbers or collection scroll position.

## Validation

- Ten town screens captured at each viewport: Library, Credits, Options, Buffs, Skills, Quests, Cauldron, Alter Echoes, Statistics and Forge.
- `geometry-1280.txt` / `geometry-2340.txt`: native grid bounds, row count and selector slice checks, with zero enabled legacy Canvases and offline Steam/UGS checks.
- `capture-routes.txt` confirms routes opened. This is functional evidence, not itself a visual pass.
- Supporting capture and check scripts are retained as `.cs.txt`, outside Unity compilation.

## Remaining acceptance work

Do not describe the migration as visually approved in full. Physical mobile touch/safe areas, long translations, every statistics sub-tab, every tooltip/modal, fresh and developed UI states, run summaries and the excluded Prospector popup still need a final complete acceptance sweep. Prior Development Player checks predate these visual corrections. These changes do not establish a measured performance gain.

## Cleanup

All eight temporary isolation files were restored byte-for-byte from their pre-validation backups. Original company/product identity was restored. The Editor is left out of Play mode; no test player or temporary runtime harness remains active. See `isolation-restoration.txt`. The private fixture and disposable saves remain only in ignored local storage.
