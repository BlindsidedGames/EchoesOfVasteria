using System;
using System.Collections.Generic;
using Blindsided.SaveData;
using TimelessEchoes.Quests;
using Blindsided.Utilities;
using UnityEngine;

namespace TimelessEchoes.Upgrades
{
    /// <summary>Food owed to unfinished quests stays out of the cauldron, including future quests.</summary>
    public static class CauldronFoodProtection
    {
        private static Dictionary<string, List<(string questId, int amount)>> requirements;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => requirements = null;

        public static double ReservedAmount(GameData owner, Resource food)
        {
            if (owner?.Quests == null || !food ||
                !owner.Quests.TryGetValue("Unlock Cauldron", out var unlock) || unlock?.Completed != true) return 0;
            if (requirements == null)
            {
                requirements = new();
                foreach (var quest in AssetCache.GetAll<QuestData>("Quests"))
                {
                    if (!quest || string.IsNullOrEmpty(quest.questId)) continue;
                    foreach (var requirement in quest.requirements)
                    {
                        if (requirement.type != QuestData.RequirementType.Resource || !requirement.resource || requirement.amount <= 0) continue;
                        var name = requirement.resource.name;
                        if (!requirements.TryGetValue(name, out var entries)) requirements[name] = entries = new();
                        entries.Add((quest.questId, requirement.amount));
                    }
                }
            }
            double reserved = 0;
            if (requirements.TryGetValue(food.name, out var costs))
                foreach (var cost in costs)
                    if (!owner.Quests.TryGetValue(cost.questId, out var record) || record?.Completed != true)
                        reserved += cost.amount;
            return reserved;
        }

        public static double AvailableAmount(GameData owner, Resource food, double owned) =>
            CauldronConversion.IsFinite(owned) ? Math.Max(0, owned - ReservedAmount(owner, food)) : 0;
    }
}
