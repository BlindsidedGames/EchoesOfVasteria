# Plant-pack integration review — 1 October 2026

> Historical read-only review. Its receipt-retention recommendation is now implemented and tested in [receipt-retention.md](receipt-retention.md). The user approved 10% behind an authored farming quest gate; seed pool/Echo wiring and that exact gate remain pending.

This read-only review finds a clear route from the existing two-bed Radish development slice to authored plant drop pools. Fresh discovery for every player and seed packs from plant drops are settled decisions. There is no legacy crop grant, starter compensation, guaranteed entitlement or pity-token proposal in this route. No production code, scene, balance or asset was changed during this review.

**Recommendation:** author a separate typed seed pool beside each plant's ordinary resource pool, reuse the existing owner-scoped fixed-roll transaction lane, and bind Radish by its logical crop identity rather than its currently mismatched world sprite. Recommend including actual adventure farming Echo completions because ordinary plant rewards already include them; this eligibility change remains for review. Address the growing receipt ledger before widening the slice. Installed tooling can test both serializer dependency variants on managed Mac, but cannot build an iOS/Android Unity Player yet.

## Actual crop pools and art

All 17 authored Farming TaskData assets have exactly one ordinary crop entry: quantity range 1–5, weight 1, required drop level 0, and no additional loot chances. The game biases quantity rolls toward the lower end; 1–5 is a range, not a mean yield. Each task's Farming requirement agrees with ResourceUnlockConfig. These plants can spawn on Grass, Grass Farmlands, Grass Woods and Grass River. Spawn weighting also depends on progression, toggles and buffs; these durations are authored isolated-task durations, not measured ordinary adventure throughput.

| Task ID / authored name | Farming level | Authored seconds | Spawn weight | Existing world family → prepared packet candidate |
| --- | ---: | ---: | ---: | --- |
| 28 / Radish | 1 | 2 | 1000 | Eggplant → `SeedPack_Eggplant` |
| 29 / Corn | 7 | 2.13 | 900 | Corn → `SeedPack_Corn` |
| 30 / Wheat | 13 | 2.38 | 800 | Wheat → `SeedPack_Wheat` |
| 31 / Wartermelone | 18 | 2.77 | 700 | Watermelon → `SeedPack_Watermelon` |
| 32 / Carrot | 23 | 3.29 | 600 | Carrot → `SeedPack_Carrot` |
| 33 / Spud | 29 | 3.94 | 500 | Potato → `SeedPack_Potato` |
| 34 / Tomato | 35 | 4.7 | 450 | Tomato → `SeedPack_Tomato` |
| 35 / Lettuce | 40 | 5.57 | 400 | Cabbage → `SeedPack_Cabbage` |
| 36 / Cucumber | 45 | 6.54 | 350 | Cucumber → `SeedPack_Cucumber` |
| 38 / Leek | 52 | 8.71 | 300 | Leek → `SeedPack_Leek` |
| 39 / Parsnip | 57 | 9.89 | 260 | PaleRoot → `SeedPack_PaleRoot` |
| 40 / Pepper | 62 | 11.11 | 220 | Beet → `SeedPack_Beet` |
| 41 / Chillie | 68 | 11.62 | 190 | RedPepper → `SeedPack_RedPepper` |
| 42 / Pumking | 74 | 11.85 | 160 | Pumpkin → `SeedPack_Pumpkin` |
| 43 / Strawberry | 79 | 11.94 | 140 | Strawberry → `SeedPack_Strawberry` |
| 44 / Funion | 84 | 11.98 | 120 | TanBulb_A → `SeedPack_TanBulb_A` |
| 45 / Turnip | 90 | 12 | 100 | Radish → `SeedPack_Radish` |

The last column records the art actually referenced by each task/prefab. It is **not an approved semantic crop-to-pack binding**. All 68 referenced growth-stage sprites resolve. Radish currently uses Eggplant's task icon and four world stages, while Turnip uses Radish. Pepper uses Beet, Lettuce uses Cabbage; Spud/Potato and the Watermelone/Watermelon and Pumking/Pumpkin spellings also require explicit content bindings. Cucumber's prefab stages are ordered Young, Developed, Mature, EarlySprout rather than youngest-to-mature. None were silently rewritten. No task 37 or crop resource ID 46 was found; a numeric gap is not evidence of missing gameplay content.

