# Startup memory

## Current result

The consolidated candidate uploaded in internal build 165, 9ca330359, has a sampled startup peak
of 583.504 MiB on the arm64 iPhone 13 mini Simulator (iOS 18.5), with
original character pixels preserved. The portrait-frame guard also passed native
character editing, animation, clearing and restoration checks. The affected physical iPhone
13 mini on iOS 27 still needs confirmation. Simulator frame rate remains capped
at 60 Hz; repeated loading/text-shaping work was eliminated, but no overall
phone FPS gain is claimed. Measurements below distinguish process footprint,
Unity counters and render-target allocations rather than adding overlapping totals.
The completed two-hour run peaked at 660.332 MiB and finished at 657.379 MiB,
with fixed post-warmup texture/sprite/panel/character-part counts. The later
20999d37e health-label cache also compiles and passes native label-value checks;
it remains outside uploaded build 165. Its isolated method benchmark is not an
overall FPS measurement.

All three retained export helpers use BuildOptions.None. Bee configuration records
Development=false, arm64 IL2CPP Release and disabled debugging/deep profiling;
Xcode logs also use Release. Nevertheless both native Simulator startup banners
say Development; that discrepancy remains unexplained and is not proof that
BuildOptions.Development was selected. Simulator builds include local memory/
frame-timing instrumentation, while uploaded device165 excludes it. Physical-phone
timings and footprint still require measurement; its runtime banner is unknown.

## Confirmed failure

The iPhone 13 mini report from 2026-10-19 records `EchoesofVasteria`
terminated for `per-process-limit`. Its footprint was 134,271 pages of
16,384 bytes, approximately 2,098 MiB. This establishes the cause of that
termination, rather than inferring it from the last startup breadcrumb.

## Root resource searches

A controlled Editor comparison returned the same 100 Resource definitions
from `Resources.LoadAll<Resource>("Resource Items")` and the root search.
The scoped search left loaded `CharacterPartFrames` assets at 8; the root
search increased them from 8 to 198. The Editor allocated-memory increase
was 164 MiB, but this is not an estimate of the iPhone saving: texture formats,
already loaded assets and Editor overhead differ.

`AssetCache` now resolves empty paths for game-data types to their specific
folders. This covers resources, tasks, enemies, quests, buffs, cores, rarities,
gear stats, infinity stats and base stats. The forge tutorial and base-stat
loader also use the scoped cache. An asset audit found every existing
definition inside its registered folder; no content was removed.

After clearing transient Editor assets, loading all ten categories through the
updated cache returned counts of 100 resources, 53 tasks, 22 enemies, 101 quests,
12 buffs, 8 cores, 8 rarities, 8 gear stats, 7 infinity stats and 9 base stats.
Loaded `CharacterPartFrames` stayed at zero throughout.

When adding a data category, use an explicit Resources path or register its
folder in `AssetCache`. Avoid a root search, even when supplying a type filter.
Diagnostic builds record both the beginning and successful end of each real
category load; cache hits no longer replace the operation breadcrumb.

## Character texture fidelity

A trial of ASTC 4x4 compression was rejected after decoded pixel comparisons
showed visible edge/colour errors in tools, hair and skin. All 214 character
texture imports retain their original settings. No source pixels, sprite slices,
resolution or animation frames are changed by this memory pass.

## Runtime retention

- Music uses streaming with the existing compression quality, sample rate and
  channels. The short sound effects retain their original low-latency settings.
- Closed menus detach and release their own `PanelSettings`, so native render
  panels and dynamic atlases do not remain allocated for every visited window.
  View state such as the library's article and scroll position remains in its
  existing fields and is restored when reopened.
- The character catalogue bounds cached, previously viewed parts. It keeps the
  preview and current outfit, drops unused catalogue references after the cache
  grows past 20 parts, and coalesces unused-asset unloads. Hidden portraits and
  disabled world/echo renderers also release their old sprite/texture references.
- Map cleanup discards inactive pools once no instances from that pool are live.
  Within-run pooling and live objects are unchanged; the next run can create
  instances through the same pool.
- The world sprite atlas no longer keeps a readable CPU copy. Runtime consumers
  render the sprites; none read the atlas's pixels.

These changes target both peak launch memory and growth after visiting menus,
trying outfits and travelling through different maps. They do not lower map
density, remove animation frames, change progression or discard player history.

## Audited systems retained

The segmented map generator already recycles its oldest segment as the player
advances; its live map window was not reduced. Sound effects share one source
and keep their immediate playback settings. Echo appearance compositing shares
the character sheets through material property blocks rather than allocating a
render texture per echo. Locale font bindings already unsubscribe on detach.
These systems were not replaced simply to increase the change count.

Focused PlayMode checks cover closed-book panel release/reopening, live-object
safety during pool cleanup, character creation/presets, and character/equipment
appearance across pooling. The iOS build comparison remains separate from
Editor validation: Editor memory is not the device's physical footprint.

The eight focused checks passed. The book-panel regression was also run with
the original `Hide` implementation and failed for the expected reason: five
transient panels remained (four closed menus and the shared dock), exceeding
the allowed one. Restoring the optimized implementation passed again.

An Editor stress probe cycled 136 non-armour appearance variants through the
production cache policy. Cached parts peaked at 20; 13 parts remained cached
and resident after the final unused-asset sweep. This validates the cache bound,
not a physical-device memory estimate. Test-only render/Play settings were
restored after validation, and the tests used isolated save directories.

## Device verification

Use a new internal build on the affected iPhone 13 mini. Run normal startup,
record native peak footprint and minimum headroom, and verify the game reaches
town. Check character creation, equipped armour, tools and transparent echoes
for colour/alpha artifacts. Test entering maps and returning to town as well
as launch, so that a lower startup peak does not hide a later loading spike.

Further work should be based on measured resident allocations;
do not add forced garbage collections or repeated unused-asset unloads to
mask assets retained by broad searches.

## Native Simulator observations (intermediate)

The Release iPhone 13 mini Simulator on iOS 18.5 reached Ready with a sampled
native startup footprint peak of 598.50 MiB and steady footprint of roughly
596–599 MiB, versus approximately 734 MiB in the earlier diagnostic run.
This candidate still used the subsequently rejected ASTC imports; the Simulator
reported ASTC unsupported and expanded those textures to RGBA. It is useful
profiling evidence, not a final original-texture result or an iOS 27 device
measurement. Peak RSS was 992.72 MiB; RSS and physical footprint are different
metrics and must not be interchanged.

