# Tilemap duplicate audit

Coverage: 4,800 imported sprite crops across 312 sheets. All identities are retained; nothing was merged or deleted.

| Comparison | Groups | Member identities |
| --- | ---: | ---: |
| Exact decoded source images | 7 | 14 |
| Exact decoded crops/frames | 801 | 1983 |
| Visible equivalence including exact crops | 802 | 1985 |
| Weak sampled-alpha candidates (unconfirmed) | 274 | 1778 |

Group counts overlap: visible equivalence includes exact matches; candidate groups are not additional confirmed duplicates. Exact hashes include dimensions and decoded BGRA pixels. Visible equivalence normalizes hidden RGB at alpha zero only. Compressed file hashes and filenames are not the basis of comparison.

## Whole-source duplicates

- `Assets/Art/Packs/Cute_Fantasy/Buildings/Houses_Interiors/Brick_Wall_Fillers.png` <-> `Assets/Art/Packs/Cute_Fantasy/House/Buildings/Houses_Interiors/Brick_Wall_Fillers.png`
- `Assets/Art/Packs/Cute_Fantasy/Buildings/Houses_Interiors/Interior_Walls.png` <-> `Assets/Art/Packs/Cute_Fantasy/House/Buildings/Houses_Interiors/Interior_Walls.png`
- `Assets/Art/Packs/Cute_Fantasy/Buildings/Houses_Interiors/Stone_Wall_Fillers.png` <-> `Assets/Art/Packs/Cute_Fantasy/House/Buildings/Houses_Interiors/Stone_Wall_Fillers.png`
- `Assets/Art/Packs/Cute_Fantasy/Buildings/Houses_Interiors/Wood_Floor_Tiles.png` <-> `Assets/Art/Packs/Cute_Fantasy/House/Buildings/Houses_Interiors/Wood_Floor_Tiles.png`
- `Assets/Art/Packs/Cute_Fantasy/Buildings/Houses_Interiors/Wood_Stairs.png` <-> `Assets/Art/Packs/Cute_Fantasy/House/Buildings/Houses_Interiors/Wood_Stairs.png`
- `Assets/Art/Packs/Cute_Fantasy/Buildings/Houses_Interiors/Wood_Wall_Fillers.png` <-> `Assets/Art/Packs/Cute_Fantasy/House/Buildings/Houses_Interiors/Wood_Wall_Fillers.png`
- `Assets/Art/Packs/Cute_Fantasy_Dungeons/Dungeon_1/Dungeon_1_Stairs_Down.png` <-> `Assets/Art/Packs/Cute_Fantasy_Dungeons/Dungeon_1/Stairs_Down_SingleFrame.png`

Six pairs are duplicate old/new Cute Fantasy interior folder trees; the seventh is two Dungeon 1 stairs-down source images. None of these whole-source groups crosses pack boundaries. Import settings and sprite slicing can differ even when whole image pixels match.

## Exact matches across packs

Found 1 exact crop group crossing pack boundaries. It contains 15 sprite identities: the plain 16x16 tan fill shared by Cute Fantasy and Cute Fantasy Desert. Its artwork supports several material contexts; identical pixels do not imply identical intended role.

