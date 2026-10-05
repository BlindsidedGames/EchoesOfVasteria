# Assets and town geometry

## What was actually inspected

The current clean Main scene was inventoried through Unity in Edit mode. [scene-geometry.json](scene-geometry.json) preserves the relevant camera, tilemap, farm renderer and woodland coordinates. The complete private inventory is retained in `/Users/matthewrushworth/Projects/Echoes Farming Planning Evidence/2026-09-30/scene-inventory.json`. The crop and soil sprite sheets were viewed directly; prototype renders instantiate their actual imported sprite subassets.

[crop-asset-inventory.json](crop-asset-inventory.json) traces all 17 Farming TaskData assets to their resource drops, actual resource-icon asset/fileID, task icon, prefab and four growth-sprite references. It also records authored adventure-task XP and drop ranges, which are current task data rather than proposed town yields. All 68 growth references and17 icon asset paths resolve; the audit does not claim every art/name pairing is semantically correct. Only the sprites used in the previews and the viewed sheets received direct visual review.

The town camera is at world (-75, 0, -10), rotation (0, 0, 0), orthographic size 18. The town tilemaps use one-world-unit cells. East is increasing X/right; south is decreasing Y/down. The displayed normal-scale north and south views use size 18 at (-67, 0) and (-61, -23); an additional retained overview uses (-61, -11). Wider context uses size 32 at (-70, -10). These views are not an unchanged default framing: the far southern expansion needs a camera pan or a farm focus action.

## Space and boundaries

| Region | Verified geometry | Proposed use |
| --- | --- | --- |
| Existing farmers' fence | Approximate world X -64 to -54, Y -1 to10; phase objects and gate exist | V4 preserves the original frontage/gate, prepares two beds at(−62,1)/(−58,1), and lowers the back fence toY6; preserve windmill/NPC access. |
| Windmill | World (-64.469, 1.827) | Keep its silhouette and approach clear; do not grow a bed underneath it. |
| Old stump field | Oak/fruit stumps around X -65 to -49, Y -5.6 to -16.5 | Reclaim a selected interior strip for two additional beds. These are already stumps, not trees cut by this proposal. |
| Southeast standing woodland | Eastern Working Woodland contains real standing oaks/birches around X -65 to -48, Y -25 to -35 | Clear20of28 local southeast standing renderers for A; retain the surrounding boundary and forest mass. This is substantial local clearing. |
| Barkley's area | Tree and staged house west of the stump field | Preserve its approach and logging identity. |
| Logging trail terminus | Existing path/log piles east of the lower clearing | Keep it legible; new farm track should connect without consuming the logging area. |

The preview manifest records every removed sprite object and position per alternative. Trees are removed only in the duplicate scene. A changes 20 of 28 local southeast standing renderers; the preserved surrounding town-wide woodland does not make this a small local change. B and C each change31 in a wider area. Clearance must include canopy bounds and fence/path approaches, not just a tree's pivot; these concepts need final collision/path review before production.

## Reusable art with evidence

| Art or content | Verified source/use | Reuse recommendation |
| --- | --- | --- |
| Crop growth sheets | `Assets/Art/Packs/Cute_Fantasy/Crops/Crops.png` and Crops_2.png; imported named sprite subassets; FarmingTask prefab growth arrays | Reuse matching crop stages, keeping recipe-to-art mapping explicit. |
| Seed packets and marker signs | Crop Tile assets resolve actual sprite fileIDs into Crops.png; packet/sign artwork visible on sheet | Use as pack icons/signs after resource/art audit. Art alone is not a gameplay seed item. |
| Soil and wet soil | FarmLand_Tile.png/FarmLand_Wet_Tile.png; existing farming prefab renderers | Use coherent beds and occasional watered presentation, without imposing a watering chore. |
| Wooden fences/gate | Fence_Big.png named rail sprites and actual farmers' Phase1/2/3 objects; gate animation asset | Use the verified corner/end/gate assembly guide; do not tile straight rails through corners or treat empty gaps as gate art. |
| Dirt path tiles | Active thin brown town path uses Farmland_WetOverlay join/corner variants; alternate town path uses GrassDirtJagged | Revision2 edits the duplicate active path using audited cardinal/diagonal masks, removing the mismatched bright-green dirt fill overlay. Production must match whichever of the seven path presentations is active. |
| Scarecrow, hay, benches | Farm decor prefabs resolve existing sheet sprites; scarecrow already in the fence group | Optional modest dressing, not new currencies or mechanics. |
| Barkley | Barkley_Lumberjack.png and actual meeting prefab/quest folder | Build quest presenter; verify actual animation frames before any work animation promise. |
| Flora/Tillman | Actual farmers' scene renderers and their sprite/animation assets | Farm guides and screen entry. |
| Logs/sticks/stumps | Existing resources, tree/stump prefabs and scene objects | Existing material costs and staged clearing presentation. |

No Seed or Plank Resource asset was found. Construction skill and animal husbandry are not established by decorative filenames. Animal enemy/decor assets exist elsewhere, but animals remain outside this update.

