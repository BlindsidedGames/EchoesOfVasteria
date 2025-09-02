#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit.BillingServicesCore.Android
{
    internal static class Converter
    {
        public static BillingTransactionState From(NativeBillingTransactionState nativeBillingTransactionState)
        {
            return (BillingTransactionState) nativeBillingTransactionState;
        }

        public static BillingReceiptVerificationState From(NativeBillingTransactionVerificationState nativeBillingTransactionVerificationState)
        {
            return (BillingReceiptVerificationState) nativeBillingTransactionVerificationState;
        }

        public static NativeBillingTransactionVerificationState From(BillingReceiptVerificationState value)
        {
            return (NativeBillingTransactionVerificationState)value;
        }

        internal static BillingTransaction From(NativeBillingTransaction nativeTransaction, Error error = null)
        {
            var productPlatformId = nativeTransaction.GetProductIdentifier();
            var productDefinition   = BillingServices.FindProductDefinitionWithPlatformId(productPlatformId, returnObjectOnFail: true);
            var product = BillingServices.GetProductWithId(productDefinition.Id, includeInactive: true);

            if(error == null)
            {
                return new BillingTransaction(product, nativeTransaction);
            }
            else
            {
                return new BillingTransaction(product, nativeTransaction, error);
            }
        }

        internal static IBillingTransaction[] From(List<NativeBillingTransaction> nativeTransactions)
        {
            List<IBillingTransaction> transactions = new List<IBillingTransaction>();
            foreach (var nativeTransaction in nativeTransactions)
            {
                var transaction = From(nativeTransaction);
                transactions.Add(transaction);
            }

            return transactions.ToArray();
        }

        internal static BillingProductSubscriptionStatus From(NativeBillingProductSubscriptionStatus nativeStatus)
        {
            if (nativeStatus == null || nativeStatus.IsNull()) 
            {
                return null;
            }

            string groupId = nativeStatus.GetGroupId();
            BillingProductSubscriptionRenewalInfo renewalInfo = From(nativeStatus.GetRenewalInfo());
            DateTime? expirationDate = nativeStatus.GetExpirationDate().GetDateTimeOptional();
            bool isUpgraded = nativeStatus.GetUpgraded();
            string appliedOfferId = nativeStatus.GetAppliedOfferId();
            BillingProductOfferCategory? offerCategory = appliedOfferId == null ? null : (BillingProductOfferCategory)nativeStatus.GetAppliedOfferCategory();
            
            return new BillingProductSubscriptionStatus(groupId, renewalInfo, expirationDate, isUpgraded, appliedOfferId, offerCategory);
           
        }

        private static BillingProductSubscriptionRenewalInfo From(NativeBillingProductSubscriptionRenewalInfo nativeRenewalInfo)
        {
            if (nativeRenewalInfo == null || nativeRenewalInfo.IsNull()) 
            {
                return null;
            }
            
            BillingProductSubscriptionRenewalState state = (BillingProductSubscriptionRenewalState)nativeRenewalInfo.GetState();
            string applicableOfferIdentifier = nativeRenewalInfo.GetApplicableOfferIdentifier();
            BillingProductOfferCategory? applicableOfferCategory = applicableOfferIdentifier == null ? null : (BillingProductOfferCategory)nativeRenewalInfo.GetApplicableOfferCategory();
            DateTime? lastRenewedDate = nativeRenewalInfo.GetLastRenewedDate().GetDateTimeOptional();
            string lastRenewalId = nativeRenewalInfo.GetLastRenewalId();
            bool isAutoRenewEnabled = nativeRenewalInfo.GetAutoRenewEnabled();
            BillingProductSubscriptionExpirationReason? expirationReason = nativeRenewalInfo.GetExpirationReason() == NativeBillingProductSubscriptionExpirationReason.None ? null : (BillingProductSubscriptionExpirationReason)nativeRenewalInfo.GetExpirationReason();
            DateTime? renewalDate = nativeRenewalInfo.GetRenewalDate().IsNull() ? null : nativeRenewalInfo.GetRenewalDate().GetDateTime();
            DateTime? gracePeriodExpirationDate = nativeRenewalInfo.GetGracePeriodExpirationDate().GetDateTimeOptional();
            BillingProductSubscriptionPriceIncreaseStatus priceIncreaseStatus = (BillingProductSubscriptionPriceIncreaseStatus)nativeRenewalInfo.GetPriceIncreaseStatus();

            return new BillingProductSubscriptionRenewalInfo(
                state,
                applicableOfferIdentifier,
                applicableOfferCategory,
                lastRenewedDate,
                lastRenewalId,
                isAutoRenewEnabled,
                expirationReason,
                renewalDate,
                gracePeriodExpirationDate,
                priceIncreaseStatus);
        }
    }
}
#endif
