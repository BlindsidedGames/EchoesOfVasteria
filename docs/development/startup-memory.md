# Startup memory

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
