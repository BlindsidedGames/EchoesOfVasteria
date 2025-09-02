using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.BillingServicesCore
{
    public interface INativeBillingServicesInterface : INativeFeatureInterface
    {
        #region Events

        event RetrieveProductsInternalCallback OnRetrieveProductsComplete;

        event PaymentStateChangeInternalCallback OnTransactionStateChange;

        event RestorePurchasesInternalCallback OnRestorePurchasesComplete;

        #endregion

        #region Methods

        bool CanMakePayments();

        void RetrieveProducts(BillingProductDefinition[] productDefinitions);

        bool IsProductPurchased(IBillingProduct product);

        void BuyProduct(string productId, string platformProductId, BuyProductOptions options); //Instead just pass BuyOptions in V4.

        IBillingTransaction[] GetTransactions();

        void FinishTransactions(IBillingTransaction[] transactions);

        void RestorePurchases(bool forceRefresh, string tag);

        void TryClearingUnfinishedTransactions();

        #endregion
    }
}