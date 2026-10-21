# Platform delivery

Read this when selecting export specifications. Verify the current official documentation for the actual asset type before final delivery; platform rules can change. Record the checked source and date in the delivery brief. Do not turn the review sizes below into claimed platform requirements.

## Steam achievements

Official source: [Steamworks achievement guide](https://partner.steamgames.com/doc/features/achievements/ach_guide), sections Icons and Icon Files.

Checked 21 October 2026: Valve recommends square 256 x 256 PNG achievement icons; 64 x 64 is accepted, and larger images are reduced to 256 x 256. JPG is also accepted. Colour achieved and grayscale unachieved artwork are recommended, and Steam can generate grayscale versions. Custom unachieved artwork is supported.

For a bulk icon-only upload, current naming uses `<api_name>_ACHIEVED.png` and `<api_name>_UNACHIEVED.png`. Preserve API names exactly. Do not change names or thresholds to make a visual naming scheme tidier. Recheck these rules before preparing an upload.

Review the final export at 256 and 64 pixels, with 32 pixels as an additional readability test when useful. For pixel art, a suitable native grid that divides the export dimensions allows integer enlargement. Use a consistent background policy; do not assume iOS launcher transparency rules apply here.

## Mobile achievement icons

Keep these separate from app launcher requirements. For Apple Game Center, consult [Game Center design guidance](https://developer.apple.com/design/human-interface-guidelines/game-center) and [manage achievements](https://developer.apple.com/help/app-store-connect/configure-game-center/manage-achievements). The approved project export is an opaque 1024 × 1024 sRGB PNG, with artwork safe under a circular mask. Replace the image in the existing achievement localization; preserve the achievement ID and all text. A replacement to a live achievement can require a separate App Review submission. Uploaded, added for review, submitted, approved, and live are distinct states.

For Google Play Games, consult the [achievement icon guidelines](https://developer.android.com/games/pgs/integrate-achievements#icon_guidelines). The approved project export is a 512 × 512 PNG, at most 1 MB, safe under a circular mask. Replace the existing icon, preserve all other fields, and inspect Play Games' separate publishing page. Do not publish unrelated existing drafts with an icon-only update.

## App launchers and store icons

First distinguish the store listing icon from the installed launcher icon. Check [Apple's app icon guidance](https://developer.apple.com/design/human-interface-guidelines/app-icons), [Android adaptive icon guidance](https://developer.android.com/develop/ui/views/launch/icon_design_adaptive), and [Google Play icon specifications](https://developer.android.com/distribute/google-play/resources/icon-design-specifications) as applicable.

Verify dimensions, alpha, masks, safe regions, and required layers for each target. A single flattened icon may not supply the adaptive layers or variants a launcher requires. Avoid importing another platform's corner treatment or assuming that all exported sizes need the same composition.

## In-game and web interfaces

The actual renderer and layout determine the asset contract. Inspect sprite slicing, filtering, atlas padding, theme backgrounds, masks, state tinting, and displayed size. For web deliverables, confirm whether SVG, raster images, favicons, or manifest icons are needed instead of generating an exhaustive size list by default. For Unity, retain source assets and metadata needed to preserve references, and test affected layouts in every supported orientation.
