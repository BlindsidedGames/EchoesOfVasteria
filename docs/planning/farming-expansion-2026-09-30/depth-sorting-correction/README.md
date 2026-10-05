# Correct depth, retained trees and a full one-tile orchard shift

Both existing young trees stay at their original positions. A canopy may cross a fence. The earlier two-tree removal and “failed full-tile fit” conclusions were incorrect; this revision replaces them. Stage2 still reclaims its 23 stump renderers. A's southeast construction still cuts20 of 28 local standing renderers. Neither count gains extra tree removal from this correction or orchard.

![Both original trees before and after, plus the opposite-depth control](../visuals/farm-fence-scenery-before-after.png)

## Actual cause and preview remedy

`BG_Decor 0` is already an **Individual** TilemapRenderer; Chunk mode was not the cause. It renders on the **Background** sorting layer, order 0. New fence sprites use **Default**, order 3, so the layer forces those tiles behind the fence regardless of their ground positions. Both use the same `Sprite-Lit-Default` material, URP2D shader and Transparent queue 3000. No shader replacement or global layer/queue change is needed.

The two original source tiles are `FruitTree_Growth_YoungTree`, PPU 16, pivot (16,16), cells(13,-13) and(23,-5), world feet(−61.5,−12.5) and(−51.5,−4.5). A named preview-only root restores their exact sprites, positions and scale as **Pivot** SpriteRenderers on Default/order 3. Their original cleared BG slots remain empty to prevent double drawing; this is a different depth representation of the same two trees, not additional tree clearing. Main, tile assets and sprite imports are untouched.

URP2D's existing custom transparency axis is(0,1,0): farther north draws behind farther south when layer/order match. New orchard trees use a SortingGroup at their actual ground anchor, also Default/order 3. Its child art retains the imported pivot offset; apple art does not hop between stages. This avoids sorting mature crowns by their canvas centre or giving every tree a blanket foreground order. Fences, gates and the original windmill/NPC exception retain their existing art, transforms and orders.

Unity's [sorting precedence](https://docs.unity3d.com/6000.0/Documentation/Manual/sprite/sort-sprites/sort-sprites.html), [URP2D axis behavior](https://docs.unity3d.com/6000.0/Documentation/Manual/2d-renderer-sorting.html) and [Individual tile rendering](https://docs.unity3d.com/6000.0/Documentation/Manual/tilemaps/work-with-tilemaps/tilemap-renderer-reference.html) support this diagnosis. A live implementation should put only appropriate tall decoration on an interleaving layer with validated ground sort points; promoting an entire grass/decor map would put ground art over crops. No live renderer migration is implemented here.

## Render proof

- South footY−12.5, fence rowY−11: all 396 opaque source-pixel centre samples match the tree-only control after correction; the old layer hid 159 of them. The opaque canopy is continuous where it crosses the fence.
- North control footY−10.9375, fence rowY−11: all 9 actual trunk/rail overlap samples match the fence-only control. This fixture is deliberately positioned to test ordering and is never saved as gameplay or stage content.
- Three upper-row mature orchard trees atY−13.5: all 2,046 visible opaque source-pixel samples match the fence-hidden control. Their canopy crossing is allowed and their ground remains south of the fence.
- Raw before/after, enlarged fence details, mixed/mature views, normal size18 camera and town context were rendered in Unity on Metal and visually inspected. Offscreen capture explicitly calls SortingGroup.UpdateAllSortingGroups before rendering; an initial capture before that update was retained as diagnostic evidence, not delivered as a finished view.

[South/north pixel proof](depth-pixel-proof.json), [orchard pixel proof](orchard-depth-pixel-proof.json), [material/layer diagnosis](diagnosis.json), [separate ground/path evidence](ground-path-proof.json) and [narrow serialized diff](narrow-serialized-proof.json) preserve concrete checks.

## Separate orchard: six or eight at the requested height

![Full one-tile shift, matched6/8 growth and fence views, normal and context cameras](../visuals/farm-optional-orchard.png)

Rows move fromY−14.5/−18 to**Y−13.5/−17**, exactly one town tile north. No canopy-clearance setback is required. Both counts retain all six crop beds, 60 crop renderers and 150 original path cells.

| Current visual option | Six trees: two rows of3 | Eight trees: two rows of4 |
| --- | --- | --- |
| Column anchors | X−60/−56/−52 | X−60/−57/−54/−51 |
| Spacing | 4 world units | 3 world units |
| Grass aisle between2 × 2 plots | 2 units | 1 unit |
| Soil envelope | 10×5.5 units | 11×5.5 units |
| Vertical row spacing / soil gap | 3.5 /1.5 | 3.5 /1.5 |
| Soil/path intersections | 0 | 0 |
| Soil/retained trunk-and-shadow intersections | 0 | 0 |
| Extra scenery clearing | 0 | 0 |

Columns are rebalanced east to leave the retained south tree clearly outside the plot edges. This is an explicit provisional presentation adjustment, not a claim that the previous column anchors were a navigation defect: previously its root lay on a plot's north boundary and its shadow projected onto soil. Shadows and canopy crossings alone are not blockers. The existing tree's base/shadow visual proxy ends atX−61.0625; current first soil startsX−61, a one-art-pixel separation. That measurement is not a required collision margin.

The current first aisle offers two units inside the six-tree layout and one inside the eight-tree layout. A checked one-unit grass approach atX−59..−58/Y−21..−12.5 reaches the existing logging-path tile centred(−58.5,−21.5) without overlap with audited nearby stump/building/wood-pile art. No new path or gate is invented for this optional area. The static test checks imported alpha bounds, planting rectangles, retained base/shadow pixels and path cells separately; it does not infer colliders from canopies. The preview's sole collider is inherited camera infrastructure, not a tree/fence navigation contract. Runtime walking, interaction reach, collision footprints and harvest focus remain future implementation validation.

Six remains the provisional roomier visual preference. Eight is a viable denser sketch, with narrower access and more future output to model; neither capacity nor orchard economy is approved. Six **crop** beds are unchanged in either case. Orchard stays a separate area, not trees substituted on cropbeds.

## Recoverability and scope

Five affected V4 stages, both four-tree historical row scenes and all current capacity scenes now show the restored trees. Current full-shift scenes are G6/H8-one-tile-up and G6/H8-mixed-up under Assets/Editor/FarmingOrchardCapacityPreview. The older “clear” filenames are historical smaller-shift alternatives, not required clearance remedies. Their earlier evidence remains archived with a superseded notice. No preview enters Build Settings and no farming/orchard script is attached.

The sibling Mac evidence folder `depth-sorting-correction-2026-10-01` retains the prior complete document/image package, preview baselines, intermediate renders, original helpers and full 41 MB scene-art audit. [Verification](verification.json) records preservation and limits; [Library delivery](library-delivery.json) records the same image identities with new versions. Only preview scenes and planning evidence change. Main, player saves/backups, seed icons/atlases and prior cleanup source remain protected. No gameplay implementation, commit or release.
