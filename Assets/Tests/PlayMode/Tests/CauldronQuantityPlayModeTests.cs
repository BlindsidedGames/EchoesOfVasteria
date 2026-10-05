#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Upgrades;
using TimelessEchoes.Upgrades.Cauldron;
using TimelessEchoes.UI.Toolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        // The actual input/preview/commit boundary owns whole-resource UI behavior;
        // conversion tests separately preserve fractional core math and save atomicity.
        [UnityTest]
        public IEnumerator CauldronQuantityFloorsInputAndFormatsPreviewWithoutChangingUnitValue()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            var food = Resources.LoadAll<Resource>("").Single(r => r.name == "Bloopicus Maximus");
            oracle.saveData.Resources[food.name] = new GameData.ResourceEntry { Amount = 973.607686865636, Earned = true, Tier = 1 };
            var inventoryObject = new GameObject("Isolated cauldron food inventory");
            var cauldronObject = new GameObject("Isolated cauldron owner"); cauldronObject.SetActive(false);
            GameObject screenObject = null;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                var inventory = inventoryObject.AddComponent<ResourceManager>();
                var cauldron = cauldronObject.AddComponent<CauldronManager>();
                typeof(CauldronManager).GetField("config", Private).SetValue(cauldron, CauldronResourceYield.Config);
                cauldronObject.SetActive(true);
                screenObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Cauldron.prefab"));
                Assert.IsTrue(screenObject.GetComponent<ToolkitCauldronScreen>().Show());
                yield return null;
                var document = screenObject.GetComponent<UIDocument>().rootVisualElement;
                var foodButton = document.Query<Button>(className: "food").ToList().Single(b => b.Q<Label>("food-name").text == ToolkitLocalization.Name(food));
                foodButton.Focus(); foodButton.SendEvent(NavigationSubmitEvent.GetPooled());
                var input = document.Q<TextField>("conversion-amount");
                var preview = document.Q<Label>("predicted-stew");
                var add = document.Q<Button>("add-to-cauldron");
                var max = document.Q<Button>(className: "amount-max");
                max.Focus(); max.SendEvent(NavigationSubmitEvent.GetPooled());
                Assert.AreEqual("973", input.value, "Max must match whole available resources.");
                Assert.AreEqual("+38.9", Regex.Replace(preview.text, "<[^>]*>", ""));
                CaptureFeedback("cauldron-whole-quantity.png");
                yield return null; yield return null;
                input.value = "12.9";
                Assert.AreEqual("12", input.value);
                Assert.AreEqual("+0.48", Regex.Replace(preview.text, "<[^>]*>", ""));
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                input.value = "13.9";
                Assert.AreEqual("13", input.value, "Editable whole quantities retain the established invariant input format.");
                Assert.AreEqual("+0,52", Regex.Replace(preview.text, "<[^>]*>", ""), "Preview follows the game's locale-sensitive number formatter.");
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                foreach (var invalid in new[] { "0", "0.9", "-1", "bad", "NaN", "Infinity" })
                {
                    input.value = invalid;
                    Assert.IsFalse(add.enabledSelf, invalid);
                    Assert.AreEqual("+0.00", Regex.Replace(preview.text, "<[^>]*>", ""));
                }
                inventory.Add(food, 5000000000, trackStats: false, eligibleForTierRoll: false);
                yield return null;
                max.Focus(); max.SendEvent(NavigationSubmitEvent.GetPooled());
                Assert.AreEqual("5000000973", input.value);
                Assert.AreEqual("+200 M", Regex.Replace(preview.text, "<[^>]*>", ""));
                input.value = "973.9";
                var before = inventory.GetAmount(food);
                var stewBefore = oracle.saveData.CauldronStew;
                add.Focus(); add.SendEvent(NavigationSubmitEvent.GetPooled());
                Assert.AreEqual(before - 973, inventory.GetAmount(food), 1e-6, "The native action spends the same floored quantity it previews.");
                Assert.AreEqual(stewBefore + 38.92, oracle.saveData.CauldronStew, 1e-8, "Formatting must not round conversion math.");
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                if (screenObject) Object.DestroyImmediate(screenObject);
                Object.DestroyImmediate(cauldronObject); Object.DestroyImmediate(inventoryObject);
            }
        }
    }
}
#endif
