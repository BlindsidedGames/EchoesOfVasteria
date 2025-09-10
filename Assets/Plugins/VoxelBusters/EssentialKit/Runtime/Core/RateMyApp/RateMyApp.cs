using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.EssentialKit.NativeUICore;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit
{
    /** @defgroup RateMyApp Rate My App
    *   @brief Provides option for user to rate the app.
    */

    /// <summary>
    /// The RateMyApp class provides an unique way to prompt user to review the app. 
    /// </summary>
    /// <description>
    /// By default, prompt system makes use of configuration available in RateMyApp section of Essential Kit Settings. 
    /// These values can be adjusted according to developer preference.
    /// </description>
    /// @ingroup RateMyApp
    public class RateMyApp
    {
        #region Static properties

        public    static RateMyAppUnitySettings       UnitySettings;

        [ClearOnReload]
        private     static RateMyAppController   s_controller   = null;

        #endregion

        #region Static events

        /// <summary>
        /// Event triggered when user clicks on any of the buttons in the confirmation dialog.
        /// </summary>
        public static event Callback<RateMyAppConfirmationPromptActionType> OnConfirmationPromptAction;

        #endregion

        #region Static methods

        /// @name Advanced Usage
        /// @{
        
        /// <summary>
        /// Initialize the RateMyApp module with the given settings. This call is optional and only need to be called if you have custom settings to initialize this feature.
        /// </summary>
        public static void Initialize(RateMyAppUnitySettings settings, string storeId)
        {
            Assert.IsArgNotNull(settings, nameof(settings));

            // Set properties
            UnitySettings           = settings;

            s_controller            = new RateMyAppController(settings, storeId);
            s_controller.SetConfirmationDialogCallback(  onAction: (selectedAction) => OnConfirmationPromptAction?.Invoke(selectedAction));

            //Debug.LogError("Reloading... : Check here added to  ");
        }
        /// @}

        /// <summary>
        /// Checks whether it is allowed to show the rate my app prompt or not. Returns true when the constraints set in settings are met, else false.
        /// </summary>
        /// <returns></returns>
        public static bool IsAllowedToRate()
        {
            return s_controller.CanShow();
        }

        /// <summary>
        /// Immediately prompts user to review. This method ignores the conditions to be satisfied and presents the confirmation dialog(optional) followed by native store review dialog(based on quota).
        /// </summary>
        /// <param name="skipConfirmation">If set to <c>true</c> skip confirmation prompt.</param>
        public static void AskForReviewNow(bool skipConfirmation = false)
        {
            // check whether feature is available
            if (UnitySettings == null || !UnitySettings.IsEnabled)
            {
                DebugLogger.LogError(EssentialKitDomain.Default, "Feature is not active or not yet initialised.");
                return;
            }

            s_controller.Show(skipConfirmation);
        }

        #endregion
    }
}