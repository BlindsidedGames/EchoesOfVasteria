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
Loaded `CharacterPartFrames` stayed at zero throughout. All 214 texture overrides
were applied; running the import policy again required zero changes.

When adding a data category, use an explicit Resources path or register its
folder in `AssetCache`. Avoid a root search, even when supplying a type filter.
Diagnostic builds record both the beginning and successful end of each real
category load; cache hits no longer replace the operation breadcrumb.

## iOS character textures

`CharacterTextureImportPolicy` sets the modular character sheets to ASTC 4x4
with high-quality encoding on iOS. It preserves the effective maximum size,
sprite IDs, slicing, point filtering and other platform settings. The policy
also applies to newly generated hair and gear colours. Existing imports can
be updated with **Tools > Characters > Apply iOS Texture Compression**.
The check runs when rebuilding the character catalogue and before iOS builds,
rather than through a global texture postprocessor that would invalidate
unrelated imports whenever editor scripts change.

The 214 source PNGs contain approximately 1,437 MiB of RGBA32 pixel data in
total. ASTC 4x4 stores eight bits per pixel instead of 32, but only the sheets
actually resident in memory contribute to runtime savings. Do not describe
the entire source total as measured resident memory or claim a measured
device saving from the compression ratio.

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

## Device verification

Use a new internal build on the affected iPhone 13 mini. Run normal startup,
record native peak footprint and minimum headroom, and verify the game reaches
town. Check character creation, equipped armour, tools and transparent echoes
for colour/alpha artifacts. Test entering maps and returning to town as well
as launch, so that a lower startup peak does not hide a later loading spike.

The loading correction is independently useful if compression needs visual
adjustments. Further work should be based on measured resident allocations;
do not add forced garbage collections or repeated unused-asset unloads to
mask assets retained by broad searches.
