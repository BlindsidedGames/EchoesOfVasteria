using System;

namespace VoxelBusters.EssentialKit
{
    public interface IBillingPayment
    {
        #region Properties

        /// <summary>
        /// The string that identifies the product within Unity environment (read-only). Potentially independent of store IDs.
        /// </summary>
        [Obsolete("This property is deprecated. Please use Product.Id(IBillingProduct.Id) in IBillingTransaction interface", true)]
        string ProductId { get; }

        /// <summary>
        /// The string that identifies the product registered in the Store (platform specific). (read-only)
        /// </summary>
        [Obsolete("This property is deprecated. Please use Product.PlatformId(IBillingProduct.PlatformId) in IBillingTransaction interface", true)]
        string ProductPlatformId { get; }

        /// <summary>
        /// The number of units to be purchased. This should be a non-zero number. This will be always 1 on Android as user can only set it from store purchase dialog window.
        /// </summary>
        [Obsolete("This property is deprecated. Please use RequestedQuantity in IBillingTransaction interface", true)]
        int Quantity { get; }

        /// <summary>
        /// An optional information provided by the developer at the time of initiating purchase.
        /// This can be used to identify a user or anything specific to the product purchase.
        /// @note This must be UUID v4 format identifier and can be generated with Guid.NewGuid().
        /// </summary>
        [Obsolete("This property is deprecated. Please use Tag in IBillingTransaction interface", true)]
        string Tag { get; }

        #endregion
    }
}