# Regular light-green terrain catalogue

This extends the approved [grass/dirt catalogue](GrassDirtJagged.md). Its names
and descriptions remain unchanged. Start with [the visual index](LightGreenTerrain-00-index.png).

| Review sheet | Coverage |
| --- | --- |
| [01: grass A](LightGreenTerrain-01-grass-A.png) | Regular transitions, old 00–33 |
| [02: grass B](LightGreenTerrain-02-grass-B.png) | Regular transitions, old 34–67 |
| [03: cliffs](LightGreenTerrain-03-cliffs.png) | All 38 stone lips, cliff faces, stairs and shadows |
| [04: water and fills](LightGreenTerrain-04-water-and-fill.png) | All 13 static stone banks, plus grass and water fills |
| [05: animated water A](LightGreenTerrain-05-water-animation-A.png) | Shapes 0–6, all 56 source frames |
| [06: animated water B](LightGreenTerrain-06-water-animation-B.png) | Shapes 7–12, all 48 source frames |

The scope follows the actual `Main` / Hometown sources: `Grass_Tiles_3`,
`Grass_3_Middle`, `Stone_Cliff_3_Tile`, `Water_Stone_Tile_3_Anim` and
`Water_Middle`, plus the matching static `Water_Stone_Tile_3` sheet. Other biome
palettes, farmland artwork, props and scene placement are outside this pass.
There are six sheets, 225 sprites and 120 named Tile assets. Three map configs
also have cached inspector/search labels synchronized; their generation
settings are unchanged.

## Explicit directions and occupancy

Directions describe native unrotated artwork: north is up, east is right, south
is down and west is left. Apply a placed cell's rotation/flip to those directions.

`GrassSide_N_DirtSide_S` means grass is north and dirt is south.
`WaterSide_N` means water is north. `VoidNotch_SE` means transparent space
intrudes into the southeast corner of surrounding grass. `DirtNotch`,
`StoneNotch` and `WaterNotch` similarly identify the intruding material.
`Corner_SE_OverVoid` identifies grass occupying the southeast corner, with
transparent space outside it. `RoundedTip_S_LeftHalf` is the left assembly
piece of a rounded grass tip pointing south. It is not a west/east facing tip.

`SoilRim` is the brown trim and `StoneRim` / `StoneLip` is the blue-gray stone
trim. A `FrontFace` is the stone cliff face projected down-screen; the name
does not assert a gameplay elevation, slope or number of height levels.
Stair and shadow `Top`, `Middle`, `Bottom`, `Left`, `Center`, `Right` and
`Single` describe assembly positions. The tan step inserts are described
visually; their intended use and material are not established by available
records and remain for review.

## Selecting assets without inspecting pixels again

Use [LightGreenTerrain.tiles.json](LightGreenTerrain.tiles.json). Every sprite
has its old index/name, descriptive name, source GUID, local ID, rectangle,
pivot, layer role and occupancy samples. Sprite local IDs are decimal strings
to avoid JSON precision loss. Match a sheet GUID and sprite local ID with
`AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid))`;
Tile assets have their own GUID/path mapping in `assets`.

`grass_connections`, `water_connections` and `transparent_sides` describe
samples at edge midpoints. `occupancy_samples` includes all four corners too.
These samples describe rendered material, not complete collision/walkability
rules; a corner notch can exist while all four edge midpoints remain grass.
Use the named notch and corner samples when selecting diagonal joins. Gray
backing in the previews makes source transparency and shadows visible.

Animated bank sprites have `shape_index` and `animation_frame` (01–08 in source
sheet order). The visual sheets show every frame, with the original numeric
ID and new frame suffix. Existing rule/animation arrays, playback order, speed,
start frame, colliders and Tile flags are unchanged. The eight plain blue
surface frames are visually identical; their individual IDs are retained.

`Grass_` and `Water_Middle` prefixes are deliberately retained because
`HabitatAnimal.IsSafe` uses them to classify land and open water. In the regular
grass sheet, `Grass_LightGreen_` identifies the palette family: the `DirtFill`
member is still dirt artwork. Its classification remains exactly as before.
The shared `Better Rule Tile.asset` and its `Grass` / `GrassWater` rules retain
their names, rule data and references; the renamed sprites are resolved by
unchanged GUID/local-ID pairs.

## Regeneration and checks

Run `RenderRegularTerrainCatalogue.ps1` under `output/scene-review` to rebuild
the PNGs. `ValidateRegularTerrain.cs` is a read-only Unity validation script.
`VerifyRegularTerrainMigration.py` checks exact names-only serialization
against the private pre-migration snapshot. Inventory/mapping builders and
`ApplyRegularTerrainNames.cs` describe the one-time migration and must not be
rerun against already migrated assets.

Historical terrain helpers now use the renamed grass-fill name. They remain
authoring tools that can alter scenes, so do not run them merely to inspect
the catalogue.
