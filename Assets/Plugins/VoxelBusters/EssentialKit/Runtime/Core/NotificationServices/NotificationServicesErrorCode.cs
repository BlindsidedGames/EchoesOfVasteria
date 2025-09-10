namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Represents error codes for notification services.
    /// </summary>
    public enum NotificationServicesErrorCode
    {
        /// <summary>
        /// An unknown error occurred.
        /// </summary>
        Unknown,

        /// <summary>
        /// The permission for notifications is not available.
        /// </summary>
        PermissionNotAvailable,

        /// <summary>
        /// The notification trigger is not valid.
        /// </summary>
        TriggerNotValid,

        /// <summary>
        /// There was a configuration error.
        /// </summary>
        ConfigurationError,

        /// <summary>
        /// The scheduled time for the notification is not valid.
        /// </summary>
        ScheduledTimeNotValid
    }
}