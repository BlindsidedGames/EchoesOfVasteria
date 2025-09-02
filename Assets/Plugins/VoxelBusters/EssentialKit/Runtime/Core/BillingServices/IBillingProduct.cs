using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Provides a cross-platform interface to access information about a product registered in Store.
    /// </summary>
    /// @ingroup BillingServices
    public interface IBillingProduct
    {
        #region Properties

        /// <summary>
        /// The string that identifies the product within Unity environment. (read-only)
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The string that identifies the product registered in the Store (platform specific). (read-only)
        /// </summary>
        string PlatformId { get; }

        /// <summary>
        /// The name of the product.
        /// </summary>
        string LocalizedTitle { get; }

        /// <summary>
        /// A description of the product.
        /// </summary>
        string LocalizedDescription { get; }

        /// <summary>
        /// The type of the product.
        /// </summary>
        BillingProductType Type { get; }

        /// <summary>
        /// The cost of the product.
        /// </summary>
        BillingPrice Price { get; }

        /// <summary>
        /// Determine whether the product can be purchased.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Gets the subscription information for a billing product.
        /// </summary>
        BillingProductSubscriptionInfo SubscriptionInfo { get; }

        IEnumerable<BillingProductOffer> Offers { get; }

        /// <summary>
        /// The Additional information associated with this product.
        /// </summary>
        IEnumerable<BillingProductPayoutDefinition> Payouts { get; }

        /// <summary>
        /// The cost of the product prefixed with local currency symbol.
        /// </summary>
        [Obsolete("Use LocalizedText property of Price(BillingPrice) property")]
        string LocalizedPrice { get; }

        /// <summary>
        /// The currency code of the price.
        /// </summary>
        [Obsolete("Use Code property of Price(BillingPrice) property")]
        string PriceCurrencyCode { get; }

        /// <summary>
        /// Gets the currency symbol for the price.
        /// </summary>
        [Obsolete("Use Symbol property of Price(BillingPrice) property")]
        string PriceCurrencySymbol { get; }

        /// <summary>
        /// Additional information associated with this product. This information is provided by the developer using <see cref="BillingProductDefinition.Tag"/> property.
        /// </summary>
        [System.Obsolete("This property is deprecated. Use Payout instead.", false)]
        object Tag { get; }

        #endregion
    }
}