- `Beach_SandFill_DuplicateVariant56` - `Assets/Art/Packs/Cute_Fantasy/Tiles/Beach/Beach_Tiles.png`; GUID `8d39ab0e797a3a64daf8d63b137bf19d`, local ID `1370976606`.
- `Beach_SandFill_DuplicateVariant31` - `Assets/Art/Packs/Cute_Fantasy/Tiles/Beach/Beach_Tiles.png`; GUID `8d39ab0e797a3a64daf8d63b137bf19d`, local ID `1927001874`.
- `Beach_SandFill_DuplicateVariant36` - `Assets/Art/Packs/Cute_Fantasy/Tiles/Beach/Beach_Tiles.png`; GUID `8d39ab0e797a3a64daf8d63b137bf19d`, local ID `512810686`.
- `Beach_SandFill_DuplicateVariant41` - `Assets/Art/Packs/Cute_Fantasy/Tiles/Beach/Beach_Tiles.png`; GUID `8d39ab0e797a3a64daf8d63b137bf19d`, local ID `-145065055`.
- `Beach_SandFill_DuplicateVariant46` - `Assets/Art/Packs/Cute_Fantasy/Tiles/Beach/Beach_Tiles.png`; GUID `8d39ab0e797a3a64daf8d63b137bf19d`, local ID `1836444481`.
- `Beach_SandFill_DuplicateVariant51` - `Assets/Art/Packs/Cute_Fantasy/Tiles/Beach/Beach_Tiles.png`; GUID `8d39ab0e797a3a64daf8d63b137bf19d`, local ID `-822577423`.
- `Grass_LightGreen_DirtFill` - `Assets/Art/Packs/Cute_Fantasy/Tiles/Grass/Grass_LightGreen_Transitions.png`; GUID `9b556bcdbea551d44b632b6439a0a187`, local ID `-752133865`.
- `Grass_ForestGreen_DirtFill` - `Assets/Art/Packs/Cute_Fantasy/Tiles/Grass/Grass_Tiles_1.png`; GUID `1f4cd33967e6b3e44b2df27724aa7e02`, local ID `2004422938`.
- `Grass_DirtJagged_ForestGreen_DirtInterior` - `Assets/Art/Packs/Cute_Fantasy/Tiles/Grass/Grass_Tiles_1_Blob_TEST.png`; GUID `26a9162a652b514408776d273f54efb4`, local ID `-856501228`.
- `Grass_BrightGreen_DirtFill` - `Assets/Art/Packs/Cute_Fantasy/Tiles/Grass/Grass_Tiles_2.png`; GUID `0eb70ad7b3525c048a6c7db69b66130a`, local ID `831560574`.
- `Grass_TealGreen_DirtFill` - `Assets/Art/Packs/Cute_Fantasy/Tiles/Grass/Grass_Tiles_4.png`; GUID `3745c5541e61a28499f8dfdf1eeb544a`, local ID `810339408`.
- `GrassDirtJagged_DirtInterior` - `Assets/Art/Packs/Cute_Fantasy/Tiles/Grass/GrassDirtJagged_Blob.png`; GUID `8c523df2568d56a42a43f494d14f321b`, local ID `-937340696`.
- `Path_DirtFill` - `Assets/Art/Packs/Cute_Fantasy/Tiles/Grass/Path_Middle.png`; GUID `d1e5e8b90bcbc6c4da9656f268360f98`, local ID `-8724323361410377488`.
- `Desert_Beach_1_SandFill_Src006` - `Assets/Art/Packs/Cute_Fantasy_Desert/Tiles/Desert_Beach_Tiles_1.png`; GUID `1bac922f48a8e2f4696d58f85062d8e9`, local ID `-626769069`.
- `Desert_Cliff_1_SandFill_Src072` - `Assets/Art/Packs/Cute_Fantasy_Desert/Tiles/Desert_Cliff_Tiles_1.png`; GUID `4c3e8b7adff11fb43ad6c842e8fd16f7`, local ID `2141454170`.

## Visually inspected lookalikes

[Review sheet](Duplicate-lookalike-review.png) shows 12 explicitly inspected native crops: four matching southwest grass void-notch variants and eight waterfall assemblies (frame 00). Labels contain original/current semantic names.

- Four grass palettes share exactly the same full-resolution alpha silhouette, but have different decoded color hashes. Visual inspection confirms matching notch geometry and different green/shadow palettes. They are palette alternatives, not exact pixel duplicates.
- Waterfalls 1–4 are palette variants of assembly A; 5–8 are matching palette variants of assembly B. In the pictured first frames, A/B have visibly different lower-foot artwork: B exposes a thin tan edge beside the lower right grass/rock bank. Keep both authored variants and all animation frames. Similar cascade flow alone does not make them interchangeable.
- No assertion is made here that every animation frame is a pure palette substitution. This supplemental review inspected frame 00 for eight assemblies; exhaustive per-frame exact hashing covers all imported frames.

The 274 weak candidate groups comprise 1,778 member identities. Their 8x8 sampled alpha masks are coarse retrieval hints, not proof of shared full-resolution silhouette, material, geometry or topology. Fully opaque tiles often share the same mask despite different interior artwork; large compound atlas sprites also defeat this heuristic. Only the 12 crops on the supplemental review sheet were visually adjudicated specifically for this lookalike report. Remaining weak groups are unconfirmed and should be consulted together with the complete semantic catalogues rather than called duplicates.

## Why matching artwork is not a replacement instruction

Preserve GUID and local ID, crop rectangle, pivot, pixels per unit, filtering, transparency, packing, collider behavior, Tile/RuleTile rules and transforms. Animation frame equality does not authorize removing a frame: repeated frames can control cadence or timing. Shared source pixels do not imply shared sprite slicing or import configuration. Tile-backed crops, full atlas assemblies and unused variants have different consumers. No duplicate consolidation is authorized.

Machine-readable evidence: [duplicates.json](duplicates.json), [pixel-audit.json](pixel-audit.json), and [tiles.json](tiles.json). Original identities remain searchable after semantic naming. Supplemental selection and full-alpha measurements are retained under output/scene-review. Catalogue artwork remains private to the asset owner under pack redistribution restrictions.
