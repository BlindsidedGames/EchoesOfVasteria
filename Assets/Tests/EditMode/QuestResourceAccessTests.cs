#if UNITY_INCLUDE_TESTS
using System.Linq;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Quests;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class QuestResourceAccessTests
    {
        [TestCase(-1, 0, true)]
        [TestCase(0, 0, false)]
        [TestCase(-1, 10, false)]
        public void FishRequirementUsesGatheringLevelUnlessAlreadyFunded(int offset, int owned, bool locked)
        {
            var task = Resources.Load<TaskData>("Tasks/Fishing/Bloopicus Maximus");
            Assert.NotNull(task);
            var resource = task.resourceDrops.First(drop => drop.resource).resource;
            var level = Mathf.Max(task.requiredSkillLevel, task.associatedSkill.GetResourceUnlockLevel(resource));
            var owner = new GameData();
            owner.SkillData[task.associatedSkill.name] = new GameData.SkillProgress { Level = level + offset };
            owner.Resources[resource.name] = new GameData.ResourceEntry { Amount = owned, Earned = owned > 0 };
            var requirement = new QuestData.Requirement { type = QuestData.RequirementType.Resource, resource = resource, amount = 10 };
            var gates = QuestResourceAccess.Blockers(requirement, owner);
            Assert.AreEqual(locked, gates.Count > 0);
            if (locked)
            {
                Assert.AreEqual(task.associatedSkill, gates.Single().skill);
                Assert.AreEqual(level, gates.Single().level);
            }
            var quest = ScriptableObject.CreateInstance<QuestData>();
            try
            {
                quest.questId = "Resource access check";
                quest.requirements.Add(requirement);
                Assert.AreEqual(locked, QuestResourceAccess.IsLocked(quest, owner));
                owner.Quests[quest.questId] = new GameData.QuestRecord { Completed = true };
                Assert.False(QuestResourceAccess.IsLocked(quest, owner), "Completed quests stay in history.");
            }
            finally { Object.DestroyImmediate(quest); }
        }

        [Test]
        public void UnlockedAlternativeSourceDoesNotLockTheResource()
        {
            var tasks = Resources.LoadAll<TaskData>("Tasks");
            var shared = tasks.SelectMany(task => task.resourceDrops.Where(drop => drop?.resource && drop.weight > 0)
                    .Select(drop => (task, drop)))
                .GroupBy(route => route.drop.resource)
                .First(group => group.Any(route => route.task.requiredSkillLevel <= 1 && route.drop.requiredSkillLevel <= 1 &&
                    route.task.associatedSkill && route.task.associatedSkill.GetResourceUnlockLevel(group.Key) <= 1) &&
                    group.Any(route => route.task.requiredSkillLevel > 1));
            var requirement = new QuestData.Requirement { type = QuestData.RequirementType.Resource, resource = shared.Key, amount = 10 };
            Assert.IsEmpty(QuestResourceAccess.Blockers(requirement, new GameData()));
        }
    }
}
#endif
