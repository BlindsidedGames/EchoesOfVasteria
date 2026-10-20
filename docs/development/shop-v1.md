# Shop v1

The Shop is a Hub submenu. It remains hidden until the final map-statistics tutorial dialogue completes (stage 9), or the saved opening-tutorial completion marker exists. It uses the common landscape/portrait window layout and closes through Hub like the other submenus.

## Products

| ID | Type | Australia base price | Benefit |
| --- | --- | --- | --- |
| `eov.gathering.permanent` | Non-consumable | A$4.99 | Permanent +50% adventure resources, with an on/off toggle |
| `eov.supporter.small` | Consumable | A$2.99 | Optional support only |
| `eov.supporter.medium` | Consumable | A$7.99 | Optional support only |
| `eov.supporter.large` | Consumable | A$14.99 | Optional support only |

Runtime prices come from the platform store. The editor fake store displays the configured AUD reference prices and clearly identifies itself as a test store. Steam purchases require a future platform-specific listing/integration; no purchase buttons are enabled there. Original paid Steam owners are supported separately.

App Store Connect draft records: permanent `6824927875`, small `6824929068`, medium `6824929555`, large `6824930018`. Each has an English display name/description, Australian base price with automatic regional prices, all-country availability and a shop review screenshot. They have not been submitted for review or released.

The gathering multiplier applies to ordinary task and enemy resources, including Echoes, after existing resource modifiers. It does not affect XP, seed drops, rarity, farming, forge or cauldron output. Permanent and temporary bonuses do not stack.

## Promotion catalogue

Uses the IDS v1 JSON contract and endpoint `https://www.blindsidedgames.com/promotions/v1/catalog.json`. The bundled catalogue and its original WebP banners are included for first-launch/offline use. Echoes excludes its own `echoes` entry. Game links remain separate from claiming: the player can immediately claim 30 minutes without visiting or installing anything. Claims bank up to two hours. The timer runs only during unpaused adventures and stops in town, map loading and application suspension.

The catalogue refreshes on opening Shop, at most every six hours (at least 60 seconds between attempts). Requests have a ten-second timeout, no redirects, content-type checks and bounded downloads: 512 KiB JSON, 250 KiB per banner. Links and banner paths are validated against the IDS allowlist. Invalid/offline responses keep the last valid catalogue. Corrupt image cache entries fall back to bundled art. Encoded banner cache is bounded to 20 MiB and stale entries are pruned. Only the currently displayed banner is decoded; its texture is destroyed when closed or replaced.

## Ownership and saves

Temporary time belongs to the gameplay save. Permanent ownership, the toggle and fulfilled supporter transaction IDs live in a separate installation ledger, written atomically before acknowledging a store order. Replayed supporter transactions are idempotent. Imported saves cannot grant purchased ownership. Restore Purchases checks the platform non-consumable entitlement; refunds/revocations remove the purchased grant. Original-owner eligibility is a separate grant.

Original-owner detection uses verified production StoreKit `AppTransaction.originalPurchaseDate` on iOS 16+, or Steam's earliest purchase timestamp excluding free-weekend/family-sharing access. The configured cutoff assumes the beginning of **17 October 2026 in Australia/Sydney**, based on the supplied transition date; confirm the exact transition time before release. Sandbox dates do not grant original ownership. Android historical paid ownership is not implemented; it requires a reliable platform entitlement source.

## Validation and release

Focused EditMode tests cover intro gating, claim limits, catalogue destinations, durable ownership and purchase replay. The isolated PlayMode flow covers portrait and landscape, immediate claims, changing saves, and the permanent toggle without modifying the user's saves. Captures are written to `%TEMP%/eov-shop-review`.

Before shipping: build on Xcode to validate the Swift bridge and StoreKit linking; test native purchase, cancellation, deferred approval, restore and original paid ownership with suitable Apple accounts. Editor fake-store results are not native billing verification. The first Apple non-consumable must be attached to a new app version for review. Store product drafts are not a production release.
