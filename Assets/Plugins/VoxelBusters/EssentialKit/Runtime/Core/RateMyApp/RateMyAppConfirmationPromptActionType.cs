namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Enum representing the actions that can be taken when the rate my app confirmation prompt is shown.
    /// </summary>
    public enum RateMyAppConfirmationPromptActionType
    {
        /// <summary>
        /// The user has chosen to be reminded later.
        /// </summary>
        RemindLater,

        /// <summary>
        /// The user has chosen to cancel.
        /// </summary>
        Cancel,

        /// <summary>
        /// The user has chosen to rate the app.
        /// </summary>
        Ok,
    }
}