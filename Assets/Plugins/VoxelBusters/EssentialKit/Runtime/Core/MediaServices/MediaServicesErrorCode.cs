namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Contains a list of error codes that can occur while using the Media Services.
    /// </summary>
    public enum MediaServicesErrorCode
    {
        /// <summary>
        /// An unknown error occurred.
        /// </summary>
        Unknown,

        /// <summary>
        /// The permission to access the camera or photo library is not available.
        /// </summary>
        PermissionNotAvailable,

        /// <summary>
        /// The user cancelled the operation.
        /// </summary>
        UserCancelled,

        /// <summary>
        /// The required data is not available.
        /// </summary>
        DataNotAvailable
    }
}