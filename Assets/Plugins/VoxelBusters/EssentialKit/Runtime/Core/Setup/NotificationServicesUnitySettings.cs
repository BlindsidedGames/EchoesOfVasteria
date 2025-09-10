using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    [Serializable]
    public partial class NotificationServicesUnitySettings : SettingsPropertyGroup
    {
        #region Fields

        [SerializeField, EnumMaskField(typeof(NotificationPresentationOptions))]
        [Tooltip("Notification display options.")]
        private     NotificationPresentationOptions m_presentationOptions;

        [SerializeField]
        [HideInInspector]
        [Tooltip("If enabled, permission required to use location based notification will be added.")]
        private     bool                            m_usesLocationBasedNotification;

        [SerializeField]
        [Tooltip("External notification service used within the app.")]
        private     PushNotificationServiceType     m_pushNotificationServiceType;

        [SerializeField]
        [Tooltip("Android specific properties.")]
        private     AndroidPlatformProperties       m_androidProperties;

        #endregion

        #region Properties

        public NotificationPresentationOptions PresentationOptions => m_presentationOptions;

        public bool UsesLocationBasedNotification => m_usesLocationBasedNotification;

        public PushNotificationServiceType PushNotificationServiceType => m_pushNotificationServiceType;

        public AndroidPlatformProperties AndroidProperties => m_androidProperties;

        #endregion

        #region Constructors

        public NotificationServicesUnitySettings(bool isEnabled = true, NotificationPresentationOptions presentationOptions = NotificationPresentationOptions.Alert | NotificationPresentationOptions.Badge | NotificationPresentationOptions.Sound,
            bool usesLocationBasedNotification = false, PushNotificationServiceType pushNotificationServiceType = PushNotificationServiceType.Custom,
            AndroidPlatformProperties androidProperties = null)
            : base(isEnabled: isEnabled, name: NativeFeatureType.kNotificationServices)
        {
            // set properties
            m_presentationOptions           = presentationOptions;
            m_usesLocationBasedNotification = usesLocationBasedNotification;
            m_pushNotificationServiceType   = pushNotificationServiceType;
            m_androidProperties             = androidProperties ?? new AndroidPlatformProperties();
        }

        #endregion
    }
}