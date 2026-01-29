using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.References.StatPanel;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Tasks;
using TimelessEchoes.Enemies;
using TimelessEchoes.UI.Core;
using UnityEngine;
using static Blindsided.Oracle;
using static TimelessEchoes.TELogger;
using TimelessEchoes.Utilities;

namespace TimelessEchoes.UI
{
    /// <summary>
    /// Event-driven item stats panel. Subscribes to ResourceManager events
    /// instead of polling via UITicker.
    /// </summary>
    public class ItemStatsPanelUI : EventDrivenStatsPanelUI
    {
        [SerializeField] private StatPanelReferences references;
        private ResourceManager resourceManager;

        private readonly Dictionary<Resource, ItemEntryUIReferences> entries = new();
        private readonly Dictionary<Resource, (double amount, double totalReceived, double totalSpent)> lastDisplayed = new();
        private readonly System.Text.StringBuilder _sb = new System.Text.StringBuilder(128);
        private readonly Dictionary<Resource, float> minDistanceLookup = new();
        private List<Resource> defaultOrder = new();
        // Scratch lists for allocation-free sorting
        private readonly List<Resource> _scratchKnown = new();
        private readonly List<Resource> _scratchUnknown = new();
        private readonly List<Resource> _scratchFinal = new();

        public enum SortMode
        {
            Default,
            Tier,
            Collected,
            Spent,
            Unknown
        }

        [SerializeField] private SortMode sortMode = SortMode.Default;

        private void Awake()
        {
            if (references == null)
                references = GetComponent<StatPanelReferences>();
            resourceManager = ResourceManager.Instance;
            if (resourceManager == null)
                Log("ResourceManager missing", TELogCategory.Resource, this);
            BuildMinDistanceLookup();
            BuildEntries();
        }

        protected override bool IsPanelVisible()
        {
            if (references != null && references.itemEntryParent != null)
                return references.itemEntryParent.gameObject.activeInHierarchy;
            return gameObject.activeInHierarchy && isActiveAndEnabled;
        }

        protected override void SubscribeToEvents()
        {
            // Re-acquire manager in case it wasn't ready at Awake
            if (resourceManager == null)
                resourceManager = ResourceManager.Instance;
            
            if (resourceManager != null)
            {
                resourceManager.OnInventoryChanged += HandleInventoryChanged;
                resourceManager.OnResourceAdded += HandleResourceAdded;
                resourceManager.OnResourceTierUpgraded += HandleTierUpgraded;
            }
        }

        protected override void UnsubscribeFromEvents()
        {
            if (resourceManager != null)
            {
                resourceManager.OnInventoryChanged -= HandleInventoryChanged;
                resourceManager.OnResourceAdded -= HandleResourceAdded;
                resourceManager.OnResourceTierUpgraded -= HandleTierUpgraded;
            }
        }

        // Event handlers
        private void HandleInventoryChanged() => OnDataChanged();
        private void HandleResourceAdded(Resource _, double __, bool ___) => OnDataChanged();
        private void HandleTierUpgraded(Resource _, int __) => OnDataChanged();

        public void SetSortMode(SortMode mode)
        {
            sortMode = mode;
            if (IsPanelVisible())
                RefreshUI();
        }

        private void BuildEntries()
        {
            if (references == null || references.itemEntryParent == null || references.itemEntryPrefab == null)
                return;

            UIUtils.ClearChildren(references.itemEntryParent);

            var allResources = Blindsided.Utilities.AssetCache.GetAll<Resource>("Resource Items");
            var sorted = allResources
                .OrderBy(r => int.TryParse(r.resourceID.ToString(), out var id) ? id : 0)
                .ThenBy(r => r.name)
                .ToList();
            defaultOrder = sorted;
            entries.Clear();

            foreach (var res in sorted)
            {
                var obj = Instantiate(references.itemEntryPrefab.gameObject, references.itemEntryParent);
                var ui = obj.GetComponent<ItemEntryUIReferences>();
                if (ui == null) continue;
                entries[res] = ui;
            }
        }

