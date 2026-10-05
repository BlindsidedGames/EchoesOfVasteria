# Grass / dirt jagged tile catalogue

Use [the visual catalogue](GrassDirtJagged-catalogue.png) to review names and
[the mapping](GrassDirtJagged.tiles.json) to choose assets. This covers all 49
sprites and 49 Tile assets from `Grass_Tiles_3_Blob_TEST.png`, including the small
dirt-islet decor Tile. The corresponding source is now `GrassDirtJagged_Blob.png`.
Other grass sheets, farmland art, terrain placement and pixels are outside this migration.
The three map configs referencing the decor islet also receive its new cached
inspector/search label. Their references and generation settings are unchanged.

## Reading a name

All names start with `GrassDirtJagged_`. Directions describe the unrotated sprite:
N = up, E = right, S = down, W = left.

| Component | Meaning |
| --- | --- |
| `DirtInterior` | Dirt joins all four edges; may have listed grass notches. |
| `DirtEdge_N` | Grass border north; dirt joins east, south, west. |
| `DirtOuterCorner_NE` | Grass borders north/east; dirt joins south/west. |
| `GrassNotch_SW` | Inner grass corner intrudes into southwest dirt. |
| `GrassNotches_NW_NE` | Both listed intrusions are present. |
| `DirtStrip_NS` / `DirtStrip_EW` | Narrow dirt strip joins named opposite edges. |
| `DirtCap_N` | Closed north tip; its dirt connection is south. |
| `DirtIslet` | Isolated dirt patch surrounded by opaque grass; decor Tile. |
| `GrassSolid_DuplicateA` / `DuplicateB` | Same pixels, distinct sprite/Tile identities. |

Edge/corner suffixes identify grass borders, not open dirt connections. Notch
suffixes identify grass intrusions. A corner plus an opposite notch differs
from a plain corner: this distinction prevents the logging-clearing mistake.

## Selecting tiles

1. Match the intended open dirt edges with the `dirt_connections` set.
2. Check `inner_grass_notches` and `dirt_diagonal_connections`. For continuous
   dirt, choose an entry without an unwanted notch.
3. Resolve `asset_guid` with `AssetDatabase.GUIDToAssetPath`, then load the Tile.
   Do not construct paths from numeric names.
4. Apply any existing Tilemap cell rotation/flip to these native directions.

Former 5 is `GrassDirtJagged_DirtOuterCorner_NE_GrassNotch_SW`; former 20 is
the plain `GrassDirtJagged_DirtOuterCorner_NE`. Former 3 is a north edge, not a
south edge. This migration names the differences and replaces no placed tiles.

Connections describe artwork at edge midpoints, not collider/walkability rules.
There are 47 dirt topology variants and two identical solid-grass entries.
Their intended A/B purpose is unknown; both identities remain for review.

## Preservation and review

The JSON stores original name/index/path, new name/path, Tile GUID, texture GUID,
sprite local ID, sprite ID, native rectangle and connection semantics. Local IDs
are decimal strings to avoid JSON numeric precision loss. No sprite is resliced.

Review every PNG variant, especially corner-plus-opposite-notch combinations
(old 5, 8, 35, 48), cap directions, and solid-grass duplicates (old 6, 42).

Regenerate the PNG with `output/scene-review/RenderGrassTileCatalogue.ps1`.
`BuildGrassTileMapping.py` is a one-time pre-migration inventory generator;
it requires original metadata, so do not rerun it against renamed assets.
`MatchLoggingLayer.cs` retains its historical layout translation using GUID
lookup from this mapping. Its old indices are not a terrain selection algorithm;
do not run it to repair a layout.
