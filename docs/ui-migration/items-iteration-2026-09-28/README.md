# Items visual iteration — 28 September 2026

Original reference: `../baseline-2026-09-27/captures/40-tabs-6-Statistics_Window-Buttons-itemsButton.png` (fresh profile). The current two-column layout retains the original content and five sorting modes; it falls back to one column below 560 reference units of content width.

Removed duplicate nested padding and hardcoded compressed line-height tags. Rows now share the Tasks/Enemies spacing, subtle separators, bold names and quiet IDs. Art is centered at native sprite.rect * 16 / pixelsPerUnit size in a full-inner-frame mask. Tier-coloured frame borders replace the old pixel-frame sprites; tier labels, bonuses, counts, collected/spent totals, minimum distance, crafted state and AE power remain unchanged. Sorting only repositions rows when needed; no measured performance benefit is claimed.

`1280-items-final.png` is the actual isolated developed-profile Editor capture. All five sort callbacks, original text/value parity for every item, native PPU, full mask bounds and smaller landscape row bounds passed (`checks-items.txt`). Unknown presentation was checked using a disposable clone, without modifying the original resource asset. The phone-check image is 844x390 despite the inherited 1280 filename prefix. Physical touch input and translations were not tested.

Captures used timeScale 0 and a 30 FPS cap. Save/network/feedback guards were restored byte-for-byte (`restoration.json`), normal project identity restored, and Play mode stopped. Harnesses and disposable saves remain ignored under Library.

## Full Items cleanup

`polish-items-final.png` supersedes the initial and no-distance captures above. Minimum distance and its unused task/enemy scan are removed from Items only. Titles now show a Cauldron-style tier star and number, with neutral artwork frames; crafted entries retain their Crafted label. Current stock is prominent below the name. Collected, Spent and ID share the quieter numbers grid; Alter Echo power is written out and tier bonuses remain visible. Number formatting respects the selected notation while trimming insignificant trailing decimal zeroes. Native sprite PPU and the two-column/one-column breakpoint remain unchanged.

`checks-polish.txt` records five sort callbacks, displayed values, ID placement, plain titles, current counts, unknown presentation, native PPU, full mask bounds and smaller landscape bounds. All checks passed. Desktop and 844x390 landscape captures were reviewed; touch and translations were not tested. The temporary save/network guards were restored byte-for-byte in `restoration-polish.json`, and the Editor was left outside Play mode with normal project identity.

Follow-up: ID moved from the numbers grid to the far right of the title row, with muted text and automatic left margin. The polish captures above predate this final placement adjustment.

`1280-items-hierarchy.png` is the updated runtime capture with right-aligned ID and a 7.5px regular-weight owned amount beneath the 8px bold title. Temporary isolation guards were restored byte-for-byte; Play mode stopped.