Lifecycle checks covered changing hair/clothes, completing character creation,
quests, inventory/equipment, statistics, repeated options visits, and entering
and leaving Farmlands. Character-part assets fell from 11 during preview to 7
in the map and after return. Streaming music changed from TownAmbient (199,731
bytes loaded) to TimelessOST (204,719 bytes loaded, TownAmbient unloaded), then
back to TownAmbient with the OST unloaded. Later footprint snapshots were
623.94 MiB in preview, 633.69 MiB in the map and 627.44 MiB after returning;
these include earlier readback and allocator history, unlike the cleaner startup
measurement. Closed quests/inventory/statistics/options documents no longer
had assigned panels. No crash or broken menu was observed in this sequence.

Final profiling uses restored original textures. A separate reversible experiment
checks whether the pipeline's sampled depth/opaque textures can be disabled
with identical rendered pixels; required depth/stencil attachments, lighting,
HDR, resolution and MSAA are retained. Do not apply those pipeline changes
until the comparison establishes presentation parity.

### Original-texture candidate

Commit 08b072 reached diagnostic Ready in 4.79 seconds at 572.13 MiB on the
same Simulator setup. The external 100 ms kernel sampler observed a 581.91 MiB
peak through Ready +35 seconds, with settled samples of 579.77–581.85 MiB.
No character-part assets were resident in the initial town snapshot. This is
an original-texture measurement; the earlier ASTC candidate is retained above
only as intermediate investigation evidence.

Settled town frame timing reported a 16.67 ms wall median and 17.29 ms p99,
3.33 ms main-thread median, 0.70 ms GPU median and 13.4 ms present wait. This
is a 60 Hz-capped Simulator run, not a performance measurement on an A15 phone.

In a reversible town experiment, disabling only optional depth/opaque copies
removed two R32 1080x2339 sampled depth textures (19.27 MiB). Render-target
allocations changed from 65.18 to 45.90 MiB and returned to 65.18 MiB when the
original pipeline was restored. The D32 depth/stencil attachment remained.
Paused whole-screen pixel comparisons found zero differences for both the
disabled-copy state and restoration against their respective frozen controls.
Two control states differed by 955 pixels due to unscaled UI animation.
Process footprint drifted upward during probing, so the allocation reduction
must not be described as a measured 19.27 MiB physical-footprint drop. Run-map
comparisons remain pending before adopting these pipeline flags.

The Farmlands run also matched the frozen original and control exactly with
optional copies disabled (zero changed pixels). Its render-target allocations
changed from 70.05 to 55.40 MiB and back to 70.05 MiB when restored, removing
three sampled depth copies totalling 14.65 MiB at that viewport. GPU medians
were 0.682/0.574/0.708 ms for original/off/restored, while wall timing remained
60 Hz-capped. Driver allocations were 251.26/238.11/254.26 MiB; physical
footprint was 601.47/600.39/612.18 MiB with probe/history drift. Restoration
pixel differences are still being investigated for unscaled UI animation;
Mines and Haloween lighting comparisons remain pending. Pipeline flags are
not yet changed in the production candidate.

### Optional render copies adopted after map comparisons

The final Mines and Haloween (Spooky) comparisons both had zero changed pixels
across original/control/copy-off/restored/post-restore-control captures, with
matching UI sprite states. Mines covered a run-end cave state; Spooky covered
a live authored state immediately after entry. These are isolated rendering
fixtures using the production BeginAdventure entry point, not quest-unlock tests.
Both removed three sampled depth copies (14.65 MiB): total render targets
changed from 70.05 to 55.40 MiB and back to 70.05 MiB after restoration. Driver
allocations fell by approximately 13.15 MiB; process footprint drift during
probing prevents claiming the same physical-memory reduction.

GPU median samples were 0.564/0.486/0.572 ms in Mines and 0.666/0.569/0.640 ms
in Spooky (original/off/restored); frame rate remained capped at 60 Hz.
These are small Simulator GPU-work reductions, not demonstrated phone FPS gains.

UniversalRP now disables only Require Depth Texture and Require Opaque Texture.
No authored shader/script samples these optional copies; Renderer2D has no
custom renderer features and the volume's depth-of-field mode is Off. Required
depth/stencil attachments, lighting, HDR, render scale, MSAA and art are retained.
Future shaders that sample scene depth or colour must explicitly request the
appropriate copy. Native CPU sampling is the next FPS investigation step.

## Measured CPU follow-up

A 20-second Apple `sample` capture of the disposable native Simulator process
resolved IL2CPP and native symbols. Of 1,505 main-thread wall-stack observations,
1,081 were waiting in mach_msg. Thirty-two observations included navigation
Refresh -> Resources.Load -> Unity's native ResourceManager.PreloadDependencies;
these are inclusive wall samples, not CPU percentages. Release inlining did
not expose FarmContent.Load as a separate sampled frame. Its direct call is
present in the generated Refresh source, and the production navigation code
polls FarmContent.Load for the Fields label every 0.1 seconds.

FarmContent.Load now reuses the authored object, checks Unity object lifetime,
and resets its reference on subsystem registration. DisplayName remains evaluated
normally, including locale changes; recipes, growth sprites and costs are not
cached separately or changed. Five warm Editor samples of 1,000 lookups had
median 0.2675 ms before and 0.0201 ms after, returning the same content (21 recipes,
9 builds). This isolates lookup overhead and is not an overall FPS gain.
All four existing FieldsContentTests passed after explicit refresh/compilation,
covering authored construction, localization, recipes, outputs and growth sprites.

Another 12 wall samples included portrait dialogue MeasureTextSize from
FitPortrait. That remains a candidate for a correctly invalidated measurement
cache; a text/width-only cache is insufficient when font/style/locale changes.
No incomplete dialogue cache is included in this candidate. Instruments Time
Profiler stalled while attaching, so the successful Apple sample fallback is
the source of the stack evidence above.

### Consolidated native follow-up

The c831dd89f original-texture candidate (runtime cleanup, optional camera
copies disabled, Fields lookup cached) exported and built successfully in
native arm64 Release. Its external sampled startup peak was 580.488 MiB;
settled samples were 579.379–580.488 MiB. In-app Ready was 5.44 seconds and
host-observed Ready 11.909 seconds on this first post-install launch. These
startup timings are not controlled speedup comparisons. UGS signed in; Game
Center authentication was requested. Optional sampled depth copies were absent
and the required D32S8 attachment remained.

Farmlands timing over 1,200 frames had wall median 16.666 ms, p95 17.199 ms,
p99 17.260 ms; main-thread median 3.596 ms, p95 4.196 ms; GPU median 0.525 ms,
p95 0.813 ms; present-wait median 13.096 ms. The run remained 60 Hz-capped.
A second 20-second Apple sample captured 1,558 main-thread wall observations:
zero Resources_Load, ResourcesAPIInternal_Load or native ResourceManager
PreloadDependencies samples, versus 32 in the earlier capture. Navigation
Refresh appeared once. Dialogue FitPortrait appeared 15 times (previously 12),
while main-runloop waiting accounted for 1,099 observations. This supports
removing repeated loader work, not a controlled overall FPS improvement.

