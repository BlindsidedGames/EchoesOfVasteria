
# Starting "New" chats
- Look through relevant code, and derive as much information related to the topic as you can.
- Present your plan and wait for me to approve the plan before you make any changes, except where asked to add documentation.
- Ask clarifying questions.
- Take performance into account when making suggestions, if there is a more performant way to do something that will achieve the same result then that is preferred. 

# Doing a task
- Attempt to keep code as clean and performnant as possible.

# Finishing a task
- Add a brief entry to Changes.md describing what you changed, always put it at the top of the file.
- Ensure no stray \r\n ect is left in code.

# Repository Instructions

- Always use best practices and clean code when modifying or creating systems.
- Use `[SerializeField] private` for fields; prefer properties for public API
- Avoid `FindObjectOfType` & `GetComponent` in `Update`; cache in `Awake`

When making changes to this **2D** Unity project:

- Consult the official Unity documentation at <https://docs.unity3d.com> to confirm APIs and best practices.
- Ensure compatibility with **Unity 6000.2.1f1**, the version used by Echoes of Vasteria (Timeless Echoes).
- Do not create or commit `.meta` files unless absolutely required.
- Avoid using obsolete Unity API calls. For example, replace `Object.FindObjectOfType` with `Object.FindFirstObjectByType` or `Object.FindAnyObjectByType`.
- Note the warning `CS0618: 'CinemachineVirtualCamera' is obsolete`. Use `CinemachineCamera` instead of the deprecated `CinemachineVirtualCamera`.
- Do not modify `Assets/Scenes/Main.unity` unless explicitly instructed to do so.
- Unity tests cannot be run in this environment. Only add or modify Unity tests when explicitly requested; otherwise, skip them but run any tests you can on your changes.

## Task Suggestion Guidelines

- Default to proposing a single task that accomplishes the user's goal.
- When identifying bugs, refactorings, or performance issues, provide a separate task for each distinct item.
- If the user explicitly asks for a suggested task, limit the response to one task.

## Documentation Standards

- When documenting new scripts or refreshing existing documentation, match the concise, present-tense tone used in the README's system overviews.
- Begin with a one-sentence overview that states what the script or system accomplishes in practice (e.g., "The HeroController uses the A* Pathfinding Project to navigate between tasks.").
- Call out automation loops, cooldown toggles, and configuration points so future readers understand how the feature supports the broader gameplay lifecycle.
- Whenever a script coordinates companion entities or AI-controlled allies, explain how they engage, follow, or defer to the player's actions.
- Summarize public methods and properties with professional descriptions that clarify responsibilities, side effects, and notable dependencies.
- Prefer short paragraphs and optional subheadings instead of long bullet lists, unless you are enumerating discrete configuration toggles or stat hooks.
## Project Structure Map

### Root
- `Assets/`   Gameplay assets, prefabs, art, runtime scripts; see breakdown below.
- `Packages/`   Unity package manifest and embedded packages.
- `ProjectSettings/`   Engine-level settings (input, graphics, addressables).
- `UserSettings/`   Per-seat editor preferences (not shared).
- `Library/`, `Logs/`, `Temp/`, `obj/`   Generated caches and build outputs; regenerate as needed.
- `docs/`   Project wiki content and supporting images.
- `Documentation/`   Legacy design docs and references.
- `GeneratedAssets/`, `GeneratedAssets_deleted/`   Auto-generated content from procedural tools.
- `SteamAssets/`   Steam integration data and configuration.
- `Recordings/`, `Screenshots/`, `Timeless Error Logs/`   Captured media and diagnostics.
- `Mobile/`   Platform-specific build artifacts.
- `.github/`, `.vscode/`, `.idea/`   Repo automation and workspace configuration.

