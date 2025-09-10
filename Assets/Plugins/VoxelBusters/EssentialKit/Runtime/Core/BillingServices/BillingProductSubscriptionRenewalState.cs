using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// State of the subscription renewal. 
    /// </summary>
    public enum BillingProductSubscriptionRenewalState
    {
        /// <summary>
        /// Unknown state of the subscription renewal.
        /// </summary>
        Unknown,
        /// <summary>
        /// User is currently subscribed to the product.
        /// </summary>
        Subscribed,
        /// <summary>
        /// User's subscription to the product has expired.
        /// </summary>
        Expired,
        /// <summary>
        /// The App Store is attempting to renew a subscription on behalf of the user.
        /// </summary>
        InBillingRetryPeriod,
        /// <summary>
        /// User's subscription is in the process of being canceled and will terminate at the end of the current billing cycle.
        /// </summary>
        InGracePeriod,
        /// <summary>
        /// User explicitly revoked the subscription.
        /// </summary>
        Revoked
    }
}