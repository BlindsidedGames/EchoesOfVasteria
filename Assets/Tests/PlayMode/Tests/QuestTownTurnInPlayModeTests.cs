#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using TimelessEchoes.Farming;
using TimelessEchoes.Quests;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [Test]
        public void QuestTurnInsRequireTownIncludingFundedFieldsAndMissingLocationOwner()
        {
            var resourceObject = new GameObject("Town turn-in inventory");
            var questObject = new GameObject("Town turn-in quests");
            var locationObject = new GameObject("Town turn-in location");
            try
            {
                var resources = resourceObject.AddComponent<ResourceManager>(); resources.enabled = false;
                oracle.saveData.CompletedNpcTasks.Add("Farmers1");
                var quests = questObject.AddComponent<QuestManager>(); quests.enabled = false;
                Assert.True(quests.IsQuestInProgress(FarmContent.IntroductionId));
                Assert.False(quests.TryTurnInQuest(FarmContent.IntroductionId), "No location owner must not grant the seed or complete the quest.");
                Assert.False(service.BuildQuest(FarmContent.IntroductionId), "The direct Fields route must enforce the same location requirement.");
                Assert.False(FarmContent.Completed(oracle.saveData, FarmContent.IntroductionId));
                var game = locationObject.AddComponent<TimelessEchoes.GameManager>(); game.enabled = false;
                void SetTown(bool value) => typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(game, value);
                SetTown(false);
                foreach (var quest in Resources.LoadAll<QuestData>("Quests"))
                    Assert.False(quests.CanTurnInAtCurrentLocation(quest), quest.questId + " must wait for town.");
                Assert.False(quests.TryTurnInQuest(FarmContent.IntroductionId));
                SetTown(true);
                Assert.True(quests.TryTurnInQuest(FarmContent.IntroductionId));
                var seed = Resources.Load<Resource>("Resource Items/Radish Seed Pack");
                Assert.AreEqual(1, resources.GetAmount(seed));
                var build = Resources.Load<QuestData>("Quests/Fields/Farm.Garden.Build01.v1");
                foreach (var requirement in build.requirements)
                    resources.Add(requirement.resource, requirement.amount, trackStats: false, eligibleForTierRoll: false);
                quests.RefreshFieldsProgression();
                Assert.True(quests.IsQuestInProgress(build.questId));
                SetTown(false);
                Assert.False(quests.TryTurnInQuest(build.questId), "Fully funded construction must not bypass the town gate.");
                Assert.False(service.BuildQuest(build.questId));
                Assert.False(service.CapturePresentation().Builds.Single(b => b.QuestId == build.questId).CanTurnIn);
                Assert.AreEqual(0, service.State.GardenCapacity);
                foreach (var requirement in build.requirements)
                    Assert.AreEqual(requirement.amount, resources.GetAmount(requirement.resource), "Rejected turn-in must preserve its materials.");
                SetTown(true);
                Assert.True(service.CapturePresentation().Builds.Single(b => b.QuestId == build.questId).CanTurnIn);
                Assert.True(quests.TryTurnInQuest(build.questId));
                Assert.True(quests.IsQuestCompleted(build));
                Assert.AreEqual(1, service.State.GardenCapacity);
                foreach (var requirement in build.requirements) Assert.AreEqual(0, resources.GetAmount(requirement.resource));
                Assert.False(quests.TryTurnInQuest(build.questId), "The town reward is applied once.");
            }
            finally
            {
                Object.DestroyImmediate(questObject);
                Object.DestroyImmediate(resourceObject);
                Object.DestroyImmediate(locationObject);
            }
        }
    }
}
#endif
