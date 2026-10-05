# Garden / Orchard desktop workspace

One desktop concept, using the preferred first-screen style, with an area switch and three separate views. Garden is active and Orchard is locked in this illustrative state; the Orchard control stays visible because its later access is an explicit user decision. No unlock threshold is supplied.

| View | Purpose | PNG / editable source |
| --- | --- | --- |
| Beds | Select seeds/beds; plant and harvest | [Garden-Beds-desktop.png](Garden-Beds-desktop.png) / [Tesseract source](Garden-Beds-desktop.tsrct) |
| Progression | Access, bed groups and new seed discoveries | [Garden-Progression-desktop.png](Garden-Progression-desktop.png) / [Tesseract source](Garden-Progression-desktop.tsrct) |
| Town | Actual area geometry and focus targets | [Garden-Town-desktop.png](Garden-Town-desktop.png) / [Tesseract source](Garden-Town-desktop.tsrct) |

All three are 1280 × 720. The cream labels, dark panels, peach selection, square controls, thin dividers, font and actual twins/packet art follow the existing Forge/Cauldron and preferred first desktop. Beds no longer contains a town map; Town has no planting inventory or harvest list. Progression has no invented NPC XP curve or perks. The numeric NPC level/XP header has been omitted until its benefit and ownership are defined.

## Selection and navigation

The two controls at the top select the area. Beds, Progression and Town select the purpose within that area. Changing view keeps the chosen area. Once Orchard is unlocked by the eventual authored rule, its own inventory, tree targets and separate town region occupy those same views; these screens do not define its inputs, fruit cycle or actions. Before unlock, Orchard stays visibly locked. No claim is made that the static controls work.

In Garden Beds, select an empty bed and a seed, then the single Plant action commits the pair. All seeds can open the expanded picker for that already selected bed. Harvest belongs to a ready bed. Town's numbered markers correspond to the focus buttons and the bed numbers; it shows the original field and reclaimed field at actual native render scale. No orchard trees occupy crop patches. Barkley and Gill systems are parked outside this design scope.

## Values are illustrative, not approval

The four open beds, two locked beds, 4/17 discovered packets, owned quantities and twelve-minute growing state are sample data. The paired 1–2/3–4/5–6 progression rows reflect the existing proposed six-bed visual staging, not approved level gates, costs or a finalized quest sequence. Orchard is confirmed to unlock later, but its position relative to those crop stages is undecided. The seventeen discovery slots are the current crop-catalogue proposal; the twenty-two art families still require explicit mapping.

The Town image is a native crop from the actual V4 preview render. It establishes placement, soil, fences, gate paths, farmers and surrounding woodland. Its existing crop art is not a simulation of the Beds screen's particular crop assignments or ready/empty states. Number markers are editable UI overlays. No gameplay scene or source art was changed.

Flora and Tillman own the interface/progression direction; **what their level does remains undecided**. Shared/separate XP, award events, thresholds, growth-speed benefits, perks, watering and automatic watering have no approved rules supplied here. Adventure Farming skill mechanics are separate facts, described in the [shared baseline](workspace-baseline.md).

## Copy audit

Every on-screen string identifies a control/item, names an action or shows a meaningful value/state. Retained: navigation, NPC names, Garden/Orchard, Locked/Available/Ready/Growing/Empty, seed identities/counts, bed numbers, discovery/capacity values, Harvest/Details/Plant and focus targets. There are no quotes, decorative subtitles, instructions, question-mark explanations, placeholder warnings or design disclaimers inside the interface. The single Plant action occurs only in Beds. Progression displays no numeric unlock thresholds or prices.

[Verification](workspace-verification.json) records all visible strings, bounds, contrast, editable-layer counts, artifact/source hashes and preservation. [Library delivery](workspace-library-delivery.json) records three newly named image identities and the portable editable-source package. Earlier images and Library versions remain intact under their existing names. No Unity/desktop automation, gameplay/save change, commit, push or release is involved.
