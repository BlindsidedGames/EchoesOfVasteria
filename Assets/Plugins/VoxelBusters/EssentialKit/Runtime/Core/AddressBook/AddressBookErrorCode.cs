using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    [IncludeInDocs]
    /// <summary>
    /// Contains the list of errors codes that can occur during address book operations.
    /// </summary>
    public enum AddressBookErrorCode : int
    {
        /// <summary>
        /// Unknown error occurred.
        /// </summary>
        Unknown,
        /// <summary>
        /// Permission to access the address book was denied.
        /// </summary>
        PermissionDenied
    }
}