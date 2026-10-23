#if UNITY_INCLUDE_TESTS
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TimelessEchoes.Shop;
using Blindsided.SaveData;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class ShopTests
    {
        [Test] public void ShopRemainsLockedThroughTheFinalIntroAndUnlocksAfterIt()
        {
            var data = new GameData();
            data.General.MapStatisticsTutorialStage = 8;
            Assert.False(ShopRules.IntroComplete(data));
            data.General.MapStatisticsTutorialStage = 9;
            Assert.True(ShopRules.IntroComplete(data));
        }
        [TestCase(0, true)] [TestCase(5400, true)] [TestCase(5401, false)] [TestCase(7200, false)]
        public void ClaimsBankWholeThirtyMinuteRewardsWithoutExceedingTwoHours(double seconds, bool allowed)
            => Assert.AreEqual(allowed, ShopRules.CanClaim(seconds));
        [Test] public void BundledCatalogUsesTheIdsContractAndExcludesThisGame()
        {
            var catalog = PromotionCatalog.Parse(Resources.Load<TextAsset>("Shop/Promotions/catalog").text);
            Assert.Greater(catalog.Eligible("ios").Length, 0);
            Assert.False(catalog.Eligible("ios").Any(g => g.id == "echoes"));
            Assert.True(catalog.Eligible("desktop").All(g => g.links["desktop"].StartsWith("https://")));
        }
        [TestCase("http://apps.apple.com/app/example")]
        [TestCase("https://apps.apple.com.evil.test/app/example")]
        [TestCase("https://user@evil.test/app/example")]
        public void CatalogRejectsUntrustedDestinations(string link)
        {
            var text = "{\"schemaVersion\":1,\"revision\":\"1\",\"games\":[{\"id\":\"test\",\"enabled\":true,\"banner\":\"/promotions/images/test.123456abcdef.webp\",\"copy\":{\"en\":{\"title\":\"Test\",\"description\":\"Description\"}},\"links\":{\"ios\":\"" + link + "\"}}]}";
            Assert.Throws<FormatException>(() => PromotionCatalog.Parse(text));
        }
        [Test] public void StoreReplaySurvivesRestartWithoutDuplicatingSupportOrLosingPermanentOwnership()
        {
            var directory = Path.Combine(Path.GetTempPath(), "eov-shop-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "ownership.json");
            try
            {
                var settings = ShopSettings.Load(); var ledger = new ShopLedger(path);
                Assert.True(ledger.Fulfill(settings, settings.supporterProductIds[0], "support-1"));
                ledger = new ShopLedger(path);
                Assert.True(ledger.Fulfill(settings, settings.supporterProductIds[0], "support-1"));
                Assert.AreEqual(1, ledger.Current.SupporterPurchases);
                Assert.True(ledger.Fulfill(settings, settings.permanentProductId, "permanent-1"));
                Assert.True(new ShopLedger(path).Current.Permanent);
                Assert.False(ledger.Fulfill(settings, "unknown", "unknown-1"));
                Assert.False(ledger.Fulfill(settings, settings.permanentProductId, ""));
            }
            finally { File.Delete(path); File.Delete(path + ".tmp"); if (Directory.Exists(directory)) Directory.Delete(directory); }
        }
        [Test] public void FailedDurableWriteCannotGrantAPurchaseInMemory()
        {
            var directory = Path.Combine(Path.GetTempPath(), "eov-shop-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var obstruction = Path.Combine(directory, "file"); File.WriteAllText(obstruction, "not a directory");
            try
            {
                var ledger = new ShopLedger(Path.Combine(obstruction, "ownership.json")); var settings = ShopSettings.Load();
                Assert.False(ledger.Fulfill(settings, settings.permanentProductId, "paid-1"));
                Assert.False(ledger.Current.Permanent);
                Assert.False(ledger.ApplySteamInventory(true, 1));
                Assert.False(ledger.Current.Permanent);
                Assert.AreEqual(0, ledger.Current.SupporterPurchases);
                Assert.False(ledger.GrantLegacyAndroidInstall(1, DateTimeOffset.Parse(settings.legacyPurchaseCutoff)));
                Assert.False(ledger.Current.OriginalOwner);
            }
            finally { File.Delete(obstruction); Directory.Delete(directory); }
        }
        [TestCase(-1, true)] [TestCase(0, false)] [TestCase(1, false)]
        public void AndroidLegacyGrantUsesTheExclusiveCutoffAndSurvivesLaterInstallDates(long offsetMilliseconds, bool eligible)
        {
            var directory = Path.Combine(Path.GetTempPath(), "eov-android-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "ownership.json");
            try
            {
                var cutoff = DateTimeOffset.Parse(ShopSettings.Load().legacyPurchaseCutoff);
                Assert.AreEqual(new DateTimeOffset(2026, 10, 17, 0, 0, 0, TimeSpan.FromHours(11)), cutoff);
                var ledger = new ShopLedger(path);
                Assert.False(ledger.GrantLegacyAndroidInstall(0, cutoff));
                Assert.False(ledger.GrantLegacyAndroidInstall(-1, cutoff));
                Assert.AreEqual(eligible, ledger.GrantLegacyAndroidInstall(cutoff.ToUnixTimeMilliseconds() + offsetMilliseconds, cutoff));
                ledger = new ShopLedger(path);
                Assert.AreEqual(eligible, ledger.Current.OriginalOwner);
                Assert.AreEqual(eligible, ledger.GrantLegacyAndroidInstall(cutoff.AddDays(30).ToUnixTimeMilliseconds(), cutoff));
                Assert.AreEqual(eligible, ledger.GrantLegacyAndroidInstall(0, cutoff));
                Assert.AreEqual(eligible, new ShopLedger(path).Current.OriginalOwner);
            }
            finally
            {
                if (Directory.Exists(directory)) { File.Delete(path); File.Delete(path + ".tmp"); Directory.Delete(directory); }
            }
        }
        [Test] public void SteamRestoreReconcilesRetainedStacksAndRefundsWithoutDuplicatingSupport()
        {
            var directory = Path.Combine(Path.GetTempPath(), "eov-steam-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "ownership.json");
            try
            {
                var ledger = new ShopLedger(path);
                Assert.True(ledger.ApplySteamInventory(true, 3));
                ledger = new ShopLedger(path);
                Assert.True(ledger.ApplySteamInventory(true, 3));
                Assert.AreEqual(3, ledger.Current.SupporterPurchases);
                Assert.True(ledger.ApplySteamInventory(true, 4));
                Assert.AreEqual(4, new ShopLedger(path).Current.SupporterPurchases);
                Assert.True(ledger.ApplySteamInventory(false, 2));
                ledger = new ShopLedger(path);
                Assert.False(ledger.Current.Permanent);
                Assert.AreEqual(2, ledger.Current.SupporterPurchases);
                Assert.False(ledger.ApplySteamInventory(true, -1));
                Assert.False(ledger.Current.Permanent);
            }
            finally { File.Delete(path); File.Delete(path + ".tmp"); if (Directory.Exists(directory)) Directory.Delete(directory); }
        }
    }
}
#endif
