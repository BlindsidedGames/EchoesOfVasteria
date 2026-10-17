# Intro progression and UI validation — 17 October 2026

## Verified

- Unity 6000.6.0f1: **41/41** `Tests.PlayMode.FieldsProductionPlayModeTests` tests passed, with no skipped or inconclusive cases.
- The suite covers opening progression, town-only quest acknowledgement, fixed equipment rewards and reload, forge conversion/craft/equip and reload, NPC conversation/combat behavior, shoreline fishing, inventory and quest interactions, scrolling, map/graph guidance, and the run summary.
- A serialized-reference audit found no references to the removed leaderboard assets/scripts or replaced direct sword reward component. Retired quest assets retain their original GUIDs outside Resources.
- A fresh-save gameplay walkthrough reached the end of Ivan's forge introduction and equipped the sword: https://youtu.be/IRcvrl5uYxY

## Test environment corrections

Unity's batch test runner defaults its Game view to 640×480, independently of player `-screen-width` and `-screen-height` arguments. The desktop UI fixture now explicitly selects 1920×1080 for each test and restores the prior rendering resolution afterward. The old adventure acknowledgement test now enforces the approved town-only hand-in rule. Inventory content-bound comparisons allow one physical pixel of UI Toolkit rounding during scroll relayout.

Run the verified suite with:

```text
unity test . --mode PlayMode --filter Tests.PlayMode.FieldsProductionPlayModeTests --output Library/pr-validation/playmode-final.xml --timeout 600
```

Reports and raw editor logs are local under `Library/pr-validation`. No standalone release build was produced by this merge validation.

## Deliverables

The implementation, authored assets, tests, UI prototype references, Steam draft text, and final actual-game poster are tracked. Raw recordings, intermediate video edits, downloaded media tooling, and temporary test scenes are ignored. The runtime-populated TMP font atlas is a local cache, not an authored change.

The Steam news post remains a saved, unpublished draft. Merging these changes does not publish it or release a new game build.
