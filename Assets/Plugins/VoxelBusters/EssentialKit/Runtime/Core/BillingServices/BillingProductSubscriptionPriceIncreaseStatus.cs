namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Status of billing product subscription price increase action
    /// </summary>
    public enum BillingProductSubscriptionPriceIncreaseStatus : int
    {
        /// <summary>
        /// Unknown status of billing product subscription price increase action
        /// </summary>
        Unknown,
        /// <summary>
        /// No price increase is pending
        /// </summary>
        NoIncreasePending,
        /// <summary>
        /// User agreed to the price increase
        /// </summary>
        Agreed,
        /// <summary>
        /// Price increase is pending
        /// </summary>
        Pending
    }
}