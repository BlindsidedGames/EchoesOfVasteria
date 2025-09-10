using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelBusters.EssentialKit
{
    public partial class WebViewUnitySettings
    {
        [Serializable]
        public class AndroidPlatformProperties 
        {
            #region Fields

            [SerializeField]
            [Tooltip ("Enabling this will allow your app to access camera from webview")]
            private     bool    m_usesCamera;

            [SerializeField]
            [Tooltip("Enabling this will allow your app to access microphone from webview")]
            private     bool    m_usesMicrophone;

            [SerializeField]
            [Tooltip("Enabling this will allow you to dismiss webview when back navigation button on the device is pressed")]
            private     bool    m_allowBackNavigationKey;

            #endregion

            #region Properties

            public bool UsesCamera => m_usesCamera;

            public bool UsesMicrophone => m_usesMicrophone;

            public bool AllowBackNavigationKey => m_allowBackNavigationKey;

            #endregion

            #region Constructors

            public AndroidPlatformProperties(bool usesCamera = false, bool usesMicrophone = false,
                bool allowBackNavigationKey = true)
            {
                // set properties
                m_usesCamera                = usesCamera;
                m_usesMicrophone            = usesMicrophone;
                m_allowBackNavigationKey    = allowBackNavigationKey;
            }

            #endregion
        }
    }
}