using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    public class GameServicesError
    {
        #region Constants

        public  const   string  kDomain = "[Essential Kit] Game Services";

        #endregion

        #region Properties

        public static Error Unknown(string description = null) => CreateError(
            code: (int)GameServicesErrorCode.Unknown,
            description: description ?? "Unknown error."
        );

        public static Error SystemError(string description = null) => CreateError(
            code: (int)GameServicesErrorCode.SystemError,
            description: description ?? "System error."
        );

        public static Error NetworkError(string description = null) => CreateError(
            code: (int)GameServicesErrorCode.NetworkError,
            description: "Network error."
        );  

        public static Error NotAllowed(string description = null) => CreateError(
            code: (int)GameServicesErrorCode.NotAllowed,
            description: description ?? "Not allowed."
        );

        public static Error DataNotAvailable(string description = null) => CreateError(
            code: (int)GameServicesErrorCode.DataNotAvailable,
            description: description ?? "Data not available."
        );  

        public static Error NotSupported(string description = null) => CreateError(
            code: (int)GameServicesErrorCode.NotSupported,
            description: description ?? "Not supported."
        );  

        public static Error ConfigurationError(string description = null) => CreateError(
            code: (int)GameServicesErrorCode.ConfigurationError,
            description: description ?? "Configuration error."
        );  

        public static Error InvalidInput(string description = null) => CreateError(
            code: (int)GameServicesErrorCode.InvalidInput,
            description: description ?? "Invalid input."
        );

        public static Error NotAuthenticated(string description = null) => CreateError(
            code: (int)GameServicesErrorCode.NotAuthenticated,
            description: description ?? "Not authenticated."        
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
