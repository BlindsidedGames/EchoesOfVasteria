# Animated environment decorations

The prepared library contains 94 animated water and bank variants. Prefabs are under `Assets/Prefabs/Environment/AnimatedDecor`, grouped into Water/Plants, Water/Rocks, Water/Timber and Bank/Grass, Flowers, Mushrooms and Rocks.

## Placement and playback

Drag a prefab into the scene for freely positioned decoration. Its `AnimatedDecor` component reads an existing AnimatedTile's frame array; it does not require an Animator or duplicate animation clip. Playback defaults to 4 FPS with 0.85–1.15 speed variation and a randomized starting phase. Disable randomization and set the speed range to (1,1) for deterministic capture. Disabling/re-enabling the object restarts playback. Missing/empty sources leave the renderer's sprite unchanged.

The renderer starts with frame zero, unit scale, the existing scene sprite material and Default sorting layer/order 0. **Set its sorting order for the placement's visible ground contact.** The component only changes the sprite; it never changes position, material, sorting or scale. Do not add it to an object whose Animator also controls the sprite.

Keep all source sprites at 16 PPU and Point filtering. Position on the 1/16-world-unit pixel grid. Inspect the full animation footprint: water-rock, reed and log ripples must remain within water throughout the cycle. Keep trees, fences and seating foot space correctly layered.

Use the AnimatedTile itself for tilemap painting. Tilemap playback uses the tile's speed range and the tilemap's animation rate, independently of the prefab's FPS. The 27 reused tiles retain their original timing. The 67 new tiles have speed 3–5, start time/frame zero and no collider. Do not change the original decor tilemap's rate simply to tune a new prop.

Dry-rock animation is available for review, but static rock bodies remain the default decor proposal. The Desert fern has no animation source and remains static. The initial setup task did not change placement; the approved river pass below subsequently integrated the water assets.

## Canonical assets

Nine duplicate water-plant PNGs were byte-identical and had no project references. Their unused 100-PPU/Bilinear imports were removed. The retained 16-PPU/Point copies moved from Water_Plants/Animated into Water_Plants while preserving GUIDs and sprite file IDs. Do not recreate the duplicate Animated folder.

Existing CatTail, WaterGrass and Lillypad tile assets were moved, preserving GUIDs, into `Assets/Tilemaps/AnimatedTiles/Water/Plants` and renamed to match their source variants. For example, the old `CatTail 1` is now `Cattail_2_Anim`; the old `Lillypad 1` is now `Lillypad_Green_1_Anim`. New tiles are under `Assets/Tilemaps/AnimatedTiles/Decor`. Existing grass tiles and legacy rock clips remain at their original paths for existing consumers.

## Verification

All 94 prefabs have resolved frames, matching first sprites, valid materials, unit scale and 16-PPU/Point imports. A separate playback scene verified that every prefab advanced and stayed within its source frame set over an 8.28-second sample interval. The temporary scene was removed, Main reopened in clean edit mode, and the original Loading play-mode start scene restored. The 94 source sheets have unique file hashes; no serialized references to the nine removed GUIDs remain.

[Browse the asset gallery](../output/scene-review/decor-proposal/index.html), [asset manifest](../output/scene-review/decor-proposal/manifest.json), and [setup validation](../output/scene-review/decor-proposal/setup-validation.json).

## Integrated river pass

83 original central-area reeds, lily pads and water rocks now use AnimatedDecor with their matching animated source. Their existing GameObjects and hierarchy are retained. Where necessary, footprints were shifted slightly inward to keep every ripple frame over water. Existing animated boat, fish and waterfall systems were preserved.

48 repetitive extension props were replaced by 57 animated placements under Hometown/Animated River Extensions/North River and South River. The pass uses all five cattail silhouettes, low water grass, small lily groups, sparse rocks and occasional floating timber. Opposite banks have different cluster sizes and gaps.

All 140 animated water props passed a pixel-footprint check: every blue ripple pixel in every prop frame lies over a water pixel common to all animation frames of its underlying river rule. Native captures of both complete extensions were visually inspected, including frames 0, 3 and 7. The eight-frame preview sequences capture the actual scene with decor phases stepped for review; they are not real-time recordings. All terrain tilemap records remained unchanged. The saved scene was reopened and checked for missing sources and leftover static water variants (zero of either).

[North animation preview](../output/scene-review/river-north-animated.gif), [south animation preview](../output/scene-review/river-south-animated.gif), and [validation evidence](../output/scene-review/river-decor-validation.json).

## Mountain rest camp
The mountain dressing now includes a five-frame Campfire_Pot_Anim AnimatedTile and reusable prefab under AnimatedDecor/Camp, using the same AnimatedDecor playback component. The placed fire is depth-sorted at its ground contact. Volcano_Rocks, Volcano_Plants and Mushrooms_Brown imports are normalized to 16 PPU, Point, uncompressed RGBA, no mipmaps. The fuller mountain pass contains 365 decorative sprites, including 90 animated props. Terrain records were unchanged by this dressing pass.

