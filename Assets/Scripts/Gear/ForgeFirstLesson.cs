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
        public static bool HasAdvancedSword
        {
            get
            {
                var data = Oracle.oracle?.saveData;
                if (data?.EquipmentBySlot == null || !data.EquipmentBySlot.TryGetValue("Weapon", out var weapon) || weapon == null)
                    return false;
                var rarity = Blindsided.Utilities.AssetCache.GetAll<RaritySO>("").FirstOrDefault(r => r && r.name == weapon.rarity);
                return rarity && rarity.tierIndex > 0;
            }
        }
        public static bool Active => Accepted && Stage < 5 && !HasAdvancedSword;

        public static bool ReconcileExistingWeapon()
        {
            var data = Oracle.oracle?.saveData;
            if (data == null || Stage >= 5 || !HasAdvancedSword) return false;
            data.General.ForgeFirstLessonStage = 5;
            data.General.ForgeFirstLessonItem = null;
            data.General.ForgeIntroductionSeen = true;
            data.Quests ??= new System.Collections.Generic.Dictionary<string, GameData.QuestRecord>();
            if (!data.Quests.TryGetValue(QuestId, out var record))
                data.Quests[QuestId] = record = new GameData.QuestRecord();
            record.Accepted = record.Completed = true;
            if (record.CompletedTimestamp == 0) record.CompletedTimestamp = System.DateTime.UtcNow.Ticks;
            data.PinnedQuests?.Remove(QuestId);
            return true;
        }
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
            var stats = Blindsided.Utilities.AssetCache.GetAll<StatDefSO>();
            var item = new GearItem { slot = record.slot, core = core, rarity = Blindsided.Utilities.AssetCache.GetAll<RaritySO>().FirstOrDefault(r => r.name == record.rarity) };
            foreach (var affix in record.affixes)
            {
                var stat = stats.FirstOrDefault(s => (s.id ?? s.name) == affix.statId);
                if (stat) item.affixes.Add(new GearAffix { stat = stat, value = affix.value });
            }
            return item;
        }
    }
}
