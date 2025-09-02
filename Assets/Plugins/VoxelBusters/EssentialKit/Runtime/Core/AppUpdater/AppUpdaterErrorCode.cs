using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// The error codes for AppUpdater.
    /// </summary>
    public enum AppUpdaterErrorCode
    {
        /// <summary>
        /// The error code is unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// There is a network issue.
        /// </summary>
        NetworkIssue,

        /// <summary>
        /// The update is not compatible with the current version.
        /// </summary>
        UpdateNotCompatible,

        /// <summary>
        /// There is no update info fetched for the current version.
        /// </summary>
        UpdateInfoNotAvailable,

        /// <summary>
        /// The update is not available.
        /// </summary>
        UpdateNotAvailable,

        /// <summary>
        /// The update is already in progress.
        /// </summary>
        UpdateInProgress,

        /// <summary>
        /// The update has been cancelled.
        /// </summary>
        UpdateCancelled
    }
}