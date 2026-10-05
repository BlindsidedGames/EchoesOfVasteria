# Approved four timber pairs — verified Mac setup

Four distinct log/stick pairs are now connected to the existing woodcutting resources and tree drops. The approved order is **Starter Brown → Oak GreyBark → Birch Pale → Spruce DarkBrown**. This resolves the earlier colour-order question and supersedes the three-pair proposal. Farming and twin mechanics remain paused; this change adds the explicitly approved timber resources and artwork only. The work is uncommitted and has not been pushed or released.

![Approved inventory art and outlined drop variants, native and enlarged](premium-wood-proposal.png)

| Family | Inventory source log / stick | Resource IDs log / stick | Known TMP glyphs log / stick |
| --- | --- | --- | --- |
| Starter Brown | 905 / 902 | **57 / 2**, original identities | 238 / 236 |
| Oak GreyBark | 935 / 932 | 72 / 71 | 242 / 240 |
| Birch Pale | 925 / 922 | 74 / 73 | 246 / 244 |
| Spruce DarkBrown | 915 / 912 | 76 / 75 | 250 / 248 |

The species assignments are the user's approved mapping of descriptive art families. They are not claims that the publisher labelled these icons as those tree species. Names and accessible text distinguish the families as well as colour.

## Actual inventory and drop rendering

![All eight items in the real inventory component and TMP sprite text](actual-eight-item-inventory-tmp.png)

The arm64/Mono Mac Player rendered all eight inventory sprites through the actual `ToolkitResourceInventoryScreen` prefab and all eight outlined drop glyphs through TextMeshPro using `ResourceIconLookup.SpriteAsset` and `GetIconTag`. Sprite checks passed on **Metal**; the images were visually inspected. The eight-item view uses a disposable in-memory copy of the inventory definition so every icon fits in one screenshot. The [full production inventory view](actual-inventory-tmp.png) separately verifies the real definition with its existing entries and the six additions.

Counts and owned status in these images are fixture data. The fixture keeps Oracle inactive and supplies notation preferences in memory; it does not run Oracle's save lifecycle, create a SaveManager or open a real bank. The first TMP preview checked glyph lookup; the follow-up below verifies actual task completion and pooled rise/fade through the production feedback path.

Inventory artwork retains the source pixels and transparency, including its dark contour and pale cut wood. All eight drop variants add only a one-pixel exterior cream outline (`#F9E6CF`). No interior recolouring or generative restyling was used. The [approved original-inventory mapping view](four-pair-order-check.png) shows the original art separately.

## Actual task completion and adventure feedback

![Actual log drops from both sizes of all four tree families](actual-log-drop-feedback.png)

![Actual stick drops from both sizes of all four tree families](actual-stick-drop-feedback.png)

**16/16 cases pass** in a second isolated arm64/Mono Metal Player. It instantiates the original eight tree prefabs and assigns their authored TaskData, then drives the public `WoodcuttingTask.Tick` completion path: `ContinuousTask.CompleteWork → GenerateDrops → ResourceManager.Add → FloatingText.SpawnResourceText`. Both sizes of Starter/Oak/Birch/Spruce pay their matching Log and Stick, use the expected known glyph and Logging XP glyph 209, swap the referenced tree renderer to a stump and notify completion once. A repeated Tick neither pays nor notifies again.

Deterministic seeds select each desired single-drop case without changing authored weights, extra-drop chances, ranges or requirements. Each payout matches the actual DropResolver quantity. All 16 pooled texts rise approximately 0.358 world units and fade to alpha 0.821 around 350ms, then return to the pool after their default one-second lifetime. Early and later screenshots were visually inspected; [later log view](log-drop-feedback-rise.png) and [later stick view](stick-drop-feedback-rise.png) retain the animation evidence.

The fixture uses completed-work timer placement, a level 1000 skill state with no active milestones/buffs, Tier1 balances and an inactive hero identity. It exercises real task/reward/animation code; it does not test hero navigation, full map generation, adventure balancing or realistic drop-frequency distributions. Oracle stays inactive, SaveManager is never created, FarmService is absent and no real save is opened. Authored quantity/chance preservation is separately verified against the pre-change data and by the original resource-drop tests.

The final run reports no runtime errors or warnings before shutdown. The second build has zero errors and the same two configuration warnings. Raw exit retains five Mono thread-finalization messages and the known ComputeBuffer warning. Earlier fixture failures assumed the renderer was on the root or that child traversal selected the tree; both are retained. The passing fixture inspects the exact renderer reference used by WoodcuttingTask. No production change was made to accommodate it.

## Resource and save behaviour

- **Log ID57**, GUID `e8ace32c9cea7aa4aa202557d1f972ea`, and save key `Log` remain the base log. **Stick ID2**, GUID `024c1162874b64e43be92b8892b40efb`, and save key `Stick` remain the base stick. Only their known-icon references change. Existing balances, tiers and other resource fields remain intact.
- Six new definitions use unused IDs **71–76**, distinct stable GUIDs and names `Oak Stick`, `Oak Log`, `Birch Stick`, `Birch Log`, `Spruce Stick`, `Spruce Log`. Old holdings are not converted or split. An absent new record follows the ordinary zero/unearned path.
- Medium/Large **Tree** still drop base Log/Stick. Medium/Large **Oak**, **Birch** and **Spruce** now reference their own pairs. Each task's original quantities, ranges, probabilities, duration, XP, requirements, prefab and spawn configuration are unchanged.
- The new definitions inherit the corresponding original Log/Stick value settings and use the existing Woodcutting cauldron category. Native inventory registration, discovery, tier and card routing use the existing systems. No new compensation, gear conversion, recipe, quest, chest reward, cauldron weight or balance redesign was added. Existing consumers of base wood retain their original references.
- Unknown inventory sprites reuse the established log/stick unknown art; unknown TMP variants copy those existing pixels. New timber starts undiscovered rather than being granted by migration.

