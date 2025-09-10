using System;
using System.Collections;
using System.Collections.Generic;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.Editor.NativePlugins;
using UnityEditor;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Simulator
{
    public sealed class BillingStoreSimulator : SingletonObject<BillingStoreSimulator>
    {
        #region Fields
        private                 BillingProductData[]        m_products;

        private     readonly    BillingStoreSimulatorData   m_simulatorData     = null;

        private                 bool                        m_isPurchasing      = false;
        
        #endregion

        #region Constructors

        private BillingStoreSimulator()
        {
            // set properties
            m_products          = null;
            m_simulatorData     = LoadFromDisk() ?? new BillingStoreSimulatorData();
        }

        #endregion

        #region Create methods

        private static BillingProductData CreateProduct(BillingProductDefinition productDefinition)
        {
            return new BillingProductData(productDefinition.Id, productDefinition.Title, productDefinition.Description, 0.99, "USD", "$", "$0.99", productDefinition.ProductType);
        }

        private static BillingTransactionData CreateTransaction(string productId, int quantity, string tag, BillingTransactionState transactionState, BillingProductType productType, Error error = null)
        {
            return new BillingTransactionData(productId, quantity, tag, "transactionId", DateTime.Now, transactionState, productType, quantity, "originalTransactionId", DateTime.Now, PlayerSettings.applicationIdentifier, BillingEnvironment.Local, null, null, null, null, error);
        }

        #endregion

        #region Public methods

        public void GetProducts(BillingProductDefinition[] productDefinitions, Action<BillingProductData[], Error> callback)
        {
            BillingProductData[]    storeProducts   = null;
            Error                   error           = null;

            // check input
            if (productDefinitions.Length == 0)
            {
                error           = new Error(description: "The operation could not be completed because product list is empty.");
            }
            else
            {
                // create product info
                storeProducts   = Array.ConvertAll(productDefinitions, (item) => CreateProduct(item));
            }

            // update cache
            m_products          = storeProducts;

            // send result
            callback(storeProducts, error);
        }

        public bool IsProductPurchased(string productId)
        {
            return m_simulatorData.IsProductPurchased(productId);
        }

        public bool BuyProduct(string productId, string productPlatformId, int quantity, string tag, Action<BillingTransactionData> callback)
        {
            // check input
            var     product     = FindProductWithId(productId);
            if (product == null)
            {
                callback(CreateTransaction(productId, quantity, tag, BillingTransactionState.Failed, product.ProductType, BillingServicesError.ProductNotAvailable()));
                return false;
            }
            else if(string.IsNullOrEmpty(productPlatformId))
            {
                var description = $"You must specify the platform id (or its overrides) for the current active build platform in the settings for billing product with id: {productId}.";
                DebugLogger.LogError(description);                
                callback(CreateTransaction(productId, quantity, tag, BillingTransactionState.Failed, product.ProductType, BillingServicesError.ProductNotAvailable()));
                return false;
            }

            // check whether item is purchased
            if (m_simulatorData.IsProductPurchased(product.Id) && (product.ProductType == BillingProductType.NonConsumable || product.ProductType == BillingProductType.Subscription))
            {
                callback(CreateTransaction(productId, quantity, tag, BillingTransactionState.Failed, product.ProductType, BillingServicesError.ProductOwned()));
                return true;
            }

            if(m_isPurchasing)
            {
                callback(CreateTransaction(productId, quantity, tag, BillingTransactionState.Failed, product.ProductType, BillingServicesError.StoreIsBusy()));
                return false;
            }

            m_isPurchasing = true;

            // confirm with user
            string  message         = string.Format("Do you want to buy {0} for {1}?", product.LocalizedTitle, product.Price);
            var     newAlertDialog  = new AlertDialogBuilder()
                .SetTitle("Confirm your purchase")
                .SetMessage(message)
                .AddButton("Ok", () => 
                {
                    m_isPurchasing = false;
                    // update purchase data
                    if (product.ProductType == BillingProductType.NonConsumable || product.ProductType == BillingProductType.Subscription)
                    {
                        m_simulatorData.AddProductToPurchaseHistory(product.Id);
                        SaveData();
                    }

                    // send result
                    callback(CreateTransaction(productId, quantity, tag, BillingTransactionState.Purchased, product.ProductType));
                })
                .AddCancelButton("Cancel", () => 
                {
                    m_isPurchasing = false;
                    // send result
                    callback(CreateTransaction(productId, quantity, tag, BillingTransactionState.Failed, product.ProductType, BillingServicesError.UserCancelled()));
                })
                .Build();
            newAlertDialog.Show();

            return true;
        }

        public void RestorePurchases(bool forceRefresh, string tag, Action<BillingTransactionData[], Error> callback)
        {
            if (m_products == null)
            {
                callback(null, BillingServicesError.StoreNotInitialized());
                return;
            }

            var     purchasedIds        = m_simulatorData.GetPurchasedProductIds();
            var     transactions        = new List<BillingTransactionData>();

            for (int iter = 0; iter < purchasedIds.Length; iter++)
            {
                string  productId       = purchasedIds[iter];
                var     product         = FindProductWithId(productId);
                if (product != null)
                {
                    var     transaction = CreateTransaction(productId, 1, tag, BillingTransactionState.Purchased, product.ProductType);
                    transactions.Add(transaction);
                }
            }
            callback(transactions.ToArray(), null);
        }

        #endregion

        #region Database methods

        private BillingStoreSimulatorData LoadFromDisk()
        {
            return SimulatorServices.GetObject<BillingStoreSimulatorData>(NativeFeatureType.kBillingServices);
        }

        private void SaveData()
        {
            SimulatorServices.SetObject(NativeFeatureType.kBillingServices, m_simulatorData);
        }

        public static void Reset() 
        {
            SimulatorServices.RemoveObject(NativeFeatureType.kBillingServices);
        }

        #endregion

        #region Private methods

        private BillingProductData FindProductWithId(string id)
        {
            return Array.Find(m_products, (item) => item.Id == id);
        }

        #endregion
    }
}