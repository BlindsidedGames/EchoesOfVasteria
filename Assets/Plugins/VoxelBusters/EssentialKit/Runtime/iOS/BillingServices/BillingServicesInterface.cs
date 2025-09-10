#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using AOT;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.CoreLibrary.NativePlugins.iOS;

namespace VoxelBusters.EssentialKit.BillingServicesCore.iOS
{
    public sealed class BillingServicesInterface : NativeBillingServicesInterfaceBase, INativeBillingServicesInterface
    {
        #region Static fields

        private static  BillingServicesInterface    s_staticInstance;

        #endregion

        #region Constructors

        static BillingServicesInterface()
        {
            BillingServicesBinding.NPBillingServicesRegisterCallbacks(HandleRequestForProductsCallbackInternal, HandlePaymentStateChangeCallbackInternal, HandleRestorePurchasesCallbackInternal);
        }

        public BillingServicesInterface()
            : base(isAvailable: true)
        {
            // initialise component
            var     unitySettings           = BillingServices.UnitySettings;
            BillingServicesBinding.NPBillingServicesInit(unitySettings.AutoFinishTransactions);

            // cache reference
            s_staticInstance    = this;
        }

        #endregion

        #region Base class methods

        public override bool CanMakePayments()
        {
            return BillingServicesBinding.NPBillingServicesCanMakePayments();
        }

        public override void RetrieveProducts(BillingProductDefinition[] productDefinitions)
        {
            var     productIds  = Array.ConvertAll(productDefinitions, (item) => item.GetPlatformIdForActivePlatform());
            BillingServicesBinding.NPBillingServicesRequestForBillingProducts(productIds, productIds.Length);
        }

        public override bool IsProductPurchased(IBillingProduct product)
        {
            return BillingServicesBinding.NPBillingServicesIsProductPurchased(product.PlatformId);
        }        

        public override void BuyProduct(string productId, string productPlatformId, BuyProductOptions options)
        {
            var offerRedeemDetails = options.OfferRedeemDetails;
            SKBillingProductOfferRedeemDetailsData billingProductOfferRedeemDetailsData = new SKBillingProductOfferRedeemDetailsData() { OfferId = offerRedeemDetails?.IosPlatformProperties.OfferId, KeyId = offerRedeemDetails?.IosPlatformProperties.KeyId, Nonce = offerRedeemDetails?.IosPlatformProperties.Nonce, Signature = offerRedeemDetails?.IosPlatformProperties.Signature, Timestamp = offerRedeemDetails != null ? offerRedeemDetails.IosPlatformProperties.Timestamp : 0 };
            SKBuyProductOptionsData buyProductOptionsData = new SKBuyProductOptionsData() { Quantity = options.Quantity, Tag = options.Tag?.ToString(), OfferRedeemDetails = billingProductOfferRedeemDetailsData};

            // make request
            BillingServicesBinding.NPBillingServicesBuyProduct(productPlatformId, buyProductOptionsData);
        }

        public override IBillingTransaction[] GetTransactions()
        {
	        IntPtr  transactionsPtr = BillingServicesBinding.NPBillingServicesGetTransactions(out var length);

            try
            {
                // convert native array to unity type
                return BillingServicesUtility.CreateTransactionArray(transactionsPtr, length);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
                return null;
            }
            finally
            {
                IosNativePluginsUtility.FreeCPointerObject(transactionsPtr);
            }
        }

        public override void FinishTransactions(IBillingTransaction[] transactions)
        { 
            // create native array
            var     transactionsPtr     = Array.ConvertAll(transactions, (item) => ((BillingTransaction)item).AddrOfNativeObject());
            var     unmangedPtr         = MarshalUtility.CreateUnmanagedArray(transactionsPtr);

            try
            {
                BillingServicesBinding.NPBillingServicesFinishTransactions(unmangedPtr, transactions.Length);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
            finally
            {
                // release 
                MarshalUtility.ReleaseUnmanagedArray(unmangedPtr);
            }
        }

        public override void RestorePurchases(bool forceRefresh, string tag)
        {
            BillingServicesBinding.NPBillingServicesRestorePurchases(forceRefresh, tag);
        }

        public override void TryClearingUnfinishedTransactions()
        {
            BillingServicesBinding.NPBillingServicesTryClearingUnfinishedTransactions();
        }

        #endregion

        #region Native callback methods

        [MonoPInvokeCallback(typeof(RequestForProductsNativeCallback))]
        private static void HandleRequestForProductsCallbackInternal(IntPtr productsPtr, int length, NativeError error, ref NativeArray invalidProductIds)
        {
            // send event
            IBillingProduct[]     products  = BillingServicesUtility.CreateProductArray(productsPtr, length);
            var     invalidIdManagedArray   = MarshalUtility.CreateStringArray(invalidProductIds.Pointer, invalidProductIds.Length);
            var     errorObj                = error.Convert(BillingServicesError.kDomain);
            s_staticInstance.SendRetrieveProductsCompleteEvent(products, invalidIdManagedArray, errorObj);
        }

        [MonoPInvokeCallback(typeof(TransactionStateChangeNativeCallback))]
        private static void HandlePaymentStateChangeCallbackInternal(IntPtr transactionsPtr, int length)
        {
            // send event
            var     transactions    = BillingServicesUtility.CreateTransactionArray(transactionsPtr, length);
            s_staticInstance.SendPaymentStateChangeEvent(transactions);
        }

        [MonoPInvokeCallback(typeof(RestorePurchasesNativeCallback))]
        private static void HandleRestorePurchasesCallbackInternal(IntPtr transactionsPtr, int length, NativeError error)
        {
            // send event
            var     transactions    = BillingServicesUtility.CreateTransactionArray(transactionsPtr, length);
            var     errorObj        = error.Convert(BillingServicesError.kDomain);
            s_staticInstance.SendRestorePurchasesCompleteEvent(transactions, errorObj);
        }

        #endregion
    }
}
#endif