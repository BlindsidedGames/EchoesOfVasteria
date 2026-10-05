# Garden and later Orchard — options for review

The preferred desktop screen is the base. Orchard will be a **separate town area that unlocks later**, preserving crop beds. No Orchard mockup is finalized in this checkpoint; choose its interface arrangement first. None of the options below assigns an unlock level, cost, XP benefit, tree count or fruiting rule.

| Option | How it extends the main screen | Minimal trade-off |
| --- | --- | --- |
| **A. Garden / Orchard switch — recommended** | Keep the farmers' workspace and its three columns. A two-way area switch changes the relevant inventory, numbered bed/tree list and town-region view together. Garden stays the first screen; Orchard has its own tree targets and Focus action. | One extra switch; each view stays short, readable and unambiguous about its target. Fits the preferred desktop and can reflow for narrow screens. |
| B. Shared list with Garden and Orchard sections | Keep one inventory and list, with separate crop-bed and tree sections; selecting a target changes the right-hand region view. | Both areas can be reviewed together, but the list is longer and mixed crop/tree inputs require clearer filtering and target labels. Narrow navigation gets busier. |
| C. Separate Orchard page in the farmers' workspace | Preserve Garden entirely; add an Orchard entry opening a matching page with its own inventory/list/region view. | Strong separation and familiar components; more navigation and duplicated page structure than the area switch. |

For A, Orchard is visibly unavailable before its later unlock. A locked state can lead to the game-authored requirement view once those requirements exist; no Level 8 gate, material price or fabricated requirement is supplied by this design. Crop beds and tree targets have distinct labels/identities. Switching areas must not accidentally apply a selected crop pack to a tree or spend anything. The Orchard inventory and its final action labels depend on the still-open acquisition/care mechanics; no new currency or input is assumed.

A shared farmers' workspace does not mean trees occupy crop beds. All options use the separately audited orchard ground and focus that separate region. A same-screen interface choice also does not decide whether the eventual fruit cycle is productive, how it is funded or whether watering exists.

## One concept and exact planting flow

Main, narrow and detail are three views of **one concept**. Select an empty numbered bed and a seed; Plant commits that pair. All seeds opens the optional expanded picker for the already selected bed. Plant there commits and returns to the main workspace with that bed growing; Back returns without planting. Narrow uses Plant in the selected bed row for the same commit. The expanded picker is not an extra mandatory step after desktop Plant.

The main and detail renders are preserved exactly. The narrow correction changes only the selected row's vertical position, so its Plant button stays within the highlight. [Before](narrow-highlight-before/Farm-narrow-landscape.png), [after](Farm-narrow-landscape.png) and [geometry/render verification](narrow-highlight-verification.json) show the defect and fix.

## What is decided and what is only a proposal

| Topic | Grounded status |
| --- | --- |
| Main UI direction | User prefers the first desktop screen. Narrow is its reflow; detail is its optional picker. |
| Crop placement | V4 visual direction accepted: retain original frontage, lower the back fence, use proper fence parts and dual gate approaches. Six-bed geometry has been previewed. [V4 evidence](../farming-expansion-2026-09-30/revision-4/README.md). |
| Crop 2→4→6 sequence | The engineering/planning package recommends three Barkley stages: original beds, reclaimed stump field, southeast extension. The isolated development milestone starts with two Radish beds. This is a proposed progression/content sequence, not an approved level/cost/XP table. [Implementation sequence](../farming-expansion-2026-09-30/implementation-sequence.md) and [proposed contracts](../farming-expansion-2026-09-30/proposed-contracts.md). |
| Legacy Fence quests | Existing authored Fence1/2/3 award +50/+100/+150 adventure distance and have their own crop/quest/material requirements. They are not new crop-bed capacity grants or new NPC-level requirements. The current plan preserves meaningful history without automatic 2/4/6 grants. [Quest/migration contract](../farming-expansion-2026-09-30/proposed-contracts.md). |
| Existing adventure crop levels | Radish TaskData requires Farming level 1; Corn requires 7. These are existing skill requirements, not Flora/Tillman NPC-level benefits or new Orchard gates. [Radish](../../../Assets/Resources/Tasks/Farming/Radish.asset), [Corn](../../../Assets/Resources/Tasks/Farming/Corn.asset). |
| Orchard access | New explicit decision: separate area, unlocks later. “Later” has no agreed position relative to the four/six-bed stages, level threshold, price or quest prerequisite. Earlier notes treating later Orchard access as optional are superseded; mechanics/capacity remain open. [Updated orchard notes](../farming-expansion-2026-09-30/optional-orchard.md). |
| Seeds / retirement | Fresh pack discovery for everyone through plant drop pools; old crop access grants no packs; no Alter Echo compensation. These are user decisions. Odds, final recipe requirements and Orchard inputs are not. |
| Flora and Tillman level | **What their level does is currently undecided.** Level 8 and 382/600 XP are display placeholders. Shared/separate XP, earning events, thresholds, speed/yield benefits, unlocks and perks are unapproved. Existing Farming skill levels do not settle this NPC progression. |

The earlier 10 Log + 20 Stick first-build example, 30-minute/2-hour growth scenarios, pack-income ranges and development recipe values are prototype/measurement settings. They are not approved release prices or progression rules. The current UI's counts and twelve-minute state remain illustrative, as documented outside the images.

The next UI decision is A, B or C. Once selected, prepare a matching later-locked and unlocked Orchard state using the existing desktop components. Keep mechanics questions separate: where its unlock belongs and what the farmers' level does still need the user's design.
