# Echoes of Vasteria Gameplay Feature Audit

Date: 2026-05-08

## Scope

This is a documentation-only gameplay and feature completeness audit for the 2D Unity project at `C:\Users\mattr\Documents\Unity\Projects\Echoes of Vasteria`. I inspected gameplay-facing code, data paths, docs, and recent change notes without modifying scripts, scenes, prefabs, package files, meta files, or `Changes.md`.

Primary systems inspected include:

- `README.md`, `DevStuff/EoV_Context.md`, `DevStuff/Changes.md`
- `Assets/Scripts/GameManager.cs`
- `Assets/Scripts/MapGeneration/MapGenerationConfig.cs`, `SegmentedMapGenerator.cs`, `TilemapChunkGenerator.cs`, `ReapLineSystem.cs`
- `Assets/Scripts/Tasks/TaskController.cs`, `ProceduralTaskGenerator.cs`, `TaskData.cs`, `TaskWeightService.cs`, `ContinuousTask.cs`, `ResourceGeneratingTask.cs`
- `Assets/Scripts/Hero/HeroBase.cs`, `HeroController.cs`, `HeroCombatController.cs`, `HeroMovementController.cs`, `EchoController.cs`, `EchoManager.cs`
- `Assets/Scripts/Enemies/Enemy.cs`, `EnemyData.cs`, `EnemyKillTracker.cs`
- `Assets/Scripts/Skills/SkillController.cs`, `Skill.cs`, `Assets/Scripts/Skills/Milestones/*`
- `Assets/Scripts/Quests/QuestManager.cs`, `QuestData.cs`, `QuestTextFormatter.cs`
- `Assets/Scripts/Gear/*`, especially `CraftingService.cs`, `EquipmentController.cs`, `SalvageService.cs`, `ForgeWindowUI.cs`
- `Assets/Scripts/Upgrades/ResourceManager.cs`, `CauldronManager.cs`, `CauldronConfig.cs`, `RunBreakdownManager.cs`
- `Assets/Scripts/NpcGeneration/AlterEchoGenerationManager.cs`, `AlterEchoGeneratorUIManager.cs`
- `Assets/Scripts/UI/MapUI.cs`, `TownWindowManager.cs`, `TaskStatsPanelUI.cs`, `RunStatsPanelUI.cs`, `ResourceTierUpPopupUI.cs`, `IntroScreenController.cs`
- `Assets/Scripts/Audio/AudioManager.cs`
- Existing tests under `Assets/Tests/EditMode`

I found roughly 49 task assets, 97 quest assets, 22 enemy data assets, 10 buff assets, 8 gear cores, 8 rarity assets, and broad resource coverage under `Assets/Resources`. The project already has enough content and systems to support richer gameplay mostly through connective tissue, presentation, and scoped data additions.

## Current Game Loop

The current loop is an auto-run incremental RPG loop:

1. The player starts a map from town through `GameManager` map generation buttons.
2. `GameManager` spawns a map prefab and sets `CurrentGenerationConfig`.
3. `SegmentedMapGenerator` warms up map segments, `TilemapChunkGenerator` builds terrain/decor, and `ProceduralTaskGenerator` places tasks, enemies, and NPC tasks.
4. `TaskController` orders available tasks and assigns the earliest suitable task to the hero or Echo helpers.
5. `HeroBase` moves the hero via A* pathfinding, performs tasks, interrupts for combat, and resumes task selection.
6. Tasks grant resources and skill XP through `ResourceGeneratingTask`, `DropResolver`, `ResourceManager`, and `SkillController`.
7. Enemies scale by distance or kill-mode rules, fight the hero/Echoes, grant combat XP, and drop resources/gear ingredients.
8. Runs end by retreat, death, reaping distance, or abandonment; `GameplayStatTracker`, `RunResourceTrackerUI`, and `RunBreakdownManager` record and show run outcomes.
9. Town systems turn run output into power: quests, skill milestones, buffs, Forge gear, Cauldron cards, resource tiers, and Alter Echo offline generation.
10. Save/offline support persists resources, skills, quests, gear, Cauldron cards, task records, recent runs, and Alter Echo generator progress.

