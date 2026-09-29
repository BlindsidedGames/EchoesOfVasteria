# Forge investigation — 28 September 2026

Investigation only: no permanent Forge code changes.

## References
- Original uGUI inventory: ../baseline-2026-09-27/captures/23-forge-inventory.png
- Original 1280 Forge: ../baseline-2026-09-27/captures/101-viewport-1280-OpenForge.png
- Current: forge-default.png and forge-inventory.png (1280 x 720).

Current captures use an existing disposable developed save (Ivan level 480), paused simulation, 30 FPS cap, VSync disabled. The original baseline has different equipment/progression values; compare presentation, not those values. No crafting or conversions performed. Temporary local save, Steam, UGS, feedback and identity guards restored byte-for-byte; see restoration.json. Play stopped afterward.

## Findings
Inventory opens and renders in this sequence (open Forge, click Inventory). Its viewport is 153 reference units wide. Six 26-unit cells with five 1-unit gaps need 161 units; ToolkitGrid.FitWidth therefore correctly falls back to five columns. This changes original row grouping and increases scrolling.
A dispatched wheel event advances scroll from 0 to 108. This verifies the scroll handler, not physical mouse input. Both inventory and Forge panels have sorting order 100: opening-order-dependent drawing/picking remains a hypothesis, not a confirmed root cause of the user's reported breakage. Input raycast sees both panels. Further reproduction should open resource inventory before Forge, and test repeated transitions, resizing and actual pointer routing.
Artwork uses ScaleToFit, preventing distortion, but this does not establish consistent native pixel scale. Resource icons still fit an 18 x 17 rectangle; Forge slots and conversion icons use separate fixed sizes. This differs from the accepted native-PPU masked treatment.
The captured window has undersized comparisons and controls, long unstructured sidebar statistics, and substantial unused vertical space. Core buttons show unlabeled upper/lower amounts. Gear comparison is constructed as multiline strings rather than aligned stat rows.

## Proposed first pass
1. Integrate inventory into Forge's sidebar visual tree (or explicitly define companion panel ordering if retaining separate documents). Preserve resource highlighting and independent scrolling. Budget six columns at desktop scale, responsive fewer columns at narrow widths; test transitions and actual pointer picking.
2. Retain original workflow: equipment/core selection on left, crafting/odds in centre, pending/equipped comparison next to it, Inventory/Info companion column on right, conversions below.
3. Apply shared icon frames, masks and native PPU sizing. Use accepted amber selection and purple active state, and standard checkbox treatment.
4. Replace comparison text blobs with aligned icon/stat rows, pending and equipped values, and meaningful improvement colour. Explain empty pending state without large dead space.
5. Give core quantity and craft capacity concise shared labels; show selected recipe costs and maximum clearly. Keep odds details accessible by click/touch without shifting controls on hover.
6. Standardize conversion rows with readable amounts, formula icons, quantity input and Smelt. Preserve hold-to-repeat behaviour and tier-dependent conversion availability.
7. Keep equipment totals and lifetime Forge history secondary; use readable stat rows and independently scrollable information. No additional nested decorative panels.

No gameplay or automation changes proposed. Validate desktop, narrow landscape, text scaling, early/locked cores, developed equipment, active automation and empty/pending results during implementation.
