# Fields production implementation — 3 October 2026

Fields replaces the retired Alter-Echo offline-production role with paid town gardening and finite orchard batches. The normal Loading → Main game installs the service, world plots, native combined screen and farmer-gated navigation. Existing adventure Echoes, crop/fruit tasks, Cauldron drops, and old quest identities remain intact.

## Delivered behavior

- Meet Flora and Tillman on an actual adventure, then hand in `Farm.Garden.Introduction.v1`. No free seed, construction, Twins XP, or capacity.
- Seventeen existing crops use their canonical hero Farming gates and matching adventure seed packs. Apple/Pear/Peach/Cherry use canonical hero gates 10/27/49/72 and owned matching saplings. Each orchard batch consumes one sapling.
- Six garden beds unlock at Twins levels 1/5/35/65/95/125; orchard capacities 2/4/6 at 150/185/225. Nine chained construction quests validate previous quest, Twins level, canonical wood source, Mining source when Stone is charged, and exact materials. Payment, completion and capacity publish through one durable transaction.
- Twins start level 1, XP 0. Each newly paid harvested batch awards 10 XP; next-level requirement is `5 + floor(level/2)` with surplus carried. No hero Farming XP from town. Yield is `1 + .01*(level-1)`; duration ranks .9/.8/.7 at 10/30/60 replace one another. Planting freezes batch duration and yield.
- One optional watering halves total duration while preserving elapsed time. Late watering can make a batch ready; no extra XP/yield. Level 20 opts into per-bed harvest/replant in stable order, with another paid matching input; no alternative seed, automatic watering, or excess-time carry.
- Growth counts accepted active frames, including a running unfocused game. Pause/resume, reload and historical UTC gaps do not accrue, spend inputs or award XP.
- One native all-in-one screen shows seeds/saplings, six beds, Twins progress and a live town preview; orchard is an inline foldout. The desktop uses one row with independent list scrolling.
- Six orchard plot **centers** are (-60,-13.5),(-56,-13.5),(-52,-13.5),(-60,-17),(-56,-17),(-52,-17). Each rounded 2×2 soil plot mirrors the accepted corner tile in all four cells with matching wet flips. Imported tree pivots remain grounded; only the two approved young-tree cells change depth. Runtime clearing counts are 23 stumps and 20 standing trees. No additional colliders or new art.

## Saves and legacy contracts

Schema 6 adds Fields progression without deleting retired investments, opaque journal history, pending intents or valid previously paid Radish contracts. Legacy paid harvests keep their original yield and applicable Cauldron bonus, pay once and grant no retroactive Twins XP. Old preparation receipts do not grant new construction capacity. Unknown future operation namespaces are retained, rather than treated as current replay receipts.

Skill/resource eligibility uses live state only for the exact loaded bank; detached or switched banks use saved state. All production writes use the existing farm economic lane, bounded operation lineage, finite resource validation, durable save before publication and owner/slot guards. Ordinary adventure completion stages a fixed seed outcome and retries it without rerolling or duplicate payout.

## Validation

Final suites passed 223 EditMode + 59 PlayMode tests (282 total). The protected normal-game Mac player exited 0 and all assertions passed; clean screenshots were visually inspected. Build succeeded with seven exactly inherited scene errors and 20 warnings; every warning delta is classified in the comparison. Final source fingerprints cover all 75 scoped source/asset/test paths and match the frozen final test inputs. The runtime player used the same production code and asset content; its only subsequent input changes were removal of trailing whitespace from 14 new YAML assets/metadata files, re-imported and tested in the final suites. Review found no remaining payment, quest, replay or cross-bank blocker. Proof-only Steam/UGS guards, runtime harness and camera overrides exist only in the disposable project, never the shared checkout. All proof saves and preferences use a unique test identity and protected test root; cloud access is disabled.

The normal-game proof exercises actual adventure startup, the authored crop prefab's public completion callback before/after introduction, native farmer meeting, real QuestManager hand-ins and farm service commands, retry-once seed credit, watering/reload/no-UTC checks, XP carry, live rendered UI preview, geometry and isolated durable reload.

Controlled fixtures deliberately supply matching crop duration/chance, first construction materials, active elapsed seconds and full orchard capacity/Farming level/saplings for visual verification. They do not establish natural progression balance or the time needed to earn level 225. Base 1,800-second duration, 10 yield, 10% seed chance and construction costs remain approved starting tuning.

Build diagnostics must be read with `build-comparison.json` and `build-warning-provenance.json`: the package baseline already succeeds with seven Main scene repair errors. The final comparison records exact inherited/new messages and distinguishes compiler warnings from proof guards and unchanged vendor source; this work does not silently repair unrelated malformed baseline scene objects.

## Publication boundary

Only explicit Fields paths and this evidence are included in the scoped local commit. Manual A* 5.4.7/Odin 4.0.2.4 changes, package work, preview assets and unrelated settings are preserved. No push, merge or release is authorized or performed.

The protected Development player logs Unity native shutdown/thread-finalization and ComputeBuffer disposal notices at exit. No managed exception or failed proof assertion occurred. These notices are retained as a limitation; this slice does not claim a general native shutdown cleanup. The temporary build/application remains under `/tmp/eov-fields-production-v1/player-proof-v2`; test saves and preference files are excluded from deliverables.

Clean runtime screenshots are saved in Library: combined Fields `libfile_79eaf9cd9ebc8191ae89c77d8d9a0a9b` and dry/wet orchard `libfile_929a5593369081918ea360f6fd517899`, both version 0. See `library-screenshots.json` for exact file identities.

The checked-in XML result copies remove trailing line whitespace for repository hygiene; untouched runner XML and full logs remain in `/tmp/eov-fields-production-v1/v7-*`.
