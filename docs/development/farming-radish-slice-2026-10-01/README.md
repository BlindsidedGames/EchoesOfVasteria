# Original two-bed Radish development slice

This is an uncommitted implementation for review, isolated in `Assets/Development/Farming/Main.unity`. The production Main scene remains intact. The new save projection and compatibility code are shared source changes; do not publish this checkout or use real saves in Editor Play before reviewing the migration gates below.

The native Hub route opens a readable Farm list. Barkley's development preparation step pays 10 Logs and 20 Sticks together with its completion receipt, unlocks the original west/east beds, and consumes no legacy Fence ownership. Primary-hero Radish adventure completions can discover matching seed packs; no starter grant, compensation or inherited crop discovery exists. A pack plants one finite batch. Growth uses measured active time and elapsed offline time, capped at that batch; Harvest Ready pays both ready beds atomically and never creates adventure/seed credit.

**Current development tuning:** 10% per completed primary Radish task, 30 minutes per planted bed, 10 base Radish per bed. The user approved the 10% chance behind a farming quest gate; the exact authored gate is pending and this development slice does not yet implement it. Growth and yield remain provisional. The smoke harness forces its rolls and shortens its recipe to exercise the loop; it does not measure ordinary adventure returns. Actual resource tiers remain saved, but this first slice pays the stored base batch yield without an additional tier multiplier. Other crop pools, production Barkley quest content and Alter Echo generator cutover are outside this slice.

## Save and migration behavior

- Schema 4 writes retain actual skill Level/XP and active chosen milestone IDs. Authored current requirements supply availability; obsolete automatic/cached unlock tiers and purchased old stat upgrades are omitted. No starter gear conversion or compensation is issued.
- Explicit legacy ES3 import tolerates the actual unquoted integer task keys, ingests typed skill records, and rejects ambiguous GameData wrappers. It never selects or repairs an old bank automatically.
- Completed Mildred1 is accepted as the paid BuffSlot2 receipt without replaying costs or rewards. The old record/timestamp stays preserved, canonical reward guards prevent a second retroactive payout, and unmatched already-earned buff capacity survives.
- Cauldron overflow uses the verified Steam 1.4.3 profile (10,000 resource cards / 3,000 buff cards). Unknown producer versions preserve counts and return a warning. A prior incorrect redistribution cannot be reconstructed from an aggregate Infinity balance.
- Quest completion counts unique current authored IDs on both sides. Historical/unknown records stay saved but do not inflate completion; the development preparation receipt does not enter the production cohort.

## Transaction and recovery contract

Oracle owns one synchronous main-thread economic lane. It drains prior writes, captures mutable contributors, prepares a separate candidate, commits immutable bytes, publishes touched state/resource caches, then releases deferred saves. Seeds, batch IDs, inventory debit/payout and receipts share the snapshot. Candidate failures leave live inventory unchanged. After a durable commit, a publication failure suspends gameplay at the existing Loading recovery boundary and blocks stale saves. Native Retry reloads the durable receipt without another payout.

A completed eligible task records its fixed hit/miss intent in the loaded save tree before ordinary completion callbacks. A captured service/owner/slot handle commits in `finally`, so thrown callbacks cannot lose its intent and a slot switch cannot pay another bank. Failed writes leave that owner-scoped intent available to routine snapshots and the automatic retry. Reload retries the same selection; neither slot changes nor a disabled task cause a fresh roll. Successful candidates consume only their matching intent. A process exit before **any** successful snapshot cannot preserve work that never became durable. The [bounded sequence journal](receipt-retention.md) now compacts completed known history while retaining fixed-roll pending work, sparse gaps and unknown legacy records. Performance and replay evidence are linked there.

Pause and focus are tracked separately. Active growth settles before suspension and resumes through UTC only once both signals permit it. Clock rollback rebases without losing accrued work; repeated intentional future-clock edits can still accelerate successive paid batches. This is a proportionate local development policy, not a server-backed anti-cheat guarantee.

