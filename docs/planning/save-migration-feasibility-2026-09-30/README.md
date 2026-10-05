# Released-save migration feasibility, 30 September 2026

**Verdict: migration is technically viable for the tested schema-1 lineage after narrow preservation fixes. The current checkout is not safe to release as a preservation migration. A mandatory reset is not established by the evidence.** Mobile remains a conditional recommendation because released mobile artifacts and device upgrade fixtures are missing. Steam can retain a legacy branch, but that branch needs a deliberate save/cloud separation policy.

This investigation changed only this report/evidence area and a new private investigation directory. It did not change production code, scenes, original saves, Unity state, or release software. All executions were command-line Mono/Roslyn probes against copies; no Editor or Player launched. The parent retained control of Editor access. The farming system remains a proposal, not implemented save schema 4.

**Earlier/mobile extension, 1 October:** [historical-lineages.md](historical-lineages.md) covers known iOS releases, source transitions, 45 backed-up ES3 files and two string-era binary archives. Skills predate the October overhaul; all 210 present ES3 skill records and 12 older binary records retain exact levels/XP in isolated adapter roundtrips. Each older binary archive contains 48 milestone strings. Current loading still lacks the ES3/representation-aware adapter.

**Resolved product contract:** current game-defined eligibility applies to everyone; no old crop-access grandfathering. Omit obsolete requirement/derived unlock data and purchased stat upgrades from the new format, with no gear compensation. Preserve skill levels/XP, meaningful quests and player selections; everyone discovers packs fresh. [unlock-omission-contract.md](unlock-omission-contract.md) audits exact fields/consumers and the bounded omission experiment. Requirements already live outside current GameData; cached milestone TierIndex is derivable, whereas Earned, NPC/quest history and earned buff capacities are not definition thresholds.

**Buff slots still reach five:** one initial plus four sequential +1 quests. The old fixture's two-versus-one reconstruction mismatch is a proven quest-ID rename, Mildred1→BuffSlot2, retaining the same asset GUID/reward. Translating that receipt restores the correct two-slot reconstruction; the remaining three quests reach five. This supports deriving capacity from current definitions and translated player history, without replaying rewards or requiring an already completed quest to be paid again. [Exact chain/evidence](evidence/buff-slot-history.json).

## What was actually released