### Assets
- `AddressableAssetsData/`   Addressables catalog, groups, and build settings.
- `Art/`   2D sprites, tiles, and visual source assets.
- `Audio/`   Audio clips, mixers, and music assets.
- `Backup/`   Archived versions of critical assets.
- `BetterRuleTiles/`   Custom rule tile assets and supporting scripts.
- `Editor/` & `Editor Default Resources/`   Custom editor tooling and required resources.
- `ExternalDependencyManager/`   Google EDM4U and dependency resolution plugins.
- `Localization/`   Unity localization tables and string assets.
- `MPUIKit/`   UI Toolkit extension package content.
- `Plugins/`   Third-party and native plugins (Cinemachine, Odin, etc.).
- `Prefabs/`   Prefab library for gameplay objects, UI, and environments.
- `Resources/` & `Resources_moved/`   Assets loaded at runtime via the `Resources` API.
- `Scenes/`   Game scenes (`Loading.unity`, `Main.unity`).
- `Screenshots/`   Reference imagery captured in-editor.
- `Scriptables/`   ScriptableObject data (map configs, skills, gear definitions).
- `Scripts/`   Runtime and editor C# code; see module map below.
- `Settings/`   Input System, URP, Addressables, and other project settings assets.
- `Tests/`   Placeholder for Unity PlayMode/EditMode tests.
- `TextMesh Pro/`   TMP essential resources.
- `Tilemaps/`   Tile assets and palettes for procedural map layouts.

### Assets/Scripts Modules
- `Audio/`   Audio systems (mixers, SFX helpers, focus muting).
- `Blindsided/`   Shared framework pieces (components, save data, UGS hooks, utilities).
  - `Components/`, `SaveData/`, `UGS/`, `Utilities/`   Modularized subsystems supporting the rest of the project.
- `Buffs/`   Buff/debuff definitions and runtime handlers.
- `Combat/`   Combat flow, damage processing, and encounter helpers.
- `Editor/`   Custom inspectors and tooling scripts.
- `Enemies/`   Enemy behaviours, data containers, kill tracking, and naming utilities.
- `Gear/`   Gear crafting logic and UI.
  - `SO/`   Gear-related ScriptableObjects (rarities, crafting configs, stat definitions).
  - `UI/ForgeWindowUI/`   Partial classes powering the forge window interface.
- `Hero/`   Hero controllers, health, audio, echoes, and hero-specific stats.
  - `Stats/`   Hero stat blocks and modifiers.
- `MapGeneration/`   Procedural map/layout generation and navmesh setup.
- `Migration/`   Save data migration helpers for backward compatibility.
- `NPC/`   Runtime NPC behaviours and interactions.
- `NpcGeneration/`   Procedural NPC roster construction.
- `Platform/`   Platform-specific integration wrappers (Steam, focus, etc.).
- `Quests/`   Quest tracking, utilities, and quest-related data.
- `References/`   Scriptable reference registries for UI and stat panels.
- `Skills/`   Skill definitions, effects, and execution logic.
- `Stats/`   Core stat architecture shared across hero, enemies, and gear.
- `Steamworks.NET/`   Steamworks bindings and helper utilities.
- `Tasks/`   Overworld task system (gathering, NPC interactions, procedural generation).
- `Tools/`   Developer tooling, debugging helpers, and editor utilities.
- `UI/`   Global UI windows, HUD components, and settings panels.
- `Upgrades/`   Player progression and upgrade management systems.
- `Utilities/`   General-purpose helpers, math utilities, and extension methods.

### Notable Entry Points
- `Assets/Scripts/GameManager.cs`   Central runtime orchestrator for map generation, hero lifecycle, and UI state.
- `Assets/Scripts/Audio/AudioManager.cs`   Global audio bootstrapper handling mixers, saved volumes, and SFX helpers.
- `Assets/Scripts/Gear/UI/ForgeWindowUI/*`   Partial classes composing the forge window crafting workflow.
- `Assets/Scenes/Main.unity`   Primary gameplay scene (avoid editing without explicit instruction).
- `Assets/Scenes/Loading.unity`   Loading/interstitial scene used during transitions.
