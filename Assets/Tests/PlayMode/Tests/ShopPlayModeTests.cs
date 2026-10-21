#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Blindsided.SaveData;
using NUnit.Framework;
using TimelessEchoes.Farming;
using TimelessEchoes.Shop;
using TimelessEchoes.UI.Toolkit;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.Localization.Settings;

namespace Tests.PlayMode
{
    public sealed partial class FieldsProductionPlayModeTests
    {
        [UnityTest]
        public IEnumerator ShopWaitsForIntroClaimsInstantlyAndKeepsPurchasesAcrossSaves()
        {
            // Earlier navigation fixtures can create the persistent shop. This test
            // already runs in the runner's protected empty scene, never the player's.
            if (ShopService.Instance) Object.DestroyImmediate(ShopService.Instance.gameObject);
            var shopObject = new GameObject("Isolated shop");
            var uiObject = new GameObject("Isolated shop UI", typeof(UIDocument));
            yield return LocalizationSettings.InitializationOperation;
            var originalLocale = LocalizationSettings.SelectedLocale;
            try
            {
                LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
                var shop = shopObject.AddComponent<ShopService>(); shop.enabled = false;
                typeof(ShopService).GetField("ledger", Private).SetValue(shop, new ShopLedger(Path.Combine(root, "shop.json")));
                var screen = uiObject.AddComponent<ToolkitShopScreen>();
                var appearance = Resources.Load<FieldsWorldAppearance>("Farming/FieldsWorldAppearance");
                screen.Configure(appearance.theme, appearance.runtimeTheme, appearance.textSettings);
                oracle.saveData.General.MapStatisticsTutorialStage = 8;
                Assert.False(screen.Show(), "The last introductory dialogue must finish first.");
                oracle.saveData.General.MapStatisticsTutorialStage = 9;
                Assert.True(screen.Show());
                yield return null; yield return null;
                var ui = uiObject.GetComponent<UIDocument>().rootVisualElement;
                var evidence = Path.Combine(Path.GetTempPath(), "eov-shop-review"); Directory.CreateDirectory(evidence);
                foreach (var size in new[] { new Vector2Int(1920,1080), new Vector2Int(960,2078) })
                {
                    UnityEditor.PlayModeWindow.SetCustomRenderingResolution((uint)size.x, (uint)size.y, "Shop review");
                    yield return null; yield return null;
                    FieldsClick(ui.Q<Button>("shop-claim"));
                    var claim = ui.Q<Button>("shop-promotion-claim");
                    Assert.NotNull(claim);
                    Assert.True(claim.enabledInHierarchy, "Claim is available immediately, before any timer or external visit.");
                    yield return null; yield return null;
                    ScreenCapture.CaptureScreenshot(Path.Combine(evidence, "promotion-" + size.x + ".png"));
                    yield return null;
                    FieldsClick(claim);
                    Assert.IsNull(ui.Q("shop-modal"));
                    Assert.GreaterOrEqual(shop.Remaining, ShopRules.ClaimSeconds);
                    ScreenCapture.CaptureScreenshot(Path.Combine(evidence, "shop-" + size.x + ".png"));
                    yield return null;
                }
                Assert.AreEqual(2 * ShopRules.ClaimSeconds, shop.Remaining);
                // Exercise the real screen and table delivery, rather than only testing
                // the localization adapter: hardcoded shop labels previously bypassed it.
                LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("fr");
                yield return LocalizationSettings.StringDatabase.GetTableAsync("TownUI");
                for (var i = 0; i < 8; i++) yield return null;
                Assert.True(ui.Query<Label>().ToList().Any(l => l.text == "Boutique"));
                Assert.AreEqual("Ajouter 30 min", ui.Q<Button>("shop-claim").text);
                FieldsClick(ui.Q<Button>("shop-claim"));
                for (var i = 0; i < 5; i++) yield return null;
                Assert.AreEqual("Obtenir 30 min", ui.Q<Button>("shop-promotion-claim").text);
                LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("ja");
                yield return LocalizationSettings.StringDatabase.GetTableAsync("TownUI");
                for (var i = 0; i < 8; i++) yield return null;
                Assert.AreEqual("30分受け取る", ui.Q<Button>("shop-promotion-claim").text, "An already-open promotion updates in place.");
                Assert.True(ui.Query<Label>().ToList().Any(l => l.text == "ゲーム紹介"));
                ScreenCapture.CaptureScreenshot(Path.Combine(evidence, "promotion-ja.png"));
                yield return null;
                screen.Hide();
                Assert.IsNull(uiObject.GetComponent<UIDocument>().panelSettings, "Closing releases the panel and its GPU atlases.");
                Assert.True(screen.Show());
                yield return null; yield return null;
                ui = uiObject.GetComponent<UIDocument>().rootVisualElement;
                Assert.True(ui.Query<Label>().ToList().Any(l => l.text == "ショップ"));
                Assert.True(shop.Fulfill(shop.Settings.permanentProductId, "test-permanent"));
                Assert.True(shop.PermanentOwned); Assert.False(shop.CanClaim);
                oracle.saveData = new GameData(); oracle.saveData.General.MapStatisticsTutorialStage = 9;
                Assert.True(shop.PermanentOwned, "Permanent ownership is independent of a gameplay save.");
                Assert.AreEqual(0, shop.Remaining);
                shop.TogglePermanent(); Assert.False(shop.Active);
                shop.TogglePermanent(); Assert.True(shop.Active);
            }
            finally
            {
                Object.DestroyImmediate(uiObject); Object.DestroyImmediate(shopObject);
                LocalizationSettings.SelectedLocale = originalLocale;
            }
        }
    }
}
#endif
