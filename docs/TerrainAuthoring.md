# Hometown terrain authoring

## Preserve the native setup

Use existing pack tiles at 16 PPU, Point filtering and unit scale. Inspect a working original junction before changing terrain geometry. Do not substitute straight edges for corner tiles or hide a broken join with decoration.

The Hometown layers have different jobs:

| Tilemap | Sorting | Purpose |
| --- | --- | --- |
| BG -5 | Background, -5, Chunk | Opaque ground, river and cliff faces |
| BG_Walkable -4 | Background, -4, Chunk | Transparent borders, corners and cliff shadows |
| BG_Decor 0 | Background, 0, Individual | Original props |

Sprite props must overlap according to their visible ground contact, so a tree farther down the screen draws in front of one farther up. Water-rock sprites contain blue pixels and must sit within water. Keep seating foot space clear and join fence segments continuously.

## Cliff palette and corners

The sheets are `Assets/Art/Packs/Cute_Fantasy/Tiles/Cliff/Stone_Cliff_{1,3}_Tile.png`; individual tile assets live under `Assets/Tilemaps/Tiles`.

- Set 1 supplies the green plateau top and borders; Set 3 supplies olive terrain transitions.
- Transparent green borders need the correct olive backing outside the plateau. Green backing leaks as a strip outside the ridge.
- South-facing cliffs use a grass lip (12/13/14, concave 6/7), face (21/22/23), foot (28/29/30), and shadow (35/36/37).
- Side borders use 3 and 5; the north rim uses 0/1/2.
- Upward concave junctions use 15 and 16. Tile 15 has the upper-right missing quadrant; 16 has the upper-left missing quadrant. A left-facing wall rising from a horizontal rim uses 16; the opposite turn uses 15. Inspect the actual sprite orientation rather than extrapolating from the straight edge.
- Green plateau tops and faces use Set 1, while feet/shadows over olive ground use Set 3.
- At mixed elevations, retain the appropriate olive cliff face or rim on BG -5 behind the transparent green overlay. Plain grass backing is insufficient where the lower cliff continues behind it.

Working original local-cell examples: (-49,-14), (-6,-13), and (24,11). Recent repaired junctions include Set 1 tile 16 at (14,25) and (18,33), Set 3 tile 15 at (-49,-26), and Set 3 tile 16 at (10,-47).

The viewable expanded map is 128 by 108 cells, local x [-64,64), y [-54,54), with world offset (-75,0). Keep the river extending two cells beyond the view: north rows 54/55 and south rows -55/-56. Do not crop that overscan to the camera bounds.

## River rule system

The river on BG -5 uses the GrassWater BetterRuleTile subasset in `Assets/Tilemaps/MapPalettes/Better Rule Tile.asset`. It has 61 ordered, fixed-orientation rules. The first matching rule wins; having any matching rule does not establish that the chosen shape is correct.

For this tile, neighbour 0 means the same tile, -3 means not the same tile, and -1 is ignored. Outputs use eight-frame animation families from Water_Stone_Tile_3_Anim. Inspect all relevant neighbours and the complete animation family when correcting a rule.

Update the authoring data in the BetterRuleTile container and regenerate through its normal generator. Editing only a generated subasset risks losing the correction on regeneration. This is a shared asset: inspect other usages and compare generated changes before applying a global fix.

## River investigation, 29 September 2026

The initial investigation was read-only. All 859 audited live water cells matched a rule; none used the default fallback. The user subsequently approved the repair described below.

Zero-based rule 54 selects the inner NE corner family beginning at Water_Stone_Tile_3_Anim_88 for seven cells whose shape instead needs the outer NE bank corner family beginning at Water_Stone_Tile_3_Anim_2. It constrains north, east and northeast to non-water and south/southeast to water, while ignoring the west-side neighbours. Affected local cells: (-14,-37), (-17,-35), (-20,-33), (-21,-32), (-22,-31), (-23,-30), and (-49,42).

A separate northern one-cell water spur at (-61,40) leaves two tight turns that the selected corner pieces do not join correctly. The proposed contour repair is to replace that water cell with the appropriate surrounding ground.

An offline simulation changing rule 54's output family from 88 to 2 and removing the spur reduced detected large water-to-water edge discontinuities from 15 to zero. This compares blue-water masks at shared edges of first animation frames, with a threshold of six disagreeing pixels out of sixteen. It is useful evidence, not a complete visual or animation test.

Before implementation: inspect the corresponding authoring rule and shared-map impact, then obtain the user's agreement to the proposal. After implementation: verify all eight animation frames, both extensions, the original central river, the two-row overscan and any other affected maps. Capture close-ups at matching framing and check ground/water boundaries as well as water/water joins. Preserve the original central terrain except for expressly agreed corrections.

