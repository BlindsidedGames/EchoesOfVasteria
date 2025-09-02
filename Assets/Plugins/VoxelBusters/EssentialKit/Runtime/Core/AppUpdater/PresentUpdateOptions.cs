namespace VoxelBusters.EssentialKit
{
    /// <summary>
    /// Represents the options for prompting an update to the user.
    /// Use the <see cref="PromptUpdateOptions.Builder"/> to create an instance of this class.
    /// </summary>
    /// @ingroup AppUpdater  
    public class PromptUpdateOptions
    {
        /// <summary>
        /// Gets a value indicating whether the update is mandatory.
        /// </summary>
        public bool IsForceUpdate { get; private set; }

        /// <summary>
        /// Gets the title of the update prompt.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Gets the message of the update prompt.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        ///  Gets a value indicating whether the update can be installed if it has already been downloaded.
        ///  @note Default value is true
        ///  @note If force update is enabled, this option will be considered as true.
        /// </summary>
        public bool AllowInstallationIfDownloaded { get; private set; } = true;

        private PromptUpdateOptions() {}

        /// <summary>
        /// Builder class for constructing <see cref="PromptUpdateOptions"/> instances.
        /// </summary>
        public class Builder
        {
            private PromptUpdateOptions m_options;

            /// <summary>
            /// Initializes a new instance of the <see cref="Builder"/> class.
            /// </summary>
            public Builder()
            {
                m_options = new PromptUpdateOptions();
            }

            /// <summary>
            /// Sets whether the update is mandatory.
            /// </summary>
            /// <param name="isForceUpdate">True if the update is mandatory; otherwise, false.</param>
            /// <returns>The current builder instance.</returns>
            public Builder SetIsForceUpdate(bool isForceUpdate)
            {
                m_options.IsForceUpdate = isForceUpdate;
                return this;
            }

            /// <summary>
            /// Sets the title of the update prompt.
            /// </summary>
            /// <param name="promptTitle">The title of the prompt.</param>
            /// <returns>The current builder instance.</returns>
            public Builder SetPromptTitle(string promptTitle)
            {
                m_options.Title = promptTitle;
                return this;
            }

            /// <summary>
            /// Sets the message of the update prompt.
            /// </summary>
            /// <param name="message">The message of the prompt.</param>
            /// <returns>The current builder instance.</returns>
            public Builder SetPromptMessage(string message)
            {
                m_options.Message = message;
                return this;
            }
            
            
            /// <summary>
            /// Sets whether the update can be installed if it has already been downloaded.
            /// </summary>
            /// <param name="allowInstallationIfDownloaded">True if the update can be installed when downloaded; otherwise, false. Default value is true.</param>
            /// @note If force update is enabled, this option will be considered as true.
            /// <returns>The current builder instance.</returns>
            public Builder SetAllowInstallationIfDownloaded(bool allowInstallationIfDownloaded)
            {
                m_options.AllowInstallationIfDownloaded = allowInstallationIfDownloaded;
                return this;
            }

            /// <summary>
            /// Builds and returns the <see cref="PromptUpdateOptions"/> instance.
            /// </summary>
            /// <returns>The constructed <see cref="PromptUpdateOptions"/> instance.</returns>
            public PromptUpdateOptions Build()
            {
                return m_options;
            }
        }
    }
}