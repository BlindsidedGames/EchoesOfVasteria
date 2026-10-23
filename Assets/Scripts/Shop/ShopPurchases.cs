using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using TimelessEchoes.UI.Toolkit;

namespace TimelessEchoes.Shop
{
    public sealed class ShopPurchases : MonoBehaviour
    {
        public event Action Changed;
        private string statusKey, statusEnglish = "";
        public string Status => steam ? steam.Status : string.IsNullOrEmpty(statusEnglish) ? "" : ToolkitLocalization.Text(statusKey, statusEnglish);
        private void SetStatus(string key, string english) { statusKey = "shop.status." + key; statusEnglish = english; }
        private bool busy;
        public bool Busy { get => steam ? steam.Busy : busy; private set => busy = value; }
        public bool IsTestStore => Application.isEditor;
        public bool Supported => Application.isEditor || Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android || SteamInventoryPurchases.Supported;
        private SteamInventoryPurchases steam;
        private ShopService shop;
        private StoreController store;
        private bool connecting, connected;
        private readonly Dictionary<string, Product> products = new();
        public void Initialize(ShopService owner)
        {
            shop = owner;
            if (!SteamInventoryPurchases.Supported) return;
            steam = gameObject.AddComponent<SteamInventoryPurchases>();
            steam.Initialize(owner); steam.Changed += SteamChanged;
        }
        private void SteamChanged() => Changed?.Invoke();
        public string Price(string id)
        {
            if (steam) return steam.Price(id);
            if (!products.TryGetValue(id, out var product)) return null;
            if (!IsTestStore) return product.metadata.localizedPriceString;
            var index = Array.IndexOf(shop.Settings.supporterProductIds, id);
            var price = id == shop.Settings.permanentProductId ? shop.Settings.permanentBasePrice
                : index >= 0 ? shop.Settings.supporterBasePrices[index] : 0;
            return "A$" + price.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }
        public bool Available(string id) => steam ? steam.Available(id) : connected && !Busy && products.TryGetValue(id, out var product) && product.availableToPurchase;
        public async void Connect()
        {
            if (steam) { steam.Connect(); return; }
            if (connecting || connected || !Supported) return;
            connecting = true; SetStatus("connecting", "Connecting to store…"); Changed?.Invoke();
            try
            {
                store ??= UnityIAPServices.StoreController();
                store.OnProductsFetched += ProductsFetched;
                store.OnProductsFetchFailed += ProductsFailed;
                store.OnPurchasePending += Pending;
                store.OnPurchaseConfirmed += Confirmed;
                store.OnPurchaseFailed += Failed;
                store.OnPurchaseDeferred += Deferred;
                store.OnPurchasesFetched += Restored;
                store.OnPurchasesFetchFailed += RestoreFailed;
                store.OnCheckEntitlement += EntitlementChecked;
                store.OnStoreDisconnected += Disconnected;
                store.ProcessPendingOrdersOnPurchasesFetched(true);
                var controller = store;
                await controller.Connect();
                if (!this || store != controller) return;
                var definitions = new List<ProductDefinition> { new(shop.Settings.permanentProductId, ProductType.NonConsumable) };
                definitions.AddRange(shop.Settings.supporterProductIds.Select(id => new ProductDefinition(id, ProductType.Consumable)));
                store.FetchProductsWithNoRetries(definitions);
            }
            catch { SetStatus("unavailable", "Store unavailable. Try again later."); connecting = false; Detach(); Changed?.Invoke(); }
        }
        private void ProductsFetched(List<Product> fetched)
        {
            products.Clear(); foreach (var product in fetched) products[product.definition.id] = product;
            connecting = false; connected = true; SetStatus(IsTestStore ? "editor" : "none", IsTestStore ? "Editor test store — no charges" : "");
            store.FetchPurchases(); Changed?.Invoke();
        }
        private void ProductsFailed(ProductFetchFailed _) { connecting = false; SetStatus("products-unavailable", "Products unavailable. Try again later."); Detach(); Changed?.Invoke(); }
        private void Disconnected(StoreConnectionFailureDescription _) { connecting = connected = Busy = false; SetStatus("disconnected", "Store disconnected."); Detach(); Changed?.Invoke(); }
        public void Buy(string id)
        {
            if (steam) { steam.Buy(id); return; }
            if (!Available(id) || id == shop.Settings.permanentProductId && shop.PermanentOwned) return;
            Busy = true; SetStatus("waiting", "Waiting for purchase…"); Changed?.Invoke();
            try { store.PurchaseProduct(products[id]); } catch { Busy = false; SetStatus("could-not-start", "Purchase could not start."); Changed?.Invoke(); }
        }
        private void Pending(PendingOrder order)
        {
            var success = true;
            foreach (var item in order.CartOrdered.Items()) success &= shop.Fulfill(item.Product.definition.id, order.Info.TransactionID);
            if (!success) { Busy = false; SetStatus("storage-failed", "Purchase received. Storage could not be updated; restore to retry."); Changed?.Invoke(); return; }
            // Persist the entitlement before acknowledging. Store replay is idempotent.
            Busy = false; SetStatus("thanks", "Thank you for your support!");
            try { store.ConfirmPurchase(order); }
            catch { SetStatus("confirmation-failed", "Purchase saved. Store confirmation will retry on restore."); }
            Changed?.Invoke();
        }
        private void Confirmed(Order order)
        {
            Busy = false;
            if (order is FailedOrder) SetStatus("confirmation-failed", "Purchase saved. Store confirmation will retry on restore.");
            Changed?.Invoke();
        }
        private void Failed(FailedOrder _) { Busy = false; SetStatus("unsuccessful", "Purchase cancelled or unsuccessful. Nothing was unlocked."); Changed?.Invoke(); }
        private void Deferred(DeferredOrder _) { Busy = false; SetStatus("deferred", "Purchase awaiting approval."); Changed?.Invoke(); }
        private void Restored(Orders orders)
        {
            foreach (var order in orders.ConfirmedOrders)
                if (order.CartOrdered.Items().Any(i => i.Product.definition.id == shop.Settings.permanentProductId)) shop.SetPermanentEntitlement(true);
            if (products.TryGetValue(shop.Settings.permanentProductId, out var product)) store.CheckEntitlement(product);
            Busy = false; SetStatus(IsTestStore ? "editor" : "restored", IsTestStore ? "Editor test store — no charges" : "Purchases restored."); Changed?.Invoke();
        }
        private void EntitlementChecked(Entitlement entitlement)
        {
            if (entitlement.Product?.definition.id != shop.Settings.permanentProductId) return;
            if (entitlement.Status == EntitlementStatus.FullyEntitled || entitlement.Status == EntitlementStatus.EntitledButNotFinished)
                shop.SetPermanentEntitlement(true);
            else if (entitlement.Status == EntitlementStatus.NotEntitled) shop.SetPermanentEntitlement(false);
            Changed?.Invoke();
        }
        private void RestoreFailed(PurchasesFetchFailureDescription _) { Busy = false; SetStatus("restore-failed", "Restore unavailable. Try again later."); Changed?.Invoke(); }
        public void Restore()
        {
            if (steam) { steam.Restore(); return; }
            if (Busy) return;
            if (!connected) { Connect(); return; }
            Busy = true; SetStatus("restoring", "Restoring purchases…"); Changed?.Invoke();
            store.RestoreTransactions((success, _) =>
            {
                if (success) { store.FetchPurchases(); GetComponent<LegacyShopOwner>()?.Check(); }
                else { Busy = false; SetStatus("restore-failed", "Restore unavailable. Try again later."); Changed?.Invoke(); }
            });
        }
        private void Detach()
        {
            if (store == null) return;
            store.OnProductsFetched -= ProductsFetched; store.OnProductsFetchFailed -= ProductsFailed;
            store.OnPurchasePending -= Pending; store.OnPurchaseConfirmed -= Confirmed; store.OnPurchaseFailed -= Failed; store.OnPurchaseDeferred -= Deferred;
            store.OnPurchasesFetched -= Restored; store.OnPurchasesFetchFailed -= RestoreFailed;
            store.OnCheckEntitlement -= EntitlementChecked; store.OnStoreDisconnected -= Disconnected; store = null;
        }
        private void OnDestroy() { if (steam) steam.Changed -= SteamChanged; Detach(); }
    }
}
