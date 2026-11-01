# 1.5.9 store outcome

Verified 1 November 2026. Steam and Android observations were recorded around 06:23 UTC; the final iOS submission was verified at 07:08 UTC. These are observed release states, not predictions of review approval.

- Steam: **live on default**, build **26356626**, containing Windows, macOS and Linux. Beta remains build 0. This is the corrected Mac signature package approved by the user. The earlier build 26356608 was briefly live before replacement; Windows and Linux content did not change.
- Google Play: **live in production**, version **1.5.9 (187)**, 100% rollout in 171 countries/regions. The production dashboard shows Active and publishing has no pending changes. Matching native symbols and ReTrace are attached.
- App Store: **Waiting for Review**, public **1.5.9 (193)**. Submission `d93ba0e6-8590-4571-9df0-914d38f40d62` was submitted on 1 November at 18:08 Sydney time (07:08 UTC). The final submission detail identifies build 193 and Waiting for Review. Automatic release after approval and immediate updates remain selected; Game Center remains enabled. This is a public App Store submission, not TestFlight-only distribution.

The [Steam patch notes](https://store.steampowered.com/news/app/2940000/view/712290761518876828) are published and publicly readable, linked to 26356626, with all eight languages saved. Steam Library visibility is still pending its moderation queue. The 15 common bullets omit the two iOS-only bullets; all eight iOS draft translations retain those two. All eight iOS notes and descriptions were read back against the approved content, and both Watch screenshots are attached.

Gameplay source is `b19047f8b52011c1ff6e6eea2fdbdb09c5d6aab8`. iOS 193 source is `382914e64797a0a7ebbbf4b0f75998578f6a3cbd`. Native packaging and provenance are recorded in the [Mac receipt](mac-distribution-receipt.md), [192 receipt](ios192-distribution-receipt.md), and [193 receipt](ios193-distribution-receipt.md). Their Processing states are historical upload observations, superseded by the final submission verification here.

Builds 191 and 192 failed review preparation with `STATE_ERROR.BUILD_INDICATES_GAME_CENTER_DISABLED`, despite the phone key being true in signed and processed metadata. The approved 186 build contained no Watch app. Adding the supported Game Center capability, explicit entitlement and GameKit link to the embedded Watch target resolved the blocker in 193. The phone capability was preserved and the widget unchanged. Apple processed metadata now lists Game Center true on both phone and Watch; Add for Review and the final Submit for Review both succeeded. All gameplay data and compiled function bodies match 192; the native relink differences are recorded in the 193 receipt.

The unchanged gameplay passed 37 EditMode and 15 release PlayMode checks, plus the final Library check in both layouts; the Mac recorded nine lifecycle/UI PlayMode checks. The native-only 192 retry did not rerun gameplay tests or establish device gameplay QA.

Screenshots, localized metadata readback, the exact 191/192 backend errors, 193 processed entitlements, final Waiting for Review proof and release-state JSON are retained in ignored `Artifacts/Release15920261101`. The user declined sending the [Apple support draft](apple-support-draft.md); it remains unsent and the submission blocker is resolved. No further upload is needed.
