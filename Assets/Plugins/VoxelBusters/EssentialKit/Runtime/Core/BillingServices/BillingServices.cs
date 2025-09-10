using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.EssentialKit.BillingServicesCore;

namespace VoxelBusters.EssentialKit
{
    /**
     * @defgroup BillingServices BillingServices
     * @brief Provides cross-platform interface to request payment from a user for additional functionality or content that your application delivers.    
     */

    /// <summary>
    /// Provides cross-platform interface to request payment from a user for additional functionality or content that your application delivers.    
    /// </summary>
    /// <description>
    /// <para>
    /// This feature connects to the Store on your app’s behalf to securely process payments from users, prompting them to authorize payment. 
    /// The feature then notifies your app, which provides the purchased items to users. 
    /// For processing requests, feature contacts App Store, Google Play Store on iOS, Android platform respectively.
    /// You need to configure iOS billing product details at <a href="https://developer.apple.com/help/app-store-connect/configure-in-app-purchase-settings/overview-for-configuring-in-app-purchases">iTunes Connect</a>. 
    /// Similarly for Android, you can set these details at <a href="https://developer.android.com/google/play/billing">Google Play Developer Console</a>.
    /// </para>
    /// <para>
    /// The interaction between the user, your app, and the Store during the purchase process take place in three stages.
    /// First, the your app displays purchasable products received from the Store. 
    /// Second, the user selects a product to buy and the app requests payment from the Store. 
    /// Third, the Store processes the payment and your app delivers the purchased product.
    /// </para>
    /// <para>
    /// Optionally, you can choose to verify receipts of completed transactions. The receipt is a record of purchase made from within the application and enabling receipt validation, adds one more level security to avoid unauthorised purchases.</para>
    /// <para>
    /// Users can also restore products that were previously purchased. As per iOS guidelines, if your application supports product types that are restorable, you must include an interface that allows users to restore these purchases.
    /// </para>
    /// </description>
    /// @ingroup BillingServices
    public static class BillingServices
    {
        #region Constants
        
        internal    const   string  kNullProductId              = "null";

        #endregion

        #region Static fields

        [ClearOnReload]
        private     static  INativeBillingServicesInterface     s_nativeInterface;

        #endregion

        #region Static properties

        public static BillingServicesUnitySettings UnitySettings { get; private set; }

        public static BillingProductDefinition[] ProductDefinitions { get; private set; }

        /// <summary>
        /// Returns the cached products array retrieved from store.
        /// </summary>
        /// @remark This property is invalid until a call to <see cref="InitializeStore"/> is completed.
        public static IBillingProduct[] Products { get; private set; }

        internal static IBillingProduct[] InactiveProducts { get; private set; }

        #endregion

        #region Static events

        /// <summary>
        /// Event that will be called when registered billing products are retreived from the Store.
        /// </summary>
        public static event EventCallback<BillingServicesInitializeStoreResult> OnInitializeStoreComplete;

        /// <summary>
        /// Event that will be called when payment state changes.
        /// </summary>
        public static event Callback<BillingServicesTransactionStateChangeResult> OnTransactionStateChange;

        /// <summary>
        /// Event that will be called when restored transaction details are received from the Store.
        /// </summary>
        public static event EventCallback<BillingServicesRestorePurchasesResult> OnRestorePurchasesComplete;
        
        #endregion

        #region Setup methods

        public static bool IsAvailable()
        {
            return (s_nativeInterface != null) && s_nativeInterface.IsAvailable;
        }

        /// @name Advanced Usage
        /// @{