        private void BuildMinDistanceLookup()
        {
            minDistanceLookup.Clear();

            var allTasks = Blindsided.Utilities.AssetCache.GetAll<TaskData>("Tasks");
            foreach (var task in allTasks)
            {
                if (task == null) continue;
                foreach (var drop in task.resourceDrops)
                {
                    if (drop.resource == null) continue;
                    var minDist = Mathf.Max(task.minX, drop.minX);
                    if (minDistanceLookup.TryGetValue(drop.resource, out var existing))
                        minDistanceLookup[drop.resource] = Mathf.Min(existing, minDist);
                    else
                        minDistanceLookup[drop.resource] = minDist;
                }
            }

            var allEnemies = Blindsided.Utilities.AssetCache.GetAll<EnemyData>("");
            foreach (var enemy in allEnemies)
            {
                if (enemy == null) continue;
                foreach (var drop in enemy.resourceDrops)
                {
                    if (drop.resource == null) continue;

                    // Enemy drops cannot occur before the enemy itself can spawn.
                    var spawnMin = enemy.minX;
                    var dropMin = drop.minX;
                    var minDist = Mathf.Max(spawnMin, dropMin);

                    if (minDistanceLookup.TryGetValue(drop.resource, out var existing))
                        minDistanceLookup[drop.resource] = Mathf.Min(existing, minDist);
                    else
                        minDistanceLookup[drop.resource] = minDist;
                }
            }
        }

        protected override void RefreshUI()
        {
            UpdateEntries();
            SortEntries();
            isDirty = false;
        }

        private void UpdateEntries()
        {
            foreach (var pair in entries)
                UpdateEntry(pair.Key, pair.Value);
        }

        private void UpdateEntry(Resource res, ItemEntryUIReferences ui)
        {
            if (res == null || ui == null) return;
            var amount = resourceManager ? resourceManager.GetAmount(res) : 0;
            var collected = res.totalReceived;
            var spent = res.totalSpent;
            var earned = collected > 0;

            if (lastDisplayed.TryGetValue(res, out var last))
            {
                if (Mathf.Approximately((float)last.amount, (float)amount) && last.totalReceived == collected && last.totalSpent == spent)
                {
                    // If earned state unchanged, skip.
                    if (earned == (last.totalReceived > 0)) return;
                }
            }
            lastDisplayed[res] = (amount, collected, spent);

            if (ui.entryIconImage != null)
            {
                ui.entryIconImage.sprite = earned ? res.icon : null;
                if (earned && res.icon != null)
                    ui.entryIconImage.SetNativeSize();
                ui.entryIconImage.enabled = earned && res.icon != null;
            }

            if (ui.entryIDText != null)
                ui.entryIDText.text = $"#{res.resourceID}";

            if (ui.entryNameText != null)
            {
                if (earned)
                {
                    var tier = resourceManager != null ? resourceManager.GetTier(res) : 1;
                    if (tier > 1)
                        ui.entryNameText.text = $"{res.name} | T{tier}";
                    else
                        ui.entryNameText.text = res.name;
                }
                else
                {
                    ui.entryNameText.text = "???";
                }
            }

            if (ui.entryHeldCollectedSpentText != null)
            {
                _sb.Clear();
                _sb.Append("Count: "); _sb.Append(CalcUtils.FormatNumber(amount, true)); _sb.Append('\n');
                _sb.Append("Collected: "); _sb.Append(CalcUtils.FormatNumber(collected, true)); _sb.Append('\n');
                _sb.Append("Spent: "); _sb.Append(CalcUtils.FormatNumber(spent, true));
                ui.entryHeldCollectedSpentText.SetText(_sb);
            }

            if (ui.bestPerMinuteText != null)
            {
                double best = 0;
                if (oracle != null &&
                    oracle.saveData.Resources != null &&
                    oracle.saveData.Resources.TryGetValue(res.name, out var record))
                    best = record.BestPerMinute;

                string aePower = res.DisableAlterEcho ? "N/A" : CalcUtils.FormatNumber(best);

                if (earned)
                {
                    var tier = resourceManager != null ? resourceManager.GetTier(res) : 1;
                    var bonusPercent = (resourceManager != null && !res.DisableAlterEcho) ? resourceManager.GetTierBonusPercent(tier) : 0f;
                    var minDist = minDistanceLookup.TryGetValue(res, out var d) ? d : 0f;
                    if (res.DisableAlterEcho)
                        ui.bestPerMinuteText.text =
                            $"Crafted\nMin Distance: {CalcUtils.FormatNumber(minDist)}\nAE Power: {aePower}";
                    else if (tier > 1)
                        ui.bestPerMinuteText.text =
                            $"Tier Bonus: {bonusPercent:0.#}%\nMin Distance: {CalcUtils.FormatNumber(minDist)}\nAE Power: {aePower}";
                    else
                        ui.bestPerMinuteText.text =
                            $"Min Distance: {CalcUtils.FormatNumber(minDist)}\nAE Power: {aePower}";
                }
                else
                {
                    ui.bestPerMinuteText.text =
                        $"Min Distance: ???\nAE Power: {aePower}";
                }
            }

            // Tier background image: always show Tier 1 background for unknown/unearthed per spec
            if (ui.tierBackgroundImage != null && references != null)
            {
                var tier = earned && resourceManager != null ? resourceManager.GetTier(res) : 1;
                // For crafted-only resources (DisableAlterEcho), display max tier background
                if (res.DisableAlterEcho && earned && references.tierBackgroundSprites != null && references.tierBackgroundSprites.Count > 0)
                    tier = references.tierBackgroundSprites.Count; // clamp below
                var sprites = references.tierBackgroundSprites;
                if (sprites != null)
                {
                    var idx = Mathf.Clamp(tier - 1, 0, sprites.Count - 1);
                    ui.tierBackgroundImage.sprite = (sprites != null && sprites.Count > 0 && idx < sprites.Count) ? sprites[idx] : null;
                    ui.tierBackgroundImage.enabled = ui.tierBackgroundImage.sprite != null;
                }
            }
        }

