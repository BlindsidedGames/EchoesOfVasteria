# Forge System Performance Analysis

## Current Architecture Overview

The forge system consists of:
- **CraftingService.cs** (507 lines) - Core crafting logic + heavy telemetry
- **EquipmentController.cs** - Equipment state management
- **ForgeWindowUI.cs** (~2400 lines) - UI + autocrafting coroutine
- **UpgradeEvaluator.cs** (133 lines) - Score computation
- **ForgeStats** (125 fields across 40+ dictionaries) - Analytics tracking

---

## Critical Performance Bottlenecks

### 1. Telemetry Overhead in `Craft()` — ~150 lines per craft

**Location:** `Assets/Scripts/Gear/CraftingService.cs:123-270`

Every single craft executes:
- **40+ Dictionary ContainsKey checks** with conditional initialization
- **2 UpgradeEvaluator score calculations** (each creates new Dictionary)
- **Nested dictionary updates** for StatRollsByRarity, StatRollsBySlot
- **Per-affix loop** with 6+ dictionary operations each

```csharp
// Example pattern repeated ~40 times:
if (!forge.CraftsByCore.ContainsKey(coreKey)) forge.CraftsByCore[coreKey] = 0;
forge.CraftsByCore[coreKey]++;
```

**Estimated cost:** ~3-5KB allocations + significant CPU per craft

---

### 2. Duplicate Score Calculations

**Location:** `CraftingService.cs:209` and `CraftingService.cs:247`

```csharp
float delta = UpgradeEvaluator.ComputeUpgradeScore(crafting, item, eq);  // Line 209
float absScore = UpgradeEvaluator.ComputeAbsoluteScore(crafting, item);   // Line 247
```

Each call at `UpgradeEvaluator.cs:19` and `UpgradeEvaluator.cs:54` creates `new Dictionary<HeroStatMapping, float>()`.

---

### 3. Autocrafting Rate Limited to 10/sec

**Location:** `ForgeWindowUI.cs:1194`

```csharp
var wait = new WaitForSecondsRealtime(0.1f); // ~10 crafts per second
```

The coroutine then calls per-craft:
- `GearStatTextBuilder.BuildCraftResultSummary()` - string building
- `ShowResult()` / `UpdateResultPreview()` - UI updates
- `OnResourcesChanged()` - resource UI refresh
- `ForceRefreshAllCoreSlots()` - core slot UI refresh
- `ThrottledRefreshOdds()` - rarity odds refresh
- `RefreshActionButtons()` - button state refresh

---

### 4. UI Event Cascade on Equipment Change

**Location:** `ForgeWindowUI.cs:340-342`

```csharp
equipment.OnEquipmentChanged += UpdateAllGearSlots;        // Iterates 4 slots
equipment.OnEquipmentChanged += UpdateSelectedSlotStats;   // Rebuilds stat text
equipment.OnEquipmentChanged += UpdateAggregateStatsText;  // Aggregates all 4 slots
```

Three separate handlers fire for every equipment change.

---

### 5. Temporary List Allocations in Hot Paths

**Locations:**
- `CraftingService.cs:278`: `new List<(RaritySO, float)>()` in RollRarity
- `CraftingService.cs:386`: `equipment.Slots.ToList()` in RollSlot
- `CraftingService.cs:432`: `new List<StatDefSO>(stats.Where(...))` in RollAffixes

---

### 6. ComputeTheoreticalMaxForSlot Called Repeatedly

**Location:** `UpgradeEvaluator.cs:91-131`

Iterates ALL rarities and ALL stats every call, with no caching:
```csharp
foreach (var rarity in AssetCache.GetAll<RaritySO>(string.Empty))
foreach (var stat in stats)
```

---

## Quantified Performance Impact

| Operation | Current Cost | Frequency at 100/sec |
|-----------|-------------|---------------------|
| Telemetry dictionaries | ~40 allocs | 4,000 allocs/sec |
| UpgradeEvaluator (×2) | ~8 allocs | 1,600 allocs/sec |
| List allocations | ~3 lists | 300 allocs/sec |
| UI updates | 6 operations | 600 ops/sec |
| **Total GC pressure** | ~3.5 KB/craft | **350 KB/sec** |

---

## Optimization Recommendations

### Tier 1: High Impact, Low Risk

