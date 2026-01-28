# HeroBase Refactoring Analysis

**Created:** 2026-01-28
**Status:** Phase 3 Complete ✓

---

## Executive Summary

`HeroBase.cs` is a 1545-line abstract class that serves as the foundation for both the main hero (`HeroController`) and echo clones (`EchoController`). The class currently violates the Single Responsibility Principle by combining movement, combat, task management, stats, UI updates, and several other concerns into one monolithic file.

This document provides a detailed analysis of the current structure, identifies specific issues, and proposes a refactoring strategy that preserves the existing inheritance hierarchy while improving maintainability.

---

## Current Inheritance Hierarchy

```
MonoBehaviour
    └── HeroBase (abstract, 1545 lines)
            ├── HeroController (137 lines) - Main hero singleton
            └── EchoController (506 lines) - Echo/clone instances
```

### Related Systems

| System | Type | Lines | Purpose |
|--------|------|-------|---------|
| HeroStatSystem | Static class | 252 | Cached stat calculation |
| HeroHealth | MonoBehaviour | 173 | Health, damage, death |
| HeroStats | ScriptableObject | 13 | Vision range, projectile prefab |
| EchoManager | Static class | 195 | Echo spawning utility |

---

## Responsibility Analysis

### Current Responsibilities in HeroBase

| Responsibility | Lines (approx) | Methods | Could Be Extracted? |
|---------------|----------------|---------|---------------------|
| **Combat System** | ~350 | 12 | YES - High Priority |
| **Movement/Pathfinding** | ~150 | 8 | YES - Medium Priority |
| **Enemy Engagement Tracking** | ~180 | 5 | YES - High Priority |
| **Task Orchestration** | ~80 | 3 | Partial - Keep interface |
| **Stat Properties** | ~100 | 8 | NO - Delegates to HeroStatSystem |
| **Animation** | ~60 | 4 | YES - Low Priority |
| **Dice Rolling** | ~30 | 1 | YES - Low Priority |
| **Reaper Spawning** | ~30 | inline | YES - Low Priority |
| **HUD/Rich Presence** | ~20 | 2 | YES - Low Priority |
| **Lifecycle (Awake/Enable/etc)** | ~200 | 6 | NO - Must stay |
| **Virtual Hook Methods** | ~50 | 10 | NO - Extension points |

### Detailed Method Inventory

#### Combat Methods (HIGH PRIORITY)
```csharp
// DPS/TTK estimation
- EstimateDpsAgainst(Enemy, HeroBase)         // 430-444
- EstimatePerHitAgainst(Enemy, HeroBase)      // 453-464
- EstimateCombinedDps(Transform)              // 466-511

// Target selection
- FindNearestEnemyTimeAware(float, float)     // 513-579
- FindNearestEnemy(float)                     // 581-631
- FindNearestEnemy()                          // 633-636
- ResolveNearestEnemyTarget()                 // 1068-1136
- FindFallbackEnemyTarget()                   // 1190-1221
- TryEngageEnemy(Transform)                   // 1138-1188

// Combat execution
- HandleCombat(Transform)                     // 638-688
- Attack(Transform)                           // 846-894
- RollForCombat(float)                        // 690-709 (coroutine)
```

#### Enemy Engagement Tracking (HIGH PRIORITY)
```csharp
// Tracking collections (fields)
- engagedEnemies: HashSet<Enemy>
- enemyDeathHandlers: Dictionary<Enemy, Action>
- enemyDisengageHandlers: Dictionary<Enemy, Action<Enemy>>
- enemyRemovalBuffer: List<Enemy>
- enemyTargets: Dictionary<Enemy, Transform>

// Methods
- OnEnemyEngage(Enemy)                        // 711-808
- UnregisterEngagedEnemy(Enemy)               // 810-843
```

#### Movement Methods (MEDIUM PRIORITY)
```csharp
- UpdateAnimation()                           // 1356-1394
- SetActiveState(bool)                        // 1397-1409
- SetDestination(Transform)                   // 1411-1416
- SetDestinationReached()                     // 1418-1421
- IsAtDestination(Transform)                  // 1423-1431
- AutoAdvance()                               // 1433-1480
- ResolveIdleAdvanceTarget(Vector3)           // 1482-1527
```

---

## Critical Issues

### 1. Bare Catch Blocks (14 occurrences)

These silently swallow exceptions, making debugging extremely difficult:

| Line | Code Pattern | Risk |
|------|--------------|------|
| 430 | `try { et = enemy.transform; } catch { et = null; }` | Hides destroyed object access |
| 483 | `try { t = main.currentEnemy; } catch { t = null; }` | Hides null reference |
| 484 | `try { st = main.setter != null ? ... } catch { st = null; }` | Hides null reference |
| 501 | `try { t = hc.currentEnemy; } catch { t = null; }` | Hides null reference |
| 502 | `try { st = hc.setter != null ? ... } catch { st = null; }` | Hides null reference |
| 544 | `try { enemyTransform = enemy.transform; } catch { continue; }` | Skips destroyed enemies |
| 609 | `try { enemyTransform = enemy.transform; } catch { continue; }` | Skips destroyed enemies |
| 730 | `try { ... dst.target ... } catch { engagedTarget = null; }` | Hides component access |
| 745 | `try { et = enemy.transform; } catch { et = null; }` | Hides destroyed object |
| 800 | `try { currentEnemyComp = enemy; } catch { ... = null; }` | Hides assignment failure |
| 835 | `try { enemyTransformSafe = enemy.transform; } catch { ... = null; }` | Hides destroyed object |
| 1086 | `try { enemyTransform = enemy.transform; } catch { continue; }` | Skips destroyed enemies |
| 1120 | `try { nearest = chosen.transform; } catch { nearest = null; }` | Hides destroyed object |
| 1205 | `try { enemyTransform = enemy.transform; } catch { continue; }` | Skips destroyed enemies |

**Root Cause:** Most of these guard against accessing `.transform` on potentially destroyed Unity objects.

**Recommended Fix:** Create a helper extension method:
```csharp
public static bool TryGetTransformSafe(this Component comp, out Transform t)
{
    t = null;
    if (comp == null) return false;
    try { t = comp.transform; return t != null; }
    catch (MissingReferenceException) { return false; }
}
```

### 2. State Machine Complexity

The `State` enum has only 4 values but the transitions are scattered throughout `UpdateBehavior()`:

```csharp
private enum State { Idle, MovingToTask, PerformingTask, Combat }
```

State transitions happen in:
- `HandleCombat()` - sets Combat
- `UpdateBehavior()` - sets Idle, MovingToTask, PerformingTask
- `OnEnable()` - resets to Idle
- `SetTask()` - resets to Idle

Consider a formal state machine pattern for clearer transitions.

### 3. Echo vs Hero Branching

The `IsEchoActor` property creates many conditional branches:

```csharp
if (!IsEchoActor) { ... }           // 13 occurrences
if (IsEchoActor) { ... }            // 7 occurrences
if (IsEchoActor && ...) { ... }     // 4 occurrences
```

This is acceptable since it's the core differentiation mechanism, but some of these branches could be moved to virtual methods.

### 4. Field Pollution

Many fields are only used by specific subsystems and could be encapsulated:

**Combat-only fields:**
```csharp
private Transform currentEnemy;
private Health currentEnemyHealth;
private Enemy currentEnemyComp;
private readonly HashSet<Enemy> engagedEnemies = new();
private readonly Dictionary<Enemy, Action> enemyDeathHandlers = new();
private readonly Dictionary<Enemy, Action<Enemy>> enemyDisengageHandlers = new();
private readonly List<Enemy> enemyRemovalBuffer = new();
private readonly Dictionary<Enemy, Transform> enemyTargets = new();
private float lastAttack = float.NegativeInfinity;
private bool isRolling;
private float combatDamageMultiplier = 1f;
```

**Movement-only fields:**
```csharp
private Vector2 lastMoveDir = Vector2.down;
private Transform idleWalkTarget;
private bool destinationOverride;
```

**Dice-only fields:**
```csharp
[SerializeField] private DiceRoller diceRoller;
[SerializeField] private string diceQuestID = "Protect the Town";
private bool diceUnlocked;
```

---

## Echo Safety Analysis

### What Makes Echoes Different

| Aspect | HeroController | EchoController |
|--------|----------------|----------------|
| `IsEchoActor` | `false` | `true` |
| Singleton | Yes (`Instance`) | No (list-tracked) |
| Lifetime | Persistent | Timed (pooled) |
| Task Assignment | Via TaskController | Via `capableSkills` filter |
| Combat | Always enabled | `combatEnabled` flag |
| Health | Own `HeroHealth` | Forwards to main hero |
| Stat System | Initializes `HeroStatSystem` | Uses shared snapshot |
| Visuals | AutoBuff animator | Skill indicators |

### Critical Echo Behaviors to Preserve