## Independent integration review

A separate read-only review found no resource ID/save-name collision, broken reference or unintended loss of base wood access. There are **75 unique resource definitions**. It confirmed several consequences of distinct materials:

- All eight Barkley quests still require original Log/Stick; species wood is not a substitute. Fence2 requires Stick 1,500 + Log 1,250 and Fence3 requires 5,000 of each. Unchanged base Medium/Large Trees remain available at Logging1/6 in Woods, Beach, River and Farmlands, with positive task weights and no enforced maximum distance. Species harvesting no longer advances those base-only holdings.
- The six new materials currently have no authored recipe, quest or chest-table demand. They use the existing discovery, tier and Alter Echo logging card/generator systems after earning and are excluded from food mixing. Base-tier bonuses do not transfer. No demand or generator redesign was silently added.
- The current completion cohort expands from 69 resources +94 quests to 75 +94. A previously complete current cohort reads 163/169 = **96.45%** until the six discoveries. This follows the existing completion calculation rather than deleting progress.
- The retired UGUI definition omits the additions, but its Main component is disabled and guarded. Active Toolkit inventory registers all six exactly once.

[Independent review and source references](../../development/timber-resources-2026-10-01/independent-reference-review.md)

## Atlas integrity

The existing atlas GUID `2d1ea1fa2e7dca4448ad7af961b8896b`, original sprite fileIDs, spriteIDs, glyph indices and rectangles remain stable. The PNG grows from **288×224 to 288×240**, appending 16 characters at **236–251**: known and unknown variants for each of the eight items. Both TMP `FloatingTextIcons.asset` and TextCore `InlineSprites.asset` contain the additions. Every prior glyph **0–235**, including the prepared seed-pack glyphs, retains exactly the same RGBA pixels and metadata. No destructive repacking was performed.

## Verification and remaining diagnostics

**78/78 targeted EditMode tests pass**, with zero failures or skips: timber 18, resource drops 6, current save codec 25, legacy migration compatibility 26 and completion cohorts 3. Coverage includes the actual tree assets, low/high drop resolution, unique IDs, source references, inventory registration, TMP glyph routing and existing/new/unknown resource save round-trips. The isolated Mac build succeeds with **zero errors and two existing warnings**:

1. `iOS App Info has not been configured. Please add and configure iOS App Info metadata to the Localization Settings in order to correctly support Localization on iOS.`
2. `Pipeline: No RuntimePipelineConfig asset found (Project Settings > Pipeline > Runtime). Pipeline will be disabled in Player builds.`

The passing render records no Error/Exception/Assert diagnostics. Raw shutdown retains 14 Mono thread-finalization messages and `GarbageCollector disposing of ComputeBuffer. Please use ComputeBuffer.Release() or .Dispose() to manually release the buffer.` The latter has already been reproduced in an empty Metal Player and attributed to Unity 2D Animation's static fallback buffer; see the [prior focused attribution](../../development/farming-radish-slice-2026-10-01/review.md). No warning was suppressed or vendor package patched.

The restored Editor compiles with zero C# errors and six distinct pre-existing compiler warning lines: CS0184 in ToolkitNativeCutover, UDR0001 in GatheringBuffValidation, three UDR0004 lines in VasteriaStyleLab and UAC0005 in SaveSystemStressPlayModeTests. Their exact messages are retained in [restored-compilation.json](../../development/timber-resources-2026-10-01/restored-compilation.json). This validates Mac Metal/Mono, actual task reward feedback and the relevant data paths; mobile devices, IL2CPP/AOT, navigation and a full adventure run were not tested by this narrow change.

## Preservation and review

Main remains clean in Edit mode and its bytes match the original hash. The final comparison covers **16,405** pre-existing files under Assets, Packages and ProjectSettings: only the 14 intended existing files changed, and 14 files were added (six definitions with metadata and the focused test with metadata). Every other source file, unrelated user change and staged diff is unchanged. Both temporary UGS guards and ProjectSettings are restored exactly; build-only scene and helper scripts were archived outside Assets and removed.

All save/cloud files are byte-identical and all **93 backups** still verify. Of 89 protected files, 88 remain byte-identical; the preferences plist contains only two added Unity Performance Testing metadata keys, `PT_Run` and `PT_Settings`, written by its Editor/TestRunBuilder. All other preference keys are unchanged. Those test metadata keys were retained; no progression or account data was rewritten.

The first render attempt lacked an in-memory notation preference fixture. Its failure is preserved; the corrected fixture passes without starting Oracle. Earlier proposals and colour-order figures are retained in [prior-version](prior-version/README.md). Existing Library identities are reused for the updated report, outlined comparison and approved four-pair mapping. The all-wood source sheet stays unchanged.

[Focused technical evidence and diff](../../development/timber-resources-2026-10-01/README.md) · [All 43 source wood-region choices](premium-wood-contact-sheet.png) · [Import verification](import-verification.json) · [Publisher import note](../../PremiumIconImport.md)
