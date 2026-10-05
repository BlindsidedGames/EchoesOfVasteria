#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Blindsided.SaveData;
using TimelessEchoes.Skills;
using NUnit.Framework;
using TimelessEchoes.Farming;
using TimelessEchoes.Upgrades;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        // Distinct UI lifecycle contract: the already-open ordinary inventory must
        // reveal a newly earned pack and refresh its count after ordinary spending.
        // Core fixtures separately own durable credit/migration/replay semantics.
        [UnityTest]
        public IEnumerator OpenOrdinaryInventoryReflectsSeedCreditAndSpend()
        {
            Assert.IsNull(ResourceManager.Instance);
            var managerObject = new GameObject("Isolated ordinary inventory owner");
            GameObject screenObject = null;
            try
            {
                var manager = managerObject.AddComponent<ResourceManager>();
                var seed = Resources.Load<Resource>("Resource Items/Radish Seed Pack");
                Assert.NotNull(seed);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/ResourceInventory.prefab");
                screenObject = Object.Instantiate(prefab);
                var screen = screenObject.GetComponent<ToolkitResourceInventoryScreen>();
                screen.Bounds = new Rect(64, 44, 640, 376);
                Assert.IsTrue(screen.Show(), "Authored standalone inventory must be configured.");
                var button = screenObject.GetComponent<UIDocument>().rootVisualElement.Q<Button>("resource-Radish Seed Pack");
                Assert.NotNull(button, "Ordinary seed resource must be represented in the authored inventory.");
                var icon = button.Q<Image>();
                var count = button.Q<Label>();
                Assert.AreSame(seed.UnknownIcon, icon.sprite);
                Assert.AreEqual("0", Regex.Replace(count.text, "<[^>]*>", ""));
                var operation = FarmJournal.StageSeed(service.State, FarmCommands.RadishSeedId, true, Now);
                Assert.IsTrue(Commit(s => FarmCommands.CreditSeed(s, operation, Now, FarmCommands.RadishSeedId, true)));
                yield return null; // Normal screen Update consumes the resource event.
                Assert.AreSame(seed.icon, icon.sprite);
                Assert.AreEqual("1", Regex.Replace(count.text, "<[^>]*>", ""));
                screen.HighlightResource(seed, false);
                Assert.AreEqual(ToolkitLocalization.Text("inventory.resource-tier", "{0} - Tier {1}", ToolkitLocalization.Name(seed), 1),
                    screenObject.GetComponent<UIDocument>().rootVisualElement.Q<Label>("selected-resource").text,
                    "Standalone inventory reports the canonical seed tier, not Forge display rarity.");
                CaptureFeedback("seed-inventory-earned.png");
                yield return null;
                yield return null;
                Assert.IsTrue(manager.Spend(seed, 1));
                yield return null;
                Assert.AreEqual("0", Regex.Replace(count.text, "<[^>]*>", ""));
                Assert.AreSame(seed.icon, icon.sprite, "Spent but discovered resources remain identifiable.");
            }
            finally
            {
                if (screenObject) Object.DestroyImmediate(screenObject);
                Object.DestroyImmediate(managerObject);
            }
        }
        // Renderer risk separate from content identity: a 32x64 authored tree
        // must fit the skill row's 16px thumbnail instead of clipping its canopy.
        [UnityTest]
        public IEnumerator OrchardUnlockThumbnailsFitTheActualSkillRow()
        {
            var definition = AssetDatabase.LoadAssetAtPath<ToolkitSkillsDefinition>("Assets/UI/Toolkit/Skills.asset");
            var farming = definition.skills.Single(x => x.name == "Farming");
            oracle.saveData.SkillData[farming.name] = new GameData.SkillProgress { Level = 72 };
            var controllerObject = new GameObject("Isolated skill presentation owner");
            controllerObject.SetActive(false);
            GameObject screenObject = null;
            try
            {
                var controller = controllerObject.AddComponent<SkillController>();
                typeof(SkillController).GetField("skills", Private).SetValue(controller, new List<Skill>(definition.skills));
                controllerObject.SetActive(true);
                screenObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Skills.prefab"));
                var screen = screenObject.GetComponent<ToolkitSkillsScreen>();
                Assert.IsTrue(screen.Show());
                var ui = screenObject.GetComponent<UIDocument>().rootVisualElement;
                yield return null; // Attach and lay out before keyboard submission.
                var selector = ui.Q<Button>("skill-Farming");
                selector.Focus();
                using (var submit = NavigationSubmitEvent.GetPooled()) selector.SendEvent(submit);
                yield return null;
                foreach (var species in new[] { "Apple", "Pear", "Peach", "Cherry" })
                {
                    var row = ui.Q("unlock-" + species + " Harvest");
                    Assert.NotNull(row);
                    var image = row.Q<Image>();
                    Assert.NotNull(image);
                    Assert.AreEqual(16f, image.style.width.value.value);
                    Assert.AreEqual(16f, image.style.height.value.value);
                    Assert.AreEqual(ScaleMode.ScaleToFit, image.scaleMode);
                }
                var cherry = ui.Q("unlock-Cherry Harvest");
                cherry.GetFirstAncestorOfType<ScrollView>().ScrollTo(cherry);
                yield return null;
                CaptureFeedback("farming-cherry-tree-unlock.png");
                yield return null;
                yield return null;
            }
            finally
            {
                if (screenObject) Object.DestroyImmediate(screenObject);
                Object.DestroyImmediate(controllerObject);
            }
        }
        private static void CaptureFeedback(string name)
        {
            var evidence = Path.Combine(Path.GetTempPath(), "eov-tutorial-investigation");
            Directory.CreateDirectory(evidence);
            ScreenCapture.CaptureScreenshot(Path.Combine(evidence, name));
        }
    }
}
#endif
