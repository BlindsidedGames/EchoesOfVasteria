using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{

    /// <summary>
    /// Describes the error code which can occur when requesting for payment.</summary>
    [IncludeInDocs]
    public enum BillingServicesErrorCode : int
    {
        /// <summary>
        /// Unknown error occurred.
        /// </summary>
        Unknown,
        /// <summary>
        /// Network error occurred.
        /// </summary>
        NetworkError,
        /// <summary>
        /// System error occurred.
        /// </summary>
        SystemError,
        /// <summary>
        /// Billing service is not available.
        /// </summary>
        BillingNotAvailable,
        /// <summary>
        /// Store is not initialized.
        /// </summary>
        StoreNotInitialized,
        /// <summary>
        /// Store is busy processing another request.
        /// </summary>
        StoreIsBusy,
        /// <summary>
        /// User cancelled the payment.
        /// </summary>
        UserCancelled,
        /// <summary>
        /// Offer is not applicable.
        /// </summary>
        OfferNotApplicable,
        /// <summary>
        /// Offer is not valid.
        /// </summary>
        OfferNotValid,
        /// <summary>
        /// Quantity is not valid.
        /// </summary>
        QuantityNotValid,
        /// <summary>
        /// Product is not available.
        /// </summary>
        ProductNotAvailable,
        /// <summary>
        /// Product is already owned.
        /// </summary>
        ProductOwned,
        /// <summary>
        /// Feature is not available.
        /// </summary>
        FeatureNotAvailable
    }
}