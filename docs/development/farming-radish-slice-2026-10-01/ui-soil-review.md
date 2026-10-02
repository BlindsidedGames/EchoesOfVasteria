# Farm interface and soil review

This change stays in the isolated development farm. Original Main, crop/seed balance, adventure eligibility, quests and existing packet art are outside this change.

## Soil diagnosis and correction

The two development beds contain 18 SpriteRenderers with the correct catalogue neighbor masks. The actual source pixels are asymmetric: the top-left corner, DryTanBorder variant01, has a transparent rounded outside boundary; the right and lower named corner variants03/15/17 have opaque tan outside corners. The right side10 is opaque, unlike left side08. The isolated circular tile00 has four rounded boundaries but does not join a 3×3 filled patch.

The candidate reuses existing variant01 mirrored horizontally/vertically for the other three corners, existing left edge08 mirrored for the right edge, and existing top02 mirrored for the bottom. Ten sprite/flip bindings change across the two beds. Center09, positions, size, materials, fences, paths, crop sprites and art imports remain exact. No replacement art was generated. This creates consistent mirrored texture; bespoke asymmetric variation would require a separately reviewed derived-art task.

The original scene can be reproduced exactly by reversing those ten bindings and removing the single new FarmScreen crop-icon reference. Its SHA-256 is `33f38b4fa3f7cc47d8c8475a3c9f6907d730883bd6c59f198ab0ee71ed6b8551`. Original production Main remains `8f912645ea9216c7f25a833da730c44524aaea0dab7ba4cd76a9c0e1ac0d9d54`.

## Native interface change

The native Forge/resource inventory and reviewed Quest panels supply the typography, palette, controls, thin frame treatment and progress bars. Farm now separates a persistent seed inventory from the bed workspace. A large packet icon and owned count remain visible during bed actions; undiscovered packs use the existing shared unknown image and hide crop identity. Depleted discovered packs retain their name and show zero.

Each bed has its own framed title, status, progress and existing mature Radish image while planted. Empty prepared beds expose **Plant Radish** with the one-pack cost or a concrete unavailable reason. Growing/ready beds hide the irrelevant planting control. **View bed** retains the existing TownCameraPan focus route. **Harvest all ready** clearly describes the existing atomic multi-bed command; it never implies a per-bed harvest command. The heading reports available, growing or ready bed counts. Narrow workspaces stack seed inventory above the scrollable bed list. Farm reserves no external inventory column because its seed inventory is embedded; this restores the full panel width.

All commands still go through IFarmPresentationSource and recheck eligibility in the service. No balance or save behavior changes here.

## Verified result and limit

Standalone compilation passes with zero diagnostics. The final combined development Mac build completed in27.91seconds with zero errors and two existing build warnings. Its first actual Metal Player phase passes with zero recorded errors or warnings. The disposable harness uses5-second growth and100% pack chance to exercise the UI; normal scene values remain30minutes and10%. Native keyboard planting debits one pack per bed; Harvest all ready pays exactly20Radish for two beds and rejects repeat payout. The earlier Steam isolation error was corrected by the validation owner; this final Player phase did not attempt Steam initialization.

The fresh1280×720 captures were inspected independently. Both beds have coherent rounded corners and matching sides; crops, fences and path approaches remain clear. Unknown pack identity remains hidden, the owned count stays visible after planting, and growing/ready actions and progress are readable. [Actual soil comparison](images/soil-actual-player-before-after.png) and [actual native interface comparison](images/native-farm-actual-player-before-after.png) use fresh final Player pixels. Original images and earlier comparison versions remain retained.

The Farm-only narrow fallback is accepted for **local scrolling stability**. Its hierarchy and style changes run once per width transition from Update; bed height comes from measured rows. The owned-empty scrolled capture shows separate Plant Radish and View bed controls, and the ready scrolled capture contains actual bed/control content. There are zero layout-stabilization messages or content-loss errors in the final phase. Preparation and the workspace share one outer vertical scroll; empty controls can wrap. The failed callback-driven version and its images remain retained in `player-v2`.

**The forced portrait stress test is unusable; portrait is disabled in the authored mobile configuration.** The actual720×1280 images show a tiny central strip: the existing shared safe-area helper enforces a minimum16:9 region, and world view is letterboxed. Navigation clips too. The scroll fix avoids Farm's own overlap/content loss; it does not create a usable mobile layout or make portrait a supported game orientation. The final [portrait top](</Users/matthewrushworth/Projects/Echoes Farming Implementation Evidence/2026-10-01/ui-soil-review/player-v3/seed-inventory-native-farm-portrait.png>) and [scrolled empty controls](</Users/matthewrushworth/Projects/Echoes Farming Implementation Evidence/2026-10-01/ui-soil-review/player-v3/seed-inventory-native-farm-portrait-bottom.png>) make this limit visible. Supported landscape device/AOT validation remains separate work; no global geometry was changed here.

The retained before source, full before scene, focused diffs, alpha contact sheet, change records and standalone compilation result are in `../Echoes Farming Implementation Evidence/2026-10-01/ui-soil-review` beside the project. Original delivered images remain in [images](images/).