### Portrait dialogue measurement cache

FitPortrait now reuses the text-height measurement when the exact text,
resolved width, font/font definition/style/size, spacing, whitespace/rich-text
settings and screen scale are unchanged. Dialogue and locale refreshes also
invalidate it. Placement and available-height clamping still run normally;
animations, scrolling and action buttons are unchanged.

A new rendering-boundary PlayMode check passed for advancing to a longer line,
changing font size, narrowing/restoring the text viewport, and limiting card
height. An initial letter-spacing assertion was removed because it assumed
this font's MeasureTextSize must grow for that setting; it did not establish
a cache regression. The final check verifies observable fitting behavior.
The test used isolated saves and restored rendering/Play settings. Native
sampling of the dialogue-cache candidate remains pending.

### Native dialogue-cache follow-up

8cfe5ed75 built in native arm64 Release and reached Ready with a 580.566 MiB
external sampled peak and settled 577.754–580.535 MiB. In-app Ready was 4.91
seconds; host-observed Ready was 8.347 seconds. The matching Farmlands/two-line
dialogue remained visibly fitted with its Close button and no clipping.

Of 1,543 main-thread wall observations in a 20-second sample, FitPortrait
appeared twice (previously 15); the measurement helper and native text-shaping
chains appeared zero times (previously 15). Resource-loader/dependency traversal
also remained at zero. Typed tuple/font comparers were not sampled as hotspots;
generated typed comparison code has no direct boxing/new-object calls, but
warm GC bytes were not instrumented, so this is not a strict zero-allocation
claim. Overall main median was 3.677 ms (prior 3.596), p95 4.161 ms (prior 4.196),
wall median 16.669 ms/p95 17.208 ms and GPU median 0.543 ms/p95 0.864 ms. No
controlled overall FPS or CPU-time gain is established beyond removing the
sampled repeated shaping work.

The pre-readback map inventory reported Unity allocated/reserved 311.544/485.922
MiB, driver 233.351 MiB and managed 69.625 MiB. Texture2D objects totalled 157.986
MiB, render targets 55.398 MiB and audio 0.225 MiB. Render targets overlap the
all-texture total of 213.388 MiB and must not be added again. The largest items
include the retained mandatory depth/stencil attachment (24.092 MiB), UI_ALL
(12.797 MiB), Simulator-decoded Unity splash (10.667 MiB), two colour targets
(9.637 MiB each), world atlas (8 MiB), and original character sheets (7.875 MiB
each). No further lossless artwork reduction has been demonstrated from this
inventory; required buffers and original pixels remain intact.

### Unchanged character portrait frames

CharacterImage now compares the six appearance IDs, catalogue, frame and tool
visibility before rewriting its eight layers/reordering the tool/scanning the
wardrobe cache. It compares values because creator profiles mutate in place;
clearing the portrait also clears the cached state and all sprite references.
The UI remains responsive to a changed style, animation frame or tool visibility.

Five warm Editor samples of 1,000 unchanged-frame calls had median 5.7136 ms
before and 0.3868 ms after. This is an isolated unattached-portrait workload,
not a native frame-time or phone FPS claim. The new boundary test passed for
same-object hairstyle edits, tool toggling, releasing all layers and restoring
the same frame after clearing. All four existing character PlayMode checks
also passed after this change, covering creation/presets, equipment visibility,
player/Echo pooling/appearance and preset validation. Native validation of this
latest portrait-frame guard is pending.

### Final portrait-guard native validation

9ca330359 built successfully in native arm64 Release (Unity export zero errors,
72 warnings; Xcode BUILD SUCCEEDED). Its sampled startup peak was 583.504 MiB
and settled footprint 581.019–583.504 MiB. An isolated creator check in an empty
second slot changed Hair 1/2/3 and Farmer/Lumberjack shirts on the same draft,
advanced Run frames, turned and showed the fishing rod, cleared hair with None
and restored Hair 1. The original disposable first slot was restored afterward;
real saves and checkouts were untouched.

The 20-second sample had 1,571 main-thread wall observations: resource-loading
and text-shaping chains remained at zero, CharacterImage.Set appeared twice
(previously three), wardrobe trimming had no resolved frame, progress actor
animation appeared four times (previously five), and one ValueTuple.Equals
frame was sampled. Counts overlap callers; zero may reflect inlining or
unsampled work. Main median/p95 were 3.563/4.090 ms, wall median/p95
16.667/17.227 ms and GPU median 0.631 ms. These do not establish a precise
CPU-time or phone FPS gain.

One consolidated internal TestFlight archive is being prepared from this source
with the original textures and normal app ID. The local profiling hooks and
disposable app identity are excluded; internal startup diagnostics are retained
so a nondeveloper can choose Normal and screenshot the completed check's native
peak/headroom. Production publishing and merging remain out of scope.

## Internal TestFlight delivery

Apple accepted one consolidated internal-only upload of **1.4.8 (165)** on
2026-10-19 at 12:50:25 UTC / 23:50:25 Australia/Sydney. Runtime source is
9ca3303593492b2a6504d66ddb8eabeb238b39e4. Xcode distribution receipt:
`d439c8f4-a32b-4d31-b72d-320dd17a5ef7`. This is an upload receipt, not an ASC
build-resource ID. Apple processing and actual internal tester availability
remain unverified.

The exported IPA has the normal `com.blindsidedgames.echoesofvasteria` app ID,
team AVX7SUF8Y6, version/build 1.4.8/165, Game Center enabled, get-task-allow
false and beta-reports true. ExportOptions has testFlightInternalTestingOnly
true. Xcode validation succeeded; no duplicate version/build rejection occurred.
The original 214 character texture metadata tree is
`9fcfd259d004db7ea158c76f952c04dd32d1934a`. The normal save implementation is
unchanged; local profiling code, metadata and disposable identity are excluded.
Six protected checkout fingerprints were unchanged.

A nonfatal upload warning reports a missing prebuilt UnityRuntime.framework
symbol file, UUID 5BDEE174-216F-35FB-9C77-BF4D20DD244A. No matching symbols were
installed locally; the upload still completed. Archive, exact IPA, export
options, receipt and source fingerprints are retained on the Mac. No production
release, external tester-group changes or main-branch merge were performed.

## Retention soak

A 20-minute isolated Simulator soak of 9ca330359 completed ten authored map/return
cycles (Farmlands four, Mines three, Spooky three) and ten Options open/close
cycles in one process, with no new build or GPU readbacks. The external sampler
captured 11,577 points; observed peak was 653.488 MiB and kernel lifetime peak
653.629 MiB. Initial town was approximately 593.35 MiB, rising to 634.05 MiB
after first visits to the three maps. Repeated town medians for cycles 4–10 were
641.29, 636.68, 643.32, 651.74, 643.91, 652.58 and 643.19 MiB. Post-cycle idle
was 643.426 MiB.