1. **Echo Static Lists:**
   ```csharp
   public static readonly List<EchoController> CombatEchoes = new();
   public static readonly List<EchoController> AllEchoes = new();
   ```
   These are used by `EstimateCombinedDps()` to calculate TTK and avoid overkill.

2. **Echo Damage Forwarding:**
   In `HeroHealth.TakeDamage()`:
   ```csharp
   if (controller.IsEcho && Instance != null && Instance != this)
   {
       Instance.TakeDamage(amount * 0.5f, bonusDamage, isCritical);
       return;
   }
   ```

3. **Echo Expiration Deferral:**
   `EchoController.BeginExpirationDeferral()` allows echoes to finish current combat/task before despawning.

4. **TaskController Nullification:**
   Combat-only echoes call `ClearTaskController()` to prevent task assignment.

5. **TTK-Aware Targeting:**
   `FindNearestEnemyTimeAware()` uses `EchoAvoidIfTTKBelowSeconds` to prevent echoes from "stealing" kills.

---

## Proposed Refactoring Strategy

### Phase 1: Extract Combat System (HIGH IMPACT)

Create `HeroCombatController.cs`:
```
HeroCombatController
├── Target selection (FindNearestEnemy*, TryEngageEnemy)
├── DPS estimation (EstimateDpsAgainst, EstimateCombinedDps)
├── Attack execution (Attack, HandleCombat)
├── Dice rolling (RollForCombat)
└── Enemy engagement tracking (engagedEnemies, handlers)
```

**Interface:**
```csharp
public class HeroCombatController : MonoBehaviour
{
    // Called by HeroBase.UpdateBehavior()
    public bool TryAttack();
    public Transform GetCurrentTarget();
    public bool IsInCombat { get; }

    // Echo TTK coordination
    public float EstimateCombinedTTK(Transform enemy);
}
```

### Phase 2: Extract Movement System (MEDIUM IMPACT)

Create `HeroMovementController.cs`:
```
HeroMovementController
├── Animation sync (UpdateAnimation)
├── Auto-advance logic (AutoAdvance, ResolveIdleAdvanceTarget)
├── Destination management
└── Idle walk target handling
```

**Keeps on HeroBase:**
- `SetActiveState()` - toggles both systems
- `SetDestination()` - delegates to movement controller

### Phase 3: Clean Up Bare Catches (QUICK WIN)

Replace all 14 bare catches with:
```csharp
// Extension method in Utilities/
public static class UnityObjectExtensions
{
    public static bool TryGetTransformSafe(this Component comp, out Transform t)
    {
        t = null;
        if (comp == null) return false;
        try
        {
            t = comp.transform;
            return t != null;
        }
        catch (MissingReferenceException)
        {
            return false;
        }
    }
}

// Usage:
if (enemy.TryGetTransformSafe(out var enemyTransform))
{
    // safe to use
}
```

### Phase 4: Formalize State Machine (OPTIONAL)

Create explicit state handlers:
```csharp
interface IHeroState
{
    void Enter(HeroBase hero);
    void Update(HeroBase hero);
    void Exit(HeroBase hero);
}

class IdleState : IHeroState { ... }
class CombatState : IHeroState { ... }
class MovingToTaskState : IHeroState { ... }
class PerformingTaskState : IHeroState { ... }
```

---

## Refactoring Risks

### High Risk Areas

| Area | Risk | Mitigation |
|------|------|------------|
| Echo combat participation | Breaking combined DPS calculation | Keep `EchoController.CombatEchoes` accessible |
| Enemy engagement events | Missing event subscriptions | Extract as cohesive unit |
| Task interruption | Combat not preempting tasks | Preserve `CurrentTask.OnInterrupt()` call |
| Dice multiplier | Not applied to echoes correctly | `combatDamageMultiplier` already per-instance |

### Testing Checklist

- [ ] Main hero attacks enemies correctly
- [ ] Echoes participate in combat without stealing kills
- [ ] TTK estimation prevents overkill
- [ ] Task interruption works when enemy engages
- [ ] Dice roll applies multiplier correctly
- [ ] Enemy death/disengage cleanup works
- [ ] Reaper spawns at correct distance
- [ ] Echo expiration deferral works
- [ ] Combat indicators update correctly

---

## Recommended Approach

### Order of Operations

1. **Create helper extension** for safe transform access (fixes all bare catches)
2. **Extract enemy engagement tracking** to `EnemyEngagementTracker` component
3. **Extract combat logic** to `HeroCombatController` component
4. **Extract movement logic** to `HeroMovementController` component
5. **Refactor HeroBase** to orchestrate the extracted components

