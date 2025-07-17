using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.References.StatPanel;
using TimelessEchoes.Upgrades;
using UnityEngine;
using static TimelessEchoes.TELogger;

namespace TimelessEchoes.UI
{
    public class ItemStatsPanelUI : MonoBehaviour
    {
        [SerializeField] private StatPanelReferences references;
        private ResourceManager resourceManager;

        private readonly Dictionary<Resource, ItemEntryUIReferences> entries = new();
        private List<Resource> defaultOrder = new();

        public enum SortMode
        {
            Default,
            Collected,
            Spent
        }

        [SerializeField] private SortMode sortMode = SortMode.Default;

        private void Awake()
        {
            if (references == null)
                references = GetComponent<StatPanelReferences>();
            resourceManager = ResourceManager.Instance;
            if (resourceManager == null)
                TELogger.Log("ResourceManager missing", TELogCategory.Resource, this);
            BuildEntries();
        }

        private void OnEnable()
        {
            UpdateEntries();
        }

        private void Update()
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

            foreach (Transform child in references.itemEntryParent)
                Destroy(child.gameObject);

            var allResources = Resources.LoadAll<Resource>("Resources");
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

        private void UpdateEntries()
        {
            foreach (var pair in entries)
                UpdateEntry(pair.Key, pair.Value);
        }

        private void UpdateEntry(Resource res, ItemEntryUIReferences ui)
        {
            if (res == null || ui == null) return;
            double amount = resourceManager ? resourceManager.GetAmount(res) : 0;
            int collected = res.totalReceived;
            int spent = res.totalSpent;
            bool earned = collected > 0;

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
                string count = CalcUtils.FormatNumber(amount, true);
                string col = CalcUtils.FormatNumber(collected, true);
                string sp = CalcUtils.FormatNumber(spent, true);
                ui.entryHeldCollectedSpentText.text = $"Count: {count}\nCollected: {col}\nSpent: {sp}";
            }
        }

        private void SortEntries()
        {
            if (entries.Count == 0)
                return;

            if (sortMode == SortMode.Default)
            {
                ApplyOrder(defaultOrder);
                return;
            }

            IEnumerable<Resource> known = defaultOrder.Where(r => r.totalReceived > 0);
            IEnumerable<Resource> unknown = defaultOrder.Where(r => r.totalReceived <= 0);

            int GetValue(Resource r) => sortMode == SortMode.Collected ? r.totalReceived : r.totalSpent;

            var sortedKnown = known
                .OrderByDescending(GetValue)
                .ThenBy(r => int.TryParse(r.resourceID.ToString(), out var id) ? id : 0)
                .ThenBy(r => r.name)
                .ToList();

            var finalOrder = sortedKnown.Concat(unknown).ToList();
            ApplyOrder(finalOrder);
        }

        private void ApplyOrder(IList<Resource> order)
        {
            int index = 0;
            foreach (var res in order)
                if (entries.TryGetValue(res, out var ui))
                    ui.transform.SetSiblingIndex(index++);
        }
    }
}
