---
title: Export Tiles
---
# Exporting Tiles

After you've created your [[Tile]]s and set up your [[Tiling rule]]s, you can head to the [[Export options]] to export your tiles into tile assets you can use in the game.

![[The editor/images/export-options.png]]

You can export your tiles by clicking the `Generate tiles` button in [[Export options]]. This will start the export process, which will take some time depending on how complex are your tiles. Usually only takes a few seconds.

![[exported-tiles.png]]

After it's done, the exported tiles will be added to the [[Better rule tile container]]. After this it's the same process as with any other tile. Add them to a `tile palette` to draw with them inside the scene view.

> [!warning]
> The exported tiles are all managed by the editor, so you cannot edit, rename or delete them in the exported form. In order to do so you have to do it in the [[Editor window]], than export them again to see your changes take effect.

> [!info]
> Exporting the tiles again will update the already existing tiles, so any changes you do in the editor will show in the tiles already placed (you might need to restart the scene or reload the tilemap).

> [!caution]
> Deleting a tile from the [[Tile drawer]] that you've already placed in your scene and exporting it will result in the old exported tile getting deleted as well, which cannot be recovered!
