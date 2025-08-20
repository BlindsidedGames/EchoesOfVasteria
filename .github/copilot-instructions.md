# Timeless Echoes - Unity 2D Incremental Game

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Prerequisites and Setup
- **CRITICAL**: This project requires Unity **6000.1.6f1** exactly. Download from Unity Archive if not available in Unity Hub.
- **Unity Hub Installation**: Download from https://unity3d.com/get-unity/download
- **Build Support Modules**: Install Linux Build Support (IL2CPP) and Windows Build Support (IL2CPP) via Unity Hub
- **NEVER CANCEL UNITY OPERATIONS**: Unity operations can take 5-45 minutes. Set timeouts to 60+ minutes minimum.
- **Project Setup**: Clone repository and open via Unity Hub by adding the project folder
- **Keystore Setup** (optional): Create `.keystore_credentials` file via Unity menu `Tools > Create Keystore Credentials`

## Common Repository Contents

Use these to quickly verify project state without searching:

### Key Directories
```
Assets/
├── Scenes/                    # Main.unity, Loading.unity  
├── Scripts/                   # 893 C# files organized by system
│   ├── Audio/                 # AudioManager, SFX helpers
│   ├── Buffs/                 # BuffManager, recipes, UI
│   ├── Enemies/               # Enemy, EnemyData, activation
│   ├── Hero/                  # HeroController, health, stats
│   ├── MapGeneration/         # SegmentedMapGenerator, TilemapChunkGenerator
│   ├── Tasks/                 # TaskController, task implementations
│   ├── UI/                    # Player-facing UI controllers
│   └── Steamworks.NET/        # Steam integration (optional)
├── Prefabs/                   # Reusable game objects
├── Editor/Build/              # BatchBuild.cs automated builds
└── com.rlabrecque.steamworks.net/  # Steam API integration
Documentation/                 # ARCHITECTURE.md, CODEMAP.md, etc.
ProjectSettings/               # Unity project configuration
Packages/                      # com.arongranberg.astar embedded
```

### Critical Files
- `Assets/Scripts/GameManager.cs` - Main singleton orchestrating runs
- `Assets/Scripts/Hero/HeroController.cs` - Player character logic
- `Assets/Scripts/Tasks/TaskController.cs` - Task management system
- `Assets/Editor/Build/BatchBuild.cs` - Multi-platform build automation
- `ProjectSettings/ProjectVersion.txt` - Unity version (6000.1.6f1)
- `ProjectSettings/EditorBuildSettings.asset` - Scene configuration
- `steam_appid.txt` - Steam App ID (2940000)

### Building the Project

**CRITICAL: NEVER CANCEL UNITY BUILDS** - They may appear frozen but are processing

#### Unity Editor Build (Recommended)
1. Open Unity → File → Build Settings
2. Verify scenes: Loading, Main are checked and enabled  
3. Select target platform (Windows/Linux/Mac)
4. Click "Build" or use menu: `Build/Build All (Linux+Windows IL2CPP, then Mac Mono)`

#### Command Line Build (Advanced)
```bash
# Full multi-platform build (45-60 minutes total)
Unity -batchmode -quit -nographics -executeMethod BuildTools.BatchBuild.BuildAllCI

# Custom build paths (optional)
Unity -batchmode -quit -nographics -executeMethod BuildTools.BatchBuild.BuildAllCI \
  -buildPathWindows="/custom/windows/path" \
  -buildPathLinux="/custom/linux/path" \
  -buildPathMac="/custom/mac/path"
```