The player mostly watches the hero run, then makes town-side optimization decisions. The strongest missing layer is a clearer sense of "why this run matters now" and "what changed because of it."

## Strong Existing Foundations

- The map/session runtime is already modular. `MapGenerationConfig`, `SegmentedMapGenerator`, terrain task settings, and generation buttons provide natural hooks for map variants and run modifiers.
- Task content is data-driven. `TaskData` already supports skill association, XP, duration, spawn ranges, skill requirements, weights, drops, and additional loot chances.
- Task preference and mastery already have a base. `TaskWeightService` tracks completion thresholds, player toggles, and effective weights, while `TaskStatsPanelUI` displays spawn chance, completions, weights, and next-improvement data.
- Progression has multiple persistent channels. Skills, milestones, resource tiers, quest rewards, buffs, gear, Cauldron cards, Alter Echo generators, and run stats all feed permanent or semi-permanent growth.
- Echoes are more than cosmetic. `EchoController` can perform skill-restricted tasks, fight, follow, show indicators, respect caps, and defer expiry until useful work finishes.
- Forge and Cauldron have deep supporting systems. Crafting has rarity, affix, core, salvage, Ivan XP, autocrafting, telemetry, and upgrade scoring. Cauldron has mix inputs, stew, tasting odds, Eva XP, card tiers, buff/resource card effects, Infinity cards, and performance-focused services.
- Feedback systems already exist. Floating text, resource/skill tier popups, run graphs, run breakdowns, pinned quests, town attention indicators, taskbar flashing, music crossfades, task/combat SFX, and rich presence are present.
- Test structure supports low-risk iteration. Existing EditMode tests use `#if UNITY_INCLUDE_TESTS` and cover discrete logic around buffs, UI popups, leaderboards, naming, and real-time systems.

## Top Feature Opportunities

### 1. Run Contracts and Map Identity

What it adds to the player experience: Each run gets a concrete purpose beyond "go farther." The player can choose a map contract such as "Gather mining resources," "Survive kill-scaling swarms," "Meet a town NPC," or "Farm Forge cores," then see progress and rewards during the run.

Existing systems/files it would touch: `GameManager.cs` generation buttons and scaling mode, `MapGenerationConfig.cs`, `SegmentedMapGenerator.cs`, `ProceduralTaskGenerator.cs`, `GameplayStatTracker.cs`, `MapUI.cs`, `RunBreakdownManager.cs`, `RunResourceTrackerUI.cs`, quest data under `Assets/Resources/Quests`.

Complexity: Medium.

First shippable slice: Add a lightweight `RunContractData` ScriptableObject referenced by a map generation button or `MapGenerationConfig`. Track one contract type first, such as "complete N tasks of a chosen skill during this run," and show it on `MapUI` plus the run breakdown.

Risks/tradeoffs: Too many contract types would fragment the loop. Keep the first version data-driven and reuse existing stat events (`OnTaskCompletedEvent`, resource added, kill counts) instead of adding per-frame polling.

### 2. Quest-Driven Next Action and Town Cohesion

What it adds to the player experience: The game already has many quests, but the player needs a stronger sense of which town action or run choice advances the current story. A guided "next useful action" layer can connect quests, NPC meetings, task unlocks, Cauldron, Forge, buffs, and max-distance growth.

Existing systems/files it would touch: `QuestManager.cs`, `QuestData.cs`, `QuestTextFormatter.cs`, `PinnedQuestUIManager`, `QuestUIManager`, `TownWindowManager.cs`, `TalkToNpcTask.cs`, `ProceduralTaskGenerator.cs`, `LocationObjectStateController.cs`, `NpcObjectStateController.cs`.

Complexity: Medium.

First shippable slice: Add a quest helper model that classifies active quest requirements into actionable categories: run farther, gather resource, kill enemy, meet NPC, mix Cauldron, cast buff, complete tasks. Surface one "recommended next action" in the pinned quest area and, where possible, highlight the matching town button.

Risks/tradeoffs: Over-guidance can make an idle RPG feel linear. Keep it advisory and based on existing quest predicates rather than forcing navigation or changing task selection.

