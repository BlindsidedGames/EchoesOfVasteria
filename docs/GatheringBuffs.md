# Gathering buffs: Slipstream replacement

Implemented 26 September 2026. Gameplay speed is unchanged by all four new buffs.

## Behaviour and starting balance

All four use a base duration of 120 seconds and a 60-second cooldown. Existing Cauldron buff power applies through the normal duration/effect policy. The new effects have explicit caps.

| Buff | Starting effect | Selected artwork |
| --- | --- | --- |
| Prospector | +100% weight for the selected unlocked task, within its existing skill category. The three preserved quest milestones increase this to +125%, +150%, and +175%. Effect capped at +200%. | P2: gold pickaxe |
| Windfall | Every 10 resource-producing task completions grants 20% of their accumulated resources. Hero and echo completions contribute. Effect capped at 100%. | W1: presents |
| Collector's Instinct | Up to +100% task weight, reduced by lifetime completions: bonus / (1 + completions / 100). Bonus before the completion adjustment is capped at +200%. | C4: bookcase |
| Echo Resonance | An echo's completion marks that task type for 10 seconds. The next hero completion of the same type gains +25% resources and consumes the mark. Echoes cannot trigger their own bonus. Effect capped at 100%. | E4: two purple lights |

Prospector and Collector's Instinct combine additively, with a maximum combined 5x multiplier before relative weight normalization. Existing skill unlocks, category weights, terrain eligibility and task-weight toggles remain in effect. Doubling a weight does not double the final probability.

### Generation timing

The spawn effects apply when new terrain is generated. Already-generated tasks are not replaced on cast or expiry. Because chunks are generated ahead, the effect can become visible later, and affected tasks can remain after the buff expires. Descriptions explicitly explain this. Initial warmup terrain is normally generated before auto-casting begins. These are not immediate reroll buffs.

### Reward accounting

Windfall accumulates the ordinary task reward totals (including Resonance where applicable), before ResourceManager's resource-tier adjustment. Its payout goes through the same resource batch and floating-text totals, with bonus tracking enabled and additional tier-up rolls disabled. It does not recursively count its own payout. Incomplete bundles and Resonance marks are activation-local and disappear with the buff on expiry/run cleanup or a profile load. Gathering cooldowns also reset on profile load. Empty drop results do not advance Windfall.

## Progression and compatibility

- The old Slipstream asset was moved to Prospector with its GUID preserved, so existing asset references still resolve.
- The old four quest IDs remain stable. Their player-facing names, descriptions and rewards now describe the replacement buffs.
- The first milestone unlocks Prospector; the next three unlock Windfall, Collector's Instinct and Echo Resonance respectively, while upgrading Prospector.
- Save schema 3 migrates Slipstream slot names and Cauldron card IDs to Prospector, along with per-quest buff-cast progress. Auto-cast flags and completed quest IDs are retained.
- Existing completed milestones unlock the corresponding new buffs. Slipstream cards transfer to Prospector rather than being duplicated across four collections.
- Prospector's target is stored per save by task ID. A missing/locked target prevents casting and requires choosing a target in town.
- No real saves were migrated during development. Migration was exercised only on synthetic data, including the detached-copy migration pipeline.

## Picker implementation

`Assets/Resources/UI/ProspectorPicker.prefab` is a reusable uGUI modal. `BuffRecipe.prefab` has a Prospector-only target row with an icon and button. Other recipe cards hide it. The modal is lazily instantiated above the current window and destroyed with its UI manager.

The picker uses skill filters, unlocked task icons/names, explicit Confirm/Cancel, fixed header/footer, a clipped ScrollRect, and reusable fixed-height rows. It follows Screen.safeArea and converts control sizes through the existing Canvas scale. A compact landscape arrangement leaves more height for the task list. No keyboard or hover is required. Escape/Android Back follows the Input System escape-key path; physical Android confirmation remains outstanding.

Selecting a row only changes pending state. Confirm revalidates and writes the target; Cancel does not. Starting a run or loading another save closes the picker. Casting and auto-casting never open it. Hidden pickers do not update or rebuild lists. Task weight changes invalidate the existing task-stat UI weight cache.

The picker and target row use the existing recipe/button sprites without dark tinting, the existing dark text material/color, and native button interaction colors. The popup compensates its device-independent layout scale through sliced-image pixelsPerUnitMultiplier so its border pixels match neighbouring canvas controls. Validation renders use the actual 16 reference PPU and 432-unit reference height.

The scroll well uses the recessed UI_Frames_1 sprite from the boosts area, using the same frame > inner scroll area > hidden Image + Mask viewport structure. Inner padding matches ScrollViews exactly: left/right/top 2, bottom 1 native canvas units; the viewport has no additional inset. The redundant title is removed and the skill selector occupies the top row. Task icons follow TaskEntry/ItemEntry: Simple images, SetNativeSize(), and a UI_Frames_24 mask. Oversized art is cropped without distorting its proportions; uniform 2.5x magnification in popup control units preserves readable icon size on high-DPI devices.

The four sprite assets reference unchanged regions of the existing art textures. No new pixel art or source-sheet reslicing was needed.

## Validation

Unity compilation succeeded. The reproducible Editor command is **Tools > Validation > Validate Gathering Buffs**, or:

```powershell
unity command eval 'return GatheringBuffValidation.Main();' --json
```

The harness completed **157 assertions**, including repeated checks across viewport configurations. It covers migration idempotence, the full detached-copy migration pipeline, source-data preservation, effect caps, additive spawn weights, Resonance eligibility/expiry/consumption, Windfall accumulation and inventory payout, required target validation, selection confirmation/cancellation, skill filtering, row reuse, modal raycast blocking, scrolling, layout bounds, touch-button dimensions, native palette matching, sliced-border pixel scale, recessed clipping bounds and native proportions for corn and oak-tree icons.

It uses a temporary preview scene, temporary resources and an in-memory profile. Static references are restored in a finally block. It never enters Play mode or calls save/cloud APIs.

Rendered previews and the result are generated under ignored `Library/BuffReplacement/`:

- `desktop.png`: 1920 x 1080
- `desktop-scrolled.png` / `desktop-scroll-bottom.png`: clipping at middle and end of list
- `desktop-style-comparison.png`: popup alongside the actual recipe-card prefab, with dimming removed for direct style comparison
- `phone-portrait.png`: 390 x 844 (additional robustness check; current mobile settings are landscape)
- `phone-landscape.png`: 844 x 390
- `phone-landscape-3x.png`: 2532 x 1170 at 3x control density
- `phone-landscape-safe-area.png`: landscape with synthetic insets
- `validation.txt`

These are actual Unity uGUI renders, not mockups. The picker preview deliberately includes the full task catalogue to exercise a developed collection; ordinary opening filters out locked tasks.

## Remaining validation

No Development Player or mobile build was made for this change. Physical touch dragging, Android Back delivery, device DPI/safe-area reporting, full-run automation interactions and balance over extended sessions remain unverified. The existing EditMode test assembly has a malformed metadata GUID; this validation command avoids relying on its test discovery. Existing workspace changes were retained. The Main scene was not saved. Early preview validation marked it dirty while creating temporary objects before moving them into the preview scene; those objects were removed, and the dirty flag was left intact rather than risking dismissal of user edits. The retained validation helper now creates objects in a separate staging scene.



