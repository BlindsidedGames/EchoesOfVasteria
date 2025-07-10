using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Blindsided;
using Blindsided.SaveData;
using TimelessEchoes.Quests;

namespace TimelessEchoes.Tests
{
    public class QuestManagerTests
    {
        private GameObject oracleObj;
        private Oracle oracle;
        private GameObject managerObj;
        private QuestManager manager;
        private QuestData quest;

        [SetUp]
        public void SetUp()
        {
            oracleObj = new GameObject();
            oracle = oracleObj.AddComponent<Oracle>();
            managerObj = new GameObject();
            manager = managerObj.AddComponent<QuestManager>();
            quest = ScriptableObject.CreateInstance<QuestData>();
            quest.questId = "Q1";
            quest.npcId = "NPC1";
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(managerObj);
            Object.DestroyImmediate(oracleObj);
            Object.DestroyImmediate(quest);
            Oracle.oracle = null;
        }

        [Test]
        public void TryStartQuest_DoesNotActivateWithoutNpc()
        {
            var method = typeof(QuestManager).GetMethod("TryStartQuest", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(manager, new object[] { quest });
            var field = typeof(QuestManager).GetField("active", BindingFlags.NonPublic | BindingFlags.Instance);
            var dict = (System.Collections.IDictionary)field.GetValue(manager);
            Assert.IsFalse(dict.Contains("Q1"));
        }

        [Test]
        public void OnNpcMet_StartsQuest()
        {
            typeof(QuestManager).GetField("startingQuests", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(manager, new List<QuestData> { quest });

            StaticReferences.CompletedNpcTasks.Add("NPC1");
            manager.OnNpcMet("NPC1");

            var field = typeof(QuestManager).GetField("active", BindingFlags.NonPublic | BindingFlags.Instance);
            var dict = (System.Collections.IDictionary)field.GetValue(manager);
            Assert.IsTrue(dict.Contains("Q1"));
        }

        [Test]
        public void LoadState_StartsQuestWithCompletedPrerequisite()
        {
            var next = ScriptableObject.CreateInstance<QuestData>();
            next.questId = "Q2";
            next.requiredQuests.Add(quest);

            typeof(QuestManager).GetField("startingQuests", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(manager, new List<QuestData> { quest, next });

            oracle.saveData.Quests["Q1"] = new GameData.QuestRecord { Completed = true };

            var method = typeof(QuestManager).GetMethod("LoadState", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(manager, null);

            var field = typeof(QuestManager).GetField("active", BindingFlags.NonPublic | BindingFlags.Instance);
            var dict = (System.Collections.IDictionary)field.GetValue(manager);
            Assert.IsTrue(dict.Contains("Q2"));

            Object.DestroyImmediate(next);
        }
    }
}

