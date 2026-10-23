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
#elif UNITY_ANDROID && !UNITY_EDITOR
            // Android preserves firstInstallTime across updates. Once granted,
            // ownership remains in the installation ledger even if a later check fails.
            if (shop.OriginalOwner) return;
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                using var manager = activity.Call<AndroidJavaObject>("getPackageManager");
                var package = activity.Call<string>("getPackageName");
                using var info = manager.Call<AndroidJavaObject>("getPackageInfo", package, 0);
                shop.GrantLegacyAndroidInstall(info.Get<long>("firstInstallTime"), cutoff);
            }
            catch (AndroidJavaException exception)
            {
                Debug.LogWarning("Could not check the Android installation date: " + exception.Message);
            }
#elif STEAMWORKS_NET && UNITY_STANDALONE && !UNITY_EDITOR
            if (!SteamManager.Initialized) return;
            shop.SelectSteamAccount(Steamworks.SteamUser.GetSteamID().ToString());
            var app = Steamworks.SteamUtils.GetAppID();
            var date = Steamworks.SteamApps.GetEarliestPurchaseUnixTime(app);
            var eligible = Steamworks.SteamApps.BIsSubscribed() && !Steamworks.SteamApps.BIsSubscribedFromFreeWeekend() &&
                !Steamworks.SteamApps.BIsSubscribedFromFamilySharing() && date > 0 && date < cutoff.ToUnixTimeSeconds();
            shop.SetOriginalOwner(eligible);
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
