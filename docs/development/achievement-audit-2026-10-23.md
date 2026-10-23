# Achievement audit — 23 October 2026

All 38 existing achievements remain applicable to the current game. Existing platform IDs and earned awards are retained. The source audit follows quest hand-ins/NPC introductions, map movement, completed tasks, successful buff casts, enemy deaths, hero deaths, Forge mastery and Cauldron progression. Farming changes do not invalidate any existing achievement.

## Repairs

- Mobile distance milestones now use the published 100/500/1,000 thresholds. The historical API names are retained; their numeric suffixes are not the current requirements.
- `Tasks5k` retains its ID and uses the published 1,000-task requirement.
- All five enemy-kill milestones are reported on mobile: 50/500/2,500/5,000/10,000.
- A five-second mobile reconciliation catches kills and town level changes without waiting for movement or another task. Immediate NPC/quest notifications and load/authentication reconciliation remain.
- A failed platform report is retried; an in-flight or successful report is deduplicated. Queuing before sign-in no longer counts as a successful unlock.
- No progress counters, saves, existing IDs or earned platform achievements are reset.

## Coverage

| ID | Current requirement |
| --- | --- |
| `MeetIvan` | Meet Ivan |
| `Mildred` | Complete Mildred |
| `Reached100` | 100 distance |
| `Reached1000` | 500 distance |
| `Reached10000` | 1000 distance |
| `Kilometers1` | 1 total kilometers |
| `Kilometers100` | 100 total kilometers |
| `Kilometers1000` | 1000 total kilometers |
| `Tasks100` | 100 tasks |
| `Tasks5k` | 1000 tasks |
| `Tasks25k` | 25000 tasks |
| `MeetBarkley` | Meet Barkley |
| `Buffs1` | 10 buff casts |
| `Buffs2` | 100 buff casts |
| `Buffs3` | 1000 buff casts |
| `Buffs4` | 2500 buff casts |
| `Buffs5` | 9001 buff casts |
| `Enemies1` | 50 enemy kills |
| `Enemies2` | 500 enemy kills |
| `Enemies3` | 2500 enemy kills |
| `Enemies4` | 5000 enemy kills |
| `Enemies5` | 10000 enemy kills |
| `MeetGill` | Complete The names Gill |
| `MeetFarmers` | Meet the Twins |
| `MeetOldTimer` | Meet the Old Timer |
| `EvaLevel10` | 10 Cauldron level |
| `EvaLevel50` | 50 Cauldron level |
| `EvaLevel100` | 100 Cauldron level |
| `EvaLevel200` | 200 Cauldron level |
| `EvaLevel500` | 500 Cauldron level |
| `IvanLevel10` | 10 Forge level |
| `IvanLevel50` | 50 Forge level |
| `IvanLevel100` | 100 Forge level |
| `IvanLevel500` | 500 Forge level |
| `IvanLevel1000` | 1000 Forge level |
| `Die10` | 10 deaths |
| `Die250` | 250 deaths |
| `Die1000` | 1000 deaths |

## Evidence and limits

The negative control against the prior milestone policy fails eight of 32 boundary cases: two distance, one task, and five kills. The remaining 24 pass. Each case checks below, at, and above its published requirement. After the repair all 32 cases pass. Current validation results and platform release receipts are retained in `Artifacts/ReleaseGoogle20261023`.

The published Steam descriptions were checked against the live achievement page and the retained platform-definition export. Mobile platform-ID mappings were checked against the protected EssentialKit configuration and the retained Google mapping. Native authentication, Google Play purchase/restore/consume behavior and achievement popups still require platform test accounts/devices; editor policy tests do not prove those services work.

The 38 approved 512px Google icons already have stable platform mappings. The retained 22 October console report records all 38 images uploaded but Play Games publication blocked by missing project description/category/icon/feature graphic. That is historical evidence; current Google publication state must be read from the console/API before claiming completion.
