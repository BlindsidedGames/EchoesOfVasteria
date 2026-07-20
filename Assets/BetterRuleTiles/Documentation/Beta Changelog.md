---
title: Changelog (Beta)
---
## Version -b

This page shows more detailed changelogs of past beta releases, if you're looking for the release changelogs [[Changelog|you can head over here]].

Once a version goes out of beta, the changelog of the beta version will move over to this page, and the main release will get it's own combined changelog.

---
## Version 1.5.0-release

- Changed the blocked overlay of flat top hexagons so the lines line up
- Implemented [Issue #26](https://github.com/Vinark117/BetterRuleTiles-Support/issues/26): Added a new "Any Other" tile that can be accessed by selecting the **ExtendedBetterRuleTile** rule tile script in the export settings
- Fixed the small gap between the **Neighbor positions** and the **Rule priority** in the **Grid cell inspector tool**, when the grid type is set to isometric
- Importing textures with a single sprite will not bring up the **sprite sheet import window** anymore
- Font size of tiles in the **tile drawer** will now adjust to fit longer names
- Fixed issue where the **sprite drawer** scrollbar would overlap with the buttons
- Updated documentation included with the tool
- Made sure project can compile with the tool included
- Updated README.md and CHANGELOG.md
- Fixed [Issue #30](https://github.com/Vinark117/BetterRuleTiles-Support/issues/30): FindObjectOfType is obsolete

## Version 1.5.0-b11

- Also applied the sprite scaling fix from version 1.5.0-b9 to the tile previews
- Fixed default hexagonal tile proportions
- Fixed locked tile overlays on the isometric and hexagonal grids

## Version 1.5.0-b10

- Creating a new preset block that overlaps two or more other preset blocks will now give you an option to merge them into one
- Fixed [Issue #18](https://github.com/Vinark117/BetterRuleTiles-Support/issues/18) - Preset block does not move with selection
- If a preset block is not fully contained in the selected area, moving the selection will split the block into two separate blocks. Moving the selection back will not merge the blocks automatically.

## Version 1.5.0-b9

- Fixed [Issue #17](https://github.com/Vinark117/BetterRuleTiles-Support/issues/17) - Sprite does not scale properly when rendering mode is tight
- Added a warning message when the user is trying to import sprites that are part of a Sprite atlas

## Version 1.5.0-b8

- User made tiles derived from `BetterRuleTileBase` and `BetterHexagonalRuleTileBase` will now show up in the BRT Editor and can be used without modifying the tool
- Brush preview will now show the correct anchored position of the sprites
- The selected sprite preview in the tile drawer is no longer stretched when selecting a non-square image
- Removed options from **sprite drawer settings**:
	- Removed deprecated **Save sprites** toggle
	- Removed deprecated **Clear sprite drawer** button
- Added options to the **sprite drawer settings**:
	- Added a **Clear sprite cache** button, which removes unused sprites and reimports used sprites from the sprite cache. This button acts as a replacement to the **clear sprite drawer** button
	- Added a **Reimport sprites** button, which reimports all sprites currently in the sprite cache
- Added drag and drop functionality to the following places:
	- Drop sprites and textures to the **sprites** field in the **grid cell inspector**
	- Drop sprites and textures to the **sprites** field in the **universal sprite settings window**
	- Drop sprites and textures to the **sprites** field in the **output tab** of the **tileset editor**

## Version 1.5.0-b7

- Fixed an error where a missing sprite reference caused the sprite drawer to break the editor
- **Import window** now works when not using **universal sprite settings**
- Padding on sprite sheets is now correctly handled in the **import window**
- You can now drag and drop multiple textures to the BRT Editor at the same time
- Importing sprite sheets with padding between the sprites will now not leave gaps on the grid

## Version 1.5.0-b6

- Fixed an issue where the selection preview created by the **grid cell inspector** was offset if the move tool was used before.
- Fixed an issue where creating a **BRT Container asset** would fail if any asset was selected in the project window
- Adjusted tooltips within the **rule priority** cell inspector group
- Slightly decreased the height of the **grid cell inspector window**
- The **bulk cell editor** now appears even if a cell is currently inspected.
- The **bulk cell editor** now appears above the **grid cell inspector window**
- The **bulk cell editor** can now be collapsed
- The **bulk cell editor** can now be moved around if the windows are not locked
- Refactored some code

## Version 1.5.0-b5

- Fixed broken Changelog file from v1.5.0-b4
- Fixed issue where if you had a neighbor rule with **Pattern** output, it'd always get prioritized if the rules match.
- Changes to rule sorting and priority:
	- Added the ability for the **priority modifier** to also decrease (instead of just increasing) the priority of the rules
	- Renamed the `Priority modifier` option to `Rule order modifier`
	- Added an information text to the grid cell inspector window that tells you the index of the tiling rule in the exported tile. It also shows how the **rule order modifier** affects the priority of the rule.
	- Added a `Priority group` option to the **rule priority** section of the the **grid cell inspector**. This will be the primary sorting option for rules, and will fall back to the already established sorting method for rules within the same group (all tiles by default).
	- The **rule order modifier** option will only affect rules withing the same **priority group**
- Changes to the **grid cell inspector tool**:
	- Selecting a grid cell with the **grid cell inspector tool** will now unfocus any GUI elements, so values inside the **cell info window** won't stay selected and show incorrect values.
	- You can now select an area while using the **grid cell inspector tool**, so you don't have to switch tools when you need a selection for editing the grid cell.
	- If the **grid cell inspector tool** is active, and there is an active selection without a cell to inspect, a new **bulk cell editor** window will appear in the place if the **cell inspector**. Using this you can change the priority group of all tiles inside a selection.

## Version 1.5.0-b4

- Updated tooltips and names in the `Editor Settings` window
- Added a new **Tile size** option in the `Editor Settings` window, which lets you change the rendering size of all tiles on the grid without affecting the sprites.
- The **Sprite anchor** (formerly **Tile anchor**) setting now only affects the anchoring position of sprites
- Fixed a small issue with the **Highlight Cell** function

## Version 1.5.0-b3

- Updated the tileset tab selector, removed text and added icons to it.
- Fixed "Spritesheets" text location in the tileset sidebar.
- Updated package.json:
	- Changed version from **2019.4** to **2022.3** to reflect the development version.
	- Changed the documentation url to point to the beta docs.
	- Added the new sample to the list of samples.

## Version 1.5.0-b2

- Added a new sample for the **Tilesets**.
- Added a new **CHANGELOG.md** file to the package folder, which contains the up-to date changes for beta releases.
- Removed the tile palette assets from the samples to not clutter the selection of tile palettes for the user.

## Version 1.5.0-b1

#### An entire new way of creating tiles was added to the tool, the **Tileset Editor**:
- The **TileSet Editor** is a an entirely new editor window, created to speed up the creation even more of simpler rule tiles.
- ***NOTE: The Tileset Editor only works with square tiles***
- You can access this new editor by creating a **Tileset Container**. You can do this by right clicking in the project window (Or clicking the `Assets` menu item) and navigating to either `Create/2D/Tiles/Tileset Container` or to `Tools/Better Rule Tiles/Create Tileset Container` in the menu bar. Open this file to show the new editor.
- To get started with the **Tileset Editor**, you need to import a sprite sheet. You can do this by drag and dropping it to the left panel of the editor or by clicking the import button in the bottom right of the panel. 
- It's important to note that this editor was designed to work with sliced sprite sheets, using individual sprites is possible but it'd defeat the purpose and ease of use of the editor.
- When selecting a sprite sheet, you can edit its rules and properties in the respective tabs of the main toolbar:
	- The **Draw** tab is the place where you'll define the neighbor rules of the sprites. You can create a tile by clicking the `Add new tile` button on the bottom of the tab. 
	  You'll see that the options are quite limited compared to the regular **BRT Editor**. Don't worry, you're still able to edit the other properties, more on that later. The two settings you can modify is the color of the tile and its name, you can do this by clicking on the color swatch and the name of the tile respectively. To select this tile for drawing, click the brush icon to its right.
	  Once you've selected a tile you can start drawing by left clicking on the sprite sheet. You can erase tiles by right clicking.
	  You'll notice that the neighbor rules are laid out a bit differently in this editor. The surrounding 3x3 grid of tiles got squished into the single cell of the sprite, this makes visualizing the connections more clear.
	- The **GameObject**, **Collision** and **Output** tabs will all show the same contents on the sidebar. Use left click to select the sprite to display. The contents are similar to the **Grid cell inspector** tool. You can assign gameobjects to the sprites, edit it's collider or change the output of the tile.
	- The **GameObject** tab will let you assign gameobjects to the tiles. Just drag and drop gameobjects to the sprites to assign them. Or right click to un-assign them.
	- In the **Collision** tab you can cycle through the collision types by right clicking on the sprites.
	- The **Output** tab will let you preview animations if your sprites are animated. You'll have to use the sidebar to assign sprites to the animation.
- The right side of the toolbar has two buttons, the left button is the **export button**, clicking this will export the tiles. Compared to the **BRT Editor**, this button will not display a dropdown, and will use the default options if you haven't modified them.
- The right-most button will switch to the **BRT Editor**:
	- Both editors use the same type of file, so you can easily switch between the two editors, if the file was created as a **Tileset Container**
	- When switching to the **BRT Editor**, you'll quickly see how the **Tileset Editor** functions and lays out the sprites and tiles. The area used by the **Tileset Editor** is highlighted in a dark red background color. **Editing this area is not restricted, however using it for manually creating rules is heavily discouraged**, as the tiles and sprites can be replaced by the **Tileset Editor**.
	- You are free to do anything else you'd able to do if it wasn't a **Tileset Container**, and any changes made here will affect the exported tiles, no matter which editor was used to export them.

#### Implemented scriptable rule tiles (Experimental)
- Adding new neighbor rules to tiles will now dynamically add those rules to the tile drawer in the editor.
- You can now change what rule tile script to use in the export dropdown.
- ***DISCLAIMER: As of this moment, adding custom rules requires to edit scripts inside the package, this means the changes you make will not be kept when updating. The code will also be changed in the future, so you'll have to edit the files with each update, creating a backup of the scripts and overwriting them when updating will lead to broken code once this feature is updated.***
- You can edit the following files to add your own rules. Or use these files as a template to create your own rule tile:
	- `BetterRuleTiles/Runtime/RuleTiles/BetterRuleTile.cs` - Square and isometric tiles
	- `BetterRuleTiles/Runtime/RuleTiles/BetterHexagonalRuleTile.cs` - Both types of hexagonal tiles
- If you wish to create a new type of tile, you have to add it to all 3 functions in the `BetterRuleTiles/Editor/Generation/ExtendedBetterRuleTileGenerator.cs` file.
	- The `CustomTileTypes` enum is responsible for selecting the tile in the export dropdown.
	- The `GetCustomTileType` function is responsible for translating the selected enum to the tile type.
	- The `GenerateCustomTiles` function gets called to generate tiles with the correct type.
- The textures are encoded into Base64 strings. You can open a converter utility by opening the menu item `Tools/Better Rule Tiles/Open TexToBase64 Converter`

#### Changed the workflow of importing textures:
- When drag & dropping an entire sprite sheet into the editor, a new window will appear instead of immediately importing the sprites. In this window you can select to import sprites as usual, or you can select the other modes to speed up the importing of animated, randomized or pattern tiles.
- This window is also the main way to import sprites into the **TileSet Editor**.

#### New features
- Sprites on the editor grid now respect the aspect ratio and the pivot point.
- When using the **Brush tool**, you can hold down the CTRL key to temporarily switch to the **Picker tool**
- You can now manually prioritize tiles by changing the new `Priority modifier` option in the **grid cell info window**.

#### Quality of life improvements
- The toolbar now expands to 4 lines if the editor window is too narrow
- Changed how the container assets get displayed in the project window
- Default sprites of tiles will now be automatically assigned if the user didn't specify it
- Changed the default 32x32 pixel **Grid size** option in the settings dropdown to a **Cell size** option with a default value of 1x1, to better match the built in Grid class.
- Exposed the tile object reference field in to container inspector so it is more accessible in case it is needed
- Increased maximum zoom amount in the grid
- The anchor of the editor grid can now be changed in the editor settings dropdown.
- Once you've exported a container, it'll not let you change the editor grid type.
- Changed the cursor type based on which tools are selected:
	- A cross icon will be displayed when the selected tool is the **picker tool**, also applies when you're holding down CTRL with the brush tool
	- When there's an active selection, the **Move tool** will display a move icon
	- A zoom icon will appear when there's a tile you can select using the **Cell inspector tool**
- Inside the BRT Editor, the current grid cell where the mouse is hovering over is now highlighted.
- When using the brush tool, the selected tile/sprite under the cursor will be displayed on the grid with half opacity, this way you can more easily see what and where you're placing it.

#### Changed how copy & paste work inside the BRT editor:
- Copying something will now deselect the copied region as a confirmation for the user
- Copy & paste is now universal and works across different BRT Editors
- The pasted object now appears at the mouse cursor instead of the original position
- When pasting to another editor, any missing tiles will be created automatically. (Only a blank new tile will be created, and if the tile with the same ID is already present in the asset than that tile will be used. You can use the replace tool to replace the tiles after pasting. An automatic replace function is planned for the future)

#### Performance and bug fixes:
- Refactored a **lot** of code
- Removed an unnecessary `ref` keyword from the `BetterRuleTileGenerator.cs` which could cause a `foreach` loop to break early in some rare cases
- Fixed [Issue #27](https://github.com/Vinark117/BetterRuleTiles-Support/issues/27): Added a check to prevent duplicate neighbor positions
- Changed how textures and styles are stored, which should lead to less warnings
- Changed how sprite textures are cached, which should lead to less missing texture warnings
- Sprite drawer and override settings can now be moved when the windows are unlocked
- Fixed shortcuts requiring CTRL not working on mac