Texture count 458, sprite count 6,806, PanelSettings count 10 and loaded character
parts 8 were stable after warmup. Options had no assigned panel after closing.
Unity reserved memory stayed at 548.953 MiB from cycle 4 onward. Allocated memory
rose from 385.48 to 387.832 MiB between cycles 3 and 10 (+2.350 MiB), then fell
slightly to 387.777 MiB during idle; managed memory declined 4.133 MiB over cycles
3–10. Texture runtime bytes changed by exactly zero; town/map render targets
were 45.904/55.398 MiB. This is bounded short-soak evidence with a small residual
allocation drift, not proof of zero growth or absence of long-session leaks.

Farmlands was tutorial-dialogue paused, so its coverage is construction/cleanup.
Mines and Spooky advanced normally, including deaths/reaping and one automatic
restart. No crash or managed exception was found; known Simulator texture-fallback
and shadow warnings remain. The startup checkpoint stops at Ready and its warning
counter is not continuous soak evidence. Direct pool counts and an unused-asset
sweep command were unavailable in the retained probe; neither a new production
seam nor a new profiling build was added solely to obtain them. All six protected
checkout fingerprints remained unchanged.

The raw report, endpoint CSV, native memory sampler, logs and preservation checks
are retained on the Mac under
`/Users/matthewrushworth/Documents/Codex/2026-10-19/task/memory-20261019/evidence/lifecycle-soak-9ca/`.
Consolidated deployment artifacts are under
`/Users/matthewrushworth/Documents/Codex/2026-10-19/task/consolidated-20261019/`,
including the archive, Export-1.4.8-165 IPA/options/summary, successful upload receipt,
build/source fingerprints and preservation reports. The Organizer archive copy is
`/Users/matthewrushworth/Library/Developer/Xcode/Archives/2026-10-19/Echoes-1.4.8-165.xcarchive`.
A longer isolated soak is the next overnight validation step; no additional
runtime changes have been made after the consolidated upload.

## Deferred lossless character-packing experiment

Temporary copies of Hands_1_Bare, Player_Base_animations and Hair_1_Brown were
packed independently into non-rotating, rectangular, Point-filtered RGBA32 atlases
with four-pixel padding. Original source images and their metadata were untouched;
all temporary Assets entries were removed after each experiment. This was an
Editor-only prototype, absent from runtime 9ca330359 and internal build 165.

Each original texture is 576 x 3584 (7.875 MiB RGBA32). Packed pages were 2 MiB
(Hands), 1 MiB (Body) and 0.5 MiB (Hair): 23.625 to 3.5 MiB combined. These are
calculated texture-page bytes, not measured native process savings. All 921
frames retained rect size, pivot, PPU, vertices, triangles and bounds. Reconstructing
3,772,416 source pixels produced zero alpha or visible-RGB differences; RGB
under fully transparent pixels differs and is not claimed byte-identical.

An isolated GPU blit probe tested affine atlas UVs plus per-frame trim clipping.
At 64- and 128-pixel output sizes, all 18,862,080 compared pixels retained alpha
and visible RGB exactly. At 96 pixels (fractional 1.5x scale), the same technique
failed parity: across 64/96 outputs, Hands had 3,425 alpha / 3,313 visible-RGB
differences; Body 14,933 / 18,873; Hair 7,600 / 14,028. Integer-texel and quantized
sampling experiments also failed to preserve those fractional-scale results.
The sampling-boundary cause remains a hypothesis, not a verified Metal diagnosis.

Consequently packing is deferred, with no production shader, atlas, appearance,
or texture-import changes. The current echo compositor assumes untrimmed canvas
UVs and would also require validated atlas-aware sampling. Any future integration
must preserve fractional-scale UI/world/echo output, keep per-part lazy loading,
and demonstrate native memory savings; source-pixel preservation alone is not
sufficient. Local reproducible scripts and JSON are retained under
`Artifacts/StartupInvestigation/LosslessCharacterPackingProbe*` and
`Artifacts/StartupInvestigation/lossless-character-packing-probe*.json`.

A separate two-hour native soak of unchanged 9ca330359 started at 00:28:15 Sydney
on 20 October, with a guarded 02:28:15 deadline. It uses an independent background
collector and retains its disposable container. After Mines/Options warmup, the
unattended phase runs Spooky with normal death/restart behavior; the existing
probe cannot automate town/menu returns or app background/foreground transitions.
Results are pending under the Mac evidence folder
`overnight-soak-9ca-20261020`; this launch is not yet counted as completed coverage.

## World-space enemy health labels

After the consolidated upload, profiling the public ToolkitWorldAnchor.Text path
found that every visible enemy-health label reformatted an unchanged pair of
integers every frame. In an isolated Editor probe (no Play mode or save access),
100,000 stable-health calls took 48.767 ms, returned 100,000 distinct string
instances and increased retained managed memory by 8,536,064 bytes while those
outputs were held. This includes formatting/boxing overhead and is not a native
allocation-rate or overall frame-time measurement.

Cache the formatted label by its displayed current/max integers and the current
number format's negative sign. Health is still read on every request; fractional
rounding, healing, initialization, replacement health components, missing health,
static text and in-place number-format changes preserve their existing output.
The cache retains no extra actor, asset or save references. The same probe after
this change took 6.7757 ms, returned one reused string for unchanged health and
reported zero additional retained managed bytes for the warm calls. Treat these
single samples as a local diagnostic, not a precise player CPU/FPS speedup.

The focused WorldHealthTextTests EditMode behavior check passes (1/1), exercising
fractional damage/healing, the minimum-one display for living enemies, zero HP,
new maximums on reuse, rebinding, fallback text and mutable number formatting.
Probe and test evidence are under
`Artifacts/StartupInvestigation/WorldHealthTextProbe.cs` and
`Artifacts/StartupInvestigation/world-health-label-tests.json`. This follow-up
is not included in TestFlight 1.4.8 (165); native verification is still pending.

## Completed two-hour native soak

The isolated 9ca330359 process completed normally at 02:28:04 Sydney on 20 October
2026 (about 7,189 seconds), then shut down under the guarded collector. Across
67,304 native samples, kernel peak was 660.332 MiB and the final ten-second median
was 657.379 MiB. Endpoint medians after the first fifteen minutes ranged from
650.316 to 660.207 MiB; Unity allocated memory ranged from 390.681 to 395.565 MiB,
while reserved memory stayed at 552.953 MiB. This bounds observed long-session
retention; it does not prove that every gameplay path is leak-free.

