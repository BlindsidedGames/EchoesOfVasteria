#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Blindsided;
using Blindsided.SaveData;
using Blindsided.Utilities;
using TimelessEchoes.Gear;
using TimelessEchoes.Upgrades;
using UnityEngine;

namespace TimelessEchoes.Tests.RealTime
{
    [UnityEngine.TestTools.PrebuildSetup(typeof(global::Tests.PlayMode.IsolatedPlayModeScene))]
    [UnityEngine.TestTools.PostBuildCleanup(typeof(global::Tests.PlayMode.IsolatedPlayModeScene))]
    public class RealTimeSystemsTests
    {
        [Test]
        public void PlaytimeAccumulatesWithProvidedUnscaledSeconds()
        {
            var originalScale = Time.timeScale;
            var go = new GameObject("Oracle_TestHarness");
            try
            {
                var oracle = go.AddComponent<Oracle>();
                Assert.IsNotNull(oracle.saveData, "Expected Oracle to create save data.");

                var accumulateMethod = typeof(Oracle).GetMethod(
                    "AccumulatePlayTime",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.IsNotNull(accumulateMethod, "AccumulatePlayTime method missing.");

                Time.timeScale = 5f;
                var startingPlaytime = oracle.saveData.PlayTime;

                accumulateMethod.Invoke(oracle, new object[] { 10f });

                Assert.AreEqual(startingPlaytime + 10f, oracle.saveData.PlayTime, 1e-5, "Playtime should advance based on provided unscaled seconds.");
            }
            finally
            {
                Time.timeScale = originalScale;
                Oracle.oracle = null;
                Object.DestroyImmediate(go);
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void GenuineTaskDropAddsBaseCardYieldWhileSecondaryAndQuestAwardsStayUnboosted(int seed)
        {
            var originalOracle = Oracle.oracle;
            var randomState = Random.state;
            var oracleGo = new GameObject("Yield contract oracle");
            var resourcesGo = new GameObject("Yield contract inventory"); resourcesGo.SetActive(false);
            var skillsGo = new GameObject("Yield contract skills"); skillsGo.SetActive(false);
            var taskGo = new GameObject("Yield contract task"); taskGo.SetActive(false);
            var combat = ScriptableObject.CreateInstance<TimelessEchoes.Skills.Skill>(); combat.name = "Combat"; combat.skillName = "Combat";
            var data = ScriptableObject.CreateInstance<TimelessEchoes.Tasks.TaskData>();
            try
            {
                Oracle.oracle = null;
                var owner = oracleGo.AddComponent<Oracle>();
                var radish = AssetCache.GetAll<Resource>("").Single(r => r.name == "Radish");
                owner.saveData.Resources["Radish"] = new GameData.ResourceEntry { Amount = 0, Earned = true, Tier = 1 };
                owner.saveData.CauldronCardCounts[radish.CardId] = 10000;
                owner.saveData.SkillData["Combat"] = new GameData.SkillProgress { Level = 100 };
                var skills = skillsGo.AddComponent<TimelessEchoes.Skills.SkillController>();
                typeof(TimelessEchoes.Skills.SkillController).GetField("combatSkill", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(skills, combat);
                typeof(TimelessEchoes.Skills.SkillController).GetField("skills", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(skills, new List<TimelessEchoes.Skills.Skill> { combat });
                skillsGo.SetActive(true);
                var resources = resourcesGo.AddComponent<ResourceManager>();
                typeof(ResourceManager).GetField("tierUpgradeDenominators", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(resources, new List<int> { 0, 0 });
                resourcesGo.SetActive(true);
                data.resourceDrops = new List<ResourceDrop>
                { new() { resource = radish, weight = 1, dropRange = new Vector2Int(100, 100) } };
                data.bonusDrops = new List<TimelessEchoes.Tasks.TaskData.BonusDrop>
                { new() { resource = radish, chance = 1, range = new Vector2Int(10, 10) } };
                var task = taskGo.AddComponent<TimelessEchoes.Tasks.FruitHarvestTask>(); task.taskData = data; task.associatedSkill = combat;
                skills.Aggregator.AddProcChance(combat, TimelessEchoes.Skills.MilestoneProcType.DoubleResources, .5f);
                Random.InitState(seed);
                typeof(TimelessEchoes.Tasks.ResourceGeneratingTask).GetMethod("GenerateDrops", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(task, new object[] { 0f });
                var gathered = resources.GetAmount(radish);
                // One shared proc outcome: (200 existing + 70 supplement) * 1 or 2, plus 10 fixed bonus.
                Assert.That(gathered, Is.EqualTo(280).Within(1e-9).Or.EqualTo(550).Within(1e-9));
                resources.Add(radish, 100, trackStats: false, eligibleForTierRoll: false);
                Assert.AreEqual(gathered + 100, resources.GetAmount(radish), 1e-9); // generic reward gains no card yield
                Blindsided.EventHandler.AwayForTime(3600);
                Assert.AreEqual(gathered + 100, resources.GetAmount(radish), 1e-9); // retired production has no resume subscriber
            }
            finally
            {
                Object.DestroyImmediate(taskGo); Object.DestroyImmediate(skillsGo); Object.DestroyImmediate(resourcesGo);
                Object.DestroyImmediate(oracleGo); Object.DestroyImmediate(combat); Object.DestroyImmediate(data);
                Oracle.oracle = originalOracle; Random.state = randomState;
            }
        }

        [Test]
        public void LiveResourceDiscoveryImmediatelyBlocksCategoryAndSurvivesOwnerReload()
        {
            var originalOracle = Oracle.oracle;
            var oracleGo = new GameObject("Discovery boundary oracle");
            var inventoryGo = new GameObject("Discovery boundary inventory"); inventoryGo.SetActive(false);
            var cauldronGo = new GameObject("Discovery boundary cauldron"); cauldronGo.SetActive(false);
            var taskGo = new GameObject("Discovery boundary task"); taskGo.SetActive(false);
            var taskData = ScriptableObject.CreateInstance<TimelessEchoes.Tasks.TaskData>();
            var definition = ScriptableObject.CreateInstance<TimelessEchoes.UI.Toolkit.ToolkitCauldronDefinition>();
            try
            {
                Oracle.oracle = null;
                var owner = oracleGo.AddComponent<Oracle>();
                var bank = owner.saveData;
                var radish = AssetCache.GetAll<Resource>("").Single(r => r.name == "Radish");
                var corn = AssetCache.GetAll<Resource>("").Single(r => r.name == "Corn");
                bank.Resources[radish.name] = new GameData.ResourceEntry { Earned = true, Tier = 1 };
                bank.Resources[corn.name] = new GameData.ResourceEntry { Earned = false, Tier = 3 };
                bank.CauldronCardCounts[radish.CardId] = 10000;
                var inventory = inventoryGo.AddComponent<ResourceManager>(); inventoryGo.SetActive(true);
                var cauldron = cauldronGo.AddComponent<CauldronManager>();
                typeof(CauldronManager).GetField("config", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(cauldron, CauldronResourceYield.Config);
                cauldronGo.SetActive(true);
                taskData.resourceDrops = new List<ResourceDrop> { new() { resource = radish, weight = 1, dropRange = new Vector2Int(100, 100) } };
                var task = taskGo.AddComponent<TimelessEchoes.Tasks.FruitHarvestTask>(); task.taskData = taskData;
                var drop = typeof(TimelessEchoes.Tasks.ResourceGeneratingTask).GetMethod("GenerateDrops", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.AreEqual(70, CauldronResourceYield.BonusPercent(bank, radish));
                inventory.Add(corn, 1, trackStats: false, eligibleForTierRoll: false);
                Assert.IsTrue(inventory.IsUnlocked(corn));
                Assert.IsFalse(bank.Resources[corn.name].Earned, "Discovery must be live before the normal save capture.");
                Assert.AreEqual(20, CauldronResourceYield.BonusPercent(bank, radish), "Unowned Corn must immediately block Farming category yield.");
                drop.Invoke(task, new object[] { 0f });
                Assert.AreEqual(120, inventory.GetAmount(radish), 1e-9);
                Assert.AreEqual(3, inventory.GetTier(corn), "Membership must not change resource rarity.");

                // Exercise the actual native collections presenter, including runtime membership and tooltip.
                definition.config = CauldronResourceYield.Config;
                var content = new UnityEngine.UIElements.VisualElement();
                var tooltip = new UnityEngine.UIElements.VisualElement();
                var tabParent = new UnityEngine.UIElements.VisualElement(); tabParent.Add(content);
                var presenterType = typeof(TimelessEchoes.UI.Toolkit.ToolkitCauldronScreen).Assembly.GetType("TimelessEchoes.UI.Toolkit.ToolkitCauldronCollections");
                var presenter = System.Activator.CreateInstance(presenterType, new object[] { definition, cauldron, inventory, content, tooltip, tabParent });
                presenterType.GetMethod("Rebuild").Invoke(presenter, null);
                presenterType.GetMethod("ShowTooltip").Invoke(presenter, new object[] { radish.CardId });
                var labels = UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.Label>(tooltip).ToList();
                Assert.That(labels.Single(l => l.name == "collection-tooltip-text").text, Does.Contain("Category Yield: +0% Farming"));

                var otherBank = new GameData();
                otherBank.Resources[radish.name] = new GameData.ResourceEntry { Earned = true };
                otherBank.CauldronCardCounts[radish.CardId] = 10000;
                Assert.AreEqual(70, CauldronResourceYield.BonusPercent(otherBank, radish), "An unrelated bank must not inherit live discoveries.");
                typeof(ResourceManager).GetMethod("SaveState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(inventory, null);
                Assert.IsTrue(bank.Resources[corn.name].Earned);
                var reloaded = CurrentSaveCodec.Clone(bank);
                owner.saveData = otherBank;
                Assert.AreEqual(70, CauldronResourceYield.BonusPercent(otherBank, radish), "Owner replacement before runtime load must use its own saved membership.");
                typeof(ResourceManager).GetMethod("LoadState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(inventory, null);
                Assert.IsFalse(inventory.IsUnlocked(corn));
                Assert.AreEqual(20, CauldronResourceYield.BonusPercent(reloaded, radish), "Detached save uses its own captured discoveries.");
                owner.saveData = reloaded;
                typeof(ResourceManager).GetMethod("LoadState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(inventory, null);
                Assert.AreEqual(20, CauldronResourceYield.BonusPercent(reloaded, radish));
                Assert.AreEqual(3, inventory.GetTier(corn));
                Assert.AreEqual(10000, reloaded.CauldronCardCounts[radish.CardId]);
                Assert.IsFalse(reloaded.CauldronCardCounts.ContainsKey(corn.CardId));
            }
            finally
            {
                Object.DestroyImmediate(taskGo); Object.DestroyImmediate(cauldronGo); Object.DestroyImmediate(inventoryGo);
                Object.DestroyImmediate(oracleGo); Object.DestroyImmediate(taskData); Object.DestroyImmediate(definition);
                Oracle.oracle = originalOracle;
            }
        }

        [Test]
        public void EquipmentRoundTripPreservesTemporarilyUnresolvedContent()
        {
            var oracleGo = new GameObject("Oracle_EquipmentTestHarness");
            var equipmentGo = new GameObject("EquipmentController_TestHarness");
            try
            {
                Oracle.oracle = null;
                typeof(EquipmentController).GetMethod(
                        "ResetStatics",
                        BindingFlags.Static | BindingFlags.NonPublic)
                    ?.Invoke(null, null);
                var oracle = oracleGo.AddComponent<Oracle>();
                var controller = equipmentGo.AddComponent<EquipmentController>();
                oracle.saveData.EquipmentBySlot["Helmet"] = new GearItemRecord
                {
                    slot = "Helmet",
                    rarity = "TemporarilyMissingRarity",
                    affixes = new List<GearAffixRecord>
                    {
                        new()
                        {
                            statId = "temporarily-missing-stat",
                            value = 42f,
                            quality = 0.75d
                        }
                    }
                };

                var load = typeof(EquipmentController).GetMethod(
                    "LoadState",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var save = typeof(EquipmentController).GetMethod(
                    "SaveState",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(load);
                Assert.IsNotNull(save);
                load.Invoke(controller, null);
                save.Invoke(controller, null);

                var preserved = oracle.saveData.EquipmentBySlot["Helmet"];
                Assert.AreEqual("TemporarilyMissingRarity", preserved.rarity);
                Assert.AreEqual(1, preserved.affixes.Count);
                Assert.AreEqual("temporarily-missing-stat", preserved.affixes[0].statId);
                Assert.AreEqual(42f, preserved.affixes[0].value, 0.0001f);
                Assert.AreEqual(0.75d, preserved.affixes[0].quality, 0.000001d);
            }
            finally
            {
                Oracle.oracle = null;
                Object.DestroyImmediate(equipmentGo);
                Object.DestroyImmediate(oracleGo);
                typeof(EquipmentController).GetMethod(
                        "ResetStatics",
                        BindingFlags.Static | BindingFlags.NonPublic)
                    ?.Invoke(null, null);
            }
        }

        [Test]
        public void EquipmentRoundTripCanonicalizesLegacyStatNameWithoutDuplication()
        {
            var oracleGo = new GameObject("Oracle_EquipmentAliasTestHarness");
            var equipmentGo = new GameObject("EquipmentController_AliasTestHarness");
            try
            {
                Oracle.oracle = null;
                typeof(EquipmentController).GetMethod(
                        "ResetStatics",
                        BindingFlags.Static | BindingFlags.NonPublic)
                    ?.Invoke(null, null);

                var stat = AssetCache.GetAll<StatDefSO>(string.Empty)
                    .FirstOrDefault(candidate => candidate != null &&
                                                 !string.IsNullOrWhiteSpace(candidate.id) &&
                                                 candidate.name != candidate.id);
                var rarity = AssetCache.GetAll<RaritySO>(string.Empty)
                    .FirstOrDefault(candidate => candidate != null);
                Assert.IsNotNull(stat, "Expected a stat asset with distinct legacy name and stable ID.");
                Assert.IsNotNull(rarity, "Expected at least one gear rarity asset.");

                var oracle = oracleGo.AddComponent<Oracle>();
                var controller = equipmentGo.AddComponent<EquipmentController>();
                oracle.saveData.EquipmentBySlot["Helmet"] = new GearItemRecord
                {
                    slot = "Helmet",
                    rarity = rarity.name,
                    affixes = new List<GearAffixRecord>
                    {
                        new()
                        {
                            statId = stat.name,
                            quality = 0.5d
                        }
                    }
                };

                var load = typeof(EquipmentController).GetMethod(
                    "LoadState",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var save = typeof(EquipmentController).GetMethod(
                    "SaveState",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(load);
                Assert.IsNotNull(save);

                load.Invoke(controller, null);
                save.Invoke(controller, null);

                var canonical = oracle.saveData.EquipmentBySlot["Helmet"];
                Assert.AreEqual(1, canonical.affixes.Count,
                    "A resolved legacy alias must not be retained beside its canonical stat ID.");
                Assert.AreEqual(stat.id, canonical.affixes[0].statId);

                load.Invoke(controller, null);
                var reloaded = controller.GetEquipped("Helmet");
                Assert.IsNotNull(reloaded);
                Assert.AreEqual(1, reloaded.affixes.Count(affix => affix?.stat == stat),
                    "The canonicalized round trip must apply the stat exactly once.");
            }
            finally
            {
                Oracle.oracle = null;
                Object.DestroyImmediate(equipmentGo);
                Object.DestroyImmediate(oracleGo);
                typeof(EquipmentController).GetMethod(
                        "ResetStatics",
                        BindingFlags.Static | BindingFlags.NonPublic)
                    ?.Invoke(null, null);
            }
        }
    }
}
#endif