Read-only investigation scripts and raw exports currently live in Temp: AuditRiverRules.cs, water-rules-readable.json, river-rule-audit.json, audit_water_seams.py, river-seam-audit.json and simulate_river_fix.py. Temp files are disposable; the findings and limitations above are the durable record.

## Applied repair and verification

The approved repair changed authoring-grid cell (33,42) from sprite 88 to sprite 2. Universal sprite settings supply the existing eight-frame outer-corner animation (2,5,8,11,14,17,20,23). Normal generation changed only GrassWater rule 54; the other generated rule tiles were identical in memory. Unrelated serializer migration was excluded from the persisted asset, leaving nine sprite-reference changes: one authoring reference and eight generated frames. The northern water cell (-61,40) was replaced with Grass_3_Middle.

The shared container is referenced by Main.unity and the Beach palette prefab; no other scene/prefab references were found. After saving and reopening Main, all 858 remaining water cells matched rules. All eight animation frames passed the large shared-edge discontinuity check with zero findings. Original central river sprite selections and the two-row north/south overscan were preserved. Native scene captures of both repaired areas were visually inspected. This pixel-mask check is scoped to large water-to-water edge mismatches; it is not a general terrain validator.

Evidence: [full scene capture](../output/scene-review/river-repaired.png) and [validation results](../output/scene-review/river-validation.json). Future regeneration must retain the source-cell correction; do not restore sprite 88 at authoring cell (33,42).

## Eastern working woodland

The redesigned woodland is grouped under `Hometown/Eastern Working Woodland`. Native 16 PPU sprites remain at unit scale with Point filtering. Tree and standing-prop order follows visible ground-contact Y; low undergrowth sits on Background. Preserve the separate original logging/task objects near Barkley.

The eastern lower woodland replaces scattered decorative tree tiles and the former Expanded Landscape dressing. Preserve the spruce/birch tiles on the green plateau above the cliff, including their original tile data. Terrain and cliff continuation uses two extra columns, x=64/65, and the southeast terrain continues to y=-55/-56. Those are overscan, not future camera bounds. Keep camera bounds inside the original landscape rectangle.

Grouped canopies frame a broad logging edge, timber stacks and open routes to a fallen-log clearing. Before changing terrain, inspect both BG -5 and BG_Walkable -4: the green plateau and olive lowland use different layered cliff pieces. Do not place forest trunks over the cliff face. Compare native-resolution scene captures after reloading saved changes; do not rely only on tile presence or sprite-reference checks.

Review captures: `output/scene-review/forest-before.png` and `forest-after.png` show this pass at matching camera framing.

## Upper mountain base terrain

The cleared plateau now has a second shelf on the northeast, a brown/purple hollow to the northwest and open green ground between them. The original mountain perimeter and mine remain in place. Shelf left boundaries step through local x=38,43,49; south-facing lips are at y=44,35,27, with full face, foot and shadow rows below. The native three-column staircase at x=53..55 crosses rows 24..27.

`Mountain Ground Transitions -3` is a Background tilemap ordered above the cliff-border layer. It contains native transparent Grass_Tiles_1 borders over opaque autumn ground, Autumn_TileSet brown/purple transitions, and Christmass_Grass snow. The snow is the pack's cyan palette. Keep these surface transitions away from cliff faces; do not use them to cover a broken join. Ground contours select the authored convex and concave pieces from their four corner occupancy, avoiding diagonal-only connections.

Mountain ground continues through north rows 54/55 and east columns 64/65 for camera overscan. Keep future bounds inside that margin. Cleared props include original BG_Decor tiles and the plateau portion of Northern Woodland, plus the isolated decorative Bombschroom. The lower redesigned oak woodland is unchanged. This is a base terrain pass; future props and progression zones should respect the shelf and the open middle approach.

Matched captures: `output/scene-review/mountain-before.png` and `mountain-after.png`. The surface audit reports no missing sprites or incorrect 16 PPU/Point settings; joins were also checked in a magnified native scene capture.

### Mountain revision: stream, corruption and tall snow shelf

The former isolated purple hollow is now a narrow winding channel entering from the north and opening into a pool around local (29,23) on the lower green plateau. Brown terrain follows its banks with uneven spreading lobes and surviving green pockets. Purple currently uses the native static Autumn_TileSet terrain; this pass establishes its shape, not flowing-water animation.

