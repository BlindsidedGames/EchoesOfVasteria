#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Blindsided;
using Blindsided.SaveData;
using NUnit.Framework;
using Sirenix.Serialization;
using TimelessEchoes.Tasks;
using TimelessEchoes.UI.Toolkit;
using TimelessEchoes.Upgrades;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Resource = TimelessEchoes.Upgrades.Resource;
using SerializationUtility = Sirenix.Serialization.SerializationUtility;

namespace TimelessEchoes.Tests
{
    public class TimberResourceTests
    {
        private static Resource Item(string name) => Resources.Load<Resource>("Resource Items/" + name);

        [TestCase("Medium Tree", "", 1, 3, 1, 2)]
        [TestCase("Large Tree", "", 2, 8, 1, 5)]
        [TestCase("Medium Oak Tree", "Oak ", 1, 5, 1, 3)]
        [TestCase("Large Oak Tree", "Oak ", 3, 9, 3, 8)]
        [TestCase("Medium Birch Tree", "Birch ", 1, 6, 2, 4)]
        [TestCase("Large Birch Tree", "Birch ", 4, 10, 5, 12)]
        [TestCase("Medium Spruce Tree", "Spruce ", 1, 7, 4, 8)]
        [TestCase("Large Spruce Tree", "Spruce ", 4, 15, 7, 22)]
        public void TreeRewardsUseTheirOwnPairWithOriginalRangesAndChance(
            string taskName, string prefix, int stickMin, int stickMax, int logMin, int logMax)
        {
            var task = Resources.Load<TaskData>("Tasks/Woodcutting/" + taskName);
            Assert.That(task, Is.Not.Null);
            Assert.That(task.resourceDrops.Count, Is.EqualTo(2));
            Assert.That(task.resourceDrops[0].resource, Is.SameAs(Item(prefix + "Stick")));
            Assert.That(task.resourceDrops[1].resource, Is.SameAs(Item(prefix + "Log")));
            Assert.That(task.resourceDrops[0].dropRange, Is.EqualTo(new Vector2Int(stickMin, stickMax)));
            Assert.That(task.resourceDrops[1].dropRange, Is.EqualTo(new Vector2Int(logMin, logMax)));
            Assert.That(task.resourceDrops.All(drop => drop.weight == 1 && drop.requiredSkillLevel == 0));
            CollectionAssert.AreEqual(new[] { .3f, .2f }, task.additionalLootChances);
            var both = DropResolver.RollDrops(task.resourceDrops, task.additionalLootChances,
                task.associatedSkill, true, () => 0f).ToArray();
            Assert.That(both.Length, Is.EqualTo(2));
            Assert.That(both[0].count, Is.EqualTo(stickMin));
            Assert.That(both[1].count, Is.EqualTo(logMin));
            var one = DropResolver.RollDrops(task.resourceDrops, task.additionalLootChances,
                task.associatedSkill, true, () => .99f).ToArray();
            Assert.That(one.Length, Is.EqualTo(1));
            Assert.That(one[0].resource, Is.SameAs(Item(prefix + "Log")));
            Assert.That(one[0].count, Is.EqualTo(logMax));
        }

        [TestCase("Stick", 2, "902_Brown_Branch_Forked", 236)]
        [TestCase("Log", 57, "905_Brown_Log_Plain", 238)]
        [TestCase("Oak Stick", 71, "932_GreyBark_Branch_Forked", 240)]
        [TestCase("Oak Log", 72, "935_GreyBark_Log_Plain", 242)]
        [TestCase("Birch Stick", 73, "922_Pale_Branch_Forked", 244)]
        [TestCase("Birch Log", 74, "925_Pale_Log_Plain", 246)]
        [TestCase("Spruce Stick", 75, "912_DarkBrown_Branch_Forked", 248)]
        [TestCase("Spruce Log", 76, "915_DarkBrown_Log_Plain", 250)]
        public void ApprovedArtHasDistinctStableIdentityInventoryAndDropReferences(
            string name, int id, string spriteSuffix, int index)
        {
            var resource = Item(name);
            Assert.That(resource, Is.Not.Null);
            Assert.That(resource.resourceID, Is.EqualTo(id));
            Assert.That(resource.icon.name, Is.EqualTo("Resources_16x16_Icon_" + spriteSuffix));
            Assert.That(resource.icon.rect.size, Is.EqualTo(new Vector2(16, 16)));
            Assert.That(resource.icon.pixelsPerUnit, Is.EqualTo(16));
            Assert.That(resource.UnknownIcon, Is.Not.Null);
            Assert.That(resource.DisableAlterEcho, Is.False);
            Assert.That(resource.baseValue, Is.EqualTo(1));
            Assert.That(resource.valueMultiplier, Is.EqualTo(name.EndsWith("Log") ? 1.2d : 1d).Within(.00001));
            Assert.That(ResourceIconLookup.TryGetIconIndex(id, out var actual), Is.True);
            Assert.That(actual, Is.EqualTo(index));
            Assert.That(ResourceIconLookup.TryGetUnknownIconIndex(id, out var unknown), Is.True);
            Assert.That(unknown, Is.EqualTo(index + 1));
            var dropAsset = ResourceIconLookup.SpriteAsset;
            Assert.That(dropAsset.spriteCharacterTable[index].glyphIndex, Is.EqualTo(index));
            Assert.That(dropAsset.spriteCharacterTable[index + 1].name, Does.EndWith("_Unknown"));
            var inventory = AssetDatabase.LoadAssetAtPath<ToolkitResourceInventoryDefinition>(
                "Assets/UI/Toolkit/ResourceInventory.asset");
            Assert.That(inventory.resources.Count(item => item == resource), Is.EqualTo(1));
            Assert.That(Resources.LoadAll<Resource>("").Count(item => item.resourceID == id), Is.EqualTo(1));
        }

