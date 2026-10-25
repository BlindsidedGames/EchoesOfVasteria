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
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [Test]
        public void CauldronWaitsForExplicitTasteAfterMixingOrLoading()
        {
            var food = Resources.LoadAll<Resource>("").Single(r => r.name == "Radish");
            oracle.saveData.CauldronStew = 20;
            oracle.saveData.Resources[food.name] = new GameData.ResourceEntry { Amount = 10, Earned = true };
            var inventoryObject = new GameObject("Manual tasting inventory");
            var cauldronObject = new GameObject("Manual tasting owner"); cauldronObject.SetActive(false);
            try
            {
                inventoryObject.AddComponent<ResourceManager>();
                var cauldron = cauldronObject.AddComponent<CauldronManager>();
                typeof(CauldronManager).GetField("config", Private).SetValue(cauldron, CauldronResourceYield.Config);
                cauldronObject.SetActive(true);
                Assert.IsFalse(cauldron.IsTasting, "Existing stew must not start tasting on activation.");
                Assert.IsTrue(cauldron.TryAddToCauldron(food, 10, 1, out var gained, out var error), error);
                Assert.AreEqual(1, gained, 1e-9);
                Assert.IsFalse(cauldron.IsTasting, "Mixing must leave tasting stopped.");
                Assert.AreEqual(21, cauldron.Stew, 1e-9);
                cauldron.StartTasting();
                Assert.IsTrue(cauldron.IsTasting, "The explicit Taste action still starts tasting.");
                Blindsided.EventHandler.LoadData();
                Assert.IsFalse(cauldron.IsTasting, "Loading a save must not consume its stew.");
                Assert.AreEqual(20, cauldron.Stew, 1e-9, "Loading does not spend another taste.");
                cauldron.StartTasting();
                cauldron.enabled = false;
                cauldron.enabled = true;
                Assert.IsFalse(cauldron.IsTasting, "Reactivation must also wait for Taste.");
            }
            finally
            {
                Object.DestroyImmediate(cauldronObject);
                Object.DestroyImmediate(inventoryObject);
            }
        }

        [TestCase(1, 2)]
        [TestCase(11, 2)]
        [TestCase(12, 10)]
        [TestCase(41, 10)]
        [TestCase(42, 100)]
        [TestCase(70, 100)]
        public void CauldronTastingRateFollowsEvaLevel(int evaLevel, int expectedTastes)
        {
            oracle.saveData.CauldronEvaLevel = evaLevel;
            oracle.saveData.CauldronStew = 1000;
            var inventoryObject = new GameObject("Tasting rate inventory");
            var cauldronObject = new GameObject("Tasting rate owner");
            cauldronObject.SetActive(false);
            try
            {
                inventoryObject.AddComponent<ResourceManager>();
                var cauldron = cauldronObject.AddComponent<CauldronManager>();
                typeof(CauldronManager).GetField("config", Private).SetValue(cauldron, CauldronResourceYield.Config);
                cauldronObject.SetActive(true);
                cauldron.enabled = false; // Advance the real taste loop with controlled elapsed time.
                var beforeStart = oracle.saveData.CauldronTotals.TotalTastings;
                var stewBeforeStart = cauldron.Stew;
                cauldron.StartTasting();
                Assert.AreEqual(beforeStart + 1, oracle.saveData.CauldronTotals.TotalTastings, "Pressing Taste performs the first taste synchronously.");
                Assert.AreEqual(stewBeforeStart - 1, cauldron.Stew, 1e-9);
                cauldron.StartTasting();
                Assert.AreEqual(beforeStart + 1, oracle.saveData.CauldronTotals.TotalTastings, "Starting an active session must not repeat the instant taste.");
                typeof(CauldronManager).GetField("_lastTasteTime", Private).SetValue(cauldron, Time.unscaledTime - .5f / expectedTastes);
                typeof(CauldronManager).GetMethod("TasteTick", Private).Invoke(cauldron, null);
                Assert.AreEqual(beforeStart + 1, oracle.saveData.CauldronTotals.TotalTastings, "The next taste must wait for its interval.");
                var before = oracle.saveData.CauldronTotals.TotalTastings;
                var stewBefore = cauldron.Stew;
                typeof(CauldronManager).GetField("_lastTasteTime", Private).SetValue(cauldron, Time.unscaledTime - 1.001f);
                typeof(CauldronManager).GetField("_accumulatedTasteTime", Private).SetValue(cauldron, 0f);
                typeof(CauldronManager).GetMethod("TasteTick", Private).Invoke(cauldron, null);
                Assert.AreEqual(expectedTastes, oracle.saveData.CauldronTotals.TotalTastings - before);
                Assert.AreEqual(stewBefore - expectedTastes, cauldron.Stew, 1e-9);
                Assert.AreEqual(expectedTastes, cauldron.TastesPerSecond, "The UI rate matches actual consumption.");
            }
            finally
            {
                Object.DestroyImmediate(cauldronObject);
                Object.DestroyImmediate(inventoryObject);
            }
        }

        [UnityTest]
        public IEnumerator CauldronRemembersEachFoodLocallyWithoutAddingPreferencesToSaves()
        {
            yield return LocalizationSettings.InitializationOperation;
            var previousLocale = LocalizationSettings.SelectedLocale;
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
            var fish = Resources.LoadAll<Resource>("").Single(r => r.name == "Bloopicus Maximus");
            var crop = Resources.LoadAll<Resource>("").Single(r => r.name == "Radish");
            var preferenceKeys = new[] { "Cauldron.LastFood", "Cauldron.Amount." + fish.name, "Cauldron.Amount." + crop.name };
            var previousPreferences = preferenceKeys.ToDictionary(key => key, key => PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null);
            foreach (var key in preferenceKeys) PlayerPrefs.DeleteKey(key);
            oracle.saveData.SavedPreferences.LastSelectedCauldronFood = fish.name;
            oracle.saveData.SavedPreferences.CauldronFoodAmounts = new System.Collections.Generic.Dictionary<string, double> { [fish.name] = 17 };
            foreach (var food in new[] { fish, crop })
                oracle.saveData.Resources[food.name] = new GameData.ResourceEntry { Amount = 1000, Earned = true };
            var inventoryObject = new GameObject("Remembered food inventory");
            var cauldronObject = new GameObject("Remembered food cauldron"); cauldronObject.SetActive(false);
            GameObject screenObject = null;
            try
            {
                var inventory = inventoryObject.AddComponent<ResourceManager>();
                var cauldron = cauldronObject.AddComponent<CauldronManager>();
                typeof(CauldronManager).GetField("config", Private).SetValue(cauldron, CauldronResourceYield.Config);
                cauldronObject.SetActive(true);
                screenObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Toolkit/Cauldron.prefab"));
                var screen = screenObject.GetComponent<ToolkitCauldronScreen>();
                Assert.IsTrue(screen.Show()); yield return null;
                var document = screenObject.GetComponent<UIDocument>().rootVisualElement;
                Assert.AreEqual("17", document.Q<TextField>("conversion-amount").value, "An existing saved selection moves to local preferences.");
                void Select(Resource food)
                {
                    var button = document.Query<Button>(className: "food").ToList().Single(b => b.Q<Label>("food-name").text == ToolkitLocalization.Name(food));
                    button.Focus(); button.SendEvent(NavigationSubmitEvent.GetPooled());
                }
                Select(fish); document.Q<TextField>("conversion-amount").value = "123";
                Select(crop); document.Q<TextField>("conversion-amount").value = "25";
                Select(fish);
                Assert.AreEqual("123", document.Q<TextField>("conversion-amount").value, "Each food remembers its requested batch.");
                var add = document.Q<Button>("add-to-cauldron");
                add.Focus(); add.SendEvent(NavigationSubmitEvent.GetPooled());
                Assert.AreEqual(877, inventory.GetAmount(fish), 1e-9);
                Assert.AreEqual("123", document.Q<TextField>("conversion-amount").value, "Mixing preserves the next batch size.");
                add.Focus(); add.SendEvent(NavigationSubmitEvent.GetPooled());
                Assert.AreEqual(754, inventory.GetAmount(fish), 1e-9, "The next batch uses a fresh conversion receipt.");
                screen.Hide();
                var bytes = CurrentSaveCodec.Serialize(oracle.saveData);
                var serialized = System.Text.Encoding.UTF8.GetString(bytes).Replace("\0", "");
                Assert.IsFalse(serialized.Contains("CauldronFoodAmounts"), "Local food amounts must not bloat the save.");
                Assert.IsFalse(serialized.Contains("LastSelectedCauldronFood"), "The selected food is also a local preference.");
                oracle.saveData = CurrentSaveCodec.Clone(oracle.saveData);
                oracle.saveData.SavedPreferences = new GameData.Preferences();
                Assert.IsTrue(screen.Show()); yield return null;
                document = screenObject.GetComponent<UIDocument>().rootVisualElement;
                Assert.AreEqual("123", document.Q<TextField>("conversion-amount").value, "Local selection and amount survive loading a save that contains neither.");
                Select(crop);
                Assert.AreEqual("25", document.Q<TextField>("conversion-amount").value);
                inventory.Spend(crop, 990);
                yield return null;
                Assert.AreEqual("25", document.Q<TextField>("conversion-amount").value, "Low stock must not overwrite the requested batch.");
                Assert.IsFalse(document.Q<Button>("add-to-cauldron").enabledSelf);
            }
            finally
            {
                LocalizationSettings.SelectedLocale = previousLocale;
                if (screenObject) Object.DestroyImmediate(screenObject);
                Object.DestroyImmediate(cauldronObject); Object.DestroyImmediate(inventoryObject);
                foreach (var preference in previousPreferences)
                    if (preference.Value == null) PlayerPrefs.DeleteKey(preference.Key);
                    else PlayerPrefs.SetString(preference.Key, preference.Value);
                PlayerPrefs.Save();
            }
        }

        // The actual input/preview/commit boundary owns whole-resource UI behavior;
        // conversion tests separately preserve fractional core math and save atomicity.
        [UnityTest]
        public IEnumerator CauldronQuantityFloorsInputAndFormatsPreviewWithoutChangingUnitValue()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            yield return LocalizationSettings.InitializationOperation;
            var previousLocale = LocalizationSettings.SelectedLocale;
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
            var food = Resources.LoadAll<Resource>("").Single(r => r.name == "Bloopicus Maximus");
            var preferenceKeys = new[] { "Cauldron.LastFood", "Cauldron.Amount." + food.name };
            var previousPreferences = preferenceKeys.ToDictionary(key => key, key => PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null);
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
                Assert.IsTrue(add.Contains(preview), "The conversion preview belongs inside the Mix button.");
                Assert.IsNotNull(add.Q<Image>()?.sprite, "The Mix button must display the stew artwork.");
                var max = document.Q<Button>(className: "amount-max");
                max.Focus(); max.SendEvent(NavigationSubmitEvent.GetPooled());
                Assert.AreEqual("973", input.value, "Max must match whole available resources.");
                Assert.AreEqual("1.55\u00a0K", Regex.Replace(preview.text, "<[^>]*>", ""));
                Assert.IsNotNull(document.Q<Button>("mix-all").Q<Image>()?.sprite);
                Assert.IsFalse(document.Q<Label>("mix-all-stew").text.Contains("+"));
                CaptureFeedback("cauldron-whole-quantity.png");
                yield return null; yield return null;
                input.value = "12.9";
                Assert.AreEqual("12", input.value);
                Assert.AreEqual("19.2", Regex.Replace(preview.text, "<[^>]*>", ""));
                LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("fr");
                yield return LocalizationSettings.StringDatabase.GetTableEntryAsync("TownUI", "cauldron.mix-preview");
                yield return null; yield return null;
                input.value = "13,9";
                Assert.AreEqual("13", input.value, "Editable whole quantities accept the selected language decimal separator.");
                Assert.AreEqual("20,8", Regex.Replace(preview.text, "<[^>]*>", ""), "Preview follows the game's locale-sensitive number formatter.");
                LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
                yield return null; yield return null;
                foreach (var invalid in new[] { "0", "0.9", "-1", "bad", "NaN", "Infinity" })
                {
                    input.value = invalid;
                    Assert.IsFalse(add.enabledSelf, invalid);
                    Assert.AreEqual("0.00", Regex.Replace(preview.text, "<[^>]*>", ""));
                }
                inventory.Add(food, 5000000000, trackStats: false, eligibleForTierRoll: false);
                yield return null;
                max.Focus(); max.SendEvent(NavigationSubmitEvent.GetPooled());
                Assert.AreEqual("5000000973", input.value);
                Assert.AreEqual("8.00\u00a0B", Regex.Replace(preview.text, "<[^>]*>", ""));
                input.value = "973.9";
                var before = inventory.GetAmount(food);
                var stewBefore = oracle.saveData.CauldronStew;
                Assert.IsTrue((bool)typeof(Blindsided.Oracle).GetMethod("SaveNowBlocking", Private)
                    .Invoke(oracle, new object[] { oracle.CurrentSlot, false }));
                add.Focus(); add.SendEvent(NavigationSubmitEvent.GetPooled());
                Assert.AreEqual(before - 973, inventory.GetAmount(food), 1e-6, "The native action spends the same floored quantity it previews.");
                Assert.AreEqual(stewBefore + 1556.8, oracle.saveData.CauldronStew, 1e-8, "Formatting must not round conversion math.");
                var disk = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
                Assert.IsTrue(disk.Succeeded, disk.Diagnostic);
                Assert.AreEqual(stewBefore, disk.Data.CauldronStew, "Mixing must wait for the next checkpoint.");
                var receipt = oracle.saveData.LastCauldronConversion;
                Assert.IsTrue(cauldron.TryAddToCauldron(food, 973, receipt.Sequence, out _, out _));
                Assert.AreEqual(before - 973, inventory.GetAmount(food), 1e-6, "Replaying a conversion must not spend twice.");
                Assert.IsTrue((bool)typeof(Blindsided.Oracle).GetMethod("SaveNowBlocking", Private)
                    .Invoke(oracle, new object[] { oracle.CurrentSlot, false }));
                disk = SaveManager.Instance.LoadDetailedAsync(oracle.GetSlotDirectoryName(oracle.CurrentSlot)).GetAwaiter().GetResult();
                Assert.IsTrue(disk.Succeeded, disk.Diagnostic);
                Assert.AreEqual(stewBefore + 1556.8, disk.Data.CauldronStew, 1e-8);
                Assert.AreEqual(before - 973, disk.Data.Resources[food.name].Amount, 1e-6);
                Assert.AreEqual(receipt.Sequence, disk.Data.CauldronConversionSequence);
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                LocalizationSettings.SelectedLocale = previousLocale;
                if (screenObject) Object.DestroyImmediate(screenObject);
                Object.DestroyImmediate(cauldronObject); Object.DestroyImmediate(inventoryObject);
                foreach (var preference in previousPreferences)
                    if (preference.Value == null) PlayerPrefs.DeleteKey(preference.Key);
                    else PlayerPrefs.SetString(preference.Key, preference.Value);
                PlayerPrefs.Save();
            }
        }
    }
}
#endif
