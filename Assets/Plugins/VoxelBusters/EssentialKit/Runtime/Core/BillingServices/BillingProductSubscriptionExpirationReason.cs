namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Reason for billing product subscription expiration
    /// </summary>
    public enum BillingProductSubscriptionExpirationReason : int
    {
        /// <summary>
        /// No expiration reason available.
        /// </summary>
        None,

        /// <summary>
        /// Unknown expiration reason.
        /// </summary>
        Unknown,

        /// <summary>
        /// Auto-Renew disabled.
        /// </summary>
        AutoRenewDisabled,

        /// <summary>
        /// Expiration due to billing error.
        /// </summary>
        BillingError,

        /// <summary>
        /// Expiration due to user did not consent to price increase.
        /// </summary>
        DidNotConsentToPriceIncrease,

        /// <summary>
        /// Expiration due to product being unavailable.
        /// </summary>
        ProductUnavailable
    }


}