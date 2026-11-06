using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Blindsided;

namespace TimelessEchoes.Shop
{
    public sealed class ShopSettings
    {
        public string permanentProductId, baseCurrency, legacyPurchaseCutoff;
        public string cosmeticProductId, cosmeticBaseCurrency;
        public double cosmeticBasePrice;
        public int steamCosmeticItemDefId;
        public string[] supporterProductIds;
        public double permanentBasePrice;
        public double[] supporterBasePrices;
        public int steamPermanentItemDefId;
        public int[] steamSupporterItemDefIds;
        public string[] NonConsumableProductIds => new[] { permanentProductId, cosmeticProductId };
        public bool IsNonConsumable(string id) => !string.IsNullOrEmpty(id) && (id == permanentProductId || id == cosmeticProductId);
        public static ShopSettings Load()
        {
            var asset = Resources.Load<TextAsset>("Shop/settings");
            var settings = JsonConvert.DeserializeObject<ShopSettings>(asset.text);
            Resources.UnloadAsset(asset); return settings;
        }
    }
    [Serializable] public sealed class ShopOwnership
    {
        public bool Permanent, Cosmetics, OriginalOwner, Enabled = true;
        public bool HasCosmetics => Cosmetics || OriginalOwner;
        public string[] Transactions = Array.Empty<string>();
        public int SupporterPurchases;
    }
    public sealed class ShopService : MonoBehaviour
    {
        public static ShopService Instance { get; private set; }
        public static ShopService Ensure()
        {
            if (Instance) return Instance;
            var found = FindAnyObjectByType<ShopService>();
            return found ? found : new GameObject("Vasteria Shop").AddComponent<ShopService>();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void ResetStatics() => Instance = null;
        public event Action Changed;
        public ShopSettings Settings { get; private set; }
        public PromotionClient Promotions { get; private set; }
        public ShopPurchases Purchases { get; private set; }
        public bool Unlocked => Oracle.oracle?.HasLoadedCurrentSlotData == true && ShopRules.IntroComplete(Oracle.oracle.saveData);
        public bool PermanentOwned => ownership.Permanent || ownership.OriginalOwner;
        public bool OriginalOwner => ownership.OriginalOwner;
        public bool CosmeticsOwned => ownership.HasCosmetics;
        public bool Owns(string id) => id == Settings.permanentProductId ? PermanentOwned : id == Settings.cosmeticProductId && CosmeticsOwned;
        public bool PermanentEnabled => PermanentOwned && ownership.Enabled;
        public double Remaining => ShopRules.Remaining(Oracle.oracle?.saveData?.Shop?.GatheringSeconds ?? 0);
        public bool Active => PermanentEnabled || !PermanentOwned && Remaining > 0;
        public bool CanClaim => Unlocked && !PermanentOwned && ShopRules.CanClaim(Remaining);
        public static double ResourceMultiplier => Instance && Instance.Active ? ShopRules.GatheringMultiplier : 1;
        private ShopLedger ledger;
        private ShopOwnership ownership => ledger.Current;
        private bool started, applicationPaused;
        private string OwnershipPath => Path.Combine(Platform.StartupDiagnostics.EditorScreenshotRoot ?? Application.persistentDataPath, Application.isEditor ? "shop-editor-ownership.json" : SteamInventoryPurchases.Supported ? "shop-steam-unverified.json" : "shop-ownership.json");
        private string steamAccount;
        public void SelectSteamAccount(string account)
        {
            if (steamAccount == account) return;
            if (!ulong.TryParse(account, out var id) || id == 0) throw new ArgumentException("Invalid Steam account.");
            ledger = new ShopLedger(Path.Combine(Application.persistentDataPath, "shop-steam-" + account + ".json"));
            steamAccount = account; Changed?.Invoke();
        }
        public bool ApplySteamInventory(bool permanent, int supporters, bool cosmetics = false)
        {
            if (steamAccount == null || !ledger.ApplySteamInventory(permanent, supporters, cosmetics)) return false;
            Changed?.Invoke(); return true;
        }
        private void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            Settings = ShopSettings.Load();
            ledger = new ShopLedger(OwnershipPath);
            Promotions = gameObject.AddComponent<PromotionClient>();
            Purchases = gameObject.AddComponent<ShopPurchases>(); Purchases.Initialize(this);
            gameObject.AddComponent<LegacyShopOwner>();
        }
        private void Update()
        {
            if (Platform.StartupDiagnostics.EditorScreenshotSession) return;
            if (Unlocked && !started) { started = true; Purchases.Connect(); GetComponent<LegacyShopOwner>()?.Check(); }
            var gm = GameManager.Instance;
            if (applicationPaused || Time.timeScale <= 0 || !gm || gm.IsInTown || gm.RunLoadingActive || !gm.CurrentMap || Oracle.oracle?.HasLoadedCurrentSlotData != true) return;
            var progress = Oracle.oracle.saveData.Shop;
            if (progress != null && progress.GatheringSeconds > 0 && !PermanentOwned)
                progress.GatheringSeconds = Math.Max(0, ShopRules.Remaining(progress.GatheringSeconds) - Time.unscaledDeltaTime);
        }
        private void OnApplicationPause(bool paused) => applicationPaused = paused;
        public bool Claim()
        {
            if (!CanClaim) return false;
            var progress = Oracle.oracle.saveData.Shop ??= new();
            progress.GatheringSeconds = ShopRules.Remaining(Remaining + ShopRules.ClaimSeconds);
            Blindsided.EventHandler.SaveData(); Changed?.Invoke(); return true;
        }
        public void TogglePermanent()
        {
            if (!PermanentOwned) return;
            if (ledger.Update(next => next.Enabled = !next.Enabled)) Changed?.Invoke();
        }
        public bool Fulfill(string productId, string transactionId)
        {
            if (!ledger.Fulfill(Settings, productId, transactionId)) return false;
            Changed?.Invoke(); return true;
        }
        public bool SetPermanentEntitlement(bool owned)
            => SetPurchasedEntitlement(Settings.permanentProductId, owned);

        public bool SetPurchasedEntitlement(string id, bool owned)
        {
            if (!Settings.IsNonConsumable(id)) return false;
            var cosmetics = id == Settings.cosmeticProductId;
            if ((cosmetics ? ownership.Cosmetics : ownership.Permanent) == owned) return true;
            var result = ledger.Update(next =>
            {
                if (cosmetics) next.Cosmetics = owned;
                else next.Permanent = owned;
            });
            if (result) Changed?.Invoke(); return result;
        }
        public void GrantLegacyAndroidInstall(long firstInstallMilliseconds, DateTimeOffset cutoff)
        {
            var owned = ownership.OriginalOwner;
            if (ledger.GrantLegacyAndroidInstall(firstInstallMilliseconds, cutoff) && !owned) Changed?.Invoke();
        }
        public void SetOriginalOwner(bool eligible)
        {
            if (ownership.OriginalOwner == eligible) return;
            if (ledger.Update(next => next.OriginalOwner = eligible)) Changed?.Invoke();
        }
    }
}
