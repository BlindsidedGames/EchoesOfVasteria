---
title: Preset Blocks
---
# Preset blocks

Preset blocks provide a way to easily create multi-tile structures. When placing tiles to the tilemap in the same layout as a preset block, the preset block will be used as the output of the tile, instead of the other rules. You can look at the [[Examples]] provided with the package to see them in action.

![[preset-blocks-showcase.png]]

To create a preset block, just simply select the area with the [[Selection tool]] and press the **create preset block** button in the [[Toolbar]] or by using [[Keyboard shortcuts]] (P). Preset blocks will be displayed with an orange outline.

![[create-preset-block.png]]

Preset blocks cannot overlap, if you try to add a preset block over an already existing one, it'll ask you whether you want to add this selection to the current preset block or not add it. If the selection is overlapping two already existing preset blocks, you wont be able to add a new one.

# Preset block settings

To change settings of the preset block or remove that, you have to use the [[Grid cell inspector]] tool. When selecting a cell inside a preset block the inspector window will show the preset block options instead of the **Neighbor options**. Here you can change: 
- If the preset block can be rotated or mirrored
- Delete the preset block