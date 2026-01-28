# Code Review Progress Tracker

**Started:** 2026-01-28
**Completed:** 2026-01-28
**Status:** COMPLETE

---

## Active Workstreams

| Workstream | Status | Agent | Started |
|------------|--------|-------|---------|
| 1. Core Systems | COMPLETE | Agent A | 2026-01-28 |
| 2. Economy & Progression | COMPLETE | Agent B | 2026-01-28 |
| 3. Quest & NPC | COMPLETE | Agent A | 2026-01-28 |
| 4. Infrastructure | COMPLETE | Agent B | 2026-01-28 |
| 5. UI & References | COMPLETE | Agent A | 2026-01-28 |
| 6. Map Generation | COMPLETE | Agent B | 2026-01-28 |

---

## Phase 1 Results

### Workstream 1: Core Systems - COMPLETE

**Key Dependencies Mapped:**
- HeroStatSystem → central stat authority
- Combat.ApplyDefense() → single source of truth for damage reduction
- HealthBase → common damage/death interface
- Tasks → generate resources via DropResolver
- EnemyKillTracker → kill-count bonuses feed HeroBase.Attack()

**Technical Debt Found:**

| File | Issue | Severity |
|------|-------|----------|
| HeroBase.cs | 1545 lines, mixes movement/combat/tasks/stats/UI | MEDIUM |
| HeroBase.cs | 14 bare `catch {}` blocks silently swallow exceptions | HIGH |
| ProceduralTaskGenerator.cs | 983 lines handling tasks/enemies/NPCs/terrain | MEDIUM |
| Health.cs | GetComponent in hot paths | LOW |
| HeroHealth.cs | pendingAttackerLevel field never used (dead code) | LOW |
| TaskData.cs | Persistent class stores runtime data on SO | MEDIUM |

**Pattern Inconsistencies:**
- 3 different singleton approaches used
- Health.cs should be renamed to EnemyHealth.cs
- Inconsistent prefixes (Hero vs Echo)

### Workstream 2: Economy & Progression - COMPLETE

**Formula Verification:** ALL MATCH DOCUMENTATION
- Defense formula verified in Combat.cs:28
- Movement speed verified in HeroStatSystem.cs:180-186
- Stew mixing verified in CauldronManager.cs:388-389
- Percentile system verified in StatRollMath.cs

**Tier Chain:** VERIFIED CONSISTENT (8 tiers, indices 0-7)
- All systems use Eznorb→Nori→Dlog→Erif→Lirium→Copium→Idle→Vastium

**Technical Debt Found:**

| File | Issue | Severity |
|------|-------|----------|
| CauldronManager.cs | 1297 lines - VERY LARGE | HIGH |
| CraftingService.cs | 180 lines of statistics tracking in Craft() | MEDIUM |
| SalvageService.cs | Duplicated statistics logic | MEDIUM |
| HeroStatSystem.cs | Static class with mutable state | MEDIUM |
| ResourceManager.cs | Add() has 6 parameters | LOW |

---

## Phase 2 Results

### Workstream 3: Quest & NPC - COMPLETE

**Requirement Types:** All 11 types implemented
- DistanceRun type exists but no quests use it

**Quest Chains Verified:**
- Skeleton questline (7 quests) - intact
- Mining ore unlock chain (7 quests) - intact
- Buff/Mildred chains - intact

**NPC Coverage:**
- Ivan1, OldTimer1, Witch1, Farmers1, Barkley1 - all active
- Gill has only 1 quest (possibly incomplete)
- Mildred has dedicated controller but no NPC ID in quests

**Technical Debt Found:**

| File | Issue | Severity |
|------|-------|----------|
| QuestEntryUI.cs | 130+ line ComputeRequirementProgress duplicates QuestManager | MEDIUM |
| PinnedQuestUIManager.cs | 140+ line UpdateProgress duplicates QuestManager | MEDIUM |
| "Rattle 'n' Roll.asset" | Special character in filename | LOW |
| QuestData.cs | No validation for questId uniqueness | MEDIUM |

### Workstream 4: Infrastructure - COMPLETE

**Save Data:** 50+ fields documented in GameData.cs
- Schema version currently 1
- Comprehensive backup rotation (snapshot + prev1 + prev2)
- Autosave every 30 seconds

**Migrations:**
- Migration_DuckHelmetSanitation (1.2.12)
- Migration_CauldronOverflowRedistribution (1.2.17)
- Migration_GearAffixQuality (1.2.17)

**Pooling:** Unity ObjectPool-based, working correctly
- Warning at 100 inactive (no auto-prune)

**Technical Debt Found:**

