using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    public class SharingServicesError
    {
        public const string kDomain = "[Essential Kit] Notification Services";

        public static Error Unknown(string description = null) => CreateError(
            code: (int)SharingServicesErrorCode.Unknown,
            description: description ?? "Unknown error."
        );

        public static Error PermissionNotAvailable(string description = null) => CreateError(
            code: (int)SharingServicesErrorCode.AttachmentNotValid,
            description: description ?? "Attacment not valid."
        );

        private static Error CreateError(int code, string description) => new Error(
            domain: kDomain,
            code: code,
            description: description);
    }
}