The prepared sheet contains 22 art families and one unknown, not 22 approved recipes. The two-bed slice already selects the actual Radish world sprites locally; its seed must remain `seed.radish` / `recipe.radish.v1` / Radish resource ID 50 / `SeedPack_Radish`, rather than inheriting Eggplant from the old prefab. Review ambiguous bindings before enabling additional crops. Exact task, crop, sprite and prefab evidence is in `authored-plant-pools.json` in the accompanying evidence directory.

## Minimum production wiring

1. **Author the typed pool.** Add an optional seed-pack collection to TaskData and a definition carrying logical pack/crop/recipe identity and known/unknown sprite references. Use canonical current Farming requirements for availability; retain actual skill Level/XP, discard obsolete saved unlock caches. Start with Radish Task GUID `42f81ca90b736cd4bb035112a8b22ad3` → `seed.radish` → crop GUID `d0108dc3e7727584d9523efdc3d331a0`. The current development code instead checks task ID 28 and a private 10% field; it does not consume an authored seed pool.
2. **Stage and commit typed rewards.** Resolve an eligible task's pack once before ordinary XP/drop callbacks, persist the selected pack/quantity and owner/slot/operation handle, then commit in the existing completion `finally` lane. A retry uses its saved selection. A miss is also a committed selection; loading, abandoning or another claimant must not reroll it. Successful seed credit updates the farm inventory and lifetime discovery, never ResourceManager.Add, crop tiers, Cauldron cards or resource multipliers. Quantity zero retains discovery; old crop Earned/Level does not reveal packs. Farm harvest, town generators and reward procs never generate adventure/seed completion credit.
3. **Show committed drops.** Reuse the prepared named glyph, e.g. `<sprite name="SeedPack_Radish">`, after newly committed success. Capture task position before callbacks/pooling; a delayed retry can show an inventory notification rather than stale world text. `AlreadyApplied` must not show another +1. The existing ordinary floating-text route uses numeric Resource IDs and has no production seed branch. Keep a readable text label and quantity; do not rely only on tiny packet colors.
4. **Make Barkley's preparation one transaction.** Author a new current quest ID and place paid materials, its completion record and bed ownership in one Oracle candidate. The development `Farm.PrepareBeds` receipt and 10 Logs / 20 Sticks are provisional, absent from the production quest cohort. Calling QuestManager's ordinary hand-in and Farm.PrepareBeds together would split the operation and can double-charge. Refresh quest UI only after publication. Include the new authored ID in both sides of the current completion cohort; retain meaningful Fence history without inventing completed records or granting new access from it. Fence1 currently costs 1,250 Sticks and grants distance +50; it is not the new farm ownership contract.
5. **Promote the reviewed scene separately.** The native Town Farm route exists in shared source, but FarmService, bindings, two beds and development Navigation are configured only in the duplicate development Main. Promotion must copy the reviewed V4 position, correct fences/soil, both gate approaches and Radish stages into production in its own review. Existing Alter Echo town/offline generators still operate there. Retirement and handling of already-earned unclaimed stock need the separately reviewed cutover contract; adding beds alone does not replace them. Orchard remains separate and optional.

These can be three reviewable implementation slices: authored Radish pool/typed credit plus committed feedback; production paid quest and coherent cohort; accepted Main placement and explicit generator cutover. Each requires failure/reload and ordinary-reward comparisons before broader crop activation. None is implemented by this review.

## Choices still open

| Choice | Existing behavior | Recommended default and consequence |
| --- | --- | --- |
| Additive vs exclusive seed roll | DropResolver rolls one weighted ordinary entry first, removes it, then tries sequential extra slots. | Separate typed secondary pool. Preserve ordinary crop output while adding a flat one-pack roll. An exclusive pool is possible but deliberately replaces crops and needs economy approval. Adding equal-weight seed beside crop makes 50% seed replacement; adding an extra-slot chance does not force crop to win first. |
| Adventure Echo eligibility | Ordinary resource drops include real task Echo completions. The development farm explicitly excludes all Echoes. | Include primary hero and actual adventure task Echoes at the same per-task chance; exclude town/offline Alter generators and bonus/harvest recursion. Farming-selective Echoes can claim plants before the hero, so this supports late-game discovery consistently. Primary-only is the bounded-income alternative, but diverges from ordinary rewards. No eligibility change was applied. |
| Rates, surplus and farm tuning | Development: 10% primary Radish completion, 30-minute batch, 10 base Radish per bed, no useful pack cap; only numeric overflow protection. | Keep these provisional pending ordinary adventure measurements with/without task Echoes. Recommend one flat pack per successful roll, unaffected by DoubleResources/resource/buff multipliers. A useful inventory cap and surplus treatment need a deliberate choice; do not silently discard credits. Quest cost and harvest tier treatment also remain provisional. |

