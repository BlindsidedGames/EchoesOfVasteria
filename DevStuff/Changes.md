- Completed Phase 3 of HeroBase refactoring: created `HeroMovementController.cs` to encapsulate movement logic (pathfinding, animation sync, destination management, idle walking), and updated HeroBase to delegate all movement operations to the new component.
- Completed Phase 2 of HeroBase refactoring: created `HeroCombatController.cs` to encapsulate combat logic (target tracking, DPS estimation, attack execution, dice rolling), and updated HeroBase to delegate all combat operations to the new component.
- Completed Phase 1 of HeroBase refactoring: created `EnemyEngagementTracker.cs` to encapsulate enemy tracking collections (engagedEnemies, deathHandlers, disengageHandlers, enemyTargets), extracted subscription management to the tracker, and updated HeroBase to delegate all engagement operations to the new component.
- Added `DevStuff/EoV_New_Employee_Systems_Overview.md` with a code-verified systems overview, corrected naming, and clarified progression gates.
- Updated return/retreat UI so the return button shows "Queue Retreat" while in combat and the retreat bonus label keeps displaying resource bonuses.
- Added a Cauldron drinking mix-all button that pairs every stocked ingredient batch and refreshes the selections after draining them.
- Blocked BuffManager auto-casting while the run loading overlay is active by exposing GameManager.RunLoadingActive so buffs wait for initial map generation to finish.
- Added MCP read_console guidance to `AGENTS.md` so log retrieval always uses `action: \"get\"` instead of the invalid `\"read\"`.
- Added SegmentedMapGenerator warmup progress/wait hooks, forced a TaskController resort, and wired new GameManager loading overlay/progress fields so hero/buff activation waits until the first map chunks and tasks finish spawning.
- Build All flows now restore the active build target back to Linux once the sequence completes so the editor stays on our primary platform.
- Split Build/Build All into dedicated Full, Demo, and Beta menu items, ensuring each button sets the shared BuildModeConfig flags up front before running the Linux→Windows→Mac build sequence.
- Removed the Forge stats Quality Equipped block so the panel only shows the best rolled values per slot.
- Beta builds now skip the demo variant so the Build All Beta flow only authorizes the full SKU per platform.
- Moved the demo/beta toggles into a `BuildModeConfig` ScriptableObject, updated Oracle/tests to read from it, and reworked BatchBuild to sequence Linux→Windows→Mac full/demo builds (plus a Beta menu option) into dedicated release/demo/beta output folders.
- Cleared the A* upgrade warnings by swapping our AIBase movement toggles to `simulateMovement`, moving Hero idle target resolution to `NearestNodeConstraint`, and fixing the unreachable cache hydration check in `UgsLeaderboardsReporter`.
- Updated pinned quest completion text to read "Ready to turn in" so UI messaging matches the new copy requirements.
- Corrected skill level-up toasts to show the full level range during multi-level gains, added synchronous edit-mode coverage, and wired EditMode.Tests to the test runner/TextMeshPro assemblies.
- Added raw XP developer console commands for each skill (and all skills) plus the developer login password update to "Matt", tightening console lookups to avoid ambiguous reference compile errors.
- Added quest skill experience rewards with raw XP application and edit-mode coverage for the quest grant path.
- Added `savesHiddenObject` handling in `GameManager` so the companion UI enables when saves are hidden during runs.
- Reworked UGS leaderboards around completion time and total tasks, backfilled quest completion data for existing saves, flagged console/fraudulent scores to a cheater board while still tracking distance reached, and added editor tests around the new fraud heuristics.
- Preserved real-time systems under Slipstream by moving Oracle playtime, Alter-Echo ticking, GameplayStatTracker run timing, death window countdowns, tier-up toast timers, and run breakdown refreshes to unscaled seconds with new edit-mode coverage for playtime and Alter-Echo generation.
- Added a max-distance toggle to TaskData so disabled entries act infinite and the Task stats panel hides max distance details.
- Added task stat weight multipliers with persistent double toggles, category-based spawn chance previews (skill-only in town, map-weighted during runs), distance slider integration, per-task weight display, and min/max distance readouts for the Tasks tab UI.
- Noted in `Agents.md` that Unity tests must be run through MCP tooling.
- Added a Bonus Experience buff effect with SkillController integration, editor coverage, and a new BuffRecipe unlocked alongside task/combat echoes.
- Documented `Agents.md` test guidance so new Unity test scripts stay inside `#if UNITY_INCLUDE_TESTS` editor-only guards.
- Added a developer console command to override BuffManager's base timescale, recalibrated baseline physics scaling, and expanded Slipstream tests to cover the override.
- Excluded test assemblies from player builds by gating their asmdefs behind UNITY_INCLUDE_TESTS, wrapping the test scripts in the same define, and keeping them Editor-only.
- Removed Android AAB support from the Build All editor menu and cleaned out the unused command-line batching code.
- Reworked Slipstream to use configurable time-scale and distance effects, added supporting buff logic/tests, reset timescale handling in BuffManager, and trimmed duplicate distance text from the buff tooltip.
- Isolated beta save slots by letting Oracle use `Beta{iteration}Save#` directories, clamping the iteration toggle beside the new Beta flag, and covering the naming helpers with edit-mode tests.
- Documented MCP tool capabilities in `Agents.md`, removed the Unity test warning, added guidance to avoid editing scripts through MCP, and noted that relevant Editor tests should be preferred over PlayMode tests.
- Updated AGENTS guidelines with a documentation exception for plan approvals and refreshed the documentation standards to emphasise concise, professional summaries without referencing specific subsystems.
- Documented hero echo scripts with XML summaries, parameter notes, and tooltips to clarify behaviour, pooling, and cap logic.
- Added Android AAB build support to Build All and menu action to increment Android/iOS bundle versions.
- Floating XP popups now reflect milestone experience bonuses by returning the applied XP from SkillController.
- Added configurable milestone experience bonuses with aggregator/controller support, a reusable effect asset, and UI lines for global and per-skill XP.
- Prevented the cauldron attention indicator and taskbar flash from firing before an auto-started session has actually run.
- Hid milestone activation toggles and active descriptions until the player unlocks active slots for a skill.
- Added per-skill task speed lines to the skills totals panel, excluding the combat skill bonus.
- Added a first-run intro screen controller that gates closing for 10 seconds, toggles prompts, and persists completion via PlayerPrefs.
- Reworked forge actions so the craft button becomes Stop during auto-crafting, pauses the auto-run button, and keeps the latest craft available for Replace after cancelling.
- Ensured forge pending vs equipped arrows compare formatted values so identical stats display as equals.
- Added a cauldron taste milestone bonus that spends extra stew each roll and multiplies card rewards accordingly.
- Removed legacy Input Manager fallbacks from RunBreakdownManager so right-click detection always uses the new Input System without throwing InvalidOperationException.
- Synced RunBreakdownManager with in-progress runs and exposed current run elapsed time so map UI toggles stay accurate when reactivated.
- Normalized RunBreakdownManager formatting so Start/Update use proper newlines.
- Removed RunDropUI and its GameManager hooks in favor of the run breakdown tracker so Map UI stays active on run start.
- Added right-click shortcut to close the run breakdown window instantly.
- Added run summary labels for distance per minute, damage per second, and kills per minute in the breakdown window.
- Sorted run breakdown resource entries by resourceID and synced UI insertion to match the sorted order.
- Rerouted retreat bonus payouts to the RunResourceTracker data so early returns grant and display bonus resources without RunDropUI.
- Grouped milestone totals by category, appended spawning skill context to echo entries, and renamed combat triggers to "when Killing".
### Added
- Run breakdown entry and manager scripts for pooled per-resource run summaries with retreat projections and runtime tracking.
- Localization manager that loads the player's saved locale, defaults to English, and exposes UI-facing APIs for manual language swaps.
- Introduced a milestone resource bonus effect with aggregator support and resource drop/UI integration.
- Display Settings and Leaderboards now share the autogenerated leaderboard name fallback when no custom display name is stored.

