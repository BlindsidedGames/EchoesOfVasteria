# Forge first design pass — 28 September 2026

Final review images: `forge-info-preview.png`, `forge-inventory-preview.png`. Earlier numbered/scenario captures in this folder show intermediate iterations and test states.

## Changes
- Forge owns the embedded resource inventory in its own visual tree. Inventory/Info tabs switch only the right column. Normal standalone inventory presentation remains available.
- Six inventory columns at desktop size. Forge gear, cores and embedded resources use common native sprite PPU with centred masks.
- Pending/Equipped comparison aligns the union of stats; absent pending stats show zero so losses remain visible. Quality uses the existing UpgradeEvaluator calculation. Gains/losses receive subdued green/red text; stat icons have name tooltips.
- Equipment totals show summed values and the selected equipped slot contribution. Selecting another slot updates both comparison and totals.
- Complete legacy history data retained in ten expandable sections, using the existing ForgeStatisticsPresentation calculations. Crafting history starts expanded; other sections start collapsed. Equipment totals remain visible above them.
- Smelting recipes sit below crafting with visible quantity fields and native callbacks. Three- and four-recipe layouts validated.
- Shared dark palette, spacing, check controls and scroll treatment. No gameplay or automation logic edited.

## Validation
Connected Unity 6000.6.0f1 Editor, disposable developed save, Ivan level 480. Normal identity and source guards restored afterward; see `restoration.json`. Temporary harnesses are ignored under Library. Steam, UGS and feedback networking were blocked during validation. Real progression was not loaded or overwritten.

`checks-forge.txt` validates embedded inventory/panel picking, six columns, selection, tab transitions, manual craft, Replace, all ten history groups, reopening and desktop conversion visibility. `checks-final.txt` validates quantity control geometry, editing a conversion amount, a Smelt callback, lower-tier core conversion visibility, and all four fields. Screens inspected at 1280x720 and 844x390. Sprite ScaleToFit checks alone do not prove native PPU; the Forge frame code also explicitly sets sprite dimensions from rect size * 16 / pixelsPerUnit.

Two visual iterations corrected off-screen Smelt controls and a collapsed flex quantity field before final capture. Compilation verified after guard restoration. No commit made.

## Remaining limits
This is the first visual iteration, not a full Forge gameplay regression suite. Sustained autocrafting throughput, every locked/early-save state, extreme localization/text scale and physical touch devices remain untested. The preexisting shared history presenter still emits formatted sections which this view separates into aligned rows; future localization should expose structured presentation records instead. Native panel picking and dispatched callbacks were tested without foreground computer control.

## Resource polish follow-up
Core tiles now show owned quantities only; selected recipe retains craft capacity. Embedded resource tiles are 32x38 with 2-unit gaps, native PPU art in larger masks, readable regular-weight quantities, and neutral borders. The selected resource keeps an amber border with its name and a tier-coloured star in the header. Standalone inventory retains its previous dimensions and timed highlighting. Highlight-to-scroll uses the larger row pitch in Forge. Recipe costs align with their icons and insufficient quantities receive the existing muted-red class; no crafting calculations were changed.

Validated six columns, one visible owned label per core, resource selection, larger-row scrolling, and shortage state. A strict width equality initially failed because panel pixel rounding resolves 32 reference units to 32.40002; the corrected geometry check allows half a reference unit, and the screenshot was inspected. See checks-resources.txt and checks-resource-shortage.txt. Final image: forge-resources-final.png. Temporary validation guards restored from ForgeResourcePolishIsolation after testing.

## Core framing and odds follow-up
Core artwork alone is framed; quantities sit outside below, matching equipment slots. The pie now emits triangle fans instead of Painter2D arc paths, covering majority and full-circle sectors. No probabilities changed. Bronze's live fixture distribution is 100% Eznorb and now renders a complete circle; a separate synthetic single-slice fixture also renders correctly.

Odds use a bounded absolute overlay with aligned rarity/name/percentage rows. Mouse hover previews it; click or navigation submit pins it; a second click, an outside press within Forge or Escape dismisses it. Leaving the chart/overlay closes an unpinned preview after a short delay. The popup does not participate in layout. Captures: forge-odds-bronze.png, forge-odds-full-circle.png, forge-odds-mixed-popup.png and forge-odds-phone.png. checks-odds.txt records no layout movement, containment, hover/leave, click pinning, outside dismissal and Escape. Input events were dispatched through UITK in the Editor; physical mobile touch hardware remains untested. Temporary save/network guards restored in restoration-odds.json; Play stopped.
