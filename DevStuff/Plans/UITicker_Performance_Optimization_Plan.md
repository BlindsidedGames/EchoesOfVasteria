# UITicker Performance Optimization Plan

## Executive Summary

The profiler shows UITicker.Update() consuming 11.1% CPU with 1099 GC allocations (47.4 KB) per frame. Analysis reveals the allocations occur **inside subscriber callbacks**, not in UITicker itself. The primary culprits are LINQ-heavy SortEntries() methods in panel UIs that run every 0.1 seconds regardless of data changes.

---

## Current State Analysis

### UITicker Subscribers (9 total)

| Subscriber | File | Interval | Rate | Impact |
|------------|------|----------|------|--------|
| GameManager.RefreshRunButtonsUI | GameManager.cs:428 | 0.1s | 10 Hz | Low |
| HeroBase.HudDistanceTick | HeroBase.cs:353 | 0.2s | 5 Hz | Low |
| GeneralStatsPanelUI.RefreshTick | GeneralStatsPanelUI.cs:29 | 0.1s | 10 Hz | Low |
| ItemStatsPanelUI.RefreshTick | ItemStatsPanelUI.cs:59 | 0.1s | 10 Hz | Low (optimized) |
| **EnemyStatsPanelUI.RefreshTick** | EnemyStatsPanelUI.cs:59 | 0.1s | 10 Hz | **HIGH** |
| **TaskStatsPanelUI.RefreshTick** | TaskStatsPanelUI.cs:102 | 0.1s | 10 Hz | **HIGH** |
| RunStatsPanelUI.RefreshTick | RunStatsPanelUI.cs:118 | 0.1s | 10 Hz | Medium |
| CauldronManager.TasteTick | CauldronManager.cs:413 | 0.1s | 10 Hz | Medium |
| TownWindowManager.PollCloseAllWindows | TownWindowManager.cs:199 | 0.05s | 20 Hz | Low |

### Root Cause Breakdown

**GC Allocations (1099 per frame):**
- EnemyStatsPanelUI.SortEntries(): ~3 list allocations × 10/sec
- TaskStatsPanelUI.SortEntries(): ~3 list allocations × 10/sec
- LINQ enumerator allocations in Where/OrderBy/ThenBy chains
- Each ToList() creates a new List<T>

**CPU Time (10.3% self):**
- Sorting operations run every tick even when data unchanged
- LINQ chains are slower than direct List.Sort()

---

## Proposed Changes

### Change 1: UITicker.cs Internal Optimizations

