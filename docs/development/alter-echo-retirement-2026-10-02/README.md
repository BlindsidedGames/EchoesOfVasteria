# Alter-Echo production retirement

Implemented on `fix/editor-startup-terrain-catalogue`, based on `f7bf7e7bcd5ec74c992c5fa2820c1e79a9e62916`. This retires the Alter-Echo passive production system, including online production, offline accrual, pending collection, assignment and upgrades. Combat/task Echoes remain.

## Removal and retained dependencies

| Area | Change | Retained |
|---|---|---|
| Production | Remove `Assets/Scripts/NpcGeneration`, generator/crop-growth components, rate refresh, offline callbacks and pending payout routes. | Decorative crop assets and world scenery. |
| UI/assets | Remove the dedicated native Alter-Echo screen, legacy window/button hierarchy, balance display, producer prefabs and obsolete migration tool. Clean both Main scenes. Retire navigation enum value 4 without shifting other values. | Actual Cauldron, town farm, Forge, Skills, quests, Library and other navigation routes. |
| Quests | Zero producer-power rewards in A Matter of Health, Tracking Twins and Finishing Touches. Remove QuestManager's production-manager dependency/reward application. Update those descriptions and Into the Woods' old screen-unlock promise. | Original quest IDs, prerequisite chains, completion records, costs and other rewards. No replacement rewards or auto-completion. |
| Saves | Advance current schema to 5 with a receipt-only migration. Legacy Disciples, stored balances, timestamps, progress, historical investments and DisciplePercent stay serialized but inert. | Import/export and existing save transport use the same current codec. No payout, erasure, refund or conversion. Older builds reject schema 5 rather than resuming production. |
| Cauldron | Resource cards now grant genuine acquisition yield; category bonuses derive from save data rather than an open screen. Update tooltips/help/stat labels. | Existing authored card thresholds, owned counts, buff cards, Eternal cards, stew value and rarity. |
| Resource rarity | Keep the legacy-named `DisableAlterEcho` flag because existing resource rarity/stat/crafting code also depends on it; excluded items receive no new yield. | Existing rarity levels, chances and per-tier yield behavior. Town farm still does not roll or multiply rarity. |

The source taxonomy `AEResourceGroup` and legacy tasting counters remain as internal compatibility names. They no longer run production; avoiding a broad rename keeps this retirement bounded.

## Approved yield rules

Existing authored thresholds stay `1,25,75,200,750,1500,3500,10000`. Resource-card yield by tiers 1–8 is `1,2,3,5,7,10,15,20%`; category yield is `2,5,10,15,20,30,40,50%`. Unowned cards are tier zero. A category uses the lowest actual card tier among earned, eligible resources in that category. No unlocked resources means zero. Card and category bonuses add, with a maximum 70%.

`CauldronResourceYield.cs` is the shared calculation owner. Normal world-task drops and enemy drops add `base drop × existing double-resource proc × (card + category)%` to the existing reward. Existing skill/milestone/buff/rarity behavior stays in its existing path. The new supplement does not multiply the fully modified result. Gathering resonance and windfall use the pre-supplement award; fixed propagation/bonus drops remain excluded. The existing double-resource outcome is reused without another random draw.

The town Radish farm captures the final base harvest plus bonus in its durable harvest proposal. Committing/reloading/replaying the transaction cannot recompute or pay the yield twice. Planting, seeds, XP and task-credit receipts are unchanged. The future twins feature is outside this change. The proposed Farming 1% per-level crop yield is not present in this source snapshot and was not added.

Quest rewards, imports, refunds, conversions, compensation and generic inventory additions receive no new bonus. Seeds/saplings remain excluded by the existing resource eligibility flag.

## Verification

Used the checked-in `tools/testing/run.py` isolation helpers and sandbox, a disposable project snapshot and fresh product identities. No real saves or production PlayerPrefs were used. Final results: **185 EditMode passed, 56 PlayMode passed; zero failures/skips**. Full XML results are included here.

Focused evidence covers authored curves/thresholds, unowned category blocking, excluded resources, schema 0/3/4 retirement round trips with pending funds and quest/card progress preserved, repeated migration, durable farm payout/replay, actual world-task acquisition, generic additions and absence of offline accrual. A seeded 50% double-resource regression first failed three of four cases when the supplement rolled independently; all four pass after reusing the original outcome.

Both `Assets/Scenes/Main.unity` and `Assets/Development/Farming/Main.unity` import with zero missing scripts in isolated Edit mode. Deleted asset GUID references and newly dangling internal scene IDs were checked. `git diff --check` passes. A final label-only cleanup compiled during the isolated native UI render after the full test suites.

The PNGs are rendered from the actual native prefabs against an in-memory fixture, not a mockup or a production save. They show the Radish +20% card/Farming +50% category tooltip, the Hub without the old production route, and retained Townsfolk routes.

## Limits and release implications

No merge or release was requested. Production cloud sync, devices and platform builds were not exercised. Existing generic transport remains unchanged; saved-schema compatibility and no-payout behavior are covered locally. Keeping schema 5 prevents an older schema-4 build from reopening the retired producer records, so a release must use the current build on each supported platform. Historical balances/investments remain recoverable data; deleting them would be a separate explicitly approved migration.

## Live discovery boundary follow-up

Independent review identified that category membership used saved `Resources.Earned`, while `ResourceManager.Add` publishes a live unlock before the next save capture. An isolated actual-path regression reproduced the bug: with max Radish cards and newly discovered unowned Corn, the Radish yield incorrectly remained 70% instead of dropping to 20% immediately.

`ResourceManager` now remembers the bank loaded into its runtime inventory and exposes an owner-aware unlock query. For that active owner, gameplay and native collection membership use live unlocks. Detached banks, replacement owners not yet loaded, and calculations without an inventory use their own saved membership. Load/reset rebind the inventory owner. The fix does not call SaveState, write a save, roll rarity, change card tiers or alter production-retirement data.

The single owner-boundary regression exercises actual Corn discovery, the next real Radish task drop, the actual native collections presenter/tooltip, ordinary SaveState capture, serialization round trip, owner replacement before load, and ResourceManager reload. It confirms category +0%, Radish-only +20%, preserved Corn rarity 3, unchanged Radish card count and no Corn card creation. The pre-fix failure is recorded in `DiscoveryBeforeFix.xml`.

Final full-suite result after this fix: **185 EditMode and 56 PlayMode passed, zero failures/skips**. Source hashes for the follow-up snapshot are in `discovery-input-updates.json`; `summary.json` and existing final XML evidence now contain the latest runs.
