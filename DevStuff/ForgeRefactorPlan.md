# Forge System Refactoring & Performance Plan

## Overview

This document outlines a two-stage plan to refactor and optimize the forge system. Each stage is divided into phases designed for **parallel execution** where possible, minimizing iteration time and reducing context complexity.

---

## Current State Summary

| File | Lines | Responsibilities |
|------|-------|------------------|
| `CraftingService.cs` | 507 | Core crafting, rarity/slot/affix rolling, 150+ lines telemetry |
| `ForgeWindowUI.cs` | ~2400 | UI management, 4 conversion pipelines, autocrafting, slot selection |
| `UpgradeEvaluator.cs` | 134 | Score calculation, quality evaluation |
| `ForgeStats` (GameData.cs) | 125 fields | Analytics structure (40+ dictionaries) |

---

# Stage 1: Refactor to Reduce Monolithic Scripts

**Goal:** Separate responsibilities, reduce file sizes, eliminate code duplication, and improve maintainability without breaking editor references.

---

## Phase 1.1: Extract Telemetry Layer (Parallel Track A)

**Target:** Remove ~180 lines from `CraftingService.Craft()` method

### 1.1.1 Create `ForgeAnalyticsService.cs`

**Location:** `Assets/Scripts/Gear/ForgeAnalyticsService.cs`

**Extract these responsibilities:**
- All dictionary initialization patterns (lines 88-270 in CraftingService)
- Stat roll aggregation logic
- Upgrade tracking and score benchmarks
- Ivan XP telemetry

**New API:**
```csharp
public class ForgeAnalyticsService : MonoBehaviour
{
    public static ForgeAnalyticsService Instance { get; private set; }

    // Record a completed craft with all relevant data
    public void RecordCraft(CraftResult result);

    // Record a conversion (ingot/crystal/chunk/core)
    public void RecordConversion(ConversionType type, CoreSO core, double amount, Dictionary<Resource, double> costs);

    // Record autocraft session events
    public void RecordAutocraftStart();
    public void RecordAutocraftStop(string reason, GearItem lastItem);

    // Record salvage
    public void RecordSalvage(GearItem item, Dictionary<Resource, double> yields, bool isAuto);
}
```

**New data class:**
```csharp
public struct CraftResult
{
    public CoreSO Core;
    public RaritySO Rarity;
    public string Slot;
    public GearItem Item;
    public GearItem EquippedComparison;
    public float UpgradeScore;
    public float AbsoluteScore;
    public bool IsUpgrade;
    public int IvanXpGranted;
}
```

### 1.1.2 Create Dictionary Helper Extensions

**Location:** `Assets/Scripts/Blindsided/Utilities/DictionaryExtensions.cs`

**DRY Pattern - Replace ~50 instances of:**
```csharp
if (!dict.ContainsKey(key)) dict[key] = 0;
dict[key]++;
```

**With:**
```csharp
public static class DictionaryExtensions
{
    public static TValue GetOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TValue> factory)
    {
        if (!dict.TryGetValue(key, out var value))
            dict[key] = value = factory();
        return value;
    }

    public static void Increment(this Dictionary<string, int> dict, string key, int amount = 1)
    {
        dict.TryGetValue(key, out var current);
        dict[key] = current + amount;
    }

    public static void IncrementDouble(this Dictionary<string, double> dict, string key, double amount)
    {
        dict.TryGetValue(key, out var current);
        dict[key] = current + amount;
    }

    public static void UpdateStatAgg(this Dictionary<string, GameData.ForgeStats.StatAgg> dict, string key, float value)
    {
        var agg = dict.GetOrCreate(key, () => new GameData.ForgeStats.StatAgg());
        agg.count++;
        agg.sum += value;
        if (value < agg.min) agg.min = value;
        if (value > agg.max) agg.max = value;
    }
}
```

### 1.1.3 Update CraftingService

- Remove telemetry code from `Craft()` (lines 88-270)
- Call `ForgeAnalyticsService.Instance.RecordCraft()` at end of method
- Remove telemetry from `GrantIvanExperience()` (lines 357-372)
- **Keep all public API signatures unchanged**

**Files Modified:**
- `CraftingService.cs` (reduce from 507 to ~330 lines)

**Files Created:**
- `ForgeAnalyticsService.cs` (~250 lines)
- `DictionaryExtensions.cs` (~50 lines)

---

## Phase 1.2: Extract Conversion Pipeline (Parallel Track B)

**Target:** Remove ~300 lines from `ForgeWindowUI.cs`

### 1.2.1 Create `ConversionPipeline.cs`

**Location:** `Assets/Scripts/Gear/UI/ConversionPipeline.cs`

**Consolidate the 4 nearly-identical conversion patterns:**
- `OnCraftIngotClicked()` (lines 864-926)
- `OnCraftCrystalClicked()` (lines 928-986)
- `OnCraftChunkClicked()` (lines 988-1046)
- `OnCraftCoreConversionClicked()` (lines 1048-1130)

**New abstraction:**
```csharp
public enum ConversionType { Ingot, Crystal, Chunk, Core }

[System.Serializable]
public class ConversionPipeline
{
    public ConversionType Type;
    public CraftSection2x1UIReferences UISection;

    // Cost formula delegates
    public Func<ResourceManager, CoreSO, bool> CanPerform;
    public Func<ResourceManager, CoreSO, double, double> GetMaxAmount;
    public Action<ResourceManager, CoreSO, double> Execute;

    // Amount tracking
    public double DesiredAmount { get; set; } = 1;

    public void PerformConversion(ResourceManager rm, CoreSO core, ForgeAnalyticsService analytics);
}
```

### 1.2.2 Create `ConversionPipelineFactory.cs`

**Location:** `Assets/Scripts/Gear/UI/ConversionPipelineFactory.cs`