### Composition Pattern

```csharp
// After refactoring
[RequireComponent(typeof(HeroCombatController))]
[RequireComponent(typeof(HeroMovementController))]
[RequireComponent(typeof(AIPath))]
public abstract class HeroBase : MonoBehaviour
{
    protected HeroCombatController combat;
    protected HeroMovementController movement;

    // Orchestration only - no direct combat/movement logic
    protected virtual void UpdateBehavior()
    {
        if (combat.TryAttack())
            return;

        if (CurrentTask != null)
            HandleTask();
        else
            movement.AutoAdvance();
    }
}
```

### Estimated Line Counts After Refactor

| File | Current | Projected |
|------|---------|-----------|
| HeroBase.cs | 1545 | ~600 |
| HeroCombatController.cs | 0 | ~400 |
| HeroMovementController.cs | 0 | ~200 |
| EnemyEngagementTracker.cs | 0 | ~200 |
| HeroController.cs | 137 | ~150 |
| EchoController.cs | 506 | ~520 |

---

## Dependencies to Preserve

### External Systems That Reference HeroBase

```csharp
// These access HeroBase.Damage, HeroBase.Defense, etc.
- Projectile.cs (damage calculation)
- HeroHealth.cs (defense lookup)
- Enemy.cs (target reference)

// These access HeroController.Instance
- EchoManager.cs
- HeroStatSystem.cs
- BuffManager.cs
- Many UI classes

// These access EchoController static lists
- HeroBase.EstimateCombinedDps()
- EchoManager.EnforceTypeCap()
```

### Events That Must Remain

```csharp
// HeroBase
public static event Action OnMainHeroDiceChanged;

// HeroStatSystem
public static event Action<HeroStatsSnapshot> OnStatsRecalculated;

// Enemy (external)
public static event Action<Enemy> OnEngage;
```

---

## Implementation Phases

### Phase 0: Performance & DRY Foundation (Do First)

**Goal:** Quick wins that reduce code before larger refactoring.

#### 0.1 Create Utility Extensions
Create `Assets/Scripts/Utilities/UnityObjectExtensions.cs`:
```csharp
using UnityEngine;

namespace TimelessEchoes.Utilities
{
    public static class UnityObjectExtensions
    {
        /// <summary>
        /// Safely get transform from a component that may have been destroyed.
        /// </summary>
        public static bool TryGetTransformSafe(this Component comp, out Transform t)
        {
            t = null;
            if (comp == null) return false;
            try { t = comp.transform; return t != null; }
            catch (MissingReferenceException) { return false; }
        }
    }
}
```

#### 0.2 Create AnimatorMovementHelper
Create `Assets/Scripts/Utilities/AnimatorMovementHelper.cs`:
```csharp
using UnityEngine;

namespace TimelessEchoes.Utilities
{
    public static class AnimatorMovementHelper
    {
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int MoveMagnitudeHash = Animator.StringToHash("MoveMagnitude");

        public static void SetMovement(Animator anim, Vector2 direction, float speed)
        {
            if (anim == null) return;
            anim.SetFloat(MoveXHash, direction.x);
            anim.SetFloat(MoveYHash, direction.y);
            anim.SetFloat(MoveMagnitudeHash, speed);
        }

        public static void ClearMovement(Animator anim)
        {
            SetMovement(anim, Vector2.zero, 0f);
        }

        public static Vector2 SnapToCardinal(Vector2 dir, bool fourDirectional)
        {
            if (!fourDirectional) { dir.y = 0f; return dir; }
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y)) dir.y = 0f;
            else dir.x = 0f;
            return dir;
        }
    }
}
```

#### 0.3 Expose Enemy.Health Property
In `Assets/Scripts/Enemies/Enemy.cs`:
```csharp
// Add public accessor (line ~83, after Stats property):
public Health Health => health;

// Add PooledObject cache in SetupInitialInstance():
public PooledObject PooledMarker { get; private set; }

// In SetupInitialInstance() after health = GetComponent<Health>():
PooledMarker = GetComponent<PooledObject>();
```

#### 0.4 Rename Health.cs to EnemyHealth.cs
1. In `Assets/Scripts/Enemies/Health.cs`:
   - Change `public class Health : HealthBase` → `public class EnemyHealth : HealthBase`
   - Cache Enemy reference in Awake instead of GetComponent per call
