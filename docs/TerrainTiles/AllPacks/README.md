# Tile catalogue and selection guidance

Coverage: 1,552 TileBase objects (1,412 Tile, 110 AnimatedTile, 30 BetterRuleTile),
312 source sheets and all 4,800 imported sprite variants across eight Cute
Fantasy packs and BetterRuleTiles samples. Unused modular tile art is included.
The inventory separately lists 873 excluded actor, UI and isolated prop textures;
linked TileBase props and modular architecture are included. Whole-atlas imports
remain whole atlases: this task does not reslice art or invent missing variants.

Start with `tiles.json`. Find a material and role, then inspect its connection,
orientation and uncertainty fields. Resolve assets using GUID plus local ID;
names and source suffixes are descriptive search aids, not permanent identity.
`old_name` is the pre-expansion name, `original_name` also retains names from the
earlier approved passes. `old_path` records migration provenance. `name` and
`path` identify the current asset. Prior reviewed names may be recorded as
superseded when final visual review corrected a direction or unsupported claim.

N is up, E right, S down, W left in **unrotated source artwork**. A Side suffix
identifies the named material on that side. The approved `DirtEdge_N` means the
north boundary of dirt, with grass on the north. Check cell transforms before
applying source topology to a placed tile: the logging clearing's bottom edge
uses a DirtEdge_N tile rotated 180 degrees, which correctly renders grass south.
Inner grass notches are real artwork; do not place them inside continuous dirt.
Doorway joins may deliberately remain open even where a path layer ends.

Frame numbers preserve source frame identity; serialized animation order and
RuleTile outputs/rules are authoritative and unchanged. Atlas/Assembly denotes
a compound imported rectangle, not an individual autotile. A/B distinguishes
visible source variants without inventing height or intended gameplay semantics.
Uncertainty fields flag artwork whose intended use cannot be established locally.

The nine per-pack PDFs in `output/tile-catalogues` form the visual review set.
They are private Library deliverables and local generated files, excluded from
the public Git repository to avoid repackaging licensed source art. On another
machine, retrieve the private Library files or regenerate them from the mapping:
`python output/scene-review/RenderAllTileCatalogue.py`, then
`python output/scene-review/BuildTileCataloguePDFs.py`. These tools require Pillow
and ReportLab; rendering supports Windows, macOS and Linux font locations.
`index.json` lists every page and its GUID/local-ID identities. Every variant is
included exactly once, with native pixels enlarged using nearest-neighbour.
Large assemblies have dedicated native 2x pages. Catalogue QA ledgers distinguish
the exhaustive initial art review from affected-page checks after label wrapping.

`Duplicates.md`, `duplicates.json` and `pixel-audit.json` distinguish exact decoded
pixels, visible equivalence ignoring hidden transparent RGB, and weak lookalike
candidates. Equal artwork does not make differing slices, pivots, rules or frame
lists interchangeable. No duplicate identities were deleted or merged.

47 TileBase objects had unresolved serialized sprite properties before naming.
`import-validation.json` checks these separately from newly missing references.
They remain unresolved; this task does not fabricate art or repair unrelated
packages. Inspect the recorded property paths before using an affected object.

Names preserve the production `Grass_` and `Water_Middle` habitat predicates.
Cached DecorEntry display labels follow renamed Tile objects while generation
weights and references remain unchanged. BetterRuleTiles user-driven substring
replacement must use current semantic names; sprite geometry and authored rule
layout remain authoritative. Historical numerical layout translators are retired
as repair tools: use GUID mappings and placed-cell topology instead.

Private owner review only. Local pack licence records permit modifications where
documented and prohibit redistribution/resale of the artwork; missing local
licence records remain flagged. The catalogues are not a public art distribution.