After warmup, textures 461, sprites 6,806, panels 10, loaded character parts 8,
materials 106 and documents 26 stayed fixed. Texture bytes were 217.483 MiB and
render-target bytes 55.398 MiB (overlapping counters, not additive totals).
Meshes varied between 12 and 94 with run state rather than increasing monotonically.
No managed exception or error was logged; known Simulator warnings remain.
The initial Mines summary explicitly showed four automatic reaps and 300 steps;
Spooky continued normal reaping/restart behavior, but its total restart count was
not exposed and is not inferred. Town/menu and background/foreground limitations
remain those documented above. All six protected checkout fingerprints matched.

A separate 15-second native wall sample at 02:24:51 collected 776 main-thread
observations: EnemyActivator.LateUpdate 5, ToolkitWorldScreen.LateUpdate 5,
AIBase 7 and AIPath 1 inclusive, with 562 Mach-wait observations. No resolved
ToolkitWorldAnchor.Text or text-shaping frame was sampled. These sparse inclusive
counts are neither CPU percentages nor evidence that an unsampled method costs
nothing; no extra activation/AI changes were made on their basis.

Final report, completion receipt, endpoints, native memory CSV and sample are
retained under the Mac evidence folder `overnight-soak-9ca-20261020` described
above. A separate disposable native build of 20999d37e is compiling for the health-
label follow-up; it is not part of the completed soak or uploaded build 165.

## Native health-label follow-up

The disposable arm64 Release Simulator build of 20999d37e compiled through Unity
and Xcode with zero Unity errors and reached Ready. Its sampled startup kernel
peak was 577.58 MiB; the difference from the earlier 583.504 MiB run is not
attributed to this small text cache because process baselines vary.

The final bounded observer checked actual Toolkit Label.text values after public
health operations: Init(137), damage, heal and Init(251) produced `137 / 137`,
`136 / 137`, `137 / 137` and `251 / 251`. Earlier observer attempts lost their
selected enemy to a queued restart or missed initially hidden bars; those are
retained observer failures, not product bug reports. The authorized correction
used the existing world viewport, public health/bar activation, completed run
loading and a local pause. Screenshot glyph verification remains incomplete:
the selected label appeared under the simulated notch in the captured views.
Property assertions are therefore not described as pixel/glyph verification.

Five native batches of 100,000 unchanged calls had a median of 2.149 ms for the
cache versus 16.0103 ms for a copied 9ca formatter. This is an isolated method
comparison, not an overall CPU or game FPS result. The IL2CPP allocation counter
returned zero for both versions including the allocating legacy control, so native
allocation measurement is unavailable rather than reported as zero allocations.

A fresh 15-second Spooky wall sample had 1,137 main observations: Mach wait 739,
EnemyActivator 2, ToolkitWorldScreen 5, AIBase 14, AIPath 3, Pathfinding 12 and
UITKTextHandle 10 inclusive; Anchor.Text had no resolved samples. Counts remain
sparse and inclusive, with no overall improvement inferred. The disposable app
was stopped, its container retained, and all six protected checkout fingerprints
were unchanged. No further rebuild or upload was performed. Evidence is retained
at `/Users/matthewrushworth/Documents/Codex/2026-10-19/task/healthtext-20261020/evidence/`,
including native-validation-report.json, visual-observation.json, health-*.json,
native-cpu-15s.txt, completion.json and preservation-after.json. Uploaded build
165 still contains 9ca330359, not this follow-up.

## Allocator sizing experiment (not adopted)

Existing logs expose configuration but no allocator utilization/overflow/block-use
summaries: 24 inspected logs contained 31 startup entries and no usable closure
reports. The approximately 157.5 MiB reserved/allocated gap is not demonstrated
reclaimable physical memory. Unity's [allocator customization documentation](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/performance-memory/native-memory/memory-allocator-customization)
requires comparing application measurements; project overrides are still absent.

A copied 9ca Simulator bundle on a new disposable iPhone 13 mini/iOS 18.5 device
ran three 300-second Town (60 seconds) / Spooky (240 seconds) sequences. The exact
initial disposable Saves were restored between runs. Only four boot.config block
sizes changed: main, shared thread, graphics main and graphics shared thread,
16 to 8 to 16 MiB. Startup logs verified each setting and all other allocator
configuration remained unchanged. No export, source, texture or gameplay changes
were involved. Earlier host-driver preflights are retained as observer failures.

| Native measurement | Default 16 MiB | Trial 8 MiB | Restored 16 MiB |
| --- | ---: | ---: | ---: |
| Settled Spooky median footprint (MiB) | 625.410 | 612.894 | 624.285 |
| Kernel peak footprint (MiB) | 626.504 | 615.269 | 626.238 |
| Town median footprint (MiB) | 556.722 | 536.496 | 556.941 |
| Final Unity reserved (MiB) | 553.953 | 527.969 | 551.953 |
| Final Unity allocated (MiB) | 390.901 | 390.794 | 390.953 |
| Map wall median (ms) | 16.6677 | 16.6712 | 16.6687 |
| Map wall p95 (ms) | 17.2585 | 17.3533 | 17.1881 |
| Main-thread median (ms) | 3.7807 | 3.9037 | 3.7584 |
| GPU median (ms) | 0.5642 | 0.5607 | 0.4930 |

The trial's map median is 11.953 MiB below the mean of the default runs, compared
with 1.125 MiB default-repeat drift. Town is 20.336 MiB lower and reserved memory
24.984 MiB lower. Late counts matched: 456 textures, 6,683 sprites, 10 panels,
7 character parts and 25 UIDocuments; texture bytes 215.384 MiB include ten render
targets totalling 55.398 MiB. Meshes (94/95/88) and normal RNG/restart states differ,
so these are promising Simulator observations, not hardware or per-allocator
attribution. No FPS improvement is established. There were nine matching known
warnings per run and no new errors; overflow statistics remain unavailable.

The reference manifest, full installed default manifest and six protected checkout
fingerprints matched after completion. Both experimental devices were shut down;
all three apps and host collectors were absent. Evidence is under
`/Users/matthewrushworth/Documents/Codex/2026-10-19/task/allocator-ab-20261020/attempt4/`,
including reviewed-results.json, final-report.json, completion.json,
post-completion-verification.json, per-run raw data and bundle manifests.
An 8/4/8 MiB confirmation is pending; no allocator project setting is adopted yet.

## Final allocator decision: keep production defaults

The final 8/4/8 MiB comparison completed at 05:20:01 Sydney on 20 October, with
exactly the same four boot keys varied and the remaining 27 startup parameters
verified unchanged. Each run restored the initial disposable state and used the
same Town/Spooky timing. No additional export or production setting was changed.

