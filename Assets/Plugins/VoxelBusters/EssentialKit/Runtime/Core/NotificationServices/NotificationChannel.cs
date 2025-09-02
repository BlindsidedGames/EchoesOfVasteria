namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Class representing a notification channel.
    /// </summary>
    /// @ingroup NotificationServices
    public class NotificationChannel
    {
        /// <summary>
        /// The unique identifier of the notification channel.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The name of the notification channel.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The description of the notification channel.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The old identifier of the notification channel. Useful to auto delete the old channel when new channel is created for the same purpose.
        /// </summary>
        public string OldId { get; set; }
    }
}