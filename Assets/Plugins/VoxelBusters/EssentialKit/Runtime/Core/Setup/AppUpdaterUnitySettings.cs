using System;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    [Serializable]
    public class AppUpdaterUnitySettings : SettingsPropertyGroup
    {
        #region Fields

        [SerializeField]
        [Tooltip("The default text used for update prompt title, if displayed.")]
        private         string           m_defaultPromptTitle = "New version available";

        [SerializeField]
        [Tooltip("The default text used for update prompt message, if displayed.")]
        private         string           m_defaultPromptMessage = "A new version of this app is available with exciting new features and improvements.";

        #endregion

        #region Properties

        public string DefaultPromptTitle => m_defaultPromptTitle;
        public string DefaultPromptMessage => m_defaultPromptMessage;

        #endregion

        #region Constructors

        public AppUpdaterUnitySettings(bool isEnabled = true, string defaultPromptTitle = null, string defaultPromptMessage = null)
            : base(isEnabled: isEnabled, name: NativeFeatureType.kAppUpdater)
        { 
            // set properties
            m_defaultPromptTitle        = defaultPromptTitle;
            m_defaultPromptMessage      = defaultPromptMessage;
        }

        #endregion
    }
    
    
}