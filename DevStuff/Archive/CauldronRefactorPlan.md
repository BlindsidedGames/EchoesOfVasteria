# Cauldron System Refactoring Plan

## Overview

This plan refactors the Cauldron system in two stages:
- **Stage 1:** Reduce monolithic scripts, improve code organization, apply DRY principles
- **Stage 2:** Implement performance optimizations from [CauldronPerformanceAnalysis.md](CauldronPerformanceAnalysis.md)

Each stage is divided into phases designed for **parallel execution** where possible, with clear dependencies noted.

---

## Current State

| File | Lines | Responsibilities |
|------|-------|------------------|
| CauldronManager.cs | 1297 | Eva progression, card pools, tiers, tasting, groups, weights, events |
| CauldronWindowUI.cs | 755 | Mixing, tasting, pie chart, stats, weights preview |
| CauldronConfig.cs | 79 | Configuration SO |

**Target:** Reduce CauldronManager to ~650 lines, CauldronWindowUI to ~400 lines

---

# Stage 1: Refactoring & DRY

## Phase 1A: Extract Utility Classes (No Dependencies)

**Can run in parallel with:** 1B, 1C

### 1A.1: CardIdentifierFactory (Static Utility)

**Purpose:** Centralize card ID string construction/parsing

**Extract from:**
- [CauldronManager.cs:208](Assets/Scripts/Upgrades/CauldronManager.cs#L208) `$"RES:{resourceName}"`
- [CauldronManager.cs:217](Assets/Scripts/Upgrades/CauldronManager.cs#L217) `$"BUFF:{buffName}"`
- [CauldronManager.cs:918](Assets/Scripts/Upgrades/CauldronManager.cs#L918) `$"RES:{res.name}"`
- [CauldronManager.cs:934](Assets/Scripts/Upgrades/CauldronManager.cs#L934) `$"BUFF:{buff.name}"`
- [CauldronManager.cs:948](Assets/Scripts/Upgrades/CauldronManager.cs#L948) `$"INF:{inf.Stat}"`
- [CauldronManager.cs:293-295](Assets/Scripts/Upgrades/CauldronManager.cs#L293) ID prefix parsing

**New file:** `Assets/Scripts/Upgrades/Cauldron/CardIdentifierFactory.cs`

```csharp
public static class CardIdentifierFactory
{
    public const string ResourcePrefix = "RES:";
    public const string BuffPrefix = "BUFF:";
    public const string InfinityPrefix = "INF:";

    public static string ForResource(string name) => $"{ResourcePrefix}{name}";
    public static string ForResource(Resource res) => ForResource(res.name);
    public static string ForBuff(string name) => $"{BuffPrefix}{name}";
    public static string ForBuff(BuffRecipe buff) => ForBuff(buff.name);
    public static string ForInfinity(HeroStatMapping stat) => $"{InfinityPrefix}{stat}";

    public static bool IsResource(string id) => id?.StartsWith(ResourcePrefix) ?? false;
    public static bool IsBuff(string id) => id?.StartsWith(BuffPrefix) ?? false;
    public static bool IsInfinity(string id) => id?.StartsWith(InfinityPrefix) ?? false;

    public static string ExtractName(string id)
    {
        if (IsResource(id)) return id.Substring(ResourcePrefix.Length);
        if (IsBuff(id)) return id.Substring(BuffPrefix.Length);
        if (IsInfinity(id)) return id.Substring(InfinityPrefix.Length);
        return id;
    }
}
```

**Impact:** ~15 locations updated, prevents string format bugs

---

### 1A.2: ThrottledAction (Reusable Throttling)

**Purpose:** DRY the repeated throttling pattern (3 implementations in CauldronManager)

**Extract from:**
- [CauldronManager.cs:57-62](Assets/Scripts/Upgrades/CauldronManager.cs#L57) `_nextStatsEmitTime`, `_nextSessionCardsEmitTime`
- [CauldronManager.cs:1275-1295](Assets/Scripts/Upgrades/CauldronManager.cs#L1275) `ShouldEmitStatsNow()`, `ShouldEmitSessionCardsNow()`
- [CauldronManager.cs:1206-1214](Assets/Scripts/Upgrades/CauldronManager.cs#L1206) `DebouncedWeightsChanged()`

**New file:** `Assets/Scripts/Utilities/ThrottledAction.cs`

```csharp
public class ThrottledAction
{
    private float _nextAllowedTime;
    private readonly float _minInterval;

    public ThrottledAction(float minIntervalSeconds)
    {
        _minInterval = minIntervalSeconds;
    }

    public bool TryExecute()
    {
        var now = Time.unscaledTime;
        if (now >= _nextAllowedTime)
        {
            _nextAllowedTime = now + _minInterval;
            return true;
        }
        return false;
    }

    public void Reset() => _nextAllowedTime = 0f;
}
```

**Impact:** Replaces 3 throttle implementations, reusable across codebase

---

### 1A.3: TastingStatsFormatter (Stateless Utility)

**Purpose:** Extract stats formatting logic from UI

**Extract from:**
- [CauldronWindowUI.cs:597-625](Assets/Scripts/UI/CauldronWindowUI.cs#L597) `OnStatsChanged()` StringBuilder logic

**New file:** `Assets/Scripts/Upgrades/Cauldron/TastingStatsFormatter.cs`

```csharp
public static class TastingStatsFormatter
{
    public static void FormatStats(StringBuilder sb, TastingStats stats, bool showSubcategories)
    {
        sb.Clear();
        sb.Append($"Tastings: {stats.tastings:N0}\n");
        sb.Append($"Cards: {stats.cardsGained:N0}\n");
        // ... remaining formatting logic
    }
}
```

**Impact:** ~30 lines extracted from UI, improves testability

---

## Phase 1B: Extract Service Classes (No Dependencies)

**Can run in parallel with:** 1A, 1C

### 1B.1: CardTierCalculator (Tier Logic Service)

**Purpose:** Centralize all tier threshold calculations

**Extract from:**
- [CauldronManager.cs:161-221](Assets/Scripts/Upgrades/CauldronManager.cs#L161) `GetTierFromThresholds()`, `GetResourceTier()`, `GetBuffTier()`, `GetTierFill01()`
- [CauldronManager.cs:274-296](Assets/Scripts/Upgrades/CauldronManager.cs#L274) `IsResourceMaxed()`, `IsBuffMaxed()`, `IsIdMaxed()`

**New file:** `Assets/Scripts/Upgrades/Cauldron/CardTierCalculator.cs`

```csharp
public class CardTierCalculator
{
    private readonly CauldronConfig _config;
    private readonly Func<Dictionary<string, int>> _cardCountsGetter;

    public CardTierCalculator(CauldronConfig config, Func<Dictionary<string, int>> cardCountsGetter)
    {
        _config = config;
        _cardCountsGetter = cardCountsGetter;
    }

    public int GetTier(string cardId)
    public int GetResourceTier(string resourceName)
    public int GetBuffTier(string buffName)
    public float GetTierProgress(string cardId) // 0-1 fill
    public bool IsMaxed(string cardId)
    public int GetCount(string cardId)

    private int GetTierFromThresholds(int count, int[] thresholds)
}
```

**Impact:** ~80 lines extracted, eliminates duplication in CollectionsWindowUI

---

### 1B.2: EvaProgressionService (Eva Level/XP)

**Purpose:** Isolate Eva leveling logic from tasting loop

**Extract from:**
- [CauldronManager.cs:132-148](Assets/Scripts/Upgrades/CauldronManager.cs#L132) `EvaLevel`, `EvaXp` properties
- [CauldronManager.cs:526-541](Assets/Scripts/Upgrades/CauldronManager.cs#L526) `GainEvaXp()`, `GetXpToNextLevel()`

**New file:** `Assets/Scripts/Upgrades/Cauldron/EvaProgressionService.cs`

```csharp
public class EvaProgressionService
{
    public event Action OnLevelUp;

    public int Level { get; private set; }
    public double Xp { get; private set; }
    public double XpToNextLevel => 50 + 10 * Math.Max(0, Level - 1);
    public float XpProgress => (float)(Xp / XpToNextLevel);

    public void GainXp(double amount)
    {
        Xp += amount;
        while (Xp >= XpToNextLevel)
        {
            Xp -= XpToNextLevel;
            Level++;
            OnLevelUp?.Invoke();
        }
    }

    public void LoadFromSave(int level, double xp)
    public void SaveTo(out int level, out double xp)
}
```

**Impact:** ~40 lines extracted, cleaner event handling

---

### 1B.3: AEResourceGroupClassifier (Resource Categorization)

**Purpose:** Centralize resource→group classification logic

**Extract from:**
- [CauldronManager.cs:983-1084](Assets/Scripts/Upgrades/CauldronManager.cs#L983) `GetResourceGroup()` (75 lines)
- [CauldronManager.cs:1059-1084](Assets/Scripts/Upgrades/CauldronManager.cs#L1059) `InferGroupFromTask()`

**New file:** `Assets/Scripts/Upgrades/Cauldron/AEResourceGroupClassifier.cs`

```csharp
public class AEResourceGroupClassifier
{
    private readonly Dictionary<Resource, AEResourceGroup> _cache = new();

    public AEResourceGroup Classify(Resource res)
    {
        if (res == null) return AEResourceGroup.Combat;
        if (_cache.TryGetValue(res, out var cached)) return cached;

        var group = ClassifyInternal(res);
        _cache[res] = group;
        return group;
    }

    private AEResourceGroup ClassifyInternal(Resource res)
    {
        // 1. Check explicit override
        // 2. Infer from task drops
        // 3. Infer from enemy drops
        // 4. Default to Combat
    }

    public void ClearCache() => _cache.Clear();
}
```

**Impact:** ~75 lines extracted, reusable by UI layer

---

## Phase 1C: Extract UI Presenters (No Dependencies)

**Can run in parallel with:** 1A, 1B

### 1C.1: CauldronPieChartPresenter

**Purpose:** Isolate pie chart rendering logic

**Extract from:**
- [CauldronWindowUI.cs:470-563](Assets/Scripts/UI/CauldronWindowUI.cs#L470) `RefreshPieChart()` (94 lines)

**New file:** `Assets/Scripts/UI/Cauldron/CauldronPieChartPresenter.cs`

```csharp
public class CauldronPieChartPresenter : MonoBehaviour
{
    [SerializeField] private List<MPImageBasic> pieSlices;
    private float[] _fractionsBuffer;

    public void Refresh(EffectiveWeightsSnapshot weights, CauldronConfig config)
    {
        var slices = BuildSliceData(weights, config);
        RenderSlices(slices);
    }

    private List<(Color color, float weight)> BuildSliceData(...)
    private void RenderSlices(List<(Color, float)> slices)
}
```

**Impact:** ~94 lines extracted, cleaner separation

---

### 1C.2: CauldronWeightsPresenter

**Purpose:** Isolate weights preview/tooltip logic

**Extract from:**
- [CauldronWindowUI.cs:638-753](Assets/Scripts/UI/CauldronWindowUI.cs#L638) `RefreshWeightsText()`, `ShowWeightsTooltip()`, `HideWeightsTooltip()`

**New file:** `Assets/Scripts/UI/Cauldron/CauldronWeightsPresenter.cs`

```csharp
public class CauldronWeightsPresenter : MonoBehaviour
{
    [SerializeField] private TMP_Text firstPercentText;
    [SerializeField] private TMP_Text spriteColText;
    [SerializeField] private TMP_Text nextPercentText;
    [SerializeField] private TMP_Text nameColText;
    [SerializeField] private GameObject tooltipObject;

    // Reusable StringBuilders (DRY from Phase 1A)
    private readonly StringBuilder _colFirst = new(256);
    private readonly StringBuilder _colSprite = new(256);
    private readonly StringBuilder _colNext = new(256);
    private readonly StringBuilder _colName = new(256);

    public void Refresh(int currentLevel, CauldronConfig config, CauldronManager cauldron)
    public void ShowTooltip()
    public void HideTooltip()
}
```

**Impact:** ~115 lines extracted, fixes StringBuilder allocation issue

---

### 1C.3: CauldronMixPresenter

**Purpose:** Isolate mixing UI logic

**Extract from:**
- [CauldronWindowUI.cs:186-393](Assets/Scripts/UI/CauldronWindowUI.cs#L186) Mix slot management, selection state, eligibility

**New file:** `Assets/Scripts/UI/Cauldron/CauldronMixPresenter.cs`

```csharp
public class CauldronMixPresenter : MonoBehaviour
{
    [SerializeField] private List<CauldronMixItemUIReferences> mixSlots;
    [SerializeField] private CauldronMixItemUIReferences slot1, slot2;
    [SerializeField] private Button mixButton;
    [SerializeField] private TMP_Text predictedStewText;

    private Resource _selectedA, _selectedB;
    private bool _nextGreen = true;

    public event Action<Resource, Resource> OnMixRequested;
    public event Action OnMixAllRequested;

    public void RefreshSlots(List<Resource> eligibleFoods, ResourceManager rm)
    public void RefreshMixButton(ResourceManager rm)
    private void ToggleSelection(Resource r)
}
```

**Impact:** ~150 lines extracted, cleaner state management

---

## Phase 1D: Extract Core Logic Classes (Depends on 1A, 1B)

**Requires:** Phase 1A complete (CardIdentifierFactory), Phase 1B complete (CardTierCalculator)

### 1D.1: CardPoolManager

**Purpose:** Centralize all card pool caching and rebuilding

**Extract from:**
- [CauldronManager.cs:48-56](Assets/Scripts/Upgrades/CauldronManager.cs#L48) Pool lists
- [CauldronManager.cs:897-981](Assets/Scripts/Upgrades/CauldronManager.cs#L897) `RebuildCardPoolsIfDirty()`, `BuildResourceIdsForGroup()`

**New file:** `Assets/Scripts/Upgrades/Cauldron/CardPoolManager.cs`

```csharp
public class CardPoolManager
{
    public event Action OnPoolsRebuilt;

    private readonly List<string> _alterEchoPool = new();
    private readonly List<string> _buffPool = new();
    private readonly List<string> _allPool = new();
    private readonly List<string> _infinityPool = new();
    private readonly Dictionary<AEResourceGroup, List<string>> _groupPools = new();

    private readonly CardTierCalculator _tierCalc;
    private readonly AEResourceGroupClassifier _groupClassifier;
    private readonly ThrottledAction _rebuildThrottle;

    private bool _dirty = true;

    public IReadOnlyList<string> AlterEchoCards => _alterEchoPool;
    public IReadOnlyList<string> BuffCards => _buffPool;
    public IReadOnlyList<string> AllCards => _allPool;
    public IReadOnlyList<string> InfinityCards => _infinityPool;

    public IReadOnlyList<string> GetGroupPool(AEResourceGroup group)
    public void MarkDirty()
    public void RebuildIfNeeded()

    // Cache eligibility flags for performance (Stage 2)
    public bool HasCardsForGroup(AEResourceGroup group)
}
```

**Impact:** ~120 lines extracted, foundation for performance optimizations

**Dependencies:**
- Uses `CardIdentifierFactory` for ID construction
- Uses `CardTierCalculator` for `IsIdMaxed()` checks
- Uses `AEResourceGroupClassifier` for group lookups

---

### 1D.2: TasteRollResolver

**Purpose:** Encapsulate the entire taste outcome resolution

**Extract from:**
- [CauldronManager.cs:560-682](Assets/Scripts/Upgrades/CauldronManager.cs#L560) `ResolveTasteOutcome()` (122 lines)
- [CauldronManager.cs:684-720](Assets/Scripts/Upgrades/CauldronManager.cs#L684) `GrantRandomCards()`, etc.
- [CauldronManager.cs:722-772](Assets/Scripts/Upgrades/CauldronManager.cs#L722) `PickRandomCardId()`, `GetLowestCountCardId()`

**New file:** `Assets/Scripts/Upgrades/Cauldron/TasteRollResolver.cs`

```csharp
public class TasteRollResolver
{
    public struct RollResult
    {
        public RollType Type;
        public List<string> GrantedCardIds;
        public int BaseCardCount;
        public int ScaledCardCount;
    }

    private readonly CardPoolManager _poolManager;
    private readonly CauldronConfig _config;

    public RollResult Resolve(int evaLevel, float cardMultiplier)
    {
        var weights = ComputeEffectiveWeights(evaLevel);
        var rollType = SelectRollType(weights);
        var cards = GrantCardsForRoll(rollType, cardMultiplier);
        return new RollResult { Type = rollType, GrantedCardIds = cards, ... };
    }

    private EffectiveWeightsSnapshot ComputeEffectiveWeights(int level)
    private RollType SelectRollType(EffectiveWeightsSnapshot weights)
    private List<string> GrantCardsForRoll(RollType type, float multiplier)
}
```

**Impact:** ~170 lines extracted, testable roll distribution

**Dependencies:**
- Uses `CardPoolManager` for available pools
- Uses `CardIdentifierFactory` for ID construction

---

## Phase 1E: Integration & Wiring (Depends on 1D)

**Requires:** Phase 1D complete

### 1E.1: Update CauldronManager

**Changes:**
1. Add fields for extracted services
2. Wire up in `Awake()`
3. Replace inline code with service calls
4. Maintain public API compatibility

```csharp
public class CauldronManager : Singleton<CauldronManager>
{
    // NEW: Extracted services
    private CardTierCalculator _tierCalculator;
    private CardPoolManager _poolManager;
    private TasteRollResolver _rollResolver;
    private EvaProgressionService _evaProgression;
    private AEResourceGroupClassifier _groupClassifier;

    // Throttled actions (replacing manual throttle fields)
    private ThrottledAction _statsEmitThrottle;
    private ThrottledAction _sessionCardsEmitThrottle;
    private ThrottledAction _weightsNotifyThrottle;

    protected override void Awake()
    {
        base.Awake();
        InitializeServices();
    }

    private void InitializeServices()
    {
        _tierCalculator = new CardTierCalculator(config, () => oracle?.saveData.CauldronCardCounts);
        _groupClassifier = new AEResourceGroupClassifier();
        _poolManager = new CardPoolManager(_tierCalculator, _groupClassifier, ...);
        _rollResolver = new TasteRollResolver(_poolManager, config);
        _evaProgression = new EvaProgressionService();
        _evaProgression.OnLevelUp += () => OnWeightsChanged?.Invoke();

        // Throttles
        _statsEmitThrottle = new ThrottledAction(0.2f);
        _sessionCardsEmitThrottle = new ThrottledAction(0.2f);
        _weightsNotifyThrottle = new ThrottledAction(weightsNotifyInterval);
    }

    // PUBLIC API remains unchanged for compatibility
    public int GetResourceTier(string name) => _tierCalculator.GetResourceTier(name);
    public int GetBuffTier(string name) => _tierCalculator.GetBuffTier(name);
    public AEResourceGroup GetResourceGroup(Resource res) => _groupClassifier.Classify(res);
}
```

---

### 1E.2: Update CauldronWindowUI

**Changes:**
1. Add references to extracted presenters
2. Wire up in `Awake()`
3. Delegate to presenters in refresh methods

```csharp
public class CauldronWindowUI : MonoBehaviour
{
    // NEW: Extracted presenters
    [SerializeField] private CauldronMixPresenter mixPresenter;
    [SerializeField] private CauldronPieChartPresenter pieChartPresenter;
    [SerializeField] private CauldronWeightsPresenter weightsPresenter;

    // Simplified refresh methods delegate to presenters
    private void RefreshPieChart()
    {
        var weights = cauldron.GetEffectiveWeightsAtLevel(cauldron.EvaLevel);
        pieChartPresenter.Refresh(weights, config);
    }
}
```

---

### 1E.3: Verify Editor References

**Checklist:**
- [ ] All SerializeField references in CauldronManager preserved
- [ ] All SerializeField references in CauldronWindowUI preserved
- [ ] Scene references to CauldronManager.Instance work correctly
- [ ] Prefab references unbroken
- [ ] No null reference exceptions on play

---

## Stage 1 Summary

| Phase | Files Created | Lines Extracted | Can Parallel With |
|-------|---------------|-----------------|-------------------|
| 1A | 3 utilities | ~60 | 1B, 1C |
| 1B | 3 services | ~195 | 1A, 1C |
| 1C | 3 presenters | ~360 | 1A, 1B |
| 1D | 2 core classes | ~290 | - (needs 1A, 1B) |
| 1E | 0 (updates only) | 0 | - (needs 1D) |

**Parallel execution:** Phases 1A, 1B, 1C can all run simultaneously (3 parallel tracks)

---

# Stage 2: Performance Optimizations

**Prerequisite:** Stage 1 complete (especially CardPoolManager extraction)

## Phase 2A: Tier 1 Optimizations (Quick Wins)

**Can run in parallel with:** 2B (if careful about conflicts)

### 2A.1: Cache Group Eligibility

**Location:** `CardPoolManager.cs` (new file from 1D.1)

**Change:** Add cached eligibility flags rebuilt with pools

```csharp
// Add to CardPoolManager
private readonly bool[] _groupHasCards = new bool[6];

private void RebuildPools()
{
    // ... existing rebuild logic ...

    // Cache eligibility
    for (int i = 0; i < 6; i++)
    {
        var group = (AEResourceGroup)i;
        _groupHasCards[i] = _groupPools.TryGetValue(group, out var list) && list.Count > 0;
    }
}

public bool HasCardsForGroup(AEResourceGroup group) => _groupHasCards[(int)group];
```

**Impact:** Eliminates 6x `BuildResourceIdsForGroup()` calls per roll

---

### 2A.2: Throttle OnStewChanged Events

**Location:** `CauldronManager.cs`

**Change:** Add throttling to Stew property setter

```csharp
private ThrottledAction _stewChangeThrottle;
private double _lastEmittedStew;

public double Stew
{
    get => oracle?.saveData.CauldronStew ?? 0;
    private set
    {
        if (oracle == null) return;
        oracle.saveData.CauldronStew = Math.Max(0, value);

        // Throttle: emit max 10 Hz or if changed significantly
        if (_stewChangeThrottle.TryExecute() || Math.Abs(value - _lastEmittedStew) > 10)
        {
            _lastEmittedStew = value;
            OnStewChanged?.Invoke();
        }
    }
}
```

**Impact:** 10x reduction in UI update frequency

---

### 2A.3: Reuse StringBuilders in WeightsPresenter

**Location:** `CauldronWeightsPresenter.cs` (new file from 1C.2)

**Change:** Already addressed in extraction - use member field StringBuilders instead of local allocation

**Impact:** Eliminates 4 allocations per weights refresh

---

## Phase 2B: Tier 2 Optimizations (Medium)

**Can run in parallel with:** 2A (if careful about conflicts)

### 2B.1: Batch Card Additions

**Location:** `CauldronManager.cs`

**Change:** Batch card grants and flush downstream updates once per roll

```csharp
private readonly List<(string id, int count)> _pendingCardGains = new();

private void AddCardCountBatched(string id, int delta)
{
    _pendingCardGains.Add((id, delta));
}

private void FlushPendingCards()
{
    if (_pendingCardGains.Count == 0) return;

    bool anyBuffTierChanged = false;
    bool anyResourceTierChanged = false;

    foreach (var (id, delta) in _pendingCardGains)
    {
        // Update dict only
        var dict = oracle.saveData.CauldronCardCounts;
        if (!dict.ContainsKey(id)) dict[id] = 0;

        var oldTier = _tierCalculator.GetTier(id);
        dict[id] += delta;
        var newTier = _tierCalculator.GetTier(id);

        if (oldTier != newTier)
        {
            if (CardIdentifierFactory.IsBuff(id)) anyBuffTierChanged = true;
            if (CardIdentifierFactory.IsResource(id)) anyResourceTierChanged = true;
        }

        sessionCardsGained += delta;
        OnCardGained?.Invoke(id, delta);
    }

    // Cascade updates ONCE
    AlterEchoGenerationManager.Instance?.MarkRatesDirty();
    if (anyBuffTierChanged) BuffManager.Instance?.RecomputeActiveBuffEffects();
    HeroStatSystem.MarkDirty(...);

    _poolManager.MarkDirty();
    _pendingCardGains.Clear();
}

// Update TasteTick to flush after resolve
private void TasteTick()
{
    // ... existing logic ...
    var result = _rollResolver.Resolve(EvaLevel, cardMultiplier);
    foreach (var cardId in result.GrantedCardIds)
        AddCardCountBatched(cardId, 1);
    FlushPendingCards();
}
```

**Impact:** 5-10x reduction for multi-card rolls (VastSurge x10)

---

### 2B.2: Cache Asset Lists

**Location:** `CardPoolManager.cs`

**Change:** Cache AssetCache.GetAll() results at startup

```csharp
private Resource[] _allResources;
private BuffRecipe[] _allBuffs;
private InfinityCauldronStatSO[] _allInfinity;

public void Initialize()
{
    _allResources = AssetCache.GetAll<Resource>().ToArray();
    _allBuffs = AssetCache.GetAll<BuffRecipe>().ToArray();
    _allInfinity = AssetCache.GetAll<InfinityCauldronStatSO>("Infinity").ToArray();
}

private void RebuildPools()
{
    _alterEchoPool.Clear();
    // ...

    // Use cached arrays instead of GetAll()
    foreach (var res in _allResources)
    {
        if (res == null || res.DisableAlterEcho) continue;
        // ...
    }
}
```

**Impact:** 30-50% faster pool rebuilds

---

### 2B.3: Cache String IDs

**Location:** `CardPoolManager.cs`

**Change:** Pre-compute card IDs on first pool build

```csharp
private readonly Dictionary<Resource, string> _resourceIdCache = new();
private readonly Dictionary<BuffRecipe, string> _buffIdCache = new();

private string GetResourceId(Resource res)
{
    if (!_resourceIdCache.TryGetValue(res, out var id))
    {
        id = CardIdentifierFactory.ForResource(res);
        _resourceIdCache[res] = id;
    }
    return id;
}
```

**Impact:** 5-10% reduction in GC pressure

---

## Phase 2C: Advanced Optimizations (All)

**Requires:** Phases 2A and 2B complete

### 2C.1: Pre-compute Resource Groups at Startup

**Location:** `AEResourceGroupClassifier.cs`

**Change:** Eagerly classify all resources during initialization

```csharp
public void PrecomputeAll(IEnumerable<Resource> resources)
{
    foreach (var res in resources)
    {
        if (res != null)
            Classify(res); // Populates cache
    }
}
```

**Called from:** `CardPoolManager.Initialize()`

**Impact:** Eliminates lazy classification during tasting

---

### 2C.2: Optimize Lowest Card Lookup

**Location:** `TasteRollResolver.cs`

**Change:** Track lowest card during pool rebuild instead of linear search

```csharp
// In CardPoolManager
private string _lowestCardId;
private int _lowestCardCount = int.MaxValue;

public string GetLowestCardId() => _lowestCardId;

private void RebuildPools()
{
    // ... existing logic ...

    // Track lowest during rebuild
    _lowestCardCount = int.MaxValue;
    _lowestCardId = null;

    foreach (var id in _allPool)
    {
        var count = _tierCalc.GetCount(id);
        if (count < _lowestCardCount)
        {
            _lowestCardCount = count;
            _lowestCardId = id;
        }
    }
}
```

**Impact:** O(1) instead of O(n) for lowest card rolls

---

### 2C.3: Update Configuration Defaults

**Location:** `CauldronConfig.cs`

**Change:** Add new throttle config, adjust defaults for higher rates

```csharp
[Header("Performance")]
[Min(0.01f)] public float stewChangeThrottleInterval = 0.1f;

// Consider updating defaults:
// rollsPerSecond: 10 → 50 (or make configurable per-player)
// cardPoolsRebuildMinInterval: 0.5s → 1.0s
// weightsNotifyInterval: 0.25s → 0.5s
```

---

## Stage 2 Summary

| Phase | Optimization | Impact | Can Parallel With |
|-------|--------------|--------|-------------------|
| 2A.1 | Cache group eligibility | 10-20x hot path | 2B |
| 2A.2 | Throttle stew events | 10x UI updates | 2B |
| 2A.3 | Reuse StringBuilders | 4 allocs/refresh | 2B |
| 2B.1 | Batch card additions | 5-10x multi-card | 2A |
| 2B.2 | Cache asset lists | 30-50% rebuild | 2A |
| 2B.3 | Cache string IDs | 5-10% GC | 2A |
| 2C.1 | Pre-compute groups | Startup cost only | - |
| 2C.2 | Optimize lowest card | O(1) lookup | - |
| 2C.3 | Update config | N/A | - |

**Parallel execution:** Phases 2A and 2B can run simultaneously (2 parallel tracks)

---

# Execution Timeline

```
Week 1: Stage 1 (Refactoring)
├─ Day 1-2: Phases 1A + 1B + 1C (parallel)
│   ├─ Track A: 1A (utilities)
│   ├─ Track B: 1B (services)
│   └─ Track C: 1C (presenters)
├─ Day 3-4: Phase 1D (core classes)
└─ Day 5: Phase 1E (integration + testing)

Week 2: Stage 2 (Performance)
├─ Day 1-2: Phases 2A + 2B (parallel)
│   ├─ Track A: 2A (Tier 1 quick wins)
│   └─ Track B: 2B (Tier 2 medium)
├─ Day 3: Phase 2C (advanced)
└─ Day 4-5: Profiling + verification
```

---

# Projected Results

### After Stage 1

| Metric | Before | After |
|--------|--------|-------|
| CauldronManager.cs | 1297 lines | ~650 lines |
| CauldronWindowUI.cs | 755 lines | ~400 lines |
| New files | 0 | 11 |
| Testable units | Low | High |

### After Stage 2

| Metric | Current | With All Optimizations |
|--------|---------|------------------------|
| Rolls/sec (tasting) | 10 | 1000+ |
| GC pressure per 100 rolls | 400 KB | <5 KB |
| Frame time impact at 100/sec | ~12ms | <0.5ms |

---

# Risk Mitigation

### Editor Reference Preservation
- All new files use `[SerializeField]` only where needed
- Existing SerializeField references remain in original classes
- Presenters are optional (graceful degradation if null)

### Backwards Compatibility
- Public API of CauldronManager unchanged
- Events remain on CauldronManager (not moved to services)
- Save data format unchanged

### Testing Strategy
- Create unit tests for extracted services before integration
- Manual play testing after each phase
- Profile before/after Stage 2 changes

---

# Approval Checklist

- [ ] Stage 1 scope approved
- [ ] Stage 2 scope approved
- [ ] New file locations approved (`Assets/Scripts/Upgrades/Cauldron/`, `Assets/Scripts/UI/Cauldron/`)
- [ ] Parallel execution strategy approved
- [ ] Timeline expectations aligned
