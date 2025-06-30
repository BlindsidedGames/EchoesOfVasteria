---
title: Export Options
---
# Export options

The export options contains every setting that'll affect the output of the exported tiles, and is also the place when you can export them.

![[The editor/images/export-options.png]]

**Grid type** - This is the place where you can change the type of the [[Grid]]. You can choose between square (which is the default one), isometric, and two types of hexagonal grids. This setting can be changed even if you already have tiles on the grid, but it'll relocate the grid cells, so it's better to change this setting before you do anything in the editor. This setting also affects some default values when creating a new tile, as different tilemaps require different shaped tiles.

**Simplify rules** - This option affects how the rules will be generated. If this option is enabled, every [[Tiling rule]] that has the same output sprite will be combined into a single tiling rule. It'll compare every of those tiling rules, and create one tiling rule that's true for all of those scenarios. This will prevent unnecessary duplicate tiling rules. For this option to work as you'd like, you need to give it as much samples as possible, so it can filter through every scenario and choose the one that applies to all. You can look at he [[Examples]] provided with the package to see this option in action.

![[The editor/images/simplify-rules.png]]

**Universal sprite settings** - If this option is disabled, you can use the [[Grid cell inspector]] tool to inspect a grid cell, and change it's properties like output sprite, collider, animation and settings like this. But this means that if you have multiple tiling rules with the same sprite, those settings will not match. If universal sprite settings is enabled, these options from the [[Grid cell inspector]] will move to the [[Sprite override settings]]. In this window you can change the sprite output settings per sprite and not per tiling rule. If you change a sprites' settings in this override settings window, every tiling rule that has the same sprite output will use the same sprite settings. You can open the [[Sprite override settings]] from the toolbar.

**Generate tiles** - Pressing this button will start the export process. After the process is done, the tiles will be saved to the container asset and the asset will be highlighted in the project window. The generated tiles will be saved into the container in order to keep them synced, and if you modify them and export them again, it will update the already existing tiles instead of creating new ones.