# Superseded scenery-removal diagnosis

The removal recommendation below was incorrect. Canopy crossings are allowed; this was a depth-ordering defect. Both scenery trees are now restored at their original feet. Stage2 reclaims23 stumps and does not clear these two trees. Use the [current depth correction and validation](../depth-sorting-correction/README.md). The remainder of this file and its original evidence record the previous iteration for comparison, not current acceptance.

# Existing trees crossing the new fences — corrected

The reported tree was existing scenery, not one of the four orchard trees. `BG_Decor 0` contains a `FruitTree_Growth_YoungTree` at cell `(13,-13,0)`, world anchor `(−61.5,−12.5)`. Its visible canopy crosses field2's south fence. Tilemap sorting order0 places it behind the new order3 rails, visibly slicing the canopy. The earlier renderer-only scenery check missed decorative Tilemap trees.

An expanded all-stage audit found a second instance at cell `(23,-5,0)`, anchor `(−51.5,−4.5)`, crossing field2's east/northeast fence. Both use the existing 32×64/PPU16 growth sprite, foot pivot(16,16). This is a placement problem: moving either canopy in front of the rails would conceal the fence and leave the overlap. The isolated correction clears both trees during stage2 site preparation, alongside the existing stump reclamation. It does not change sorting, tree art, fence pieces or enclosure geometry.

![Both conflicts before and after, with identical enlarged cameras](../visuals/farm-fence-scenery-before-after.png)

| Preview state | Exact correction |
| --- | --- |
| A0 baseline and A1 first two beds | Unchanged; both existing trees remain. |
| A2 field2 preparation and A2b southeast work | Remove the two boundary tree cells. |
| A3 compact, B3 eight-bed alternative, C3 courtyard | Inherit the same two removals; no additional fence-edge scenery changes. |
| Separate orchard mixed/all-mature scenes | Inherit the same correction; all four new tree anchors, soil plots and six crop beds stay exact. |

The stage2 description now includes **23 existing stump renderer objects plus two standing decorative tree tiles**. These two tiles are additional preparation work, not part of A's20-of28 local southeast standing-renderer count or B/C's31 wider-renderer count. Counts describe preview objects, not resource payouts or approved quest costs. The third quest's persisted tree-to-stump work phase remains unchanged. Production would need a deliberate stage2 clearing presentation; these endpoint previews do not implement it.

## Verification and limits

The audit reads actual source alpha pixels and applies each sprite/tile's imported rectangle, pivot, PPU and world transform. It checks new fence pieces against active adjacent SpriteRenderers and decorative Tilemap cells across all nine scenes. This covers bushes, stumps, trees, rocks and buildings, rather than relying on filenames or transparent canvas bounds. The two conflicts touched159 and64 scenery pixels respectively across their fence pieces. After correction there are no vegetation/fence intersections. The remaining three scenery intersections are original Flora, Tillman and windmill foreground occlusion at the first farm's frontage; their positions and intended depth remain unchanged.

[Before intersections](opaque-intersection-before.json), [after intersections](opaque-intersection-after.json) and [exact scene-art difference](exact-scene-art-diff.json) show that each affected scene loses precisely two Tilemap entries, with zero changed or added visible art entries. Every fence corner/end/gate, crop, soil tile, path cell, retained tree and orchard tree keeps its sprite reference, transform and sorting. [The narrowed serialized diff](narrow-serialized-diff.json) also verifies every other scene component byte-for-byte: incidental animation-rate and soil tile-flag changes from Unity scene serialization were restored. Only the two cell records and their four internal reference counts differ. A0/A1 and every scene metadata GUID are preserved.

All nine scenes were rendered in the Metal Editor. Both offenders, all field boundaries and the courtyard tree were inspected enlarged; baseline, every work/built stage, both alternatives and mixed/all-mature orchard were inspected at normal size18. Wider town context was inspected as well. The four orchard mature silhouettes retain their0.4375-unit fence clearance and0.625-unit row gap. The new audit supersedes the previous claim that the renderer-only orchard check covered every existing scenery type.

- [Corrected progression](../visuals/farm-progression.png)
- [Normal camera comparison](../visuals/farm-normal-scale.png)
- [Town context](../visuals/farm-town-context-annotated.png)
- [Separate orchard](../visuals/farm-optional-orchard.png)
- [Current raw matched captures](../visuals/fence-scenery-correction/renders/)
- [Final preservation and delivery checks](verification.json)

Main remains open, clean and outside Play. Its temporary light/culling isolation was restored exactly. Real player files and verified backups are unchanged; seed icon/atlas preparation and unrelated Mac cleanup are preserved. Temporary Editor workers were removed. No live farming/orchard behavior, runtime collision/navigation test, commit or release is included.

Prior scenes, documents and figures remain in the sibling planning evidence directory under `fence-scenery-correction-2026-10-01/baseline` and `prior-documents`. Earlier public `revision-4` and `orchard-rows` render evidence is retained as historical. The correction helper and alpha audit are preserved as `.txt` evidence here; they are not active Editor scripts.
