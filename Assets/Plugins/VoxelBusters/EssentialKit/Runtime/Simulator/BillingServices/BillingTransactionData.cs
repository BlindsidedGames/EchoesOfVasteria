using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Simulator
{
    [Serializable]
    public sealed class BillingTransactionData 
    {
        #region Properties

        public string ProductId
        {
            get;
            set;
        }


        public int Quantity
        {
            get;
            set;
        }

        public string Tag
        {
            get;
            set;
        }

        public string TransactionId
        {
            get;
            set;
        }

        public DateTime TransactionDate
        {
            get;
            set;
        }

        public BillingTransactionState TransactionState
        {
            get;
            set;
        }

        public BillingProductType ProductType
        {
            get;
            set;
        }

        public int PurchasedQuantity
        {
            get;
            set;
        }

        public string OriginalTransactionId
        {
            get;
            set;
        }

        public DateTime OriginalPurchaseDate
        {
            get;
            set;
        }

        public string ApplicationBundleIdentifier
        {
            get;
            set;
        }

        public BillingEnvironment Environment
        {
            get;
            set;
        }

        public DateTime? RevocationDate
        {
            get;
            set;
        }

        public BillingProductRevocationReason? RevocationReason
        {
            get;
            set;
        }
        public BillingProductSubscriptionStatus SubscriptionStatus
        {
            get;
            set;
        }
        public string RawData
        {
            get;
            set;
        }

        public Error Error
        {
            get;
            set;
        }

        #endregion

        #region Constructors

        public BillingTransactionData(string productId, int quantity, string tag, string transactionId, DateTime transactionDate, BillingTransactionState transactionState, BillingProductType productType, int purchasedQuantity, string originalTransactionId, DateTime originalPurchaseDate, string applicationBundleIdentifier, BillingEnvironment environment, DateTime? revocationDate, BillingProductRevocationReason? revocationReason, BillingProductSubscriptionStatus subscriptionStatus, string rawData, Error error)
        {
            ProductId = productId;
            Quantity = quantity;
            Tag = tag;
            TransactionId = transactionId;
            TransactionDate = transactionDate;
            TransactionState = transactionState;
            ProductType = productType;
            PurchasedQuantity = purchasedQuantity;
            OriginalTransactionId = originalTransactionId;
            OriginalPurchaseDate = originalPurchaseDate;
            ApplicationBundleIdentifier = applicationBundleIdentifier;
            Environment = environment;
            RevocationDate = revocationDate;
            RevocationReason = revocationReason;
            SubscriptionStatus = subscriptionStatus;
            RawData = rawData;
            Error = error;
        }

        #endregion
    }
}