### 3. Gear Payoff and Build Identity

What it adds to the player experience: Forge already has a strong crafting engine, but gear can feel like stat math unless the player sees build identity emerge. Add named build goals, gear set bonuses, or run-facing stat callouts so Forge output visibly changes combat, task speed, survivability, and run strategy.

Existing systems/files it would touch: `GearItem.cs`, `CraftingService.cs`, `EquipmentController.cs`, `StatDefSO.cs`, `ForgeWindowUI.cs`, `ForgeResultPreview.cs`, `ScoreEvaluationService.cs`, `UpgradeEvaluator.cs`, `HeroStatSystem`, `HeroBase.cs`, `RunBreakdownManager.cs`.

Complexity: Medium to High, depending on set bonus depth.

First shippable slice: Add a "build summary" panel derived from equipped stat mappings: damage build, speed build, sustain build, crit build. Show the top changed run-facing values and include those same labels in the run breakdown when a new best run occurs.

Risks/tradeoffs: Full set bonuses add balance risk and save migration work. A read-only build summary gives player-facing payoff first without changing combat formulas.

### 4. Encounter Variety and Combat Readability

What it adds to the player experience: Combat is automatic, so enemy variety needs strong readable moments: elite packs, assist swarms, ranged threats, shielded enemies, and clear "why did I die or win" feedback. This would make watching the hero more dynamic without changing the core idle loop.

Existing systems/files it would touch: `Enemy.cs`, `EnemyData.cs`, `EnemyHealth.cs`, `EnemyKillTracker.cs`, `HeroCombatController.cs`, `HeroBase.cs`, `Combat.cs`, `MapUI.cs`, `RunBreakdownManager.cs`, enemy assets under `Assets/Resources/Enemies`.

Complexity: Medium.

First shippable slice: Add an elite modifier layer to spawned enemies using existing `EnemyData` plus a small runtime modifier component: increased health/damage, visible level/name label change, bonus drop weight, and a run breakdown count for elites defeated.

Risks/tradeoffs: Combat is already performance-sensitive because many enemies can be active. Prefer spawn-time modifiers and cached references over new per-frame scans. Avoid telegraphs that require PlayMode-heavy validation until elite data and UI feedback are proven.

### 5. Echo Command and Contribution Layer

What it adds to the player experience: Echoes are a strong fantasy but can be hard to evaluate. Give the player clearer control and feedback: what Echoes are active, what skill they are helping, how much they contributed, and when a milestone or buff caused them to appear.

Existing systems/files it would touch: `EchoController.cs`, `EchoManager.cs`, `MilestoneSpawnEchoEffectDefinition.cs`, `BuffManager.cs`, `BuffRecipe.cs`, `TaskController.cs`, `BaseTask.cs`, `Enemy.cs`, `AlterEchoGenerationManager.cs`, town Alter Echo UI.

Complexity: Medium.

First shippable slice: Track per-run Echo contribution counters for tasks completed, resources gained, combat kills/assists, and XP fraction. Show them in `RunBreakdownManager` or the Alter Echo window after each run.

Risks/tradeoffs: Attribution can get expensive if every drop/combat event searches actors. Use existing claim/source data where available (`BaseTask.ClaimedBy`, `Enemy.RegisterDamageSource`, Echo lists) and aggregate only at event boundaries.

### 6. Cauldron Collection Goals and Recipe Payoff

What it adds to the player experience: Cauldron has deep math but can become a throughput machine. Add visible collection goals, recipe achievements, and "next card target" payoff so tasting feels like building a deck/collection rather than only spending stew.

Existing systems/files it would touch: `CauldronManager.cs`, `CauldronConfig.cs`, `CardPoolManager.cs`, `CardTierCalculator.cs`, `TasteRollResolver.cs`, `CauldronWindowUI.cs`, Cauldron presenters, `QuestData.cs`, `QuestManager.cs`, `BuffRecipe.cs`, `AlterEchoGenerationManager.cs`.

Complexity: Medium.