**Factory to configure each conversion type:**
```csharp
public static class ConversionPipelineFactory
{
    public static ConversionPipeline CreateIngotPipeline(CraftSection2x1UIReferences section);
    public static ConversionPipeline CreateCrystalPipeline(CraftSection2x1UIReferences section, Resource slime);
    public static ConversionPipeline CreateChunkPipeline(CraftSection2x1UIReferences section, Resource stone);
    public static ConversionPipeline CreateCorePipeline(CraftSection2x1UIReferences section, List<Resource> coreResources);
}
```

### 1.2.3 Update ForgeWindowUI

**Replace in Awake():**
- Individual button wiring (~70 lines) → single loop over pipelines
- Individual input handlers → generic `OnPipelineAmountChanged()`

**Replace click handlers:**
- 4 separate methods → single `OnConversionClicked(ConversionPipeline)`

**Replace affordability checks:**
- `CanCraftIngot()`, `CanCraftCrystal()`, etc. → `pipeline.CanPerform()`

**Files Modified:**
- `ForgeWindowUI.cs` (reduce by ~300 lines)

**Files Created:**
- `ConversionPipeline.cs` (~80 lines)
- `ConversionPipelineFactory.cs` (~120 lines)

---

## Phase 1.3: Extract Score Evaluation Service (Parallel Track C)

**Target:** Cache expensive calculations, provide clean API

### 1.3.1 Enhance `UpgradeEvaluator.cs` → `ScoreEvaluationService.cs`

**Location:** `Assets/Scripts/Gear/UI/ScoreEvaluationService.cs`

**Add caching and consolidation:**
```csharp
public class ScoreEvaluationService : MonoBehaviour
{
    public static ScoreEvaluationService Instance { get; private set; }

    // Cached theoretical max per slot (computed once, invalidated on asset reload)
    private Dictionary<string, float> _theoreticalMaxBySlot = new();

    // Evaluation result struct to avoid multiple calls
    public struct EvaluationResult
    {
        public float UpgradeScore;
        public float AbsoluteScore;
        public float QualityPercent;
        public bool IsUpgrade;
    }

    // Single call replaces 3 separate calls in CraftingService
    public EvaluationResult Evaluate(GearItem candidate, GearItem current, string slot);

    // Cached max lookup
    public float GetTheoreticalMaxForSlot(string slot);

    // Cache invalidation (call on asset database refresh)
    public void InvalidateCache();
}
```

### 1.3.2 Reuse Static Dictionaries

**DRY Pattern - Replace dictionary allocations in hot path:**

Current (creates 2 dictionaries per craft):
```csharp
var deltaByMapping = new Dictionary<HeroStatMapping, float>(); // Line 19
var totalsByMapping = new Dictionary<HeroStatMapping, float>(); // Line 54
```

New (reuse and clear):
```csharp
private static readonly Dictionary<HeroStatMapping, float> _scratchDict = new();

public static float ComputeUpgradeScore(...)
{
    _scratchDict.Clear();
    // ... use _scratchDict ...
}
```

**Files Modified:**
- `UpgradeEvaluator.cs` → renamed/enhanced

**Files Created:**
- `ScoreEvaluationService.cs` (~180 lines)

---

## Phase 1.4: Extract UI Subsystems (Parallel Track D)

**Target:** Break ForgeWindowUI into focused components

### 1.4.1 Create `ForgeSlotManager.cs`

**Location:** `Assets/Scripts/Gear/UI/ForgeWindowUI/ForgeSlotManager.cs`

**Extract slot mapping and selection:**
```csharp
public class ForgeSlotManager
{
    private Dictionary<GearSlotUIReferences, string> _gearSlotNameByRef;
    private Dictionary<CoreSlotUIReferences, CoreSO> _coreSlotCoreByRef;

    public string SelectedSlot { get; private set; }
    public CoreSO SelectedCore { get; private set; }

    public event Action<string> OnSlotSelected;
    public event Action<CoreSO> OnCoreSelected;

    public void Initialize(List<GearSlotUIReferences> gearSlots, List<CoreSlotUIReferences> coreSlots);
    public void SelectSlot(string slotName);
    public void SelectCore(CoreSO core);
    public void UpdateVisualSelections();
}
```

### 1.4.2 Create `ForgeResultPreview.cs`

**Location:** `Assets/Scripts/Gear/UI/ForgeWindowUI/ForgeResultPreview.cs`

**Extract result display logic:**
```csharp
public class ForgeResultPreview : MonoBehaviour
{
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text resultTierText;
    [SerializeField] private Image resultImage;
    [SerializeField] private List<Sprite> unknownGearSprites;
    [SerializeField] private Sprite migratedHelmetSprite;

    public void ShowResult(GearItem item, GearItem comparison);
    public void ShowUnknown(string slot);
    public void Clear();
    public void ApplyTierText(RaritySO rarity);
}
```

### 1.4.3 Create `ForgeIvanXpDisplay.cs`

**Location:** `Assets/Scripts/Gear/UI/ForgeWindowUI/ForgeIvanXpDisplay.cs`

**Extract Ivan XP UI:**
```csharp
public class ForgeIvanXpDisplay : MonoBehaviour
{
    [SerializeField] private SlicedFilledImage xpBar;
    [SerializeField] private TMP_Text xpText;
    [SerializeField] private TMP_Text levelText;

    public void UpdateDisplay(int level, float currentXp, float neededXp);
    public void OnLevelUp(int newLevel);
}
```

**Files Modified:**
- `ForgeWindowUI.cs` (delegates to new components, reduces by ~150 lines)

**Files Created:**
- `ForgeSlotManager.cs` (~100 lines)
- `ForgeResultPreview.cs` (~80 lines)
- `ForgeIvanXpDisplay.cs` (~40 lines)

