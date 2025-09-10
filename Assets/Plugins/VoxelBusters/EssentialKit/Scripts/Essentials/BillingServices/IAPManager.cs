using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.BillingServicesCore
{
    public static class IAPManager
    {
        #region Static fields

        private     static      IBillingProduct[]                       s_cachedProducts;

        private     static      Dictionary<string, IBillingProduct>     s_cachedProductsMap;

        private     static      InternalState                           s_currentState;

        private     static      bool                                    s_isPurchasing;

        private     static      bool                                    s_isRestoring;

        private     static      EventCallback<IBillingProduct[]>        s_getProductsObservers;

        private     static      EventCallback<IBillingTransaction>      s_buyProductObserver;

        private     static      EventCallback<IBillingTransaction[]>    s_restorePurchasesObservers;

        #endregion

        #region Static events

        public static event EventCallback<BillingServicesInitializeStoreResult> OnInitializeStoreComplete
        {
            add { BillingServices.OnInitializeStoreComplete += value; }
            remove { BillingServices.OnInitializeStoreComplete -= value; }
        }

        public static event Callback<BillingServicesTransactionStateChangeResult> OnTransactionStateChange
        {
            add { BillingServices.OnTransactionStateChange += value; }
            remove { BillingServices.OnTransactionStateChange -= value; }
        }

        public static event EventCallback<BillingServicesRestorePurchasesResult> OnRestorePurchasesComplete
        {
            add { BillingServices.OnRestorePurchasesComplete += value; }
            remove { BillingServices.OnRestorePurchasesComplete -= value; }
        }

        #endregion

        #region Constructors

        static IAPManager()
        {
            // set default state
            s_cachedProducts    = null;
            s_cachedProductsMap = new Dictionary<string, IBillingProduct>(capacity: 8);
            s_currentState      = InternalState.NotInitialized;

            // complete other actions
            RegisterForCallbacks();
        }

        #endregion

        #region Public methods

        public static void SetDirty()
        {
            if (s_currentState != InternalState.Initialized)
            {
                return;
            }

            // update state value
            s_currentState      = InternalState.NotInitialized;
        }

        public static void GetBillingProducts(EventCallback<IBillingProduct[]> callback)
        {
            Assert.IsArgNotNull(callback, nameof(callback));

            switch (s_currentState)
            {
                case InternalState.NotInitialized:
                    InitializeStore();
                    s_getProductsObservers += callback;
                    break;

                case InternalState.Initializing:
                    s_getProductsObservers += callback;
                    break;

                case InternalState.Initialized:
                    callback.Invoke(s_cachedProducts, null);
                    break;

                default:
                    throw VBException.SwitchCaseNotImplemented(s_currentState);
            }
        }

        public static void BuyProduct(string productId, EventCallback<IBillingTransaction> callback, string tag = null)
        {
            Assert.IsArgNotNull(callback, nameof(callback));

            // ensure that store is initialized
            var     initializing    = TryInitializeStore((products, error) =>
            {
                if (error == null)
                {
                    BuyProduct(productId, callback, tag);
                }
                else
                {
                    callback.Invoke(null, error);
                }
            });
            if (initializing)
            {
                return;
            }

            // check whether there are any ongoing operations
            if (!IsStoreAvailable())
            {
                callback.Invoke(null, BillingServicesError.StoreIsBusy());
                return;
            }

            // get specified product information
            s_cachedProductsMap.TryGetValue(productId, out IBillingProduct product);
            if (product == null)
            {
                callback.Invoke(null, BillingServicesError.ProductNotAvailable());
                return;
            }

            // initiate purchase request
            s_isPurchasing          = true;
            s_buyProductObserver    = callback;
            BillingServices.BuyProduct(product, new BuyProductOptions.Builder().SetTag(tag).Build());
        }

        public static void RestorePurchases(EventCallback<IBillingTransaction[]> callback, bool forceRefresh, string tag = null)
        {
            Assert.IsArgNotNull(callback, nameof(callback));

            // ensure that store is initialized
            var     initializing    = TryInitializeStore((products, error) =>
            {
                if (error == null)
                {
                    RestorePurchases(callback, forceRefresh, tag);
                }
                else
                {
                    callback.Invoke(null, error);
                }
            });
            if (initializing)
            {
                return;
            }

            // check whether there are any ongoing operations
            if (s_isPurchasing)
            {
                callback.Invoke(null, BillingServicesError.StoreIsBusy());
                return;
            }

            // start restore request
            s_restorePurchasesObservers    += callback;
            if (!s_isRestoring)
            {
                s_isRestoring   = true;
                BillingServices.RestorePurchases(forceRefresh, tag);
            }
        }

        #endregion

        #region Private methods

        private static void RegisterForCallbacks()
        {
            BillingServices.OnInitializeStoreComplete  += HandleOnInitializeStore;
            BillingServices.OnTransactionStateChange   += HandleOnTransactionStateChange;
            BillingServices.OnRestorePurchasesComplete += HandleOnRestorePurchasesComplete;
        }

        private static void InitializeStore()
        {
            s_currentState  = InternalState.Initializing;

            BillingServices.InitializeStore();
        }

        // returns true incase if initialisation is required
        private static bool TryInitializeStore(EventCallback<IBillingProduct[]> callback)
        {
            if (s_currentState != InternalState.Initialized)
            {
                GetBillingProducts(callback);
                return true;
            }

            return false;
        }

        private static bool IsStoreAvailable()
        {
            return !(s_isPurchasing || s_isRestoring);
        }

        #endregion

        #region Event callback methods

        private static void HandleOnInitializeStore(BillingServicesInitializeStoreResult result, Error error)
        {
            // update internal state
            s_cachedProductsMap.Clear();
            s_cachedProducts    = result.Products;
            if (error == null)
            {
                s_currentState  = InternalState.Initialized;
                foreach (var product in result.Products)
                {
                    s_cachedProductsMap[product.Id] = product;
                }
            }
            else
            {
                s_currentState  = InternalState.NotInitialized;
            }

            // forward results to the observer
            if (s_getProductsObservers != null)
            {
                s_getProductsObservers.Invoke(result.Products, error);
                s_getProductsObservers  = null;
            }
        }

        private static void HandleOnTransactionStateChange(BillingServicesTransactionStateChangeResult result)
        {
            // reset state
            s_isPurchasing          = false;

            // send result to the observer objects
            foreach (var transaction in result.Transactions)
            {
                if (transaction.TransactionState == BillingTransactionState.Purchasing)
                {
                    continue;
                }

                s_buyProductObserver.Invoke(transaction, null);
            }
            s_buyProductObserver    = null;
        }

        private static void HandleOnRestorePurchasesComplete(BillingServicesRestorePurchasesResult result, Error error)
        {
            // reset state
            s_isRestoring           = false;

            // send result to the observer objects
            s_restorePurchasesObservers.Invoke(result.Transactions, error);
            s_restorePurchasesObservers = null;
        }

        #endregion

        #region Nested types

        private enum InternalState
        {
            NotInitialized = 0,

            Initializing,

            Initialized
        }

        #endregion
    }
}