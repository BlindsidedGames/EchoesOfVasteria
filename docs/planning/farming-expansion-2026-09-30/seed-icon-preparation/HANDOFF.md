# Seed icon and separate-orchard handoff

Prepared22 known inventory icons plus one shared brown unknown, all16×16/PPU16. Removed only the verified40-pixel outer halo for inventory; retained black contour, pale paper and crop detail. Drop glyphs retain the halo. Source Crops.png/meta stay exact.

Appended23 entries213–235 to the existing floating-text atlas, TMP sprite asset and native inline mirror. Preserved all213 old sprite/character/glyph definitions and55,296 original RGBA pixels at their existing coordinates. Mac Metal verified every new TMP name/reference, native inventory Image rendering, TextCore named packet glyphs and representative old resource/stat/skill glyphs. Successful validation pass logged zero warnings/errors. This is isolated art rendering; no Seed Resource, drop routing, discovery/save logic or live gameplay UI was added, and no new Player/mobile test/build was run.

| Review image | Exact Library identity | Version |
| --- | --- | --- |
| [Before/after and shared unknown](../visuals/seed-icon-preparation/seed-pack-before-after.png) | `libfile_7dcc8a97d1188191bc53ffe9f6f3aa81` | 1 |
| [Unity inventory and TextCore](../visuals/seed-icon-preparation/unity-inventory.png) | `libfile_84d50dd664b88191a9f064e5e645ea0d` | 0 |
| [Unity TMP drop examples](../visuals/seed-icon-preparation/unity-drops.png) | `libfile_49b174e4a17c819197f43cbfee585cb6` | 0 |
| [Separate orchard comparison/context](../visuals/farm-optional-orchard.png) | `libfile_555f413a50c881919dc35beb045aa5c4` | 2 |

The five approved V4 crop figures retain their existing Library identities/version3. The subsequent [orchard rows revision](../orchard-rows/HANDOFF.md) supersedes the two-tree grass sketch: four evenly spaced trees have dedicated orchard dirt plots between field2 and the logging approach, retaining all six crop beds and60 crop renderers. It adds16 orchard soil tiles, with no path/fence/standing-tree removal. The earlier crop-bed substitution is historical. Orchard gameplay remains optional; fruit use, access/material costs and batch input are undecided.

Focused diff: four existing atlas files extended; one new normalized inventory sheet/meta; one isolated separate-orchard preview scene; planning/evidence updates. Temporary Unity helper and native alias removed. Two temporary Main-light guards restored exactly. Main remains open clean in Edit mode; branch/HEAD unchanged. All38 protected cleanup paths,77 actual player files and93 backups match their baselines. Existing user cleanup and migration report are untouched. No commit/push/release.

See [asset preparation](../seed-icon-preparation.md), [exact sprite IDs](asset-manifest.json), [pixel checks](pixel-verification.json), [Unity checks](unity-verification.json), [final preservation checks](verification.json) and [current executive plan](../README.md). No current blocker. Content mapping and later Player/mobile integration remain explicit future review work.