        private void SortEntries()
        {
            if (entries.Count == 0)
                return;

            // Split into known/unknown using scratch lists (allocation-free)
            _scratchKnown.Clear();
            _scratchUnknown.Clear();
            _scratchFinal.Clear();

            for (int i = 0; i < defaultOrder.Count; i++)
            {
                var r = defaultOrder[i];
                if (r.totalReceived > 0)
                    _scratchKnown.Add(r);
                else
                    _scratchUnknown.Add(r);
            }

            if (sortMode == SortMode.Default)
            {
                // Already sorted by ID/name in defaultOrder, just partition
                _scratchKnown.Sort(CompareByIdThenName);
                _scratchUnknown.Sort(CompareByIdThenName);
                _scratchFinal.AddRange(_scratchKnown);
                _scratchFinal.AddRange(_scratchUnknown);
                ApplyOrderScratch();
                return;
            }

            if (sortMode == SortMode.Tier)
            {
                _scratchKnown.Sort(CompareByTierDescThenIdName);
                _scratchFinal.AddRange(_scratchKnown);
                _scratchFinal.AddRange(_scratchUnknown);
                ApplyOrderScratch();
                return;
            }

            if (sortMode == SortMode.Unknown)
            {
                _scratchUnknown.Sort(CompareByIdThenName);
                _scratchKnown.Sort(CompareByIdThenName);
                _scratchFinal.AddRange(_scratchUnknown);
                _scratchFinal.AddRange(_scratchKnown);
                ApplyOrderScratch();
                return;
            }

            // Collected or Spent mode
            if (sortMode == SortMode.Collected)
                _scratchKnown.Sort(CompareByCollectedDescThenIdName);
            else
                _scratchKnown.Sort(CompareBySpentDescThenIdName);

            _scratchFinal.AddRange(_scratchKnown);
            _scratchFinal.AddRange(_scratchUnknown);
            ApplyOrderScratch();
        }

        private void ApplyOrderScratch()
        {
            var index = 0;
            for (int i = 0; i < _scratchFinal.Count; i++)
            {
                if (entries.TryGetValue(_scratchFinal[i], out var ui))
                    ui.transform.SetSiblingIndex(index++);
            }
        }

        private static int GetResourceId(Resource r)
        {
            return int.TryParse(r.resourceID.ToString(), out var id) ? id : 0;
        }

        private int CompareByIdThenName(Resource a, Resource b)
        {
            var idA = GetResourceId(a);
            var idB = GetResourceId(b);
            if (idA != idB) return idA.CompareTo(idB);
            return string.Compare(a.name, b.name, System.StringComparison.Ordinal);
        }

        private int CompareByTierDescThenIdName(Resource a, Resource b)
        {
            var tierA = resourceManager != null ? resourceManager.GetTier(a) : 1;
            var tierB = resourceManager != null ? resourceManager.GetTier(b) : 1;
            if (tierA != tierB) return tierB.CompareTo(tierA); // Descending
            return CompareByIdThenName(a, b);
        }

        private int CompareByCollectedDescThenIdName(Resource a, Resource b)
        {
            if (a.totalReceived != b.totalReceived) return b.totalReceived.CompareTo(a.totalReceived); // Descending
            return CompareByIdThenName(a, b);
        }

        private int CompareBySpentDescThenIdName(Resource a, Resource b)
        {
            if (a.totalSpent != b.totalSpent) return b.totalSpent.CompareTo(a.totalSpent); // Descending
            return CompareByIdThenName(a, b);
        }

    }
}
