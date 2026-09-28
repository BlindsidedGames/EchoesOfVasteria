using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Tasks;
using TimelessEchoes.Enemies;
using UnityEngine;
using static Blindsided.Oracle;
namespace TimelessEchoes.UI.Toolkit
{
    public sealed class ItemStatisticsPresentation
    {
        private readonly Dictionary<Resource, float> minimumDistance = new();
        private readonly List<Resource> defaultOrder;
        public ItemStatisticsPresentation()
        {
            defaultOrder = AssetCache.GetAll<Resource>("Resource Items").OrderBy(r => r.resourceID).ThenBy(r => r.name).ToList();
            foreach (var task in AssetCache.GetAll<TaskData>("Tasks"))
                if (task) foreach (var drop in task.resourceDrops) AddMinimum(drop.resource, task.GetEffectiveMinX());
            foreach (var enemy in AssetCache.GetAll<EnemyData>(""))
                if (enemy) foreach (var drop in enemy.resourceDrops) AddMinimum(drop.resource, enemy.minX);
        }
        private void AddMinimum(Resource resource, float distance)
        {
            if (!resource) return;
            minimumDistance[resource] = minimumDistance.TryGetValue(resource, out var current) ? Mathf.Min(current, distance) : distance;
        }
        public List<Resource> Ordered(ItemStatsPanelUI.SortMode mode, ResourceManager manager)
        {
            var known = defaultOrder.Where(r => r.totalReceived > 0).ToList();
            var unknown = defaultOrder.Where(r => r.totalReceived <= 0).ToList();
            int Tie(Resource a, Resource b) { var c = a.resourceID.CompareTo(b.resourceID); return c != 0 ? c : string.Compare(a.name, b.name, StringComparison.Ordinal); }
            known.Sort((a, b) =>
            {
                var result = mode switch
                {
                    ItemStatsPanelUI.SortMode.Tier => (manager ? manager.GetTier(b) : 1).CompareTo(manager ? manager.GetTier(a) : 1),
                    ItemStatsPanelUI.SortMode.Collected => b.totalReceived.CompareTo(a.totalReceived),
                    ItemStatsPanelUI.SortMode.Spent => b.totalSpent.CompareTo(a.totalSpent),
                    _ => 0
                };
                return result != 0 ? result : Tie(a, b);
            });
            if (mode == ItemStatsPanelUI.SortMode.Default || mode == ItemStatsPanelUI.SortMode.Unknown) unknown.Sort(Tie);
            if (mode == ItemStatsPanelUI.SortMode.Unknown) { unknown.AddRange(known); return unknown; }
            known.AddRange(unknown); return known;
        }
        public (string name, string totals, string detail, int tier, Sprite icon) Describe(Resource resource, ResourceManager manager, int tierCount)
        {
            var earned = resource.totalReceived > 0;
            var amount = manager ? manager.GetAmount(resource) : 0;
            var tier = earned && manager ? manager.GetTier(resource) : 1;
            var name = earned ? resource.name + (tier > 1 ? " | T" + tier : "") : "???";
            var totals = $"Count: {CalcUtils.FormatNumber(amount, true)}\nCollected: {CalcUtils.FormatNumber(resource.totalReceived, true)}\nSpent: {CalcUtils.FormatNumber(resource.totalSpent, true)}";
            double best = 0;
            if (oracle != null && oracle.saveData.Resources != null && oracle.saveData.Resources.TryGetValue(resource.name, out var record)) best = record.BestPerMinute;
            var power = resource.DisableAlterEcho ? "N/A" : CalcUtils.FormatNumber(best);
            var detail = $"Min Distance: ???\nAE Power: {power}";
            if (earned)
            {
                var distance = minimumDistance.TryGetValue(resource, out var min) ? min : 0;
                detail = $"Min Distance: {CalcUtils.FormatNumber(distance)}\nAE Power: {power}";
                if (resource.DisableAlterEcho) detail = "Crafted\n" + detail;
                else if (tier > 1) detail = $"Tier Bonus: {(manager ? manager.GetTierBonusPercent(tier) : 0):0.#}%\n" + detail;
            }
            if (resource.DisableAlterEcho && earned && tierCount > 0) tier = tierCount;
            return (name, totals, detail, tier, earned ? resource.icon : null);
        }
    }
}