---

## Phase 1.5: Consolidate Event Handlers (Sequential, depends on 1.4)

**Target:** Reduce UI event cascade

### 1.5.1 Combine Equipment Change Handlers

**Current (3 separate subscriptions):**
```csharp
equipment.OnEquipmentChanged += UpdateAllGearSlots;
equipment.OnEquipmentChanged += UpdateSelectedSlotStats;
equipment.OnEquipmentChanged += UpdateAggregateStatsText;
```

**New (single combined handler):**
```csharp
equipment.OnEquipmentChanged += OnEquipmentChangedCombined;

private void OnEquipmentChangedCombined()
{
    UpdateAllGearSlots();
    UpdateSelectedSlotStats();
    UpdateAggregateStatsText();
}
```

### 1.5.2 Add Dirty Flag for Redundant Updates

```csharp
private bool _gearSlotsNeedRefresh;
private bool _statsNeedRefresh;

private void LateUpdate()
{
    if (_gearSlotsNeedRefresh)
    {
        UpdateAllGearSlots();
        _gearSlotsNeedRefresh = false;
    }
    if (_statsNeedRefresh)
    {
        UpdateSelectedSlotStats();
        UpdateAggregateStatsText();
        _statsNeedRefresh = false;
    }
}
```

---

## Stage 1 Summary

| Phase | Track | Dependencies | Est. Lines Changed | Files Created |
|-------|-------|--------------|-------------------|---------------|
| 1.1 | A | None | -180 (CraftingService) | 2 |
| 1.2 | B | None | -300 (ForgeWindowUI) | 2 |
| 1.3 | C | None | ~50 (UpgradeEvaluator) | 1 |
| 1.4 | D | None | -150 (ForgeWindowUI) | 3 |
| 1.5 | - | 1.4 | ~30 (ForgeWindowUI) | 0 |

**Parallel Execution:** Phases 1.1, 1.2, 1.3, 1.4 can be worked in parallel. Phase 1.5 requires 1.4 to complete.

**Total Reduction:** ForgeWindowUI ~2400 → ~1600 lines, CraftingService ~507 → ~330 lines

---

# Stage 2: Performance Optimizations

**Goal:** Implement optimizations from ForgePerformanceAnalysis.md to increase crafts/sec and reduce GC pressure.

---

## Phase 2.1: Deferred Telemetry (Parallel Track A)

**Target:** Remove telemetry from hot path during autocrafting

### 2.1.1 Add Batch Recording Mode to ForgeAnalyticsService

```csharp
public class ForgeAnalyticsService
{
    private bool _batchMode;
    private List<CraftResult> _pendingResults = new();
    private const int MaxBatchSize = 50;

    public void BeginBatch() => _batchMode = true;

    public void EndBatch()
    {
        _batchMode = false;
        FlushPendingResults();
    }

    public void RecordCraft(CraftResult result)
    {
        if (_batchMode)
        {
            _pendingResults.Add(result);
            if (_pendingResults.Count >= MaxBatchSize)
                FlushPendingResults();
        }
        else
        {
            RecordCraftImmediate(result);
        }
    }

    private void FlushPendingResults()
    {
        foreach (var r in _pendingResults)
            RecordCraftImmediate(r);
        _pendingResults.Clear();
    }
}
```

### 2.1.2 Update Autocrafting Coroutine

```csharp
private IEnumerator CraftUntilUpgradeCoroutine()
{
    ForgeAnalyticsService.Instance.BeginBatch();
    try
    {
        // ... existing loop ...
    }
    finally
    {
        ForgeAnalyticsService.Instance.EndBatch();
    }
}
```

**Estimated savings:** ~60% of per-craft telemetry overhead

---

## Phase 2.2: Cache Score Calculations (Parallel Track B)

**Target:** Eliminate redundant dictionary allocations and iterations

### 2.2.1 Cache Theoretical Max Per Slot

**In ScoreEvaluationService:**
```csharp
private Dictionary<string, float> _theoreticalMaxBySlot;
private bool _cacheValid;

public float GetTheoreticalMaxForSlot(string slot)
{
    if (!_cacheValid)
        RebuildCache();
    return _theoreticalMaxBySlot.TryGetValue(slot, out var max) ? max : 0f;
}

private void RebuildCache()
{
    _theoreticalMaxBySlot = new();
    foreach (var slot in new[] { "Weapon", "Helmet", "Chest", "Boots" })
        _theoreticalMaxBySlot[slot] = ComputeTheoreticalMaxForSlotInternal(slot);
    _cacheValid = true;
}

// Invalidate on asset reload or game start
public void InvalidateCache() => _cacheValid = false;
```

### 2.2.2 Reuse Scratch Dictionaries

**In ComputeUpgradeScore/ComputeAbsoluteScore:**
```csharp
// Thread-local or instance-level scratch space
[ThreadStatic] private static Dictionary<HeroStatMapping, float> _scratch;

public static float ComputeUpgradeScore(CraftingService crafting, GearItem candidate, GearItem current)
{
    _scratch ??= new Dictionary<HeroStatMapping, float>();
    _scratch.Clear();

    // ... use _scratch instead of new Dictionary ...
}
```

**Estimated savings:** ~800 allocations/sec at 100 crafts/sec

---

## Phase 2.3: Pool Temporary Lists (Parallel Track C)

**Target:** Eliminate list allocations in hot paths

### 2.3.1 Pool Rarity Weights List in RollRarity

**Current:**
```csharp
var weights = new List<(RaritySO rarity, float w)>(); // Line 278
```

**New (using instance-level list):**
```csharp
private readonly List<(RaritySO rarity, float w)> _rarityWeights = new(8);

private RaritySO RollRarity(CoreSO core)
{
    _rarityWeights.Clear();
    // ... populate _rarityWeights ...
}
```

