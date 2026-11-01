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
        [TestCase(false)]
        [TestCase(true)]
        public void MinesTimberHandInPersistsAcceptanceAndPreservesExistingAccess(bool alreadyUnlocked)
        {
            var owner = new GameObject("Mines timber services");
            try
            {
                var mine = Resources.LoadAll<QuestData>("Quests").Single(q => q.questId == "Rock and Stone");
                oracle.saveData.CompletedNpcTasks.Add(mine.npcId);
                foreach (var prerequisite in mine.requiredQuests)
                    oracle.saveData.Quests[prerequisite.questId] = new Blindsided.SaveData.GameData.QuestRecord { Completed = true };
                if (alreadyUnlocked)
                    oracle.saveData.Quests[mine.questId] = new Blindsided.SaveData.GameData.QuestRecord { Completed = true };
                var game = owner.AddComponent<TimelessEchoes.GameManager>(); game.enabled = false;
                typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(game, true);
                var inventory = owner.AddComponent<ResourceManager>();
                var quests = owner.AddComponent<QuestManager>();
                var logs = Resources.Load<Resource>("Resource Items/Log");
                inventory.Add(logs, 100, trackStats: false, eligibleForTierRoll: false);
                if (!alreadyUnlocked)
                {
                    Assert.True(quests.NeedsAcceptance(mine));
                    Assert.True(quests.TryTurnInQuest(mine.questId));
                    Assert.False(quests.NeedsAcceptance(mine));
                    Assert.False(FarmContent.Completed(oracle.saveData, mine.questId), "The opening must not unlock the Mines.");
                    Assert.AreEqual(100, inventory.GetAmount(logs), "The opening must not spend the hand-in.");
                    Assert.True((bool)typeof(Blindsided.Oracle).GetMethod("SaveNowBlocking", Private)
                        .Invoke(oracle, new object[] { oracle.CurrentSlot, false }));
                    var accepted = Blindsided.SaveData.SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
                    Assert.True(accepted.Succeeded, accepted.Diagnostic);
                    Assert.True(accepted.Data.Quests[mine.questId].Accepted, "Reloading must not repeat the opening.");
                    Assert.False(accepted.Data.Quests[mine.questId].Completed);
                    Assert.True(quests.TryTurnInQuest(mine.questId));
                }
                Assert.True(FarmContent.Completed(oracle.saveData, mine.questId));
                Assert.False(quests.NeedsAcceptance(mine));
                Assert.False(quests.TryTurnInQuest(mine.questId), "Existing or newly earned access must not charge again.");
                Assert.AreEqual(alreadyUnlocked ? 100 : 0, inventory.GetAmount(logs));
                Assert.True((bool)typeof(Blindsided.Oracle).GetMethod("SaveNowBlocking", Private)
                    .Invoke(oracle, new object[] { oracle.CurrentSlot, false }));
                var completed = Blindsided.SaveData.SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
                Assert.True(completed.Succeeded, completed.Diagnostic);
                Assert.True(FarmContent.Completed(completed.Data, mine.questId));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void PantryStartsColdRonAtItsOwnBaselineAndLegacyProgressSurvivesTheNewPrerequisite()
        {
            var owner = new GameObject("Cauldron progression services");
            try
            {
                oracle.saveData.Quests["ForTheJam"] = new Blindsided.SaveData.GameData.QuestRecord { Completed = true };
                oracle.saveData.Quests["Tracking Twins"] = new Blindsided.SaveData.GameData.QuestRecord { Completed = true };
                oracle.saveData.General.TotalResourcesGathered = 1000;
                var game = owner.AddComponent<TimelessEchoes.GameManager>(); game.enabled = false;
                typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(game, true);
                var inventory = owner.AddComponent<ResourceManager>();
                var stats = owner.AddComponent<TimelessEchoes.Stats.GameplayStatTracker>(); stats.enabled = false;
                var quests = owner.AddComponent<QuestManager>();
                Assert.True(quests.IsQuestInProgress("Unlock Corn"));
                Assert.False(quests.IsQuestInProgress("Unlock Cauldron"));
                var radish = Resources.Load<Resource>("Resource Items/Radish");
                inventory.Add(radish, 500, trackStats: false, eligibleForTierRoll: false);
                Assert.True(quests.TryTurnInQuest("Unlock Corn"));
                Assert.AreEqual(0, inventory.GetAmount(radish));
                Assert.True(quests.IsQuestInProgress("Unlock Cauldron"));
                Assert.AreEqual(1000, oracle.saveData.Quests["Unlock Cauldron"].ResourcesBaseline);
                stats.AddResources(20999);
                Assert.False(quests.TryTurnInQuest("Unlock Cauldron"));
                stats.AddResources(1);
                Assert.True(quests.TryTurnInQuest("Unlock Cauldron"), "21,000 gathered since the pantry hand-in is sufficient.");

                // An older save can have Cold Ron in progress without the new pantry prerequisite.
                oracle.saveData.Quests.Remove("Unlock Corn");
                oracle.saveData.Quests["Unlock Cauldron"] = new Blindsided.SaveData.GameData.QuestRecord { ResourcesBaselineSet = true, ResourcesBaseline = 250 };
                Object.DestroyImmediate(quests);
                quests = owner.AddComponent<QuestManager>();
                Assert.True(quests.IsQuestInProgress("Unlock Cauldron"));
                Assert.AreEqual(250, oracle.saveData.Quests["Unlock Cauldron"].ResourcesBaseline);
                Assert.True(quests.TryTurnInQuest("Unlock Cauldron"));
            }
            finally { Object.DestroyImmediate(owner); }
        }

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
                var build = content.Build("Farm.Garden.Build01.v1");
                foreach (var cost in build.costs)
                    resources.Add(cost.resource, cost.amount, trackStats: false, eligibleForTierRoll: false);
                quests.RefreshFieldsProgression();
                Assert.NotNull(quests.GetQuestData(build.questId), "Construction must be available as a quest.");
                Assert.True(quests.IsQuestInProgress(build.questId));
                SetTown(false);
                Assert.False(quests.TryTurnInQuest(build.questId), "Fully funded construction must not bypass the town gate.");
                Assert.False(service.BuildQuest(build.questId));
                Assert.False(service.CapturePresentation().Builds.Single(b => b.QuestId == build.questId).CanTurnIn);
                Assert.AreEqual(0, service.State.GardenCapacity);
                foreach (var cost in build.costs)
                    Assert.AreEqual(cost.amount, resources.GetAmount(cost.resource), "Rejected construction must preserve its materials.");
                SetTown(true);
                Assert.True(service.CapturePresentation().Builds.Single(b => b.QuestId == build.questId).CanTurnIn);
                Assert.True(quests.TryTurnInQuest(build.questId));
                Assert.True(FarmContent.Completed(oracle.saveData, build.questId));
                Assert.AreEqual(1, service.State.GardenCapacity);
                foreach (var cost in build.costs) Assert.AreEqual(0, resources.GetAmount(cost.resource));
                Assert.False(quests.TryTurnInQuest(build.questId), "Construction can only be purchased once.");
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
