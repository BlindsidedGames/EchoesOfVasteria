using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Defines the categories for billing product payouts.
    /// </summary>
    public enum BillingProductPayoutCategory
    {
        /// <summary>
        /// Represents the category of currency payout.
        /// </summary>
        Currency = 1,

        /// <summary>
        /// Represents the category of item payout.
        /// </summary>
        Item,

        /// <summary>
        /// Represents all other categories of payout.
        /// </summary>
        Other = 1000
    }
}