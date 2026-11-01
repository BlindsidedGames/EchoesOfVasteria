# App Store Connect submission blocked by conflicting Game Center metadata

Draft only. Not sent to Apple.

App: Echoes of Vatheria, Apple ID 6749265146
Bundle ID: `com.blindsidedgames.echoesofvasteria`
Version: 1.5.9
Affected builds: 191 and 192

App Store Connect refuses Add for Review with:

`STATE_ERROR.BUILD_INDICATES_GAME_CENTER_DISABLED`

> Your appStoreVersion has gameCenterConfigurations relationship but build is missing com.apple.developer.game-center key.

However, App Store Connect's own Build Metadata lists `com.apple.developer.game-center: true` on the main iPhone executable for both builds. We also independently decoded the XML and DER entitlements in the actual transmitted distribution IPAs: both contain the key set to true, and the embedded provisioning profile permits it. The Live Activity and Watch targets do not use Game Center and omit this entitlement.

Build 192 is Validated and Ready to Submit. It is selected and saved in the version draft with Game Center enabled. Its build ID is `835b9179-0bfa-43be-8a79-e7b5981f4057`; Apple accepted the upload on 1 November 2026 at 06:34:05 UTC. Build 191 is `00b2edae-9670-4aae-9abd-057aad68f5c1`.

The ordinary Add for Review flow creates a review submission, then adding the version returns HTTP 409. App Store Connect deletes the resulting empty submission. The affected appStoreVersion is `7ac791ae-f7bc-4b4a-8707-1d821b24deb9`. The latest error ID is `5b373d33-f41b-4a37-a5cb-ffde06fc5baa`, associated error `a0adcca6-60d6-4731-901b-a30707b8acce`.

Reloading and reattaching build 191 did not help. Refreshing the version's Game Center setting, restoring it to enabled, also did not help. One fresh archive/export and upload as 192 produced the same failure. Build 192 changes bundle versions only; gameplay and signed capabilities remain equivalent to 191.

Please investigate why the review validator classifies these builds as Game Center disabled when their processed metadata contains the required entitlement, and correct the classification so version 1.5.9 can be submitted with Game Center enabled.

Evidence available: screenshots of build metadata and the submission error, the exact 409 response, and signed IPA entitlement/profile verification receipts. We have not removed Game Center from the release or added it to unrelated targets.
