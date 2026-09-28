# Run HUD and Cauldron styling study

This is an isolated, Editor-only UI Toolkit proposal for the run HUD and Cauldron. The latest iteration is in **polished/**; the older root-level captures and smoke scripts document superseded designs. It is not a production UI cutover.

## Open and compare

In the main Unity project: **Tools → Vasteria → Style study → Cauldron** or **Run HUD**. No Play mode is needed. The Editor toolbar, outside the game preview, contains:

- **Dark mode**, shared by both screens. Switching changes semantic colours without recreating the view or losing selection/scroll state.
- **360 px**, **130% text**, and **Deutsch** for layout checks.
- **mixed / maxed / empty** sample states and **Reset sample**.

Adventure and Cauldron switch routes inside the preview. Other destinations are disabled because this study is restricted to these two screens. The theme toggle is not part of the demo scene or player HUD.

## What changed

- Warm original-UI light colours: peach `#E4A672`, plum text `#3F2832`, terracotta `#B86F50`. Dark mode uses warm charcoal with peach accents; semantic health remains green.
- One flat Cauldron surface. Its columns, collections, details and ingredient lists have no decorative enclosing panels. Only functional controls receive selection/hover states. Confirmation dialogs are sibling overlays, not recursive content panels.
- All **30 eligible ingredient assets** inside a bounded native ScrollView. The original grid had no search or category filters; those added prototype controls have been removed. Wheel, touch scrolling support from the control, and Home/End/PageUp/PageDown handling remain available with hidden scroll chrome. Mix actions stay visible below the viewport.
- The arbitrary **+ Taste** button is gone. Start/pause represents automated tasting; the sample rate and cost come from the current Cauldron config. The fixture does not run a real-time reward simulation.
- Resource cards, buff cards and Eternal Boons are distinct. Eight original tier colours are visible as small stars beside the progress bars, coloured progress fills and labelled swatches in the legend. Each resource/buff card has a progress bar using the production calculator’s progress between its current and next tier thresholds; maxed cards show a full bar. Tier text remains in the tooltip, details and legend; adjacent icon tier labels are removed. Raw ingredients do not invent a rarity field absent from their assets.
- Resource and buff tiers/counts use `CardTierCalculator` and the current thresholds. Eternal uses the actual seven boon assets, count/exponent effects, and no fabricated tier cap.
- Mixing shows the exact full stacks to consume and the computed stew before confirmation. Cancel leaves the fixture unchanged. Mix all pairs uses the current ordered-pair rule and retains an unpaired last stack.
- The run HUD keeps floating navigation, an open centre, compact corners, a separate health fill, circular skill timers and explicit autocast states. A fixed-size repeat-arrow symbol indicates autocast: highlighted when enabled, muted and slashed when disabled. Tooltips name the state. Buff slots keep identical dimensions in both states.
- Narrow Cauldron layouts use Mix / Tasting / Collection routes rather than squeezing three columns. Tab rows share equal-width buttons and a common row height instead of wrapping into uneven rows. Resource and buff section headers share the same heading/star/bonus layout. Enlarged text wraps; mobile card grids use two columns. On narrow run views, the full run summary is behind Run breakdown.

## Design system

`Assets/Editor/StyleLab/StyleStudy.uss` defines semantic colour roles (`surface`, `text`, `muted`, `control`, `hover`, `line`, `accent`, `on-accent`, `selected`, `field`, `health`), type roles and shared components. Theme changes are class changes. Body/small/heading/title/amount roles have a 130% variant. HUD spacing retains 24px outer insets, 8px padding/group gaps and 4px closely related gaps; the Cauldron uses 24px column separation. Narrow screens use 12px outer insets.

All sprites use `ScaleMode.ScaleToFit`; non-square resources are not forced into square proportions. The tier colours were sampled from `Assets/Art/Packs/Cute_Fantasy_UI/UI_Framesinnar.png`:

| Tier | Colour |
|---|---|
| T1 | `#B86F50` |
| T2 | `#6C7C9D` |
| T3 | `#FEAE34` |
| T4 | `#ED7614` |
| T5 | `#0098DC` |
| T6 | `#33984B` |
| T7 | `#C42430` |
| T8 | `#DB3FFD` |

Prototype strings live in `Copy.en.json` and `Copy.de.json`. German is a layout stress translation, not an approved game translation; proper asset names retain their existing names. This is a starting catalogue boundary, not integration with the production localization pipeline.

The attached gameplay style guide informed flat surfaces, concise copy, disclosures, semantic tokens, localization checks and scrolling without visible chrome. Its referenced web-project paths do not exist here, so those project-specific systems were not imported.

The following external guidance informed concrete choices:

- Visible state, consistent terms, short copy and reversible cancellation: [Nielsen Norman Group usability heuristics](https://www.nngroup.com/articles/ten-usability-heuristics/).
- Stable content area across tabs and keyboard navigation: [IBM Carbon tabs guidance](https://carbondesignsystem.com/components/tabs/usage/).
- Semantic light/dark roles rather than a blanket colour inversion: [Apple Dark Mode guidance](https://developer.apple.com/design/human-interface-guidelines/dark-mode).
- Controls have a 44px minimum height, exceeding the cited 24px minimum size baseline: [W3C target size guidance](https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum.html). This does not claim WCAG certification or a completed accessibility audit.

## Current captures

All are real Unity 6000.6.0f1 UITK panel renders, not generated mockups. Companion `.txt` files report viewport geometry, scroll range, fixture counts and sprite aspect checks.

| Scenario | Capture |
|---|---|
| Light Cauldron, mixed collection | [1920 × 1080](polished/cauldron-light-1920x1080-en-100-mixed.png) |
| Dark Cauldron, mixed collection | [1920 × 1080](polished/cauldron-dark-1920x1080-en-100-mixed.png) |
| Light run HUD | [1920 × 1080](polished/run-light-1920x1080-en-100-mixed.png) |
| Dark run HUD | [1920 × 1080](polished/run-dark-1920x1080-en-100-mixed.png) |
| Ingredients scrolled to the bottom | [Dark desktop](polished/cauldron-dark-1920x1080-en-100-mixed-scrolled.png) |
| Empty inventory / disabled actions | [Light desktop](polished/cauldron-light-1920x1080-en-100-empty.png) |
| Maxed normal collections / Eternal | [Dark 1920 × 900](polished/cauldron-dark-1920x900-en-100-maxed-eternal.png) |
| Ingredients, German, 130% text | [Dark 360 × 800](polished/cauldron-dark-360x800-de-130-mixed.png) |
| Buff collection, German, 130% text | [Light 360 × 800](polished/cauldron-light-360x800-de-130-mixed-buffs-collection.png) |
| Tasting, German, 130% text | [Dark 360 × 800](polished/cauldron-dark-360x800-de-130-mixed-taste.png) |
| Confirmation, German, 130% text | [Light 360 × 800](polished/cauldron-light-360x800-de-130-mixed-confirm.png) |
| Run HUD, German, 130% text | [Dark 360 × 800](polished/run-dark-360x800-de-130-mixed.png) |

The world behind the HUD is the user's screenshot, cropped to exclude its old corner UI. It is not a live run. The world-space bars/reward text were not redesigned. Sample stock, counts, health, distance and cooldown progress are fabricated fixtures; they are not a real save or naturally reachable progression claim.

## Mechanics and source boundaries

`StudyModel.cs` reads current resource, task, buff, boon and configuration assets. It discovers 69 resource assets, 30 eligible foods, 13 buffs and seven boons. Resource collection display excludes `DisableAlterEcho` assets. All eligible assets are treated as unlocked for the fixture; quest/save unlock bindings are not simulated.

Checked against:

- `Assets/Scripts/Upgrades/CauldronManager.cs`: full-stack mixing, pairing and tasting behavior.
- `Assets/Scripts/UI/Cauldron/CauldronMixingPresentation.cs`: two-food selection and stew presentation.
- `Assets/Scripts/Upgrades/Cauldron/CardTierCalculator.cs` and `Assets/Resources/Cauldron/CauldronConfig.asset`: thresholds, maxing, configured 100 rolls/s and 1 stew/roll.
- `Assets/Scripts/Upgrades/Cauldron/AEResourceGroupClassifier.cs`: resource grouping.
- `Assets/Scripts/UI/Cauldron/CauldronCollectionPresentation.cs`: group tier and effect presentation.
- `Assets/Scripts/Upgrades/InfinityCauldronStatSO.cs`: actual Eternal names, sprites and count-based effects.

The preview mirrors reward-weight eligibility using fixture counts and Eva level. It does not instantiate `CardPoolManager` or gameplay managers, because those reach live progression. Skill-milestone cost/card multipliers, real unlock filtering, reward processing and save-backed automation are not wired. Those must be bound and verified before this becomes a gameplay implementation.

## Verification

[Interaction results](polished/interaction-checks.txt) and [reproduction script](polished/PolishChecks.cs.txt) cover native wheel scrolling, End reaching the last ingredient row, stable workspace bounds across Resources/Buffs/Eternal, theme/scroll retention, distinct-food mixing, exact-stack confirmation, cancellation, correct fixture gain/consumption, disabled empty states, maxed-collection Eternal unlock and a settled final layout.

Native renders were visually inspected and revised to fix collapsed narrow search inputs, awkward card wrapping, missing radial rings, mobile HUD crowding and translated confirmation-button overflow. Sprite checks report zero invalid scale modes. Editor compilation succeeds. The capture helper's static-event analyzer warnings are conservative: all three subscriptions are removed by its shared cleanup callback on completion, error, assembly reload or quitting.

Not established: physical-device touch behavior, screen-reader output, controller navigation, mobile safe areas/keyboard occlusion, all languages, production performance, live save binding or a player build. The style study is reviewable and interactive, not a claim that the whole game UI is finished or objectively proven superior through user testing.

## Reproduce safely

```powershell
unity command eval --project-path . --code 'return Vasteria.StyleLab.VasteriaStyleLab.CaptureView(true,1920,1080,true);' --json
# Wait for the matching polished PNG and TXT to update.
unity command eval --project-path . --code 'return Vasteria.StyleLab.VasteriaStyleLab.CaptureView(true,360,800,false,1.3f,"de","mixed","buffs","collection");' --json
```

`CaptureView` creates a disposable preview scene with only a UIDocument, renders to a texture, and destroys the capture objects afterward. Editor-only files are excluded from player builds. No Play mode, game saves, cloud services, production scenes or progression settings are used. Temporary validation code lives under ignored `Library`; its text copy is retained with the results for reproduction.

Latest layout refinement: the standalone Cauldron title/header row is removed. Eva's portrait, level and XP now sit under Tasting, allowing the three main columns to use the former header space.

Button/modal refinement: pointer focus no longer applies contrasting side edges, pressed states no longer invert the bevel, and keyboard focus uses a uniform outline. Main surfaces share a thin warm border with a top highlight. Mix All uses an icon/name/quantity list, exact consumed-stack count, total stew and a pair-count action. See polished/focus-check.txt for native focus-state validation.

Adventure spacing refinement: navigation-to-HUD spacing follows the measured navigation height plus 8px. Five equal buff slots span the player panel width, with 8px desktop gaps (4px narrow). Player name/health spacing and run-summary gaps are reset to the shared rhythm. Return on Death retains Toggle behavior with themed square checkmark, matching button borders and sample-state retention.

HUD follow-up: restored the compact two-line Run breakdown action (+50% resources in the sample), with elapsed time, distance/damage/kill rates and earned/projected resource totals inside its popup. The hero now has a larger framed, aspect-preserved portrait; health, regeneration and defense above the health bar; and damage, attack rate, critical chance/damage and movement icons below. Stat sprites come from the existing StatIconLookup. Ability boxes are 58x60 logical pixels with clockwise perimeter progress and the existing top-right echo sprite when automation is enabled; repeat symbols were removed. Preview cooldowns and run values remain fixtures, not gameplay bindings.

Validated in Edit mode: dark 1920x1080, dark 360x800 with German and 130% text, and the light desktop breakdown popup. Captures are in polished/run-*.png; the narrow capture reports 17 sprite images and zero non-aspect-preserving modes. Compilation passed. One CLI request timed out during reload; retry succeeded with no subsequent errors.

Latest HUD correction supersedes the previous ordering: defense left and health right above the bar; regeneration left and movement right below; four combat stats along the bottom. Automation uses the existing echo sprite in a 40px aspect-preserving box overlapping the top-right corner. Removed the Farmlands title surface and restored a compact distance track with the original hero/reaper sprites. Run breakdown follows RunBreakdownEntryUI's two-line earned/per-minute format, projected totals in parentheses, with an explanatory bonus note; overflow has a visible scrollbar. All values remain isolated preview fixtures.

Final compact hero pass: removed all four combat readouts from the hero panel. It now contains only portrait, defense/health above the bar, and regeneration/movement below. Width is 360 logical units at normal text scale and 420 at 130%, capped to the available viewport; narrow layouts use the safe available width. Re-captured desktop dark at 100%, desktop light at 130%, and 360px German at 130%; the retained values fit without overlap, and sprites remain aspect-preserved.

Run breakdown rebuilt from the original baseline capture (`docs/ui-migration/baseline-supplement-2026-09-27/captures/08-run-breakdown.png`), Main.unity's ResponsiveGrid3ColumnLayoutGroup (three columns, 26-unit rows), and RunSlotItemNew.prefab. The former tall list was the wrong structure. Current preview restores the wide header (rate legend/title/timer), three-column icon-and-two-line resource grid, and split footer (dismissal/distance left, damage/kills right). No large Close button or visible scrollbar. Wheel/touch overflow remains available on narrow screens, with a single-column layout and fixed header/footer. Twelve synthetic resource entries exercise the layout; names are available on hover and icons keep aspect ratio. Checked light desktop 100%, dark desktop 130%, and 360px German 130% captures. Outside click, right click, and Escape dismiss the overlay.

Toolbar follow-up: moved the enlarged distance display into the navigation row immediately after Library on desktop (236 logical units wide, 36px aspect-preserved endpoint image boxes, 8px track). Narrow layouts place it below navigation. Return controls now follow the original right-aligned stack: Return to Town, then a smaller Return on Death button, without a checkbox. These remain preview actions. Rechecked desktop 100%/130% and narrow German 130% captures.
