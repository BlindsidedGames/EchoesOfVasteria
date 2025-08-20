# Timeless Echoes - Unity 2D Game Development Instructions

ALWAYS reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Project Overview
Timeless Echoes is an incremental hero management Unity 2D game built with **Unity 6000.1.6f1**. The hero automatically runs through procedurally generated maps, completing tasks while managing resources, combat, and progression systems.

## Working Effectively

### Initial Setup
- **REQUIRED UNITY VERSION**: Unity 6000.1.6f1 exactly. Do not use other versions.
- Clone the repository to your local machine
- Open Unity Hub and add the project folder
- When prompted, select Unity 6000.1.6f1 or install it if not available
- The main scene is located at `Assets/Scenes/Main.unity`

### Keystore Credentials Setup (Required for Builds)
```bash
# Create local keystore credentials (not stored in repository)
# In Unity editor: Tools > Create Keystore Credentials
# Enter keystore details, creates .keystore_credentials file
# Load credentials before opening Unity:
source Tools/load_keystore_credentials.sh
```

**IMPORTANT**: Never commit keystore passwords to the repository. The `.keystore_credentials` file is git-ignored.

### Unity Development Guidelines (CRITICAL)
- **NEVER modify `Assets/Scenes/Main.unity`** unless explicitly instructed
- **NEVER create or commit `.meta` files** unless absolutely required
- Replace obsolete Unity API calls:
  - Use `Object.FindFirstObjectByType` or `Object.FindAnyObjectByType` instead of `Object.FindObjectOfType`
  - Use `CinemachineCamera` instead of deprecated `CinemachineVirtualCamera` (CS0618 warning)
- Consult Unity documentation at https://docs.unity3d.com for APIs and best practices
- Ensure all changes work with Unity 6000.1.6f1

### Testing and Validation
- **CRITICAL LIMITATION**: Unity tests cannot be run in sandboxed environments
- Only add or modify Unity tests when explicitly requested
- Focus on manual testing in the Unity editor by pressing Play in the Main scene
- Test core gameplay loops: hero movement, task completion, combat, resource collection

### Building the Project

#### Unity Editor Build (Interactive)
```
1. Open Unity with the project
2. Go to Build > Build All (Linux+Windows IL2CPP, then Mac Mono)
3. Or use File > Build Settings... for single platform builds
```

#### Command Line Build (CI/Automated)
```bash
# Unity must be installed and available in PATH
# Build all platforms - NEVER CANCEL: Takes 45+ minutes. Set timeout to 90+ minutes.
Unity -batchmode -quit -nographics -projectPath . -executeMethod BuildTools.BatchBuild.BuildAllCI

# Custom build paths (optional)
Unity -batchmode -quit -nographics -projectPath . -executeMethod BuildTools.BatchBuild.BuildAllCI -buildPathWindows="/path/to/windows/build" -buildPathLinux="/path/to/linux/build"
```

**NEVER CANCEL BUILDS**: Unity builds take 45-90 minutes to complete. Always set timeouts to 120+ minutes minimum.

## Critical Timing and Timeout Guidelines

### Build Operations (NEVER CANCEL)
- **Unity Build All Platforms**: 45-90 minutes - Set timeout to 120+ minutes
- **Single Platform Build**: 15-30 minutes - Set timeout to 60+ minutes  
- **IL2CPP Builds**: Longer than Mono builds - Add 50% buffer to timeouts
- **First-time builds**: May take significantly longer due to cache warming

### Testing and Validation Timeouts
- **Manual Play Testing**: 5-15 minutes per core scenario
- **Map Generation Testing**: 2-5 minutes per test
- **Build Verification**: 5-10 minutes after successful build

### Development Operations
- **Unity Editor Startup**: 30-60 seconds (first time may be longer)
- **Script Compilation**: 10-30 seconds for typical changes
- **Asset Import**: Varies by asset type (textures, audio can take minutes)

**CRITICAL**: If any operation appears to hang, wait at least 60 minutes before considering alternatives. Unity operations often appear stalled but are working.

