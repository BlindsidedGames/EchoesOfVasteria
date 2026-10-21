# Pixel Relics: Echoes of Vasteria achievement style

Approved 21 October 2026 after three art passes and focused mobile review. Use this direction for new achievements and revisions unless the user asks to change it.

## Approved references

- [Full Steam set](../assets/pixel-relics-steam.png)
- [Full mobile set](../assets/pixel-relics-mobile.png)
- Working masters and platform exports: `docs/planning/steam-achievement-icons/pixel-relics-v3/` from the repository root.

## Rendering and composition

Use chunky, readable fantasy pixel art. Steam has a deliberate 64 × 64 native grid, exported to 256 × 256 with nearest-neighbour enlargement. Build solid pixel clusters, a dark outline, restrained highlights, and two or three principal shading steps per material. Avoid antialiasing, smooth gradients on subjects, hairline detail, stray bright pixels, text, and numbers.

Centre one strong subject or tightly grouped motif. Keep its silhouette readable at 64 pixels and test at 32 pixels. Use a dark plum background, warm copper square frame and small stepped corner details for Steam. Metals use warm copper/gold or cool silver; parchment is cream and ochre; leather is brown; foliage is green; magic uses cyan spirits and violet accents. These are colour relationships, not a mandatory global palette quantization step: do not flatten material distinctions while cleaning an image.

Light generally comes from the upper left. Preserve established character identity, eye treatment, costume, and prop geometry across related icons. A sword's blade, guard, grip, and pommel must share one continuous axis, even when partly occluded. Bands should wrap the object's volume rather than float in front of it.

## Families and progression

| Family | Stable motif | Progression |
|---|---|---|
| Characters | Recognizable portrait and profession prop | Keep approved face, costume, and eye shapes |
| Distance | Parchment map, dotted trail, brown mountains with white peaks | One peak, then larger groups of matching peaks |
| Travel | Boots and a winding trail | More substantial footwear and journey detail |
| Tasks | Leather pack, scrolls, green completion mark | More tools and supplies without losing the pack silhouette |
| Buffs | Brown-haired hero in green, cyan spirit echoes, violet magic | One through five echoes; consistent human eyes |
| Enemies | Green slimes, steel swords, yellow impact accents | Groups, crowned green enemy, then crowned purple king with small green enemies; avoid replacing group milestones with a lone enemy |
| Eva | Cauldron with violet vapour | Increasing ornament, gold reinforcement, gem detail |
| Ivan | Anvil, hammer, forged blade | Copper then silver then richer gold bands; level 500 blade points left and grip sits right |
| Resets | Weathered gravestone with plants and repairs | Branching cracks, chipped stone, greater damage and repairs; do not polish away its age |

Higher tiers should be visibly richer, but do not confuse detail reduction with removing the meaning of the achievement. Preserve the final group compositions and cracked gravestones in the approved references.

## Mobile identity

Use the same subject pixels as Steam wherever possible. Remove the copper square frame, including connected corner remnants, and add a dark plum margin for the circular platform crop. The established exporter crops the 64-pixel source at `(4,4,60,60)`, clears connected frame remnants within the outer seven pixels, and centres the 56-pixel payload on an 80 × 80 canvas at `(12,12)`.

The sole approved exception is `Buffs1` (cast 10 buffs): its square crop ended with a visibly flat cloak and spirit. Use its mobile override with a short, naturally finished lower silhouette. Do not apply that redesign to Steam or redraw the other 37 mobile icons. For future square-to-circle conversions, outpaint only when framing exposes a visibly unfinished subject; preserve the rest of the artwork.

Export opaque sRGB PNGs: Steam 256 × 256, Google 512 × 512, Apple 1024 × 1024. The current mobile exporter uses nearest-neighbour sampling from the 80-pixel composition; these export ratios are not integer multiples. Inspect the actual exports and circular previews instead of claiming integer scaling for mobile. Recheck official platform requirements when delivering again.

## Revision and handoff

Use stable game IDs, not guessed thresholds from filenames. `Reached1000` means 500 distance, `Reached10000` means 1,000 distance, and `Tasks5k` currently means 1,000 tasks. Verify against the platform mapping before uploads. Preserve requirements, names, points, localization, and unlock state unless separately requested.

For AI-assisted edits, supply the exact existing icon and a narrowly scoped edit prompt. Inspect the generated master at native size, then inspect exported Steam and circular mobile previews. Keep original artwork until the replacement is approved. Upload only the authorized platform assets and verify saved/published state; Apple review submission is distinct from approval and live availability.
