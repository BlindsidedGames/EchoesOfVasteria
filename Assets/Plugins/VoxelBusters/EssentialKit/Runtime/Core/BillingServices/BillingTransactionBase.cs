using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.BillingServicesCore
{
    public abstract class BillingTransactionBase : NativeObjectBase, IBillingTransaction
    {
        #region Properties

        protected BillingTransactionBase(string transactionId, IBillingProduct product)
        {
            // set properties
            Id                  = transactionId;
            Product             = product;
        }

        ~BillingTransactionBase()
        {
            Dispose(false);
        }

        #endregion

        #region Abstract methods

        protected abstract int GetRequestedQuantityInternal();
        protected abstract string GetTagInternal();
        protected abstract DateTime GetTransactionDateUTCInternal();
        protected abstract BillingTransactionState GetTransactionStateInternal();
        protected abstract BillingReceiptVerificationState GetReceiptVerificationStateInternal();
        protected abstract void SetReceiptVerificationStateInternal(BillingReceiptVerificationState value);
        protected abstract string GetReceiptInternal();
        protected abstract BillingEnvironment GetEnvironmentInternal();
        protected abstract string GetApplicationBundleIdentifierInternal();
        protected abstract int GetPurchasedQuantityInternal();
        protected abstract BillingProductRevocationInfo GetRevocationInfoInternal();
        protected abstract BillingProductSubscriptionStatus GetSubscriptionStatusInternal();
        protected abstract Error GetErrorInternal();
        protected abstract string GetRawDataInternal();
        
        #endregion

        #region IBillingTransaction implementation

        public string Id { get; private set; }
        public IBillingPayment Payment { get; private set; }
        public IBillingProduct Product { get; private set; }
        public int RequestedQuantity    => GetRequestedQuantityInternal();
        public string Tag => GetTagInternal();

        public DateTime DateUTC => GetTransactionDateUTCInternal();
        public DateTime Date => DateUTC.ToLocalTime();
        public BillingTransactionState TransactionState => GetTransactionStateInternal();
        public BillingReceiptVerificationState ReceiptVerificationState
        {
            get => GetReceiptVerificationStateInternal();
            set => SetReceiptVerificationStateInternal(value);
        }

        public string Receipt => GetReceiptInternal();
        public BillingEnvironment Environment => GetEnvironmentInternal();
        public string ApplicationBundleIdentifier => GetApplicationBundleIdentifierInternal();
        public int PurchasedQuantity => GetPurchasedQuantityInternal();
        public BillingProductRevocationInfo RevocationInfo => GetRevocationInfoInternal();
        public BillingProductSubscriptionStatus SubscriptionStatus => GetSubscriptionStatusInternal();
        public Error Error => GetErrorInternal();
        public BillingTransactionAndroidProperties AndroidProperties => null;
        public string RawData => GetRawDataInternal();

        #endregion

        #region  Override methods

        public override string ToString()
        {
            var     sb  = new StringBuilder();

            sb.Append("BillingTransaction { ")
                .Append($"Id: {Id} ")
                .Append($"Product: {Product} ")
                .Append($"RequestedQuantity: {RequestedQuantity} ")
                .Append($"Tag: {Tag} ")
                .Append($"TransactionState: {TransactionState} ");

            if(TransactionState != BillingTransactionState.Failed)
            {

                sb.Append($"Date: {Date} ")
                .Append($"DateUTC: {DateUTC} ")
                .Append($"ReceiptVerificationState: {ReceiptVerificationState} ")
                .Append($"Receipt: {Receipt} ")
                .Append($"Environment: {Environment} ")
                .Append($"ApplicationBundleIdentifier: {ApplicationBundleIdentifier} ")
                .Append($"PurchasedQuantity: {PurchasedQuantity} ")
                .Append($"RevocationDate: {RevocationInfo} ")
                .Append($"SubscriptionStatus: {SubscriptionStatus} ")
                .Append($"RawData: {RawData} ");
            }
            else
            {
                sb.Append($"Error: {Error} ");
            }
            sb.Append("}");

            return sb.ToString();
        }

        #endregion
    }
}