| Platform | Evidence | What remains unknown |
| --- | --- | --- |
| Steam Mac | Installed app manifest: **public build 20851856**, depot 2940002, manifest 1239504221550847585; local install updated 2026-01-27. App plist: **1.4.3**, Unity 6000.2.6f2. Actual Assembly-CSharp.dll and serialized Cauldron configuration were inspected. | Whether this remains the server's latest public build; its exact source commit; Windows/Linux depot contents. |
| iOS/iPadOS | Public [App Store version history](https://apps.apple.com/us/app/echoes-of-vasteria/id6749265146): **1.4.3, 19 November 2025**. | Exact build number, signed artifact, native/AOT serialization behavior, device saves and container upgrade behavior. |
| Android | [Google Play listing](https://play.google.com/store/apps/details?id=com.blindsidedgames.echoesofvasteria&hl=en_US): updated **18 November 2025**. | The fetched page does not expose an exact version name/code. No released APK/AAB or device fixture was obtained. |

Current ProjectSettings still say 1.4.3 / mobile build 160; these settings do **not** prove that build 160 was deployed. There are no repository release tags. Commit `7faf9d4c5` (version bump, 19 November 2025) is a source comparison candidate, not a proven deployed source hash. Installed Steam artifacts provide the stronger serializer/config reference. Artifact identities and hashes are in [release-evidence.json](evidence/release-evidence.json).

The local real-save backup is also qualified: Save1 has header/payload schema 1, LastGameVersion 1.4.3, GameVersionCreated 1.4, and a July 2026 timestamp. It is not proven untouched since release, or a mobile save. The released serializer successfully decodes it. Its values therefore demonstrate compatibility/preservation failures, but cannot establish mobile deployment provenance or general player balance distributions.

## Ranked blockers and bounded corrections

### P1 — Typed milestone selections disappear before migration

The released model stores `SkillProgress.Milestones` as typed records. Current [GameData.cs](../../../Assets/Scripts/Blindsided/SaveData/GameData.cs) renames that serialized field to `MilestoneRecords`, adds `[PreviouslySerializedAs("Milestones")]`, and exposes a `Milestones` property. In the tested runtime serializers that alias does not retain the payload: **23 records become null/empty**, while skill XP and level still load. `SkillController.LoadState` uses saved `Id`, `IsActive` and `TierIndex` to restore selections; deriving available milestones from level does not recover those selections.

This is reproduced with freshly compiled current GameData, the actual legacy payload, default Odin binding, and both repository NoEditor and NoEmitAndNoEditor serializers. A stale cleanup validation assembly was identified and excluded as final evidence. Removing only the property also failed; do not infer a specific alias-collision cause from this test.

**Demonstrated correction:** retain the original typed serialized field name `Milestones`. In an isolated model copy, all six complete skill objects—including every record's Id/IsActive/TierIndex, XP and Level—equal the released decoder's values after load/save/reload. Combining that field strategy with current schema migrations and the corrected Cauldron profile also preserves all 23 records through schema-3 migration/save/reload. Three normal 1.4.3 backups and three 1.4 beta backups decode with exact skill equality under the retained-field variant.

**An even smaller correction also works in the isolated test:** replace the Sirenix attribute with `[UnityEngine.Serialization.FormerlySerializedAs("Milestones")]`, keeping both the new field and compatibility property. With the verified Cauldron-profile correction, all 23 typed records and every original skill field survive schema-3 migration/save/reload exactly; resource inventories, equipment and generator archives also equal the released decoder's values. Prefer validating this one-attribute correction first; retaining the original field is a demonstrated fallback. This establishes which attribute works in the tested serializer, without claiming a fully diagnosed internals/stripping cause.

Earlier string-era milestones need a compatible reader/DTO adapter. Two archives decode with zero typed records, but **the historical model recovers 48 string IDs from each**. The isolated adapter preserves Level/XP. Under the resolved current-definition rule, omit those automatic eligibility strings from new saves and recompute eligibility/effects from retained levels. Typed player Id/IsActive choices still require safe legacy decoding; their cached TierIndex can be omitted afterward. The earlier exact-record tests prove ingestion, not a requirement to keep derived tiers forever. Native/AOT and runtime effects remain untested.

### P1 — Schema-2 repair converts legitimate Cauldron investment

[SaveMigrationRunner.cs](../../../Assets/Scripts/Blindsided/SaveData/Migrations/SaveMigrationRunner.cs) registers `SchemaV2ZZCauldronOverflowRepair` with TargetSchema 2. A schema-1 save runs it regardless of LastGameVersion 1.4.3. That repair invokes [Migration_CauldronOverflowRedistribution.cs](../../../Assets/Scripts/Blindsided/SaveData/Migrations/Migration_CauldronOverflowRedistribution.cs), whose hardcoded maxima are **500 resource / 300 buff**.

The actual released Steam `resources.assets` has the length-prefixed threshold arrays immediately following the serialized CauldronConfig name and its 10f/1f tasting settings. They match both the candidate release source asset and today's [CauldronConfig.asset](../../../Assets/Resources/Cauldron/CauldronConfig.asset): maxima **10,000 / 3,000**, not the C# field defaults. [released-config.json](evidence/released-config.json) records the byte offsets and full arrays.

| Real backed-up save | Before | Current migration | Verified-release-profile correction |
| --- | ---: | ---: | ---: |
| 61 resource-card keys | 610,000 | 30,500 | 610,000 |
| 10 buff-card keys | 30,000 | 3,000 | 30,000 |
| Infinity cards | 1,371,876 across 7 keys | 1,978,376 across 8 keys | 1,371,876 across 7 keys |
| All cards | 2,011,876 | 2,011,876 | 2,011,876 |

The stock repair moves **579,500 resource cards + 27,000 buff cards = 606,500** into Infinity. Under current CardTierCalculator/config, resource tier 8 becomes tier 4 (400% power bonus becomes 75%); buff tier 8 becomes tier 5 (100% cooldown reduction becomes 40%, effect bonus 30% becomes 15%). Adding Infinity also changes hero statistics. Equal total cards is not semantic preservation, and this is unrelated to approved Alter Echo compensation removal.

**Demonstrated correction:** a disposable copy substitutes the verified 1.4.3 profile maxima 10,000/3,000. The same runner then preserves every resource and Infinity key/value exactly, and every buff key/value after its explicit Slipstream→Prospector rename. Production should select an immutable, verified release profile or defer ambiguous overflow repair; do not consult mutable C# defaults or assume every older release shared these thresholds. The historical version migration is separately skipped/ledger-stamped for schema-1 saves already at 1.4.3, but that does not skip this new schema repair. Existing schema-2/3 saves with this repair already stamped require a separate recovery investigation; raising the constants cannot reconstruct previously redistributed per-card balances from aggregate Infinity alone. Recover from preserved pre-migration snapshots when available.

### P1 release gate — Retirement is a semantic change requiring explicit preservation rules

**No Alter Echo compensation is approved.** No invested card/quest/scalar value should become farm perks, seeds, currency or a transition allowance. Preserve existing resource Amount/Earned/Tier/BestPerMinute, gear, XP, quest receipts, statistics and historical records unless a specifically approved retirement rule changes their future use.

`Disciples.StoredResources` contains already generated, unclaimed holdings; `TotalCollected` is history, not a payable balance. The farm plan correctly leaves **unclaimed earnings unresolved** and archives their source values pending the ruling. Decide collection before cutover, exact existing-balance settlement, or explicit forfeiture before shipping. Do not silently delete them or reconstruct months of offline production. Any approved claim needs an idempotent receipt and the economic save barrier; repeating a migration must not pay again.

Retire generation and new RES-card rolls atomically with the new model; retaining archived records must not leave old generators accruing or claiming on load. Retain original Cauldron counts, paid fence history and completed quest records as evidence even when their future benefits are deliberately retired. The current migration renames Slipstream slots/cards/cast counters to Prospector, but current BuffRecipe marks both names retired and BuffManager clears their slots/autocast on load. There is no active Prospector asset. Treat this as an explicit retirement/archive rule, not a replacement reward. Existing unrelated buffs/cards and Infinity must retain their values.

### P2 — IDs largely survive, but progression meaning changes

The candidate-source comparison found unchanged identities for **69 resources, 49 task IDs, six skill names and 23 milestone IDs**. ResourceManager saves by resource asset **name**, not numeric resourceID; Cauldron IDs also embed names. Quest keys are authored questId strings. Keep those stable or provide explicit aliases across inventory, statistics, generators, card keys and quest requirements.

Quest assets go from 96 to 94: Slipstream30/45/60 retire and Rattle ‘n’ Roll is added. The real save has 97 quest records, including historical keys. Current QuestManager's completion audit counts all completed saved records against the current asset count; obsolete completions can falsely satisfy a new cohort. The farm plan's explicit 94-ID membership rule addresses this design issue but still needs implementation. Archive old records; intersect numerator, denominator and timestamps with the same declared cohort; keep expansion grants separate from actual hand-ins and historical leaderboard uploads.

Crop-unlock quests gained XP rewards and skill assets gained level-based resource maps. Preserve meaningful completed/incomplete quest history and earned inventory; do not replay rewards. **Everyone discovers packs fresh. Current adventure requirements apply to everyone:** Farming 57 with completed Pepper/Chilli quests is now below the task levels 62/68, an approved eligibility change rather than save corruption. Preserve the receipts and actual XP without an old-access override. Onion/task37 coexisted with Funion/task44; do not rename them. Fence receipts can document paid construction without fabricating quests. Farm state/transactions remain implementation work. [source-id-audit.json](evidence/source-id-audit.json) qualifies the November comparison; the [historical audit](historical-lineages.md) and [omission contract](unlock-omission-contract.md) distinguish approved deletions from meaningful player history.

## Save architecture and rollback evidence

The deployed Steam model has schema 1 and Odin binary snapshots with the legacy schema/timestamp/build/size/HMAC header. Current GameData schema is 3; the new snapshot **format version 4** is a separate concept. SaveManager adds immutable generation files, SHA checksums, lineage/parent-write IDs and authoritative SlotState records, while retaining legacy snapshot.bin/tmp/prev1/prev2 readers. Legacy HMAC is treated as unverified; Oracle requires a verified write before publishing migrated or recovered data.

The exact current reader was compiled in a CLI with its existing root override and exercised only disposable directories:

- Valid primary → Success, legacy integrity unverified.
- Truncated primary plus valid rotation → Recovered, same schema-1 data.
- Only truncated primary → Corrupt, no data.
- Future header schema 999 → UnsupportedNewer, no data.

The current migration runner succeeds, produces schema 3, is unchanged on a second run, and leaves its input graph untouched. An injected migration that clears inventory then throws is rejected; the same original object/values are returned. These are genuine data/reader checks, not full native publication or crash-at-commit tests. The test envelopes deliberately reuse a copied legacy header; they do not demonstrate new-format durability or claim that a released game wrote schema 3.

ResourceManager, SkillController, EquipmentController and GameplayStatTracker retain unavailable records while updating known entries; native effects still need validation. Current loading does not import `.es3`; ES3-only banks can appear NotFound. All 45 fixtures parse in the isolated adapter, without establishing mobile provenance or a shipped importer. Known September mobile source candidates already use Odin; device artifacts remain missing. TE1 is not an ES3 adapter. Six ES3 fixtures have positive stat purchases/no gear: the user approves discarding those purchases, without conversion/compensation. Read their legacy shape safely, then omit the old fields from new payloads; preserve actual equipment separately.

Searches of local skills, Codex memories, relevant Computer History summaries, repository documentation and commit messages found **no authoritative original mandatory-reset rationale**. The available history is recent and incomplete. This absence does not prove a reset was never chosen; it means neither technical impossibility nor an old balance-reset decision should be invented as its justification.

## Recommended staged test and rollback plan

1. **Pin releases and fixtures.** Obtain App Store/Play Console build identifiers, exact deployed artifacts and anonymized early/mid/advanced real device saves. Include a dormant older save, all three slots, beta separation, pending claims, changed quest chains and cloud conflicts. Archive original bytes with hashes before any candidate writes. Assign the next update a distinct app version and schema; do not keep 1.4.3 as the only identifier.
2. **Implement the two preservation fixes in separate reviewable work.** Add the typed-field/adapter path and a verified Cauldron profile rule. Test exact keys/fields/values, derived tiers and equipped/runtime effects, not aggregate counts. Test interrupted/repeated migration and backups from before the faulty repair; do not reverse guesses from inflated Infinity.
3. **Implement approved omission and remaining retirement rules.** Apply current eligibility, omit redundant unlock data/stat purchases without compensation, retain actual levels/XP and meaningful quest/player state. Resolve StoredResources, validate historical reward-ID aliases and translate meaningful old objective fields. Preserve paid construction/cohorts; initialize only approved farm entitlements. Packs stay hidden until earned/discovered in new gameplay. Old timestamps grant no new batch/offline yield. Zero seeds remains playable through adventures.
4. **Run native upgrade and transaction tests.** Install each actual old build, create/restore a disposable save, update in place to the candidate with the same platform identity, and load/autosave/relaunch. Cover background/kill at each durable-write/publication boundary, low disk/write failure, corrupt primary, rotation recovery, mixed slot authorities, duplicate commands, clock rollback/advance, import, slot switch and cloud restore. Expected outcome is exactly one authoritative economic result or explicit recovery, never a silent fresh save or duplicate harvest/claim. Validate iOS/Android AOT/stripping and app container paths on devices.
5. **Stage distribution and retain a repair route.** Start with an opt-in Steam/test cohort and mobile test tracks, then a limited mobile rollout after the fixture matrix passes. Keep pre-upgrade archives outside ordinary generation pruning. Pause rollout on a preservation mismatch; fix forward using those archives and explicit receipts. Restoring an old executable is not a general rollback of an already migrated schema. Any recovery that reverts new progress must be explicit and reviewable.

Phased distribution limits exposure rather than undoing migrations. Apple's phased release still permits anyone to download the update manually; pausing automatic rollout is not complete containment. Google Play halting a staged rollout leaves users who already updated on that version. Prepare a corrected forward update and recovery path before releasing. [Apple phased-release documentation](https://developer.apple.com/help/app-store-connect/update-your-app/release-a-version-update-in-phases/), [Google staged-rollout documentation](https://support.google.com/googleplay/android-developer/answer/6346149?hl=en).

## Steam legacy branch versus mobile migration

[Steam branches](https://partner.steamgames.com/doc/store/application/branches) allow players to select a retained legacy build. [Steam Cloud](https://partner.steamgames.com/doc/features/cloud) synchronizes configured app files; branch selection is not an automatic separate save bank. The installed released reader's IL has no future-schema guard, and it reads snapshot.bin/rotations rather than the new authoritative generation tree. Sharing the directory can produce two diverging progressions: legacy advances old snapshots while the new build advances generation files. Feeding a new payload to the old reader may also discard unsupported fields.

| Option | Practical consequence |
| --- | --- |
| Mobile migration plus optional Steam legacy branch | Best fit for preserving existing mobile players, after the above fixes/device tests. Old accounts keep inventory/progress while future idle production retires. Maintain a repair-capable update and pre-upgrade archives. |
| Steam-only new update, legacy branch retained | Lets mobile remain on its released game and Steam players choose the old economy. Requires preserved platform depots, explicit save-root/PlayerPrefs/cloud isolation, and a one-time import policy. It does not itself solve migration or let old/new gameplay progress merge. |
| Mandatory fresh starts for the new build | Lower initial semantic migration effort, but knowingly abandons continuity and still requires protecting old banks. This is a product decision, not a requirement established by the format investigation. |

Recommendation: proceed with migration engineering behind fixture/release gates; retain a Steam legacy build as an additional compatibility option if desired. If mobile artifacts/fixtures cannot be obtained before the intended release, a Steam-only rollout with isolated old/new save banks is the defensible temporary fallback. Do not make an irreversible mobile reset to work around these two demonstrated, bounded defects.

## Evidence and reproducibility

[probe-results.json](evidence/probe-results.json) contains hashes and exact preservation checks; [fixture-matrix.json](evidence/fixture-matrix.json) qualifies eight decoded backups; [backed-up-snapshot-manifest.json](evidence/backed-up-snapshot-manifest.json) records schema/version/hash provenance. The [reproducer](evidence/run-probes.py) and three C# probes run outside Unity and never invoke live SaveManager writes. The [lineage reproducer](evidence/run-lineage-probes.py), [ES3 results](evidence/es3-results.json), [historical binary results](evidence/string-era-binary-results.json) and [crop map](evidence/crop-lineage-map.json) support the earlier/mobile extension.

Private raw/materialized evidence is under `/Users/matthewrushworth/Projects/Echoes Migration Investigation/2026-09-30/`; repository evidence contains no full player inventories or deployed assemblies. See [evidence/README.md](evidence/README.md) for the verified invocation and scope. No commits, pushes or deployments were performed.
