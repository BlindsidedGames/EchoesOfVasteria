# Forge Autocraft UI Throttling Plan

## Goal
Reduce GC allocations and overhead during high-speed autocrafting by:
1. Throttling UI updates at different frequencies
2. Batching resource operations to reduce event firing
3. Maintaining resource validation per craft for correctness

## Current State
At 100 crafts/sec, each craft:
- Fires `OnInventoryChanged` event (from `EndBatch()` in `Craft()`)
- Updates UI via `GearStatTextBuilder.BuildCraftResultSummary()` (heavy allocations)
- Calls `OnResourcesChanged()`, `ForceRefreshAllCoreSlots()`, `ThrottledRefreshOdds()`

## Strategy Overview

### 1. Resource Batching (New)
Wrap entire autocraft batch in `ResourceManager.BeginBatch()/EndBatch()`:
- All per-craft `Spend()` calls accumulate
- Single `OnInventoryChanged` event fires at batch end
- Reduces event overhead from 100/sec to ~10/sec (at 10 batches/sec)

### 2. UI Throttling (Multi-frequency)

| Category | Frequency | Methods | Rationale |
|----------|-----------|---------|-----------|
| Visual Preview | 10 Hz | `ShowResult()`, `UpdateResultPreview()` | Smooth visual feedback |
| Stats Rebuild | 1 Hz | `ForceRefreshAllCoreSlots()`, `ThrottledRefreshOdds()` | Heavy computation |
| Resource Display | 1 Hz (staggered) | `OnResourcesChanged()` | Offset from stats by 0.5s |
| Resource Validation | Per craft | `CanCraft()` | Required for correctness |

## Implementation

### Phase 1: Add Resource Batching
Location: `ForgeWindowUI.CraftUntilUpgradeCoroutine()` - wrap batch loop

**Before (current):**
```csharp
for (int batch = 0; batch < craftsPerBatch && isAutoCrafting; batch++)
{
    // ... craft logic, each Craft() triggers OnInventoryChanged
}
```

**After:**
```csharp
var rm = ResourceManager.Instance;
rm?.BeginBatch();
try
{
    for (int batch = 0; batch < craftsPerBatch && isAutoCrafting; batch++)
    {
        // ... craft logic, OnInventoryChanged deferred
    }
}
finally
{
    rm?.EndBatch(); // Single OnInventoryChanged fires here
}
```

### Phase 2: Add Throttle Timers
Location: After `lastConfigCheck` declaration (~line 1165)

```csharp
// Throttle timers for UI updates during autocraft
float lastVisualUpdate = Time.unscaledTime;
float lastStatsUpdate = Time.unscaledTime;
float lastResourceUpdate = Time.unscaledTime - 0.5f; // Stagger by 0.5s

const float visualUpdateInterval = 0.1f;   // 10 Hz
const float statsUpdateInterval = 1.0f;    // 1 Hz
const float resourceUpdateInterval = 1.0f; // 1 Hz (offset by initial value)
```

### Phase 3: Replace Batch UI Logic
Location: Lines 1262-1281

**Replace:**
```csharp
// Only update UI on last item in batch (or when stopping)
bool updateUI = isLastInBatch;

// Check for upgrade (always check, may stop early)
bool isUpgrade = UpgradeEvaluator.IsPotentialUpgrade(crafting, lastCrafted, eq);
bool isVastium = StaticReferences.StopAutocraftOnVastium &&
                 lastCrafted?.rarity?.GetName() == "Vastium";

if (isUpgrade || isVastium)
    updateUI = true;

if (updateUI)
{
    var summary = GearStatTextBuilder.BuildCraftResultSummary(lastCrafted, eq);
    ShowResult(summary);
    UpdateResultPreview(lastCrafted);
    OnResourcesChanged();
    ForceRefreshAllCoreSlots();
    ThrottledRefreshOdds();
}
```

**With:**
```csharp
// Check for stop conditions (always check, may stop early)
bool isUpgrade = UpgradeEvaluator.IsPotentialUpgrade(crafting, lastCrafted, eq);
bool isVastium = StaticReferences.StopAutocraftOnVastium &&
                 lastCrafted?.rarity?.GetName() == "Vastium";
bool isStopping = isUpgrade || isVastium;

// Throttled UI updates during autocraft
float now = Time.unscaledTime;

// Visual preview: 10 Hz or immediate on stop
bool doVisualUpdate = isStopping || (isLastInBatch && (now - lastVisualUpdate) >= visualUpdateInterval);
if (doVisualUpdate)
{
    lastVisualUpdate = now;
    var summary = GearStatTextBuilder.BuildCraftResultSummary(lastCrafted, eq);
    ShowResult(summary);
    UpdateResultPreview(lastCrafted);
}

// Stats rebuild: 1 Hz or immediate on stop
bool doStatsUpdate = isStopping || (isLastInBatch && (now - lastStatsUpdate) >= statsUpdateInterval);
if (doStatsUpdate)
{
    lastStatsUpdate = now;
    ForceRefreshAllCoreSlots();
    ThrottledRefreshOdds();
}

// Resource display: 1 Hz staggered or immediate on stop
bool doResourceUpdate = isStopping || (isLastInBatch && (now - lastResourceUpdate) >= resourceUpdateInterval);
if (doResourceUpdate)
{
    lastResourceUpdate = now;
    OnResourcesChanged();
}
```

### Phase 4: Keep Per-Craft Validation
The existing `CanCraft()` check remains untouched:
```csharp
if (!CanCraft())
{
    // Out of resources stop reason
    ...
    shouldBreak = true;
    break;
}
```
This runs every craft iteration, ensuring resources are validated before each craft.

## Expected Impact at 100 crafts/sec

| Metric | Before | After |
|--------|--------|-------|
| `OnInventoryChanged` events | 100/sec | ~10/sec (per batch) |
| `GearStatTextBuilder` calls | 100/sec | 10/sec (visual) |
| `ForceRefreshAllCoreSlots` calls | 100/sec | 1/sec |
| `OnResourcesChanged` calls | 100/sec | 1/sec |
| Resource validation | 100/sec | 100/sec (unchanged) |

## Validation Checklist

- [ ] Visual updates appear smooth at 10 Hz during autocraft
- [ ] Stats/odds don't visibly lag (1 Hz acceptable for numbers)
- [ ] Resource counts update without noticeable delay
- [ ] Autocrafting stops immediately when resources deplete (CanCraft per-craft)
- [ ] Stop conditions (upgrade/vastium) trigger immediate full UI refresh
- [ ] No frame spikes from stats + resources updating same frame (0.5s stagger)
- [ ] GC allocations significantly reduced in profiler
- [ ] `OnInventoryChanged` event frequency reduced (verify in profiler)

## Future Optimizations (Out of Scope)

1. Pool collections in `GearStatTextBuilder` to eliminate remaining allocations
2. Cache sorted rarities in `RarityOddsCalculator`
3. Replace LINQ fallbacks in `CraftingService.RollRarity`
4. True pre-paid batch crafting (calculate N crafts affordable, spend once, craft N times)
