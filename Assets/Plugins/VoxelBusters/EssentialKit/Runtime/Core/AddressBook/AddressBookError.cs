using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    public static class AddressBookError
    {
        #region Constants

        public  const   string  kDomain = "[Essential Kit] Address Book";

        #endregion

        #region Properties

        public static Error Unknown { get; } = CreateError(
            code: (int)AddressBookErrorCode.Unknown,
            description: "Unknown error."
        );

        public static Error PermissionDenied { get; } = CreateError(
            code: (int)AddressBookErrorCode.PermissionDenied,
            description: "Permission denied by the user."
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