### 2.3.2 Avoid ToList() in RollSlot

**Current:**
```csharp
var pool = equipment.Slots.ToList(); // Line 386
```

**New (use IReadOnlyList or cache):**
```csharp
private static readonly string[] DefaultSlots = { "Weapon", "Helmet", "Chest", "Boots" };
private readonly List<string> _slotPool = new(4);
private readonly float[] _slotWeights = new float[4];

private string RollSlot(CoreSO core, List<string> slotWhitelist)
{
    _slotPool.Clear();
    var slots = equipment?.Slots ?? (IReadOnlyList<string>)DefaultSlots;
    // ... filter into _slotPool without allocation ...
}
```

### 2.3.3 Pool Available Stats in RollAffixes

**Current:**
```csharp
var available = new List<StatDefSO>(stats.Where(s => s != null)); // Line 432
```

**New:**
```csharp
private readonly List<StatDefSO> _availableStats = new(16);

private void RollAffixes(GearItem item)
{
    _availableStats.Clear();
    foreach (var s in stats)
        if (s != null) _availableStats.Add(s);
    // ... filter MoveSpeed if not boots ...
}
```

**Estimated savings:** ~300 allocations/sec

---

## Phase 2.4: Batch Crafting API (Sequential, depends on 2.1-2.3)

**Target:** Enable 100+ crafts/sec with minimal UI updates

### 2.4.1 Add Batch Craft Method to CraftingService

```csharp
public List<GearItem> CraftBatch(CoreSO core, int count, string selectedSlot = null,
    List<string> slotWhitelist = null, Resource coreResource = null, int coreCostPerCraft = 1)
{
    var results = new List<GearItem>(count);

    // Pre-validate total affordability
    var totalIngotCost = core.ingotCost * count;
    var totalCoreCost = coreCostPerCraft * count;
    if (rm.GetAmount(core.requiredIngot) < totalIngotCost) return results;
    if (rm.GetAmount(coreResource) < totalCoreCost) return results;

    // Batch spend all resources at once
    rm.BeginBatch();
    rm.Spend(core.requiredIngot, totalIngotCost);
    rm.Spend(coreResource, totalCoreCost);
    rm.EndBatch();

    // Generate all items
    for (int i = 0; i < count; i++)
    {
        var item = CraftInternal(core, selectedSlot, slotWhitelist);
        if (item != null) results.Add(item);
    }

    return results;
}
```

### 2.4.2 Add Batch Mode to Autocrafting

```csharp
private IEnumerator CraftUntilUpgradeCoroutine()
{
    const int BatchSize = 10;
    var wait = new WaitForSecondsRealtime(0.05f); // 20 batches/sec = 200 crafts/sec

    while (isAutoCrafting)
    {
        var batch = crafting.CraftBatch(selectedCore, BatchSize, selectedSlot, null, coreRes, 1);

        // Find best upgrade in batch
        GearItem bestUpgrade = null;
        foreach (var item in batch)
        {
            if (UpgradeEvaluator.IsPotentialUpgrade(crafting, item, eq))
            {
                if (bestUpgrade == null ||
                    UpgradeEvaluator.ComputeUpgradeScore(crafting, item, eq) >
                    UpgradeEvaluator.ComputeUpgradeScore(crafting, bestUpgrade, eq))
                {
                    bestUpgrade = item;
                }
            }
        }

        // Auto-salvage non-upgrades
        foreach (var item in batch)
            if (item != bestUpgrade)
                SalvageService.Instance.Salvage(item, isAuto: true);

        // Update UI once per batch
        if (bestUpgrade != null)
        {
            lastCrafted = bestUpgrade;
            ShowResult(GearStatTextBuilder.BuildCraftResultSummary(bestUpgrade, eq));
            UpdateResultPreview(bestUpgrade);
            break;
        }

        OnResourcesChanged();
        yield return wait;
    }
}
```

**Estimated improvement:** 10 crafts/sec → 200+ crafts/sec

---

## Phase 2.5: UI Update Throttling (Parallel Track D)

**Target:** Reduce UI operations during rapid crafting

### 2.5.1 Throttle Resource Display Updates

```csharp
private float _nextResourceRefreshTime;
private const float ResourceRefreshInterval = 0.1f;

private void OnResourcesChanged()
{
    if (Time.unscaledTime < _nextResourceRefreshTime)
        return;
    _nextResourceRefreshTime = Time.unscaledTime + ResourceRefreshInterval;

    // ... actual refresh logic ...
}
```

### 2.5.2 Defer Odds Refresh During Autocrafting

```csharp
private void ThrottledRefreshOdds()
{
    if (isAutoCrafting)
        return; // Skip during autocrafting, refresh when stopped

    if (Time.unscaledTime < nextOddsRefreshTime)
        return;
    nextOddsRefreshTime = Time.unscaledTime + 0.5f;
    RefreshOdds();
}
```

### 2.5.3 Skip Core Slot Refresh During Autocrafting

```csharp
private void ForceRefreshAllCoreSlots()
{
    if (isAutoCrafting)
        return; // Defer until autocrafting stops

    // ... existing refresh logic ...
}
```

---

## Phase 2.6: Object Pooling for GearItem/GearAffix (Parallel Track E)

**Target:** Near-zero GC for crafting objects - no editor changes required

### 2.6.1 Create `GearItemPool.cs`

**Location:** `Assets/Scripts/Gear/GearItemPool.cs`

