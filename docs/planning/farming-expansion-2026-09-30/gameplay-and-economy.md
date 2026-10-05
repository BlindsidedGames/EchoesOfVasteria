# Gameplay and economy

## A complete bounded loop

1. Return from an adventure with materials and eligible seed packs.
2. Hand materials to Barkley for the next visible farm stage.
3. Choose a crop for an empty bed; inspect pack cost, expected yield and time before planting.
4. Plant once. The bed changes from sprouts through growth stages while adventuring, in town or offline.
5. Return to a ready crop. Harvest once, individually or with Harvest Ready. The bed becomes empty.

A bed is one persistent batch. Planted crops never require attendance at a clock time, never spoil, and cannot produce a second batch without another seed pack. Pause, adventure speed multipliers and town animation frame rates should not alter growth. First release has no watering chore, seasons, fertilizer inventory, animals or automatic replanting.

Recommended first-release harvest grants no repeatable Farming XP. Adventures and existing quest XP keep the crop-level gates meaningful. Any one-time tutorial/build XP should be separately authored and cannot loop through planting.

Make Barkley the builder and Flora/Tillman the farm guides. This reuses their actual roles and place in town. The third build quest has a marked/cut work phase, then stump clearance, fencing and soil; its work phase persists after a durable hand-in and grants no additional capacity until completion. Each completed stage is permanent per save slot; crop growth is a separate repeating state within the unlocked beds.

## Existing crops and their actual unlock gates

The game already has 17 Farming TaskData assets with referenced prefabs. TaskWeightService filters adventure tasks by skill. The similarly named crop quest chain spends resources and awards Farming XP; the names do not establish a task prerequisite. Keep the current asset keys intact even where the spelling differs from display text.

| Existing resource key | Farming level | Existing resource key | Farming level |
| --- | ---: | --- | ---: |
| Radish | 1 | Cucumber | 45 |
| Corn | 7 | Leek | 52 |
| Wheat | 13 | Parsnip | 57 |
| Watermelone | 18 | Pepper | 62 |
| Carrot | 23 | Chillie | 68 |
| Spud | 29 | Pumking | 74 |
| Tomato | 35 | Strawberry | 79 |
| Lettuce | 40 | Funion | 84 |
| Turnip | 90 | | |

Evidence: [TaskWeightService](../../../Assets/Scripts/Tasks/TaskWeightService.cs), [ResourceUnlockConfig](../../../Assets/Resources/ResourceUnlockConfig.asset), [FarmingTask](../../../Assets/Scripts/Tasks/FarmingTask.cs), authored assets under `Assets/Resources/Tasks` and `Assets/Prefabs/Tasks/Farming`.

Every player follows the same new game-defined crop requirements, including legacy players. Preserve actual skill levels/XP, not grandfathered crop access. Recommend deriving recipe availability from a reference to canonical plant TaskData, current authored prerequisites and independent fresh pack discovery; no saved RecipeUnlocked flag. The inspected TaskWeightService gate compares SkillController level to TaskData.requiredSkillLevel. All 17 TaskData requirements match the ResourceUnlockConfig UI entries. [Requirements evidence](revision-4/requirements-evidence.json) retains the comparison. Final new requirements remain content authoring for review; obsolete saved unlock data and purchased old stat upgrades are discarded without starter-gear conversion. The [released-format omission contract](../save-migration-feasibility-2026-09-30/unlock-omission-contract.md) identifies meaningful quest/NPC history to retain and the Mildred1 → BuffSlot2 paid-receipt alias; native/runtime migration validation remains separate.

## Seed packs

Seed-packet art exists; seed gameplay assets/balances do not. The approved source is plant drop pools. The former all-gathering work guarantee/token mechanism is withdrawn. [The seed contract](proposed-contracts.md) explains the actual weighted-without-replacement resolver, why direct insertion can replace crop drops, recommended bonus sub-pool routing, surplus handling, task attribution and zero-seed access.

Recommend crop-specific integer packs, one pack per batch, recipe availability derived from new pack discovery/current authored requirements and later packs for planting. Existing levels remain; matching-crop versus eligible-global drop selection and the precise recipe rule need review. Packs are newly discovered by every player, including legacy players; old crop discovery grants/reveals none. The earlier starter grant is withdrawn. Persist new discovery separately from quantity, keeping spent packs known. No new farming variants/art or automatic crop-to-seed exchange are assumed. There is no approved pity timer, useful-pack wait guarantee, work entitlement, token refill or storage cap.

## Barkley's build stages

| Proposed stage | Visible result | Material purpose | Capacity proposal |
| --- | --- | --- | ---: |
| Prepare the beds | Soil and two planted beds within the current farmers' fence footprint | Introductory Log/Stick hand-in | 2 |
| Reclaim the old field | Remove selected existing stumps; fence and prepare a second field south of the windmill | More Log/Stick, modest Stone for a path | 4 |
| Make room southeast | Clear a limited standing-tree edge; connect the track and add the final field | Meaningful collected wood/stone sink | 6 |