2. Rename file: `Health.cs` → `EnemyHealth.cs` (keep .meta file)
3. Update all references (22 occurrences across 7 files)

#### 0.5 Simplify EnemyHealth.TakeDamage
Remove duplicated logic - only override what's different:
```csharp
public class EnemyHealth : HealthBase
{
    private Enemy _enemy;

    protected override void Awake()
    {
        _enemy = GetComponent<Enemy>();
        base.Awake();
    }

    protected override float CalculateDamage(float fullDamage)
    {
        var defense = _enemy != null ? _enemy.GetDefense() : 0f;
        return Combat.ApplyDefense(fullDamage, defense);
    }

    // Keep: SetHealthBarVisible, HandleHealthChanged, OnZeroHealth (death SFX)
    // Remove: TakeDamage override (let base handle it)
}
```

#### 0.6 Update HeroBase to Use New Utilities
- Replace 14 try-catch blocks with `TryGetTransformSafe()`
- Replace animator SetFloat calls with `AnimatorMovementHelper.SetMovement()`
- Replace `enemy.GetComponent<Health>()` with `enemy.Health`
- Replace `enemy.GetComponent<PooledObject>()` with `enemy.PooledMarker`

#### 0.7 Update Other Files Using AnimatorMovementHelper
- Enemy.cs (3 locations)
- MildredMovementController.cs (1 location)
- AnimalDecorationController.cs (1 location)

**Files Modified in Phase 0:**
- NEW: `Utilities/UnityObjectExtensions.cs`
- NEW: `Utilities/AnimatorMovementHelper.cs`
- RENAME: `Enemies/Health.cs` → `Enemies/EnemyHealth.cs`
- MODIFY: `Enemies/Enemy.cs` (expose Health, PooledMarker)
- MODIFY: `Hero/HeroBase.cs` (use utilities, cached refs)
- MODIFY: `Hero/EchoController.cs` (use EnemyHealth)
- MODIFY: `Tasks/TaskController.cs` (use EnemyHealth)
- MODIFY: `Tasks/KillEnemyTask.cs` (use EnemyHealth)
- MODIFY: `Steamworks.NET/SlimeCombatTracker.cs` (use EnemyHealth)
- MODIFY: `NPC/MildredMovementController.cs` (use AnimatorMovementHelper)
- MODIFY: `NPC/AnimalDecorationController.cs` (use AnimatorMovementHelper)

---

### Phase 1: Extract Enemy Engagement Tracker

**Goal:** Move engagement tracking out of HeroBase into dedicated component.

#### 1.1 Create EnemyEngagementTracker
Create `Assets/Scripts/Hero/EnemyEngagementTracker.cs`:
```csharp
public class EnemyEngagementTracker : MonoBehaviour
{
    // Move from HeroBase:
    private readonly HashSet<Enemy> engagedEnemies = new();
    private readonly Dictionary<Enemy, Action> enemyDeathHandlers = new();
    private readonly Dictionary<Enemy, Action<Enemy>> enemyDisengageHandlers = new();
    private readonly List<Enemy> enemyRemovalBuffer = new(16);
    private readonly Dictionary<Enemy, Transform> enemyTargets = new();

    // Public API
    public IReadOnlyCollection<Enemy> EngagedEnemies => engagedEnemies;
    public bool HasEngagedEnemies => engagedEnemies.Count > 0;

    public void RegisterEnemy(Enemy enemy, Transform target);
    public void UnregisterEnemy(Enemy enemy);
    public void CleanupStaleEnemies();
    public Enemy GetNearestEngaged(Vector2 position, float maxDistance);
}
```

#### 1.2 Update HeroBase
- Add `[RequireComponent(typeof(EnemyEngagementTracker))]`
- Replace direct collection access with tracker API
- Move `OnEnemyEngage` and `UnregisterEngagedEnemy` to tracker

**Estimated reduction:** ~180 lines from HeroBase

---

### Phase 2: Extract Combat Controller

**Goal:** Move combat logic into dedicated component.

