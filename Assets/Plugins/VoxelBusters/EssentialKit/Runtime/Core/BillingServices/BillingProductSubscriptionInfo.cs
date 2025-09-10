namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Represents the information about the billing product subscription.
    /// </summary>
    /// @ingroup BillingServices
    public class BillingProductSubscriptionInfo
    {
        /// <summary>
        /// Gets the group identifier of the product.
        /// </summary>
        public string GroupId { get; private set; }

        /// <summary>
        /// Gets the localized group title of the product.
        /// </summary>
        /// <remarks>
        /// The localized group title is the title of the product that is displayed to the user.
        /// It should be used when displaying the product information to the user.
        /// </remarks>
        public string LocalizedGroupTitle { get; private set; }

        /// <summary>
        /// Gets the level of the product.
        /// </summary>
        /// <remarks>
        /// The level of the product is used to distinguish between different products in the same group.
        /// For example, if a product is a subscription, the level can be used to identify the different tiers of the subscription, such as a basic, premium, or enterprise tier.
        /// </remarks>
        /// @note This property is not relevant on Android platform.
        public int Level { get; private set; }


        /// <summary>
        /// Gets the billing period of the product.
        /// </summary>
        /// <remarks>
        /// The billing period defines the duration of the subscription cycle.
        /// </remarks>
        public BillingPeriod Period { get; private set; }

        public BillingProductSubscriptionInfo(string groupId, string localizedGroupTitle, int level, BillingPeriod period)
        {
            GroupId = groupId;
            LocalizedGroupTitle = localizedGroupTitle;
            Level = level;
            Period = period;
        }

        public override string ToString()
        {
            return string.Format("[GroupId={0}, LocalizedGroupTitle={1}, Level={2}, Period={3}]", GroupId, LocalizedGroupTitle, Level, Period);
        }
    }
}