## Validation status

**Player validation passes.** Evidence is retained outside the Assets tree at `../Echoes Farming Implementation Evidence/2026-10-01` relative to the project folder.

- Online/native run: fresh hidden packs, actual primary Radish task completion, preparation costs, two pack debits, both ready beds, atomic +20 Radish, repeat rejection and no harvest-to-seed recursion. Forced first-task capture failure retained its fixed intent; a routine disk save captured that unawarded intent and automatic retry credited it once.
- Month-offline/reload run: two finite ready beds, persisted zero seed balance with discovery retained, one +20 harvest, repeat rejection and exact final disk state.
- Publication failure: a +10 harvest committed before an injected inventory-cache callback failed. Recovery became required, stale autosave was blocked, and disk reload retained the empty bed/payout/receipt with no second harvest.
- Pause/focus permutations: focus alone cannot resume a paused app, and clearing pause cannot resume an unfocused app. Growth resumes only after both permit it.
- Seven actual 1280×720 Metal captures were inspected independently. The separate preview path tilemap was copied into the development scene after the first visual check exposed missing approaches. Both brown paths now join the opening/door; Radish sprites stay on soil and clear entrances. No concept image was substituted for Player evidence.

All four reviewed Player phases pass. The publication run deliberately emits and retains one exact injected failure diagnostic; unexpected gameplay errors/warnings are empty. Raw teardown still contains Mono thread-finalization messages and the pre-existing ComputeBuffer disposal warning. The empty-Player comparison attributes the latter to Unity 2D Animation's static fallback buffer, with no farm/SpriteSkin/Oracle instances; diagnostic release removes it. No vendor patch or diagnostic suppression was shipped. See [the focused review](review.md).

Verified at the latest checkpoint:121/121 Unity EditMode cases; 51/51 Unity PlayMode cases; 54/54 focused migration cases; all 45 copied ES3 banks and eight related binary snapshots read/roundtrip without modifying inputs. The binary samples are related historical backups, not independent device upgrade cohorts. The native arm64/Mono Mac Player builds with zero errors and two configuration warnings. IL2CPP/AOT mobile builds, signed device containers, in-place upgrades and Windows gameplay remain separate gates.

## How to review safely

Use the latest disposable offline Mac Player `receipt-retention/EOVFarmRetentionReview.app` from the evidence directory. The earlier `review/EOVFarmDevelopmentReview.app` is retained for before/after evidence. The first delivered `EOVFarmDevelopment.app` predates these review fixes and is retained as historical evidence. The latest Player uses `/tmp/eov-farm-retention-20261001/saves`, a unique product preference namespace, and compiled service guards. Its default development scene has the authored 30-minute recipe and 10% chance; automated smoke flags override those values only in memory. All 12 source/settings isolation guards were restored exactly and temporary validation helpers were archived outside Assets and removed. Production Main remains open in Edit mode, clean. All 77 protected player files and 93 backup files match their original hashes; prepared icon assets and both atlas tables also match their verified baseline. The earlier 5,392-file comparison belongs to the first checkpoint, before the additional review fixes. Opening the duplicate scene alone does **not** isolate Editor saves or cloud services; do not enter Play against real player banks.

The accepted V4 farm placement, soil, corner/end pieces and dual gate approaches are copied only into the development town. Actual Radish world growth sprites are bound locally; no existing adventure task art has been globally rewritten. Orchard remains a separate optional area and has no implementation here. No commit, push, merge or release is authorized.

## Playable evidence

These are original Player captures, not repainted previews:

| View | Local image |
| --- | --- |
| Fresh discovery and preparation | [Undiscovered farm](images/undiscovered-native-farm.png) |
| Two planted batches | [Native planted list](images/planted-native-farm.png) |
| Early world growth and approaches | [Growing field](images/growing-world-farm.png) |
| Harvest enabled | [Native ready list](images/ready-native-farm.png) |
| Mature world growth and approaches | [Mature field](images/ready-world-farm.png) |
| One-month finite settlement | [Offline ready list](images/ready-after-offline-reload.png) |
| Once-only harvest | [Empty beds](images/harvested-native-bed-list.png) |