#### 2.1 Create HeroCombatController
Create `Assets/Scripts/Hero/HeroCombatController.cs`:
```csharp
[RequireComponent(typeof(EnemyEngagementTracker))]
public class HeroCombatController : MonoBehaviour
{
    // Move from HeroBase:
    private Transform currentEnemy;
    private EnemyHealth currentEnemyHealth;
    private Enemy currentEnemyComp;
    private float lastAttack;
    private bool isRolling;
    private float combatDamageMultiplier = 1f;

    // Dependencies
    private EnemyEngagementTracker tracker;
    private HeroBase hero;

    // Public API
    public bool IsInCombat { get; }
    public float CombatDamageMultiplier => combatDamageMultiplier;
    public Transform CurrentTarget => currentEnemy;

    public bool TryFindAndEngageEnemy(float range, float ttkThreshold = 0f);
    public void HandleCombat();
    public void ExitCombat();

    // Internal
    private float EstimateDpsAgainst(Enemy enemy, HeroBase attacker);
    private float EstimateCombinedDps(Transform enemyTransform);
    private Transform FindNearestEnemy(float range);
    private Transform FindNearestEnemyTimeAware(float range, float thresholdSec);
    private void Attack(Transform target);
    private IEnumerator RollForCombat(float duration);
}
```

#### 2.2 Update HeroBase
- Add `[RequireComponent(typeof(HeroCombatController))]`
- Replace combat logic with `combat.TryFindAndEngageEnemy()` / `combat.HandleCombat()`
- Keep stat accessors (Damage, Defense, etc.) - they delegate to HeroStatSystem

**Estimated reduction:** ~350 lines from HeroBase

---

### Phase 3: Extract Movement Controller

**Goal:** Move pathfinding/movement logic into dedicated component.

#### 3.1 Create HeroMovementController
Create `Assets/Scripts/Hero/HeroMovementController.cs`:
```csharp
[RequireComponent(typeof(AIPath))]
[RequireComponent(typeof(AIDestinationSetter))]
public class HeroMovementController : MonoBehaviour
{
    // Move from HeroBase:
    private AIPath ai;
    private AIDestinationSetter setter;
    private Transform idleWalkTarget;
    private Vector2 lastMoveDir = Vector2.down;
    private bool destinationOverride;

    // Public API
    public Vector2 LastMoveDirection => lastMoveDir;
    public bool IsAtDestination(Transform dest);

    public void SetDestination(Transform dest);
    public void SetDestinationReached();
    public void AutoAdvance(Transform followTarget = null);
    public void UpdateAnimation(Animator anim, SpriteRenderer sprite, bool fourDirectional);
    public void SetActiveState(bool active);
}
```

#### 3.2 Update HeroBase
- Replace movement code with `movement.SetDestination()` / `movement.AutoAdvance()`
- Keep high-level behavior orchestration in `UpdateBehavior()`

**Estimated reduction:** ~150 lines from HeroBase

---

### Phase 4: Performance Optimizations (Optional)

After structural changes are stable:

#### 4.1 Add Camera Bounds Cache
Create `Assets/Scripts/Utilities/CameraBoundsCache.cs` for enemy visibility checks.

#### 4.2 Add Spatial Partitioning
Create `Assets/Scripts/Enemies/EnemySpatialHash.cs` for O(nearby) enemy queries.

#### 4.3 Throttle Engagement Cleanup
Add timer to `EnemyEngagementTracker.CleanupStaleEnemies()` - run at 10Hz instead of every frame.

---

## Phase Summary

| Phase | Focus | Lines Removed | New Files |
|-------|-------|---------------|-----------|
| 0 | Performance/DRY/Rename | ~100 | 2 utilities |
| 1 | Engagement Tracking | ~180 | EnemyEngagementTracker |
| 2 | Combat System | ~350 | HeroCombatController |
| 3 | Movement System | ~150 | HeroMovementController |
| 4 | Performance (optional) | 0 | 2 utilities |

**Final HeroBase target:** ~600 lines (down from 1545)

---

## Testing Checklist Per Phase

### After Phase 0 ✓
- [x] Game compiles without errors
- [x] Hero attacks enemies correctly
- [x] Enemy health bars display correctly
- [x] Floating damage text appears
- [x] Echoes work correctly

### After Phase 1 ✓
- [x] Enemies engage correctly
- [x] Enemy death clears engagement
- [x] Multiple enemies can be tracked
- [x] Assist-echo behavior works

### After Phase 2 ✓
- [x] Main hero combat works
- [x] Echo combat works
- [x] TTK estimation prevents overkill
- [x] Dice roll multiplier applies
- [x] Combined DPS calculation correct

### After Phase 3 ✓
- [x] Hero pathfinds to tasks
- [x] Auto-advance works
- [x] Animation syncs with movement
- [x] Echoes follow main hero when idle

---

## Performance Optimization Opportunities

### High Impact (Do During Refactor)

#### 1. Expose Already-Cached Component References on Enemy

