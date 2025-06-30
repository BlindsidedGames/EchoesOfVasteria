---
title: Tile Drawer
---
# Tile drawer

The tile drawer is the place where you can create and select [[Tile]]s to use. On top of that, this is also the place where you can see the currently selected tile or sprite which you can use to draw with the [[Brush]] tool.

![[tile-drawer.png]]

To create a tile just simply click on the "**Add Tile**" button in the drawer. The settings of the tiles you create can be changed in the [[Tile inspector]] window. When selecting a tile from the drawer which can be edited, the tile inspector will automatically show up in the bottom left corner of the editor window. 

# Rules

Next to the tiles you create, there are a few default **rules** which you will need to use to create [[Tiling rule]]s that will determine how the tile will behave when placing it on the tilemap. These default rules are:

- **Delete** - It's a rule that removes every other rule or tile. This is the tile you need to use to erase tiles from the grid using the [[Brush]] tool.
- **Empty** - When this rule is placed, the rule passes if there's no tile in this position.
- **Not same** - When this rule is place, the rule passes if the tile at this position is not the same as the tile which is checking.
- **Any** - This rule will pass if there's any tile placed at this position.

> [!hint]
> You can read more about how rules work on the [[Tiling rule]] page.