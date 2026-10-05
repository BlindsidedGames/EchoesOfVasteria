# Revision3 review handoff

Corrected the isolated farm previews using actual fence corner and gate art. Main, player saves and the existing Mac cleanup remain unchanged. No farming gameplay, save migration, commit or publication was implemented.

The old preview generator repeated straight EW rails at the corners and used bare fence gaps. The corrected assembly uses distinct upper/lower E/W corner pieces, vertical post runs and open gate frames with integral end posts. North and south frames require different half-tile pivot offsets. Each field now has one connected entrance; redundant southern holes are closed. No intersecting enclosures require T/X junctions. [Fence assembly evidence](fence-assembly.md) explains why a small assembly supplement is sufficient for the existing semantic catalogue.

Visually inspected the enlarged first field, all three enlarged southeast alternatives, progression/clearing comparisons, normal-scale panels and wider town context. The [matched before/after](../visuals/farm-fence-before-after.png) exposes the correction. All five existing Library files are version2, with unchanged identities recorded in [delivery](../library-delivery.json). A local before/after figure is additional review evidence, not a sixth replacement.

Current separate previews are under `Assets/Editor/FarmingPreviewsV3`: seven scenes and 49 raw renders. The original and revision2 scenes remain intact. Revision2's last reviewed documents, scenes and 42 renders are hash-archived privately at `/Users/matthewrushworth/Projects/Echoes Farming Planning Evidence/2026-09-30/revision-2-archive/`. [Generator text](preview-generator.cs.txt), [manifest](preview-manifest.json) and [figure source](../visuals/revision-3/compose-figures.swift) retain reproduction; no live helper remains.

## Planning decisions incorporated

- User chose **no Alter Echo compensation**. Removed finite non-crop compensation, crop-card/quest/collection yield conversions, transition allowances and the old 50% farm modifier recommendation. Existing inventory/history preservation is distinct from compensation; unclaimed StoredResources still needs an explicit interpretation before migration.
- User chose **plant drop pools for seed packs**. Removed the all-gathering work counter, useful-pack guarantee, entitlement/token refill, related UI/schema and invented storage cap. The plan recommends a bonus seed sub-pool to preserve existing crop drops, while explicitly comparing literal substitution. No drop odds or new unlock semantics were approved by this revision.
- Recommended first pack unlocks that crop's farm recipe at its existing level; subsequent packs sustain batches. This would give late-game players a new recipe collection, but needs review alongside matching versus global packs, eligible actors and a proposed Radish tutorial grant. Random acquisition has no maximum guaranteed wait.
- Capacity remains a provisional six-bed/three-quest recommendation for compact town composition; eight has more reserves/clearing, four is a smaller endpoint. Planks/Construction/animals remain deferred. Economy tables are unmeasured continuous-active scenarios with an explicit duty-cycle correction.
- Mobile save migration/reset and Steam legacy-branch fallback remain undecided. The separate released-format audit owns its report. No audit deliverable was edited. The proposed transaction coordinator, failure gates, exact Fence carryover/cohort rules and local-clock limitations remain engineering proposals.

## Verification and limits

[Verification](verification.json) records source/save/backup preservation, old-preview hashes, build-scene exclusion, helper removal, role counts, static path checks and focused current-contract checks. The preview helper compiled and rendered in Metal Edit mode; no Play or save selection ran. Static tile connectivity and visual gate alignment are demonstrated; gameplay pathfinding/colliders/UI remain future checks. Existing 64-test and Mac Player cleanup evidence is retained, not claimed as farm runtime coverage.

The shared coordination file `/tmp/eov-unity-coordination.json` marks Unity free after helper removal and clean Main verification. Existing disposable Player: `/Users/matthewrushworth/Projects/Echoes Mac Verification/2026-09-30-cleanup/EOVOfflineVerification.app`. Harness source: `MacCleanupSmoke.cs.txt` in that directory; its reports are `/tmp/eov-mac-cleanup-20260930/`. It is a cleanup/offline-path harness rather than a released-save migration harness.
