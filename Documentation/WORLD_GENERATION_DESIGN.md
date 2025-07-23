# Procedural World Generation System

## 1. Core Philosophy: A Tiered, Data-Driven System
The world generator separates logic from data by using ScriptableObjects. All spawn
rules and map profiles live in assets, allowing designers to update content
without touching code.

A two-step weighting model ensures balance:
1. **Category Selection** – decide the overall type to spawn (e.g. `Woodcutting`, `Mining`, `Looting`, `Farming`, `Fishing`).
2. **Specific Selection** – once a category is chosen, select an individual object
   from that category (e.g. `Pine Tree`, `Iron Ore`, `Wooden Chest`).

This keeps high-level distribution consistent even as new tasks are added.

## 2. Order of Operations
Each map segment is generated in sequence by `SegmentedMapGenerator`:
1. **Terrain Generation** – `TilemapChunkGenerator` reads the active `MapProfile`
   and paints tiles for each terrain zone. This yields a completed tilemap with
   no tasks.
2. **Task Generation** – `ProceduralTaskGenerator` analyses that tilemap to decide
   what gameplay objects to place based on the rules in the ScriptableObjects.

The terrain must exist before tasks can be positioned.

## 3. Required Data Assets (ScriptableObjects)

### TaskCategoryType (Enum)
Central enum listing every task category such as Trees, Ores, Chests or Fishing.

### TaskCategory
Represents a group of related tasks.
- **categoryType** – the enum value for this category.
- **weight** – relative chance for the category to spawn.
- **tasksInThisCategory** – all `TaskSpawnRule` assets in this group.

### TaskSpawnRule
Defines the spawning conditions of one specific task.
- **taskPrefab** – prefab to spawn.
- **requiredQuest** – quest that must be completed for this task to appear.
- **weight** – chance to spawn after the category is chosen.
- **overrideSpawnRange** – whether to use a custom X range.
- **minX**, **maxX** – horizontal limits when overriding.
- **spawnOnEdgesOnly** – only place on the edge of a terrain patch.
- **avoidDifferentTerrainBuffer** – distance from other terrain types.
- **topBuffer** – required vertical space handled by the generator.

### TerrainType
Describes a single terrain type.
- **tile** – the `BetterRuleTile` used for painting.
- **validCategories** – list of `TaskCategory` assets allowed on this terrain.

### MapProfile
Controls the vertical composition of the map.
- **topZone**, **middleZone**, **bottomZone** – each links to a `TerrainType` and
defines its potential depth.

## 4. Generator Responsibilities
`TilemapChunkGenerator` and `ProceduralTaskGenerator` remain separate components.

### TilemapChunkGenerator
- Only creates the visual world.
- Uses a `MapProfile` to determine which tiles to place and their depth ranges.

### ProceduralTaskGenerator
- Populates the map with tasks.
- Reads the `MapProfile` to learn which terrain type exists at each location.
- For a given position it:
  1. Identifies the `TerrainType` present.
  2. Retrieves the `validCategories` for that terrain.
  3. Filters out categories with no spawnable task (quest incomplete or rules not met).
  4. Performs the **Category Roll** using the weights.
  5. Performs the **Task Roll** within the chosen category.
  6. Spawns the chosen prefab.
- Enforces global `mapTopBuffer` and `mapBottomBuffer` settings to stop spawns
  near the screen edges.
