# Bounded farm receipt journal

The development farm now uses a per-save sequence journal. Completed known operations collapse into a durable contiguous watermark; unpaid fixed-roll intents and committed operations beyond a gap remain saved. This replaces unbounded UUID receipt/tombstone growth without changing pack chance, yield, elapsed growth or the production Main scene. These changes are uncommitted and require review before integration.

## What is bounded, and what is preserved

Each bank has a lineage and monotonically issued sequence. Economic candidates reserve UI sequences after mutable save contributors have been captured. A rejected command or failed write consumes no UI sequence. Adventure completion reserves its sequence together with its fixed hit/miss intent, so ordinary snapshots can preserve completed work before a successful credit transaction. Successful candidates remove only their matching intent. Compaction advances only through a recognized contiguous committed prefix; an outstanding gap cannot be discarded by a time or size limit.

Replaying an issued committed token returns AlreadyApplied and pays nothing. A stored hit/miss conflicts with a changed selection and cannot reroll. Stale intents next to an existing durable receipt are acknowledged without another seed payout. Tokens below the watermark no longer retain individual historical fingerprints; this is replay protection for issued commands, not cryptographic authentication of deliberately forged save graphs or externally restored older valid saves.

The legacy UUID namespace closes during migration. Only explicitly saved pending UUID intents may settle; fresh or previously completed UUIDs cannot re-enter. Matching already-paid legacy intents are cleaned without payout. Conflicting records and unknown history remain intact for diagnosis. A saved active legacy batch can harvest once through its exact saved whitelist; a live already-paid marker still prevents payout. New beds retain their last harvested plant sequence, so old or uncommitted batches cannot be reintroduced for a second harvest.

Recognized completed history is bounded by the watermark plus sparse gaps and per-bed paid sequence. Pending work and unknown legacy records deliberately have no destructive cap. A permanently broken write path can still accumulate outstanding intents; compaction cannot promise constant storage for unpaid work. Unknown future formats, ambiguous legacy sequence state, invalid bounds and sequence exhaustion fail closed.

## Growth and saves

Mutable save-contributor capture updates only the two growing original beds' elapsed clocks. It does not clone the historical journal, allocate economic receipts, debit seeds or publish rewards. Pure economic candidates still clone state before immutable serialization. Ready or inactive beds skip clock work. Oracle retains the existing synchronous transaction lane, write drain, immutable capture, durable commit, publication and Loading-boundary recovery.

Pause/focus settlement and local rollback rebasing remain unchanged. Finite batches cap each settlement; repeated intentional forward clock edits can still accelerate successive paid batches. No server infrastructure or anti-cheat claim is added.

## Performance evidence

The before/after fixture uses the same Mac managed Mono host, two beds, 31 growth samples and five serialization samples. It tests copied pre-change source against current source; these are not Unity frame timings, full disk I/O or mobile/AOT results.

| 50,000 completed historical operations | Before | After migration |
| --- | ---: | ---: |
| Serialized fixture bytes | 13,269,704 | 20,632 |
| Growth capture median | 9.0076 ms | 0.0003 ms |
| Serialization median | 65.8596 ms | 0.1714 ms |
| Individual known historical receipts retained | 50,000 | 0 |

The one-time 50,000-record upgrade took 9.9988 ms. A separate 50,000 new completion stress run took 357.6469 ms, produced exactly 5,000 selected seeds, advanced the watermark to 50,000 and retained no individual receipts. Balance/serialization roundtrips stayed exact. Unknown records and pending gaps are outside the compacted fixture.

## Source and validation

The focused implementation is in FarmJournal, FarmState, FarmCommands, FarmService, ContinuousTask and the current-collection migration. Tests cover fixed rolls, gaps, cancellation, ordinary-save/reload retry, stale paid intents, conflicting/unknown legacy records, foreign tokens, exhausted/invalid journals, old-batch replay, one-time harvest and ready-bed avoidance. The focused CLI suite passes 37/37; Unity EditMode passes 121/121 and PlayMode 51/51. An independent source review found no normal-path replay, reroll or lost-intent defect after the final encoded-roll guard.

Raw test XML, source hashes, before/after benchmark drivers/results, focused patch and review evidence are retained at `../Echoes Farming Implementation Evidence/2026-10-01/receipt-retention`. Actual refreshed Player phases and final preservation status are recorded in the final handoff beside this document. A process exit before any successful durable snapshot cannot preserve memory-only intents.

## Production decisions still separate

The 10% chance is approved behind a farming quest gate. Current TaskData/DropResolver only supports skill gates; QuestManager.IsQuestCompleted is the reusable existing quest check. The proposed Farm.FirstBeds quest after Tracking Twins needs an authored identity and material/work requirements, with no seed requirement. Check its committed completion before seed RNG/staging and fail closed if content is missing. The development PrepareBeds receipt is not production quest proof. No pre-gate production drop path, additive/global pack wiring or adventure Echo integration is approved by this journal change.

The native landscape UI and consistent existing-art soil correction are documented in [ui-soil-review.md](ui-soil-review.md). Portrait uses a shared 16:9 safe-area convention and requires an explicit mobile layout review. Installed native Mac support is Mono only; iOS, Android and Mac IL2CPP modules are absent. No modules, accounts or grants were changed.
