namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Contains information about the game service credentials which can be used to connect to game service from your backend.
    /// </summary>
    /// @ingroup GameServices
    public partial class ServerCredentials
    {
        #region Fields

        private IosPlatformProperties m_iosProperties;

        private AndroidPlatformProperties m_androidProperties;

        #endregion

        #region Properties

        /// <summary>
        /// Contains the iOS platform properties.
        /// </summary>
        public IosPlatformProperties IosProperties => m_iosProperties;

        /// <summary>
        /// Contains the Android platform properties.
        /// </summary>
        public AndroidPlatformProperties AndroidProperties => m_androidProperties;

        #endregion

        #region Constructors

        public ServerCredentials(IosPlatformProperties iosProperties = null, AndroidPlatformProperties androidProperties = null)
        {
            m_iosProperties     = iosProperties;
            m_androidProperties = androidProperties;
        }

        #endregion

        #region Utility

        public override string ToString()
        {
            return $"[IosProperties={IosProperties}, AndroidProperties={AndroidProperties}]";
        }

        #endregion
    }

}
