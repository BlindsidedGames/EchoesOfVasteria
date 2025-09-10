#if UNITY_IOS || UNITY_TVOS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VoxelBusters.EssentialKit.BillingServicesCore.iOS
{
    internal static class BillingServicesBinding
    {
        [DllImport("__Internal")]
        public static extern bool NPBillingServicesCanMakePayments();

        [DllImport("__Internal")]
        public static extern void NPBillingServicesRegisterCallbacks(RequestForProductsNativeCallback requestForProductsCallback, TransactionStateChangeNativeCallback transactionStateChangeCallback, RestorePurchasesNativeCallback restorePurchasesCallback);
        
        [DllImport("__Internal")]
        public static extern void NPBillingServicesInit(bool autoFinishTransactions);

        [DllImport("__Internal")]
        public static extern bool NPBillingServicesIsProductPurchased(string productId);
        
        [DllImport("__Internal")]
        public static extern void NPBillingServicesRequestForBillingProducts(string[] productIds, int length);
        
        [DllImport("__Internal")]
        public static extern void NPBillingServicesBuyProduct(string productId, SKBuyProductOptionsData buyProductOptionsData);

        [DllImport("__Internal")]
        public static extern IntPtr NPBillingServicesGetTransactions(out int length);

        [DllImport("__Internal")]
        public static extern void NPBillingServicesRestorePurchases(bool forceRefresh, string tag);

        [DllImport("__Internal")]
        public static extern void NPBillingServicesFinishTransactions(IntPtr transactionsPtr, int length);

        [DllImport("__Internal")]
        public static extern void NPBillingServicesTryClearingUnfinishedTransactions();
    }
}
#endif