At 10%, a matching completed task has independent probability 0.1: mean 10 completions to a pack, and 29 completions for at least 95% chance of one. At 5% these are 20 and 59; at 1%, 100 and 299. None guarantees a maximum wait, and these are completion counts, not adventure minutes. Travel, other tasks, spawn selection and helper share have not been measured here. No new entitlement system is inferred from the fresh-discovery decision.

Two continuously replanted 30-minute beds yield at most 40 base Radish per elapsed hour and consume four packs per elapsed hour. At 10% that requires an expected 40 eligible completions per elapsed hour. If adventure occupies one quarter of elapsed time, the matching completion rate during adventure would need to average 160 per active adventure hour to sustain that example. Offline/town time grows a paid finite batch but supplies no new adventure seeds. This is a scenario, not a measurement or approved balance.

## Receipt cost and bounded retention

The slice records every eligible hit and miss in an unbounded Operations dictionary, with additional pending intents and harvested batch IDs. FarmService calls CaptureGrowth every 0.25 seconds while any bed is planted, including already-ready beds; AdvanceGrowth deep-clones the complete ledger and advances its UTC baseline. Thus finite crop batches do not bound lifetime memory or update cost. Each seed completion also commits an immutable full save synchronously.

A disposable Mac-host Mono harness exercised the actual FarmState, FarmCommands and CurrentSaveCodec with two planted beds and 0–50,000 receipts (90% misses / 10% hits). It used 31 growth samples and five serialization samples per size, a 45-second bound and completed in 1.30 seconds. Exact receipt-count roundtrip and unchanged source growth passed at every size. Approximate retained clone bytes come from managed GC snapshots, not an allocation profiler.

| Receipts | Growth median / p95, ms | Serialize median / max, ms | Snapshot bytes | Additional retained clone bytes |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 0.0007 / 0.0046 | 0.0787 / 0.0957 | 19,897 | 5,344 |
| 1,000 | 0.1203 / 0.1773 | 1.3231 / 1.3796 | 285,016 | 135,424 |
| 10,000 | 1.0757 / 3.3125 | 12.5034 / 12.7997 | 2,670,016 | 1,291,584 |
| 25,000 | 4.0680 / 6.4388 | 32.8024 / 34.3952 | 6,645,016 | 3,018,936 |
| 50,000 | 8.4693 / 12.2104 | 68.7718 / 70.6274 | 13,270,016 | 6,113,200 |

At 50,000 receipts, four median growth updates cost about 33.9 ms per second and retain approximately 6.1 MB for one additional clone; serializing the 13.27 MB snapshot alone takes about 69 ms. Disk, CRC, full GameData capture, frame scheduling and mobile GC are excluded, so this does not establish a device frame budget. Payload grows roughly 265 bytes per receipt in this fixture. SaveManager's 256 MiB payload limit is a hard ceiling, not a useful retention target. Fifty thousand 2-second Radish completions represent 27.8 hours of authored isolated task work before travel/other tasks; this is not measured ordinary playtime.

**Proposed fix, not an implementation:** stop clock work when all beds are ready and update only bed clock state during mutable contributor capture, retaining immutable candidate transactions for economic changes. Replace lifetime UUID receipts with a per-save-lineage monotonic sequence, durable contiguous committed watermark, and a sparse journal of outstanding or out-of-order fixed results. Allocate sequence once at completion; persist it with the intent. Preserve gaps until settled, and reject tokens at/below the sealed watermark. Bound history by outstanding work rather than age. Maintain owner/slot identity, no rerolls, no duplicate feedback and crash-after-commit recovery. Existing UUID receipts and pending intents need explicit migration into a sealed legacy namespace or retained mapping; deleting old UUID records by date alone would reopen replay. Harvest receipts need equivalent proof before retiring batch tombstones.

Gate compaction with replay/out-of-order gaps, write failure, commit-before-publication failure, save switch, reload and old-format pending fixtures. Demonstrate that clock cost no longer scales with settled history, then profile capture/write and GC in an actual Unity Player on representative mobile hardware. No compaction, pruning or snapshot changes were made.

## Mobile and AOT: what can be verified here

