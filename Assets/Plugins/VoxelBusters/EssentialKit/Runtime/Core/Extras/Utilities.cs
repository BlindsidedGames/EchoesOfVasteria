using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.EssentialKit.ExtrasCore;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit
{
    /** @defgroup Utilities Utilities
    *   @brief Provides cross-platform interface to access commonly used native features.
    */
    /// <summary>
    /// Provides a cross-platform interface to access commonly used native features.
    /// </summary>
    /// @ingroup Utilities
    public static class Utilities
    {
        #region Static fields

        [ClearOnReload]
        private     static  INativeUtilityInterface     s_nativeInterface;

        #endregion

        #region Static methods

        /// @name Advanced Usage
        /// @{

        /// <summary>
        /// Initializes the utilities module with the given settings. This call is optional and only need to be called if you have custom settings to initialize this feature.
        /// </summary>
        /// <param name="settings">The settings to be used for initialization.</param>
        /// <remarks>
        /// The settings configure the utilities module.
        /// </remarks>
        public static void Initialize(UtilityUnitySettings settings)
        {
            // Configure interface
            s_nativeInterface       = NativeFeatureActivator.CreateInterface<INativeUtilityInterface>(ImplementationSchema.Extras, true);
        }
        /// @}

        /// <summary>
        /// Opens the app store website page associated with this app.
        /// </summary>
        public static void OpenAppStorePage()
        {
            // validate argument
            var     settings    = EssentialKitSettings.Instance.ApplicationSettings;
            string  appId       = settings.GetAppStoreIdForActiveOrSimulationPlatform();
            OpenAppStorePage(appId);
        }


        /// <summary>
        /// Opens the app store page associated with the specified application id.
        /// </summary>
        /// <description>
        /// For iOS platform, id is the value that identifies your app on App Store. 
        /// And on Android, it will be same as app's bundle identifier (com.example.test).
        /// </description>
        /// <param name="applicationIds">An array of string values, that holds app id's of each supported platform.</param>
        /// <example>
        /// The following code example shows how to open store link.
        /// <code>
        /// using UnityEngine;
        /// using System.Collections;
        /// using VoxelBusters.EssentialKit;
        /// 
        /// public class ExampleClass : MonoBehaviour 
        /// {
        ///     public void OpenStorePage ()
        ///     {
        ///         Utilities.OpenStoreLink(PlatformValue.Android("com.example.app"), PlatformValue.IOS("ios-app-id"));
        ///     }
        /// }
        /// </code>
        /// </example>
        public static void OpenAppStorePage(params RuntimePlatformConstant[] applicationIds)
        {
            // validate arguments
            Assert.IsNotNullOrEmpty(applicationIds, "applicationIds");

            try
            {
                var     targetValue  = RuntimePlatformConstantUtility.FindConstantForActivePlatform(applicationIds);
                if (targetValue == null)
                {
                    DebugLogger.LogWarning(EssentialKitDomain.Default, "Application id not found for current platform.");
                    return;
                }

                s_nativeInterface.OpenAppStorePage(targetValue.Value);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
        }

        /// <summary>
        /// Opens the app store website page associated with the specified application id.
        /// </summary>
        /// <param name="applicationId">Application id.</param>
        public static void OpenAppStorePage(string applicationId)
        {
            // validate arguments
            Assert.IsNotNullOrEmpty(applicationId, "Application id null/empty.");

            try
            {
                s_nativeInterface.OpenAppStorePage(applicationId);
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
        }

        /// <summary>
        /// Opens the app settings page associated with this app.
        /// </summary>
        /// <description>
        /// For iOS platform, this will open the settings app to the app's custom settings page.
        /// On Android, this will open app's settings page in the device's settings app.
        /// </description>
        public static void OpenApplicationSettings()
        {
            try
            {
                s_nativeInterface.OpenApplicationSettings();
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(EssentialKitDomain.Default, exception);
            }
        }

        #endregion
    }
}