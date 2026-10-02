using System;
using System.Collections.Generic;
using System.Linq;
using Blindsided.Utilities;
using TimelessEchoes.Upgrades;
using UnityEngine;
using static Blindsided.Oracle;
namespace TimelessEchoes.UI.Toolkit
{
    public sealed class ItemStatisticsPresentation
    {
        private readonly List<Resource> defaultOrder;
        public ItemStatisticsPresentation()
        {
            defaultOrder = AssetCache.GetAll<Resource>("Resource Items").OrderBy(r => r.resourceID).ThenBy(r => r.name).ToList();
        }
        // Keep the player's notation and precision; only remove insignificant decimal zeroes.
        internal static string Number(double value)
        {
            var formatted = CalcUtils.FormatNumber(value);
            var separator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            return System.Text.RegularExpressions.Regex.Replace(formatted,
                @"(\d+)" + System.Text.RegularExpressions.Regex.Escape(separator) + @"(\d+)",
                match => { var fraction = match.Groups[2].Value.TrimEnd('0'); return match.Groups[1].Value + (fraction.Length > 0 ? separator + fraction : ""); });
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
        public (string name, string totals, string detail, int tier, Sprite icon, string count) Describe(Resource resource, ResourceManager manager, int tierCount)
        {
            var earned = resource.totalReceived > 0;
            var amount = manager ? manager.GetAmount(resource) : 0;
            var tier = earned && manager ? manager.GetTier(resource) : 1;
            var name = earned ? resource.name : "???";
            var totals = $"Collected: {Number(resource.totalReceived)}\nSpent: {Number(resource.totalSpent)}";
            double best = 0;
            if (oracle != null && oracle.saveData.Resources != null && oracle.saveData.Resources.TryGetValue(resource.name, out var record)) best = record.BestPerMinute;
            var power = resource.DisableAlterEcho ? "N/A" : Number(best);
            var detail = $"Best gathered/min: {power}";
            if (earned)
            {
                if (resource.DisableAlterEcho) detail = "Crafted\n" + detail;
                else if (tier > 1) detail = $"Tier Bonus: {(manager ? manager.GetTierBonusPercent(tier) : 0):0.#}%\n" + detail;
            }
            if (resource.DisableAlterEcho && earned && tierCount > 0) tier = tierCount;
            return (name, totals, detail, tier, earned ? resource.icon : null, Number(amount));
        }
    }
}