First shippable slice: Add a Cauldron goals strip: nearest resource card tier, nearest buff card tier, and nearest group tier. Reuse `GetTierFill01`, `GetResourceTier`, `GetBuffTier`, and existing card count dictionaries.

Risks/tradeoffs: Cauldron already had significant allocation work. Keep UI refresh event-driven and throttled, and avoid rebuilding full card lists every frame.

### 7. Task Mastery and Profession Identity

What it adds to the player experience: The player repeatedly completes tasks, but completions mainly affect stats panels and spawn weighting. Turn task mastery into a visible progression track for each profession: unlock previews, mastery badges, task favorites, and profession-specific run goals.

Existing systems/files it would touch: `TaskData.cs`, `TaskWeightService.cs`, `TaskStatsPanelUI.cs`, `Skill.cs`, `SkillController.cs`, `ProceduralTaskGenerator.cs`, `ResourceGeneratingTask.cs`, resource/task assets under `Assets/Resources/Tasks`.

Complexity: Low to Medium.

First shippable slice: Extend the task stats UI to show mastery tier badges based on `TaskWeightService.GetCompletedTierCount`, and add a small bonus description for toggled tasks so the player understands why favoriting a task matters.

Risks/tradeoffs: Increasing spawn weights can distort resource balance. Start with clearer display and tiny non-economic bonuses, then tune task weight progression after observing run results.

### 8. Return-from-Away and Offline Summary

What it adds to the player experience: The code supports away-time handling and Alter Echo offline generation, but the player needs a strong return moment: what happened while away, what resources accrued, and what changed because the game was closed.

Existing systems/files it would touch: `Oracle.cs`, `EventHandler.cs`, `AlterEchoGenerationManager.cs`, `AlterEchoGeneratorUIManager.cs`, `ResourceManager.cs`, `GameplayStatTracker.cs`, `RunResourceTrackerUI.cs`, save data in `GameData.cs`.

Complexity: Medium.

First shippable slice: Add an offline summary window for Alter Echo generation only: elapsed away time, resources generated, best generator, and collect-all action. This avoids simulating full active runs while still honoring the idle promise.

Risks/tradeoffs: README language says offline runs continue while closed, but inspected runtime support appears centered on away-time events and Alter Echo generators. Full offline run simulation would be much riskier because it would need deterministic task, enemy, death, gear, quest, and map logic outside normal play.

### 9. Run HUD and Moment-to-Moment Feedback

What it adds to the player experience: The run HUD can explain the current action without breaking automation: current task, active threat, Echo count, active buffs, contract progress, and "why stopped" messages. This makes watching the hero feel authored rather than opaque.

Existing systems/files it would touch: `MapUI.cs`, `TaskController.cs`, `HeroBase.cs`, `HeroCombatController.cs`, `BuffUIManager.cs`, `RunBreakdownManager.cs`, `ResourceTierUpPopupUI.cs`, `FloatingText.cs`.

Complexity: Low to Medium.

First shippable slice: Add a compact current-action label fed by `TaskController.CurrentTaskObject`, hero combat state, and run loading state: "Mining Eznorb Ore," "Fighting Red Slime," "Retreat queued," "Searching for next task."

Risks/tradeoffs: HUD updates can become noisy. Follow the existing cache/throttle pattern used in `MapUI`, `GameManager.RefreshRunButtonsUI`, and recent UI performance changes.

### 10. Onboarding That Teaches the Real Loop

What it adds to the player experience: The intro screen currently gates closing with a timer, but it does not appear to teach the actual idle loop. A short contextual onboarding sequence can show the first run, first retreat, first quest turn-in, first Forge/Cauldron decision, and first Echo unlock.

Existing systems/files it would touch: `IntroScreenController.cs`, `QuestManager.cs`, pinned quest UI, `TownWindowManager.cs`, `GameManager.cs`, `ResourceTierUpPopupUI.cs`, `SettingsPanelUI.cs`.

Complexity: Low to Medium.

First shippable slice: Replace or supplement the generic first-run intro with a persistent "first objective" quest/pin that points to starting a run and returning to town once enough resources are earned.

