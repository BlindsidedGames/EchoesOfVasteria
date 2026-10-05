# Earlier and mobile save lineages

Extension of the [migration report](README.md), investigated without Unity, production edits or original-save writes. **The evidence supports adapters rather than a mandatory reset, but does not establish that every released mobile version upgrades safely.** Tested decoding/roundtrips and a supported in-place platform upgrade are different claims.

## Release and format compatibility matrix

| Lineage | Actual release evidence | Format/skill evidence | Current support and remaining work |
| --- | --- | --- | --- |
| Before the repository import | The first imported project is June 2025; earlier public deployments are not established. | `8262c6ef9` imports an existing project, so Git is not its complete origin history. No pre-import fixture obtained. | Do not invent a universal old schema or infer that pre-import players lacked progression. Obtain artifacts/fixtures if this cohort is supported. |
| Early Steam / August 2025 | [Steam listing](https://store.steampowered.com/app/2940000/Echoes_of_Vasteria/) gives release date 10 August 2025, without a deployed source hash. | Source on that date uses ES3; backups have July/August dates, embedded `Data*`/`Beta*Data*` keys, six skill names and string milestones. They lack schema/producer-version fields. These are real backed-up files, **not proven Steam-release or mobile-device fixtures**. | Current reader does not import ES3. An ES3-only bank can appear absent. A bounded parser/DTO adapter is demonstrated below; routing, publication and old economic semantics are not implemented. |
| Local binary archives labelled 1.2.12 and 1.2.16 | Headers are direct fixture evidence, not proof of platform distribution. | Schema 1; historical string-era decoder recovers six skill levels/XP and **48 milestone strings in each archive**. One payload has no LastGameVersion; the other says 1.2.16. | Same schema number does not identify milestone representation. Shape-aware or historical-DTO decoding is needed. Asset-dependent gear/version migrations are still unvalidated. |
| iOS 1.2.18–1.2.24 | [App Store history](https://apps.apple.com/us/app/echoes-of-vasteria/id6749265146) lists 1.2.18/19/20 on 11 September, 1.2.22 on 13 September, 1.2.23 on 15 September and 1.2.24 on 25 September 2025. | Corresponding version-bump source candidates have schema-1 Odin snapshots and string `Milestones`. They are reconstruction evidence; signed artifacts and device fixtures are missing. | Need string-era decoding plus the appropriate old gear/profile transitions and device upgrade tests. Do not assume a player installed every intermediate update. |
| iOS 1.3.1 / 1.3.2 | App Store history: 4 / 5 October 2025; 1.3.1 documents the skill/milestone overhaul. | Source candidates use typed milestone records, still schema 1. The typed-field change entered source `546dbc4b6` on 24 September. No exact 1.3 device fixture obtained. | Typed alias correction is promising, but 1.4 fixture results are not 1.3 device validation. Pin milestone IDs/configs and test both equipped effects and values. |
| iOS 1.4.3 | App Store: 19 November 2025. | Exact signed artifact/device fixture missing; `7faf9d4c5` is a candidate source only. | Conditional mobile support; native/AOT/container/cloud validation required. |
| Installed Steam Mac 1.4.3 | Actual public build 20851856/depot manifest and managed assembly inspected. | Schema-1 typed serializer and Cauldron thresholds verified from deployed artifacts. Three normal and three beta backups decode with exact skill values using the isolated typed-field correction. | Two demonstrated preservation fixes, new architecture publication tests and explicit retirement rules remain required. |
| Android release lineage | Listing updated 18 November 2025; exact version/code and earlier history not obtained. | No released APK/AAB/device fixture. | Do not infer parity from iOS, current ProjectSettings, or the Mac assembly. Obtain Play Console identities and fixtures. |
| Current planned update | Not released; current source schema 3, format 4; farm schema proposed. | Immutable generations/SlotState replace authoritative legacy snapshots. Farm transaction/state migration remains proposed. | Must explicitly import supported old banks, commit a verified candidate, and preserve premigration archives. Old executables cannot generally read/reverse the new authoritative state. |

The dated source transitions are useful for choosing decoders, but not proof of release contents: `cf50b9e0b` (23 August) introduces Odin SaveManager and retains an ES3 fallback; `3b55e84dd` (24 August) removes Easy Save and that fallback. Mobile's known September source candidates are already on the binary path. This makes ES3 support especially relevant to dormant early Steam/beta/import banks; it does **not** prove released mobile saves used ES3. Every supported cohort should be based on an artifact/fixture, not a guessed version number. The source has no release tags mapping these commits to platform builds.

A read-only scan of project build/output trees found mobile packages for unrelated IDS projects, but no Echoes IPA/APK/AAB. Library/Temp/dependency trees were excluded; this does not establish that release artifacts are unavailable in account storage or Xcode archives elsewhere. App Store/Play Console release identities and exact device fixtures remain retrieval work before platform certification.

## Skills existed before the overhaul

`06e90055e` (27 June 2025) adds saved skill Level and CurrentXP; `edee9c0a2` (29 June) adds Farming. A July 29 ES3 backup already contains Combat, Mining, Woodcutting, Fishing, Farming and Looting, including Farming level 3 / XP 21.7157288. An August backup contains Farming level 90 / XP 7967.081. The oldest investigated binary archives have Farming level 115; their XP differs and is preserved separately. Skill progression was not introduced by the October overhaul.

The broader backup scan contains **45 ES3 files, 34 distinct byte hashes**, dated July 29–August 22 in their payloads. Thirty-five have all six skill records (**210 saved skill records**); ten contain an empty SkillData dictionary, rather than evidence of a missing skill feature. Retain present level and XP exactly. An empty dictionary is initialized by current SkillController at level 1 / zero XP per known skill; do not zero other systems or reconstruct earned skill XP from playtime/quest counts. All populated fixtures use `Woodcutting`; mentions of Logging in source/assets are not evidence that these saves require a Logging-key rename.

The typed decoder's empty lists were misleading: compiling `GameData` from `546dbc4b6^` recovers 48 old string IDs per archive. Historical CheckMilestones records these automatically when a level threshold is reached, rather than as player choices. **Resolved rule:** current definitions determine eligibility/effects; omit those obsolete markers from new saves while retaining actual Level/XP. The earlier sidecar retains investigation evidence only; no per-player legacy block is required. Typed active selections remain meaningful and need safe ingestion; cache-only TierIndex can be omitted. See the [exact omission contract](unlock-omission-contract.md).

## What the isolated probes prove

[run-lineage-probes.py](evidence/run-lineage-probes.py) reads only the backed-up input tree and compiles generated model copies outside the repository. The adapter quotes **only integer property names outside strings**, needed because Easy Save writes unquoted integer TaskRecords keys; it does not rewrite numeric values. A strict JSON parser failing on those keys would be a format limitation, not proof that the save is corrupt.

All 45 files parsed; all 45 embedded GameData entries completed a current-model Odin load/save/reload probe. Every present Level is equal and every XP has identical IEEE float bytes to its legacy numeric value after conversion to its historical float type. The two string-era binary fixtures also retain all 12 skill levels/XP values after adaptation, with exact archival string-list equality. All original input bytes remain unchanged. The normalization and raw materializations stay private; public evidence uses anonymous fixture IDs and hashes.

The adapter explicitly assigns legacy schema 1 and preserves an absent producer version as unknown. Blindly materializing ES3 into `new GameData()` otherwise inherits schema 3, potentially skipping required migrations. Assigning the target release version to an unknown old save would similarly stamp old transitions as already completed. Production needs separate source-format/version provenance and migration receipts; an unknown producer may use a verified header/profile when available, not the filename or filesystem date.

The probe also compares Resources, Quests, TaskRecords, UpgradeLevels and Disciples **after mapping into the current DTO versus after reloading it**. Those checks prove roundtrip stability of represented fields, **not complete preservation of every removed source field or gameplay effect**. Raw legacy fields remain available in copied input/sidecar material. The harness does not run full historical migrations, native asset loading, automatic slot discovery, current runtime contributors, or durable SaveManager publication. Keeping archival milestone strings in a test sidecar is not a shipped archive implementation.

## Crop progression: quests then skill levels

The user's recollection is supported. July source first gates Farming behind `CompletedNpcTasks.Contains("Witch1")`, then introduces task quest references on 10 July (`cc0cdd5d1`) and crop-unlock quests on 15 July (`7f0857de9`). These early prerequisites are source history; they are not all the released rule. **The actual installed Steam assembly has TaskData.requiredQuest and its PickTaskFromCategory predicate calls QuestUtils.QuestCompleted(requiredQuest.questId).** It has no requiredSkillLevel field. This is direct deployed-code evidence, not a comparison against today's repo version string.

Source `1a4d75ead` (22 January 2026) replaces task/drop quest references with requiredSkillLevel, and `d5d402268` (28 January) adds the effective weight level check. The installed Mac build, despite its January install date, still has the older quest gate. The exact per-crop deployed asset references were not extracted; the following table resolves GUIDs from the November source candidate, with matching crop mappings in the September/October candidates. Current required levels come from actual checkout assets. Full mappings and limits are in [crop-lineage-map.json](evidence/crop-lineage-map.json).

| Saved crop/resource name | Task ID | Historical candidate questId | Current adventure task level |
| --- | ---: | --- | ---: |
| Radish | 28 | None | 1 |
| Corn | 29 | Unlock Corn | 7 |
| Wheat | 30 | Unlock Wheat | 13 |
| Watermelone | 31 | Unlock Watermelone | 18 |
| Carrot | 32 | Unlock Carrot | 23 |
| Spud | 33 | Unlock Spud | 29 |
| Tomato | 34 | Unlock Tomato | 35 |
| Lettuce | 35 | Unlock Lettuce | 40 |
| Cucumber | 36 | Unlock Cucumber | 45 |
| Leek | 38 | Unlock Leek | 52 |
| Parsnip | 39 | Unlock Parsnip | 57 |
| Pepper | 40 | Unlock Pepper | 62 |
| Chillie | 41 | Unlock Chilli | 68 |
| Pumking | 42 | Unlock Pumpking | 74 |
| Strawberry | 43 | Unlock Strawberry | 79 |
| Funion | 44 | Unlock Funion | 84 |
| Turnip | 45 | Unlock Turnip | 90 |

Preserve literal saved keys: `Chillie`, `Pumking`, `Watermelone` and quest `Unlock Pumpking` differ from display spellings. Task asset `Wartermelone` drops resource `Watermelone`. ID 37 is absent today; do not renumber later tasks to close the gap.

**Resolved product rule:** everyone discovers new seed packs fresh. Old completed crop quests, earned crop inventory, Farming levels, or paid fences must not reveal, grant, or alias to seed packs. Packs start hidden with no earned/discovery receipt; the new gameplay earns/discovers them. The approved starter tutorial route should operate in the new version, not masquerade as an old crop unlock. No migration allowance or Alter Echo compensation is authorized.

**Resolved adventure eligibility:** everyone follows current requirements, with no grandfathering. Fixture es3-34 has Farming 57 and Pepper/Chilli quests completed; tasks remain gated at current levels 62/68. This is an approved eligibility change, while original inventory, XP and meaningful completed receipts survive. A low-level beta fixture with all sixteen unlock quests complete also shows why quest counts cannot imply XP. Do not replay added quest XP, raise levels to force access or create an old-adventure override. Fixture provenance still does not prove untouched mobile release data.

## Earlier removals and meaningful losses

**Onion is not an old name for Funion.** Launch-day candidate source includes both resources, both quests and separate tasks (Onion 37, Funion 44). `eb060fd82` (24 August) deletes Onion/resource/task/Unlock Onion assets. A backed-up Data0 fixture still has earned Onion quantity 79,780.06889283087, task 37 completed 5,436 times and Unlock Onion completed. Keep these records under their original IDs in a legacy archive; do not silently convert them to Funion, count them toward the new completion cohort or discard them as unknown. Whether retired inventory needs any present use is a separate decision, not authorized compensation.

**Old purchases are approved for removal, without gear compensation.** Six ES3 fixtures have positive UpgradeLevels/no equipment; one stores Damage25, AttackRate90, MoveSpeed174, Health50, Regeneration25 and Defense42. Old adde98d60 multiplied purchases into a null-rarity helmet, but that converter is gone. Read the legacy shape safely, then omit UpgradeLevels and obsolete conversion guards from the new format after recording the transition. Do not reproduce that formula or grant starter gear. Preserve actual existing equipment independently. Original source backups remain recovery evidence rather than repeated obsolete fields in every new payload.

**Incomplete old quest progress needs field-aware handling.** Earlier records include DistanceBaseline/DistanceBaselineSet and KillBaseline; current distance quests use DistanceTravelProgress. Completed receipts survive the keyed dictionary, but removed baseline fields do not automatically transform into current progress. None of the 45 ES3 fixtures contains a pending distance quest with DistanceBaselineSet true, so a claimed real-fixture conversion would be unsupported. For a pinned historical rule, disposable fixtures should test bounded `max(0, saved effective cumulative distance - baseline)` against the old implementation and requirement, with no reward replay; otherwise archive and explicitly choose how to restart the affected objective. No blanket quest reset is approved.

Other removed fields, including early CraftHistory/PityCraftsSinceLast, should remain in the legacy source archive even if the current model lacks them. Earlier gear stores absolute affix values; current quality migration depends on loaded StatDef/Rarity assets and can change movement units. These asset-dependent migrations were skipped for 1.4.3 fixtures, so the initial successful runner test does not cover this earlier cohort. Run fixtures using frozen, verified historical/current profiles and compare resulting equipped stats, not only record count or quantity.

## Safe initialization recommendations and pending approvals

These are technical defaults, not product approval for a progress reset:

- Absent/empty skills: create only the missing known skill at level 1 / zero XP; preserve actual levels/XP and meaningful unknown records. Omit automatic old milestone strings and derive eligibility from current definitions. Keep typed player selections; do not invent them. Reject/quarantine invalid nonfinite values instead of replacing the whole save.
- Absent newer systems: initialize valid empty gear/Forge/Cauldron ledgers and required container defaults, with no fabricated balances, claims, gear, quest completions or retroactive offline production. A pre-system absence is not a reason to erase existing inventory or skill progress.
- Retired/unknown IDs: preserve raw keyed inventory/progression/statistics in a durable legacy archive, excluded from live reward/completion calculations. Existence of an archived row must not reactivate retired generation.
- Slot ambiguity: use embedded data keys and explicit provenance. Renamed `Sd0.es3` can contain `Beta5Data0`; older `Beta4Data` has no slot suffix. Do not promote beta to live or overwrite an occupied newer bank automatically. Surface a recoverable ambiguous-import result; keep original and candidate banks intact.
- Unknown source version: keep it unknown; choose adapters by payload representation and verified release profiles. Do not infer it from a stale current app version or assign schema 3 before transformations. Preserve migration receipts separately from producer-version evidence.
- Successful import: requires deterministic field/semantic checks, an idempotent source-hash receipt, verified durable publication, then selection of the new authoritative bank. Failure remains recoverable and must not autosave a fresh empty account over the only old copy.

Current eligibility, fresh packs, obsolete unlock-data omission and stat-purchase retirement without compensation are resolved. The investigated buff-capacity mismatch now has a proven historical alias, Mildred1→BuffSlot2; current quests still reach five slots. Translate the receipt before deriving capacity, without replaying payment/reward. Other historical reward IDs need validation. Unclaimed Alter holdings and meaningful removed objective adapters remain unresolved; none justifies a universal reset or deleting quest state. The [omission contract](unlock-omission-contract.md) contains exact evidence.

## Next validation gates

Extend the main staged plan with separate cohorts: ES3-only; schema-1 string milestones; schema-1 typed milestones; already upgraded faulty schema-2/3; no-skill records; unknown/beta slot keys; retired Onion/task37; positive stat upgrades/no gear; old absolute gear values; pending baseline objectives; and imports containing unavailable IDs. Test missing/newer systems independently from retirement choices.

Pass conditions: exact original keys and supported values survive; archived removed fields equal the source; approved semantic changes are enumerated; no old crop history reveals a pack; producer provenance is retained; no historical reward/offline generation is replayed; repeated/interrupted imports publish at most once; failed or ambiguous imports do not replace the old bank. Include actual iOS/Android artifacts and in-place device upgrades before calling mobile migration proven. The Steam legacy branch remains a fallback with isolated save/PlayerPrefs/cloud roots, not an alternative that magically repairs or merges these lineages.
