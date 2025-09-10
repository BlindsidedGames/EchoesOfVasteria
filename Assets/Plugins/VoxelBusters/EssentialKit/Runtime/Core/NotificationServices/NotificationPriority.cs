namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Specifies the priority levels for notifications, determining how they are delivered to the user.
    /// </summary>
    public enum NotificationPriority
    {
        /// <summary>
        /// Low priority. Can be silent or with no disturbance.
        /// </summary>
        Low = 0,

        /// <summary>
        /// Medium priority. 
        /// (Default) Can make noise, alert in banner, but respects battery optimization settings or focus mode.
        /// </summary>
        Medium = 1,

        /// <summary>
        /// High priority. 
        /// Can make noise, alert in banner and can be visually intruding ("May" break-through doze/battery optimization or focus modes).
        /// </summary>
        High = 2,

        /// <summary>
        /// Max priority. 
        /// Can make noise, alert in banner and visually intruding ("Must" break-through doze or focus modes).
        /// </summary>
        Max = 3
    }
}