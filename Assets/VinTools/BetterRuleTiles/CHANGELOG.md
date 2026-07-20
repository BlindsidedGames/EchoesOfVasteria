## Version 1.5.0

#### An entire new way of creating tiles was added to the tool, the **Tileset Editor**:
- The **TileSet Editor** is a an entirely new editor window, created to speed up the creation even more of simpler rule tiles. ***(NOTE: The Tileset Editor only works with square tiles)***
- You can access this new editor by creating a **Tileset Container**. You can do this by right clicking in the project window (Or clicking the `Assets` menu item) and navigating to either `Create/2D/Tiles/Tileset Container` or to `Tools/Better Rule Tiles/Create Tileset Container` in the menu bar. Open this file to show the new editor.
- To get started with the **Tileset Editor**, you need to import a sprite sheet. You can do this by drag and dropping it to the left panel of the editor or by clicking the import button in the bottom right of the panel. 
- It's important to note that this editor was designed to work with sliced sprite sheets, using individual sprites is possible but it'd defeat the purpose and ease of use of the editor.
- When selecting a sprite sheet, you can edit its rules and properties in the respective tabs of the main toolbar. Refer to the [[Tileset Editor|documentation]] to learn more.
- The right side of the toolbar has two buttons, the left button is the **export button**, clicking this will export the tiles. Compared to the **BRT Editor**, this button will not display a dropdown, and will use the default options if you haven't modified them.
- The right-most button will switch to the **BRT Editor**:
	- Both editors use the same type of file, so you can easily switch between the two editors, if the file was created as a **Tileset Container**
	- When switching to the **BRT Editor**, you'll quickly see how the **Tileset Editor** functions and lays out the sprites and tiles. The area used by the **Tileset Editor** is highlighted in a dark red background color. **Editing this area is not restricted, however using it for manually creating rules is heavily discouraged**, as the tiles and sprites can be replaced by the **Tileset Editor**.
	- You are free to do anything else you'd able to do if it wasn't a **Tileset Container**, and any changes made here will affect the exported tiles, no matter which editor was used to export them.

#### Implemented scriptable rule tiles:
- Adding new neighbor rules to tiles will now dynamically add those rules to the tile drawer in the editor.
- You can now change what rule tile script to use in the export dropdown.
- Tile scripts derived from `BetterRuleTileBase` and `BetterHexagonalRuleTileBase` will show up in the BRT Editor and can be selected in the **Export Options Window**
- The textures are encoded into Base64 strings. You can open a converter utility by opening the menu item `Tools/Better Rule Tiles/Open TexToBase64 Converter`
- Refer to the [[Custom rule tile scripts|documentation]] to learn more 

#### New features:
- When drag & dropping an entire sprite sheet into the editor, a new window will appear instead of immediately importing the sprites. In this window you can select to import sprites as usual, or you can select the other modes to speed up the importing of animated, randomized or pattern tiles. (This window is also the main way to import sprites into the **TileSet Editor**.)
- Sprites on the editor grid now respect the aspect ratio and the pivot point.
- When using the **Brush tool**, you can hold down the CTRL key to temporarily switch to the **Picker tool**
- You can now manually prioritize tiles by changing the new `Priority modifier` option in the **grid cell info window**.
- Added a new **Tile size** option in the `Editor Settings` window, which lets you change the rendering size of all tiles on the grid without affecting the sprites.
- If the **grid cell inspector tool** is active, a new **bulk cell editor** window will appear above the **cell inspector**. Using this you can change the priority group of all tiles inside a selection.

#### Quality of life improvements:
- Copying something will now deselect the copied region as a confirmation for the user
- Copy & paste is now universal and works across different BRT Editors
- The pasted object now appears at the mouse cursor instead of the original position
- When pasting to another editor, any missing tiles will be created automatically. (Only a blank new tile will be created, and if the tile with the same ID is already present in the asset than that tile will be used. You can use the replace tool to replace the tiles after pasting. An automatic replace function is planned for the future)
- The toolbar now expands to 4 lines if the editor window is too narrow
- Changed how the container assets get displayed in the project window
- Default sprites of tiles will now be automatically assigned if the user didn't specify it
- Changed the default 32x32 pixel **Grid size** option in the settings dropdown to a **Cell size** option with a default value of 1x1, to better match the built in Grid class.
- The **Sprite anchor** (formerly **Tile anchor**) setting now only affects the anchoring position of sprites
- Exposed the tile object reference field in to container inspector so it is more accessible in case it is needed
- Increased maximum zoom amount in the grid
- The anchor of the editor grid can now be changed in the editor settings dropdown.
- Once you've exported a container, it'll not let you change the editor grid type to an incompatible one.
- Changed the cursor type based on which tools are selected:
	- A cross icon will be displayed when the selected tool is the **picker tool**, also applies when you're holding down CTRL with the brush tool
	- When there's an active selection, the **Move tool** will display a move icon
	- A zoom icon will appear when there's a tile you can select using the **Cell inspector tool**