### Changed
- Added death window messaging to display "You were reaped..." versus "You have Died..." and clear when the window closes.
- Hooked QuestEntryUI noticeboard text into LocalizeStringEvent components so locale changes refresh existing entries.
- Switched the Halloween leaderboards and stat panels to track per-run kill totals instead of distance.

### Fixed
- Persisted localization settings by storing the locale code explicitly and broadening lookup when reloading.

### Removed
- ForceEnglishLocale bootstrap script that enforced an English locale before scenes loaded.





- Gated task completion weight bonuses behind the task toggle so disabled tasks stay at base weight while enabled tasks get the tier bonus before the 2x multiplier.
- Added `DevStuff/HeroBaseRefactor.md` with comprehensive analysis of HeroBase (1545 lines), its inheritance hierarchy (HeroController, EchoController), 14 bare catch blocks, and a phased extraction plan for combat/movement/engagement systems.
- Updated `DevStuff/HeroBaseRefactor.md` with detailed implementation phases: Phase 0 (DRY utilities, rename Health→EnemyHealth, expose Enemy.Health), Phase 1-3 (extract engagement/combat/movement controllers), and testing checklists.
- Implemented Phase 0 of HeroBase refactoring: created `UnityObjectExtensions.cs` (TryGetTransformSafe), `AnimatorMovementHelper.cs` (cached animator hashes), renamed Health.cs to EnemyHealth.cs, exposed Enemy.Health and Enemy.PooledMarker properties, replaced 14 bare try-catch blocks with safe extension methods, and updated animator calls across HeroBase, Enemy, MildredMovementController, and AnimalDecorationController.