## Art mapping issue to keep visible

Main's current `Farming_Radish/Crop` uses `Crop_Eggplant_EarlySprout`, and its sign also depicts eggplant. `Farming_Pepper` uses beet art; Lettuce uses cabbage. These are verified imported sprite names and rendered artwork, not assumed equivalences. Some substitutions may be intentional stylization, but recipe art must be approved explicitly. Do not rename serialized crop resources or silently replace unrelated adventure prefabs while planning.

The actual `Farming_Cucumber.prefab` growth array is YoungPlant → DevelopedPlant → MaturePlant → EarlySprout. FarmingTask uses the array in order, so blind reuse would regress the final visual stage. A town recipe must explicitly order the verified sprite references; an adventure-prefab correction would be a separate reviewed visual fix. Turnip currently maps to Radish art, while the Radish task maps to Eggplant. The resource icons come from the inspected Food_Icons_NO_Outline sheet and retain their authored indices in the inventory; individual semantic icon approvals remain part of content review.

Corn, Carrot, Tomato and Pumpkin stages used in the layouts were visually confirmed on the real sheet and rendered from the imported assets. Pumpkin is illustrative art for the existing `Pumking` key; spellings stay intact in persistent IDs. The scene examples do not imply those high-level crops are available at tutorial progression.

## Remaining production art work

Approve resource-to-art mappings for all 17 crops; build a small atlas/contact sheet from actual imported subassets; confirm packet visibility at high DPI and small UI sizes; validate the selected soil/path variants for every active path presentation; test fence occlusion and tree canopy clearances. Revision2 uses actual DryTanBorder soil perimeter/corner variants and matching WetOverlay path joins. Gate tile connectivity and open fence apertures pass preview checks; gameplay navigation, collisions, all seven path presentations and animation remain production validation.

## Revision2 terrain evidence

The original sheet pixels were viewed directly. FarmLand_Tile has tan-edged brown soil variants; FarmLand_Wet_Tile is the current town track's darker transparent overlay. Existing Main path-cell inspection identifies those actual Tile names, rather than inferring use from filenames. Revision2 selects all cardinal joins and diagonal absent-corner masks from these audited assets, clears only preview enclosure dressing, and uses true south/north fence openings. The compact field's beds move one cell south for mature-crop entrance clearance. The raw path audit and per-scene gate proofs are retained in the private revision2 evidence directory and linked from visuals.md.

## Revision3 fence assembly evidence

Fence_Big.png has16 imported named sprites, while horizontal gate art has10 frames and vertical gate art5. The atlas and Main placements establish the needed roles; [fence assembly guide](revision-3/fence-assembly.md) records exact asset GUID/localID, spacing, corner variants and frame-specific gate offsets. Names correctly describe E/W rail arms but do not by themselves encode vertical post continuation or a safe assembled corner. Add an assembly recipe supplement rather than renaming already referenced assets.

Revision3 changes14 preview enclosures across six conceptual built/work scenes:56 explicit corner pieces and14 actual open gate assemblies. Nonintersecting rectangles need no T/X junction. Gate end posts are integral, so adjacent rails join without extra duplicate posts. Stage0 remains an unchanged authored reference. Final enlarged views and normal-camera images are visually reviewed; counts are supporting evidence rather than the acceptance result.

## V4 original anchor and dual approaches

The actual Main front gate is centered(−61,−1), with adjacent approach columnsX−62/−61 atY−3/−2. V4 keeps those cells/transform geometry and the farmers/windmill poses. Field2 joins the lane by retouching only the existing endpoint(−61,−3) from WetOverlay NW to NEW cardinal joins; the front approach is not extended north. First-field north/back fence movesY10→6, its sprite topY6.5. The actual nearest mountain artwork is the legacy BG CliffFoot shadow beginningY10, front faceY11, yielding3.5/4.5 world-unit canvas clearance. This is measured visual clearance, not tested collision clearance. [Reference proof](revision-4/layout-reference.json) retains72 cliff cells, poses, original/preview path cells and the exact join change.

Field2 north gate(−58,−4) has adjacent columnsX−59/−58 joining its local lane. A southeast gate(−58,−23) usesX−59/−58; B/C gate(−57,−23) usesX−58/−57. All connect to the real logging lane at(−60,−22). Four corner pieces, integral gate ends and vertical fence runs remain in each enclosure. First-front rails sort behind the original farmers and use inward-opening north-swing artwork; the windmill forms the lower west boundary, matching authored Main. The catalogue assembly supplement remains sufficient; no GUID/name/reslicing change was needed.

[Seed discovery/art](seed-discovery-and-art.md) verifies22 same-silhouette packets, the shared core-unknown convention, and the shared unknown convention. The authorized [asset preparation](seed-icon-preparation.md) fills that gap and verifies normalized inventory/drop art. [Optional orchard](optional-orchard.md) audits existing world tree stages and fruit objects separately from current scope.