### Key Directory Structure
```
Assets/
├── Scenes/
│   ├── Main.unity          # Main game scene (DO NOT MODIFY without instruction)
│   └── Loading.unity       # Loading scene
├── Scripts/
│   ├── Hero/               # Hero controller, stats, health
│   ├── Tasks/              # Task system (mining, fishing, combat, etc.)
│   ├── MapGeneration/      # Procedural map and task generation
│   ├── Combat/             # Combat mechanics and projectiles
│   ├── Enemies/            # Enemy AI and behavior
│   ├── Quests/             # Quest system and management
│   ├── Buffs/              # Buff system and auto-casting
│   ├── Gear/               # Equipment and stat modifications
│   ├── UI/                 # User interface components
│   ├── Steamworks.NET/     # Steam integration
│   └── Editor/             # Unity editor tools and windows
├── Editor/
│   ├── Build/BatchBuild.cs # Automated build system
│   ├── Gear/               # Gear roll testing window
│   └── Tasks/              # Task-related editor tools
└── Prefabs/                # Game object prefabs
```

## Core Systems and Architecture

### GameManager (`Assets/Scripts/GameManager.cs`)
- Singleton orchestrating run lifecycle and map instantiation
- Coordinates camera/UI toggling and death window flow
- Entry point for starting runs via `StartRun()`

### Map Generation
- `SegmentedMapGenerator`: Streams map segments as hero advances
- `TilemapChunkGenerator`: Generates terrain (water/sand/grass) and decorative tiles
- `ProceduralTaskGenerator`: Places tasks procedurally across terrain types

### Task System (`Assets/Scripts/Tasks/`)
- `TaskController`: Central coordinator owning ordered task list
- Task types: `MiningTask`, `FishingTask`, `FarmingTask`, `WoodcuttingTask`, `TalkToNpcTask`, `OpenChestTask`
- Tasks derive from `BaseTask` and implement `ITask` interface
- Tasks expose `Target` transform and `IsComplete()` method

### Hero System (`Assets/Scripts/Hero/`)
- `HeroController`: Movement, behavior state machine (Idle/Task/Combat)
- Uses A* Pathfinding Project for navigation
- Stats calculated from base values + upgrades + active buffs
- Properties: `Damage`, `MoveSpeed`, `Defense`, `AttackRate`, `MaxHealthValue`

### Combat System
- Hero automatically engages enemies within vision range
- Projectile-based combat system
- Enemy levels scale with world X position
- Enemies drop resources and gear on death

## Common Development Tasks

### Adding a New Task Type
```csharp
1. Create MonoBehaviour implementing ITask or derive from BaseTask
2. Expose Transform Target and implement IsComplete()
3. Raise TaskCompleted event when finished
4. Place component under map segment or add via TaskController.AddRuntimeTaskObject
5. Optional: Add spawn rules to ProceduralTaskGenerator for procedural generation
```

### Adding a New Enemy
```csharp
1. Create EnemyData ScriptableObject with stats, range, speed, drop table
2. Create prefab with Enemy, AIPath, AIDestinationSetter, RVOController, Health
3. Assign EnemyData and projectile prefab
4. TaskController automatically creates KillEnemyTask for enemies
```

### Adding a New Buff
```csharp
1. Create BuffRecipe asset with effects (% bonuses, lifesteal, instant tasks)
2. BuffManager aggregates effects and exposes multipliers
3. For auto-cast: ensure quests unlock slots via unlockBuffSlots
```

### Adding a New Quest
```csharp
1. Create QuestData under Resources/Quests with requirements and rewards
2. Set dependencies in requiredQuests
3. QuestUIManager auto-creates UI entries
```

## Steam Integration
- Uses Steamworks.NET for achievements and rich presence
- `AchievementManager`: Handles Steam achievement unlocking
- `RichPresenceManager`: Updates Steam status during gameplay
- Achievement examples: `MeetIvan`, `MeetEva`, `SlimeSwarm`, `Mildred`

## Important Files to Reference

### Documentation
- `Documentation/ARCHITECTURE.md` - Detailed system architecture
- `Documentation/CODEMAP.md` - Code organization guide  
- `Documentation/HOW_TO_ADD_FEATURES.md` - Feature addition guide
- `AGENTS.md` - Unity development guidelines and constraints

