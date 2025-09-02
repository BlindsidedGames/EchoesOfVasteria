namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// The status of the application update.
    /// </summary>
    public enum AppUpdaterUpdateStatus
    {
        /// <summary>
        /// The application update status is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The application is not up to date and needs to be updated.
        /// </summary>
        Available = 1,

        /// <summary>
        /// The application is up to date.
        /// </summary>
        NotAvailable = 2,

        /// <summary>
        /// The application is currently being updated.
        /// </summary>
        InProgress = 3,
        
        Downloaded = 4
    }
}