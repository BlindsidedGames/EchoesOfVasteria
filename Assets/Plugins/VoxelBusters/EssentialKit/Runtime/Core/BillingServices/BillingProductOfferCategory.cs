namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Represents a category of a billing product offer.
    /// </summary>
    public enum BillingProductOfferCategory : int
    {
        /// <summary>
        /// Unknown offer category.
        /// </summary>
        Unknown,
        /// <summary>
        /// Offer category for introductory offers.
        /// </summary>
        Introductory,
        /// <summary>
        /// Offer category for promotional offers.
        /// </summary>
        Promotional,
        /// <summary>
        /// Offer category for code-based offers.
        /// </summary>
        Code
    }
}