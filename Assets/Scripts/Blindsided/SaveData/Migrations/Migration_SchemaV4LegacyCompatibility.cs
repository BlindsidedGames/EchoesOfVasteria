using System;
using System.Collections.Generic;

namespace Blindsided.SaveData.Migrations
{
    internal sealed class Migration_SchemaV4LegacyCompatibility : IPostVersionSaveMigration
    {
        public int? TargetSchema => 4;
        public string TargetVersion => null;
        public string Id => "SchemaV4LegacyCompatibility";

        public void Apply(GameData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            data.Farm ??= new TimelessEchoes.Farming.FarmState();

            data.Quests ??= new Dictionary<string, GameData.QuestRecord>();
            if (data.Quests.TryGetValue("Mildred1", out var paid) && paid?.Completed == true)
            {
                if (!data.Quests.TryGetValue("BuffSlot2", out var canonical) || canonical == null)
                {
                    // Preserve both the historical receipt and its timestamp. The canonical key
                    // is the same paid entitlement, never another quest/reward execution.
                    canonical = new GameData.QuestRecord();
                    data.Quests["BuffSlot2"] = canonical;
                }

                if (!canonical.Completed)
                {
                    canonical.Completed = true;
                    canonical.CompletedTimestamp = paid.CompletedTimestamp;
                }
            }

            var receiptCapacity = 1;
            foreach (var id in new[] { "BuffSlot2", "BuffSlot3", "BuffSlot4", "BuffSlot5" })
                if (data.Quests.TryGetValue(id, out var receipt) && receipt?.Completed == true)
                    receiptCapacity++;

            // Keep unmatched earned capacity; mapped aliases cannot double-grant it.
            data.UnlockedBuffSlots = Math.Min(5, Math.Max(receiptCapacity, data.UnlockedBuffSlots));

            // Previous faulty repairs cannot be reversed from aggregate Infinity balances.
            // This only handles genuinely over-cap cards under a verified source profile.
            new Migration_CauldronOverflowRedistribution().Apply(data);

            // No stat-to-gear compensation. Compatibility guards remain available to the reader
            // until historical migrations finish; the current snapshot writer omits them.
            data.UpgradeLevels?.Clear();
        }
    }
}