        /// <summary>
        /// Initializes the billing services module with the given settings. This call is optional and only need to be called if you have custom settings to initialize this feature.
        /// </summary>
        /// <param name="settings">The settings to be used for initialization.</param>
        /// <remarks>
        /// <para>
        /// The settings configure the default image to be used for products.
        /// </para>
        /// </remarks>
        public static void Initialize(BillingServicesUnitySettings settings)
        {
            Assert.IsArgNotNull(settings, nameof(settings));

            // Reset event properties
            OnInitializeStoreComplete   = null;
            OnTransactionStateChange    = null;
            OnRestorePurchasesComplete  = null;

            // Set default properties
            UnitySettings               = settings;
            ProductDefinitions          = new BillingProductDefinition[0];
            Products                    = new IBillingProduct[0];
            InactiveProducts            = new IBillingProduct[0];

            // Configure interface
            s_nativeInterface           = NativeFeatureActivator.CreateInterface<INativeBillingServicesInterface>(ImplementationSchema.BillingServices, true);

            RegisterForEvents();
        }
        /// @}

        internal static BillingProductDefinition FindProductDefinitionWithId(string id, bool returnObjectOnFail = false)
        {
            var     definition  = Array.Find(ProductDefinitions, (item) => string.Equals(item.Id, id));
            if (definition != null)
            {
                return definition;
            }
            return returnObjectOnFail
                ? new BillingProductDefinition(id: id, platformId: kNullProductId)
                : null;
        }

        internal static BillingProductDefinition FindProductDefinitionWithPlatformId(string platformId, bool returnObjectOnFail = false)
        {
            var     definition  = Array.Find(ProductDefinitions, (item) => string.Equals(item.GetPlatformIdForActivePlatform(), platformId));
            if (definition != null)
            {
                return definition;
            }

            DebugLogger.LogWarning(EssentialKitDomain.Default, $"There is no product definition found for platform identifier: {platformId}. This can happen if product is not added in the Essential Kit settings. If the product is obsolete and to avoid this error, you can just add the obsolete product and mark it inactive in the settings.");

            return returnObjectOnFail
                ? new BillingProductDefinition(id: kNullProductId, platformId: platformId)
                : null;
        }

        #endregion

        #region Product methods

        /// <summary>
        /// Sends a request to retrieve localized information about the billing products from the Store.    
        /// </summary>
        /// <description> 
        /// Call to this method retrieves information of the products that are configured in <c>Billing Settings</c>.
        /// Your application uses this request to present localized prices and other information to the user without having to maintain that list itself. 
        /// </description>
        /// <remarks>
        /// \note When the request completes, <see cref="OnInitializeStoreComplete"/> is fired.
        /// </remarks>
        public static void InitializeStore()
        {
            InitializeStore(productDefinitions: UnitySettings.Products);
        }

        /// <summary>
        /// Sends a request to retrieve localized information about the billing products from the Store.    
        /// </summary>
        /// <description> 
        /// Call to this method retrieves information of the products that are configured in <c>Billing Settings</c>.
        /// Your application uses this request to present localized prices and other information to the user without having to maintain that list itself. 
        /// </description>
        /// <remarks>
        /// \note When the request completes, <see cref="OnInitializeStoreComplete"/> is fired.
        /// </remarks>
        public static void InitializeStore(BillingProductDefinition[] productDefinitions)
        {
            // validate arguments
            Assert.IsNotNullOrEmpty(productDefinitions, nameof(productDefinitions));

            try
            {
                // copy all definitions
                ProductDefinitions  = productDefinitions;
                
                // filter active definitions only and query them
                var activeDefinitions = productDefinitions.Where((item) => !item.IsInactive).ToArray();

                var inctiveDefinitions = productDefinitions.Where((item) => item.IsInactive).ToArray();
                InactiveProducts = ConvertToProductArray(inctiveDefinitions);
                // make request
                s_nativeInterface.RetrieveProducts(activeDefinitions);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
                OnInitializeStoreComplete?.Invoke(new BillingServicesInitializeStoreResult(new IBillingProduct[0], productDefinitions.Select((item) => item.Id).ToArray()), BillingServicesError.Unknown(exception.Message));
            }
        }

