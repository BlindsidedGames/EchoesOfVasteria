using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.References.StatPanel;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Tasks;
using TimelessEchoes.Enemies;
using UnityEngine;
using static Blindsided.Oracle;
using static TimelessEchoes.TELogger;
using TimelessEchoes.Utilities;

namespace TimelessEchoes.UI
{
    public class ItemStatsPanelUI : MonoBehaviour
    {
        [SerializeField] private StatPanelReferences references;
        private ResourceManager resourceManager;

        [SerializeField] private float updateInterval = 0.1f;
        private float nextUpdateTime;

        private readonly Dictionary<Resource, ItemEntryUIReferences> entries = new();
        private readonly Dictionary<Resource, float> minDistanceLookup = new();
        private List<Resource> defaultOrder = new();

        public enum SortMode
        {
            Default,
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

        private void OnEnable()
        {
            UpdateEntries();
            SortEntries();
            UITicker.Instance?.Subscribe(RefreshTick, updateInterval);
        }

        private void OnDisable()
        {
            UITicker.Instance?.Unsubscribe(RefreshTick);
        }

        private void RefreshTick()
        {
            UpdateEntries();
            SortEntries();
        }

        public void SetSortMode(SortMode mode)
        {
            sortMode = mode;
            SortEntries();
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

            SortEntries();
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
                ui.entryNameText.text = earned ? res.name : "???";

            if (ui.entryHeldCollectedSpentText != null)
            {
                var count = CalcUtils.FormatNumber(amount, true);
                var col = CalcUtils.FormatNumber(collected, true);
                var sp = CalcUtils.FormatNumber(spent, true);
                ui.entryHeldCollectedSpentText.text = $"Count: {count}\nCollected: {col}\nSpent: {sp}";
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
                    var minDist = minDistanceLookup.TryGetValue(res, out var d) ? d : 0f;
                    ui.bestPerMinuteText.text =
                        $"Min Distance: {CalcUtils.FormatNumber(minDist)}\nAE Power: {aePower}";
                }
                else
                {
                    ui.bestPerMinuteText.text =
                        $"Min Distance: ???\nAE Power: {aePower}";
                }
            }
        }

        private void SortEntries()
        {
            if (entries.Count == 0)
                return;

            var known = defaultOrder.Where(r => r.totalReceived > 0);
            var unknown = defaultOrder.Where(r => r.totalReceived <= 0);

            if (sortMode == SortMode.Default)
            {
                var sortedKnownDefault = known
                    .OrderBy(r => int.TryParse(r.resourceID.ToString(), out var id) ? id : 0)
                    .ThenBy(r => r.name)
                    .ToList();
                var sortedUnknownDefault = unknown
                    .OrderBy(r => int.TryParse(r.resourceID.ToString(), out var id) ? id : 0)
                    .ThenBy(r => r.name)
                    .ToList();
                var finalDefault = sortedKnownDefault.Concat(sortedUnknownDefault).ToList();
                ApplyOrder(finalDefault);
                return;
            }

            if (sortMode == SortMode.Unknown)
            {
                var sortedUnknownUnknown = unknown
                    .OrderBy(r => int.TryParse(r.resourceID.ToString(), out var id) ? id : 0)
                    .ThenBy(r => r.name)
                    .ToList();
                var sortedKnownUnknown = known
                    .OrderBy(r => int.TryParse(r.resourceID.ToString(), out var id) ? id : 0)
                    .ThenBy(r => r.name)
                    .ToList();
                var finalUnknown = sortedUnknownUnknown.Concat(sortedKnownUnknown).ToList();
                ApplyOrder(finalUnknown);
                return;
            }

            int GetValue(Resource r)
            {
                return sortMode == SortMode.Collected ? r.totalReceived : r.totalSpent;
            }

            var sortedKnownByValue = known
                .OrderByDescending(GetValue)
                .ThenBy(r => int.TryParse(r.resourceID.ToString(), out var id) ? id : 0)
                .ThenBy(r => r.name)
                .ToList();

            var finalOrder = sortedKnownByValue.Concat(unknown).ToList();
            ApplyOrder(finalOrder);
        }

        private void ApplyOrder(IList<Resource> order)
        {
            var index = 0;
            foreach (var res in order)
                if (entries.TryGetValue(res, out var ui))
                    ui.transform.SetSiblingIndex(index++);
        }
    }
}