```csharp
public class GearItemPool
{
    private static readonly Stack<GearItem> _itemPool = new(128);
    private static readonly Stack<GearAffix> _affixPool = new(512);

    public static GearItem RentItem()
    {
        return _itemPool.Count > 0 ? _itemPool.Pop() : new GearItem();
    }

    public static GearAffix RentAffix()
    {
        return _affixPool.Count > 0 ? _affixPool.Pop() : new GearAffix();
    }

    public static void Return(GearItem item)
    {
        if (item == null) return;

        // Return affixes first
        if (item.affixes != null)
        {
            foreach (var affix in item.affixes)
                Return(affix);
            item.affixes.Clear();
        }

        // Reset item state
        item.slot = null;
        item.rarity = null;
        item.core = null;

        _itemPool.Push(item);
    }

    public static void Return(GearAffix affix)
    {
        if (affix == null) return;
        affix.stat = null;
        affix.value = 0f;
        _affixPool.Push(affix);
    }

    // Pre-warm the pool at game start
    public static void Prewarm(int items = 64, int affixes = 256)
    {
        for (int i = 0; i < items; i++)
            _itemPool.Push(new GearItem { affixes = new List<GearAffix>(8) });
        for (int i = 0; i < affixes; i++)
            _affixPool.Push(new GearAffix());
    }
}
```

### 2.6.2 Update CraftingService to Use Pool

```csharp
public GearItem Craft(CoreSO core, ...)
{
    // ... validation ...

    var item = GearItemPool.RentItem();
    item.rarity = rarity;
    item.slot = slot;
    item.core = core;
    item.affixes ??= new List<GearAffix>(8);

    RollAffixes(item);
    // ...
    return item;
}

private void RollAffixes(GearItem item)
{
    // Use pooled affixes
    var affix = GearItemPool.RentAffix();
    affix.stat = guaranteed;
    affix.value = RollValue(guaranteed);
    item.affixes.Add(affix);
    // ...
}
```

### 2.6.3 Update SalvageService to Return to Pool

```csharp
public void Salvage(GearItem item, bool isAuto)
{
    // ... existing salvage logic ...

    // Return to pool instead of letting GC collect
    GearItemPool.Return(item);
}
```

**No editor changes required** - this is purely code-side pooling.

**Estimated savings:** ~90% reduction in GearItem/GearAffix allocations

---

## Phase 2.7: Background Thread Telemetry (Parallel Track F)

**Target:** Move all analytics processing off the main thread

### 2.7.1 Create `BackgroundTelemetryProcessor.cs`

**Location:** `Assets/Scripts/Gear/BackgroundTelemetryProcessor.cs`

```csharp
using System.Collections.Concurrent;
using System.Threading;

public class BackgroundTelemetryProcessor : MonoBehaviour
{
    public static BackgroundTelemetryProcessor Instance { get; private set; }

    private readonly ConcurrentQueue<CraftResult> _pendingResults = new();
    private Thread _processorThread;
    private volatile bool _running;
    private AutoResetEvent _workAvailable = new(false);

    // Cached reference to ForgeStats - only accessed from background thread
    private GameData.ForgeStats _forgeStats;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        _running = true;
        _forgeStats = Oracle.oracle?.saveData?.Forge;
        _processorThread = new Thread(ProcessLoop)
        {
            Name = "ForgeTelemetry",
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal
        };
        _processorThread.Start();
    }

    private void OnDisable()
    {
        _running = false;
        _workAvailable.Set(); // Wake thread to exit
        _processorThread?.Join(1000);
    }

    public void EnqueueResult(CraftResult result)
    {
        _pendingResults.Enqueue(result);
        _workAvailable.Set();
    }

    private void ProcessLoop()
    {
        while (_running)
        {
            _workAvailable.WaitOne(100); // Wake on signal or every 100ms

            while (_pendingResults.TryDequeue(out var result))
            {
                if (_forgeStats == null) continue;
                ProcessResultThreadSafe(result);
            }
        }
    }

    private void ProcessResultThreadSafe(CraftResult result)
    {
        // All dictionary operations here - off main thread
        // Use Interlocked for simple counters
        Interlocked.Increment(ref _forgeStats.TotalCrafts);

        // Dictionary updates need locks or concurrent collections
        lock (_forgeStats)
        {
            _forgeStats.CraftsByCore.Increment(result.Core?.name ?? "(null)");
            _forgeStats.CraftsByRarity.Increment(result.Rarity?.name ?? "(null)");
            _forgeStats.CraftsBySlot.Increment(result.Slot ?? "(null)");
            // ... rest of telemetry ...
        }
    }
}
```

### 2.7.2 Update ForgeAnalyticsService Integration

```csharp
public void RecordCraft(CraftResult result)
{
    if (BackgroundTelemetryProcessor.Instance != null)
    {
        // Fire and forget - background thread handles it
        BackgroundTelemetryProcessor.Instance.EnqueueResult(result);
    }
    else
    {
        // Fallback to synchronous if background processor unavailable
        RecordCraftImmediate(result);
    }
}
```

**Estimated savings:** Telemetry processing completely off main thread

---

## Phase 2.8: Burst-Mode Autocrafting with Skill-Based Speed (Sequential)

**Target:** Configurable craft rate unlocked via Mining skill milestones (similar to Cauldron taste speed)

### 2.8.1 Add Craft Speed Configuration

**In CraftingConfigSO:**

The base craft speed and per-milestone bonus are configurable for easy balance tuning:

```csharp
// In CraftingConfigSO
[Header("Craft Speed Settings")]
[Tooltip("Base crafts per second before any skill bonuses")]
public int baseCraftsPerSecond = 10;

[Tooltip("Additional crafts per second per milestone level (e.g., 10 = +10/sec per level)")]
public int craftsPerSecondPerMilestone = 10;

[Tooltip("Maximum crafts per second cap (0 = no cap)")]
public int maxCraftsPerSecond = 100;

[Tooltip("Batch interval in seconds (controls how often batches fire). Lower = smoother but more overhead.")]
public float batchInterval = 0.1f; // 10 batches/sec
```