        [Test]
        public void BaseGuidAndSaveKeysSurviveWithoutSpeciesConversion()
        {
            Assert.That(AssetDatabase.AssetPathToGUID("Assets/Resources/Resource Items/Stick.asset"),
                Is.EqualTo("024c1162874b64e43be92b8892b40efb"));
            Assert.That(AssetDatabase.AssetPathToGUID("Assets/Resources/Resource Items/Log.asset"),
                Is.EqualTo("e8ace32c9cea7aa4aa202557d1f972ea"));
            var legacy = new GameData();
            legacy.Resources["Log"] = new GameData.ResourceEntry { Amount = 123456789.125d, Earned = true, Tier = 4 };
            legacy.Resources["Stick"] = new GameData.ResourceEntry { Amount = 937.75d, Earned = true, Tier = 2 };
            legacy.ResourceStats["Log"] = new GameData.ResourceRecord { TotalReceived = 199999999.375d, TotalSpent = 1234.5d };
            legacy.Disciples["Log"] = new GameData.DiscipleGenerationRecord();
            legacy.Quests["Fence2"] = new GameData.QuestRecord { Completed = true, CompletedTimestamp = 123456 };
            var loaded = SerializationUtility.DeserializeValue<GameData>(CurrentSaveCodec.Serialize(legacy), DataFormat.Binary);
            Assert.That(loaded.Resources.Keys, Is.EquivalentTo(new[] { "Log", "Stick" }));
            Assert.That(loaded.Resources["Log"].Amount, Is.EqualTo(legacy.Resources["Log"].Amount));
            Assert.That(loaded.Resources["Stick"].Amount, Is.EqualTo(legacy.Resources["Stick"].Amount));
            Assert.That(loaded.Resources["Log"].Tier, Is.EqualTo(4));
            Assert.That(loaded.ResourceStats["Log"].TotalReceived, Is.EqualTo(199999999.375d));
            Assert.That(loaded.Disciples.ContainsKey("Log"));
            Assert.That(loaded.Quests["Fence2"].Completed);
        }

        [Test]
        public void RuntimeResourceCaptureKeepsOldAndNewBalancesAndUndiscoveredDefaultsSeparate()
        {
            var previousOracle = Oracle.oracle;
            var owner = new GameObject("Disposable timber save fixture"); owner.SetActive(false);
            var managerObject = new GameObject("Disposable timber inventory"); managerObject.SetActive(false);
            var all = Resources.LoadAll<Resource>("");
            var stats = all.Select(resource => (resource, resource.totalReceived, resource.totalSpent)).ToArray();
            try
            {
                var oracle = owner.AddComponent<Oracle>(); Oracle.oracle = oracle;
                oracle.saveData = new GameData();
                oracle.saveData.Resources["Log"] = new GameData.ResourceEntry { Amount = 100.125d, Earned = true, Tier = 4 };
                oracle.saveData.Resources["Stick"] = new GameData.ResourceEntry { Amount = 77.75d, Earned = true, Tier = 2 };
                oracle.saveData.Resources["Oak Log"] = new GameData.ResourceEntry { Amount = 8.5d, Earned = true, Tier = 3 };
                oracle.saveData.Resources["Retired resource"] = new GameData.ResourceEntry { Amount = 91d, Earned = true };
                var manager = managerObject.AddComponent<ResourceManager>();
                typeof(ResourceManager).GetMethod("LoadState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
                Assert.That(manager.GetAmount(Item("Log")), Is.EqualTo(100.125d));
                Assert.That(manager.GetAmount(Item("Oak Log")), Is.EqualTo(8.5d));
                Assert.That(manager.GetAmount(Item("Birch Log")), Is.Zero);
                Assert.That(manager.IsUnlocked(Item("Birch Log")), Is.False);
                Assert.That(manager.GetTier(Item("Log")), Is.EqualTo(4));
                Assert.That(manager.GetTier(Item("Oak Log")), Is.EqualTo(3));
                typeof(ResourceManager).GetMethod("SaveState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
                var reloaded = SerializationUtility.DeserializeValue<GameData>(CurrentSaveCodec.Serialize(oracle.saveData), DataFormat.Binary);
                Assert.That(reloaded.Resources["Log"].Amount, Is.EqualTo(100.125d));
                Assert.That(reloaded.Resources["Stick"].Amount, Is.EqualTo(77.75d));
                Assert.That(reloaded.Resources["Oak Log"].Amount, Is.EqualTo(8.5d));
                Assert.That(reloaded.Resources.ContainsKey("Birch Log"), Is.False);
                Assert.That(reloaded.Resources["Retired resource"].Amount, Is.EqualTo(91d));
            }
            finally
            {
                Object.DestroyImmediate(managerObject); Object.DestroyImmediate(owner); Oracle.oracle = previousOracle;
                foreach (var saved in stats) { saved.resource.totalReceived = saved.totalReceived; saved.resource.totalSpent = saved.totalSpent; }
            }
        }
    }
}
#endif
