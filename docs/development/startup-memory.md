# Startup memory

## Current result

The consolidated candidate uploaded in internal build 165, 9ca330359, has a sampled startup peak
of 583.504 MiB on the arm64 Release iPhone 13 mini Simulator (iOS 18.5), with
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
