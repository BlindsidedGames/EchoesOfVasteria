# General statistics iteration - 28 September 2026

Original reference: `../baseline-2026-09-27/captures/40-tabs-1-Statistics_Window-Buttons-generalButton.png`.

Restored all five map artwork sprites from the original scene references into Statistics.asset. Artwork uses native sprite dimensions at 16 / PPU with centered full-frame masking. Overall retains the hero portrait. Each row has the existing two columns of statistics, separated labels and right-aligned values, a bold map name and a subtle separator. Existing statistics formulas and number formatting remain unchanged, including Most Kills for Spooky and reaping statistics in Overall. Shared spacing tokens replace the previous bespoke inset and offsets. No nested decorative panels.

`1280-general-final.png` is the final 1280x720 capture. `1280-general-phone-check.png` is actually 844x390. All labels and values matched the existing presentation output, all six rows and map artwork were present, native PPU checks passed, and label/value bounds did not overlap in the smaller landscape layout. See checks-general.txt. Smaller-screen text legibility, portrait layout, physical touch and translations remain unverified.

Used a disposable developed profile, offline guards, timeScale 0, VSync off and a 30 FPS cap. Restored temporary source guards byte-for-byte and normal project identity, and stopped Play mode. See restoration.json. No real saves or cloud progression were changed. Harnesses and disposable saves remain ignored under Library.
