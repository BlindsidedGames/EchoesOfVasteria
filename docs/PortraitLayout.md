# Portrait view mode

The dedicated portrait profile activates at a 4:5 or taller window (width/height <= 0.8). This leaves usable space for the lower interface rather than switching in an almost-square window. Landscape keeps its existing presentation; mobile autorotation permits portrait.

## Layout

Adventures use an edge-to-edge 16:9 camera viewport from the top of the screen, behind the Dynamic Island. Interactive UI still respects the device safe area. Town fills the screen while no menu is open. An open menu uses an opaque surface all the way to the screen edges, with its controls inside the safe area. The interface fills the area below it, with content padding inside the panel rather than gaps around a desktop window. Bottom navigation provides Adventure, Hub, Quests and Town, with separate Statistics and Settings icon buttons on the right. Its background continues through the bottom safe area, while the buttons remain above the home indicator. Secondary choices open as full-width pages; their lists scroll on shorter screens. Portrait omits window-close and desktop-quit X controls. Summaries use Continue.

Run progress overlays the bottom of the adventure viewport; health and abilities share a 36-unit-high strip immediately below the map. Neither covers the character. Portrait menus do not blur the run HUD. The health readout is frameless; combat stats open downward in two columns without a surrounding card or character-name heading. Locked ability labels remain on one line. Return actions and Summary share a compact row. Dialogue uses a bounded lower sheet, separate from the map, with a fixed action and scrollable long text. Abilities yield while dialogue is visible, and simultaneous conversations queue. The Quests tab replaces floating tracked-quest overlays; Auto Pin is inside Quests. Secondary page navigation stays directly above the main bottom bar. Scrollbars use the outer gutter independently of content width, preserving equal left and right content padding.

## Information and actions

| Mechanic | Portrait presentation |
| --- | --- |
| Travel | Adventure page with available map choices. |
| Resources and equipment | Hub > Inventory; Resources and Equipment pages, including the character preview, visibility controls and equipment bonuses. |
| Skills | Shared skill selection with separate Progress and Bonuses pages. |
| Echoes and buffs | Hub > Buffs; assignment slots and scrollable effects, duration and cooldown. |
| Quests | Tracked, untracked and completed groups, requirements, rewards, Auto Pin and optional resource inventory. |
| Forge | Craft and Materials pages; new-item comparison and Equip stay with crafting. Rarity odds expand on demand. The first-forge tutorial selects the page containing its next action. Statistics tutorials target the visible mobile section and area dropdowns. |
| Fields | Crops, Orchard and Planting pages. Plot selection opens its details; build costs and seed actions retain the existing rules. |
| Cauldron | Ingredients, Tasting and Collection pages. |
| Statistics | Section and sort selectors; graph metric/map selectors leave room for the chart. Lists and graph details scroll. |
| Library | Article-first reader with a separate topic picker, Overview and Reference. Patch notes remain in the topic list. |
| Settings | Preferences and Save files. |

## Implementation

`ToolkitResponsiveLayout` owns shared dimensions, panel scaling and the dock background. Geometry uses `UnityEngine.Device.Screen` and mobile checks use `UnityEngine.Device.Application`, so Device Simulator supplies the same values as a phone. Panels use explicit pixel scale from device width in portrait and device height in landscape; the editor Game View size cannot change their scale. `AspectRatioBox` applies the world viewport. `ToolkitWindowLayout` supplies panel bounds. `ToolkitWorldScreen` clips projected labels and health bars to the map.

`ToolkitPortraitPages` composes existing live controls into dedicated pages and restores their desktop hierarchy on rotation. `Portrait.uss` owns portrait presentation separately from `Gameplay.uss`. Portrait uses a 360-unit reference width; landscape retains its 432-unit reference height. The profile adds no save fields and changes no unlock requirements.

Physical-device checks for notches, the keyboard, touch scrolling and rotation are still needed before a mobile release. Preserve input rejection outside the world viewport and the loading screen's full-screen opacity when refining the layout.



## Simulator review

Use the included Apple iPhone 15 Pro profile (1179 x 2556) under `Assets/Editor/DeviceSimulator/iPhone15Pro`, including its Dynamic Island overlay, 102-pixel bottom inset and 177-pixel top inset. The upstream profile and license are documented alongside the asset. Match the companion Game View resolution for composite screenshots, then focus Device Simulator. Verify both simulator dimensions and safe area before capturing. Do not infer a device check from a tall Game View alone. Review town, adventure HUD, conversations, each menu, touch drags, and orientation changes.
