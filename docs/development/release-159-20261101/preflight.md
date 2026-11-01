# 1.5.9 release preflight

The user approved the complete 17-bullet English notes for the public 1.5.9 release on 1 November 2026. The same content is installed in all eight Library languages. iPhone Live Activity and Apple Watch bullets remain iOS-only; older releases retain muted styling. Google Play uses a shorter summary of the same approved content, with full notes in the Library.

Store preflight confirmed Google Play production 1.5.8 (186), App Store 1.5.8 (186), latest Apple upload 1.5.9 (190), and Steam default build 26293025. New mobile identifiers are iOS 191 and Android 187, retaining marketing version 1.5.9 and the existing package identifiers. These observations are preflight evidence, not evidence that 1.5.9 has been submitted or released.

The loading-screen fix extends the existing cover across the interval before progress is applied and the deferred run restoration begins. It does not change saves, combat, map generation, or progress initialization order. Normal town starts and rejected continuations release the cover. The new UI regression failed on the old implementation and passed after the repair in portrait and landscape.

The final Windows PlayMode release pass completed 15 of 15 tests, covering continuation and one-time settlement, Hardcore death and safe reaps, creation and read-only memorial navigation, cauldron protection and progression, Forge limits, portrait dragging, and platform-specific Library notes with muted history. During the broader pass the isolated continuation fixture exposed a combat-retreat race: its GameManager.Update is intentionally disabled, so queued combat retreats cannot complete. The test now exits incidental combat through the real combat controller before checking the immediate-return payout; production retreat behavior is unchanged.

The experimental background-progress setting is compiled for desktop, Android and iOS and still defaults off. The unsupported continued-processing experiment is not enabled. Build normal content with lossless LZ4HC; keep Steam beta disabled and preserve existing saves, store identities and unrelated primary-checkout edits.

Final source review caught a prototype isolation leak: opting into background progress also suppressed achievement authentication and account services. Removed that coupling from the shared service policy while retaining explicit capture/diagnostic isolation. The new policy regression failed before the fix. All 37 estimator/service-policy EditMode tests and the same 15 release PlayMode tests passed on the correction. The initial candidate builds were stopped in their isolated checkouts before upload; mobile build numbers remain unused.

Raw test results, store preflight responses, source manifests, build support manifests, package verification and submission receipts are retained under the ignored Artifacts/Release15920261101 directory.