**Current:** GetComponent called per-enemy, per-frame in combat loops
```csharp
// Called 10-50+ times per frame during combat
var hp = enemy.GetComponent<Health>();
var pooled = enemy.GetComponent<PooledObject>();
```

**Good News:** `Enemy.cs` already caches `health` (line 64) but it's `private`.

**Optimized:** Expose existing cached references + add PooledObject
```csharp
// Enemy.cs already has:
private Health health;  // line 64

// Just add public accessor and cache PooledObject:
public Health Health => health;
public PooledObject PooledMarker { get; private set; }

void SetupInitialInstance()  // existing method
{
    health = GetComponent<Health>();       // already exists (line 110)
    PooledMarker = GetComponent<PooledObject>();  // add this
}
```

**HeroBase usage becomes:**
```csharp
// Before
var hp = enemy.GetComponent<Health>();

// After
var hp = enemy.Health;
```

**Estimated Savings:** 50-200 GetComponent calls per frame during combat

#### 2. Use sqrMagnitude Instead of Distance

**Current:**
```csharp
var d = Vector2.Distance(pos, enemyTransform.position);
if (d <= range)
```

**Optimized:**
```csharp
var delta = (Vector2)enemyTransform.position - pos;
if (delta.sqrMagnitude <= rangeSqr)  // rangeSqr = range * range, cached
```

**Estimated Savings:** Eliminates sqrt per comparison (~10-50 per frame)

#### 3. Cache Camera Bounds

**Current:** ViewportToWorldPoint called twice per FindNearestEnemy call
```csharp
min = cam.ViewportToWorldPoint(Vector3.zero);
max = cam.ViewportToWorldPoint(Vector3.one);
```

**Optimized:** Cache in EnemyActivator or dedicated CameraBoundsCache
```csharp
public class CameraBoundsCache : MonoBehaviour
{
    public static Bounds Current { get; private set; }
    private Camera _cam;
    private Vector3 _lastPos;

    void LateUpdate()
    {
        if (_cam.transform.position != _lastPos)
        {
            // Only recalculate when camera moves
            _lastPos = _cam.transform.position;
            var min = _cam.ViewportToWorldPoint(Vector3.zero);
            var max = _cam.ViewportToWorldPoint(Vector3.one);
            Current = new Bounds((min + max) * 0.5f, max - min);
        }
    }
}
```

**Estimated Savings:** 2 ViewportToWorldPoint calls per FindNearestEnemy

#### 4. Remove FindFirstObjectByType from Runtime

**Current:** Called in Awake, OnEnable, and sometimes Update paths
```csharp
taskCtrl = FindFirstObjectByType<TaskController>();
mapUI = FindFirstObjectByType<MapUI>();
```

**Optimized:** Ensure singletons are properly accessible
```csharp
// Already have these - use consistently:
taskCtrl = TaskController.Instance;
mapUI = GameManager.Instance?.mapUIInstance;
```

**Estimated Savings:** Eliminates O(n) scene traversal on enable

### Medium Impact

#### 5. Spatial Partitioning for Enemy Queries

**Current:** Linear search through all active enemies
```csharp
foreach (var enemy in EnemyActivator.ActiveEnemies)  // Could be 20-100 enemies
```

**Optimized:** Use grid-based spatial hash
```csharp
public class EnemySpatialHash
{
    private Dictionary<Vector2Int, List<Enemy>> _cells;
    private const float CellSize = 5f;

    public IEnumerable<Enemy> GetNearby(Vector2 pos, float radius)
    {
        // Only check cells within radius
        var cellRadius = Mathf.CeilToInt(radius / CellSize);
        var center = GetCell(pos);
        // Iterate only relevant cells
    }
}
```

**Estimated Savings:** Reduces O(n) to O(nearby) for enemy queries

#### 6. Throttle Non-Critical Updates

**Current:** HUD distance updated via UITicker at 5Hz - good!
**Current:** Rich presence at 0.5Hz - good!

**Additional opportunities:**
```csharp
// Engaged enemy cleanup could run every 0.1s instead of every frame
private float _nextEngagementCleanup;
void UpdateBehavior()
{
    if (Time.time >= _nextEngagementCleanup)
    {
        CleanupEngagedEnemies();
        _nextEngagementCleanup = Time.time + 0.1f;
    }
}
```

#### 7. Pool the Removal Buffer

**Current:** Already using a buffer, but could pre-size
```csharp
private readonly List<Enemy> enemyRemovalBuffer = new();
```

**Optimized:**
```csharp
private readonly List<Enemy> enemyRemovalBuffer = new(16); // Pre-allocate
```

