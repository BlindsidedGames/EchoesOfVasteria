# Independent timber data/reference review

A separate read-only reviewer checked the approved setup against the immediately preceding local baseline. It found no ID collision, broken reference or unintended loss of base-resource access. The reviewer did not write files or operate Unity.

| Family | Stick / Log IDs | Known outlined glyphs |
| --- | --- | --- |
| Starter | 2 / 57 | 236 / 238 |
| Oak | 71 / 72 | 240 / 242 |
| Birch | 73 / 74 | 244 / 246 |
| Spruce | 75 / 76 | 248 / 250 |

All 75 Resource definitions have unique IDs and save names. Each new GUID has one matching metadata file. The eight tree assets retain every prior non-resource field: ranges, weights, extra-drop chances, skill requirements, XP, durations and spawn settings. Both atlases contain 252 unique glyph indices, and the active Toolkit inventory registers each new resource once. Original Log57/Stick2 identities and fields are preserved except their known inventory art.

## Integration consequences

Species timber is **not a substitute for base Log/Stick**. Eight Barkley quests still require the original resources: Fence2 requires Stick 1,500 and Log 1,250; Fence3 requires 5,000 of each. Species trees no longer contribute to those holdings or base-only requirements. Base Medium/Large Trees remain in Woods, Beach, River and Farmlands task lists at positive woodcutting weights, Logging 1/6, with maximum-distance enforcement disabled. Their drop tables remain intact, so base access survives.

The six additions have no authored quest, recipe or chest-table references. Their current uses are independent discovery, inventory, tiers and the existing Alter Echo logging card/generator systems after earning. They are excluded from food mixing. Old base tiers do not transfer to new species: a species begins at Tier1. These are material consequences of distinct resources, not grounds to silently broaden this setup into recipe, quest, balance or Alter Echo redesign.

The current completion cohort grows from 69 resources +94 quests to 75 +94. A previously complete cohort therefore reads **163/169 =96.45%** until six new discoveries. This is the existing `StaticReferences.CalculateCompletionPercentage` behaviour, not a new progress-reset migration.

The retired UGUI inventory definition omits the new resources, but the Main component is disabled with an Awake guard. The active Toolkit inventory includes all six. No legacy UI definition was rewritten merely to match an inactive consumer.

## Evidence

- `Assets/Resources/Resource Items`: IDs, stable GUIDs, values and save names.
- `Assets/Resources/Tasks/Woodcutting`: all eight real tree drop tables, plus the [immediate-baseline diff](focused-existing.diff).
- `Assets/Resources/Quests/Barkley/Fence 2.asset` and `Fence 3.asset`: unchanged base-only costs.
- `Assets/Scripts/Tasks/ResourceGeneratingTask.cs`: actual reward, batching and floating-text route.
- `Assets/Scripts/Blindsided/SaveData/StaticReferences.cs`: current completion cohort calculation.
- `Assets/UI/Toolkit/ResourceInventory.asset`: active native registration.

The requested actual adventure-drop feedback check is separate runtime evidence; this review does not claim to verify animation by inspecting data alone. No commit, push, release or recipe redesign was performed.
