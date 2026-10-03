#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using TimelessEchoes.Farming;
using TimelessEchoes.Quests;
using UnityEditor;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Tests.EditMode
{
    public sealed class FieldsContentTests
    {
        private static T Asset<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.IsNotNull(asset, path + " must import as its actual asset type.");
            return asset;
        }
        private static void AssertEnglishReference(LocalizedString reference, SharedTableData shared, StringTable english)
        {
            Assert.IsNotNull(reference);
            Assert.AreEqual(shared.TableCollectionNameGuid, reference.TableReference.TableCollectionNameGuid);
            var key = reference.TableEntryReference.KeyId;
            Assert.Greater(key, 0);
            Assert.IsNotNull(shared.GetEntry(key), "Shared table must register key " + key);
            var entry = english.GetEntry(key);
            Assert.IsNotNull(entry, "English table must resolve key " + key);
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Value), "English value must exist for key " + key);
        }
        [Test] public void EveryFieldsQuestLocalizedReferenceResolvesThroughImportedSharedAndEnglishTables()
        {
            var shared = Asset<SharedTableData>("Assets/Localization/Tables/Quests/Quests Shared Data.asset");
            var english = Asset<StringTable>("Assets/Localization/Tables/Quests/Quests_en.asset");
            Assert.AreSame(shared, english.SharedData);
            var quests = AssetDatabase.FindAssets("t:QuestData", new[] { "Assets/Resources/Quests/Fields" })
                .Select(guid => Asset<QuestData>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();
            Assert.AreEqual(10, quests.Length);
            Assert.AreEqual(10, quests.Select(q => q.questId).Distinct().Count());
            foreach (var quest in quests)
            {
                AssertEnglishReference(quest.questName, shared, english);
                AssertEnglishReference(quest.description, shared, english);
                AssertEnglishReference(quest.rewardDescription, shared, english);
            }
        }
        [Test] public void FieldsDisplayNameHasOneWorkingTownTableBinding()
        {
            var content = FarmContent.Load(); Assert.IsNotNull(content);
            var shared = Asset<SharedTableData>("Assets/Localization/Tables/Town/TownUI Shared Data.asset");
            var english = Asset<StringTable>("Assets/Localization/Tables/Town/TownUI_en.asset");
            Assert.AreSame(shared, english.SharedData);
            var registered = shared.GetEntry("farm.display-name"); Assert.IsNotNull(registered);
            var translated = english.GetEntry(registered.Id); Assert.IsNotNull(translated);
            Assert.AreEqual("Fields", translated.Value);
            Assert.AreEqual("Fields", content.displayName);
            Assert.AreEqual("farm.display-name", content.displayNameKey);
            Assert.AreEqual("TownUI", content.localizedDisplayName.TableReference.TableCollectionName);
            Assert.AreEqual("farm.display-name", content.localizedDisplayName.TableEntryReference.Key);
        }
        [Test] public void ProductionRecipesResolveCanonicalTasksOutputsAndEveryGrowthSprite()
        {
            var content = FarmContent.Load(); Assert.IsNotNull(content);
            Assert.AreEqual(21, content.recipes.Count);
            Assert.AreEqual(21, content.recipes.Select(r => r.id).Distinct().Count());
            Assert.AreEqual(17, content.recipes.Count(r => !r.orchard));
            Assert.AreEqual(4, content.recipes.Count(r => r.orchard));
            foreach (var recipe in content.recipes)
            {
                Assert.IsNotNull(recipe.source, recipe.id); Assert.IsNotNull(recipe.source.associatedSkill, recipe.id);
                Assert.AreEqual("Farming", recipe.source.associatedSkill.name, recipe.id);
                Assert.IsNotNull(recipe.output, recipe.id);
                Assert.IsTrue(recipe.source.resourceDrops.Any(drop => drop != null && drop.resource == recipe.output), recipe.id + " must output its actual source resource.");
                Assert.AreEqual(4, recipe.stages.Length, recipe.id);
                Assert.IsTrue(recipe.stages.All(sprite => sprite != null), recipe.id + " needs four imported growth sprites.");
                Assert.IsNotNull(recipe.packIcon, recipe.id); Assert.IsNotNull(recipe.unknownIcon, recipe.id);
                if (recipe.orchard) Assert.IsNotNull(recipe.paidInput, recipe.id + " needs a matching sapling.");
                else { Assert.IsNull(recipe.paidInput); StringAssert.StartsWith("seed.", recipe.seedId); }
            }
            Assert.AreEqual(10, content.Recipe("recipe.apple.v1").source.requiredSkillLevel);
            Assert.AreEqual(27, content.Recipe("recipe.pear.v1").source.requiredSkillLevel);
            Assert.AreEqual(49, content.Recipe("recipe.peach.v1").source.requiredSkillLevel);
            Assert.AreEqual(72, content.Recipe("recipe.cherry.v1").source.requiredSkillLevel);
        }
        [Test] public void AuthoredConstructionChainMatchesApprovedLevelsCapacitiesAndAtomicMaterialCosts()
        {
            var content = FarmContent.Load(); Assert.IsNotNull(content); Assert.AreEqual(9, content.builds.Count);
            var levels = new[] { 1, 5, 35, 65, 95, 125, 150, 185, 225 };
            var materialNames = new[] { "Log,Stick", "Log,Stick", "Oak Log,Oak Stick,Stone", "Oak Log,Oak Stick", "Birch Log,Birch Stick,Stone", "Birch Log,Birch Stick", "Birch Log,Birch Stick,Stone", "Spruce Log,Spruce Stick", "Spruce Log,Spruce Stick,Stone" };
            var amounts = new[] { new[] {10,20}, new[] {40,80}, new[] {750,1500,100}, new[] {2500,5000}, new[] {4000,6000,1000}, new[] {8000,12000}, new[] {10000,15000,2500}, new[] {15000,20000}, new[] {25000,35000,5000} };
            var previous = FarmContent.IntroductionId;
            for (var i = 0; i < content.builds.Count; i++)
            {
                var build = content.builds[i];
                Assert.AreEqual(previous, build.previousQuestId); Assert.AreEqual(levels[i], build.twinsLevel);
                Assert.AreEqual(i < 6 ? i + 1 : 6, build.gardenCapacity);
                Assert.AreEqual(i < 6 ? 0 : (i - 5) * 2, build.orchardCapacity);
                CollectionAssert.AreEqual(materialNames[i].Split(','), build.costs.Select(cost => cost.resource.name).ToArray());
                CollectionAssert.AreEqual(amounts[i], build.costs.Select(cost => cost.amount).ToArray());
                Assert.IsTrue(build.sources.All(source => source != null && source.associatedSkill != null));
                var quest = Asset<QuestData>("Assets/Resources/Quests/Fields/" + build.questId + ".asset");
                Assert.AreEqual(build.questId, quest.questId); Assert.AreEqual("Farmers1", quest.npcId);
                Assert.AreEqual(1, quest.requiredQuests.Count); Assert.AreEqual(previous, quest.requiredQuests[0].questId);
                Assert.AreEqual(build.costs.Count, quest.requirements.Count);
                for (var j = 0; j < build.costs.Count; j++)
                {
                    Assert.AreEqual(QuestData.RequirementType.Resource, quest.requirements[j].type);
                    Assert.AreSame(build.costs[j].resource, quest.requirements[j].resource);
                    Assert.AreEqual(build.costs[j].amount, quest.requirements[j].amount);
                }
                Assert.IsEmpty(quest.rewards, "Construction must not grant generic inventory or hero XP rewards.");
                previous = build.questId;
            }
            var intro = Asset<QuestData>("Assets/Resources/Quests/Fields/" + FarmContent.IntroductionId + ".asset");
            Assert.IsEmpty(intro.requiredQuests); Assert.IsEmpty(intro.rewards);
            Assert.AreEqual(1, intro.requirements.Count); Assert.AreEqual(QuestData.RequirementType.Instant, intro.requirements[0].type);
            Assert.AreEqual("Farmers1", intro.npcId);
        }
    }
}
#endif
