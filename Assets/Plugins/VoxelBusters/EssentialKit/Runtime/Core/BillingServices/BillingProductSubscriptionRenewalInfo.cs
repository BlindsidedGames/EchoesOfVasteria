
using System;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Represents the information about the billing product subscription renewal.
    /// </summary>
    /// @ingroup BillingServices
    public class BillingProductSubscriptionRenewalInfo
    {
        /// <summary>
        /// Gets the renewal state of the billing product subscription.
        /// </summary>
        public BillingProductSubscriptionRenewalState State { get; private set; }

        /// <summary>
        /// Gets the identifier of the applicable offer.
        /// </summary>
        private string ApplicableOfferIdentifier { get; set; }

        /// <summary>
        /// Gets the category of the applicable offer.
        /// </summary>
        private BillingProductOfferCategory? ApplicableOfferCategory { get; set; }

        /// <summary>
        /// Gets the date when the billing product subscription was last renewed.
        /// </summary>
        private DateTime? LastRenewedDate { get; set; }

        /// <summary>
        /// Gets the unique identifier of the last transaction.
        /// </summary>
        private string LastRenewalId { get; set; }

        /// <summary>
        /// Gets a value indicating whether the billing product subscription is auto-renewed.
        /// </summary>
        public bool IsAutoRenewEnabled { get; private set; }

        /// <summary>
        /// Gets the reason why the billing product subscription is expired.
        /// </summary>
        public BillingProductSubscriptionExpirationReason? ExpirationReason { get; private set; }

        /// <summary>
        /// Gets the date when the billing product subscription will be renewed.
        /// </summary>
        public DateTime? RenewalDate { get; private set; }

        /// <summary>
        /// Gets the date when the grace period for the billing product subscription will expire.
        /// </summary>
        public DateTime? GracePeriodExpirationDate { get; private set; }

        /// <summary>
        /// Gets the price increase status of the billing product subscription.
        /// </summary>
        private BillingProductSubscriptionPriceIncreaseStatus PriceIncreaseStatus { get;  set; } //Remove as no option to check on Android

        internal BillingProductSubscriptionRenewalInfo(BillingProductSubscriptionRenewalState state, string applicableOfferIdentifier, BillingProductOfferCategory? applicableOfferCategory, DateTime? lastRenewedDate, string lastRenewalId, bool isAutoRenewEnabled, BillingProductSubscriptionExpirationReason? expirationReason, DateTime? renewalDate = null, DateTime? gracePeriodExpirationDate = null, BillingProductSubscriptionPriceIncreaseStatus priceIncreaseStatus = BillingProductSubscriptionPriceIncreaseStatus.NoIncreasePending)
        {
            State = state;
            ApplicableOfferIdentifier = applicableOfferIdentifier;
            ApplicableOfferCategory = applicableOfferCategory;
            LastRenewedDate = lastRenewedDate;
            LastRenewalId = lastRenewalId;
            IsAutoRenewEnabled = isAutoRenewEnabled;
            ExpirationReason = expirationReason;
            RenewalDate = renewalDate;
            GracePeriodExpirationDate = gracePeriodExpirationDate;
            PriceIncreaseStatus = priceIncreaseStatus;
        }

        public override string ToString()
        {
            return string.Format("[BillingProductSubscriptionRenewalInfo] State: {0}, ApplicableOfferIdentifier: {1}, ApplicableOfferCategory: {2}, LastRenewedDate: {3}, LastRenewalId: {4}, IsAutoRenewEnabled: {5}, ExpirationReason: {6}, RenewalDate: {7}, GracePeriodExpirationDate: {8}, PriceIncreaseStatus: {9}", 
                State, 
                ApplicableOfferIdentifier, 
                ApplicableOfferCategory,
                LastRenewedDate,
                LastRenewalId,
                IsAutoRenewEnabled,
                ExpirationReason,
                RenewalDate,
                GracePeriodExpirationDate,
                GracePeriodExpirationDate,
                PriceIncreaseStatus);
        }
    }
}