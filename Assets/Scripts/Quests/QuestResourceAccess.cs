using System.Collections.Generic;
using System.Linq;
using Blindsided.SaveData;
using Blindsided.Utilities;
using TimelessEchoes.Enemies;
using TimelessEchoes.Skills;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Quests
{
    public static class QuestResourceAccess
    {
        private readonly struct Source
        {
            public readonly Skill Skill;
            public readonly int Level;
            public readonly bool Combat, ResourceUnlock;
            public Source(Skill skill, int level, bool combat = false, bool resourceUnlock = true)
            { Skill = skill; Level = level; Combat = combat; ResourceUnlock = resourceUnlock; }
        }
        private static Dictionary<Resource, List<Source>> sources;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => sources = null;

        private static void BuildSources()
        {
            sources = new Dictionary<Resource, List<Source>>();
            void Add(Resource resource, Source source)
            {
                if (!resource) return;
                if (!sources.TryGetValue(resource, out var list)) sources[resource] = list = new List<Source>();
                list.Add(source);
            }
            foreach (var task in AssetCache.GetAll<TaskData>("Tasks"))
            {
                if (!task || !task.taskPrefab || task.weight <= 0) continue;
                foreach (var drop in task.resourceDrops)
                    if (drop?.resource && drop.weight > 0 && drop.dropRange.y > 0)
                        Add(drop.resource, new Source(task.associatedSkill, Mathf.Max(task.requiredSkillLevel, drop.requiredSkillLevel)));
                foreach (var drop in task.bonusDrops)
                {
                    if (drop?.resource && drop.range.y > 0 && drop.chanceMultiplier > 0 &&
                        (Farming.FarmContent.Load()?.BonusDropChance(drop.resource, drop.chance) ?? drop.chance) > 0)
                        Add(drop.resource, new Source(task.associatedSkill, task.requiredSkillLevel, resourceUnlock: false));
                }
            }
            foreach (var enemy in AssetCache.GetAll<EnemyData>(""))
            {
                if (!enemy || !enemy.prefab || enemy.weight <= 0) continue;
                foreach (var drop in enemy.resourceDrops)
                    if (drop?.resource && drop.weight > 0 && drop.dropRange.y > 0)
                        Add(drop.resource, new Source(null, drop.requiredSkillLevel, combat: true));
            }
        }

        // Different sources are alternatives. Only block when every known gathering
        // route needs a higher skill level, and the player still needs this item.
        public static IReadOnlyList<(Skill skill, int level)> Blockers(QuestData.Requirement requirement, GameData owner)
        {
            var result = new List<(Skill skill, int level)>();
            if (owner == null || requirement?.type != QuestData.RequirementType.Resource || !requirement.resource) return result;
            var resource = requirement.resource;
            var bank = ResourceManager.Instance;
            var owned = bank ? bank.GetAmount(resource, owner) : owner.Resources.TryGetValue(resource.name, out var saved) ? saved.Amount : 0;
            if (owned >= requirement.amount) return result;
            if (sources == null) BuildSources();
            if (!sources.TryGetValue(resource, out var routes)) return result;
            var levels = new Dictionary<Skill, int>();
            var controller = SkillController.Instance;
            foreach (var route in routes)
            {
                var skill = route.Combat ? controller?.CombatSkill : route.Skill;
                // An unclassified route must not invent a skill lock.
                if (!skill) return result;
                var level = Mathf.Max(route.Level, route.ResourceUnlock ? skill.GetResourceUnlockLevel(resource) : 0);
                var current = controller ? controller.GetLevel(skill, owner) : owner.SkillData.TryGetValue(skill.name, out var progress) ? Mathf.Max(1, progress.Level) : 1;
                if (current >= level) return result;
                if (!levels.TryGetValue(skill, out var previous) || level < previous) levels[skill] = level;
            }
            result.AddRange(levels.Select(pair => (pair.Key, pair.Value)).OrderBy(gate => gate.Value));
            return result;
        }

        public static bool IsLocked(QuestData quest, GameData owner)
        {
            if (!quest || owner == null || quest.requirements == null) return false;
            owner.Quests.TryGetValue(quest.questId, out var record);
            if (record?.Completed == true || quest.acceptanceQuest && record?.Accepted != true) return false;
            return quest.requirements.Any(requirement => Blockers(requirement, owner).Count > 0);
        }
    }
}
