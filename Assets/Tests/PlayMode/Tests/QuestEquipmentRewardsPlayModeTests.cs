#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Gear;
using TimelessEchoes.Quests;
using UnityEngine;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [Test]
        public void GillHandInRequiresEquipmentOwnerAndBootsSurviveSaveReloadWithoutRepeatReward()
        {
            var quest = Resources.Load<QuestData>("Quests/Gill/The names Gill");
            var questsObject = new GameObject("Isolated Gill quest owner");
            var equipmentObject = new GameObject("Isolated Gill equipment owner");
            var locationObject = new GameObject("Isolated Gill return to town");
            try
            {
                foreach (var prerequisite in quest.requiredQuests)
                    oracle.saveData.Quests[prerequisite.questId] = new GameData.QuestRecord { Completed = true };
                var quests = questsObject.AddComponent<QuestManager>();
                quests.enabled = false;
                Assert.False(quests.IsQuestInProgress(quest.questId), "Gill must first be met in Farmlands.");
                oracle.saveData.CompletedNpcTasks.Add("Gill1");
                quests.OnNpcMet("Gill1");
                Assert.True(quests.IsQuestInProgress(quest.questId));
                Assert.False(quests.TryTurnInQuest(quest.questId), "A missing equipment owner must leave the reward claimable.");
                Assert.False(quests.IsQuestCompleted(quest));

                var equipment = equipmentObject.AddComponent<EquipmentController>();
                Blindsided.EventHandler.LoadData();
                var location = locationObject.AddComponent<TimelessEchoes.GameManager>(); location.enabled = false;
                typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(location, false);
                Assert.False(quests.TryTurnInQuest(quest.questId), "Gill promised to give the equipment in town.");
                Assert.False(quests.IsQuestCompleted(quest));
                typeof(TimelessEchoes.GameManager).GetProperty("IsInTown").SetValue(location, true);
                Assert.True(quests.TryTurnInQuest(quest.questId));
                Assert.False(quests.TryTurnInQuest(quest.questId), "A completed reward cannot be claimed twice.");
                Assert.True(oracle.SaveToSlot(oracle.CurrentSlot), "Save through the normal checkpoint before reloading.");
                var saved = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
                Assert.True(saved.Succeeded, saved.Diagnostic);
                Assert.True(saved.Data.Quests[quest.questId].Completed);
                Assert.True(saved.Data.EquipmentBySlot.ContainsKey("Boots"));
                oracle.saveData = saved.Data;
                Blindsided.EventHandler.LoadData();
                var boots = equipment.GetEquipped("Boots");
                Assert.NotNull(boots);
                Assert.AreEqual("Old Boots", boots.GetName());
                var reward = Resources.Load<QuestEquipmentReward>("Quest Rewards/Gill's Old Boots");
                var art = UnityEditor.AssetDatabase.LoadAssetAtPath<TimelessEchoes.UI.Toolkit.ToolkitForgeDefinition>("Assets/UI/Toolkit/Forge.asset");
                Assert.NotNull(reward.icon);
                Assert.AreSame(reward.icon, art.GearSprite(boots, "Boots"), "Existing saves must retain Gill's leather-boot artwork.");
                Assert.AreNotSame(reward.icon, art.GearSprite(null, "Boots"), "Empty slots retain their unknown icon.");

                Assert.AreEqual(10f, boots.GetStatValue(Resources.Load<StatDefSO>("Gear/StatDef/Move Speed")), .0001f);
                Assert.AreEqual(25f, boots.GetStatValue(Resources.Load<StatDefSO>("Gear/StatDef/Health")), .0001f);
                Assert.AreEqual(5f, boots.GetStatValue(Resources.Load<StatDefSO>("Gear/StatDef/Defense")), .0001f);
                Assert.AreEqual(1f, boots.GetStatValue(Resources.Load<StatDefSO>("Gear/StatDef/Regen")), .0001f);
            }
            finally
            {
                Object.DestroyImmediate(questsObject);
                Object.DestroyImmediate(equipmentObject);
                Object.DestroyImmediate(locationObject);
            }
        }
    }
}
#endif
