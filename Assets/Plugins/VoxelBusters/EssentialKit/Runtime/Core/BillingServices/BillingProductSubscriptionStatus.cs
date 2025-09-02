
using System;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Represents the subscription status of a billing product.
    /// </summary>
    /// @ingroup BillingServices
    public class BillingProductSubscriptionStatus
    {
        /// <summary>
        /// Gets the group ID of the subscription.
        /// </summary>
        public string GroupId { get; private set; }

        /// <summary>
        /// Gets the renewal information of the subscription.
        /// </summary>
        public BillingProductSubscriptionRenewalInfo RenewalInfo { get; private set; }

        /// <summary>
        /// Gets or sets the expiration date of the subscription.
        /// Note: On Android, it's not possible to get the last renewal date, so this is skipped until provided.
        /// </summary>  
        private DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the subscription is upgraded.
        /// Note: On Android, this can be determined by checking all available subscriptions in the group sorted by time.
        /// </summary>
        private bool IsUpgraded { get; set; }

        /// <summary>
        /// Gets or sets the applied offer ID for the subscription.
        /// </summary>
        private string AppliedOfferId { get; set; }

        /// <summary>
        /// Gets or sets the category of the applied offer for the subscription.
        /// </summary>
        private BillingProductOfferCategory? AppliedOfferCategory { get; set; }

        internal BillingProductSubscriptionStatus(string groupId, BillingProductSubscriptionRenewalInfo renewalInfo = null, DateTime? expirationDate = null, bool isUpgraded = false, string appliedOfferIdentifier = null, BillingProductOfferCategory? appliedOfferCategory = null)
        {
            GroupId = groupId;
            RenewalInfo = renewalInfo;
            ExpirationDate = expirationDate;
            IsUpgraded = isUpgraded;
            AppliedOfferId = appliedOfferIdentifier;
            AppliedOfferCategory = appliedOfferCategory;
        }

        public override string ToString()
        {
            return string.Format("[{0}] GroupId: {1}, RenewalInfo: {2}, ExpirationDate: {3}, IsUpgraded: {4}, AppliedOfferId: {5}, AppliedOfferCategory: {6}", 
                GetType().Name, 
                GroupId, 
                RenewalInfo, 
                ExpirationDate, 
                IsUpgraded, 
                AppliedOfferId, 
                AppliedOfferCategory);
        }
    }
}