Use new stable quest IDs. Do not rewrite old completed quests or reuse their IDs for a different payment. Existing Fence1/2/3 prices are large late-game sinks, not sensible tutorial prices.

Verified old chain: Into The Woods spends 10 Log; A Sturdier Frame 50 Log; Putting up Walls 100 Log; Adding A Roof 150 Log; Finishing Touches 20 Log + 200 Stone, and rewards one percentage point of disciple generation. Fence1 spends 1,250 Stick; Fence2 1,500 Stick + 1,250 Log; Fence3 5,000 Stick + 5,000 Log + 1,000 Slime. Those quests also affect adventure distance and prerequisites. Keep their earned non-farm rewards intact. Evidence: `Assets/Resources/Quests/Barkley` and [QuestManager](../../../Assets/Scripts/Quests/QuestManager.cs).

Logs and sticks already provide distinct sinks. Planks would add a resource, conversion UI, recipes, costs, sorting and migration; show Barkley sawing as construction presentation and defer a playable plank economy. A Construction skill is unnecessary for the proposed three-stage loop.

## Retiring Alter Echoes with no compensation

The current manager permits61 resources:69 authored resources minus eight DisableAlterEcho ingots. Rate is BestPerMinute × DisciplePercent × Cauldron multiplier. Base DisciplePercent1%; authored cards reach+400%; online/offline storage is uncapped. [Manager](../../../Assets/Scripts/NpcGeneration/AlterEchoGenerationManager.cs), [Generator](../../../Assets/Scripts/NpcGeneration/AlterEchoGenerator.cs), [Cauldron config](../../../Assets/Resources/Cauldron/CauldronConfig.asset).

User chose no compensation. Remove recurring production across crop and non-crop categories, without converting old cards, disciple scalar, quest or collection generation rewards to farm yield, seeds or resource payouts. Archive records if migrating; preserve ordinary owned inventory/history. The treatment of unclaimed StoredResources requires an explicit decision and must not be disguised as an approved compensation claim. No reconstructed offline accrual or transition allowance is recommended.

All generation-only RES cards, including 17 crops, need removal from ordinary/grouped/lowest-count pools unless a new forward-looking purpose is separately approved. Future Eva/Barkley hand-ins and collection descriptions must stop promising retired generation. Recommend preserving unrelated rewards without retroactive conversion; review whether their remaining rewards suffice. Pool normalization and Infinity entry must be tested together. [Retirement decision matrix](proposed-contracts.md) keeps those choices separate from the approved no-compensation direction.

## Balance measurement before prices

Measure ordinary fresh, early-farm, midgame and advanced adventures, including abandoned runs. Record materials per productive return, crop consumption by quests, seed drop distributions, pack use, farm output and Cauldron stew conversion. Resource asset debug totals are not pacing telemetry.

Tune introductory construction to a few productive returns; later stages should feel earned without displacing core upgrades. Use authored finite yields, rather than historical BestPerMinute. At fixed capacity, absent-player output is bounded by the batches already planted. Active output is bounded by capacity, growth duration and adventure seed income. Test all three limits rather than one idealized player.

Measure median and low-percentile materials per return, low-percentile useful-pack acquisition and random drought tails, pack surplus/shortage against bed capacity, farm share of quest payments/stew without assuming any transitional compensation. Test fresh-player wood/fence, ore/core and crafting sinks after non-crop retirement: advanced owned inventories can hide an unhealthy new-player economy. Removing all generation-only RES cards also redistributes Cauldron probabilities and can accelerate Infinity, whose eligibility depends on exhausted ordinary pools. Approve intended tasting effort and Infinity timing, not just whether retired cards disappear.

Extra crops also generate stew: CauldronManager converts inventory value into stew using points / 100. Farm output therefore affects card progression even without granting adventure XP. Keep harvest out of BestPerMinute, gathering statistics, seed acquisition and tier rolls unless separately approved. ResourceManager.Add still applies existing resource-tier yield bonuses with tracking flags disabled; specify this once and display the same payout the transaction grants.

## Quantitative recommendation

[The capacity scenarios and retirement decision matrix](proposed-contracts.md) compare2/4/6/8 beds,30-minute and2-hour growth,1/4/12 useful packs per hour and baseline10-unit yields. These are continuous-active-play scenarios; seed income is per active adventure hour while growth uses elapsed time. The duty-cycle example in the contracts converts income before estimating town/offline output. Six and eight have equal quick throughput in the high-seed scenario, but eight increases long-growth and unattended output. This supports six as a bounded visual expansion, not a universal balance optimum. Ordinary-return rates remain unmeasured; authored Radish drop math and synthetic profile limits are documented rather than promoted into player pacing facts.
