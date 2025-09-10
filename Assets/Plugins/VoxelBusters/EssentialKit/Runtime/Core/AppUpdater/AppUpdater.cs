using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;
using VoxelBusters.EssentialKit.AppUpdaterCore;

namespace VoxelBusters.EssentialKit
{
    /** 
     * @defgroup AppUpdater AppUpdater
     * @brief Provides cross-platform interface to facilitate updating the current version of the application.
     */

    /// <summary>
    /// The AppUpdater class provides cross-platform interface to facilitate updating the current version of the application.
    /// </summary>
    /// <description> 
    /// <para>
    /// On iOS platform, an alert is shown to let the user update the application.
    /// On Android, it uses native provided In-App update feature and if it fails to update, an alert is shown to let the user update the application.
    /// </para>
    /// </description>
    /// @ingroup AppUpdater  
    public static class AppUpdater
    {
        #region Static fields

        [ClearOnReload]
        private     static  INativeAppUpdaterInterface    s_nativeInterface    = null;

        #endregion

        #region Static properties

        public static AppUpdaterUnitySettings UnitySettings { get; private set; }

        #endregion

        #region Static methods

        public static bool IsAvailable()
        {
            return (s_nativeInterface != null) && s_nativeInterface.IsAvailable;
        }

        /// <summary>
        /// Initialize the AppUpdater module with the given settings. This call is optional and only need to be called if you have custom settings to initialize this feature.
        /// </summary>
        /// <param name="settings">The settings to be used for initialization.</param>
        /// <remarks>
        /// <para>
        /// The settings configure the behavior of the AppUpdater module.
        /// </para>
        /// </remarks>
        public static void Initialize(AppUpdaterUnitySettings settings)
        {
            Assert.IsArgNotNull(settings, nameof(settings));

            // Set default properties
            UnitySettings                       = settings;

            // Configure interface
            s_nativeInterface       = NativeFeatureActivator.CreateInterface<INativeAppUpdaterInterface>(ImplementationSchema.AppUpdater, true);
        }


        /// <summary>
        /// Request the latest available update information for the current version of the application.
        /// </summary>
        /// <param name="callback">The callback that will be executed after the operation has a result or error.</param>
        /// <remarks>
        /// <para>
        /// The callback will be invoked with an <see cref="AppUpdaterUpdateInfo"/> containing the result
        /// of the operation.
        /// </para>
        /// </remarks>
        public static void RequestUpdateInfo(EventCallback<AppUpdaterUpdateInfo> callback)
        {
            // Make request
            s_nativeInterface.RequestUpdateInfo((result, error) => CallbackDispatcher.InvokeOnMainThread(callback, result, error));
        }

        /// <summary>
        /// Shows a prompt to the user to update the app.
        /// </summary>
        /// <param name="options">The options to customize the prompt. <see cref="PromptUpdateOptions"/> instance can be created with <see cref="PromptUpdateOptions.Builder"/></param>
        /// <param name="callback">The callback that will be executed after the operation has a result or error.</param>
        /// <remarks>
        /// <para>
        /// The prompt will be dismissed when the user selects an option only if IsForceUpdate is false provided in the <see cref="PromptUpdateOptions"/>.
        /// </para>
        /// <para>
        /// The callback will receive a boolean value indicating whether the user chose to update the app.
        /// </para>
        /// </remarks>
        public static void PromptUpdate(PromptUpdateOptions options, EventCallback<float> callback)
        {
            s_nativeInterface.PromptUpdate(options, (result, error) => CallbackDispatcher.InvokeOnMainThread(callback, result, error));
        }
        
        #endregion
    }
}