# Code Map

## Top-Level
- `Assets/` — Unity project content
- `ProjectSettings/` — Unity project settings
- `Packages/` — Unity packages (includes A* Pathfinding Project)
- `Documentation/` — Project documentation and summaries
- `docs/` — Static site and wiki assets

## Assets/Scripts
- `Audio/` — `AudioManager`, SFX helpers
- `Blindsided/` — Shared utilities (pooling, layout, save data glue, helpers)
- `Buffs/` — `BuffManager`, recipes, UI
- `Enemies/` — `Enemy`, `EnemyData`, activation, balance window
- `Hero/` — `HeroController`, health, stats, echoes
- `MapGeneration/` — `SegmentedMapGenerator`, `TilemapChunkGenerator`, decor config, terrain settings
- `NpcGeneration/` — Disciple generation systems and UI
- `Quests/` — `QuestManager`, UI, quest data
- `References/` — UI reference holders to wire scenes to presenters
- `Scripts/` (root) — glue classes like `GameManager`, `HealthBase`, etc.
- `Skills/` — Skill system and milestones
- `Stats/` — `GameplayStatTracker` and panels
- `Tasks/` — `TaskController`, task types (mining/fishing/farming/woodcutting/kill/open chest/talk)
- `Tools/` — Console and editor utilities
- `UI/` — Player-facing UI controllers and panels
- `Utilities/` — Small helpers (snapping, anim utils, etc.)

## Assets/Scriptables
- `Buffs/` — `BuffRecipe` assets
- `Tasks/` — Task spawn settings
- `Enemies/` — `EnemyData` assets
- Other gameplay data referenced by the above systems

## Steam Integration
- `Assets/Scripts/Steamworks.NET/` — Steam API wrappers; build-guarded for non-Steam/non-PC runs

## Scenes & Prefabs
- `Assets/Scenes/` — Playable scenes
- `Assets/Prefabs/` — Reusable objects (map, UI, enemies, etc.)

## Where Things Start
- Entry scene loads managers (UI, Audio, SaveData), then `GameManager` toggles between Town UI and Map UI.
- In a run, `SegmentedMapGenerator` + `TaskController` drive world and objectives; `HeroController` is the main agent.