        private static IBillingProduct[] ConvertToProductArray(BillingProductDefinition[] inctiveDefinitions)
        {
            if (inctiveDefinitions.IsNullOrEmpty())
            {
                return new IBillingProduct[0];
            }
            else
            {
                List<IBillingProduct>  list = new List<IBillingProduct>();
                foreach (var item in inctiveDefinitions)
                {
                    var product = new BillingProductPlain(
                        id: item.Id,
                        platformId: item.GetPlatformIdForActivePlatform(),
                        type: item.ProductType,
                        localizedTitle: item.Title,
                        localizedDescription: item.Description,
                        price: null,
                        subscriptionInfo: null,
                        offers: null,
                        payouts: item.Payouts
                    );
                    list.Add(product);
                }
                return list.ToArray();
            }
        }

        /// <summary>
        /// Gets the billing product with localized information, which was previously fetched from the Store.
        /// </summary>
        /// <returns>The billing product fetched with localized information.</returns>
        /// <param name="id">A string used to identify a billing product.</param>
        /// <param name="includeInactive">Whether inactive products should be considered in search. Default is false.</param>
        public static IBillingProduct GetProductWithId(string id, bool includeInactive = false)
        {
            // validate arguments
            Assert.IsNotNullOrEmpty(id, "Id is null/empty");

            // check whether store is initialized
            if (Products.IsNullOrEmpty())
            {
                DebugLogger.LogWarning(EssentialKitDomain.Default, "Not initialized.");
                return null;
            }

            // find specified product
            var     targetProduct   = Array.Find(Products, (item) => string.Equals(item.Id, id));

            if(targetProduct == null && includeInactive)
            {
                targetProduct = Array.Find(InactiveProducts, (item) => string.Equals(item.Id, id));
            }

            return targetProduct;
        }

        #endregion

        #region Payment methods

        /// <summary>
        /// Determines whether the user is authorised to make payments.
        /// </summary>
        /// <returns><c>true</c> if the user is allowed to make product purchase payment; otherwise, <c>false</c>.</returns>
        public static bool CanMakePayments()
        {
            try
            {
                // make request
                return s_nativeInterface.CanMakePayments();
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
                return false;
            }
        }

        /// <summary>
        /// Determines whether specified billing product is already purchased.
        /// </summary>
        /// <returns><c>true</c> if specified billing product is already purchased; otherwise, <c>false</c>.</returns>
        /// <param name="productId">The identifier of the billing product.</param>
        /// <remarks> 
        /// \note This works only for Non-Consumable (Managed) billing product. For Consumable products, this will always returns false.
        /// </remarks>
        public static bool IsProductPurchased(string productId)
        {
            // validate arguments
            Assert.IsArgNotNull(productId, nameof(productId));
            
            // find specified product
            var     targetProduct   = GetProductWithId(productId);
            return IsProductPurchased(targetProduct);
        }

        /// <summary>
        /// Determines whether specified billing product is already purchased.
        /// </summary>
        /// <returns><c>true</c> if specified billing product is already purchased; otherwise, <c>false</c>.</returns>
        /// <param name="product">The object identifies the billing product registered in the Store.</param>
        /// <remarks> 
        /// \note This works only for Non-Consumable (Managed) billing product. For Consumable products, this will always returns false.
        /// </remarks>
        public static bool IsProductPurchased(IBillingProduct product)
        {
            // validate arguments
            Assert.IsArgNotNull(product, nameof(product));

            try
            {
                // make request
                return s_nativeInterface.IsProductPurchased(product);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
                return false;
            }
        }

        /// <summary>
        /// Initiates purchase process for the specified billing product.
        /// </summary>
        /// <param name="productId">The product you want to purchase.</param>
        /// <param name="options">Additional options for the purchase.</param>
        public static void BuyProduct(string productId, BuyProductOptions options = null)
        {
            // find specified product
            var     targetProduct   = GetProductWithId(productId);
            if (targetProduct == null)
            {
                DebugLogger.LogError(EssentialKitDomain.Default, $"Product not found with id: {productId}.");
            }

            BuyProduct(targetProduct, options);
        }

