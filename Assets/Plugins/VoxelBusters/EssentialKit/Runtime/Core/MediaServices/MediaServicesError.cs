using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    public class MediaServicesError
    {
        public const string kDomain = "[Essential Kit] Media Services";



        public static Error Unknown(string description = null) => CreateError(
            code: (int)MediaServicesErrorCode.Unknown,
            description: description ?? "Unknown error."
        );


        public static Error PermissionNotAvailable(string description = null) => CreateError(
            code: (int)MediaServicesErrorCode.PermissionNotAvailable,
            description: description ?? "Permission not available."
        );


        public static Error UserCancelled(string description = null) => CreateError(
            code: (int)MediaServicesErrorCode.UserCancelled,
            description: description ?? "User cancelled."
        );


        public static Error DataNotAvailable(string description = null) => CreateError(
            code: (int)MediaServicesErrorCode.DataNotAvailable,
            description: description ?? "Data not available."
        );


        private static Error CreateError(int code, string description) => new Error(
            domain: kDomain,
            code: code,
            description: description);
    }
}