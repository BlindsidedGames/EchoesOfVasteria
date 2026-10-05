# Isolated migration probes

Verified invocation on this Mac:

```sh
python3 docs/planning/save-migration-feasibility-2026-09-30/evidence/run-probes.py \
  --backup '/Users/matthewrushworth/Projects/Echoes Farming Planning Evidence/2026-09-30/player-backup/Saves/Save1/snapshot.bin' \
  --released-managed '/Users/matthewrushworth/Library/Application Support/Steam/steamapps/common/Echoes of Vasteria/Echoes of Vasteria.app/Contents/Resources/Data/Managed' \
  --current-managed '/Users/matthewrushworth/Projects/Echoes Mac Verification/2026-09-30-cleanup/EOVOfflineVerification.app/Contents/Resources/Data/Managed' \
  --out '/Users/matthewrushworth/Projects/Echoes Migration Investigation/2026-09-30/reproduced'
```

The script copies DLLs and the backed-up input before working. Current GameData and migration sources are freshly compiled; the cleanup Player supplies Unity framework dependencies, not the current save model. Actual installed released Assembly-CSharp supplies the old serializer model and gear definitions for linking the historical migrations. Schema-1 version 1.4.3 skips those asset-dependent historical transforms; their older-version/native behavior is not proven here.

PayloadProbe uses default Odin binding, with serializer errors throwing and Unity's managed logger disabled for CLI use. No custom binder or Unity native method is substituted. MigrationProbe materializes JSON with collection replacement, then invokes the actual source runner; this avoids JSON's constructor/default-list duplication. Real binary decoding is independently checked by PayloadProbe. The retained-field variant modifies only a generated copy of GameData; the released-profile variant modifies only a generated copy of the overflow migration.

LegacyReadProbe compiles current SaveManager unchanged with UNITY_INCLUDE_TESTS and its existing root override. To link without Oracle/native export UI, the script includes only the unmodified pure rescue/export helper section from SaveImportExport; the unused Migrations import is omitted. No rescue or write path is invoked. Cases use newly generated disposable directories. The script verifies that the input backup's SHA-256 remains unchanged.

Schema-3 roundtrip files use a copied legacy-header test envelope with updated schema/size. This tests Odin load/migrate/save/reload equality, not new-format atomic disk publication. All Id/IsActive/TierIndex, XP and Level values are compared as complete skill objects; counts alone are insufficient. The combined correction also preserves resource inventories, gear and archived generator records in the recorded fixture.

A second isolated correction replaces only the Sirenix rename attribute with UnityEngine.Serialization.FormerlySerializedAs. The revised reproducer independently runs that model through the release-profile correction and schema-3 load/migrate/save/reload, comparing every original skill field plus its new canonical MilestoneRecords field. This works in the tested NoEmitAndNoEditor serializer, and remains subject to native/device validation.

The actual installed release's `resources.assets` was scanned read-only for the exact length-prefixed resource and buff arrays directly after the named CauldronConfig object. Offsets/full arrays are recorded in released-config.json. Current and candidate-release source assets contain the same serialized arrays. run-probes.py uses that recorded release profile; reusing it for a different deployed release requires fresh artifact/profile verification.

Raw player JSON, copied save bytes, assemblies, generated experimental source and IL extracts are private investigation material outside the repository. The eight-case fixture matrix includes three normal saves, three beta saves and two older archives. The extension below recovers the older archives' string milestones; historical asset-dependent migrations remain unproven.

Earlier-lineage invocation:

```sh
python3 docs/planning/save-migration-feasibility-2026-09-30/evidence/run-lineage-probes.py \
  --backup-root '/Users/matthewrushworth/Projects/Echoes Farming Planning Evidence/2026-09-30/player-backup' \
  --current-managed '/Users/matthewrushworth/Projects/Echoes Mac Verification/2026-09-30-cleanup/EOVOfflineVerification.app/Contents/Resources/Data/Managed' \
  --out '/Users/matthewrushworth/Projects/Echoes Migration Investigation/2026-09-30/lineage-probes'
```

This scans all backed-up ES3 files, including nested historic/imported examples and rotations: 45 entries, 34 unique byte hashes, 210 present skill records. Ten entries have empty SkillData dictionaries. Lexical normalization quotes unquoted integer dictionary keys only outside strings. LegacySkillsProbe preserves unknown version fields and sets legacy schema 1, then checks exact Level and XP float bytes through current-model Odin serialization. It archives milestone strings in isolated sidecar outputs without asserting their former effects map to the new system.

`es3-lineage-matrix.json` is the preliminary six top-level-file summary; `es3-results.json` supersedes it with the full 45-file scan. Milestone archive equality compares the reread sidecar JSON to each original parsed string list, not only counts.

The script also compiles historical GameData from `546dbc4b6^`; each of the two old binary archives decodes 48 milestone strings, rather than the zero typed records recorded in fixture-matrix.json. Those two archives then run the bounded current-model adapter, preserving 12 skill records and exact archived strings. The original binary/ES3 bytes remain unchanged. No full historical migration, asset-dependent operation, native Player/Editor, live save write or publishing action occurs.

Known-dictionary comparisons are **current DTO before serialization versus after reload**, not raw historical source-field equality. Removed baseline/crafting fields, old stat benefits, milestone effects, ambiguous beta routing and mobile identity still need explicit handling. Results identify anonymous fixtures; private-fixture-paths.json remains outside the repository. Crop map GUIDs come from six source revisions and are qualified as source candidates; only the old quest-gating code itself is verified from the installed Steam assembly.

Approved-omission invocation:

```sh
python3 docs/planning/save-migration-feasibility-2026-09-30/evidence/run-omission-probes.py \
  --current-managed '/Users/matthewrushworth/Projects/Echoes Mac Verification/2026-09-30-cleanup/EOVOfflineVerification.app/Contents/Resources/Data/Managed' \
  --typed-json '/Users/matthewrushworth/Projects/Echoes Migration Investigation/2026-09-30/reproduced/released.json' \
  --lineage-dir '/Users/matthewrushworth/Projects/Echoes Migration Investigation/2026-09-30/lineage-probes' \
  --out '/Users/matthewrushworth/Projects/Echoes Migration Investigation/2026-09-30/omission-probes'
```

The compatible model ingests legacy typed choices/Level/XP; old string milestones are recognized as automatic eligibility and omitted per the user decision. A generated future DTO omits UpgradeLevels, both obsolete stat guards, milestone records/strings and TierIndex, replacing typed records with ActiveMilestoneIds. Forty-eight projections pass: 228 skill records retain exact levels/XP; every one of 23 typed Id/IsActive choices has the same meaning with five active IDs persisted. Canonical represented quests/NPCs/inventory/gear/capacity/loadout/Cauldron/statistics remain equal. The omitted fields are absent from object and binary payload. Existing equipment equality excludes starter-gear compensation.

JSON numeric forms are normalized before dictionary comparison: FromObject creates float-backed JValues while parsed JSON uses doubles; that representational difference is not a changed TaskRecord. Skill XP additionally compares actual float bytes. The future payload is a DTO experiment marked schema 4; it does not run production migration/profile repair, native effects or durable publication. Compatible-model comparisons do not prove preservation of removed old objective fields. See [unlock-omission-contract.md](../unlock-omission-contract.md) for exact scope and unresolved capacity/progress semantics.