#### Build Configuration Details
- **Default Paths**: `C:\Users\mattr\Documents\Unity\Builds\Echoes of Vasteria\[Platform]\`
- **Windows Target**: `TimelessEchoes.exe` (IL2CPP, 64-bit)
- **Linux Target**: `TimelessEchoes.x86_64` (IL2CPP, 64-bit)  
- **macOS Target**: `TimelessEchoes.app` (Mono, Universal)
- **Scenes**: Loading.unity + Main.unity (automatically included)
- **Build Size**: ~150-300 MB per platform

### Testing and Validation
- **No Automated Tests**: This project uses manual testing only
- **ALWAYS** test gameplay after making changes:
  1. Open Main scene and press Play
  2. Click a map generation button to start a run
  3. Watch hero pathfind to tasks and complete them
  4. Verify combat engages when enemies are nearby
  5. Verify resource drops and UI updates
  6. Test retreat functionality or wait for death/reaper
- **Performance**: Monitor for frame drops during map generation or large enemy spawns

### Key Systems Overview
- **GameManager**: Singleton orchestrating run lifecycle (`Assets/Scripts/GameManager.cs`)
- **Map Generation**: `SegmentedMapGenerator` + `TilemapChunkGenerator` create procedural levels
- **Tasks**: `TaskController` manages mining, fishing, farming, combat, and NPC interactions
- **Hero**: `HeroController` uses A* pathfinding, auto-combat, stats from upgrades/buffs
- **Enemies**: Level-scaled enemies with drop tables and Steam achievement integration
- **Steam Integration**: Behind `STEAMWORKS_NET` defines - builds work without Steam

### Dependencies and Packages
- **A* Pathfinding Project**: Embedded in `Packages/com.arongranberg.astar/`
- **Steamworks.NET**: `Assets/com.rlabrecque.steamworks.net/`
- **Cinemachine 3.1.3**: Camera following system
- **Input System 1.14.0**: New Unity input system
- **URP 17.1.0**: Universal Render Pipeline
- **Localization 1.5.5**: Multi-language support

## Validation Scenarios

### MANDATORY: Complete Gameplay Validation
After ANY code changes, ALWAYS run this complete validation:

1. **Project Load**: Open Unity → Load Main.unity scene (should load in 15-30 seconds)
2. **Play Mode**: Press Play button (5-10 seconds to enter)
3. **Town Interface**: 
   - Verify resource counters display (top UI)
   - Check quest panel shows available/completed quests
   - Confirm upgrade panels show purchasable improvements
   - Test settings and menu toggles
4. **Map Generation**: 
   - Click any map generation button 
   - **TIMING**: Map should generate within 3-8 seconds
   - Verify terrain appears (water, sand, grass tiles)
   - Check that tasks spawn (mining, fishing, farming icons)
   - Confirm hero spawns at left side of map
5. **Hero Behavior**:
   - Hero should automatically pathfind to nearest task
   - Observe A* pathfinding navigation around obstacles
   - Verify hero reaches and completes first task
   - Check resources increment after task completion
6. **Combat System**:
   - Wait for or move hero near enemies
   - Verify automatic engagement when enemy in range
   - Check projectile firing from hero
   - Confirm enemy takes damage and fights back
   - Validate resource drops on enemy death
7. **UI Updates**:
   - Resource counters update in real-time
   - Distance traveled increases as hero moves
   - Health bar responds to damage
   - Buff/quest UI updates appropriately
8. **Run Termination**:
   - Test retreat button (should return to town)
   - OR let hero die (health reaches 0)
   - OR wait for reaper to catch up
   - Verify return to town interface
   - Check run statistics are recorded

### Steam Integration (if enabled)
1. **Rich Presence**: Verify Steam shows "In Town" or distance during runs
2. **Achievements**: 
   - Meet NPC Ivan1 → "MeetIvan" achievement
   - Meet NPC Witch1 → "MeetEva" achievement  
   - Engage 5+ slimes simultaneously → "SlimeSwarm" achievement
   - Complete Mildred's quest → "Mildred" achievement
3. **Steam App ID**: 2940000 (check steam_appid.txt)
4. **Stats Tracking**: Combat metrics, distance traveled, resources earned

### Performance Validation  
- **Map Generation**: Should complete within 5-10 seconds for standard maps
- **Enemy Spawning**: Monitor performance with 10+ enemies active
- **A* Pathfinding**: Hero should smoothly navigate around obstacles

## Common Development Tasks

### Adding New Features
- **New Task Type**: Derive from `BaseTask`, implement `IsComplete()`, add to spawn settings
- **New Enemy**: Create `EnemyData` ScriptableObject, prefab with AI components
- **New Buff**: Create `BuffRecipe` asset with effects, reference in UI/quests
- **New Quest**: Create `QuestData` in `Resources/Quests/` with requirements and rewards

### Build Configuration
- **Default Build Paths**: Windows builds to `Documents/Unity/Builds/Echoes of Vasteria/Windows/`
- **CLI Overrides**: Use `-buildPathWindows="custom/path"` to override defaults
- **IL2CPP**: Used for Linux/Windows for performance, Mono for macOS compatibility
- **Scene Configuration**: Loading + Main scenes automatically included via EditorBuildSettings

### Code Style and Patterns
- **Singletons**: `GameManager`, `BuffManager`, `QuestManager`, etc. use `Instance` pattern
- **Events**: Listen to `GameplayStatTracker.OnRunEnded`, `ResourceManager.OnInventoryChanged`
- **Save System**: Use `Blindsided.Oracle.saveData` for persistence
- **Steam Defines**: Wrap Steam code in `#if STEAMWORKS_NET` blocks

## Timing Expectations and Performance

### Build Times (NEVER CANCEL - CRITICAL)
- **WINDOWS IL2CPP**: 25-35 minutes actual time → Set timeout to **45+ minutes**
- **LINUX IL2CPP**: 20-30 minutes actual time → Set timeout to **40+ minutes**  
- **MACOS MONO**: 10-15 minutes actual time → Set timeout to **25+ minutes**
- **ALL PLATFORMS**: 45-60 minutes total → Set timeout to **90+ minutes**
- **WARNING**: Builds may appear frozen but are processing. Monitor Unity logs, not just console output.