| Native measurement | First 8 MiB | Trial 4 MiB | Repeated 8 MiB |
| --- | ---: | ---: | ---: |
| Town median footprint (MiB) | 538.941 | 533.175 | 546.738 |
| Settled Spooky median footprint (MiB) | 615.707 | 612.566 | 621.519 |
| Kernel lifetime peak (MiB) | 617.910 | 616.113 | 625.691 |
| Final Unity allocated (MiB) | 390.827 | 389.909 | 391.303 |
| Final Unity reserved (MiB) | 527.969 | 514.781 | 529.969 |
| Map wall median / p95 (ms) | 16.6612 / 17.6752 | 16.6675 / 17.6924 | 16.6573 / 16.7138 |
| Map main median / p95 (ms) | 3.7844 / 4.6470 | 3.7438 / 4.4424 | 3.5678 / 4.3566 |
| Map GPU median / p95 (ms) | 0.4996 / 0.7711 | 0.5577 / 0.8045 | 0.5660 / 0.9339 |

The 4 MiB map median is only 3.141 MiB below the first 8 MiB control, versus
5.812 MiB drift between the 8 MiB controls. Its 6.047 MiB mean-control difference
is inconclusive. Final listed assets matched (456 textures, 6,683 sprites,
10 panels, 7 character parts and 25 documents), but intermediate states did not:
at 180 seconds the last control had 455 textures/8 panels versus 456/10, matching
again by 240 seconds. Normal RNG, restart and host/driver states are not identical.
There is no demonstrated overall FPS benefit or causal allocator regression.
All runs had the same nine known warnings and no new crash/error/exception.

The allocator Simulator native engine banner says Development despite the retained
BuildOptions.None, Development=false and IL2CPP/Xcode Release configuration.
The reason is unestablished; explicit BuildOptions.Development was not selected. Its
main-mimalloc-enabled=1 log is configuration evidence, not proof of the actual
per-allocator backend or utilization. The uploaded device165 uses BuildOptions.None
and has no allocator overrides in its archived boot.config. No physical-device
startup/backend log establishes comparable allocator behavior; Unity export logs
are host Editor evidence. Consequently neither trial is adopted. Project memory
settings remain original, and the entire reserved/allocated gap is not claimed
as reclaimable memory.

Read-only verification at 05:32–05:33 confirmed all experiment helpers/apps were
absent, the host lock released, both owned devices shut down and the full installed
16 MiB configuration restored to both the reference and attempt4 baseline manifests.
Reference bundles, six checkout fingerprints and retained container metadata matched.
Evidence is under the sibling `allocator-ab-20261020/attempt5/` folder, including
reviewed-results.json, post-completion-verification.json, final-report.json,
completion.json and all run data/manifests. The reviewed inconclusive decision
supersedes the automated report's earlier interpretation. No more variants or
builds are requested for this sizing experiment.


## Retained build provenance clarification

Read-only inspection of all three export helpers confirms BuildOptions.None. Their
Bee Player input configurations record Development=false, iOSDebugConfig=false,
arm64, IL2CPP ConfigurationName=Release, AllowDebugging=false, deep profiling
disabled and Full LTO. Simulator=true for 9ca and 20999; false for device165. All
three Xcode commands use Release. Compact result JSONs omit final BuildReport.options,
so that field is not invented. Incremental Bee files are associated by matching
destination/app ID/variant, rather than claimed as immutable per-source snapshots.

Both actual Simulator stderr logs nevertheless print Build type Development. The
reason for this engine banner/configuration discrepancy is unestablished. The
accurate measurement description retains both facts, plus local EOV_MEMORY_PROFILE
and frame-timing instrumentation in Simulator exports. Device165 enables startup
diagnostics but not those profiling hooks/frame timing. Its native player banner
and actual allocator backend remain unknown without a device log. Host Editor
export logs are not device startup measurements.

Exact paths, excerpts and hashes are retained at
`/Users/matthewrushworth/Documents/Codex/2026-10-19/task/build-provenance-clarification-20261020.json`.
This clarification required no build, launch or settings/source change.

## Matched pre-optimization Unity startup profile (20 October 2026)

This comparison supersedes any claim that the earlier 734 MiB Simulator result
and later 583.504 MiB result establish a matched total reduction. The original
734.050720 MiB capture really used pre-optimization source `8a6bd24c`, but it
sampled at 500 ms until Ready, with the fresh-save character creator open.
The later native capture sampled externally at 100 ms through Ready plus
35 seconds with a created character. Those are different states and windows.
Neither reproduces the physical iPhone's 2,097.984 MiB Jetsam footprint, and the
cause of that Simulator/device gap remains unestablished.

### Controlled Editor comparison

The baseline was checked out and actually launched, rather than estimated from
current code. Separate fresh Unity 6000.6.0f1 processes ran source
`8a6bd24c473d5a109324707a5168b9c481db48cd` and optimized source
`472eb7ad53e356a9bd994c30449dc5b4fb9c0c20` in an isolated managed worktree.
Both used Loading.unity, full domain/scene reload, fresh isolated saves and
PlayerPrefs, a 1080 x 2340 GameView, deep profiling disabled, and byte-identical
observer code. A single identical AssetCache.GetAll profiler scope was added
to both sources. Real player saves and the user's primary Editor were preserved.

Both matched runs started with zero loaded CharacterPartFrames, 281 sprites and
974 textures. Their first captured Total Used Memory differed by only 0.709 MiB.
CPU binary capture continued to Main data ready plus 5 seconds; memory sampling
continued to ready plus 35 seconds. Native/managed memory snapshots were taken
after that measurement window, so snapshot capture does not inflate its peak.
An initial baseline pilot had a different preloaded Editor asset cache and was
excluded; the reported pair is before-fresh-02 / after-fresh-02.

| Startup measurement | Before | After | Difference |
| --- | ---: | ---: | ---: |
| Profiler Total Used Memory peak, including Editor (MiB) | 2,321.560 | 2,063.208 | -258.353 (-11.13%) |
| Unity allocated peak (MiB) | 1,515.791 | 1,299.436 | -216.356 |
| Profiler's own used-memory peak (MiB) | 137.937 | 134.936 | -3.002 |
| BeforeSceneLoad to Main data ready (seconds) | 4.411 | 2.777 | -1.635 (-37.05%) |
| AssetCache.GetAll inclusive time, 149 calls (ms) | 2,278.849 | 14.726 | -2,264.123 |
| Loaded character-part assets after startup | 198 | 7 | -191 |
| Loaded sprites after startup | 61,255 | 6,702 | -54,553 |
| Loaded textures after startup | 1,575 | 1,493 | -82 |
| Audio clip memory peak (MiB) | 23.584 | 0.201 | -23.383 |
| Texture memory peak (MiB) | 619.270 | 588.678 | -30.593 |