Installed Unity 6000.6.0f1 has only MacStandaloneSupport, whose installed Player variations are Mono. iOS and Android Unity modules are absent. Xcode 26.4.1 and iPhone device/simulator SDK 26.4 are present, with available simulator runtimes 17.5, 18.0, 18.5, 26.0.1, 26.2 and 26.4. A harmless supported Simulator query succeeded after the sandbox connection restriction was resolved; no simulator was booted or created. Unity requires its iOS Build Support module as well as Xcode to export a local iOS project. [Unity setup requirements](https://docs.unity3d.com/6000.0/Documentation/Manual/ios-environment-setup.html)

The repository enables Sirenix's NoEmitAndNoEditor serializer dependency on Mac/Android and NoEditor on iOS/tvOS. The same bounded fixtures were also compiled and passed using the iOS-enabled dependency on this managed Mac host: exact sizes and counts agree. This checks dependency compatibility and current roundtrip, **not IL2CPP generic generation, linker stripping, device containers or in-place mobile upgrades**. Existing EditMode/PlayMode and isolated Mac Player evidence remains the previous reviewed checkpoint; no Unity build or new gameplay session was run in this review.

AOTGenerationConfig has `automateBeforeBuilds: 0` and an empty explicit type list. The new codec registers SkillProgressCompatibilityFormatter; farm dictionaries, hash sets and nested records also require target coverage. Vendor link.xml preserves serializer assemblies, but is not proof that all new generic variants/formatters survive a stripped Player. Generate targeted AOT support in an isolated validation build and check it on IL2CPP. Odin documents both stripping/generic risks and that its whole-project scan loads and saves relevant assets; avoid that scan in the preserved working checkout. [Odin AOT guidance](https://odininspector.com/tutorials/serialize-anything/aot-serialization)

The new TimelessEchoes.Runtime asmdef explicitly references Steamworks.NET, whose asmdef supports Editor/desktop only. Desktop source guards already provide non-Steam paths. Treat mobile assembly graph resolution as an unverified build gate, not a confirmed compiler defect or an excuse to change accounts/references blindly.

**Next practical prerequisite:** install the matching Unity iOS Build Support module through supported Hub controls, then export an isolated IL2CPP simulator validation project with disposable copied fixtures and service guards. Verify farm collections/custom formatter, historical migration → current write → reload, exactly-once crash/retry, clock/pause, and named inventory/drop glyphs. A physical-device in-place upgrade is a later distinct gate with existing authorized tooling/identity. No new account, grant, module installation, signing or deployment was attempted here.

## Icon and preservation evidence

Reopened and visually inspected all three prepared icon figures: all 22 packet families and the shared unknown are legible, inventory removes only the verified outer halo, and drop glyphs retain it. Existing evidence checks all 23 GUID/fileID/name references, centered pivots, 16×16 / PPU16, Point / RGBA32 / no mipmaps, transparency and named TMP/TextCore rendering. The old 213 atlas entries and 55,296 RGBA pixel positions are preserved. No new art, destructive packing or visual replacement was made in this review.

| Verified figure | Preserved Library identity / version |
| --- | --- |
| Before/after and shared unknown | `libfile_7dcc8a97d1188191bc53ffe9f6f3aa81` / 1 |
| Native inventory and TextCore | `libfile_84d50dd664b88191a9f064e5e645ea0d` / 0 |
| TMP drop examples | `libfile_49b174e4a17c819197f43cbfee585cb6` / 0 |

Radish inventory: GUID `7bac5cbb13a44452b84e002f5ab62650`, fileID `759725064393614726`. Radish drop: texture GUID `2d1ea1fa2e7dca4448ad7af961b8896b`, fileID `469787315396090978`, named glyph `SeedPack_Radish`, index 226. Shared unknown uses the same inventory/drop GUIDs, fileIDs `250467468400558940` / `398324854314620224`, glyph index 235. Prefer names/definitions over new numeric Resource IDs.

Focused source hashes, Main and backed-up player data match the prior verified checkpoint in `preservation-check.json`. Icon contents were rechecked against the original source pixels, old atlas entries and exact prepared references; whole-file prior hashes for the prepared icon assets were not retained in this review. No Editor operation, source/gameplay change, commit, push or release occurred. Main was last verified clean in Edit mode at the previous checkpoint and its file hash remains exact; this review did not introduce a new Editor state probe.

Evidence lives in `../Echoes Farming Implementation Evidence/2026-10-01/integration-review` relative to the project root: authored pool/GUID inventory, reproducible audit script, bounded harness/source hashes, both dependency outputs, mobile tooling and preservation checks. The original accepted visual/Player Library identities and versions are retained.
