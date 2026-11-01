# Mac distribution evidence for Echoes 1.5.9

The frozen source is `b19047f8b52011c1ff6e6eea2fdbdb09c5d6aab8`. This receipt covers the Mac release lane; the Windows coordinator owns Google Play, Steam publication, and final App Store browser submission.

## Steam

The complete, corrected build is **26356626**. No SetLive action was performed on the Mac.

| Depot | Platform | Manifest |
| --- | --- | --- |
| 2940001 | Windows IL2CPP | 3314075571749984887 |
| 2940002 | Mac universal Mono | 5742832878672576254 |
| 2940003 | Linux x86-64 Mono | 8265499075316969021 |

The Windows download matches the coordinator's independent canonical tree digest: `ebd6509910946aa0e5a81243652c997a4d5691f81e1a4236d966ea33ab4bf9e5`, 50 files, 246,666,345 bytes. Steam reused that exact manifest. Every Mac native binary contains both x86_64 and arm64; Linux has an executable ELF64 x86-64 player and the Mono runtime. Both builds completed with zero errors, normal Loading/Main content, LZ4HC, and development, profiling, demo, and beta disabled.

Initial staged build 26356608 is superseded. Final strict signing verification caught the branding postprocess invalidating Unity's ad hoc signature. The shipped Mac app was re-signed ad hoc with identifier, entitlements, flags, and runtime preserved, then passed `codesign --verify --deep --strict`. The repair changed 14 signature-related files. The other 294 product files, all 13 native UUIDs and code sections, and every Unity data and managed assembly byte are unchanged. `Info.plist` is unchanged, SHA-256 `161ef1da887c2c3ec7e6398076db639930d756d243c1b9926bd1b1ae8d74e8d0`. The private receipt retains exact before/after file hashes.

A separate local tool issue affected the first Mac build: the official Unity 6000.6.0f1 `llvm-lipo` file had mode 0644. The installed editor was preserved. An APFS clone used the identical tool with owner execute permission restored to 0744, then both desktop builds passed. No product source or Burst setting changed. Future Mac packaging should check this tool and apply branding before final signing, with strict signature verification before staging.

## iOS 1.5.9 (191)

The full DeviceSDK IL2CPP arm64 archive and App Store eligible IPA passed package verification. The host and Live Activity are arm64; Watch contains arm64 and arm64_32. All four native binaries have matching dSYMs. Signed archive Unity data matches every file from the fresh final-source export. Export/upload options explicitly set `testFlightInternalTestingOnly:false` and preserve version/build identities.

Apple accepted the public upload at **2026-11-01T05:39:41+00:00**, upload ID `00b2edae-9670-4aae-9abd-057aad68f5c1`. The recorded state is **Processing**, with structured errors/warnings/infos all empty. The actual transmitted IPA is 251,682,357 bytes, SHA-256 `3dffcc197b4542fb234ab90b5c5500cd645f8fa38308ac97cc18b84160ba92d5`; native UUID/code sections and all serialized Unity data match the prepared package. The prepared IPA is 251,682,359 bytes, SHA-256 `1c91e6380f44c75197407c44088534c46d451c90f0a44ed7652634e1eb3880cf`. Both are retained. Processing, build selectability, final review submission and public availability must be verified independently by the coordinator.

Apple validation passed. The CLI retained the known missing vendor `UnityRuntime.framework` dSYM warning, UUID `603B8E5D-94BB-3C1A-98DC-175E340472CD`. The game UnityFramework and other three native binary dSYMs are verified separately and match.

## Checks and store preparation

Final-source validation passed one focused EditMode service-policy check and nine existing iOS-target PlayMode lifecycle/UI checks, with no failures. Actual generated device C++ has release diagnostic/capture gates disabled and no background-opt-in dependency in the service skip policies. Actual compiled desktop methods return false for diagnostic/capture/service-skip flags and filter the Library to the 15 common bullets. Background progress remains available, defaults off, and retains the Hardcore guard.

Desktop verification here covers packages and compiled code; this lane did not run a native desktop gameplay session or hardware QA. The Watch screenshots are Simulator evidence.

All eight built localization tables match the approved notes exactly. Decompression of the real player assets confirms the corrected 17-bullet English fallback and absence of the old 11-bullet fallback.

All eight App Store note sets were saved and verified after reload. Both Watch screenshots were uploaded and confirmed shared across all eight locales. They use the unmodified production Watch store and renderer with controlled fixtures, in an isolated Simulator app. The previous QA app/cache was restored; production progression was untouched. See the [capture receipt](watch-store-capture-receipt.json), [current run](watch-current-run.jpg), and [estimated away gains](watch-away-gains.jpg).

Review instructions were saved with the experimental feature's default-off, one-hour and Hardcore restrictions, and cached/queued/timed-out Refresh behavior; inherited Shop/Restore guidance remains. Automatic release and immediate automatic updates were selected. The required Watch capability bullet was appended from each approved translation while preserving every original description paragraph. Final description reload verification was interrupted by Safari session expiry. The coordinator owns the final browser readback, build selection, and public submission at [the inflight version](https://appstoreconnect.apple.com/apps/6749265146/distribution/ios/version/inflight). This receipt does not establish review submission or public availability.

## Preservation

The primary checkout's two existing edits and all player progression hashes are unchanged. Differences in the player directory are limited to generated test results and editor analytics. Signed archives, prepared/actual IPAs, receipts, and the protected SDK environment are retained. Only verified, completed generated compiler inputs and private caches were reclaimed. Temporary SDK copies, Editor helpers, and owned build configuration changes were removed from the isolated checkout after delivery.
