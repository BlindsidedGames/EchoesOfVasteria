# Gameplay UI design system

The approved runtime direction is the dark HUD/Cauldron study, extended to the game's existing menus. Original content, routes, progression gates and actions remain the functional reference. The earlier pixel-for-pixel migration is historical; new work should not reintroduce its decorative sprite frames or nested panels.

## Shared sources

| Concern | Source |
| --- | --- |
| Dark palette, typography, borders, control states and screen arrangements | `Assets/Resources/UI/Gameplay.uss` |
| Composition, aspect-preserving icons, progress bars, toggles, slider/scroll treatment | `Assets/Scripts/UI/Toolkit/ToolkitGameplay.cs` |
| Existing semantic controls and localization bindings | `ToolkitControls.cs`, `ToolkitTextBinding.cs` |
| Window safe-area geometry | `ToolkitWindowLayout.cs` |
| Panel scale, sorting ownership and build inclusion | `ToolkitPanel.cs`, `Assets/Resources/UI/ToolkitPanel.asset` |
| Font asset | `ToolkitTheme.gameplayFont` |
| Live values and actions | Existing game services and screen presenters |

`Theme.uss` still supplies structural rules for migrated screens. `Gameplay.uss` overrides their visual presentation. Some imported inline geometry remains; this is not a claim that every dimension is controlled by one token.

## Rules

- Dark only; no runtime light-mode toggle.
- Floating navigation, no full-width solid navigation bar or game-title branding.
- Sharp corners, thin panel edge, subtle top highlight, shallow button bevel. Selected and disabled states must remain distinct. Keyboard focus must not become a persistent mouse-click decoration.
- Use one outer surface. Separate internal groups with layout and whitespace, not recursively nested decorative panels.
- Start with the heading. No decorative eyebrow labels or pill-shaped section tags.
- Preserve original content and progression gates. Never shorten a layout by silently dropping a statistic, action, tooltip or tab.
- Use consistent icon/button boxes. Keep sprite aspect ratios with `ScaleMode.ScaleToFit`; a tall tree must remain tall. Nine-slicing applies only to deliberately retained frame art, never resource or character artwork.
- Use content-driven text height and clipping scroll viewports. Do not reproduce old TMP line-height arithmetic in new TextCore layouts.
- Keep numerical values aligned; reserve enough room for large values and localized labels. Use meaningful symbols for compact toggle states, with labels/tooltips where needed.
- Keep gameplay automation independent of a panel's visibility. Subscribe when shown and unsubscribe when hidden.

## Authoring

```csharp
theme.Apply(root);                 // Existing structural styles
ToolkitGameplay.Apply(root, theme); // Approved dark presentation
root.AddToClassList("menu-surface");
var entries = ToolkitGameplay.Scroll(root, "entries");
var heading = ToolkitGameplay.L(entries, "", "heading");
var binding = new ToolkitTextBinding(heading, definition.title);
ToolkitGameplay.B(root, "Confirm", Confirm, "primary");
// Dispose the binding and unsubscribe game events when this view closes.
```

Use localized definitions/templates for new copy rather than concatenated English fragments. Existing presenters still contain hardcoded strings; full localization coverage is not established by this styling migration. External player names and diagnostic messages must disable rich text.

Use `ToolkitGameplay.StyleSlider` / `StyleScroll` after constructing legacy shared controls. Use `ToolkitGameplay.SetToggle` for compact functional states. `ToolkitControls.RepeatWhileHeld` preserves conversion hold-repeat input.

`ToolkitPanel` uses a 768×432 reference and height scaling. Do not compensate for that independently in each popup. Each visible view owns its cloned PanelSettings. Keep the authored Resources template so player builds include UI shaders and text data.

## Review loop

1. Inspect the original surface, its source bindings and representative progression states.
2. Apply shared components while retaining all actions and content.
3. Capture the actual Unity runtime, including populated, empty, selected, disabled and expanded states.
4. Inspect alignment, density, text wrapping, clipping, hierarchy, contrast and image proportions.
5. Exercise real callbacks against disposable offline saves, including automation with the view closed.
6. Correct and recapture. A compiled view or a route-opening check is not visual approval.