#### 1. Defer Telemetry to Batch Updates
- Queue craft results and flush telemetry every N crafts or on timer
- Skip detailed stat roll tracking during autocrafting
- **Estimated savings: ~60% of per-craft cost**

#### 2. Cache UpgradeEvaluator Results
- Store score on GearItem when crafted
- Reuse static dictionaries with `.Clear()` instead of `new`
- **Estimated savings: ~800 allocs/sec at 100 crafts/sec**

#### 3. Pool/Reuse Temporary Lists
- Pre-allocate `weights` list in RollRarity
- Use `IReadOnlyList` instead of `.ToList()` in RollSlot
- **Estimated savings: ~300 allocs/sec**

#### 4. Combine UI Event Handlers
- Single `OnEquipmentChanged` handler that calls all three updates
- Add dirty flag to skip redundant updates
- **Estimated savings: Reduced UI churn**

---

### Tier 2: Medium Impact, Medium Effort

#### 5. Batch Crafting Mode
- Craft N items in single frame, update UI once
- Return `List<GearItem>` and find best upgrade
- **Could enable 100+ crafts/sec with minimal UI updates**

#### 6. Pre-compute Static Data
- Cache `ComputeTheoreticalMaxForSlot()` results per slot (4 values)
- Cache rarity weight lookups in CoreSO
- **Estimated savings: Eliminates repeated iteration**

#### 7. Async/Background Telemetry
- Write ForgeStats updates to queue, process on background thread
- Only sync back on save or when viewing stats UI
- **Estimated savings: Telemetry off main thread entirely**

---

### Tier 3: Architectural Changes

#### 8. Burst-Mode Autocrafting
- Remove `WaitForSecondsRealtime(0.1f)` delay
- Craft in batches of 10-50 per frame
- Only update UI for upgrade candidates
- **Potential: 1000+ crafts/sec**

#### 9. Object Pooling for GearItem
- Pool GearItem and GearAffix objects
- Reset and reuse instead of allocate
- **Potential: Near-zero GC for crafting**

#### 10. Streamlined Analytics Mode
- Add "Fast Craft" toggle that skips all telemetry
- Users who want speed trade detailed stats
- **Potential: 80% reduction in per-craft cost**

---

## Projected Performance Gains

| Scenario | Current | With Tier 1 | With Tier 1+2 | With All |
|----------|---------|-------------|---------------|----------|
| Crafts/sec (autocrafting) | 10 | 50 | 200 | 1000+ |
| GC pressure per 100 crafts | 350 KB | 140 KB | 50 KB | <10 KB |
| Frame time impact at 100/sec | ~15ms | ~6ms | ~2ms | <1ms |

---

## Recommended Implementation Order

### Quick wins (1-2 hours)
- Cache `ComputeTheoreticalMaxForSlot`
- Reuse dictionaries in UpgradeEvaluator
- Combine UI event handlers

### Medium effort (half day)
- Add batch crafting API
- Pool temporary lists in crafting hot path
- Add telemetry deferral mode

### Larger refactor (1-2 days)
- Implement burst-mode autocrafting
- Background telemetry processing
- Full object pooling

---

## Summary

The biggest single improvement would be **batch crafting + deferred telemetry** - this alone could take you from 10 crafts/sec to 100+ with minimal architectural change.

The current system is heavily instrumented with analytics tracking that runs synchronously on every craft. For high-speed crafting, consider:
1. A "lite" craft path that skips telemetry
2. Batching multiple crafts before UI updates
3. Pooling allocations to reduce GC pressure

---

## Key Files Reference

| Aspect | File | Lines of Interest |
|--------|------|-------------------|
| Core crafting | `Assets/Scripts/Gear/CraftingService.cs` | 57-273 (Craft method) |
| Telemetry | `Assets/Scripts/Gear/CraftingService.cs` | 123-270 (ForgeStats updates) |
| Autocrafting | `Assets/Scripts/Gear/UI/ForgeWindowUI/ForgeWindowUI.cs` | 1192-1326 (coroutine) |
| Score calculation | `Assets/Scripts/Gear/UI/UpgradeEvaluator.cs` | 17-76 |
| ForgeStats structure | `Assets/Scripts/Blindsided/SaveData/GameData.cs` | 446-571 |
| UI event handlers | `Assets/Scripts/Gear/UI/ForgeWindowUI/ForgeWindowUI.cs` | 336-366 (OnEnable) |