- Inside the BRT Editor, the current grid cell where the mouse is hovering over is now highlighted.
- When using the brush tool, the selected tile/sprite under the cursor will be displayed on the grid with half opacity, this way you can more easily see what and where you're placing it.
- Added new sample to the project showcasing the **Tileset Editor**
- Updated the included documentation in the project
- Removed the tile palette assets from the samples to not clutter the selection of tile palettes for the user.
- Updated tooltips and names in the `Editor Settings` window
- Changes to rule sorting and priority:
	- Added the ability for the **priority modifier** to also decrease (instead of just increasing) the priority of the rules
	- Renamed the `Priority modifier` option to `Rule order modifier`
	- Added an information text to the grid cell inspector window that tells you the index of the tiling rule in the exported tile. It also shows how the **rule order modifier** affects the priority of the rule.
	- Added a `Priority group` option to the **rule priority** section of the the **grid cell inspector**. This will be the primary sorting option for rules, and will fall back to the already established sorting method for rules within the same group (all tiles by default).
	- The **rule order modifier** option will only affect rules withing the same **priority group**
- Changes to the **grid cell inspector tool**:
	- Selecting a grid cell with the **grid cell inspector tool** will now unfocus any GUI elements, so values inside the **cell info window** won't stay selected and show incorrect values.
	- You can now select an area while using the **grid cell inspector tool**, so you don't have to switch tools when you need a selection for editing the grid cell.
	- Slightly decreased the height of the **grid cell inspector window**
- You can now drag and drop multiple textures to the BRT Editor at the same time
- Importing sprite sheets with padding between the sprites will now not leave gaps on the grid
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
- Added a warning message when the user is trying to import sprites that are part of a Sprite atlas
- Creating a new preset block that overlaps two or more other preset blocks will now give you an option to merge them into one
- If a preset block is not fully contained in the selected area, moving the selection will split the block into two separate blocks. Moving the selection back will not merge the blocks automatically.
- Updated documentation included with the tool

