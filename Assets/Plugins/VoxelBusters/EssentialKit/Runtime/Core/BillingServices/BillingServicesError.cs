using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    public static class BillingServicesError
    {
        #region Constants

        public  const   string  kDomain = "[Essential Kit] Billing Services";

        #endregion

        #region Properties

        public static Error Unknown(string description = null) => CreateError(
            code: (int)BillingServicesErrorCode.Unknown,
            description: description ?? "Unknown error."
        );

        public static Error NetworkError(string description = null) => CreateError(
            code: (int)BillingServicesErrorCode.NetworkError,
            description: "Network error."
        );  
        
        public static Error SystemError(string description = null) => CreateError(
            code: (int)BillingServicesErrorCode.SystemError,
            description: description ?? "System error."
        );

        public static Error StoreNotInitialized(string description = null) => CreateError(
            code: (int)BillingServicesErrorCode.StoreNotInitialized,
            description: description ?? "Store not initialized."
        );

        public static Error StoreIsBusy(string description = null) => CreateError(
            code: (int)BillingServicesErrorCode.StoreIsBusy,
            description: description ?? "Store is busy processing another request."
        );

        public static Error UserCancelled(string description = null) => CreateError(
            code: (int)BillingServicesErrorCode.UserCancelled,
            description: description ?? "User cancelled."
        );

        public static Error OfferNotApplicable(string description = null) => CreateError(
            code: (int)BillingServicesErrorCode.OfferNotApplicable,
            description: description ?? "Offer not applicable."
        );

        public static Error OfferNotValid(string description = null) => CreateError(
            code: (int)BillingServicesErrorCode.OfferNotValid,
            description: description ?? "Offer not valid."
        );

        public static Error QuantityNotValid(string description = null) => CreateError(
            code: (int)BillingServicesErrorCode.QuantityNotValid,
            description:description ?? "Quantity not valid."
        );

        public static Error ProductNotAvailable(string description = null) => CreateError(
            code: (int)BillingServicesErrorCode.ProductNotAvailable,
            description: description ?? "Product not available."
        );

        public static Error ProductOwned(string description = null) => CreateError(
            code: (int)BillingServicesErrorCode.ProductOwned,
            description: description ?? "Product already owned."
        );

        public static Error FeatureNotAvailable(string description = null) => CreateError(
            code: (int)BillingServicesErrorCode.FeatureNotAvailable,
            description: description ?? "Feature not available."
        );

        #endregion

        #region Static methods

        private static Error CreateError(int code, string description) => new Error(
            domain: kDomain,
            code: code,
            description: description);

        #endregion
    }
}