### Low Impact (Nice to Have)

#### 8. Avoid String Comparisons in Animation

**Current:**
```csharp
animator.SetFloat("MoveX", lastMoveDir.x);
animator.SetFloat("MoveY", lastMoveDir.y);
```

**Optimized:** Cache animator parameter hashes
```csharp
private static readonly int MoveXHash = Animator.StringToHash("MoveX");
private static readonly int MoveYHash = Animator.StringToHash("MoveY");

animator.SetFloat(MoveXHash, lastMoveDir.x);
```

#### 9. Replace Try-Catch with Null Checks

**Current:** 14 try-catch blocks
```csharp
try { et = enemy.transform; } catch { et = null; }
```

**Optimized:** Single null check is cheaper than exception setup
```csharp
// Using the proposed extension:
if (!enemy.TryGetTransformSafe(out var et))
    continue;
```

### Performance Profiling Targets

After refactoring, profile these methods:
| Method | Current Hotspot | Target |
|--------|-----------------|--------|
| `FindNearestEnemy` | O(n) enemies + GetComponent each | O(nearby) + cached refs |
| `UpdateBehavior` | Called every frame | < 0.1ms per hero |
| `EstimateCombinedDps` | Iterates all echoes + enemies | < 0.05ms |
| `OnEnemyEngage` | Event handler | < 0.02ms per call |

### Memory Allocation Targets

| Operation | Current | Target |
|-----------|---------|--------|
| Per-frame allocations in combat | Unknown | 0 bytes |
| Enemy list iteration | Enumerator allocation | foreach on List (no alloc) |
| Dictionary operations | Possible boxing | Direct struct access |

---

## Appendix: Full Field Inventory

### SerializeField (16 total)
```csharp
[SerializeField] private HeroStats stats;
[SerializeField] private Animator animator;
[SerializeField] private SpriteRenderer spriteRenderer;
[SerializeField] private bool fourDirectional = true;
[SerializeField] private Transform projectileOrigin;
[SerializeField] private DiceRoller diceRoller;
[SerializeField] private string diceQuestID = "Protect the Town";
[SerializeField] private Skill combatSkill;
[SerializeField] private BuffManager buffController;
[SerializeField] private LayerMask enemyMask = ~0;
[SerializeField] private string currentTaskName;
[SerializeField] private MonoBehaviour currentTaskObject;
[SerializeField] private bool allowAttacks = true;
[SerializeField] [Range(0f, 10f)] private float echoAvoidIfTTKBelowSeconds = 1.0f;
[SerializeField] private bool assistEchoWhileOnTask = true;
[SerializeField] private float assistEchoThreatRadius = 5f;
[SerializeField] private float combatAggroRange = 20f;
[SerializeField] private AIPath ai;
[SerializeField] private float idleWalkStep = 5f;
[SerializeField] private AIDestinationSetter setter;
[SerializeField] private float richPresenceUpdateInterval = 2f;
```

### Private Fields (26 total)
```csharp
private bool diceUnlocked;
private Transform currentEnemy;
private Health currentEnemyHealth;
private Enemy currentEnemyComp;
private readonly HashSet<Enemy> engagedEnemies = new();
private readonly Dictionary<Enemy, Action> enemyDeathHandlers = new();
private readonly Dictionary<Enemy, Action<Enemy>> enemyDisengageHandlers = new();
private readonly List<Enemy> enemyRemovalBuffer = new();
private readonly Dictionary<Enemy, Transform> enemyTargets = new();
private float attackSpeedBonus;
private float gearAttackSpeedBonus;
private float baseAttackSpeed;
private float baseDamage;
private float baseDefense;
private float baseHealth;
private float baseMoveSpeed;
private float gearDamageBonus;
private float gearDefenseBonus;
private float gearHealthBonus;
private float gearMoveSpeedBonus;
private float combatDamageMultiplier = 1f;
private bool logicActive = true;
private float damageBonus;
private float defenseBonus;
private bool destinationOverride;
private HeroHealth health;
private float healthBonus;
private Transform idleWalkTarget;
private bool isRolling;
private float lastAttack = float.NegativeInfinity;
private Vector2 lastMoveDir = Vector2.down;
private float moveSpeedBonus;
private MapUI mapUI;
private float nextRichPresenceUpdate;
private State state;
```

### Public/Protected Fields (2 total)
```csharp
[NonSerialized] protected TaskController taskCtrl;
public ITask CurrentTask { get; private set; }
```
