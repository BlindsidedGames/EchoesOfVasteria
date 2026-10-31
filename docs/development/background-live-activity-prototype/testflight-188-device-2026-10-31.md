# Internal iPhone and Watch TestFlight build 188

Apple accepted **1.5.9 (188)** from source `ae4e2a6c69acf3998e8e0143f82b8910c620bc13` at **2026-10-31 10:46:36 UTC**. The structured upload record is `05bfe8b3-882f-474a-a1e5-aa4ae92f570a`, initially **Processing**, with no errors or warnings in that record. Upload acceptance does not establish tester availability. The coordinating thread owns the subsequent App Store Connect testing-notes, existing Internal group and Testing-state verification.

This updates the previously uploaded build 187 with the run-resume fixes, native background Refresh, current distance and elapsed time, four icon metrics, saved character/equipment appearance, and measured brief portrait bob. Background progress remains experimental and **off by default**. Existing opt-in preferences remain user choices. The approved internal testing instructions are recorded in the accompanying JSON; public Library/store release notes were preserved.

## Distribution evidence

- Fresh full **DeviceSDK** export using Unity 6000.6.0f1, IL2CPP arm64, normal full content, Demo/Beta false and no development build, profiler or synthetic QA seed. The only experimental build define is `EOV_BACKGROUND_PROTOTYPE`; generated IL2CPP confirms the PlayerPrefs fallback is zero. Temporary Editor QA helpers are absent from player metadata.
- Native Release archive and cloud-managed Apple Distribution export passed. Host, Live Activity widget and Watch app all report **1.5.9 (188)** with their existing production bundle identifiers. Host/widget are arm64; Watch includes **arm64_32 and arm64**. Distribution signing uses the existing team and `get-task-allow=false` for all three bundles.
- The current C bridges, main-executable Swift activity owner, host/widget Refresh intent metadata, all **179** named idle portrait assets and the Watch app icon were verified in the actual archive/package. The existing non-exempt-encryption declaration is false, matching build 187.
- The two protected opt-out and read-only companion lifecycle tests passed **2/2** with the actual release prototype define enabled and no force-enable launch argument. Existing simulator visual/motion evidence remains separately recorded; this upload is not physical-device gameplay acceptance.
- Apple validation passed before upload. The actual uploaded IPA was retained: **250,837,522 bytes**, SHA-256 `92cdd5a43b1947c0809f64644e99ffcda6e19730e7b8014eca817fd52c7ce7bc`. Its native UUID/TEXT sections, including both Watch slices, and every serialized Unity Data hash match the reviewed prepared IPA. The JSON records both package hashes, which differ because packaging/signing may vary.
- Xcode reported the same nonblocking missing vendor dSYM warning for `UnityRuntime.framework` seen in build 187. The game's matching UnityFramework dSYM is present; Apple accepted the upload.

## Preservation and scope

Only the iOS build-number field advances to **188** in source configuration. Marketing version remains **1.5.9**; Android stays **186**. Known local importer/assembly/build adaptations remain outside this commit. The primary checkout's original HEAD and user diff are unchanged. All recorded progression files match; only the generated test report and Editor Analytics cache differ among the 228 original player-directory files.

This delivery is **internal TestFlight only** for the existing app `6749265146`. No public App Store submission, external tester change, main-branch merge, Android deployment or Steam deployment occurred. Private signing/distribution logs, archives, IPAs and credentials remain outside Git.
