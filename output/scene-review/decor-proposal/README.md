# River decor and animation proposal

Initial read-only audit, 29 September 2026. The asset setup and deduplication were subsequently approved and completed; scene placement remains unchanged.

## Setup completed

94 animated water/bank prefabs are now available under `Assets/Prefabs/Environment/AnimatedDecor`, backed by 27 reused and 67 new AnimatedTiles. Nine unused byte-identical water-plant sheets were removed; retained imports moved to the parent Water_Plants folder with GUIDs preserved. Existing water-plant tiles were renamed to match their sprite variants and grouped under Water/Plants.

The shared AnimatedDecor component uses each tile’s existing frame array, randomized phase and 3.4–4.6 FPS playback. No duplicate clip/controller sets were generated. Existing legacy clips remain intact for their current consumers. All 94 prefabs were validated in Play mode; no stale references to removed GUIDs were found. See [validation](setup-validation.json) and [usage guide](../../../docs/AnimatedDecor.md).

The sections below record the original audit and proposal; the gallery and manifest reflect the completed setup.

[Open the animated gallery](index.html) Â· [Static palette](proposed-palette.png) Â· [Exact asset manifest](manifest.json)

## Existing scene

The expansion placement script repeats Outdoor_Decor_122 and 123 every three rows, with later terrain edits removing some instances. These are ordinary SpriteRenderers with no Animator. The central river uses a broader range of static water-decoration silhouettes; its reeds, lilies and water rocks also lack animation playback.

Hometown has no placed CatTail, WaterGrass or Lillypad AnimatedTiles. BG_Decor 0 does use eight animated grass/flower types in the original central area: three grass types and Flower_Grass 3, 4, 9, 12 and 14 (85 cells total). None of these animated tiles are in the expansions. The boat, campfire, fish and waterfall already have assigned Animators.

## Ready assets versus remaining setup

- Five cattail variants, two water grasses and two plain green lilies have eight-frame AnimatedTile assets. The correct sources are in Water_Plants/Animated, at 16 PPU and Point filtering. Selected ready assets have eight resolved sprite references and consistent frame dimensions/pivots.
- All three dry grasses and fifteen flower-grass variants also have AnimatedTile assets. The proposal uses three grasses and four flower-grass variants.
- Ten water-rock variants, four floating timber variants and the additional lily variants have eight-frame source artwork, but no corresponding AnimatedTile or animation clip was found in the project audit.
- Ground-rock clips exist for Rock 1 and Rock 4. The proposed dry rocks 3, 5, 9 and 12 have sheets but no playback assets. I propose these as static structural accents; their pack animation can be reviewed separately rather than making every rock move.
- Mushroom sheets exist; selected variants 1 and 8 have six frames, variant 7 has eight. They need playback setup if animated.
- Nine duplicate parent-folder water-plant sheets have 100 PPU/Bilinear import settings; parent-folder Cattail 1 is also a single imported sprite. Use the already corrected Animated-folder copies. Do not rescale objects to compensate or change unused duplicates unnecessarily.
- Desert Fern is a compatible, correctly imported static accent. No dedicated fern animation was found. Shroomlands props, Military Camp log benches, Dungeon sewer decor and seasonal/volcanic props are comparison options in the gallery, not default river additions.

The 37-item proposed palette contains 16 variants with existing AnimatedTile setup, 20 variants whose animation artwork needs playback wiring if used animated, and one static fern. Four of those 20 are dry rocks proposed to remain static. The gallery also exposes 45 water options and further bank/other-pack alternatives, with exact asset paths and status per item. Preview timing is illustrative, not a captured Unity playback test.

## Proposed implementation

Use all five reed silhouettes in irregular small groups, mixed with low water grass. Leave clear gaps instead of repeating a three-tile cadence. Give opposite banks different compositions. Keep green lilies dominant with restrained pale/pink flowers in calmer pools. Place small rock groups at selected bends and occasional floating timber near the logging area. Blend the dry bank with grass, sparse flowers, shaded mushrooms and a few ferns.

For water props, use reusable animated SpriteRenderer prefabs so fractional positions and existing depth sorting survive. Reuse frame arrays from existing AnimatedTiles; create missing clips/controllers for selected sheets. Reuse and verify AnimatorStartOffset for staggered phases. Ready tile assets still need integration into these prefabs: an AnimatedTile does not animate an arbitrary SpriteRenderer by itself. Retain existing tilemap animation for established ground flora; avoid globally changing BG_Decor's animation rate to tune new props. Its current rate is 1; CatTail assets have speed 1â€“2 and start frame/time zero, so review actual scene pace rather than assuming the gallery's default 4 FPS is the game rate.

Keep 16 PPU, Point filtering, unit scale and integer-pixel placement. Base sorting on ground contact and account for each animation's full frame bounds. Keep every frame's blue ripple footprint inside water. Inspect bank overlaps, tree occlusion, paths and seating foot space. Compare north/south close-ups and a complete animation cycle before accepting the pass.