### Development Operations Timing
- **Unity Project Load**: 30-60 seconds (loads packages, compiles scripts)
- **Scene Opening**: 15-30 seconds for Main scene (complex scene with many GameObjects)
- **Script Compilation**: 10-30 seconds after C# changes (893 scripts to process)
- **Play Mode Entry/Exit**: 5-10 seconds (initializes singletons, loads systems)
- **Map Generation**: 3-8 seconds per map (procedural terrain + task spawning)
- **A* Graph Scanning**: 1-3 seconds (pathfinding grid calculation)

### Performance Benchmarks  
- **Target FPS**: 60 FPS in town, 30+ FPS during gameplay
- **Map Generation**: Should not drop below 10 FPS during generation
- **Enemy Count**: Performance degradation expected with 15+ active enemies
- **Memory Usage**: Expect 300-500 MB in Development, 150-300 MB in builds

## Critical Warnings

- **NEVER** modify `Assets/Scenes/Main.unity` unless explicitly instructed
- **NEVER** cancel Unity builds or operations - they may appear stuck but are processing
- **ALWAYS** use Unity 6000.1.6f1 - other versions may have compatibility issues
- **ALWAYS** test gameplay scenarios after making changes
- **ALWAYS** commit frequently using small, focused changes

## Troubleshooting and Debugging

### Build Issues
- **Missing Build Modules**: Install Linux/Windows Build Support (IL2CPP) in Unity Hub → Installs → Add Modules
- **IL2CPP Errors**: Check Player Settings → Configuration → Scripting Backend is set to IL2CPP
- **Path Issues**: Use forward slashes in CLI build paths, escape spaces in paths with quotes
- **Build Hangs**: Check Unity Editor logs in ~/Library/Logs/Unity/ or %USERPROFILE%\AppData\Local\Unity\Editor\
- **Memory Issues**: IL2CPP builds require 4+ GB RAM, close other applications during builds

### Runtime Issues
- **A* Pathfinding Fails**: 
  - Check A* → Settings → Show Graphs to verify grid covers terrain
  - Ensure graph scans complete before hero spawns
  - Look for "PathfindingError" messages in console
- **Missing Singleton References**: 
  - Check Console for "Instance is null" messages
  - Verify singleton GameObjects exist in Main scene
  - Use defensive null checks: `BuffManager.Instance?.ApplyBuff()`
- **Performance Problems**: 
  - Open Window → Analysis → Profiler
  - Monitor Draw Calls (target <100) and Batches (target <50)
  - Check Memory allocation spikes during map generation
- **Steam Integration**:
  - Game runs without Steam - all Steam code is behind #if STEAMWORKS_NET defines
  - Check Console for "Steamworks" error messages if Steam features expected
  - Verify steam_appid.txt contains "2940000" for testing

### Common Console Messages (Normal)
- `"A* Pathfinding Project: Scanning graph"` - Normal during map generation
- `"BuffManager: Applying buff effect"` - Normal during gameplay
- `"TaskController: Adding task"` - Normal during map setup
- `"Steamworks not available"` - Normal when Steam not running

### Console Messages Requiring Action
- `"PathfindingError"` - A* pathfinding issue, check graph configuration
- `"NullReferenceException"` - Missing reference, check singleton initialization
- `"IndexOutOfRangeException"` - Array bounds issue, check list sizes
- `"IL2CPP build failed"` - Build configuration problem, check Player Settings

## Development Best Practices

### Code Changes
- **Small Commits**: Make focused changes to single systems
- **Test Immediately**: Run validation scenarios after each change
- **Console Monitoring**: Always watch Unity Console for errors/warnings
- **Backup Scenes**: Never modify Main.unity without backing it up first
- **Singleton Safety**: Always null-check singleton instances: `Manager.Instance?.Method()`

### Performance Guidelines
- **Profiler Usage**: Enable during development, disable in builds
- **Draw Call Limit**: Keep under 100 draw calls for stable performance
- **Enemy Spawning**: Limit to 10-15 active enemies simultaneously
- **Map Size**: Standard maps should generate within 8 seconds

### Steam Development
- **Local Testing**: Use steam_appid.txt (2940000) for development
- **Achievement Testing**: Use Steam development environment
- **Rich Presence**: Test with Steam client running
- **Build Defines**: Wrap Steam code in `#if STEAMWORKS_NET` blocks

### Unity Version Compatibility
- **CRITICAL**: Only use Unity 6000.1.6f1 - other versions may break compatibility
- **Package Versions**: Do not update embedded packages (A* Pathfinding, etc.) 
- **API Changes**: Be aware of obsolete Unity APIs (e.g., use CinemachineCamera not CinemachineVirtualCamera)

Always validate changes by running through complete gameplay scenarios. The project prioritizes stability and user experience over development speed.