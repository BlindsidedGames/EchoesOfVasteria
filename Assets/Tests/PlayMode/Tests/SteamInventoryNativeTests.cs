#if UNITY_INCLUDE_TESTS && UNITY_EDITOR && UNITY_STANDALONE && STEAMWORKS_NET
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Blindsided;
using NUnit.Framework;
using Steamworks;
using TimelessEchoes.Shop;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    // Opt-in native integration: reads a developer's prearranged Steam test inventory.
    // It never starts checkout, grants/consumes items, or writes a player save/cache.
    [PrebuildSetup(typeof(IsolatedPlayModeScene))]
    [PostBuildCleanup(typeof(IsolatedPlayModeScene))]
    public sealed class SteamInventoryNativeTests
    {
        [UnityTest, Explicit("Requires an authenticated Steam developer account and EOV_STEAM_NATIVE_TEST=1.")]
        public IEnumerator FreshCacheRestoresTheVerifiedInventoryThroughTheProductionProvider()
        {
            if (Environment.GetEnvironmentVariable("EOV_STEAM_NATIVE_TEST") != "1")
                Assert.Ignore("Native Steam integration is explicitly opt-in.");
            Assert.IsNull(Oracle.oracle, "Requires the runner's isolated scene; never load player progression.");
            Assert.IsNull(ShopService.Instance);
            Assert.IsNull(UnityEngine.Object.FindAnyObjectByType<SteamManager>());
            var expectedPermanent = Environment.GetEnvironmentVariable("EOV_STEAM_EXPECT_PERMANENT") == "1";
            var expectedSupporters = int.Parse(Environment.GetEnvironmentVariable("EOV_STEAM_EXPECT_SUPPORTERS") ?? "0");
            const BindingFlags instance = BindingFlags.Instance | BindingFlags.NonPublic;
            var enabledField = typeof(SteamManager).GetField("editorEnabled", BindingFlags.Static | BindingFlags.NonPublic);
            var previousEnabled = enabledField.GetValue(null);
            var directory = Path.Combine(Application.temporaryCachePath, "SteamShopNative-" + Guid.NewGuid().ToString("N"));
            GameObject owner = null;
            try
            {
                enabledField.SetValue(null, true); // Session only; never changes the user's EditorPrefs.
                Assert.True(SteamManager.Initialized, "Steam client must be running under a publisher account.");
                Assert.AreEqual(2940000u, SteamUtils.GetAppID().m_AppId);
                owner = new GameObject("Isolated native shop test");
                var shop = owner.AddComponent<ShopService>();
                shop.enabled = false;
                // Bind an owned temporary cache before the provider can identify the account.
                // The installation-level cache is never selected or written by this test.
                var path = Path.Combine(directory, "ownership.json");
                var stale = new ShopLedger(path);
                Assert.True(stale.ApplySteamInventory(!expectedPermanent, expectedSupporters + 1));
                typeof(ShopService).GetField("ledger", instance).SetValue(shop, stale);
                typeof(ShopService).GetField("steamAccount", instance).SetValue(shop, SteamUser.GetSteamID().ToString());
                var provider = owner.AddComponent<SteamInventoryPurchases>();
                provider.Initialize(shop);

                IEnumerator Complete(Action start)
                {
                    start();
                    var deadline = Time.realtimeSinceStartup + 65;
                    while (provider.Busy && Time.realtimeSinceStartup < deadline) yield return null;
                    Assert.False(provider.Busy, "Native request timed out: " + provider.Status);
                }

                yield return Complete(provider.Connect);
                foreach (var id in shop.Settings.supporterProductIds)
                    Assert.IsNotEmpty(provider.Price(id), "Missing native price: " + id);
                Assert.IsNotEmpty(provider.Price(shop.Settings.permanentProductId));
                Assert.AreEqual(expectedPermanent, new ShopLedger(path).Current.Permanent);
                Assert.AreEqual(expectedSupporters, new ShopLedger(path).Current.SupporterPurchases);
                yield return Complete(provider.Restore);
                Assert.AreEqual(expectedSupporters, new ShopLedger(path).Current.SupporterPurchases,
                    "Restoring retained items must not count them twice.");

                var freshPath = Path.Combine(directory, "fresh-install.json");
                typeof(ShopService).GetField("ledger", instance).SetValue(shop, new ShopLedger(freshPath));
                yield return Complete(provider.Restore);
                Assert.AreEqual(expectedPermanent, new ShopLedger(freshPath).Current.Permanent);
                Assert.AreEqual(expectedSupporters, new ShopLedger(freshPath).Current.SupporterPurchases);
            }
            finally
            {
                if (owner) UnityEngine.Object.DestroyImmediate(owner);
                var manager = UnityEngine.Object.FindAnyObjectByType<SteamManager>();
                if (manager) UnityEngine.Object.DestroyImmediate(manager.gameObject);
                enabledField.SetValue(null, previousEnabled);
                if (Directory.Exists(directory))
                {
                    foreach (var name in new[] { "ownership.json", "ownership.json.tmp", "fresh-install.json", "fresh-install.json.tmp" })
                        File.Delete(Path.Combine(directory, name));
                    Directory.Delete(directory);
                }
            }
        }
    }
}
#endif