The CPU trace places the major removed work in synchronous bulk resource loading
inside AssetCache.GetAll. Before optimization, CauldronManager.Awake alone takes
1,379.663 ms inclusive. LoadSceneOperation.CompleteAwakeSequence falls from
2,160.184 to 533.642 ms inclusive. These nested timings overlap; they must not be
summed or treated as separate additive savings. The object counts independently
confirm that startup no longer brings in almost the entire character wardrobe.

The figures are Windows Editor measurements with Editor, graphics and profiler
overhead. They are not standalone iOS resident footprint, and crossing 2 GiB in
this Editor trace is not a reproduction of the phone's Jetsam event. The main
runtime startup interval excludes Editor domain reload. No overall FPS gain is
claimed from a startup-only trace. Two matching UnityEditor.Search indexing
exceptions occurred in the cloned project; no game exception was observed.

Retained local evidence under `Artifacts/StartupComparison/` includes
`matched-results.json`, the comparison chart `matched-startup-memory.png`, the
observer and analysis sources, and per-run raw CPU profiles, memory-counter CSVs,
50 ms external process samples, object inventories, completion records and full
post-startup `.snap` files. The matched raw profiles are 369,691,590 and
345,237,902 bytes; snapshots are 462,283,120 and 431,495,831 bytes respectively.
The Memory Profiler package was installed only in the disposable analysis Editor
after all four captures; it was not part of either measurement configuration.
Native snapshots are retained for allocation/reference inspection; their file
sizes are not presented as runtime memory savings. Physical iPhone 13 mini
validation of the updated build remains the required hardware check.

## Standalone player attribution and lazy enemy appearances (20 October 2026)

The next pass uses actual standalone players, not Editor totals. Windows controls
are Unity 6000.6.0f1, IL2CPP Release compiler configuration with an explicitly
Development player for snapshots. Baseline runtime is 9632d780d; the animal
candidate is 41ef12454. Both use the same compiled observer, isolated company/
product/save roots and fresh PlayerPrefs, Steam disabled for isolation, identical
startup/normal character confirmation/Farmlands timings, and 100 ms Unity plus
50 ms external process sampling. The requested 1080 x 2340 window is clamped by
Windows to 1080 x 2160 in both captures; that actual viewport is recorded.
Snapshots are taken after the steady measurements. The Memory Profiler analysis
package was installed only after baseline capture and removed before candidate
build, so it is not part of either measured player. Raw files are retained under
`Artifacts/StandaloneMemory/{baseline-dev-01,animal-lazy-dev-01}/`.

The player snapshot identifies additional memory beyond artwork: font source
objects total 34.273 MiB, native Font Engine/Layout Tables total 81,770,376 bytes
(~78 MiB) after map play, and AsyncUploadManager retains 16,824,960 bytes. These
root/native-object/GPU categories overlap and must not be summed as a process
footprint. Per-panel ownership does not support a large UI-image atlas leak:
the creator has one 64 x 32 atlas, and it is released after confirmation. Thus
panel sharing is not adopted for hypothetical atlas savings.

Farm enemy prefabs did retain all 52 animal appearance libraries before any
adventure. Their references are now lazy Resources paths; only the selected
library is resolved, with direct references still supported for existing/custom
prefabs. All 52 original library files and metadata are exact renames with the
same GUIDs. Original image bytes, sprite bindings, geometry, texture imports,
shaders, audio and save schema are unchanged. The authored order and Sheep's
regular/weighted section probabilities remain intact.

| Windows player steady inventory | Before | Animal candidate |
| --- | ---: | ---: |
| Creator Texture2D allocations (MiB) | 149.175 | 124.655 |
| Creator sprite count | 6,703 | 4,958 |
| Town Texture2D allocations (MiB) | 146.549 | 122.091 |
| Town sprite count | 6,562 | 4,817 |
| Farmlands Texture2D allocations (MiB) | 148.680 | 127.851 |
| Farmlands sprite count | 6,685 | 5,225 |

The clear startup/town texture saving is about 24.5 MiB. Farmlands retains the
skins actually selected by that run; world RNG is not forced in these memory
captures, so it is not a deterministic whole-game timing comparison. Unity
allocated CPU memory is only about 2-3 MiB lower while reported graphics memory
is about 24 MiB lower in town; Windows has dedicated VRAM. Neither is described
as the physical iPhone saving. Full-run external peaks include snapshot overhead
and are retained for diagnostics, not used as the optimization result.

Native iOS Simulator fresh-creator inventories independently show animal textures
52 to 5, 25.8972 to 2.4357 MiB. Total Texture2D allocations change from
166,117,028 to 141,515,876 bytes. The fifth retained appearance is a separate
Cow_04 reference; no visible/default decoration is removed. Native process and
map measurements remain separately attributable to the Mac run, rather than
extrapolated from Windows GPU counters.

The four prefab-dependency regression cases fail on the original source for the
intended reason (16/18/9/9 libraries), then pass with one base library each and
all alternate libraries resolvable with usable animation sprites. An independent
probe of the original Enemy.InitForSpawn entry point captured 1,024 selected
library GUIDs and following random values. The candidate matches all 1,024,
including authored selection, uniform, mixed and weighted-only branches and
Resources-based weighted entries. Evidence includes `enemy-memory-before.xml`,
`enemy-memory-after.xml`, `enemy-selection-{before,after}.csv`, the migration
byte/GUID manifest and `animal-lazy-comparison.json`.

A non-development Windows confirmation and bounded native font-face/upload-buffer
lifetime experiments are underway. No font features, glyphs or quality settings
are removed on the strength of the category totals alone. The physical iPhone
13 mini still requires validation; the earlier 2,098 MiB Jetsam is not equated
to a development-player or Simulator total.

## Matched original standalone control and release confirmation

The Mac reran original source 8a6bd24c473d5a109324707a5168b9c481db48cd
with the same observer, isolated fresh save, normal character confirmation and
100 ms external sampling as candidate 41ef12454. Through Ready + 35 seconds,
the kernel lifetime footprint peak falls from 751,308,736 bytes (716.50 MiB)
to 578,473,776 bytes (551.68 MiB): 164.82 MiB, or 23.0%. This is a matched
native iOS Simulator host measurement, not the physical iPhone's footprint.
The original retains 198 character-part definitions and 61,253 sprites until
normal character confirmation; no transient unload before Ready was observed.
After confirmation it drops to seven parts. The original engine-reported
creator Texture2D size is 1,629,316,950 bytes, while the host footprint remains
about 716 MiB. Those counters have different accounting and must not be added.
The reason for the remaining difference from the physical 2,098 MiB Jetsam
has not been established. The original whole-run 815.72 MiB peak includes later
map and snapshot work and is not substituted for the startup window.

