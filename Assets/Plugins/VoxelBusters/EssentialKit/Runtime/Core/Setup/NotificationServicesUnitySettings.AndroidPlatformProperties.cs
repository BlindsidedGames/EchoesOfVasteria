using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    public partial class NotificationServicesUnitySettings 
    {
        [Serializable]
        public class AndroidPlatformProperties 
        {
            #region Fields

            [HideInInspector]
            [SerializeField]
            [Tooltip("If enabled, app will use big style notification.")]
            private     bool            m_needsBigStyle; 

            [SerializeField]
            [Tooltip("If enabled, device vibrates on receiving a notification.")]
            private     bool            m_allowVibration; 
            
            [SerializeField]
            [Tooltip("The texture used as small icon in post Android L Devices.")]
            private     Texture2D       m_whiteSmallIcon;

            [SerializeField]
            [Tooltip("The texture used as small icon in pre Android L Devices.")]
            private     Texture2D       m_colouredSmallIcon;

            [SerializeField]
            [Tooltip("If enabled, notifications are displayed even when app is foreground.")]
            private     bool            m_allowNotificationDisplayWhenForeground;

            [SerializeField, DefaultValue("#FFFFFF")]
            [Tooltip("If set, the value will be used as accent color for notification.")]
            private     string          m_accentColor;

            [SerializeField]
            [Tooltip("Array of payload keys.")]
            private     Keys            m_payloadKeys;

            [Space]
            [Header("Advanced Settings")]
            [Space]
            [Header("Exact timing settings - Can affect battery optimisation and focus mode")]
            [SerializeField]
            [Tooltip("Enable if you need notifications at exact time (Make sure your app is eligible for using this feature.). Enabling this will NOT have energy saving capabilities.")]
            private     bool            m_allowExactTimeScheduling;

            [SerializeField]
            [Tooltip("Enable this if you want exact timing notifications to even ignore doze mode. This may consume user's device battery and not recommended for most apps.")]
            private     bool            m_canIgnoreDozeMode;



            #endregion

            #region Properties

            public bool NeedsBigStyle => m_needsBigStyle;

            public bool AllowVibration => m_allowVibration;

            public Texture2D WhiteSmallIcon => m_whiteSmallIcon;

            public Texture2D ColouredSmallIcon => m_colouredSmallIcon;

            public Keys PayloadKeys => m_payloadKeys;

            public bool AllowNotificationDisplayWhenForeground => m_allowNotificationDisplayWhenForeground;

            public string AccentColor => PropertyHelper.GetValueOrDefault(
                    instance: this,
                    fieldAccess: (field) => field.m_accentColor,
                    value: m_accentColor);

            public bool AllowExactTimeScheduling => m_allowExactTimeScheduling;

            public bool AllowExactTimeSchedulingIgnoringDozeMode => m_canIgnoreDozeMode;


            #endregion

            #region Constructors

            public AndroidPlatformProperties(bool needsBigStyle = false, bool allowVibration = true,
                Texture2D whiteSmallIcon = null, Texture2D colouredSmallIcon = null,
                bool allowNotificationDisplayWhenForeground = false, string accentColor = null,
                Keys payloadKeys = null, bool allowExactTimeScheduling = false, bool canIgnoreDozeMode = false)
            {
                // set properties
                m_needsBigStyle                             = needsBigStyle;
                m_allowVibration                            = allowVibration;
                m_whiteSmallIcon                            = whiteSmallIcon;
                m_colouredSmallIcon                         = colouredSmallIcon;
                m_allowNotificationDisplayWhenForeground    = allowNotificationDisplayWhenForeground;
                m_accentColor                               = PropertyHelper.GetValueOrDefault(
                    instance: this,
                    fieldAccess: (field) => field.m_accentColor,
                    value: accentColor);
                m_payloadKeys                               = payloadKeys ?? new Keys();
                m_allowExactTimeScheduling                  = allowExactTimeScheduling;
                m_canIgnoreDozeMode                         = canIgnoreDozeMode;
            }

            #endregion

            #region Nested types

            [Serializable]
            public class Keys
            {
                #region Fields

                [SerializeField, DefaultValue("content_title")]
                [Tooltip("The key used to capture content title property from the payload.")]
                private     string          m_contentTitle;

                [SerializeField, DefaultValue("content_text")]
                [Tooltip("The key used to capture content text property from the payload.")]
                private     string          m_contentText;

                [SerializeField, DefaultValue("ticker_text")]
                [Tooltip("The key used to capture ticker text property from the payload.")]
                private     string          m_tickerText;

                [SerializeField, DefaultValue("user_info")]
                [Tooltip("The key used to capture user info dictionary from the payload.")]
                private     string          m_userInfo;

                [SerializeField, DefaultValue("tag")]
                [Tooltip("The key used to capture tag property from the payload.")]
                private     string          m_tag;

                [SerializeField, DefaultValue("badge")]
                [Tooltip("The key used to capture badge property from the payload.")]
                private     string          m_badge;

                [SerializeField, DefaultValue("priority")]
                [Tooltip("The key used to capture priority property from the payload.")]
                private     string          m_priority;

                [SerializeField, DefaultValue("sound")]
                [Tooltip("The key used to capture sound property from the payload.")]
                private     string          m_sound;

                [SerializeField, DefaultValue("big_picture")]
                [Tooltip("The key used to capture big picture property from the payload.")]
                private     string          m_bigPicture;

                [SerializeField, DefaultValue("large_icon")]
                [Tooltip("The key used to capture large icon property from the payload.")]
                private     string          m_largeIcon;

                #endregion

                #region Properties

                public string TickerTextKey => PropertyHelper.GetValueOrDefault(
                        instance: this,
                        fieldAccess: (field) => field.m_tickerText,
                        value: m_tickerText);

                public string ContentTitleKey => PropertyHelper.GetValueOrDefault(
                        instance: this,
                        fieldAccess: (field) => field.m_contentTitle,
                        value: m_contentTitle);

                public string ContentTextKey => PropertyHelper.GetValueOrDefault(
                        instance: this,
                        fieldAccess: (field) => field.m_contentText,
                        value: m_contentText);

                public string UserInfoKey => PropertyHelper.GetValueOrDefault(
                        instance: this,
                        fieldAccess: (field) => field.m_userInfo,
                        value: m_userInfo);

                public string TagKey => PropertyHelper.GetValueOrDefault(
                        instance: this,
                        fieldAccess: (field) => field.m_tag,
                        value: m_tag);

                public string BadgeKey => PropertyHelper.GetValueOrDefault(
                        instance: this,
                        fieldAccess: (field) => field.m_badge,
                        value: m_badge);

                public string PriorityKey => PropertyHelper.GetValueOrDefault(
                        instance: this,
                        fieldAccess: (field) => field.m_priority,
                        value: m_priority);

                public string SoundFileNameKey => PropertyHelper.GetValueOrDefault(
                        instance: this,
                        fieldAccess: (field) => field.m_sound,
                        value: m_sound);

                public string BigPictureKey => PropertyHelper.GetValueOrDefault(
                        instance: this,
                        fieldAccess: (field) => field.m_bigPicture,
                        value: m_bigPicture);

                public string LargeIconKey => PropertyHelper.GetValueOrDefault(
                        instance: this,
                        fieldAccess: (field) => field.m_largeIcon,
                        value: m_largeIcon);

                #endregion

                #region Constructors

                public Keys(string tickerText = null, string contentTitle = null,
                    string contentText = null, string userInfo = null,
                    string tag = null, string badge = null, string priority = null, string sound = null,
                    string bigPicture = null, string largeIcon = null)
                {
                    // set properties
                    m_tickerText        = PropertyHelper.GetValueOrDefault(
                        instance: this,
                        fieldAccess: (field) => field.m_tickerText,
                        value: tickerText);
                    m_contentTitle      = PropertyHelper.GetValueOrDefault(
                        instance: this,
                        fieldAccess: (field) => field.m_contentTitle,
                        value: contentTitle);
                    m_contentText       = PropertyHelper.GetValueOrDefault(
                        instance: this,
                        fieldAccess: (field) => field.m_contentText,
                        value: contentText);
                    m_userInfo          = PropertyHelper.GetValueOrDefault(
                        instance: this,
                        fieldAccess: (field) => field.m_userInfo,
                        value: userInfo);
                    m_tag               = PropertyHelper.GetValueOrDefault(
                        instance: this,
                        fieldAccess: (field) => field.m_tag,
                        value: tag);
                    m_priority          = PropertyHelper.GetValueOrDefault(
                        instance: this,
                        fieldAccess: (field) => field.m_priority,
                        value: priority);
                    m_badge             = PropertyHelper.GetValueOrDefault(
                        instance: this,
                        fieldAccess: (field) => field.m_badge,
                        value: badge);
                    m_sound             = PropertyHelper.GetValueOrDefault(
                        instance: this,
                        fieldAccess: (field) => field.m_sound,
                        value: sound);
                    m_bigPicture        = PropertyHelper.GetValueOrDefault(
                        instance: this,
                        fieldAccess: (field) => field.m_bigPicture,
                        value: bigPicture);
                    m_largeIcon         = PropertyHelper.GetValueOrDefault(
                        instance: this,
                        fieldAccess: (field) => field.m_largeIcon,
                        value: largeIcon);
                }

                #endregion
            }

            #endregion
        }
    }
}