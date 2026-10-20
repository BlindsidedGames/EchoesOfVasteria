using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TimelessEchoes.Shop
{
    public sealed class LegacyShopOwner : MonoBehaviour
    {
        [Serializable] private sealed class AppleResult { public bool verified, eligible; }
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void EOV_CheckLegacyPurchase(string objectName, double cutoff);
#endif
        public void Check()
        {
            var shop = GetComponent<ShopService>();
            if (!DateTimeOffset.TryParse(shop.Settings.legacyPurchaseCutoff, out var cutoff)) return;
#if UNITY_IOS && !UNITY_EDITOR
            EOV_CheckLegacyPurchase(gameObject.name, cutoff.ToUnixTimeSeconds());
#elif STEAMWORKS_NET && !UNITY_EDITOR && !UNITY_ANDROID
            if (!SteamManager.Initialized) return;
            var app = Steamworks.SteamUtils.GetAppID();
            var date = Steamworks.SteamApps.GetEarliestPurchaseUnixTime(app);
            var eligible = Steamworks.SteamApps.BIsSubscribed() && !Steamworks.SteamApps.BIsSubscribedFromFreeWeekend() &&
                !Steamworks.SteamApps.BIsSubscribedFromFamilySharing() && date > 0 && date < cutoff.ToUnixTimeSeconds();
            shop.SetOriginalOwner(eligible);
            if (shop.Settings.steamPermanentDlcAppId != 0)
                shop.SetPermanentEntitlement(Steamworks.SteamApps.BIsDlcInstalled(new Steamworks.AppId_t(shop.Settings.steamPermanentDlcAppId)));
#endif
        }
        // Invoked only by the verified native StoreKit result; never from imported save data.
        public void OnLegacyAppleResult(string json)
        {
#if UNITY_IOS && !UNITY_EDITOR
            try { var result = JsonUtility.FromJson<AppleResult>(json); if (result.verified) GetComponent<ShopService>().SetOriginalOwner(result.eligible); }
            catch { }
#endif
        }
    }
}
