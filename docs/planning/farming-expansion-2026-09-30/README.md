# A farm that grows with your adventures

Latest UI/progression decision, October 2: use the first desktop style with a Garden/Orchard switch and separate Beds, Progression and Town views. Orchard is a separate area and must unlock later; its exact gate, costs and Flora/Tillman level benefits remain undecided. Barkley/Gill systems are parked for this garden/orchard design session. The visually accepted 2→4→6 layout is not approval of an unlock-level/price table. [Current desktop designs and shared baseline](../farming-ui-concept-2026-10-02/README.md) distinguish actual prototype facts and illustrative states.

Implementation update, October 1: the approved [isolated two-bed Radish slice](../../development/farming-radish-slice-2026-10-01/README.md) now has native Player, offline and failure evidence. This planning package remains the broader design reference.

Recommended first complete expansion: retire recurring Alter Echo production and build six finite crop beds through three Barkley stages. Adventures supply materials and seed packs; town crops grow while adventuring or offline, then wait for one harvest. This is a planning package with isolated Unity previews. No farming gameplay, save migration, commit or release has been implemented; existing uncommitted Mac cleanup is preserved.

## What can be implemented next

[Concrete implementation order and first playable slice](implementation-sequence.md) now connects the accepted town layout to the released-save audit. Start with compatibility ingress fixes and the pure farm model, then the economic coordinator and a **two-bed Radish loop** in the original plots: adventure pack discovery → plant → equal online/offline growth → one harvest → verified reload, with a readable native bed list. Prove one bed internally before completing that stage. No orchard capacity choice or mobile release decision is needed to start this isolated development work.

The migration report establishes a Mildred1 → BuffSlot2 paid-receipt alias; preserve it without repaying fish or replaying rewards, and regular capacity still reaches five. It also demonstrates typed milestone ingestion and Cauldron-profile preservation defects that need bounded fixes before release. Production seed-source membership, unclaimed Echo stock, Cauldron/Infinity pacing, platform transition and optional orchard mechanics/capacity remain direction choices; rates/prices need measurement. [Verification and checkout evidence](implementation-readiness.json) record this planning-only update.

## Decisions already made

**No Alter Echo compensation. Seed packs enter plant drop pools and are fresh discoveries for everyone, including legacy players.** Old crop unlocks, levels, card counts and rewards do not reveal or grant packs. Everyone follows the current game-defined crop requirements; obsolete unlock data is discarded, actual skill levels/XP retained, and purchased old stat upgrades discarded without starter-gear conversion. Meaningful quest progress is not blanket-deleted. All new seed balances start at zero and undiscovered; a new-version pack reward records actual acquisition history and reveals its own pack. Discovery is derived from that history and survives spending the final pack. The earlier tutorial starter grant and guaranteed work/token proposals are withdrawn.

Use existing unknown-resource presentation until discovery: a shared unknown packet icon, “Undiscovered”/“???”, and no hidden crop-name tooltip. All 22 packet sprites have an identical visible12×15-pixel silhouette. One shared unknown variant is coherent, like the eight cores' shared brown silhouette; a shared packet variant is now prepared and rendered in Unity. The journal already uses a hidden image and question mark. Stock inventory does not automatically tint an icon. [Art and UI evidence](seed-discovery-and-art.md) and [prepared icons with actual UI/drop renders](seed-icon-preparation.md) document the source convention and completed asset work.

## The bounded player loop

Barkley prepares the original farmers' plots, reclaims the existing stump field, then clears and prepares the southeast edge: capacity 2→4→6. A tree-to-stump work phase persists inside quest 3 after a committed material hand-in; it is not a fourth unlock. Reuse Log, Stick and modest Stone, with competing sinks measured before prices are final. Quests already support materials, prerequisites and staged objects. Planks and a Construction skill would add currencies, screens and migration for little benefit to this first loop. Animals remain future scope.

One pack pays for one whole visual bed/batch, not nine independent plants. No automatic replanting, watering attendance, crop death or repeating farm XP. Ready crops wait safely. Reuse17 existing crop recipes/resources after explicit art mapping: Radish currently uses eggplant, Pepper beet, Lettuce cabbage, Turnip radish; Cucumber's current growth array ends at a sprout. Do not copy those mismatches into town recipes or rename persistent keys.

Recommend matching-crop packs, a separately rolled seed sub-pool preserving the existing plant reward, and recipe access computed from new pack discovery and current authored level/quest requirements. Drop routing and the final authored requirements need review; no saved recipe-unlock flags or grandfathered crop access are proposed. Radish is level 1; Corn is level 7. A player with zero seeds can continue adventuring and complete Radish plants until an eligible pack drops; the farm can wait without blocking the rest of the game. Random acquisition has no guaranteed maximum wait. Farm harvest never rolls seed packs.

Crop choice should serve specific quest shortages as well as stew value, with a quick/long return-frequency versus seed-efficiency trade-off. The 2/4/6/8-bed tables use provisional30-minute/2-hour growth and1/4/12 packs per active adventure hour. They are continuous-active-play scenarios, not measurements. An explicit one-hour-adventure/three-hour-town-or-offline example corrects seed income to one-quarter per elapsed hour. Six is preferred for compact town composition and three readable stages, not as a proven economic optimum; eight raises long-growth throughput and unattended reserves.

## Corrected town fit

