using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

namespace TimelessEchoes.Shop
{
    public sealed class ShopPurchases : MonoBehaviour
    {
        public event Action Changed;
        public string Status { get; private set; } = "";
        public bool Busy { get; private set; }
        public bool IsTestStore => Application.isEditor;
        public bool Supported => Application.isEditor || Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android;
        private ShopService shop;
        private StoreController store;
        private bool connecting, connected;
        private readonly Dictionary<string, Product> products = new();
        public void Initialize(ShopService owner) => shop = owner;
        public string Price(string id)
        {
            if (!products.TryGetValue(id, out var product)) return null;
            if (!IsTestStore) return product.metadata.localizedPriceString;
            var index = Array.IndexOf(shop.Settings.supporterProductIds, id);
            var price = id == shop.Settings.permanentProductId ? shop.Settings.permanentBasePrice
                : index >= 0 ? shop.Settings.supporterBasePrices[index] : 0;
            return "A$" + price.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }
        public bool Available(string id) => connected && !Busy && products.TryGetValue(id, out var product) && product.availableToPurchase;
        public async void Connect()
        {
            if (connecting || connected || !Supported) return;
            connecting = true; Status = "Connecting to store…"; Changed?.Invoke();
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
                await store.Connect();
                var definitions = new List<ProductDefinition> { new(shop.Settings.permanentProductId, ProductType.NonConsumable) };
                definitions.AddRange(shop.Settings.supporterProductIds.Select(id => new ProductDefinition(id, ProductType.Consumable)));
                store.FetchProductsWithNoRetries(definitions);
            }
            catch { Status = "Store unavailable. Try again later."; connecting = false; Detach(); Changed?.Invoke(); }
        }
        private void ProductsFetched(List<Product> fetched)
        {
            products.Clear(); foreach (var product in fetched) products[product.definition.id] = product;
            connecting = false; connected = true; Status = IsTestStore ? "Editor test store — no charges" : "";
            store.FetchPurchases(); Changed?.Invoke();
        }
        private void ProductsFailed(ProductFetchFailed _) { connecting = false; Status = "Products unavailable. Try again later."; Detach(); Changed?.Invoke(); }
        private void Disconnected(StoreConnectionFailureDescription _) { connecting = connected = Busy = false; Status = "Store disconnected."; Detach(); Changed?.Invoke(); }
        public void Buy(string id)
        {
            if (!Available(id) || id == shop.Settings.permanentProductId && shop.PermanentOwned) return;
            Busy = true; Status = "Waiting for purchase…"; Changed?.Invoke();
            try { store.PurchaseProduct(products[id]); } catch { Busy = false; Status = "Purchase could not start."; Changed?.Invoke(); }
        }
        private void Pending(PendingOrder order)
        {
            var success = true;
            foreach (var item in order.CartOrdered.Items()) success &= shop.Fulfill(item.Product.definition.id, order.Info.TransactionID);
            if (!success) { Busy = false; Status = "Purchase received. Storage could not be updated; restore to retry."; Changed?.Invoke(); return; }
            // Persist the entitlement before acknowledging. Store replay is idempotent.
            store.ConfirmPurchase(order);
            Busy = false; Status = "Thank you for your support!"; Changed?.Invoke();
        }
        private void Confirmed(Order order)
        {
            Busy = false;
            if (order is FailedOrder) Status = "Purchase saved. Store confirmation will retry on restore.";
            Changed?.Invoke();
        }
        private void Failed(FailedOrder _) { Busy = false; Status = "Purchase cancelled or unsuccessful. Nothing was unlocked."; Changed?.Invoke(); }
        private void Deferred(DeferredOrder _) { Busy = false; Status = "Purchase awaiting approval."; Changed?.Invoke(); }
        private void Restored(Orders orders)
        {
            foreach (var order in orders.ConfirmedOrders)
                if (order.CartOrdered.Items().Any(i => i.Product.definition.id == shop.Settings.permanentProductId)) shop.SetPermanentEntitlement(true);
            if (products.TryGetValue(shop.Settings.permanentProductId, out var product)) store.CheckEntitlement(product);
            Busy = false; Status = IsTestStore ? "Editor test store — no charges" : "Purchases restored."; Changed?.Invoke();
        }
        private void EntitlementChecked(Entitlement entitlement)
        {
            if (entitlement.Product?.definition.id != shop.Settings.permanentProductId) return;
            if (entitlement.Status == EntitlementStatus.FullyEntitled || entitlement.Status == EntitlementStatus.EntitledButNotFinished)
                shop.SetPermanentEntitlement(true);
            else if (entitlement.Status == EntitlementStatus.NotEntitled) shop.SetPermanentEntitlement(false);
            Changed?.Invoke();
        }
        private void RestoreFailed(PurchasesFetchFailureDescription _) { Busy = false; Status = "Restore unavailable. Try again later."; Changed?.Invoke(); }
        public void Restore()
        {
            if (Busy) return;
            if (!connected) { Connect(); return; }
            Busy = true; Status = "Restoring purchases…"; Changed?.Invoke();
            store.RestoreTransactions((success, _) =>
            {
                if (success) { store.FetchPurchases(); GetComponent<LegacyShopOwner>()?.Check(); }
                else { Busy = false; Status = "Restore unavailable. Try again later."; Changed?.Invoke(); }
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
        private void OnDestroy() => Detach();
    }
}