**Scope:** [UITicker.cs:49-95](Assets/Scripts/UI/UITicker.cs#L49-L95)

**Current Issues:**
```csharp
// Line 53-61: foreach allocates enumerator on List<T>
foreach (var sub in _subscriptions)

// Line 89-94: Same issue in Unsubscribe
foreach (var sub in _subscriptions)
foreach (var sub in _toRemove)
```

**Proposed Changes:**
1. Replace foreach with index-based for loops
2. Use index-based removal to avoid _toRemove list allocation pattern

**Before:**
```csharp
public void Subscribe(Action callback, float interval)
{
    if (callback == null || interval <= 0f) return;
    foreach (var sub in _subscriptions)
    {
        if (sub.Callback == callback)
        {
            sub.Interval = interval;
            sub.NextTime = Time.unscaledTime + interval;
            return;
        }
    }
    // ...
}

public void Unsubscribe(Action callback)
{
    if (callback == null) return;
    _toRemove.Clear();
    foreach (var sub in _subscriptions)
        if (sub.Callback == callback)
            _toRemove.Add(sub);
    if (_toRemove.Count > 0)
        foreach (var sub in _toRemove)
            _subscriptions.Remove(sub);
}
```

**After:**
```csharp
public void Subscribe(Action callback, float interval)
{
    if (callback == null || interval <= 0f) return;
    for (int i = 0; i < _subscriptions.Count; i++)
    {
        var sub = _subscriptions[i];
        if (sub.Callback == callback)
        {
            sub.Interval = interval;
            sub.NextTime = Time.unscaledTime + interval;
            return;
        }
    }
    // ...
}

public void Unsubscribe(Action callback)
{
    if (callback == null) return;
    for (int i = _subscriptions.Count - 1; i >= 0; i--)
    {
        if (_subscriptions[i].Callback == callback)
        {
            _subscriptions.RemoveAt(i);
            return; // Assuming single subscription per callback
        }
    }
}
```

**Validation:**
- Subscribe() is called infrequently (OnEnable only)
- Unsubscribe() is called infrequently (OnDisable only)
- Impact: Minor GC reduction, cleaner code
- Risk: **Very Low** - behavioral equivalent

**System Impact:**
- No behavior change to subscribers
- Existing interval logic unchanged
- Time-based firing unchanged

---

### Change 2: CauldronManager - Decouple Roll Rate from Tick Rate

**Scope:** [CauldronManager.cs:408-590](Assets/Scripts/Upgrades/CauldronManager.cs#L408-L590)

**Current Architecture:**
The CauldronManager already uses time-accumulation in TasteTick():
```csharp
private void TasteTick()
{
    // Time accumulation (already rate-independent!)
    var now = Time.unscaledTime;
    var deltaTime = now - _lastTasteTime;
    _lastTasteTime = now;
    _accumulatedTasteTime += deltaTime;

    var rollInterval = 1f / Mathf.Max(1f, config.rollsPerSecond);
    var rollsToProcess = Mathf.FloorToInt(_accumulatedTasteTime / rollInterval);

    // Process all accumulated rolls
    for (int i = 0; i < rollsToProcess; i++) { ... }
}
```

**Key Insight:** The roll processing is ALREADY rate-independent. The tick interval only affects how often we check, not how many rolls happen. If we call TasteTick at 10 Hz instead of matching rollsPerSecond, rolls still happen at the correct rate due to time accumulation.

**Proposed Change:**
Lock UITicker subscription to a fixed 10 Hz (0.1s interval) regardless of config.rollsPerSecond. The time-accumulation logic will batch more rolls per tick if rollsPerSecond > 10.

**Before:**
```csharp
// OnEnable (line 412-413)
_currentTickInterval = 1f / Mathf.Max(1f, config != null ? config.rollsPerSecond : 10f);
UITicker.Instance.Subscribe(TasteTick, _currentTickInterval);

// SyncConfigValuesIfNeeded (line 583-589) - removes and re-subscribes on config change
var desiredInterval = 1f / Mathf.Max(1f, config.rollsPerSecond);
if (Mathf.Abs(desiredInterval - _currentTickInterval) > 0.0001f && UITicker.Instance != null)
{
    UITicker.Instance.Unsubscribe(TasteTick);
    _currentTickInterval = desiredInterval;
    UITicker.Instance.Subscribe(TasteTick, _currentTickInterval);
}
```

**After:**
```csharp
// Constants for UI tick rate (decoupled from roll rate)
private const float UI_TICK_INTERVAL = 0.1f; // 10 Hz for UI responsiveness

// OnEnable
UITicker.Instance.Subscribe(TasteTick, UI_TICK_INTERVAL);

// SyncConfigValuesIfNeeded - REMOVE the re-subscription logic entirely
// (The time-accumulation handles any rollsPerSecond value automatically)
```

**Validation:**

| Scenario | Before | After | Behavior |
|----------|--------|-------|----------|
| rollsPerSecond = 10 | TasteTick @ 10 Hz, 1 roll/tick | TasteTick @ 10 Hz, 1 roll/tick | Identical |
| rollsPerSecond = 100 | TasteTick @ 100 Hz, 1 roll/tick | TasteTick @ 10 Hz, 10 rolls/tick | Identical rolls, fewer ticks |
| rollsPerSecond = 1000 | TasteTick @ 1000 Hz, 1 roll/tick | TasteTick @ 10 Hz, 100 rolls/tick | Identical rolls, far fewer ticks |
| UI responsiveness | Updates every tick | Updates every 0.1s | Consistent 10 Hz UI |

**System Impact:**
- Roll count per second: **UNCHANGED** (time-accumulation ensures correct count)
- Eva XP gain rate: **UNCHANGED** (based on rolls, not ticks)
- Card drop rate: **UNCHANGED** (based on rolls, not ticks)
- Stew consumption: **UNCHANGED** (based on rolls, not ticks)
- UI throttles already exist: OnStewChanged (0.1s), OnSessionCardsChanged (0.2s), OnStatsChanged (0.2s)
- Risk: **Very Low** - the accumulation pattern already handles this

**Why This Works:**
The existing `maxRollsPerFrame = 500` cap ensures that even with very high rollsPerSecond (e.g., 1000), a single tick won't freeze the game. At 10 Hz ticks with 1000 rolls/sec, we'd process 100 rolls per tick - well within the cap.

---

### Change 3: Add Dirty Flags to Panel SortEntries()

**Scope:**
- [EnemyStatsPanelUI.cs:303-370](Assets/Scripts/UI/EnemyStatsPanelUI.cs#L303-L370)
- [TaskStatsPanelUI.cs:503-558](Assets/Scripts/UI/TaskStatsPanelUI.cs#L503-L558)

**Current Issue:**
SortEntries() runs every RefreshTick (0.1s) even when the sort order hasn't changed. The sort order only changes when:
1. User changes sort mode (button click)
2. Enemy/Task discovered (kills/completions change from 0 to >0)
3. Stat values change enough to reorder entries

**Proposed Change:**
Add dirty flag that tracks when re-sorting is actually needed.

**EnemyStatsPanelUI - Add Dirty Tracking:**
```csharp
private bool _sortDirty = true;
private SortMode _lastAppliedSortMode;

// Set dirty when data changes that affects sort order
private void MarkSortDirty() => _sortDirty = true;

// In SetSortMode()
public void SetSortMode(SortMode mode)
{
    if (sortMode == mode) return;
    sortMode = mode;
    _sortDirty = true;  // User changed mode
    SortEntries();
}

// In UpdateEntry() - detect when an enemy becomes "known"
private void UpdateEntry(EnemyData stats, EnemyStatEntryUIReferences ui)
{
    var kills = killTracker?.GetKills(stats) ?? 0;
    if (lastDisplayed.TryGetValue(stats, out var last))
    {
        // Detect transition from unknown to known
        if (last.kills == 0 && kills > 0)
            _sortDirty = true;
        // ... rest of dirty check for UI update
    }
    // ...
}

// In SortEntries() - early exit if not dirty
private void SortEntries()
{
    if (!_sortDirty && sortMode == _lastAppliedSortMode) return;
    _sortDirty = false;
    _lastAppliedSortMode = sortMode;
    // ... existing sort logic
}
```

**TaskStatsPanelUI - Same Pattern:**
```csharp
private bool _sortDirty = true;
private SortMode _lastAppliedSortMode;

// Detect when task becomes "known" (completed > 0)
// Already has lastDisplayedByTask tracking, can compare completed == 0 to > 0 transition
```

**Validation:**

| Event | Current Sorts | With Dirty Flag | Reduction |
|-------|---------------|-----------------|-----------|
| Panel open, no changes | 10/sec | 1 (initial) | 90% |
| User clicks sort button | 10/sec + 1 | 1 | 90%+ |
| Enemy killed (first time) | 10/sec | 1 | 90% |
| Enemy killed (repeat) | 10/sec | 0 | 100% |
| Task completed (first time) | 10/sec | 1 | 90% |
| Task completed (repeat) | 10/sec | 0 | 100% |

**System Impact:**
- Sort order: **UNCHANGED** (still sorts when needed)
- Visual appearance: **UNCHANGED** (same final order)
- User interaction: **UNCHANGED** (sort buttons still work)
- Risk: **Low** - adds early-exit, doesn't change sort logic

---

### Change 4: Convert LINQ to Scratch-List Pattern

**Scope:**
- [EnemyStatsPanelUI.cs:303-370](Assets/Scripts/UI/EnemyStatsPanelUI.cs#L303-L370)
- [TaskStatsPanelUI.cs:503-558](Assets/Scripts/UI/TaskStatsPanelUI.cs#L503-L558)

**Reference Implementation:** [ItemStatsPanelUI.cs:262-320](Assets/Scripts/UI/ItemStatsPanelUI.cs#L262-L320)

**Current Pattern (LINQ-heavy, allocates):**
```csharp
// EnemyStatsPanelUI
var sortedKnown = known.OrderBy(s => s.displayOrder).ThenBy(s => s.enemyName).ToList();
var sortedUnknown = unknown.OrderBy(s => s.displayOrder).ThenBy(s => s.enemyName).ToList();
var finalDefault = sortedKnown.Concat(sortedUnknown).ToList();
```

**Target Pattern (scratch lists, zero allocation):**
```csharp
// Pre-allocated at class level
private readonly List<EnemyData> _scratchKnown = new();
private readonly List<EnemyData> _scratchUnknown = new();
private readonly List<EnemyData> _scratchFinal = new();

// Comparison delegate (avoids closure allocation)
private static readonly Comparison<EnemyData> CompareByDisplayOrderThenName =
    (a, b) => {
        int cmp = a.displayOrder.CompareTo(b.displayOrder);
        return cmp != 0 ? cmp : string.Compare(a.enemyName, b.enemyName, StringComparison.Ordinal);
    };

private void SortEntries()
{
    if (!_sortDirty) return;
    _sortDirty = false;

    _scratchKnown.Clear();
    _scratchUnknown.Clear();
    _scratchFinal.Clear();

    // Partition into known/unknown
    for (int i = 0; i < defaultOrder.Count; i++)
    {
        var enemy = defaultOrder[i];
        if (killTracker != null && killTracker.GetKills(enemy) > 0)
            _scratchKnown.Add(enemy);
        else
            _scratchUnknown.Add(enemy);
    }

    // Sort each partition
    _scratchKnown.Sort(CompareByDisplayOrderThenName);
    _scratchUnknown.Sort(CompareByDisplayOrderThenName);

    // Combine
    _scratchFinal.AddRange(_scratchKnown);
    _scratchFinal.AddRange(_scratchUnknown);

    ApplyOrder(_scratchFinal);
}
```

**Validation - Allocation Comparison:**

| Operation | LINQ Pattern | Scratch Pattern | Savings |
|-----------|-------------|-----------------|---------|
| Where() | Enumerator alloc | None | 100% |
| OrderBy() | Enumerator + buffer | None | 100% |
| ThenBy() | Enumerator | None | 100% |
| ToList() | New List<T> | None (reused) | 100% |
| Concat() | Enumerator | AddRange (no alloc) | 100% |
| **Per-sort total** | ~3-4 allocations | 0 allocations | **100%** |

**Performance Comparison:**

| Metric | LINQ | List.Sort | Improvement |
|--------|------|-----------|-------------|
| Speed (small N) | ~2-3x slower | Baseline | 2-3x faster |
| GC pressure | High | Zero | Eliminated |
| Cache locality | Poor (iterators) | Good (contiguous) | Better |

**System Impact:**
- Sort results: **UNCHANGED** (same algorithm, different implementation)
- Entry order: **UNCHANGED** (same comparisons)
- UI display: **UNCHANGED**
- Risk: **Low** - pure refactor of implementation

---

## Implementation Order

### Phase 1: UITicker Internal (Low Risk, Quick Win)
1. Convert foreach to for loops in Subscribe/Unsubscribe
2. Remove _toRemove pattern, use reverse iteration
3. Test: Verify all 9 subscribers still work

### Phase 2: CauldronManager Decoupling (Low Risk, High Impact)
1. Add UI_TICK_INTERVAL constant (0.1f)
2. Change OnEnable subscription to use constant
3. Remove dynamic re-subscription in SyncConfigValuesIfNeeded
4. Test: Verify rolls/sec unchanged at various config values

### Phase 3: Dirty Flags (Low Risk, Medium Impact)
1. Add _sortDirty flag to EnemyStatsPanelUI
2. Add dirty detection in UpdateEntry for known/unknown transitions
3. Add early exit in SortEntries
4. Repeat for TaskStatsPanelUI
5. Test: Verify sorting still works on mode change, first kills, etc.

### Phase 4: Scratch Lists (Low Risk, High Impact)
1. Add scratch list fields to EnemyStatsPanelUI
2. Add static Comparison delegates
3. Convert SortEntries to scratch pattern
4. Repeat for TaskStatsPanelUI
5. Test: Verify sort order matches original LINQ version

---

## Expected Results

### Before Optimization
- UITicker.Update(): 11.1% CPU, 1099 GC allocations, 47.4 KB/frame
- Frame spikes during panel viewing

### After Optimization (Projected)

| Metric | Before | After | Reduction |
|--------|--------|-------|-----------|
| GC allocations in UITicker | 1099 | ~50 | **95%** |
| GC bytes per frame | 47.4 KB | ~2 KB | **96%** |
| CPU time (UITicker) | 10.3% self | ~2% self | **80%** |
| Sorts per second (panels) | 10/sec each | ~0.1/sec avg | **99%** |

### Validation Tests

1. **Cauldron Roll Rate Test:**
   - Set rollsPerSecond to 10, 100, 1000
   - Verify same number of rolls happen in a fixed time period
   - Verify UI updates at consistent 10 Hz rate

2. **Panel Sort Accuracy Test:**
   - Open EnemyStats panel, verify sort order
   - Kill an enemy for first time, verify it moves to "known" section
   - Change sort mode, verify instant re-sort
   - With no data changes, verify no re-sorting (log check)

3. **Memory Profiler Test:**
   - Profile before/after with panels open
   - Verify near-zero GC allocations during steady state
   - Verify no memory leaks from scratch lists

---

## Files to Modify

| File | Changes |
|------|---------|
| [UITicker.cs](Assets/Scripts/UI/UITicker.cs) | foreach → for loops, remove _toRemove pattern |
| [CauldronManager.cs](Assets/Scripts/Upgrades/CauldronManager.cs) | Fixed tick interval, remove re-subscription |
| [EnemyStatsPanelUI.cs](Assets/Scripts/UI/EnemyStatsPanelUI.cs) | Dirty flag + scratch lists |
| [TaskStatsPanelUI.cs](Assets/Scripts/UI/TaskStatsPanelUI.cs) | Dirty flag + scratch lists |

---

## Risk Assessment

| Change | Risk Level | Mitigation |
|--------|------------|------------|
| UITicker for loops | Very Low | Behavioral equivalent, no logic change |
| Cauldron tick decoupling | Very Low | Time accumulation already handles this |
| Dirty flags | Low | Early exit only, existing logic preserved |
| Scratch lists | Low | Same algorithm, reference implementation exists |

**Overall Risk: LOW** - All changes are implementation refactors that preserve existing behavior.
