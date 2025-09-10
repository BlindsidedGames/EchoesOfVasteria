using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit
{

    [Serializable]
    public class AdditionalPlatformSupportSettings
    {
        #region Fields
        [SerializeField]
        private bool m_supportAndroidPc = false;
        #endregion

        #region Properties

        public bool SupportAndroidPc => m_supportAndroidPc;

        #endregion

        #region Constructors

        public AdditionalPlatformSupportSettings(bool supportAndroidPc = false)
        {
            m_supportAndroidPc = supportAndroidPc;
        }

        #endregion
    }

    [Serializable]
    public class ApplicationSettings
    {
        #region Fields

        [SerializeField]
        private DebugLogger.LogLevel m_logLevel;

        [SerializeField]
        [Tooltip("Stores the registration ids of this app.")]
        private RuntimePlatformConstantSet m_appStoreIds;

        [SerializeField]
        [Tooltip("Usage permission settings.")]
        private NativeFeatureUsagePermissionSettings m_usagePermissionSettings;

        [SerializeField]
        [Tooltip("Stores the registration ids of this app.")]
        private AdditionalPlatformSupportSettings m_additionalPlatformSupportSettings;

        #endregion

        #region Properties

        public DebugLogger.LogLevel LogLevel => m_logLevel;

        public NativeFeatureUsagePermissionSettings UsagePermissionSettings => m_usagePermissionSettings;

        #endregion

        #region Constructors

        public ApplicationSettings(RuntimePlatformConstantSet appStoreIds = null,
            NativeFeatureUsagePermissionSettings usagePermissionSettings = null, DebugLogger.LogLevel logLevel = DebugLogger.LogLevel.Critical, AdditionalPlatformSupportSettings additionalPlatformSupportSettings = null)
        {
            // set properties
            m_appStoreIds = appStoreIds ?? new RuntimePlatformConstantSet();
            m_usagePermissionSettings = usagePermissionSettings ?? new NativeFeatureUsagePermissionSettings();
            m_logLevel = logLevel;
            m_additionalPlatformSupportSettings = additionalPlatformSupportSettings ?? new AdditionalPlatformSupportSettings();
        }

        #endregion

        #region Public methods

        public string GetAppStoreIdForPlatform(RuntimePlatform platform)
        {
            return m_appStoreIds.GetConstantForPlatform(platform);
        }

        public string GetAppStoreIdForActivePlatform()
        {
            return m_appStoreIds.GetConstantForActivePlatform();
        }

        public string GetAppStoreIdForActiveOrSimulationPlatform()
        {
            return m_appStoreIds.GetConstantForActiveOrSimulationPlatform();
        }
        
        public bool IsAndroidPcSupported()
        {
            return m_additionalPlatformSupportSettings.SupportAndroidPc;
        }

        #endregion
    }
}