Check desktop and narrow landscape layouts, large text, keyboard and actual touch input. Emulated dimensions do not prove device input behavior. Track gaps explicitly in the [runtime worklist](ui-migration/approved-runtime-2026-09-27/WORKLIST.md).

## Performance boundaries

Retain stable rows, coalesce visible updates, avoid rebuilding unchanged lists, and keep simulation separate from rendering. Measure before claiming gains. Save/cloud protection applies to every validation pass. Never upload test progression.

## Shared spacing and selection controls

Runtime spacing uses `--space-tight` (2), `--space-control` (4), `--space-section` (8), and `--space-panel` (12), in the panel reference coordinate system. Use those roles for margins and padding; do not introduce per-window 3/5/6/10 pixel spacing variants. Window top edges align below navigation at 44 reference units. Remove nested layout padding where the outer panel already provides the inset.

Boolean controls use `ToolkitGameplay.SetToggle`: a shared 12-unit square with a drawn checkmark and a 22-unit minimum hit target. An unchecked control is an empty outlined square, not a dash. Labelled checkboxes keep their label alongside the indicator; do not fill the whole label row as a selected button. Preserve each game's existing toggle callback. Skills selectors keep their level in normal layout below the proportional icon, with a consistent font size. Navigation close is a single text × control, never an old framed sprite inside another button.

These are implementation rules, not visual acceptance: inspect checked, unchecked, hovered, disabled and focused controls on each window during its own review.


## Window dimming and town conversations

Town menus share a 60% black, full-viewport backdrop with native 3-unit blur, above the run HUD and below the menu content. It blocks clicks to the dimmed HUD. Only controls use safe-area bounds; the dimmer must always reach all four screen edges. Do not restore the tiled wood background. The top navigation bar stays sharp, undimmed and interactive above the backdrop so players can switch screens without closing first. The scene and lower gameplay HUD remain blurred.

Dad-o-cado is an in-scene conversation: his button stays below the town character and his content-sized speech bubble opens above-left with its tail pointing towards him. It does not open the run-summary overlay or a dimmer. A slim bottom countdown empties over three real seconds and then dismisses it. Clicking/tapping the bubble, keyboard submit, or right-click/Escape dismisses it early. Choosing another joke restarts the timer; opening a town menu or starting a run hides it.


## Skills entries and summaries

Selected-skill summaries show bonuses contributed by that skill. Cross-skill XP stays with its source skill as the deliberate exception to target-skill categorization. Keep global active-slot controls and complete totals available. Frame existing effect/task icons at a consistent size, but render the art at sprite.rect size × 16 / sprite.pixelsPerUnit inside a centered mask filling the frame to its inner border on all four sides. Never fit different sprites to a common square or inherit inline-icon margins. Keep next-level metadata beside the bold name, and use faint overlay dividers without increasing entry height. Compact simple stat/resource/XP descriptions, retaining full conditional echo mechanics.

Skill-entry state borders: warm amber `#d0a166` for unlocked entries, muted purple `#b38ac9` while an activatable milestone is active. Active styling takes precedence; disabling restores amber. Locked entries retain the neutral border.

Checkbox painter paths must include `contentRect.position`, not just its dimensions, so ticks remain centred with normal and focus borders. Skill summary lines use existing effect icons in aligned masked slots at the shared sprite PPU; select icons from structured bonus metadata, never by parsing translated descriptions.

Task statistics retain the original two information columns and sorting controls. Frame task art at native PPU with a full-inner-border crop, use subtle row dividers, and keep task IDs secondary to names. The task-weight checkbox is labelled Boost: amber means known and purple means boosted; it does not enable or disable task spawning.

Enemy statistics share the Tasks framed-art and row-spacing rules. Keep the four original combat/reveal columns and full-width kill-progress bar. Preserve unknown-stat masking and all sorting modes; the distance slider previews existing scaling without changing gameplay.
