# 1.5.9 store outcome

Verified 1 November 2026, approximately 06:43 UTC. These are observed release states, not predictions of review approval.

- Steam: **live on default**, build **26356626**, containing Windows, macOS and Linux. Beta remains build 0. This is the corrected Mac signature package approved by the user. The earlier build 26356608 was briefly live before replacement; Windows and Linux content did not change.
- Google Play: **live in production**, version **1.5.9 (187)**, 100% rollout in 171 countries/regions. The production dashboard shows Active and publishing has no pending changes. Matching native symbols and ReTrace are attached.
- App Store: **not submitted**. Public-eligible **1.5.9 (192)** is processed, Validated, Ready to Submit, selected and saved. Add for Review returns the same Game Center classification error as 191. Apple's own processed main-app entitlements show the key enabled. Automatic release after approval and immediate updates remain selected; Game Center remains enabled. No review submission containing the version was created.

The [Steam patch notes](https://store.steampowered.com/news/app/2940000/view/712290761518876828) are published and publicly readable, linked to 26356626, with all eight languages saved. Steam Library visibility is still pending its moderation queue. The 15 common bullets omit the two iOS-only bullets; all eight iOS draft translations retain those two. All eight iOS notes and descriptions were read back against the approved content, and both Watch screenshots are attached.

Gameplay source is `b19047f8b52011c1ff6e6eea2fdbdb09c5d6aab8`. iOS 192 source is `3931ab1e68d8e371e7c98f0b3e854708558e59aa`, whose only production change is the iOS bundle build number. Native packaging and provenance are recorded in the [Mac receipt](mac-distribution-receipt.md) and [192 receipt](ios192-distribution-receipt.md). The latter's Processing state is its historical upload observation; the later processed state and failed review attempt are recorded here.

The unchanged gameplay passed 37 EditMode and 15 release PlayMode checks, plus the final Library check in both layouts; the Mac recorded nine lifecycle/UI PlayMode checks. The native-only 192 retry did not rerun gameplay tests or establish device gameplay QA.

Screenshots, localized metadata readback, the exact 191/192 backend errors and release-state JSON are retained in ignored `Artifacts/Release15920261101`. The [Apple support report](apple-support-draft.md) is prepared but unsent. No further upload or capability change is planned without new evidence.