**Example progression with defaults:**
| Milestone Level | Crafts/sec |
|-----------------|------------|
| 0 (base) | 10 |
| 1 | 20 |
| 2 | 30 |
| 3 | 40 |
| ... | ... |
| 9 | 100 (capped) |

### 2.8.2 Create Mining Skill Milestone for Forge Speed

**Location:** `Assets/Resources/Skills/Milestones/Mining/ForgeSpeed/`

Create milestone ScriptableObjects similar to Cauldron taste speed:

```csharp
// MilestoneDefinition already exists - create assets for each tier
// ForgeSpeed_Tier1.asset, ForgeSpeed_Tier2.asset, etc.

// The milestone effect is read at runtime via:
public int GetForgeCraftSpeedBonus()
{
    // Count completed forge speed milestones from Mining skill
    var miningSkill = SkillManager.Instance?.GetSkill("Mining");
    if (miningSkill == null) return 0;

    int bonus = 0;
    foreach (var milestone in miningSkill.Milestones)
    {
        if (milestone.IsCompleted && milestone.Definition.effectType == MilestoneEffectType.ForgeCraftSpeed)
            bonus += milestone.Definition.effectValue; // e.g., +10 crafts/sec per milestone
    }
    return bonus;
}
```

### 2.8.3 Add MilestoneEffectType for Forge Speed

**In MilestoneDefinition.cs or relevant enum:**

```csharp
public enum MilestoneEffectType
{
    // ... existing types ...
    CauldronTasteSpeed,    // Already exists
    ForgeCraftSpeed,       // NEW: Increases autocrafting speed
}
```

### 2.8.4 Update Autocrafting Coroutine for Dynamic Speed

```csharp
private IEnumerator CraftUntilUpgradeCoroutine()
{
    var config = crafting.Config;
    var craftsPerSecond = GetCurrentCraftsPerSecond();
    var batchInterval = config.batchInterval;
    var craftsPerBatch = Mathf.Max(1, Mathf.RoundToInt(craftsPerSecond * batchInterval));

    var wait = batchInterval > 0
        ? new WaitForSecondsRealtime(batchInterval)
        : null;

    ForgeAnalyticsService.Instance.BeginBatch();
    BackgroundTelemetryProcessor.Instance?.SetHighThroughputMode(true);

    try
    {
        while (isAutoCrafting)
        {
            if (!CanCraft()) break;

            // Craft entire batch in single frame
            GearItem bestUpgrade = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < craftsPerBatch && CanCraft(); i++)
            {
                var item = crafting.Craft(selectedCore, selectedSlot, null, coreRes);
                if (item == null) continue;

                var score = UpgradeEvaluator.ComputeUpgradeScore(crafting, item, eq);
                if (score > 0.0001f && score > bestScore)
                {
                    // Return previous best to pool
                    if (bestUpgrade != null)
                        SalvageService.Instance.Salvage(bestUpgrade, isAuto: true);

                    bestUpgrade = item;
                    bestScore = score;
                }
                else
                {
                    // Not an upgrade - salvage immediately
                    SalvageService.Instance.Salvage(item, isAuto: true);
                }
            }

            // Found an upgrade this batch?
            if (bestUpgrade != null)
            {
                lastCrafted = bestUpgrade;
                ShowResult(GearStatTextBuilder.BuildCraftResultSummary(bestUpgrade, eq));
                UpdateResultPreview(bestUpgrade);
                RecordUpgradeStop();
                break;
            }

            // Update UI once per batch (not per craft)
            OnResourcesChanged();

            if (wait != null)
                yield return wait;
            else
                yield return null;
        }
    }
    finally
    {
        BackgroundTelemetryProcessor.Instance?.SetHighThroughputMode(false);
        ForgeAnalyticsService.Instance.EndBatch();
        isAutoCrafting = false;
        RefreshActionButtons();
    }
}

/// <summary>
/// Calculate current crafts/sec based on config + skill milestones.
/// Formula: base + (milestoneLevels * perMilestoneBonus), capped at max.
/// </summary>
private int GetCurrentCraftsPerSecond()
{
    var config = crafting.Config;
    var baseSpeed = config.baseCraftsPerSecond;
    var perMilestone = config.craftsPerSecondPerMilestone;
    var maxSpeed = config.maxCraftsPerSecond;

    // Get milestone bonus from Mining skill
    int milestoneBonus = GetForgeCraftSpeedBonus();

    int total = baseSpeed + milestoneBonus;

    // Apply cap if configured
    if (maxSpeed > 0)
        total = Mathf.Min(total, maxSpeed);

    return Mathf.Max(1, total);
}

private int GetForgeCraftSpeedBonus()
{
    var miningSkill = SkillManager.Instance?.GetSkill("Mining");
    if (miningSkill == null) return 0;

    int bonus = 0;
    foreach (var milestone in miningSkill.CompletedMilestones)
    {
        if (milestone.Definition != null &&
            milestone.Definition.effectType == MilestoneEffectType.ForgeCraftSpeed)
        {
            bonus += Mathf.Max(0, milestone.Definition.effectValue);
        }
    }
    return bonus;
}
```

### 2.8.5 Add UI for Current Craft Speed (Optional)

Display current craft speed in forge UI so players can see their progress:

```csharp
// In ForgeWindowUI or a dedicated component
private void UpdateCraftSpeedDisplay()
{
    var speed = GetCurrentCraftsPerSecond();
    var maxSpeed = crafting.Config.maxCraftsPerSecond;

    if (craftSpeedText != null)
        craftSpeedText.text = maxSpeed > 0
            ? $"Craft Speed: {speed}/{maxSpeed} per sec"
            : $"Craft Speed: {speed} per sec";
}
```

