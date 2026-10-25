#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.Linq;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Gear;
using TimelessEchoes.UI.Toolkit;
using TimelessEchoes.Upgrades;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [UnityTest]
        public IEnumerator MixAllConvertsWholeEligibleFoodsOnceAndLeavesSeedsAndNonFood()
        {
            yield return UnityEngine.Localization.Settings.LocalizationSettings.InitializationOperation;
            var previousLocale = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale;
            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale = UnityEngine.Localization.Settings.LocalizationSettings.AvailableLocales.GetLocale("en");
            var foods = new[] { "Radish", "Bloopicus Maximus" }.Select(name => Resources.LoadAll<Resource>("").Single(r => r.name == name)).ToArray();
            var seed = Resources.Load<Resource>("Resource Items/Radish Seed Pack");
            var log = Resources.LoadAll<Resource>("").Single(r => r.name == "Log");
            for (int i=0;i<foods.Length;i++) oracle.saveData.Resources[foods[i].name] = new GameData.ResourceEntry { Amount = 12.5 + i, Earned = true, Tier = 1 };
            foreach (var resource in new[]{seed, log}) oracle.saveData.Resources[resource.name] = new GameData.ResourceEntry { Amount = 100, Earned = true, Tier = 1 };
            var owner = new GameObject("Mix all services"); var cauldronObject = new GameObject("Mix all cauldron");cauldronObject.SetActive(false);GameObject screen=null;
            try
            {
                var inventory=owner.AddComponent<ResourceManager>();var manager=cauldronObject.AddComponent<CauldronManager>();
                typeof(CauldronManager).GetField("config",Private).SetValue(manager,CauldronResourceYield.Config);cauldronObject.SetActive(true);
                screen=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Cauldron.prefab"));Assert.True(screen.GetComponent<ToolkitCauldronScreen>().Show());yield return null;
                var all=screen.GetComponent<UIDocument>().rootVisualElement.Q<Button>("mix-all");Assert.NotNull(all,"The Cauldron must offer Mix All.");
                StringAssert.Contains("+22.0 stew", System.Text.RegularExpressions.Regex.Replace(all.text, "<[^>]*>", ""), "Preview counts whole food units and excludes seeds and non-food.");
                inventory.Add(foods[0], 10, trackStats: false, eligibleForTierRoll: false);
                yield return null;
                StringAssert.Contains("+23.0 stew", System.Text.RegularExpressions.Regex.Replace(all.text, "<[^>]*>", ""), "Preview follows inventory changes.");
                var before=oracle.saveData.CauldronStew;var sequence=oracle.saveData.CauldronConversionSequence;
                const double gain = 23;
                FieldsClick(all);yield return null;
                Assert.AreEqual(before+gain,oracle.saveData.CauldronStew,1e-8);
                foreach(var f in foods)Assert.AreEqual(.5,inventory.GetAmount(f),1e-8);
                foreach(var f in new[]{seed,log})Assert.AreEqual(100,inventory.GetAmount(f));
                Assert.AreEqual(sequence+2,oracle.saveData.CauldronConversionSequence);Assert.False(all.enabledSelf);
                FieldsClick(all);Assert.AreEqual(before+gain,oracle.saveData.CauldronStew,1e-8,"Repeated input must not spend the same stock twice.");
            }
            finally {UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale=previousLocale;if(screen)Object.DestroyImmediate(screen);Object.DestroyImmediate(cauldronObject);Object.DestroyImmediate(owner);}
        }
        [UnityTest]
        public IEnumerator ForgeNeverStopIgnoresUpgradesDisablesOtherTogglesAndHonoursStopAndMaterials()
        {
            oracle.saveData.General.ForgeIntroductionSeen=true;oracle.saveData.CraftingMasteryLevel=3;
            var owner=new GameObject("Never stop forge services");GameObject screen=null;
            try
            {
                var inventory=owner.AddComponent<ResourceManager>();owner.AddComponent<CraftingService>();owner.AddComponent<EquipmentController>();owner.AddComponent<SalvageService>();
                screen=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Forge.prefab"));Assert.True(screen.GetComponent<ToolkitForgeScreen>().Show());yield return null;
                var ui=screen.GetComponent<UIDocument>().rootVisualElement;var toggle=ui.Q<Button>("forge-never-stop");Assert.NotNull(toggle,"The Forge must offer Never stop.");
                var session=screen.GetComponent<ForgeSession>();var binding=session.Catalog.Find(session.Core);
                inventory.Add(binding.coreResource,100,trackStats:false,eligibleForTierRoll:false);inventory.Add(binding.ingotResource,100*session.Core.ingotCost,trackStats:false,eligibleForTierRoll:false);
                FieldsClick(toggle);yield return new WaitForSecondsRealtime(.2f);
                foreach(var id in new[]{"forge-stop-vastium","forge-lock-stats","forge-repeat-craft"})Assert.False(ui.Q<Button>(id).enabledInHierarchy,id);
                var before=oracle.saveData.Forge.TotalCrafts;FieldsClick(ui.Q<Button>("forge-craft"));yield return new WaitForSecondsRealtime(.5f);
                Assert.GreaterOrEqual(oracle.saveData.Forge.TotalCrafts-before,2,"An empty equipment slot makes every roll an upgrade, which must no longer stop crafting.");Assert.True(session.IsAutoCrafting);
                FieldsClick(ui.Q<Button>("forge-craft"));Assert.False(session.IsAutoCrafting,"Stop must always work.");
                var coreBalance=inventory.GetAmount(binding.coreResource);inventory.Spend(binding.coreResource,coreBalance);
                session.StartAutoCrafting();Assert.False(session.IsAutoCrafting,"Never stop cannot craft without materials.");
                FieldsClick(toggle);yield return new WaitForSecondsRealtime(.2f);
                foreach(var id in new[]{"forge-stop-vastium","forge-lock-stats","forge-repeat-craft"})Assert.True(ui.Q<Button>(id).enabledInHierarchy,id);
            }
            finally{if(screen)Object.DestroyImmediate(screen);Object.DestroyImmediate(owner);}
        }
    }
}
#endif

