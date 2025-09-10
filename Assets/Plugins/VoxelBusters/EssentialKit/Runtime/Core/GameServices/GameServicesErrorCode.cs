namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Enum used to specify the error code for game services operations.
    /// </summary>
    public enum GameServicesErrorCode
    {
        /// <summary>
        /// The error code indicating that the cause of the error could not be
        /// determined.
        /// </summary>
        Unknown,
        /// <summary>
        /// The error code indicating that an error occurred on the server.
        /// </summary>
        SystemError,
        /// <summary>
        /// The error code indicating that a network error occurred.
        /// </summary>
        NetworkError,
        /// <summary>
        /// The error code indicating that the operation was not allowed.
        /// </summary>
        NotAllowed,
        /// <summary>
        /// The error code indicating that the data being requested is not available.
        /// </summary>
        DataNotAvailable,
        /// <summary>
        /// The error code indicating that the feature is not supported or enabled.
        /// </summary>
        NotSupported,
        /// <summary>
        /// The error code indicating that the configuration is invalid.
        /// </summary>
        ConfigurationError,
        /// <summary>
        /// The error code indicating that the input was invalid.
        /// </summary>
        InvalidInput,
        /// <summary>
        /// The error code indicating that the user is not authenticated.
        /// </summary>
        NotAuthenticated
    }
}