### 2.8.6 Balance Configuration Notes

All values are in `CraftingConfigSO` for easy tuning without code changes:

| Parameter | Default | Description |
|-----------|---------|-------------|
| `baseCraftsPerSecond` | 10 | Starting speed for all players |
| `craftsPerSecondPerMilestone` | 10 | Bonus per completed milestone |
| `maxCraftsPerSecond` | 100 | Hard cap (0 = unlimited) |
| `batchInterval` | 0.1f | How often batches fire (lower = smoother) |

**To adjust progression:** Simply change milestone asset values or config defaults. No code changes needed.

**Estimated improvement at max (100/sec):** With object pooling + background telemetry, this runs smoothly with <2ms frame impact

---

## Phase 2.9: Optional Streamlined Analytics Mode (Sequential)

**Target:** Allow users to trade detailed stats for even more speed

### 2.9.1 Add Toggle to StaticReferences

```csharp
// In StaticReferences.cs
public static bool FastCraftMode
{
    get => Oracle.oracle?.saveData?.SavedPreferences?.FastCraftMode ?? false;
    set { if (Oracle.oracle?.saveData?.SavedPreferences != null) Oracle.oracle.saveData.SavedPreferences.FastCraftMode = value; }
}
```

### 2.9.2 Skip Detailed Telemetry in Fast Mode

```csharp
public void RecordCraft(CraftResult result)
{
    // Always record essential stats (thread-safe)
    Interlocked.Increment(ref forge.TotalCrafts);

    if (!StaticReferences.FastCraftMode)
    {
        // Detailed stat roll tracking - can skip for max speed
        RecordDetailedStats(result);
    }
}
```

---

## Stage 2 Summary

| Phase | Track | Dependencies | Performance Impact |
|-------|-------|--------------|-------------------|
| 2.1 | A | Stage 1.1 | -60% telemetry overhead |
| 2.2 | B | Stage 1.3 | -800 allocs/sec |
| 2.3 | C | None | -300 allocs/sec |
| 2.4 | - | 2.1, 2.2, 2.3 | 10 → 200+ crafts/sec (baseline) |
| 2.5 | D | None | Reduced UI churn |
| 2.6 | E | None | -90% GearItem allocations |
| 2.7 | F | Stage 1.1 | Telemetry off main thread |
| 2.8 | - | 2.4, 2.6, 2.7 | Skill-based speed: 10 → 100 crafts/sec |
| 2.9 | - | 2.7 | Optional additional 80% reduction |

**Parallel Execution:**
- Phases 2.1, 2.2, 2.3, 2.5, 2.6, 2.7 can be worked in parallel
- Phase 2.4 requires 2.1-2.3
- Phase 2.8 requires 2.4, 2.6, 2.7
- Phase 2.9 requires 2.7

---

## Projected Performance Gains

| Scenario | Current | With Tier 1 | With Tier 1+2 | With All |
|----------|---------|-------------|---------------|----------|
| Crafts/sec (autocrafting) | 10 | 50 | 100 | 100 (capped)* |
| GC pressure per 100 crafts | 350 KB | 140 KB | 50 KB | <10 KB |
| Frame time impact at 100/sec | ~15ms | ~6ms | ~2ms | <1ms |

*Cap is configurable in CraftingConfigSO. System can handle 1000+/sec if cap is raised.

**Note:** Final craft rate is player-upgradeable via Mining skill milestones (similar to Cauldron taste speed). All values configurable in `CraftingConfigSO` for balance tuning.

| Mining Milestone | Bonus | Total Crafts/sec |
|------------------|-------|------------------|
| Base (no milestones) | +0 | 10 |
| Forge Speed I | +10 | 20 |
| Forge Speed II | +10 | 30 |
| Forge Speed III | +10 | 40 |
| Forge Speed IV | +10 | 50 |
| Forge Speed V | +10 | 60 |
| Forge Speed VI | +10 | 70 |
| Forge Speed VII | +10 | 80 |
| Forge Speed VIII | +10 | 90 |
| Forge Speed IX | +10 | 100 (capped) |

*Values are configurable defaults - adjust `baseCraftsPerSecond`, `craftsPerSecondPerMilestone`, and `maxCraftsPerSecond` in CraftingConfigSO for balance.*

---

## Final Code Summary

| Metric | Current | After Stage 1 | After Stage 2 |
|--------|---------|---------------|---------------|
| CraftingService.cs lines | 507 | ~330 | ~380 |
| ForgeWindowUI.cs lines | ~2400 | ~1600 | ~1700 |
| Total new files | 0 | 8 | 11 |
| New MonoBehaviours | 0 | 4 | 6 |
| New static utilities | 0 | 2 | 3 |

---

## Implementation Order

### Recommended Sequence

```
Week 1 - Stage 1 (Refactoring):
├── [Parallel] Phase 1.1 (Telemetry extraction)
├── [Parallel] Phase 1.2 (Conversion pipeline)
├── [Parallel] Phase 1.3 (Score evaluation)
└── [Parallel] Phase 1.4 (UI subsystems)

Week 1 End:
└── [Sequential] Phase 1.5 (Event handlers)

Week 2 - Stage 2 Part A (Core Optimizations):
├── [Parallel] Phase 2.1 (Deferred telemetry)
├── [Parallel] Phase 2.2 (Cache scores)
├── [Parallel] Phase 2.3 (Pool lists)
├── [Parallel] Phase 2.5 (UI throttling)
├── [Parallel] Phase 2.6 (Object pooling)
└── [Parallel] Phase 2.7 (Background telemetry)

Week 2 End:
└── [Sequential] Phase 2.4 (Batch crafting API)

Week 3 - Stage 2 Part B (Advanced Features):
├── [Sequential] Phase 2.8 (Burst mode + upgradeable rate)
└── [Sequential] Phase 2.9 (Fast mode toggle)
```

