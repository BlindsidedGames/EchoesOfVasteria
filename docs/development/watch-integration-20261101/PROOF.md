# Native integration evidence

Source checkpoint: `ece1881bed427e1924bc37151545f24587ada62c`. Final proof media is committed separately. This work changes the native Watch source and editor packaging; it does not deploy or change release 1.5.9 (188).

## Production views at both supported sizes

The 14 `captures/{01,03,04,05,07,08,09}-{42,46}-final.png` files are current production SwiftUI views, compiled and rendered by the isolated Preview adapter. The two final contact sheets show the seven selected layouts. They use the shipping decoder contract, source-traced game icons and the full current character catalog. The adapter supplies fixture state and a frozen presentation clock; it does not prove a live phone transfer. The initial black `01-42-initial.png` is rejected startup evidence, not acceptance.

Additional captures at both sizes verify automatic foreground/away routing, offline saved estimates, restarting and expired trajectories, a terminal death, empty state and large-text Skillbook layout. `01/03-*-personalized-hair.png` show a second actual Unity-captured appearance in the production compositor, including the purple hairstyle. These are distinct from the older ten-design lab screenshots. Large text uses native scrolling; its first viewport deliberately does not compress all six rows into the screen.

## Actual production App and Store

`EchoesWatchStore` compiles the actual `EOVWatchApp.swift` and `EOVWatchCompanionStore.swift`, uses the real Watch clock and initializes the real WCSession. Its unique QA bundle is `com.blindsidedgames.echoes.watch.integration.store`. The harness stages a controlled valid snapshot only into that bundle's own cache, rebasing the origin consistently with real time. No QA arguments, frozen clock, replacement WCSession, test hook or fixed character exists in the production app.

`01-*-store-foreground.png` shows the cached foreground dial, explicitly marked Cached. `05-*-store-away.png` shows the real app selecting and interpolating the saved background trajectory. `05-*-store-expired.png` shows it freezing the completed trajectory at 180 resource gains, 100 task gains and 22 kill gains, with Estimate ended. Both Watch sizes passed these checks. The fixture has a deliberately short five-minute horizon; production uses the actual received horizon/earlier forecast end, without a hardcoded fifteen-minute duration.

## Motion

`clips/personalized-hair-active-42.mp4` records the second saved appearance animating through the native production compositor in the foreground fixture adapter. `clips/real-store-cached-static-42.mp4` records the actual app/store's cached appearance remaining static. Both are native Simulator recordings, not generated animation. The fixed hero-region measurements in `animation-check.txt` distinguish the substantial authored frame changes from small lossy-codec differences; they are not transport or input proof. The implementation also gates motion on scene phase, luminance and Reduce Motion. Those OS settings were not all re-exercised in this integration pass.

## Refresh semantics and input limitation

The visible 44-point Refresh buttons call the actual store closure. The real store guards activation/reachability, sends the existing `requestSnapshot` command, retains newer data over older cached replies, rejects invalid updates, and labels queued/unavailable responses. Source inspection confirms that the unchanged phone bridge returns its saved snapshot and queues a flag for active Unity. Suspended Unity cannot be promised to wake and recompute. The Watch can continue sampling its existing validated trajectory locally without extending its end.

A new paired live roundtrip and native Refresh-button gesture were **not verified** in this pass. CUA selected another task's iPhone Simulator window, and native window/menu selection returned stale element IDs or no-op input. Coordinate taps and Crown input were already unavailable in this environment. Input retries were stopped without changing the other task's simulator. Preview Refresh state substitution is explicitly not real transport proof. Main-navigation selection, pinning, chart selection, receipts and large-text scrolling compile but need interactive device/simulator acceptance for this integrated shell; earlier lab interaction proof does not establish these new production paths.

## Checks and preserved state

- Current-source Preview and actual Store targets: Xcode 26.4.1/watchOS 26.4 Simulator, both BUILD SUCCEEDED, minimum watchOS 11 unchanged.
- Eleven focused Foundation presentation contracts pass; output is `presentation-checks.txt`. They own routing, scope, stale animation, opt-out, legacy unknown values, map/origin mismatch, forecast expiry, terminal and empty/town boundaries. They do not duplicate the managed estimator or existing sampler tests.
- Both editor packaging C# files compile against installed Unity 6000.6 SDK assemblies and the existing character runtime assembly. This typechecks actual APIs; it is not execution of a fresh full Unity export. `packaging-check.txt` states this boundary.
- The QA catalog stages 179 supported character parts, each with a base and six authored idle images. Production exports the Watch-only animation catalog separately, retaining the existing Live Activity base assets.
- Decoder, native phone bridge and shipping ProjectSettings are unchanged from the incoming source. No main merge, upload or tester-availability claim is made here.
- Primary user checkout still has only its two original asset changes; its binary diff SHA256 remains `7d45e5d114159fefba437f0b4df201af1939c4ffc59e6c29725a468ee1c68ec2`. No player save or progression was modified. Old release worktree/build188 artifacts were preserved.

The root thread owns the combined managed/native review, gallery and merge. Native simulator acceptance here establishes rendering and controlled-cache behavior within the stated limits.
