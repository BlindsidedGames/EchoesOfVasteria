#if UNITY_INCLUDE_TESTS
using System.Linq;
using NUnit.Framework;
using TimelessEchoes.Tasks;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Skills;
using TimelessEchoes.MapGeneration;
using TimelessEchoes.UI.Cauldron;
using TimelessEchoes.UI.Toolkit;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class OrchardContentTests
    {
        [TestCase("Apple",77,54,10,.01)] [TestCase("Pear",79,55,27,.012)]
        [TestCase("Peach",81,56,49,.016)] [TestCase("Cherry",83,57,72,.025)]
        public void HarvestHasFoodSaplingSkillWorldArtAndStableIconReferences(string species,int id,int taskId,int level,double value)
        {
            var fruit=Resources.Load<Resource>("Resource Items/"+species);
            var seed=Resources.Load<Resource>("Resource Items/"+species+" Sapling");
            var task=Resources.Load<TaskData>("Tasks/Farming/Orchard/"+species+" Harvest");
            Assert.IsNotNull(fruit);Assert.IsNotNull(seed);Assert.IsNotNull(task);
            Assert.AreEqual(id,fruit.resourceID);Assert.AreEqual(id+1,seed.resourceID);Assert.AreEqual(taskId,task.taskID);
            Assert.AreEqual(level,task.requiredSkillLevel);Assert.AreEqual("Farming",task.associatedSkill.name);
            Assert.AreEqual(value,CauldronConversion.UnitValue(fruit),1e-8);Assert.IsTrue(CauldronMixingPresentation.IsFood(fruit));
            Assert.IsFalse(CauldronMixingPresentation.IsFood(seed));Assert.IsTrue(seed.DisableAlterEcho);
            Assert.AreEqual(Resource.FoodEligibility.NotFood,seed.foodEligibility);
            Assert.AreEqual(1,task.resourceDrops.Count);Assert.AreSame(fruit,task.resourceDrops[0].resource);
            Assert.AreEqual(new Vector2Int(2,6),task.resourceDrops[0].dropRange);
            Assert.AreEqual(1,task.bonusDrops.Count);Assert.AreSame(seed,task.bonusDrops[0].resource);
            Assert.AreEqual(.1f,task.bonusDrops[0].chance);Assert.AreEqual(new Vector2Int(1,1),task.bonusDrops[0].range);
            Assert.IsTrue(task.taskPrefab.GetComponentsInChildren<ToolkitWorldAnchor>(true).All(x=>x.task==task.taskPrefab),"Native progress anchors must reference the new harvest task.");
            Assert.IsInstanceOf<FruitHarvestTask>(task.taskPrefab);Assert.IsNotInstanceOf<WoodcuttingTask>(task.taskPrefab);
            Assert.AreEqual(1,task.associatedSkill.resourceUnlocks.Count(x=>x.task==task&&x.requiredLevel==level));
            foreach(var resource in new[]{fruit,seed})
            {
                Assert.IsNotNull(resource.icon);Assert.IsNotNull(resource.UnknownIcon);
                Assert.AreEqual(new Vector2(16,16),resource.icon.rect.size);Assert.AreEqual(new Vector2(16,16),resource.UnknownIcon.rect.size);
                Assert.AreNotSame(resource.icon,resource.UnknownIcon);Assert.AreEqual(16,resource.UnknownIcon.pixelsPerUnit);
                Assert.AreEqual(FilterMode.Point,resource.UnknownIcon.texture.filterMode);
                Assert.IsTrue(ResourceIconLookup.TryGetIconIndex(resource.resourceID,out var index));
                Assert.IsTrue(ResourceIconLookup.TryGetUnknownIconIndex(resource.resourceID,out var unknown));Assert.AreEqual(index+1,unknown);
                var tmp=Resources.Load<TMP_SpriteAsset>("Fonts/FloatingTextIcons");
                Assert.AreSame(resource.icon,tmp.spriteGlyphTable[index].sprite);Assert.AreSame(resource.UnknownIcon,tmp.spriteGlyphTable[unknown].sprite);
                Assert.AreEqual(240,tmp.spriteGlyphTable[index].glyphRect.y);Assert.AreEqual(268,tmp.spriteGlyphTable.Count);
                Assert.AreEqual(256,tmp.spriteSheet.height);
                Assert.AreEqual(1,Resources.LoadAll<Resource>("Resource Items").Count(x=>x.resourceID==resource.resourceID));
                var inline=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.TextCore.Text.SpriteAsset>("Assets/UI/Toolkit/InlineSprites.asset"));
                var glyphs=inline.FindProperty("m_SpriteGlyphTable");Assert.AreEqual(268,glyphs.arraySize);
                AssertSpriteIdentity(resource.icon,glyphs.GetArrayElementAtIndex(index).FindPropertyRelative("sprite").objectReferenceValue);
                AssertSpriteIdentity(resource.UnknownIcon,glyphs.GetArrayElementAtIndex(unknown).FindPropertyRelative("sprite").objectReferenceValue);
                var inventory=AssetDatabase.LoadAssetAtPath<ToolkitResourceInventoryDefinition>("Assets/UI/Toolkit/ResourceInventory.asset");
                Assert.IsTrue(inventory.resources.Contains(resource));
            }
            var serialized=new SerializedObject(task.taskPrefab);
            var art=(Sprite)serialized.FindProperty("fruitingSprite").objectReferenceValue;
            Assert.AreEqual(new Vector2(32,64),art.rect.size);
            Assert.IsNotNull(serialized.FindProperty("harvestedSprite").objectReferenceValue);
            Assert.IsNotNull(serialized.FindProperty("harvestPoint").objectReferenceValue);
        }
        private static void AssertSpriteIdentity(Sprite expected,Object actual)
        {
            Assert.IsNotNull(actual);Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(expected,out string expectedGuid,out long expectedId));
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(actual,out string actualGuid,out long actualId));
            Assert.AreEqual(expectedGuid,actualGuid);Assert.AreEqual(expectedId,actualId);
        }
        [Test] public void ExactlyThirtyFourFoodsAndNoPropagationItems()
        {
            var foods=CauldronMixingPresentation.BuildFoodDefinitions();Assert.AreEqual(34,foods.Count);
            Assert.IsFalse(foods.Any(x=>x.name.Contains("Sapling")||x.name.Contains("Seed Pack")));
            foreach(var fish in Resources.LoadAll<TaskData>("Tasks/Fishing").SelectMany(x=>x.resourceDrops).Select(x=>x.resource).Distinct())Assert.Contains(fish,foods.ToList());
        }
        [Test] public void OrchardSpawnsOnlyInApprovedMapsAndSecondaryTerrainWeightsAreLower()
        {
            var orchard=Resources.LoadAll<TaskData>("Tasks/Farming/Orchard");Assert.AreEqual(4,orchard.Length);
            foreach(var guid in AssetDatabase.FindAssets("t:MapGenerationConfig"))
            {
                var map=AssetDatabase.LoadAssetAtPath<MapGenerationConfig>(AssetDatabase.GUIDToAssetPath(guid));
                var count=map.taskGeneratorSettings.farming.tasks.Count(x=>orchard.Contains(x));
                Assert.AreEqual(new[]{"Farmlands","Woods","River"}.Contains(map.name)?4:0,count,map.name);
            }
            foreach(var task in orchard)
            {
                Assert.AreEqual(3,task.spawnTerrains.Count);Assert.AreEqual(3,task.terrainWeights.Count);
                Assert.AreEqual(1,task.GetTerrainMultiplier(task.terrainWeights[0].terrain));
                Assert.AreEqual(.1f,task.GetTerrainMultiplier(task.terrainWeights[1].terrain));
                Assert.AreEqual(.05f,task.GetTerrainMultiplier(task.terrainWeights[2].terrain));
            }
            foreach(var task in Resources.LoadAll<TaskData>("Tasks").Where(x=>!orchard.Contains(x)))Assert.AreEqual(1,task.GetTerrainMultiplier(),task.name);
        }
    }
}
#endif
