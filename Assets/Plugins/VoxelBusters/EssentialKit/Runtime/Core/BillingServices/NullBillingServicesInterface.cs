using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.BillingServicesCore
{
    internal class NullBillingServicesInterface : NativeBillingServicesInterfaceBase
    {
        #region Constructors

        public NullBillingServicesInterface()
            : base(isAvailable: false)
        { }

        #endregion

        #region Private static methods

        private static void LogNotSupported()
        {
            Diagnostics.LogNotSupported("BillingServices");
        }

        #endregion

        #region Base class methods

        public override bool CanMakePayments()
        {
            LogNotSupported();

            return false;
        }

        public override void RetrieveProducts(BillingProductDefinition[] productDefinitions)
        { 
            LogNotSupported();

            SendRetrieveProductsCompleteEvent(new IBillingProduct[0], Array.ConvertAll(productDefinitions, (item) => item.GetPlatformIdForActivePlatform()), Diagnostics.kFeatureNotSupported);
        }

        public override bool IsProductPurchased(IBillingProduct product)
        {
            return false;
        }

        public override void BuyProduct(string productId, string productPlatformId, BuyProductOptions options)
        { 
            // set default value to reference parameters
            LogNotSupported();
        }

        public override IBillingTransaction[] GetTransactions()
        {
            LogNotSupported();

            return new IBillingTransaction[0];
        }

        public override void FinishTransactions(IBillingTransaction[] transactions)
        { 
            LogNotSupported();
        }

        public override void RestorePurchases(bool forceRefresh, string tag)
        {
            LogNotSupported();

            SendRestorePurchasesCompleteEvent(new IBillingTransaction[0], Diagnostics.kFeatureNotSupported);
        }

        public override void TryClearingUnfinishedTransactions()
        {
            LogNotSupported();
        }

        #endregion
    }
}