# Snowy spruce variants

The plain snowy variants are in `Assets/Art/Environment/Snow/`:

- `Small_Spruce_Tree_Snow.png`: 32 by 64, pivot (0.5,0.35)
- `Medium_Spruce_Tree_Snow.png`: 32 by 48, pivot (0.5,0.35)
- `Big_Spruce_tree_Snow.png`: 64 by 80, pivot (0.5,0.25)

Each is derived from the corresponding original `_1` sprite in the Cute_Fantasy Trees folder. All retain 16 PPU, Point filtering, uncompressed RGBA, no mipmaps, original canvas dimensions and original pivots. Source pack assets are unchanged.

Generated snowy edits were used as foliage placement guides only. The final PNGs copy the original native pixel array and constrain snow recolouring to opaque green foliage; bright source foliage highlights retain the existing branch structure. Every original alpha value and every non-foliage pixel, including the trunk and ground shadow, remains byte-identical. The new colours come from the installed Christmas snow palette. Do not substitute the generated guide images directly into the game.

Validation: `output/scene-review/spruce-snow/validation.json`. Changed foliage pixel counts are 19, 56 and 273; changed alpha and protected pixel counts are zero for all three. Native-scale comparison: `output/scene-review/spruce-snow/comparison.png`.

The earlier `SnowySprucePlain.png` is a separate generated design with a different silhouette and stem. It is retained as prior work, but is not one of these three faithful variants. None of the new variants have been placed in the scene yet.
