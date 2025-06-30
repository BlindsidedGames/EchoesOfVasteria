---
title: Changelog
---
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

- Added a new [[Sprite override settings]] window, here you can change the output parameters of a sprite, so if you place that sprite down multiple times it'll always have the same output.
- Separated the `sprite drawer` toolbar button and added a new button to open the `Universal sprite settings`
- Added an `Enable universal sprite settings` option to the export menu, this option is enabled by default on newly created assets, and disabled on already existing assets
- Improved UI for better readability and separation between sections of the window.
- Renamed `tile variations` and `variation of` options to make them less confusing.
- Added a confirmation window when deleting a tile.
- Fixed error which caused the game to not build
- Textures will be marked readable automatically if they're not already, it's not required to set it manually anymore
- Added a new [[Preset block]] feature. Draw part of a scene in the editor and mark it as a preset block to make sure it'll look as you wanted it to. To remove the preset block, use the `grid cell inspector` on a tile inside the preset block.
- Added more tooltips
- Added [[Examples]] each with their own tilemaps and scenes for the following features:
    - simplify rules feature
    - creating rules
    - preset blocks
    - connections between tiles
    - universal sprite settings and sprite options
    - variations
    - custom properties
- Toolbar now better adapts to the width of the editor window

## Version 1.3.3

- Fixed bug: Tiles randomly disappear from the grid and the tile drawer.
- Fixed bug: Editor crashes when entering or exiting play mode.
- Added a `Close window` button to the container asset, in case you can't close the window in any other way.

## Version 1.3.1 & Version 1.3.2

- Changed the way the editor handles arrays.

## Version 1.3.0

- Added a new sprite output type: `Pattern`.
- Other backend changes.

## Version 1.2.0

- Tested and verified compatibility with Unity 2019.
- Changed how locked cells are displayed.
- Added options to customize how locked cells are displayed.
- Added a sprite drawer.
- Added options to customize the new sprite drawer.
- Import all sprites with a single button press.
- Fixed a build error related to the Functions class.

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