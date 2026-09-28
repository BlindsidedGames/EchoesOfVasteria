using System;

namespace Blindsided.SaveData.Migrations
{
    public sealed class Migration_GatheringBuffs : ISaveMigration
    {
        public int? TargetSchema => 3;
        public string TargetVersion => null;
        public string Id => "GatheringBuffs";

        public void Apply(GameData data)
        {
            if (data == null) return;
            // Older binary payloads can default the previously absent integer to zero.
            if (data.SchemaVersion < 3) data.ProspectorTaskId = -1;
            if (data.BuffSlots != null)
                for (int i = 0; i < data.BuffSlots.Count; i++)
                    if (data.BuffSlots[i] == "Slipstream") data.BuffSlots[i] = "Prospector";
            if (data.CauldronCardCounts != null && data.CauldronCardCounts.TryGetValue("BUFF:Slipstream", out int count))
            {
                data.CauldronCardCounts.TryGetValue("BUFF:Prospector", out int existing);
                data.CauldronCardCounts["BUFF:Prospector"] = checked(existing + count);
                data.CauldronCardCounts.Remove("BUFF:Slipstream");
            }
            if (data.Quests != null)
                foreach (var quest in data.Quests.Values)
                    if (quest?.BuffCastProgress != null && quest.BuffCastProgress.TryGetValue("Slipstream", out int casts))
                    {
                        quest.BuffCastProgress.TryGetValue("Prospector", out int existing);
                        quest.BuffCastProgress["Prospector"] = checked(existing + casts);
                        quest.BuffCastProgress.Remove("Slipstream");
                    }
            // Quest IDs stay stable: completed legacy milestones unlock their replacement rewards.
        }
    }
}