        /// <summary>
        /// Initiates purchase process for the specified billing product.
        /// </summary>
        /// <remarks>
        /// \note The payment request must have a product identifier registered with the Store.
        /// </remarks>
        /// <param name="product">The product you want to purchase.</param>
        /// <param name="options">Create BuyProductOptions object to set custom quantity and purchase tag.</param>
        public static void BuyProduct(IBillingProduct product, BuyProductOptions options = null)
        {
            var buyOptions = options ?? BuyProductOptions.Default;

            // validate arguments
            Assert.IsArgNotNull(product, nameof(product));
            if (buyOptions.Quantity < 1)
            {
                DebugLogger.LogError(EssentialKitDomain.Default, $"Product quantity can't be less than 1.");
            }
                
            try
            {
                // make request           
                s_nativeInterface.BuyProduct(product.Id, product.PlatformId, buyOptions);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
        }

        /// <summary>
        /// Returns the pending transactions available in transaction queue.
        /// </summary>
        /// /// <remarks>
        /// \note User needs to manually call this method after receiving completed transactions, incase if autoFinishTransactions flag is turned off in billing settings.
        /// </remarks>
        /// <returns>An array of unfinished transactions.</returns>
        public static IBillingTransaction[] GetTransactions()
        {
            try
            {
                // make request
                return s_nativeInterface.GetTransactions() ?? new IBillingTransaction[0];
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
                return new IBillingTransaction[0];
            }
        }

        /// <summary>
        /// Completes the pending transactions and removes it from transaction queue.
        /// </summary>
        /// <param name="transactions">An array of unfinished transactions.</param>
        public static void FinishTransactions(IBillingTransaction[] transactions)
        {
            // validate arguments
            Assert.IsArgNotNull(transactions, "transactions");

            try
            {
                // make request
                var     finishableTransactions  = Array.FindAll(transactions, (item) =>
                {
                    var     transactionState    = item.TransactionState;
                    return !(transactionState == BillingTransactionState.Purchasing ||
                             transactionState == BillingTransactionState.Deferred);
                });
                if (finishableTransactions.Length > 0)
                {
                    s_nativeInterface.FinishTransactions(finishableTransactions);
                }
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
        }

        #endregion

        #region Restore methods
        
         /// <summary>
        /// Sends a request to restore completed purchases.
        /// </summary>
        /// <description>
        /// Your application calls this method to restore transactions that were previously purchased so that you can process them again.
        /// </description>
        /// <remarks> 
        /// \note 
        /// Internally this feature requires non-consumable product information. So ensure that <see cref="InitializeStore"/> is called prior to this. 
        /// </remarks>
        /// <param name="forceRefresh">If set to <c>true</c> force refresh by contacting server. On iOS this will trigger a login prompt to let user signin. So set this to true only if user clicks manually "Restore" option in IAP screens.</param>
        /// <param name="tag">This can be a unique identifier which was passed in BuyProductOptions when purchasing a product. Usually, this can be the application username or any unique identifier for which you want to restore purchases for. (optional)</param>
        public static void RestorePurchases(bool forceRefresh = false, string tag = null)
        {
           try
            {
                // make request
                s_nativeInterface.RestorePurchases(forceRefresh, tag);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            } 
        }

        #endregion

        #region Obsolete methods

        /// @name Obsolete methods
        /// @{
            
        /// <summary>
        /// Initiates purchase process for the specified billing product.
        /// </summary>
        /// <returns><c>true</c>, if request was initiated, <c>false</c> otherwise failed. This can happen if product is not found</returns>
        /// <remarks>
        /// \note The payment request must have a product identifier registered with the Store.
        /// </remarks>
        /// <param name="productId">The product you want to purchase.</param>
        /// <param name="quantity">The number of units you want to purchase. Default quantity value is 1.</param>
        /// <param name="applicationUsername">Application provided username that initiated this request. (optional)</param>
        [Obsolete("This method is deprecated. Use BuyProduct with BuyProductOptions parameter", true)] //Obsolete:2024 
        public static bool BuyProduct(string productId, int quantity = 1, string applicationUsername = null)
        {
            return false;
        } 

        /// <summary>
        /// Initiates purchase process for the specified billing product.
        /// </summary>
        /// <returns><c>true</c>, if request was initiated, <c>false</c> otherwise failed. This can happen if product is not found</returns>
        /// <remarks>
        /// \note The payment request must have a product identifier registered with the Store.
        /// </remarks>
        /// <param name="product">The product you want to purchase.</param>
        /// <param name="quantity">The number of units you want to purchase. Default quantity value is 1.</param>
        /// <param name="applicationUsername">Specify user data associated with the purchase. Eg: Application provided username that initiated this request. (optional)</param>
        [Obsolete("This method is deprecated. Use BuyProduct with BuyProductOptions parameter", true)] //Obsolete:2024 
        public static bool BuyProduct(IBillingProduct product, int quantity = 1, string applicationUsername = null)
        {
            return false;
        }

        [Obsolete("This method is obsolete. Use RestorePurchases with forceRefresh parameter", true)] //Obsolete:2024
        public static void RestorePurchases(string tag = null) {}

        /// @}

        #endregion

        #region Private methods

        private static void RegisterForEvents()
        {
            s_nativeInterface.OnRetrieveProductsComplete    += HandleOnRetrieveProductsComplete;
            s_nativeInterface.OnTransactionStateChange      += HandleOnTransactionStateChange;
            s_nativeInterface.OnRestorePurchasesComplete    += HandleOnRestorePurchasesComplete;
        }

        private static void UnregisterFromEvents()
        {
            s_nativeInterface.OnRetrieveProductsComplete    -= HandleOnRetrieveProductsComplete;
            s_nativeInterface.OnTransactionStateChange      -= HandleOnTransactionStateChange;
            s_nativeInterface.OnRestorePurchasesComplete    -= HandleOnRestorePurchasesComplete;
        }

        #endregion

        #region Event callback methods

        private static void HandleOnRetrieveProductsComplete(IBillingProduct[] products, string[] invalidProductIds, Error error)
        {
            // update cache
            Products                = products ?? new IBillingProduct[0];

            // invoke event
            var     result          = new BillingServicesInitializeStoreResult(
                products: products,
                invalidProductIds: invalidProductIds ?? new string[0]);
            CallbackDispatcher.InvokeOnMainThread(OnInitializeStoreComplete, result, error);

            // check whether any pending transactions are available
            if (error == null)
            {
                DebugLogger.Log(EssentialKitDomain.Default, "Trying to clear any unfinished transactions. If any transaction state changes events are reported, make sure you are calling BillingServices.FinishTransactions after processing transactions.");
                s_nativeInterface.TryClearingUnfinishedTransactions();
            }
        }
       
        private static void HandleOnTransactionStateChange(IBillingTransaction[] transactions)
        {
            // prepare transaction array
            transactions        = transactions ?? new IBillingTransaction[0];

            // invoke event
            var     result      = new BillingServicesTransactionStateChangeResult(transactions);
            CallbackDispatcher.InvokeOnMainThread(OnTransactionStateChange, result);

            // mark transactions as completed
            if (UnitySettings.AutoFinishTransactions)
            {
                FinishTransactions(transactions);
            }
        }

        private static void HandleOnRestorePurchasesComplete(IBillingTransaction[] transactions, Error error)
        {
            // prepare transaction array
            transactions        = transactions ?? new IBillingTransaction[0];

            // invoke event
            var     result      = new BillingServicesRestorePurchasesResult(transactions);
            CallbackDispatcher.InvokeOnMainThread(OnRestorePurchasesComplete, result, error);

            // mark transactions as completed
            if (UnitySettings.AutoFinishTransactions)
            {
                FinishTransactions(transactions);
            }
        }

        #endregion
    }
}