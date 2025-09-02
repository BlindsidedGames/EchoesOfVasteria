using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    [Serializable]
    public class RateMyAppUnitySettings  : SettingsPropertyGroup
    {
        #region Fields

        [SerializeField]
        [Tooltip("Automatically show the rating prompt when conditions are met. This presents as soon as conditions are met on app launch. Disable this if you want to control on when to show it with the help of RateMyApp.IsAllowedToRate + RateMyApp.AskForReviewNow methods.")]
        private     bool    m_autoShow = true;

        [Tooltip("Allow users to rate the app again in a new version, even if they have rated it previously. If enabled, users will be prompted to provide feedback on the new version.")]
        [SerializeField]
        private     bool    m_allowReratingForNewVersion = false;

        [Space]
        [SerializeField]
        [Tooltip("Confirmation dialog settings.")]
        private     RateMyAppConfirmationDialogSettings m_confirmationDialogSettings;

        [SerializeField]
        [Tooltip("Constraints to meet for rating.")]
        private     RateMyAppConstraints  m_contraintsSettings;

    
        #endregion

        #region Properties

        public bool AllowReratingForNewVersion => m_allowReratingForNewVersion;

        public bool AutoShow => m_autoShow;

        public RateMyAppConfirmationDialogSettings ConfirmationDialogSettings => m_confirmationDialogSettings;

        public RateMyAppConstraints ConstraintsSettings => m_contraintsSettings;

        #endregion

        #region Constructors

        public RateMyAppUnitySettings(bool isEnabled = true, RateMyAppConfirmationDialogSettings dialogSettings = null,
            RateMyAppConstraints defaultValidatorSettings = null, bool allowRatingAgainForNewVersion = false, bool autoShowWhenConditionsAreMet = true) : base(isEnabled: isEnabled, name: NativeFeatureType.kRateMyApp)
        {
            // set properties
            m_confirmationDialogSettings    = dialogSettings ?? new RateMyAppConfirmationDialogSettings();
            m_contraintsSettings     = defaultValidatorSettings ?? new RateMyAppConstraints();
            m_allowReratingForNewVersion = allowRatingAgainForNewVersion;
            m_autoShow  = autoShowWhenConditionsAreMet;
        }

        #endregion
    }
}