The upper shelf retains its lip positions but now has four body rows, a foot row and a shadow row (seven rows including the lip). At each inward wall join, opaque rock continues behind the transparent side rim. The staircase extends down through y=21. Snow uses an irregular continuous upper field, exposed ground pockets and detached lower patches. Surface contours are checked against cliff cells so snow and corruption do not cover the wall faces.

### Corrected elevation placement

The purple stream and brown banks now lie on the light-green western ground, with the pool centred near local (0,32). Use Grass_Tiles_3 for its grass borders, not the dark plateau's Grass_Tiles_1. The former purple location on the lower dark-green plateau now has 18 small snow patches assembled from the snow sheet's authored round corners and edges. The raised northeast shelf is continuous snow, including native raised snow banks (3/4/5,11) and snow-covered stair halves (6/7,12/13). Its wall now has TWO rock-body rows, followed by foot and shadow; the previous four-row height is superseded. The staircase is two cells wide. The upper western ground has two-row overscan for the relocated stream.

### Snow stone rim correction

Brown-edged Christmass_Grass bank overlays are removed from the stone ridge. `Mountain Snow Stone Rims -3` uses the original Stone_Cliff_1 geometry with `Assets/Materials/SnowCliffPalette.mat` and `Assets/Shaders/SnowCliffPalette.shader`. The shader maps only the three source grass colours into the snow sheet palette, preserving stone colours, silhouettes, alpha and native pixels. Foot/shadow tiles remain on the green lower ground. Stair crest geometry matches the underlying two-column stone stairs.

Snow sheet corners 0/2/14/16 are concave hole corners, not convex patch corners. The detached islands use 22/23/28/29 with the corresponding side edges. Do not exchange those groups based on the corner name alone. The native cliff rim uses the original cliff corner sprites; inspect a magnified render at each join.

Christmas Decor.png contains a snow-covered decorated spruce and a green decorated variant; both have stars/ornaments. No undecorated snow-covered tree set was found in the installed pack images.

### Pre-dressing shape pass

The summit outline now has a shallow bay followed by a projecting ledge: left edges x=38 above y=44, x=44 through y=39, x=40 through y=32, and x=49 through y=24. South faces remain two body rows; lips are y=44,31,23. The north-facing return at y=38 uses 0/1/16, with matching snow palette variants, and proper ground behind its alpha. Summit stairs are x=54..55, y=19..23. A lower stair connection east of the mine is x=20..22, y=10..14, using Set 1 at the green cap and Set 3 at the olive foot.

Lower snow is grouped into uneven drifts with open space downhill. The western purple pool now has unequal lobes, a narrow stream inlet and a southeast grassy tongue. Shore contours use the existing authored edge/corner mask mappings. The main blue river, southern woodland and orchard terrain are not part of this pass.

### Mine backing and runtime terrain review

The olive shelf above the forge backs three mine entrances visible in Play mode. Preserve its continuous south-facing rock wall and enough ground above all three entrances. Do not recess it using an edit-mode-only preview: this hides the runtime mine layout and can leave entrances opening into unsupported ground. The stepped overlook experiment was reverted for that reason. Validate future changes here in Play mode with the mine entrances and tracks visible.

Inspect every tilemap in world coordinates. Original shelf shadows also occur on -2 Tracks2Grass (local x -43 through -41, y 15), whose transform is offset from Hometown; checking only BG and BG_Walkable misses them. Those shadows remain necessary with the original supporting wall.

The accepted-direction revision keeps the mine lip at y18 intact and moves only the rear outline: north rim y23 turns upward at x2 to y25, then at x7 to y28. Set 3 tile16 handles each inward upturn; tile0 caps each outward corner. The Set 1 plateau junction is y24, x10..14, with olive backing beneath the shared upper rim. Verified in Play mode with all mine entrances visible.


### Staggered cliff joins
The larger woodland-facing ridge now has short lower faces and an upper rise stepping at x32/41/56. Keep opaque ground beneath transparent side rims: putting a shadow tile on BG at the summit join (49,27) exposed black through both layers. That cell now has Grass_1_Middle backing. The shortened adjoining summit face uses foot y30 and shadow y29; obsolete shadows at y27 were removed. Recheck joins in Play mode after reopening the saved scene.


### Cumulative elevations and opacity
Snow to the lowest mountain ground is a four-tile drop; snow to the intermediate terrace and terrace to low ground are each two tiles. Where the intermediate ledge ends, use the combined height. The upper snowy edge now projects to x46 while the lower terrace edge remains x49. Mountain Terrain Foundation -6 supplies opaque native grass only beneath background cells whose sprites contain transparency, including rock-foot and shadow backing cells. Transparent shadow sprites cannot themselves serve as the bottom opaque layer. Validate mixed-height joins with the scene reopened in Play mode.

