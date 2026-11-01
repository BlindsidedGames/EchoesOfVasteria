# Internal iPhone and Watch TestFlight build 190

**1.5.9 (190)** from `bcd40fc88291a1a5d6a97fe60843b8758cb6dcf1` is **Testing** in the existing **Internal** group with **three testers**. Apple accepted the upload at **2026-11-01T03:48:04+00:00** (upload ID `63ab1bf8-9c9c-4e21-a5b4-4c1752eb1716`); its initial structured receipt reported Processing. The final App Store Connect check at **2026-11-01T03:54:51.458693+00:00** confirmed Testing and persisted approved English (Australia) notes after reload. This establishes tester availability, not installation or physical gameplay acceptance.

The user approved [these exact 11 testing-note bullets](notes-en.txt), build 190, and the internal upload. Live Activity and Apple Watch bullets remain iOS-only in the in-game notes. The Git LF note hash is `cdb112fce36d8216fd17e369360836d6bfc419389ebfcbdf25ec55173b81cef6`; the supplied Windows CRLF hash `c7a5d4f7d8a5a3f0e90d5efbd0075c86e1418f10f5b51afd8393a13ec87aba08` reproduces exactly from the same text. Marketing stays 1.5.9; Android stays 186.

## Build and package verification

- Fresh full Unity 6000.6.0f1 DeviceSDK/IL2CPP arm64 export from the complete frozen source, normal Loading/Main scenes, CompressWithLz4HC: **Succeeded, 0 errors, 110 warnings**. Demo, Beta, development and profiler are off. Only `EOV_BACKGROUND_PROTOTYPE` is enabled experimentally; the actual IL2CPP PlayerPrefs fallback remains 0/off. No old Unity Data/export/archive was retrofitted.
- **7/7 existing release tests passed** in Editor PlayMode with iOS target and the release define: companion read-only capture; one-time settlement/warm resume; full-hour credit; Hardcore permanent death/durable reload; safe Carl reaps/normal restart; creation/memorial navigation; positive iOS Library notes with muted history. The exported IL2CPP notes renderer independently retains native bullets while stripping their iOS marker. No new tracked tests or gameplay changes were introduced in this Mac lane.
- Native Release archive, automatic/cloud Apple Distribution export and Apple validation passed. Host, widget and Watch all report 1.5.9 (190), use existing lowercase identifiers, team AVX7SUF8Y6, and final distribution get-task-allow=false. Host/widget are device arm64; Watch includes arm64_32+arm64. All four native binaries, including both Watch slices, have matching dSYMs. The non-exempt-encryption declaration remains false.
- Generated Xcode membership puts PhoneSession exactly once in the host with WatchConnectivity, absent from UnityFramework; all copied native sources match the frozen bytes. Refresh intent metadata, 179 portrait assets, Watch icon, compiled Hardcore code and exact iron-pick EOVTask/EOVSkill_Mining assets pass checks.
- Prepared IPA: **251,676,033 bytes**, SHA256 `6478f8e8464eb21b59a5c44ca70e7482c30b5018e92459ce5295c31abb3a0c78`. Actual transmitted IPA retained: **251,676,030 bytes**, SHA256 `b6fa2a354e64063bbba97863809a0bf64d84aab0960e66ac908846820365c674`. Host/widget/Watch (both slices)/UnityFramework UUID and TEXT identities, plus every serialized Unity Data hash, match the reviewed prepared package. Other frameworks were not independently compared.

## Library portrait verification

The real `ToolkitBookScreen.Show/ApplyLayout` renders all 11 approved bullets and muted 1.5.8 history at 1080x1920, scrolls, rotates to 1920x1080 and returns to portrait. Rendering-context Screen, Device.Screen, GameView and PNG dimensions agree. Portrait root is 360x584 panel units, viewport 336x428.33, current text 336x299; all 11 bullets match the approved text. The direct-view test's early capture used stale/Editor callback geometry (including a 768x376 root while the render was portrait); it is retained privately as a capture limitation and is not portrait product acceptance. No production source fix was needed; the coordinator explicitly cleared the upload hold.

[Portrait](library-owner/portrait.png), [scrolled history](library-owner/portrait-scrolled.png), [landscape](library-owner/landscape.png), [portrait after rotation](library-owner/portrait-after-rotation.png), and [full measurements](library-owner/measurements.json) preserve the owner verification. The diagnostic used a private empty scene/disposable save root and normal Library/theme/template; it selected the existing section route reflectively to avoid unrelated pointer-delivery instability. It is Editor render evidence, not a device 190 installation.

## Environment and preservation

All **2,705** licensed dependency/config/meta files were restored byte-for-byte from the protected SDK-complete 189 snapshot and re-audited. Their exact manifest, runtime configuration, local importer/assembly/linker adaptations, build helper, raw logs, signed archive and both IPAs remain privately retained. The JSON receipt records configuration/manifest/adaptation hashes without committing vendor code or signing data.

Temporary local changes were cleaned. The primary checkout HEAD and its two existing edits remain unchanged; progression files are unchanged. Generated TestResults.xml and Editor analytics changes are recorded separately in the companion receipt. Only verified regenerable owned Echoes caches/intermediates were reclaimed; all archives, IPAs and receipts were retained.

UnityRuntime.framework dSYM missing for UUID 4B96169B-4B8E-36D9-B0FB-BC2C01BBC73E; nonblocking Apple validation/upload warning. The host, widget, Watch device slices and UnityFramework all have matching dSYMs.

Delivery is internal TestFlight only. External/public App Store state, Android, Steam and main were not changed.
