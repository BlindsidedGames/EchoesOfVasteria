using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    public partial class GameServicesUnitySettings
    {
        [Serializable]
        public class AndroidPlatformProperties 
        {
            #region Fields

            [SerializeField]
            [Tooltip ("Your application id in Google Play services.")]
            private     string      m_playServicesApplicationId;

            [Header("External Server Control")]
            [SerializeField]
            [Tooltip("Your Server Client Id for getting external authentication credentials (Make sure its from a web app)")]
            private     string      m_serverClientId;

            /// <summary>
            /// If enabled, allows the use of refresh tokens to obtain long-lived access to Google Play Services.
            /// </summary>
            /// <remarks>
            /// This is useful for long-lived access to Google Play Services, such as when offline access is required.
            /// </remarks>
            [SerializeField]
            [Tooltip("If enabled, allows the use of refresh tokens to obtain long-lived access to Google Play Services.")]
            private bool m_forceRefreshToken = true;

            [Tooltip("Text formats used to derive completed achievement description. Note: Achievement title will be inserted in place of token \'#\'.")]
            private     string[]    m_achievedDescriptionFormats;

            [Header("Extra Settings")]
            [SerializeField]
            [Tooltip("If enabled, alert dialog is shown automatically on error(for ex: signin failure).")]
            private     bool        m_showErrorDialogs;

            [SerializeField]
            [Tooltip("If enabled, auto-initialization of play games services happens on app start(But still to sign-in you need to call Authenticate). If disabled, initialization happens once you call Authenticate method. This is useful when you handling GDPR handling based on age gating.")]
            private     bool        m_enableAutoInitialization = true;
            
            #endregion

            #region Properties

            public string PlayServicesApplicationId => PropertyHelper.GetValueOrDefault(m_playServicesApplicationId);
        
            public string ServerClientId => PropertyHelper.GetValueOrDefault(m_serverClientId)?.Trim();

            public string[] AchievedDescriptionFormats => m_achievedDescriptionFormats;

            public bool ShowErrorDialogs => m_showErrorDialogs;

            public bool ForceRefreshToken => m_forceRefreshToken;
            
            public bool EnableAutoInitialization => m_enableAutoInitialization;
            
            #endregion

            #region Constructors

            public AndroidPlatformProperties(string playServicesApplicationId = null, string serverClientId = null,
                string[] achievedDescriptionFormats = null, bool showErrorDialogs = true,
                bool forceRefreshToken  = true, bool enableAutoInitialization = true)
            {
                // set properties
                m_playServicesApplicationId     = playServicesApplicationId;
                m_serverClientId                = serverClientId;
                m_achievedDescriptionFormats    = achievedDescriptionFormats ?? new string[] 
                {
                    "Awesome! Achievement # completed."
                };
                m_showErrorDialogs              = showErrorDialogs;
                m_forceRefreshToken             = forceRefreshToken;
                m_enableAutoInitialization      = enableAutoInitialization;
            }

            #endregion
        }
    }
}