| File | Issue | Severity |
|------|-------|----------|
| GameData.cs | No max collection sizes - potential memory bloat | MEDIUM |
| SaveManager.cs | Sync GetAwaiter().GetResult() in async ops | MEDIUM |
| PoolManager.cs | No max pool size limit | MEDIUM |
| UgsLeaderboardIds.cs | Typo: "Completion_TIme" | LOW |
| SilentAuthBootstrap.cs | Duplicates UgsInitializer logic | LOW |

---

## Phase 3 Results

### Workstream 5: UI & References - COMPLETE

**References Pattern:** GOOD PATTERN - NOT DEBT
- MonoBehaviour classes with SerializeFields for UI elements
- Separates data binding from logic
- Prefab-friendly, avoids FindChild() calls

**24 Reference Classes Documented:**
- StatPanel/ (5) - Panel entry references
- UI/ (19) - General UI element references
- Gear/UI/ (3) - Forge-specific references

**Large UI Classes Found:**

| File | Lines | Issue |
|------|-------|-------|
| SettingsPanelUI.cs | ~1130 | Too many responsibilities |
| CollectionsWindowUI.cs | ~900 | Section management extractable |
| CauldronWindowUI.cs | ~755 | Mixing/drinking separable |
| ForgeWindowUI.cs | ~750 | Conversion logic extractable |
| LeaderboardsPanelUI.cs | ~720 | Pool management extractable |

**Pattern Issues:**
- Namespace inconsistency: `References.UI` vs `TimelessEchoes.References.StatPanel`
- Some References classes contain logic (ResourceUIReferences, SkillUIReferences)

### Workstream 6: Map Generation - COMPLETE

**Generation Flow Documented:**
1. SegmentedMapGenerator orchestrates
2. TilemapChunkGenerator creates terrain tiles
3. ProceduralTaskGenerator spawns tasks/enemies/NPCs
4. 3-segment queue with streaming replacement

**CRITICAL ISSUE FOUND:**
- **Haloween.asset** has all task categories empty - NO TASKS SPAWN

**Map Config Audit:**

| Map | Status | Notes |
|-----|--------|-------|
| Beach | Complete | No enemies (intentional?) |
| Farmlands | Complete | Mining disabled |
| River | Complete | Fishing-focused |
| Woods | Complete | Woodcutting-focused |
| Mines | Complete | Mining+Looting only |
| Haloween | BROKEN | Empty task lists! |

**Technical Debt:**

| File | Issue | Severity |
|------|-------|----------|
| ProceduralTaskGenerator.cs | 983 lines - very large | HIGH |
| TilemapChunkGenerator.cs | Duplicate sync/async logic | MEDIUM |
| "Haloween" spelling | Should be "Halloween" | LOW |

---

## Issues Found

### Critical
| File | Issue |
|------|-------|
| Haloween.asset | All task categories have empty task lists - NO TASKS SPAWN |

### Major
| File | Issue |
|------|-------|
| HeroBase.cs | 1545 lines mixing concerns + 14 bare catch blocks |
| CauldronManager.cs | 1297 lines - very large class |
| ProceduralTaskGenerator.cs | 983 lines handling multiple concerns |
| Progress calculation | Duplicated across 4 quest files |

### Minor
- Root-level clutter: 15 files in Assets/Scripts/ root
- Empty Migration directory exists
- Inconsistent singleton patterns (3 approaches)
- Pooling logic split between directories
- Lowercase "logging" folder naming
- Health.cs should be renamed EnemyHealth.cs
- DistanceRun requirement type unused
- Special character in quest filename

---

## Documentation Updates Completed

### CLAUDE.md - UPDATED
- [x] Added missing directories (Audio, Editor, Localization, NPC, Platform, References, Stats, Tools, Utilities)
- [x] Documented root-level files (15 files)
- [x] Added Architecture Patterns section (Singleton, Service, References patterns)
- [x] Added full directory structure with file counts
- [x] Added NPC IDs and quest folder counts
- [x] Added Maps table with status (including Halloween BROKEN warning)
- [x] Added Quest Requirement Types table
- [x] Added Key Files Reference table

### EoV_Context.md - UPDATED
- [x] Added Codebase Statistics section
- [x] Added Milestone Effect Types table (9 types)
- [x] Added Design Patterns section
- [x] Expanded Save Data schema with categories and migrations
- [x] Added Map Generation System section
- [x] Added Known Technical Debt section (Critical/Major/Medium/Minor)

---

## Completed Reviews

*(None yet)*

---

## Notes

- Total C# Files: 289
- Total Directories: 32
- ScriptableObject Classes: 27
- Singleton/Manager Classes: 19+
