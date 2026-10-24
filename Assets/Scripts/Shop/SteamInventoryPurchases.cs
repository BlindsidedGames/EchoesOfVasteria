using System;
using UnityEngine;
#if STEAMWORKS_NET && UNITY_STANDALONE
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Steamworks;
using TimelessEchoes.UI.Toolkit;
#endif

namespace TimelessEchoes.Shop
{
    // Steam Inventory owns checkout, delivery and restores; items are never consumed.
    public sealed class SteamInventoryPurchases : MonoBehaviour
    {
        public static bool Supported
        {
            get
            {
#if STEAMWORKS_NET && UNITY_STANDALONE
                return !Application.isEditor;
#else
                return false;
#endif
            }
        }
        public event Action Changed;
        public bool Busy { get; private set; }
#if STEAMWORKS_NET && UNITY_STANDALONE
        public string Status => string.IsNullOrEmpty(statusEnglish) ? "" : ToolkitLocalization.Text("shop.status." + statusKey, statusEnglish);
        private ShopService shop;
        private string statusKey, statusEnglish = "";
        private readonly Dictionary<string, string> prices = new();
        private readonly Dictionary<SteamInventoryResult_t, TaskCompletionSource<EResult>> queries = new();
        private readonly CancellationTokenSource lifetime = new();
        private Callback<SteamInventoryResultReady_t> delivered;
        private Callback<MicroTxnAuthorizationResponse_t> authorization;
        private readonly HashSet<ulong> declinedOrders = new();
        private bool dirty;
        private bool connected;
        private CSteamID account;
        private bool Connected => connected && SteamManager.Initialized && SteamUser.BLoggedOn() && SteamUser.GetSteamID() == account;
        private bool CatalogLoaded => prices.ContainsKey(shop.Settings.permanentProductId) && Array.TrueForAll(shop.Settings.supporterProductIds, prices.ContainsKey);
        private void SetStatus(string key, string english) { statusKey = key; statusEnglish = english; Changed?.Invoke(); }
        public void Initialize(ShopService owner) => shop = owner;
        public string Price(string id) => prices.TryGetValue(id, out var price) ? price : null;
        public bool Available(string id) => !Busy && Connected && prices.ContainsKey(id) && SteamUtils.IsOverlayEnabled();
        private int Definition(string id)
        {
            if (id == shop.Settings.permanentProductId) return shop.Settings.steamPermanentItemDefId;
            var index = Array.IndexOf(shop.Settings.supporterProductIds, id);
            return index >= 0 && index < shop.Settings.steamSupporterItemDefIds.Length ? shop.Settings.steamSupporterItemDefIds[index] : 0;
        }
        private void Identify()
        {
            if (!SteamManager.Initialized) throw new InvalidOperationException("Steam unavailable.");
            var current = SteamUser.GetSteamID();
            if (account != current)
            {
                connected = false;
                prices.Clear();
                declinedOrders.Clear();
            }
            account = current;
            shop.SelectSteamAccount(account.ToString());
            delivered ??= Callback<SteamInventoryResultReady_t>.Create(result =>
            {
                if (queries.TryGetValue(result.m_handle, out var pending))
                {
                    pending.TrySetResult(result.m_result); return;
                }
                // Purchase delivery is a delta, not a complete ownership snapshot.
                if (result.m_result == EResult.k_EResultOK) dirty = true;
                SteamInventory.DestroyResult(result.m_handle);
            });
            authorization ??= Callback<MicroTxnAuthorizationResponse_t>.Create(result =>
            {
                if (result.m_unAppID == SteamUtils.GetAppID().m_AppId && result.m_bAuthorized == 0)
                    declinedOrders.Add(result.m_ulOrderID);
            });
        }
        private void VerifyAccount(CSteamID expected)
        {
            lifetime.Token.ThrowIfCancellationRequested();
            if (!SteamManager.Initialized || SteamUser.GetSteamID() != expected) throw new InvalidOperationException("Steam account changed.");
        }
        private async Task<T> Call<T>(SteamAPICall_t handle, CSteamID expected)
        {
            if (handle == SteamAPICall_t.Invalid) throw new InvalidOperationException("Steam request unavailable.");
            var completion = new TaskCompletionSource<T>();
            using var result = CallResult<T>.Create((value, failed) =>
            {
                if (failed) completion.TrySetException(new InvalidOperationException("Steam request failed."));
                else completion.TrySetResult(value);
            });
            result.Set(handle);
            if (await Task.WhenAny(completion.Task, Task.Delay(30000, lifetime.Token)) != completion.Task)
                throw new TimeoutException("Steam request timed out.");
            VerifyAccount(expected);
            return await completion.Task;
        }
        private async Task<Dictionary<int, int>> Inventory(CSteamID expected)
        {
            VerifyAccount(expected);
            if (!SteamInventory.GetAllItems(out var handle)) throw new InvalidOperationException("Inventory unavailable.");
            var completion = new TaskCompletionSource<EResult>();
            queries.Add(handle, completion);
            try
            {
                if (await Task.WhenAny(completion.Task, Task.Delay(30000, lifetime.Token)) != completion.Task)
                    throw new TimeoutException("Inventory timed out.");
                VerifyAccount(expected);
                if (await completion.Task != EResult.k_EResultOK || !SteamInventory.CheckResultSteamID(handle, expected))
                    throw new InvalidOperationException("Inventory could not be verified.");
                uint count = 0;
                if (!SteamInventory.GetResultItems(handle, null, ref count) || count > 100000) throw new InvalidOperationException("Invalid inventory.");
                var items = new SteamItemDetails_t[count];
                if (!SteamInventory.GetResultItems(handle, items, ref count)) throw new InvalidOperationException("Inventory unavailable.");
                var totals = new Dictionary<int, int>();
                foreach (var item in items)
                {
                    if ((item.m_unFlags & (int)ESteamItemFlags.k_ESteamItemRemoved) != 0 || item.m_unQuantity == 0) continue;
                    var definition = (int)item.m_iDefinition;
                    totals.TryGetValue(definition, out var quantity);
                    totals[definition] = checked(quantity + item.m_unQuantity);
                }
                var supporters = 0;
                foreach (var definition in shop.Settings.steamSupporterItemDefIds)
                    supporters = checked(supporters + Quantity(totals, definition));
                if (!shop.ApplySteamInventory(Quantity(totals, shop.Settings.steamPermanentItemDefId) > 0, supporters))
                    throw new System.IO.IOException("Ownership could not be saved. Restore to retry.");
                return totals;
            }
            finally
            {
                if (queries.Remove(handle) && !stopping && SteamManager.Initialized) SteamInventory.DestroyResult(handle);
            }
        }
        private static int Quantity(Dictionary<int, int> items, int definition) => items.TryGetValue(definition, out var count) ? count : 0;
        public async void Connect()
        {
            if (Busy || Connected && CatalogLoaded) return;
            Busy = true; SetStatus("connecting", "Connecting to store…");
            try
            {
                Identify(); var expected = account;
                await Inventory(expected);
                GetComponent<LegacyShopOwner>()?.Check();
                SteamInventory.LoadItemDefinitions();
                var result = await Call<SteamInventoryRequestPricesResult_t>(SteamInventory.RequestPrices(), expected);
                if (result.m_result != EResult.k_EResultOK) throw new InvalidOperationException("Prices unavailable.");
                prices.Clear();
                var ids = new List<string>(shop.Settings.supporterProductIds) { shop.Settings.permanentProductId };
                foreach (var id in ids)
                    if (Definition(id) > 0 && SteamInventory.GetItemPrice(new SteamItemDef_t(Definition(id)), out var price, out _))
                        prices[id] = FormatPrice(price, result.m_rgchCurrency);
                connected = true;
                if (SteamUtils.IsOverlayEnabled()) SetStatus("none", "");
                else SetStatus("unavailable", "Store unavailable. Try again later.");
            }
            catch (OperationCanceledException) { }
            catch { if (this) { connected = false; prices.Clear(); SetStatus("unavailable", "Store unavailable. Try again later."); } }
            finally { Busy = false; if (this) Changed?.Invoke(); }
        }
        // Steam prices are hundredths in every currency, including JPY and KWD.
        public static string FormatPrice(ulong amount, string currency)
        {
            if (currency == null || currency.Length != 3) throw new ArgumentException("Invalid currency.");
            foreach (var character in currency) if (character < 'A' || character > 'Z') throw new ArgumentException("Invalid currency.");
            return currency + " " + (amount / 100m).ToString("0.##", Blindsided.Utilities.LocaleNumbers.Culture);
        }
        public async void Buy(string id)
        {
            if (!Available(id) || Definition(id) <= 0) return;
            Busy = true; SetStatus("waiting", "Waiting for purchase…");
            try
            {
                Identify(); var expected = account; var definition = Definition(id);
                var before = Quantity(await Inventory(expected), definition);
                if (id == shop.Settings.permanentProductId && shop.PermanentOwned) return;
                var result = await Call<SteamInventoryStartPurchaseResult_t>(SteamInventory.StartPurchase(new[] { new SteamItemDef_t(definition) }, new uint[] { 1 }, 1), expected);
                if (result.m_result != EResult.k_EResultOK) throw new InvalidOperationException("Purchase unsuccessful.");
                var deadline = Time.realtimeSinceStartup + 120;
                do
                {
                    await Task.Delay(2000, lifetime.Token);
                    VerifyAccount(expected);
                    if (declinedOrders.Remove(result.m_ulOrderID))
                    {
                        SetStatus("unsuccessful", "Purchase cancelled or unsuccessful. Nothing was unlocked."); return;
                    }
                    if (Quantity(await Inventory(expected), definition) > before)
                    {
                        SetStatus("thanks", "Thank you for your support!"); return;
                    }
                } while (Time.realtimeSinceStartup < deadline);
                // StartPurchase success only opens checkout. Delivery remains authoritative.
                SetStatus("waiting", "Waiting for purchase…");
            }
            catch (OperationCanceledException) { }
            catch (System.IO.IOException) { if (this) SetStatus("storage-failed", "Purchase received. Storage could not be updated; restore to retry."); }
            catch { if (this) SetStatus("unsuccessful", "Purchase cancelled or unsuccessful. Nothing was unlocked."); }
            finally { Busy = false; if (this) Changed?.Invoke(); }
        }
        public void Restore() => RefreshOwnership(true);
        private async void RefreshOwnership(bool userInitiated)
        {
            if (Busy) return;
            Busy = true;
            if (userInitiated) SetStatus("restoring", "Restoring purchases…");
            try
            {
                Identify(); dirty = false;
                await Inventory(account);
                GetComponent<LegacyShopOwner>()?.Check();
                if (userInitiated) SetStatus("restored", "Purchases restored.");
            }
            catch (OperationCanceledException) { }
            catch (Exception error)
            {
                if (this)
                {
                    if (userInitiated) SetStatus("restore-failed", "Restore unavailable. Try again later.");
                    else Debug.LogWarning("Steam ownership sync failed: " + error.Message);
                }
            }
            finally { Busy = false; if (this) Changed?.Invoke(); }
        }
        private void Update() { if (dirty && !Busy) RefreshOwnership(false); }
        private void OnApplicationFocus(bool focused)
        {
            if (!focused || !shop || !shop.Unlocked) return;
            if (Connected && CatalogLoaded) dirty = true;
            else Connect();
        }
        private bool stopping;
        private bool quitting;
        private void OnApplicationQuit() => quitting = true;
        private void OnDisable()
        {
            stopping = true;
            lifetime.Cancel(); delivered?.Dispose(); authorization?.Dispose();
            if (quitting || !SteamManager.Initialized) return;
            foreach (var handle in queries.Keys) SteamInventory.DestroyResult(handle);
            queries.Clear();
        }
        private void OnDestroy() => lifetime.Dispose();
#else
        public string Status => "";
        public void Initialize(ShopService owner) { }
        public string Price(string id) => null;
        public bool Available(string id) => false;
        public void Connect() { }
        public void Buy(string id) { }
        public void Restore() { }
#endif
    }
}
