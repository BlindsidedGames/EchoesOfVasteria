using System.Linq;
using Blindsided;
using Blindsided.SaveData;
using TimelessEchoes.Quests;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Gear
{
    public static class ForgeFirstLesson
    {
        public const string QuestId = "Ivan's First Lesson";
        public static int Stage => Oracle.oracle?.saveData?.General.ForgeFirstLessonStage ?? 0;
        public static bool Accepted => Oracle.oracle?.saveData?.Quests != null &&
            Oracle.oracle.saveData.Quests.TryGetValue(QuestId, out var record) && (record.Accepted || record.Completed);
        public static bool Active => Accepted && Stage < 5;
        public static void Advance(int stage) => Oracle.oracle.saveData.General.ForgeFirstLessonStage = stage;

        public static GearItemRecord Record(GearItem item) => new GearItemRecord
        {
            slot = item.slot, rarity = item.rarity.name,
            affixes = item.affixes.Select(a => new GearAffixRecord { statId = a.stat.id ?? a.stat.name, value = a.value, quality = -1 }).ToList()
        };
        public static GearItem Restore(CoreSO core)
        {
            var record = Oracle.oracle?.saveData?.General.ForgeFirstLessonItem;
            if (record == null) return null;
            var stats = Resources.LoadAll<StatDefSO>("");
            var item = new GearItem { slot = record.slot, core = core, rarity = Resources.LoadAll<RaritySO>("").FirstOrDefault(r => r.name == record.rarity) };
            foreach (var affix in record.affixes)
            {
                var stat = stats.FirstOrDefault(s => (s.id ?? s.name) == affix.statId);
                if (stat) item.affixes.Add(new GearAffix { stat = stat, value = affix.value });
            }
            return item;
        }
    }
}