Risks/tradeoffs: Hardcoded tutorial state can fight save data. Prefer quest-backed onboarding or PlayerPrefs flags that only affect UI prompts, not gameplay progression.

### 11. Audio and Biome Atmosphere Pass

What it adds to the player experience: Audio systems already support music, task clips, combat clips, chest clips, hero death, UI clicks, and crossfades. A focused pass can make maps and milestones feel less static: biome music, elite stingers, level-up/card tier cues, and town system ambience.

Existing systems/files it would touch: `AudioManager.cs`, `HeroAudio.cs`, `ProjectileHitSfx.cs`, `GameManager.cs`, `MapGenerationConfig.cs`, `ResourceTierUpPopupUI.cs`, `CauldronWindowUI.cs`, `ForgeWindowUI.cs`.

Complexity: Low.

First shippable slice: Add map-specific music selection to `MapGenerationConfig` or generation button data, then route `GameManager` run start through that track instead of hardcoded/default choices.

Risks/tradeoffs: Audio can become repetitive in an idle game. Add cooldowns or random variation where repeated system events can fire frequently.

## Suggested First Three Feature Slices

1. Run Contract MVP

Create one data-driven run contract type: complete N tasks of a selected skill during the current run. Display contract progress in `MapUI`, record completion in `GameplayStatTracker` or a small contract runtime, and show the result in `RunBreakdownManager`. This is the best first slice because it gives each run a purpose while reusing task completion events and existing run UI.

2. Current Action HUD

Add a compact run status label that shows the current task, combat target state, retreat queue, and loading/searching states. This improves the repeated watch loop immediately and helps validate whether task/combat interruptions are understandable before adding more features.

3. Cauldron Goals Strip

Add a low-allocation goals strip to the Cauldron window showing nearest resource card tier, nearest buff card tier, and nearest group tier. This gives a heavily developed system clearer player-facing motivation without changing balance.

## Validation Strategy

- Prefer Editor tests for pure logic and UI formatting:
  - Contract progress counters and completion rules.
  - Quest next-action classification.
  - Cauldron nearest-goal selection.
  - Task mastery tier display helpers.
  - Gear build-summary classification.
- Use PlayMode tests only when required for runtime object orchestration:
  - Hero task/combat state transitions.
  - Echo contribution attribution involving live `GameObject` actors.
  - Segment generation plus contract progress in an active map.
- Follow existing test patterns:
  - Wrap new test scripts in `#if UNITY_INCLUDE_TESTS` / `#endif`.
  - Keep tests under `Assets/Tests/EditMode` when they can operate on ScriptableObjects, services, and reflected UI helpers.
  - Use Unity MCP tooling to run tests in this project.
- Performance validation should be part of each feature slice:
  - Avoid new per-frame LINQ, `FindFirstObjectByType`, `GetComponent`, or full asset scans in runtime loops.
  - Reuse current event-driven patterns from `TaskStatsPanelUI`, `RunStatsPanelUI`, `MapUI`, and recent Forge/Cauldron throttling work.
  - For UI additions, cache last displayed values and update only on state changes or throttled intervals.
  - For spawn/combat additions, compute modifiers at spawn or event boundaries, not every frame.

## Open Questions

- Should the game promise full offline run simulation, or should the player-facing promise be reframed around Alter Echo/offline generation? The inspected code clearly supports away-time generators; full offline map runs would be a much larger feature.
- How many distinct map identities are intended beyond generation configs: biome, contract type, enemy scaling mode, resource focus, or story chapter?
- Should Echoes become a player-managed build pillar, or remain mostly automatic milestone/buff effects?
- Are Forge set bonuses desired, or should Forge remain a pure affix/stat optimization system with better summaries?
- Should quests be the main onboarding/guidance layer, or should tutorial prompts remain separate from quest progression?
- How much direct control should the player have during runs? Current architecture strongly favors automation, so new features should mostly improve choices before/after a run and readability during a run.
- Which system should own "run goals": `GameManager`, `GameplayStatTracker`, quest data, or a new small contract runtime? A small runtime referenced by `GameManager` and reported by `GameplayStatTracker` looks lowest risk.