### Key Scripts
- `Assets/Scripts/GameManager.cs` - Main game controller
- `Assets/Scripts/Hero/HeroController.cs` - Hero behavior and stats
- `Assets/Scripts/Tasks/TaskController.cs` - Task coordination
- `Assets/Editor/Build/BatchBuild.cs` - Build automation

## Validation Scenarios
After making changes, always test these core workflows:

### Basic Gameplay Loop
1. Open Main scene in Unity
2. Press Play
3. Verify hero spawns and begins moving toward first task
4. Confirm task completion advances hero to next task
5. Test combat engagement when enemies are present
6. Verify resource collection and UI updates

### Map Generation
1. Start a new run from town
2. Verify terrain generation (water/sand/grass tiles)
3. Confirm task placement across different terrain types
4. Test segment streaming as hero advances

### Steam Integration (if available)
1. Test achievement unlocking for key NPCs
2. Verify rich presence updates during gameplay
3. Confirm Steam initialization doesn't block gameplay

## Common Issues and Solutions

### Build Failures
- Ensure Unity 6000.1.6f1 is installed and activated
- Check that all required build modules are installed (Linux, Windows, Mac)
- Verify scenes are enabled in Build Settings
- Build paths must be writable directories
- **Missing keystore**: Run `Tools > Create Keystore Credentials` in Unity

### API Warnings and Errors
- Replace `FindObjectOfType` with `FindFirstObjectByType`
- Use `CinemachineCamera` instead of `CinemachineVirtualCamera`
- Check Unity 6000.1.6f1 compatibility for any new APIs
- CS0618 warnings indicate obsolete APIs - check AGENTS.md for replacements

### Performance Issues
- Enemy activation system automatically manages performance via `EnemyActivator`
- Map segments recycle automatically to prevent memory leaks
- A* grid updates dynamically to cover visible chunks

### Unity Editor Issues
```bash
# Common fixes for Unity editor problems
# Clear Unity cache (close Unity first)
rm -rf Library/
rm -rf Temp/
rm -rf obj/

# Force reimport all assets (in Unity)
Assets > Reimport All

# Reset Unity editor layout
Window > Layouts > Default
```

### Package Manager Issues
```bash
# Reset package cache if packages fail to resolve
# Close Unity first, then:
rm -rf Library/PackageCache/
# Reopen Unity to regenerate package cache
```

### Steam Integration Issues
- Verify `steam_appid.txt` exists in project root
- Check Steamworks.NET initialization in console logs
- Steam must be running for achievements and rich presence to work
- Achievement errors are logged but don't block gameplay

## External Dependencies
- A* Pathfinding Project (embedded package)
- Cinemachine 3.1.3
- Unity Input System 1.14.0
- Unity Localization 1.5.5
- Unity 2D Animation 10.2.0
- Steamworks.NET (vendored)

Always verify these packages are properly installed when setting up the project.

## Frequently Used Commands and Tools

### Unity Editor Menu Items
- `Build > Build All (Linux+Windows IL2CPP, then Mac Mono)` - Build all platforms
- `Tools > Create Keystore Credentials` - Create keystore credential file
- `Window > General > Console` - View debug logs and errors

### Editor Windows (Custom)
- `Window > Gear Roll Tester` - Test gear stat rolling and distributions
- Quest Flow Window - Visual quest dependency editor
- Resource Editor - Manage resource types and balancing
- Skill Editor - Configure skill trees and abilities

### Common File Locations
```bash
# Key configuration files
Assets/Scenes/Main.unity                    # Main game scene
Assets/Scripts/GameManager.cs               # Central game controller
ProjectSettings/EditorBuildSettings.asset  # Build configuration
.keystore_credentials                       # Local keystore (git-ignored)

# Frequently referenced directories
Assets/Scripts/Tasks/                       # Task implementations
Assets/Scripts/Hero/                        # Hero behavior and stats
Assets/Prefabs/                            # Reusable game objects
Documentation/                             # Architecture and guides
```

### Git Workflow
```bash
# Standard development workflow
git status                                  # Check current changes
git add .                                   # Stage all changes
git commit -m "Description of changes"     # Commit with message
git push origin branch-name                # Push to remote branch

# Files to check before committing
git diff                                    # Review changes
git log --oneline -10                      # Recent commits
```

Remember: The `.keystore_credentials` file and Unity-generated files are automatically ignored by Git.