# Steam shop configuration

Echoes uses Steam Inventory Service, following the retained-item purchase flow
in Idle Dyson Swarm. It does not need a separate payment server or a DLC store
page. The item definitions are in `SteamAssets/inventory-items.json`.

| ItemDef | Game product | Australian base price | Ownership |
| --- | --- | --- | --- |
| 1001 | eov.gathering.permanent | A$4.99 | Permanent; purchase limit 1 |
| 1002 | eov.supporter.small | A$2.99 | Repeatable retained stack |
| 1003 | eov.supporter.medium | A$7.99 | Repeatable retained stack |
| 1004 | eov.supporter.large | A$14.99 | Repeatable retained stack |

Items cannot be traded or marketed. Supporter items remain in the player's
inventory and provide no additional gameplay benefit. Steam converts the AUD
base prices for other currencies; the game requests the player's actual price.

## Delivery and restoration

`ShopPurchases` selects `SteamInventoryPurchases` for standalone Steam players.
Mobile builds continue to use Unity IAP; the Editor continues to use its test
store. The Steam provider checks ownership before checkout, opens Steam Wallet
checkout with `StartPurchase`, and waits for an increased inventory quantity.
Starting checkout alone never grants an item. Cancellation, timeout and failed
inventory requests do not grant ownership. A late delivery can be recovered by
Restore purchases, reopening the shop, or relaunching.

Only a successful, complete inventory result belonging to the current Steam user
can reconcile ownership. Delivery callbacks are deltas, so they trigger a full
inventory read. Retained supporter quantities replace the cached count instead
of incrementing it on every restore. Refunds remove inventory ownership on the
next successful reconciliation. Failed checks preserve the previous cache.

Ownership caches use `shop-steam-<SteamID>.json` under the game's persistent data
directory. They are separate from mobile/legacy installation caches and player
save slots. Steam original-owner checks still use the base game's acquisition
date, exclude family sharing and free weekends, and run separately from paid
inventory ownership. That date does not distinguish complimentary licenses from
paid purchases; this change does not claim otherwise.

## Verified on 23 October 2026

- Steamworks accepted all four item definitions and published the Inventory
  Service setting. The existing economy endpoint was already correct.
- A read-only native Steam API probe returned AUD 499/299/799/1499 hundredths,
  verified the inventory owner, and successfully read the empty test inventory.
- Unity compiled the Steam provider and all 12 focused EditMode shop tests passed.
- The initial checks requested no purchase. Subsequent sandbox purchases are
  documented below; no game build was uploaded or released.

## No-charge native validation

The private Item Store preview supplies sandbox checkout even though the base
game has already released. Its payment method explicitly displays **My Steam
Wallet (sandbox)**. This was verified at checkout before each authorization;
the ordinary page also contains generic payment wording. Never infer sandbox
status just from the Steam beta branch or publisher membership.

The sandbox test completed a permanent-bonus order, an order containing all
three supporter tiers, and a second Small Thanks order. Native inventory reads
verified delivery and the repeated supporter stack. Cancelling a separate
supporter checkout left inventory unchanged. A second permanent checkout failed
with Steam's generic authorization error; the error does not explicitly identify
the purchase-limit rule, so do not report its cause as verified.

`SteamInventoryNativeTests` exercises the actual Unity component against that
Steam inventory: native connection/prices, correcting a stale temporary cache,
repeated restores, and rebuilding ownership in a fresh cache. It requires an
explicit test filter and `EOV_STEAM_NATIVE_TEST=1`, with the expected fixture state
in `EOV_STEAM_EXPECT_PERMANENT` and `EOV_STEAM_EXPECT_SUPPORTERS`. It never starts
checkout or modifies Steam items. User gameplay saves and installation ownership
caches are not used. The test restores the previous Editor integration setting.

All three native Unity runs passed (one test each, none skipped): permanent-only,
four supporter purchases, and empty inventory after cleanup. The last run also
verified removal of stale cached ownership. The broader shop/save-migration
EditMode run passed all 21 tests. All sandbox item instances were removed after
testing, and a fresh native process confirmed an empty inventory. Total actual
spend was A$0.00.

Evidence for this run is under `Artifacts/SteamNoCharge20261023/`. The web sandbox
tests Steam's actual authorization/delivery and the native component tests the
game's restoration path. Clicking the purchase button in a Steam-launched
player's overlay remains a separate UI check; these tests do not claim to cover
that interaction. Keep the Item Store private until its public launch is intended.

The native purchase overlay was subsequently tested in an isolated Windows
player launched through Steam, using the production `ShopPurchases.Buy` and
`SteamInventoryPurchases` code. Steam reported the overlay enabled and activated.
Matt observed a real-payment/add-funds checkout and cancelled it. The game
received `MicroTxnAuthorizationResponse_t` with authorization zero, cleared Busy,
reported cancellation, and retained the supporter count at zero. No payment was
authorized. This verifies the native open/cancel path; successful native-overlay
payment and delivery remain untested because this checkout was not sandboxed.
The temporary player loaded no gameplay scene or save, and the original Steam
installation was restored with its executable hash verified. Evidence is under
`Artifacts/SteamOverlay20261023/`.

The abandoned DLC setup created app 5494210 and packages 1895478–1895480. It is
unreleased, has no configured sale price, was not added to the old paid package,
and is not used by the game. Do not release or configure it for this shop.

Evidence is retained in the ignored `Artifacts/SteamInventory20261023/` folder.

## Android original purchasers

Android now grants the permanent boost to installations whose native
`PackageInfo.firstInstallTime` is positive and strictly before
`2026-10-17T00:00:00+11:00` (Sydney midnight). This is the approved existing-install
policy, not a receipt/payment verification. It can include old testers and miss
paid owners who reinstalled after the cutoff.

The check runs directly through Unity's Android JNI bridge without a server or
additional permissions. An eligible grant is atomically persisted as
`OriginalOwner` in the existing installation ownership ledger, separate from
gameplay saves. Later dates, missing timestamps and native-query failures never
revoke an Android grant. Normal updates retain the same ledger and Android's
first-install timestamp. Uninstall/data clearing and device transfer do not have
the same persistence guarantee; no receipt-claim service has been added.

References: [Steam Inventory API](https://partner.steamgames.com/doc/api/ISteamInventory),
[Steam Inventory schema](https://partner.steamgames.com/doc/features/inventory/schema),
[Google licensing response fields](https://developer.android.com/google/play/licensing/licensing-reference).
