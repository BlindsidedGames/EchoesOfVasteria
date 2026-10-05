# V4 review handoff

> Historical V4 checkpoint. The orchard substitution and missing-unknown-art recommendation are superseded by the [separate orchard](../optional-orchard.md) and [completed icon preparation](../seed-icon-preparation.md). Approved V4 crop figures and scene files remain unchanged.

Corrected isolated farm layouts from the user's placement/path feedback and incorporated fresh seed discovery. Main, player saves and the existing cleanup remain unchanged. One separate optional orchard space sketch uses existing world art. No farming/orchard gameplay, save migration, commit or publication was implemented.

## Before and after

Actual Main has front gate(−61,−1), back fenceY10 and two adjacent approach columnsX−62/−61. V3 incorrectly raised the front fence toY3 and extended a single track north. V4 restores the original gate/frontage and lowers the back fence toY6. Both original farmers and the windmill/sails keep exact positions/scales/sorting. First-field front fence pieces sort behind those NPCs and open inward; the windmill forms the lower west boundary rather than adding a post run over its wall.

Field2 and every southeast alternative now have two adjacent path columns reaching their actual gates and joining an existing lane. Branching field2 updates one source endpoint(−61,−3), WetOverlay NW→NEW join; original approach coordinates/transforms remain intact. A redundant upper stub was removed after enlarged visual review. Proper56 corner pieces and14 actual open gate assemblies are retained across built/work scenes; no intersecting fences require T/X junctions.

The first fence's top isY6.5, mountain CliffFoot shadow startsY10 and front faceY11:3.5/4.5 world-unit canvas clearance. The reference audit covers72 cliff cells and original/preview approach and actor poses. [Reference proof](layout-reference.json), [baseline/V3/V4 comparison](../visuals/farm-layout-before-after.png) and [dual-path comparison](../visuals/farm-dual-path-before-after.png) expose exact changes.

## What was reviewed

Visually inspected all five composed progression, alternatives, clearing, normal-scale and town-context figures; enlarged first-field/NPC frontage, second-field join, A/B/C gates/corners; matched before/after images; packet/ore-unknown contact and world-tree stage/fruit contacts; separate orchard close/normal/context views. Fourteen gates pass two-column tile-route and aperture checks,28 lane checks total. These are Edit-mode visual/static checks, not runtime navigation/collision/UI coverage.

Seven crop-only V4 scenes remain at `Assets/Editor/FarmingPreviewsV4`,49 raw renders in the private evidence directory. The optional orchard remains in `Assets/Editor/FarmingOrchardOptionPreview`, one scene and three raw renders. [Generator text](preview-generator.cs.txt), [crop manifest](preview-manifest.json) and [figure source](../visuals/revision-4/compose-figures.swift) preserve reproduction. All previews are outside normal Loading/Main build settings and have no gameplay MonoBehaviours.

V3's final125-file documents/scenes/renders archive at `/Users/matthewrushworth/Projects/Echoes Farming Planning Evidence/2026-09-30/revision-3-archive/` remains hash-verified; V1/V2 archives and original V1/V2/V3 scenes remain unchanged. The five current Library identities are retained, with current versions and exact file IDs in [delivery](../library-delivery.json). Additional local before/after contacts are linked review evidence. Orchard is a separate deliverable.

## Seed and orchard findings

- Approved: no Echo compensation; packs in plant drop pools; packs fresh/hidden until discovery for all players; old crop unlocks grant/reveal none. Starter and work/token grants are withdrawn.
- Proposed: integer quantity plus committed lifetime acquisition total, with derived discovery rather than another saved flag; first committed new-version acquisition reveals once and remains known at zero. Recipe access is computed from current game-defined criteria, not grandfathered crop access or extra saved unlock flags; exact criteria, bonus sub-pool/matching selection, actor eligibility and odds remain decisions.
- Verified:22 packets share one12×15 visible alpha mask. Existing eight cores share a flat brown unknown Sprite; inventory requires authored UnknownIcon while journal can show “?”. One common packet unknown asset/presenter is enough; none was generated. Normalize transparent16×32 versus Wheat16×16 presentation without reslicing references.
- Verified: actual32×64/PPU 16 sapling/young/bare-mature world sprites and separate fruiting apple tree. No authored orchard fruit resource/harvest exists. The optional two-tree substitution gives four crop beds plus two trees, no extra land/clearing, and remains unapproved scope.

## Diagnostics and preservation

The first additive orchard load emitted duplicate-global-light errors because Main and its preview registered global lights on the same sorting layers. Subsequent captures/reference reads use a temporary preview-only guard disabling and restoring the two Main Light2D enabled states exactly. [Orchard light guard](orchard-light-isolation.json) and [reference light guard](reference-light-isolation.json) both report clean Main and identical before/after states. Initial messages remain in the private log evidence, not suppressed or called Player defects.

The first strict path-tile identity check failed because field2 legitimately changes the lane endpoint's join. The revised audit separately verifies unchanged geometry/transform and records that one tile difference. Its initial exception remains in private evidence. No production fix was needed for either preview diagnostic.

[Final verification](verification.json) records38 source paths,77 player/preferences files,93 verified backups, prior archive/scene preservation, scene-script/build exclusion, static route/role checks, current contracts, Library results and helper removal. Existing cleanup64-test/Mac Player evidence remains independent; no new farm Player test was performed. The migration investigator's separate report was not edited.

The shared `/tmp/eov-unity-coordination.json` marks Unity free after clean Main/helper removal. The existing disposable cleanup Player and harness remain available at `/Users/matthewrushworth/Projects/Echoes Mac Verification/2026-09-30-cleanup/`; this is not a released-save migration harness.

## Latest requirements decision

All players follow the same new unlock requirements derived from code/assets. Retain actual skill levels/XP; discard obsolete unlock data and purchased old stat upgrades without starter-gear conversion. Keep meaningful quest history pending precise migration mapping. The current proposal no longer automatically grants farm stages from Fence1/2/3, and saves real construction/phase/payment facts instead of redundant unlocked flags. Existing quest criteria count only where a new game-authored requirement explicitly uses them, equally for everyone. Seed discovery stays newly earned for all. [Seventeen-task requirement audit](requirements-evidence.json) confirms canonical TaskData/UI levels agree. No migration report or save was edited.

Current follow-up: [decorative Tilemap tree correction](../fence-scenery-correction/README.md) clears two boundary trees from stage2 onward. The original V4 generator above reproduces the prior geometry; apply the recorded two-cell delta for the current scenes. Older optional orchard substitution below is historical; [current separate orchard](../optional-orchard.md) keeps all six crop beds.
