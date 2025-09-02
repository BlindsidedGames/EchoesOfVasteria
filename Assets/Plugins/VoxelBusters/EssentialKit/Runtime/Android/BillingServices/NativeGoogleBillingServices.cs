#if UNITY_ANDROID
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins.Android;
using VoxelBusters.EssentialKit.Common.Android;
namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    public class NativeGoogleBillingServices : NativeAndroidJavaObjectWrapper
    {
        #region Static properties

         private static AndroidJavaClass m_nativeClass;

        #endregion
        #region Private properties
        private NativeActivity Activity
        {
            get;
            set;
        }
        #endregion

        #region Constructor

        public NativeGoogleBillingServices(NativeContext context) : base(Native.kClassName, (object)context.NativeObject)
        {
            Activity    = new NativeActivity(context);
        }

        #endregion
        #region Static methods
        private static AndroidJavaClass GetClass()
        {
            if (m_nativeClass == null)
            {
                m_nativeClass = new AndroidJavaClass(Native.kClassName);
            }
            return m_nativeClass;
        }
        #endregion
        #region Public methods

        public void BuyProduct(string requestedProductIdentifier, int quantity, string tag, string offerId)
        {
            Activity.RunOnUiThread(() => {
#if NATIVE_PLUGINS_DEBUG_ENABLED
                DebugLogger.Log("[Class : NativeGoogleBillingServices][Method(RunOnUiThread) : BuyProduct]");
#endif
                Call(Native.Method.kBuyProduct, new object[] { requestedProductIdentifier, quantity, tag, offerId } );
            });
        }
        public bool CanMakePayments()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeGoogleBillingServices][Method : CanMakePayments]");
#endif
            return Call<bool>(Native.Method.kCanMakePayments);
        }
        public void FetchProductDetails(NativeFetchBillingProductsListener listener)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeGoogleBillingServices][Method : FetchProductDetails]");
#endif
            Call(Native.Method.kFetchProductDetails, new object[] { listener } );
        }
        public void FinishBillingTransaction(string targetProductId, bool isValidPurchase)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeGoogleBillingServices][Method : FinishBillingTransaction]");
#endif
            Call(Native.Method.kFinishBillingTransaction, new object[] { targetProductId, isValidPurchase } );
        }
        public string GetFeatureName()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeGoogleBillingServices][Method : GetFeatureName]");
#endif
            return Call<string>(Native.Method.kGetFeatureName);
        }
        public NativeList<NativeBillingTransaction> GetIncompleteBillingTransactions()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeGoogleBillingServices][Method : GetIncompleteBillingTransactions]");
#endif
            AndroidJavaObject nativeObj = Call<AndroidJavaObject>(Native.Method.kGetIncompleteBillingTransactions);
            NativeList<NativeBillingTransaction> data  = new  NativeList<NativeBillingTransaction>(nativeObj);
            return data;
        }
        public void Initialise(string publicKey, NativeBillingTransactionStateListener listener)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeGoogleBillingServices][Method : Initialise]");
#endif
            Call(Native.Method.kInitialise, new object[] { publicKey, listener } );
        }
        public bool IsProductPurchased(string productIdentifier)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeGoogleBillingServices][Method : IsProductPurchased]");
#endif
            return Call<bool>(Native.Method.kIsProductPurchased, productIdentifier);
        }
        public void RestorePurchases(bool forceRefresh, string tag, NativeRestorePurchasesListener listener)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeGoogleBillingServices][Method : RestorePurchases]");
#endif
            Call(Native.Method.kRestorePurchases, new object[] { forceRefresh, tag, listener } );
        }
        public void SetProducts(string[] consumableProductIds, string[] nonConsumableProductIds, string[] subscriptionProductIds)
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeGoogleBillingServices][Method : SetProducts]");
#endif
            Call(Native.Method.kSetProducts, new object[] { consumableProductIds, nonConsumableProductIds, subscriptionProductIds } );
        }
        public void TryClearingIncompleteTransactions()
        {
#if NATIVE_PLUGINS_DEBUG_ENABLED
            DebugLogger.Log("[Class : NativeGoogleBillingServices][Method : TryClearingIncompleteTransactions]");
#endif
            Call(Native.Method.kTryClearingIncompleteTransactions);
        }

        #endregion

        internal class Native
        {
            internal const string kClassName = "com.voxelbusters.essentialkit.billingservices.providers.google.GoogleBillingServices";

            internal class Method
            {
                internal const string kTryClearingIncompleteTransactions = "tryClearingIncompleteTransactions";
                internal const string kFetchProductDetails = "fetchProductDetails";
                internal const string kSetProducts = "setProducts";
                internal const string kIsProductPurchased = "isProductPurchased";
                internal const string kInitialise = "initialise";
                internal const string kBuyProduct = "buyProduct";
                internal const string kFinishBillingTransaction = "finishBillingTransaction";
                internal const string kGetFeatureName = "getFeatureName";
                internal const string kCanMakePayments = "canMakePayments";
                internal const string kRestorePurchases = "restorePurchases";
                internal const string kGetIncompleteBillingTransactions = "getIncompleteBillingTransactions";
            }

        }
    }
}
#endif