The final empty-list message is the intentional repeat-harvest rejection. The pack art remains discovered at zero quantity.

## Focused source review

| Area | Files and reason |
| --- | --- |
| Compatibility ingress/current projection | `CurrentSaveCodec`, `LegacyEs3Adapter`, `GameData`, `SaveImportExport`, one serializer call in `SaveManager`; accept real historical records and intentionally omit obsolete current-write caches. |
| Historical compatibility | `LegacyCauldronProfile`, schema-4 compatibility migration, migration interfaces/runner and overflow migration; verified profile caps, paid quest alias, no compensation. |
| Farm authority | `FarmState`, `FarmCommands`, `FarmTransaction`, `Oracle.FarmTransactions`, bounded additions in `Oracle`/`ResourceManager`; immutable candidate, durable fixed-roll journal and exactly-once inventory publication. |
| Slice entry/presentation | `ContinuousTask`, `FarmService`, `FarmView`, `ToolkitFarmScreen`, `TownWindowManager`, `TownCameraPan`, duplicate development scene/navigation; actual Radish source, accepted town visuals and native bed controls. |
| Completion/tests | `StaticReferences`, `QuestManager`, codec/migration/cohort/farm tests; same authored completion cohort, test Config DLL reference, two historical PlayMode expectations aligned with verified profile. Prior Mac/art/test routing work remains preserved. |

`focused-existing-source-diff.patch` and the worker source hashes in the evidence directory support the review. No whole-checkout reset or cleanup was performed.

## Remaining gates

There is no blocker to reviewing this playable slice. Before production integration, decide matching/global plant-pack pools and measured rates; validate release growth/yield/tier treatment, mobile journal/write cost, real Barkley quest staging and its pre-roll gate, and Alter Echo retirement including already-earned unclaimed stock. Exact mobile/AOT/in-place upgrade and Windows device evidence is still required. Real Windows Save1 authority conflict and feedback HTTP404 remain separate unchanged diagnostics.

Steam app 2940000's installed manifest and local app cache (September 28) expose only `public`, build 20851856. [Public branch metadata](https://steamdb.info/app/2940000/depots/) agrees. No beta branch is verified; hidden/password branches require authenticated Steamworks inspection. No branch or release was created.

## Library copies

All seven actual Player image identities are preserved and refreshed to version1 by the latest follow-up; approved concept/icon identities remain unchanged.

| View | Library ID |
| --- | --- |
| Undiscovered | `libfile_fe05889466088191af54cf7b432cacaf` |
| Planted native list | `libfile_95b326d882748191b2d134b0107ce95b` |
| Growing world | `libfile_b6edae6f65488191bd663a325bbed186` |
| Ready native list | `libfile_289c4a6ded3c8191be7bba8d449102aa` |
| Mature world | `libfile_367bda1e944881919e4a4502b7351190` |
| Offline ready | `libfile_1c9f8a0362e08191aaa7e44ce581cc60` |
| Harvested list | `libfile_926bfda670dc81918adf4aaf12a9de1d` |


## Plant-pack integration follow-up

See [the read-only integration review](integration-review.md) for all 17 authored crop pools, exact Radish packet binding, proposed typed drop wiring, adventure Echo eligibility, bounded receipt measurements and installed mobile/AOT limits. Its recommendations do not alter the development values or production gameplay.

## Latest follow-up checkpoint

[Bounded receipt retention](receipt-retention.md), [native UI and soil review](ui-soil-review.md), and [tree-tier/quest-gate audit](tree-tier-proposal/README.md) record the current follow-up. The [final combined follow-up](followup-handoff.md) is authoritative for current test counts, performance, actual Player renders and preservation. Earlier raw evidence remains retained.
