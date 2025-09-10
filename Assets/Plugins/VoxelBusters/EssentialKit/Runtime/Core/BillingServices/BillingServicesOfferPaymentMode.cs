namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Payment mode of Billing Product Subscription
    /// </summary>
    public enum BillingProductOfferPaymentMode : int
    {
        /// <summary>
        /// Unknown payment mode.
        /// </summary>
        Unknown,

        /// <summary>
        /// Payment mode for a free trial.
        /// </summary>
        FreeTrial,

        /// <summary>
        /// Payment mode for pay as you go subscriptions.
        /// </summary>
        PayAsYouGo,

        /// <summary>
        /// Payment mode for pay up front subscriptions.
        /// </summary>
        PayUpFront
    }
}