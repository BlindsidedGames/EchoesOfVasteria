# Internal iPhone and Watch TestFlight build 189

Apple accepted **1.5.9 (189)** from source `4f1ead91ef50e1e8174c1a31144eba2409795b2e` at **2026-11-01 00:06:12 UTC**. Upload ID: `c69dcf40-aace-4bde-992a-82954c0aa08a`. The structured response reports **Processing**, with no errors/warnings in that response. Internal tester availability and approved English (Australia) notes remain the coordinator's separate App Store Connect check; this receipt does not establish installation or physical gameplay acceptance.

The user approved [these exact testing notes](testflight-189-notes.txt) and the internal-only upload before delivery. Marketing version stays1.5.9 and Android stays186. Public Library/store release notes remain unchanged.

## Actual game and packaging proof

- Fresh full Unity6000.6.0f1 DeviceSDK/IL2CPP arm64 export from the final complete tracked source, using its normal Loading/Main scenes. No prior188 Unity Data/export/archive was retrofitted. Final export: Succeeded, **0errors**,94warnings. Demo/Beta/development/profiler are off; `EOV_BACKGROUND_PROTOTYPE` is the only experimental define, and the actual compiled PlayerPrefs fallback remains0/off. Editor automation and QA/test classes are absent from player metadata.
- Three existing focused lifecycle tests pass3/3 with the release prototype define: read-only companion capture, one-time settlement/warm world resume, and full-hour high-rate credit. No new tests or force-enable launch argument were introduced.
- Native Release archive, automatic/cloud Apple Distribution export and Apple validation pass. Host, widget and embedded Watch all report1.5.9(189), signed for the existing team with get-task-allow=false. Phone/widget are device arm64; Watch includes arm64_32 and arm64.
- The actual generated Unity Xcode graph and compiled host verify PhoneSession migrated into the main executable with WatchConnectivity linkage, while the C/ObjC caller remains in UnityFramework. Exported Swift/ObjC sources match the frozen source hashes. Host/widget Refresh intent metadata, compiled default-off logic,179 portrait assets and Watch icon pass verification. The non-exempt-encryption declaration staysfalse, matching188.
- The prepared IPA passes ZIP integrity. The **actual transmitted IPA** was retained:251,616,182bytes, SHA256 `9e27e9d0db0be5d43252d62ee060c39e47762e62b20609c0b0458643f316bc10`. All native UUID/TEXT sections, including both Watch slices, and every serialized Unity Data hash match the reviewed prepared IPA. Packaging/signature differences account for the different whole-package hash.

## Local build environment and preservation

The tracked checkout omits the licensed VoxelBusters managed sources and runtime settings used by188. Its complete2,705-file local vendor/config/meta set was restored byte-for-byte, hash-recorded, and privately retained along with the prior Mac importer/assembly/linker adaptations. The initial incomplete-SDK export raised a postprocessor exception and was discarded. The final SDK-complete export has zero errors. Unrelated188 gameplay code, synthetic QA helpers and player files were excluded. The companion JSON records exact manifest/configuration/adaptation hashes; the full dependency snapshot and raw signing logs remain local outside Git.

Temporary source/configuration changes were cleaned after successful delivery. The primary checkout's original HEAD and user diff remain unchanged (diff SHA256 `7d45e5d114159fefba437f0b4df201af1939c4ffc59e6c29725a468ee1c68ec2`). Of228 original player-directory files, only the generated TestResults.xml changed; no new files or progression changes were observed. Only regenerable Echoes build caches were cleared for disk headroom; prior/current signed archives and IPA packages were retained.

Xcode reported the existing nonblocking vendor UnityRuntime.framework dSYM warning (new UUID `D551654C-FD0B-35F5-B8AE-E0C93C43E314`); the game's matching UnityFramework dSYM is present. Apple accepted validation and upload.

This delivery is internal TestFlight only for existing app6749265146. No public App Store submission, external tester changes, main merge, Android or Steam distribution occurred. Tester-group/Testing evidence belongs to the coordinator and must be reported separately from this accepted upload.