Native evidence is retained on the Mac under
`/Users/matthewrushworth/Documents/Codex/2026-10-19/task/original-native-20261020/evidence/`
and the exact comparison under
`/Users/matthewrushworth/Documents/Codex/2026-10-19/task/native-retention-20261020/evidence/matched-original-current-startup.json`.

A separate Windows IL2CPP standalone build explicitly uses BuildOptions.None
and confirms Debug.isDebugBuild=false at runtime. It runs the same fresh creator,
normal confirmation, town and Farmlands sequence. Three sequential runs of this
identical binary test upload-buffer persistence true / false / true, without any
production setting change. All measured steady windows have a wall-frame median
around 8.3334 ms and p95 around 8.3418 ms, at this desktop's 120 Hz cap. These
are capped frame intervals, not an uncapped CPU benchmark or a phone FPS claim.
The first control's cold startup differs from the later warm runs, and map
encounters are not forced to identical RNG. Release profiling APIs report
incomplete object sizes and zero graphics-driver memory, so their totals are
not compared to the Development-player totals as an optimization result.

The second true control drifts from the first (Farmlands allocated medians
986.41 to 968.94 MiB); the intervening false run is 964.25 MiB. Therefore those
aggregate counters alone do not establish an exact 16 MiB upload-buffer saving.
The native owner-level true / false / true test remains the decision gate.
`release-buffer-comparison.json` records exact windows and sample counts.

An observer-only end-of-frame FontEngine.UnloadAllFontFaces experiment returns
Success and allocated bytes fall from 1,011,156,223 to 901,513,926 after five
seconds (104.56 MiB). Cached rendering continues, but that does not establish
new-glyph, text-input or CJK shaping safety. Native new-glyph/locale/panel-reentry
checks are required before adoption. No font features, source fonts, fallback
coverage or font-face lifetime policy have been changed in production. A
read-only comparison also finds that baked feature tables do not cover all
source ligature/mark records, so blindly disabling feature discovery is rejected.

The non-development snapshot request never calls back, although Update/rendering
continue; the bounded observer exits at 240 seconds. This is an unsupported
capture path in this configuration, not reported as a game hang. Both matched
Development-player snapshots remain the allocation evidence. All owned Windows
players and their process samplers exited; the user's Editor and saves remain
untouched. Local artifacts include release environment/stage/frame/process CSVs,
command-result.json and the observer sources under Artifacts/StandaloneMemory.

## Release idle upload buffers (adopted)

The native true / false / true control isolates AsyncUploadManager allocation:
16,822,080 -> 44,824 -> 16,822,080 bytes in the settled map snapshot. This is
16 MiB of retained upload capacity reclaimed, independently of aggregate process
counter drift. Set asyncUploadPersistentBuffer=false in all six quality presets;
retain the existing 16 MiB upload capacity, time slice, render scale, textures,
lighting, HDR and required depth/stencil. The setting allows the upload buffer
to be released when idle rather than permanently retaining its high-water size.
The open Editor confirms false at quality level five with capacity still 16 MiB.

Matching 45-second capture-free Spooky windows have 675 timing samples each
(every fourth frame, Simulator capped at 60 Hz):

| Native timing (ms) | True control | False candidate | True repeat |
| --- | ---: | ---: | ---: |
| Main median / p95 | 3.478 / 4.321 | 3.446 / 4.428 | 3.490 / 4.398 |
| Render median / p95 | 1.489 / 2.208 | 1.517 / 2.069 | 1.434 / 2.128 |
| GPU median / p95 | 0.605 / 0.958 | 0.597 / 0.898 | 0.597 / 0.936 |
| Wall median / p95 | 16.667 / 17.879 | 16.665 / 18.103 | 16.667 / 16.713 |

No sampled interval exceeds 25 ms in those windows, and no sustained slowdown
is observed with false. This does not establish a physical-device FPS gain or
certify every individual upload spike. Resolving all 52 animal libraries succeeds
in all three runs, but the unmarked tail has only 14/15/14 timing samples and no
post-resolution allocation-owner snapshot, so it is not treated as a rigorous
upload-stutter benchmark. Exact immediate release time is also not established;
reclamation is proven by the settled map capture. Native evidence:
`native-retention-20261020/evidence/cache-experiment/upload-performance-gate.json`.
Windows true / false / true results separately show no steady capped-frame
regression, with their aggregate-memory drift retained rather than hidden.

All nine native experimental snapshots completed analysis. The corrected Windows
baseline and candidate snapshots also complete full PostProcess and clean disposal;
the earlier baseline reader-only disposal warning was an analysis-tool issue,
not an exception in either player. Font-face cleanup remains separate: native
Layout Tables fall from 81,770,376 bytes to eight, but immediate process-footprint
reduction is about 14 MiB. No production font change is adopted without corrected
visible-text, new-glyph and locale/panel-reentry comparisons.

## Font-cache trial: pixel-safe, rejected for an in-run hitch

The corrected native font controls use an opaque, visibly rendered 1080 x 1080
fixture. Cached text, genuinely new Latin/SC/JP text and a TextField value, ja /
zh-CN / en / ja reentry, and panel reconstruction all have zero changed pixels
between control and unload runs. Setup-to-new-text changes 24,682 pixels in both
runs, and dark-pixel counts rise from 16,038 to 20,358: this is a positive
visibility check, not equality between blank/covered panels. Serialized font
feature hashes and getFontFeatures=true remain unchanged. Legacy character-table
inspection is ATG-limited and is not used as glyph-count proof; the TextField
check uses its value API, not OS IME input or exhaustive script coverage.

All six snapshots parse and dispose cleanly. Layout Tables remain at eight bytes
in the unload run after the new text, locale reentry and panel recreation; the
control retains about 81,776,520 bytes. The immediate native footprint reduction
in this corrected capture is about 9.67 MiB, separately from the ~78 MiB root
allocation reduction. Readback/snapshot history perturbs totals. Evidence:
`native-retention-20261020/evidence/font-verify/{summary.json,pixel-comparison.json,reader-results.json}`.

A concrete one-time production integration was then trialled in e7b3439dc: an
independent coroutine waits 30 seconds after the first map/HUD initializes, then
calls the public API at end of frame, with an exact profiler marker and internal
diagnostic clock. The scheduled call takes 29.498 ms natively and produces a
34.643 ms next-frame wall interval; the corresponding delayed main-thread timing
is 34.442 ms. The clean same-binary control tops out at 18.319 ms. Every-frame
records and a capture-free call window confirm this is not a missed sampled
frame or PNG/snapshot overhead. Windows independently records 12.1532 ms for the
same scheduled marker. Therefore the in-run one-shot is rejected and removed;
no font-cache flushing policy remains in production. The original source fonts,
features and rendering caches remain. Any future cleanup at a loading boundary
would require its own integration timing validation rather than reusing the
pixel-only result. The lazy appearance and idle upload-buffer changes are kept.