#### Performance and bug fixes:
- Changed package version from **2019.4** to **2022.3** to reflect the current development version.
- Refactored a **LOT** of code
- Removed an unnecessary `ref` keyword from the `BetterRuleTileGenerator.cs` which could cause a `foreach` loop to break early in some rare cases
- Fixed [Issue #27](https://github.com/Vinark117/BetterRuleTiles-Support/issues/27): Added a check to prevent duplicate neighbor positions
- Changed how textures and styles are stored, which should lead to less warnings
- Changed how sprite textures are cached, which should lead to less missing texture warnings
- Sprite drawer and override settings can now be moved when the windows are unlocked
- Fixed shortcuts requiring CTRL not working on mac
- Fixed issue where if you had a neighbor rule with **Pattern** output, it'd always get prioritized if the rules match.
- Fixed an issue where the selection preview created by the **grid cell inspector** was offset if the move tool was used before.
- Fixed an issue where creating a **BRT Container asset** would fail if any asset was selected in the project window
- Fixed an error where a missing sprite reference caused the sprite drawer to break the editor
- Brush preview will now show the correct anchored position of the sprites
- The selected sprite preview in the tile drawer is no longer stretched when selecting a non-square image
- Fixed [Issue #17](https://github.com/Vinark117/BetterRuleTiles-Support/issues/17): Sprite does not scale properly when rendering mode is tight
- Fixed [Issue #18](https://github.com/Vinark117/BetterRuleTiles-Support/issues/18): Preset block does not move with selection
- Fixed default hexagonal tile proportions
- Fixed locked tile overlays on the isometric and hexagonal grids
- Changed the width of the names of tiles in the **Connect to** section of the **Tile info window**
- Font size of tiles in the **tile drawer** will now adjust to fit longer names
- Fixed issue where the **sprite drawer** scrollbar would overlap with the buttons
- Fixed [Issue #30](https://github.com/Vinark117/BetterRuleTiles-Support/issues/30): FindObjectOfType is obsolete

---
## Version 1.4.6

- Fixed [Issue #32](https://github.com/Vinark117/BetterRuleTiles-Support/issues/32): Can't build app

## Version 1.4.5

- Fixed an issue with patterns not working correctly
- Implemented [Feature #29](https://github.com/Vinark117/BetterRuleTiles-Support/issues/29): Improved the sprite & tile replace window
	- The window is now separated into 3 tabs. The previous **replace sprites** and **replace tiles** options have been moved to their own tab, and a new **replace overrides** tab has been added.
	- The replace sprites tab now has an option to replace all sprites that are used for animations, patterns, etc. This option is hidden when using **universal sprite settings**
	- In the new **Replace overrides** tab you can replace sprites inside the **sprite override settings** window. Just select an override, and you can either modify that, or create a duplicate with the modified sprites.

## Version 1.4.4

- Fixed [Issue #15](https://github.com/Vinark117/BetterRuleTiles-Support/issues/15): Improved performance of better rule tiles
- Fixed [Issue #16](https://github.com/Vinark117/BetterRuleTiles-Support/issues/16): Changed how modified tiles are highlighted:
	- Yellow flashing indicates that the neighbor positions or the transform have been modified.
	- Pink flashing indicates if either:
	    - The collider type has been changed
	    - The sprite output type has been changed
	    - The sprites array has more than one sprite in it
- Fixed bug: Tiles with ID 1 and 2 were displayed incorrectly when inspecting the tiling rules in a Better Rule Tiles asset
- Fixed [Issue #25](https://github.com/Vinark117/BetterRuleTiles-Support/issues/25): Fixed issue with transition rules
- Tiles that are set up to connect to each other now won't be treated as the same tile by the tiling rules. This behavior can be re-enabled inside the export menu under the new option: "Treat similar tiles as same"

## Version 1.4.3

- Fixed [Issue #9](https://github.com/Vinark117/BetterRuleTiles-Support/issues/9): Can't build app

## Version 1.4.2

- Restructured folder layout
- Separated utility scripts to their own assemblies (for use in other packages)
- Several Unity error fixes
- Fixed [Issue #1](https://github.com/Vinark117/BetterRuleTiles-Support/issues/1): Moving hexagonal tiles shift neighbor positions
- Fixed [Issue #7](https://github.com/Vinark117/BetterRuleTiles-Support/issues/7): Copy-paste neighbor sync
- Fixed [Issue #8](https://github.com/Vinark117/BetterRuleTiles-Support/issues/8): Pasting and moving hexagonal tiles doesn't keep shape
- Fixed [Issue #2](https://github.com/Vinark117/BetterRuleTiles-Support/issues/2): Hexagonal tile export fix
- Hexagonal tiles will now be generated as the new `BetterHexagonalRuleTile` class
- Removed `Patterns` option from hexagonal tiles
- New [documentation website](https://docs.vinark.dev/better-rule-tiles/)!
- Added offline documentation to the package

## Version 1.4.1

- Fixed sprite output transform when using universal sprite settings
- Readded sprite output transform option when using preset blocks
- Fixed [Issue #4](https://github.com/Vinark117/BetterRuleTiles-Support/issues/4): Moving and pasting over something will now override what's already there
- Fixed [Issue #3](https://github.com/Vinark117/BetterRuleTiles-Support/issues/3): Added support for light theme
- Toolbar bugfix

## Version 1.4.0

- Added a new **Sprite override settings window**, here you can change the output parameters of a sprite, so if you place that sprite down multiple times it'll always have the same output.
- Separated the `sprite drawer` toolbar button and added a new button to open the `Universal sprite settings`
- Added an `Enable universal sprite settings` option to the export menu, this option is enabled by default on newly created assets, and disabled on already existing assets
- Improved UI for better readability and separation between sections of the window.
- Renamed `tile variations` and `variation of` options to make them less confusing.
- Added a confirmation window when deleting a tile.
- Fixed error which caused the game to not build
- Textures will be marked readable automatically if they're not already, it's not required to set it manually anymore
- Added a new **Preset block** feature. Draw part of a scene in the editor and mark it as a preset block to make sure it'll look as you wanted it to. To remove the preset block, use the `grid cell inspector` on a tile inside the preset block.
- Added more tooltips
- Added **Examples** each with their own tilemaps and scenes for the following features:
    - simplify rules feature
    - creating rules
    - preset blocks
    - connections between tiles
    - universal sprite settings and sprite options
    - variations
    - custom properties
- Toolbar now better adapts to the width of the editor window

---
## Version 1.3.3

- Fixed bug: Tiles randomly disappear from the grid and the tile drawer.
- Fixed bug: Editor crashes when entering or exiting play mode.
- Added a `Close window` button to the container asset, in case you can't close the window in any other way.

## Version 1.3.2

- Changed the way the editor handles arrays, again

## Version 1.3.1

- Changed the way the editor handles arrays.

## Version 1.3.0

- Added a new sprite output type: `Pattern`.
- Other backend changes.

---
## Version 1.2.0

- Tested and verified compatibility with Unity 2019.
- Changed how locked cells are displayed.
- Added options to customize how locked cells are displayed.
- Added a sprite drawer.
- Added options to customize the new sprite drawer.
- Import all sprites with a single button press.
- Fixed a build error related to the Functions class.

---
## Version 1.1.0

- Changed toolbar button placement code to be more dynamic.
- Added the ability to lock and unlock a selection, so it can't be edited accidentally.
- Added a warning message if the default sprite was not assigned.

## Version 1.0.1

- Added support for differently sliced isometric tiles, previously it only supported square sliced sprites.
- Updated package dependencies to reflect the minimum supported versions of packages and unity version.
- The tool now supports Unity 2020.

## Version 1.0.0

Initial release
