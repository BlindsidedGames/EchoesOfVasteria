using System;
using Blindsided.SaveData;

namespace TimelessEchoes.Farming
{
    /// <summary>Inventory and farm proposal form one immutable durable snapshot.</summary>
    public static class FarmTransaction
    {
        public static bool TryPrepare(GameData captured, FarmCommandResult proposal, out GameData candidate, out string error)
        {
            candidate = null; error = null;
            if (proposal == null || !proposal.Accepted || proposal.Candidate == null)
            { error = proposal?.Reason ?? "No farm proposal."; return false; }
            candidate = CurrentSaveCodec.Clone(captured);
            candidate.Farm = proposal.Candidate;
            foreach (var delta in proposal.ResourceDeltas)
            {
                candidate.Resources.TryGetValue(delta.Key, out var entry);
                entry ??= new GameData.ResourceEntry { Tier = 1 };
                if (double.IsNaN(delta.Value) || double.IsInfinity(delta.Value) ||
                    double.IsNaN(entry.Amount) || double.IsInfinity(entry.Amount) || entry.Amount < 0 ||
                    entry.Amount + delta.Value < 0 || double.IsInfinity(entry.Amount + delta.Value))
                { candidate = null; error = "Insufficient or invalid " + delta.Key + "."; return false; }
                entry.Amount += delta.Value;
                if (delta.Value > 0) entry.Earned = true;
                candidate.Resources[delta.Key] = entry;
                candidate.ResourceStats.TryGetValue(delta.Key, out var stats);
                stats ??= new GameData.ResourceRecord();
                if (double.IsNaN(stats.TotalReceived) || double.IsInfinity(stats.TotalReceived) || stats.TotalReceived < 0 ||
                    double.IsNaN(stats.TotalSpent) || double.IsInfinity(stats.TotalSpent) || stats.TotalSpent < 0)
                { candidate = null; error = "Invalid resource ledger."; return false; }
                if (delta.Value > 0) stats.TotalReceived += delta.Value;
                else stats.TotalSpent -= delta.Value;
                if (double.IsInfinity(stats.TotalReceived) || double.IsInfinity(stats.TotalSpent))
                { candidate = null; error = "Resource ledger overflow."; return false; }
                candidate.ResourceStats[delta.Key] = stats;
            }
            if (captured.Farm?.OriginalBedsPrepared != true && candidate.Farm.OriginalBedsPrepared)
                candidate.Quests["Farm.PrepareBeds"] = new GameData.QuestRecord { Completed = true, CompletedTimestamp = DateTime.UtcNow.Ticks };
            foreach (var id in proposal.CompletedQuests)
                candidate.Quests[id] = new GameData.QuestRecord { Completed = true, CompletedTimestamp = DateTime.UtcNow.Ticks };
            return true;
        }
    }
}
