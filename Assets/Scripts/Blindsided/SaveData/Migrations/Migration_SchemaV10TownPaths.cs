using System;
using System.Collections.Generic;
using TimelessEchoes;

namespace Blindsided.SaveData.Migrations
{
    internal sealed class Migration_SchemaV10TownPaths : ISaveMigration
    {
        public int? TargetSchema => 10;
        public string TargetVersion => null;
        public string Id => "SchemaV10TownPaths";

        public void Apply(GameData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.SchemaVersion >= 10) return;
            data.Quests ??= new Dictionary<string, GameData.QuestRecord>();
            var dirt = Completed("Chunky Business");
            var paving = Completed("Unlock Idle");
            if (dirt != null || paving != null) Grant(TownPathProgression.DirtQuest, dirt ?? paving);
            if (paving != null) Grant(TownPathProgression.PavingQuest, paving);

            GameData.QuestRecord Completed(string id) => data.Quests.TryGetValue(id, out var q) && q?.Completed == true ? q : null;
            void Grant(string id, GameData.QuestRecord original)
            {
                if (Completed(id) != null) return;
                data.Quests[id] = new GameData.QuestRecord { Completed = true, CompletedTimestamp = original.CompletedTimestamp };
            }
        }
    }
}
