# Radish slice review — 1 October 2026

The reviewed slice remains isolated in the development scene. It supports the original two beds, fresh Radish-pack discovery through completed primary-hero Radish tasks, planting, finite online/offline growth, a readable native bed list and atomic manual harvest. Production Main, other crop pools, Barkley's production quest content and Alter Echo cutover remain unchanged. No commit or release was made.

## Concrete fixes

- Record an earned task's fixed pack intent before ordinary completion callbacks and commit in `finally`. The immutable handle captures service, save owner, slot and operation. A thrown callback no longer loses the selected credit; a slot switch cannot pay it into another bank.
- After a durable harvest whose runtime inventory publication fails, enter the existing Loading recovery boundary and native Retry flow. Block stale writes; reload the paid receipt instead of continuing an unsavable game.
- Settle an adopted bank's persisted offline clock before an early command/save can replace its baseline. Harvest before the service's first Update now pays the finite ready batches once.
- Reject declared future/malformed legacy ES3 schemas and malformed recognized bank wrappers before selection. Repair only missing containers and present null records under the six authored skill keys. Preserve unknown records and actual non-null progress. Reject known NaN/Infinity XP without resetting it.
- Refuse old-schema writes from the two actual Editor save tools that bypassed migration; direct the user to supported migration before editing/cleanup. Read-only inspection remains available.

## Evidence and limits

The refreshed Unity suites passed **96/96 EditMode** and **51/51 PlayMode** cases. Focused migration cases passed **54/54**. All **45 ES3 banks and eight related binary snapshots** passed their corpus reruns with unchanged input hashes. These are local historical-format fixtures, not independent mobile/device cohorts. Mobile native containers, IL2CPP/AOT upgrades and asset-dependent historical gear transformations remain unverified release gates.

Four Player phases pass: callback/capture failure and ordinary native loop; immediate month-offline harvest; publication failure/native Retry; actual slot-switch-during-completion. The target bank receives no old credit; the source bank checkpoints the intent and retries it once on return.

The Mac Player is arm64/Mono on Metal. It builds with zero errors and two unchanged warnings: missing iOS localization App Info, and missing RuntimePipelineConfig (Pipeline disabled). Its raw teardown logs still contain Mono thread-finalization messages and the pre-existing buffer warning described below. The publication-failure run deliberately emits one retained Error diagnostic; unexpected runtime error/warning lists are empty in passing runs.

The native Retry regression waits for the existing Loading transition to finish. Initial Retry and return-slot harness attempts acted during their guarded transitions and are retained as failed evidence. The final harness awaits the completed transition before initiating another action. No production diagnostic was suppressed and no recovery control was changed to accommodate the test.

## Buffer warning attribution

Exact message: `GarbageCollector disposing of ComputeBuffer. Please use ComputeBuffer.Release() or .Dispose() to manually release the buffer.`

This message occurs in Mac validation logs from before farming. A separate empty Metal Player reproduces it with zero farm services, zero SpriteSkins, no Oracle and uninitialized UGS. It owns `UnityEngine.U2D.Animation.GpuDeformationSystem.s_FallbackBuffer`: 64 entries × 64-byte stride (4 KiB). Releasing only that buffer in a diagnostic run removes the message. The installed Unity 2D Animation 16.0.0 package creates the fallback at AfterSceneLoad; its release belongs to a deformation-system Cleanup path. This is a pre-existing package lifetime issue, not a farm allocation. No reflection-based shipping cleanup or vendor patch was added. The smallest proposed package correction is a supported lifetime/quit release that is safe when no deformation system was instantiated; verify against a maintained package fix before adopting it.

## Next integration decision

Recommend the next slice author typed matching-pack entries in the real plant drop pools, starting with Radish, using names already prepared in the atlas. Keep completed primary-hero adventure work as the first eligible source and exclude Echo generators, farm harvests and bonus-recursion paths. Review that eligibility alongside chance/cap behaviour before extending to every crop; the current 10%, 30-minute and 10-base-Radish values remain provisional. Production Barkley quests and Alter Echo cutover follow as separate reviewable slices.

Before widening completion sources, measure sustained receipt growth and snapshot time. The prototype retains hit/miss receipts indefinitely and currently clones its journal during growth updates; bounded retention must preserve exactly-once replay protection.

Prepared seed art and the approved layout/Library images are preserved. All 22 packet families, shared unknown, 23 appended drop characters and existing atlas references retain their verified versions. The native farm uses the prepared Radish/unknown sprites; no new art or resource routing was invented in this review.

The refreshed app and exact reports are retained at `/Users/matthewrushworth/Projects/Echoes Farming Implementation Evidence/2026-10-01/review`. Main remains clean in Edit mode. All 77 protected player files, 93 backup files and prepared icon/atlas hashes match. Every temporary isolation guard is restored exactly. Original Library image identities and versions are unchanged.

Restored compilation has zero C# errors and five distinct pre-existing Editor warnings: one CS0184 in ToolkitNativeCutover, one UDR0001 in GatheringBuffValidation, and three UDR0004 subscription warnings in VasteriaStyleLab. Their exact messages are retained in `restored-compilation-diagnostics.json`; they were not suppressed or broadened into unrelated rewrites. The focused final source patch contains only the implementation-owned code/test paths and the two Editor save guards.