### Dependency Graph

```
Stage 1:
  1.1 ──┐
  1.2 ──┼──► 1.5
  1.3 ──┤
  1.4 ──┘

Stage 2:
  2.1 ──────────┐
  2.2 ──────────┼──► 2.4 ──┐
  2.3 ──────────┘         │
  2.5 (independent)       ├──► 2.8 ──► 2.9
  2.6 ────────────────────┤
  2.7 ────────────────────┘
```

---

## Risk Mitigation

### Editor Reference Preservation

All new MonoBehaviour components will be added as children or siblings of ForgeWindowUI, with SerializeField references maintained:

1. **ForgeResultPreview** - Add as component on existing result panel GameObject
2. **ForgeIvanXpDisplay** - Add as component on existing Ivan XP panel
3. **ForgeSlotManager** - Internal class, no scene references needed
4. **ForgeAnalyticsService** - Add to GameManager or similar persistent object
5. **GearItemPool** - Static class, no scene references needed
6. **BackgroundTelemetryProcessor** - Add to GameManager or similar persistent object

### Object Pooling Safety

- Pool is purely code-side - no editor changes required
- `GearItem` and `GearAffix` classes remain unchanged
- Pool automatically grows if demand exceeds capacity
- Returned items are reset to clean state before reuse

### Thread Safety for Background Telemetry

- Use `ConcurrentQueue` for thread-safe enqueueing
- Lock ForgeStats dictionary operations
- Use `Interlocked` for simple counters
- Background thread runs at `BelowNormal` priority to avoid starving main thread

### Testing Checkpoints

After each phase:
1. Verify all crafting operations work correctly
2. Verify all UI updates display correctly
3. Verify telemetry is recorded (check ForgeStats values)
4. Run 1000-craft stress test to verify no memory leaks
5. **Phase 2.6:** Verify items are properly returned to pool after salvage
6. **Phase 2.7:** Verify telemetry still records correctly with background processing
7. **Phase 2.8:** Test craft speed scales correctly with Mining milestones
8. **Phase 2.8:** Verify cap is enforced and configurable values work

### Rollback Strategy

Each phase creates new files rather than heavily modifying existing ones. If issues arise:
1. Revert to using original methods
2. Keep new classes for future iteration
3. **Object pooling:** Can be disabled by using `new GearItem()` instead of `GearItemPool.RentItem()`
4. **Background telemetry:** Falls back to synchronous if processor unavailable

---

## Key Files Reference

| New File | Purpose | Dependencies |
|----------|---------|--------------|
| `ForgeAnalyticsService.cs` | Centralized telemetry | DictionaryExtensions |
| `DictionaryExtensions.cs` | Dictionary helper methods | None |
| `ConversionPipeline.cs` | Conversion abstraction | None |
| `ConversionPipelineFactory.cs` | Pipeline configuration | ConversionPipeline |
| `ScoreEvaluationService.cs` | Cached score calculations | None |
| `ForgeSlotManager.cs` | Slot selection state | None |
| `ForgeResultPreview.cs` | Result display UI | None |
| `ForgeIvanXpDisplay.cs` | Ivan XP UI | None |
| `GearItemPool.cs` | Object pooling for gear | None |
| `BackgroundTelemetryProcessor.cs` | Off-thread analytics | ForgeAnalyticsService |
| `ForgeSpeed_TierX.asset` (×9) | Mining milestones for craft speed | MilestoneDefinition |
| `MilestoneEffectType.ForgeCraftSpeed` | New enum value | MilestoneDefinition |

---

## Player Progression Integration

The craft speed system ties into the existing **Mining skill milestone system** (same pattern as Cauldron taste speed):

### How It Works

1. **Base speed** starts at 10 crafts/sec for all players
2. **Mining skill milestones** (Forge Speed I-IX) each grant +10 crafts/sec
3. **Configurable cap** (default 100/sec) prevents runaway speeds
4. **All values in ScriptableObjects** - no code changes needed for balance tuning

### Milestone Asset Structure

```
Assets/Resources/Skills/Milestones/Mining/
├── ForgeSpeed/
│   ├── ForgeSpeed_Tier1.asset  (requires Mining level X, grants +10/sec)
│   ├── ForgeSpeed_Tier2.asset  (requires Mining level Y, grants +10/sec)
│   └── ...
```

### Why Mining Skill?

- **Thematic fit:** Mining provides forge materials (ores, chunks, crystals, ingots)
- **Consistent pattern:** Mirrors Cauldron speed upgrades on relevant skill
- **Progression incentive:** Encourages Mining investment beyond just resource gathering

### Balance Flexibility

All values are designer-tunable without code changes:

| Config Field | Location | Purpose |
|--------------|----------|---------|
| `baseCraftsPerSecond` | CraftingConfigSO | Starting speed |
| `craftsPerSecondPerMilestone` | CraftingConfigSO | Bonus per milestone |
| `maxCraftsPerSecond` | CraftingConfigSO | Hard cap |
| `effectValue` | Each MilestoneDefinition | Individual milestone bonus |
| Milestone requirements | MilestoneDefinition | When each tier unlocks |

This allows rapid iteration on progression feel without rebuilding.

---

## Approval Required

Please review this plan and confirm:
1. The proposed file structure is acceptable
2. The parallel execution strategy is understood
3. The Mining skill milestone system for craft speed is acceptable
4. The default progression (10/sec base → 100/sec max in +10 increments) is acceptable as a starting point
5. Any phases should be prioritized or deprioritized
6. Any additional requirements or constraints

Once approved, implementation will begin with Phase 1.1-1.4 in parallel.
