---
title: Editor Settings
---
# Editor settings

The editor settings is the place where you can change the appearance and behavior of the [[Editor window]] and other windows inside the editor. These settings are separated into groups for better readability.

# Tile drawer

This group of settings changes the appearance of the [[Tile drawer]] window:
- **Tile drawer size** - this option changes the width of the tile drawer, the number specified is the amount of tiles visible at once in the drawer.

![[settings-tile-drawer.png]]

# Sprite drawer

These options change the appearance of the [[Sprite drawer]] window:
- **Sprite drawer height** - Changes the size of the buttons in the sprite drawer, the number specified is the width and height of a button in the sprite drawer.
- **Sprite drawer columns** - Specifies how many columns of sprites should be displayed in the sprite drawer, increasing this value will increase the width of the drawer.
- **Expanded drawer columns** - How many columns should be displayed when the sprite drawer is expanded.
- **Save sprite drawer** - If this option is enabled, the drawer will remember the sprites you added to it when you reopen the editor. If the option is disabled, when reopening it'll only show sprites that you've already placed on the grid. If you have a lot of sprites enabling this option could cause the editor to load slower.
- **Clear sprite drawer** - Clears every sprite from the drawer that is not used on the grid.
- **Add all sprites** - Clicking this will search for every sprite in the asset database and add all of them to the sprite drawer. This could slow down your editor if you have too many sprites in your project.

![[settings-sprite-drawer.png]]

# Grid

These options can change the look of the [[Grid]]:
- **Current zoom** - This value represents how much you're zoomed in to the grid, here you can set a zoom amount by hand, and even go outside the zoom limit.
- **Render small grid** - This option set whether the grid should keep rendering when zoomed out by a large amount, or just keep rendering. Setting this option to disabled will help better seeing the tiles when zoomed out, and will also help with performance so the grid doesn't need to render a bunch of lines.
- **Zoom threshold** - If the render small grid is disabled, the grid will stop rendering when the current zoom amount goes below this value.
- **Grid size** - Specifies the size of a grid cell at 1x zoom. With this option you can also change the shape of the tile.
- **Grid cell offset** - If your tiles do not show perfectly on the grid, but between two grid cells, you can change the offset with this option.

![[settings-grid.png]]

# Locked cells

These options change the behavior of the [[Lock selection]] feature:
- **Show locked overlay** - Adds a striped overlay on top of locked cells.
- **Show locked outline** - Displays an outline around individual locked cells.
- **Outline color** - The color of the outline of locked cells.

![[settings-locked-cells.png]]

