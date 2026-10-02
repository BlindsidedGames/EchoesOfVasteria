using System;
using System.Collections.Generic;
using Blindsided.SaveData;

namespace TimelessEchoes.Upgrades
{
    /// <summary>Pure candidate preparation: food debit, stew and active quest progress share one immutable commit.</summary>
    public static class CauldronConversion
    {
        public static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        public static double UnitValue(Resource resource) => resource != null
            ? resource.baseValue * resource.valueMultiplier / 100d : 0;

        public static bool TryPrepare(GameData captured, string resourceName, double quantity,
            double unitValue, long sequence, IEnumerable<string> questIds,
            out GameData candidate, out bool replay, out string error)
        {
            candidate = null; replay = false; error = null;
            if (captured == null || string.IsNullOrEmpty(resourceName) || sequence <= 0 ||
                !IsFinite(quantity) || quantity <= 0 || !IsFinite(unitValue) || unitValue <= 0)
            { error = "Invalid food or amount."; return false; }
            var last = captured.LastCauldronConversion;
            if (sequence == captured.CauldronConversionSequence && last != null &&
                last.Sequence == sequence && last.ResourceName == resourceName &&
                last.Quantity == quantity && last.UnitValue == unitValue)
            { replay = true; return true; }
            if (captured.CauldronConversionSequence < 0 || captured.CauldronConversionSequence == long.MaxValue ||
                sequence != captured.CauldronConversionSequence + 1)
            { error = "This conversion is stale. Select the food again."; return false; }
            if (!captured.Resources.TryGetValue(resourceName, out var entry) || entry == null ||
                !entry.Earned || !IsFinite(entry.Amount) || quantity > entry.Amount)
            { error = "Not enough food."; return false; }
            var remaining = entry.Amount - quantity;
            var gained = quantity * unitValue;
            var stew = captured.CauldronStew + gained;
            if (remaining < 0 || remaining == entry.Amount || !IsFinite(gained) || gained <= 0 ||
                !IsFinite(captured.CauldronStew) || captured.CauldronStew < 0 || !IsFinite(stew) || stew <= captured.CauldronStew)
            { error = "This amount cannot be represented safely."; return false; }
            candidate = CurrentSaveCodec.Clone(captured);
            candidate.Resources[resourceName].Amount = remaining;
            candidate.ResourceStats.TryGetValue(resourceName, out var stats);
            stats ??= new GameData.ResourceRecord();
            stats.TotalSpent += quantity;
            if (!IsFinite(stats.TotalSpent)) { candidate = null; error = "Food statistics are out of range."; return false; }
            candidate.ResourceStats[resourceName] = stats;
            candidate.CauldronStew = stew;
            candidate.CauldronConversionSequence = sequence;
            candidate.LastCauldronConversion = new GameData.CauldronConversionReceipt
            { Sequence = sequence, ResourceName = resourceName, Quantity = quantity, UnitValue = unitValue };
            if (questIds != null)
                foreach (var id in new HashSet<string>(questIds))
                    if (candidate.Quests.TryGetValue(id, out var quest) && quest != null && !quest.Completed)
                    {
                        var progress = quest.CauldronMixProgress + quantity;
                        if (!IsFinite(quest.CauldronMixProgress) || quest.CauldronMixProgress < 0 || !IsFinite(progress))
                        { candidate = null; error = "Quest progress is out of range."; return false; }
                        quest.CauldronMixProgress = progress;
                    }
            return true;
        }
    }
}