V4 preserves the original south gate at(−61,−1), windmill and farmers. It lowers the north/back fence fromY10 toY6 instead of shifting the farm north. Its upper sprite bound isY6.5:3.5 world units below the nearest mountain foot-shadow canvas and4.5below the front face. No farm path extends toward the mountain. “Dual paths” means the two adjacent path columns at the actual original gate; both later fields now match them. Connecting field2 retouches one existing lane-end join at(−61,−3), preserving the original approach's position and transform.

Proper corner/end/gate art and cultivated soil remain in every built preview. The compact final field occupiesX−63..−53/Y−29..−23, cutting20of28 local southeast standing renderers; this is substantial local clearing. The eight-bed and courtyard alternatives each cut 31 in wider areas. [Baseline/previous/V4 comparison](visuals/farm-layout-before-after.png), [dual-path comparison](visuals/farm-dual-path-before-after.png), five updated figures and enlarged alternatives make the changes inspectable. The two boundary trees remain: canopy crossing is allowed. Their original Background-layer tiles caused incorrect fence occlusion; [ground-depth correction and matched controls](depth-sorting-correction/README.md) restore their original feet without changing fences or clearing them. Stage2 reclaims23 stumps; the20/28 southeast count stays separate. Main is untouched. Static checks cover14 gates and28 approach columns; runtime navigation/UI remain future validation.

## Retire and migrate coherently

Alter Echoes cover61 resources, including44 non-crops. No old investment becomes seeds, farm modifiers, resources or a transition allowance. Preserve owned ordinary inventory and history if migration is chosen. Unclaimed stored earnings still need an explicit treatment; no payout or deletion is assumed. Remove generation-only cards from every Cauldron pool, then review normalized odds, future quest/collection rewards and Infinity entry together. Existing inventory-tier bonuses alone reach+150%, so crop/stew output needs measurement.

Preserve meaningful Fence1/2/3/house quest progress and already-earned distance history, but do not automatically translate old Fence completions into new farm-stage grants. Everyone evaluates the same game-defined requirements. New build definitions can explicitly accept meaningful existing quest criteria as prerequisites; that is ordinary requirement evaluation, not migration grandfathering. Store real quest/phase/material receipts, not redundant crop/bed-unlocked flags. The 94-ID original quest cohort stays on both sides of original completion; new farm progression has a separate three-ID cohort. The [released-format omission contract](../save-migration-feasibility-2026-09-30/unlock-omission-contract.md) identifies exact fields and the Mildred1 alias; runtime/device validation and platform choice remain gates.

Extend Oracle's existing coordinator with a short economic command barrier and narrow typed publication into the live tree. One atomic Harvest Ready commit records all payouts, empty beds and claim identities. Queue whole reward/hand-in operations, coalesce autosaves, and recover committed state after a crash before publication. Growth uses elapsed time consistently and pays each planted batch once. Local clock jumps can accelerate held packs; finite stock and adventure-only replenishment constrain supply but are not anti-cheat.

## Review choices

| Choice | Recommended direction | Trade-off |
| --- | --- | --- |
| Seed routing and progression | Bonus plant sub-pool, matching packs, first-pack recipe access, primary hero initially | Shared-pool substitution reduces crop supply; global/Echo sources change targeting and pack income. Odds/amounts remain unmeasured. |
| Retirement economy | No compensation; remove generation-only cards and measure resulting pools/Infinity | Unclaimed earnings and future obsolete reward text need a final ruling. |
| Platform/save transition | Use released-format audit before selecting migration/reset/legacy route; retain skills/XP and meaningful quests, discard obsolete unlocks/purchased old stat upgrades | No starter-gear conversion or blanket quest reset; avoid unsupported rollback/automatic valid-branch merging. |

The user approves the V4 visual direction, especially the final field alignment; small matching adjustments can follow. Recipe durations/yields/material prices remain proposals. [Seed icon preparation](seed-icon-preparation.md) now provides22 borderless packet variants, one shared unknown and appended drop glyphs with verified Unity rendering. The optional [orchard sketch](optional-orchard.md) now uses evenly spaced rows with dedicated tree dirt plots in a separate area between fields, preserving all six crop beds; orchard gameplay and any fruit economy remain unapproved.

## Linked engineering and validation

- [Concrete seed/economy/migration/transaction contracts and acceptance tests](proposed-contracts.md)
- [Current gameplay and resource economy](gameplay-and-economy.md)
- [Engineering entry points and persistence](engineering.md)
- [Assets and measured geometry](assets-and-geometry.md)
- [Five V4 figures, close-ups and recoverable sources](visuals.md)
- [Small implementation slices, cleanup and outstanding diagnostics](validation-and-rollout.md)
- [V4 evidence and preservation handoff](revision-4/README.md)

Implementation should proceed as pure model/contracts, migration fixtures, one complete bed, readable native bed list plus TownCameraPan focus, staged town content, all 17 art-verified recipes, then recovery/platform/economy gates. Scope estimates are rough review cycles, not promises. Ordinary adventure returns remain unmeasured. The separate cleanup passed64 tests and an offline Mac Player check; four build warnings, the Windows valid-save conflict origin and feedback404 remain documented independently.

## Latest optional orchard comparison

[Two rows of three or four trees](optional-orchard.md) use the separate strip and preserve all six crop beds. The requested full one-tile shift is valid with ground-based draw order; canopy crossings are allowed. Both original scenery trees remain. Six offers two-unit aisles; eight offers one-unit aisles. Column rebalance is explicitly provisional. [Current render controls and ground/path evidence](depth-sorting-correction/README.md) replace the previous failed-fit claim. Orchard mechanics and final capacity remain unapproved.
