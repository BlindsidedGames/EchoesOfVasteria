using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;
namespace VoxelBusters.EssentialKit
{
    [Serializable]
    public class AppShortcutsUnitySettings : SettingsPropertyGroup
    {
        #region Fields

        [SerializeField]
        [Tooltip("The texture used as small icon in post Android L Devices.")]
        private     List<Texture2D>       m_icons;
        

        #endregion

        #region Properties

        /// <summary>
        ///  The list of textures that can be used as icon for app shortcuts.
        /// </summary>
        /// \note Icon file names set in <see cref="AppShortcutItem.Builder.SetIconFileName"/> must be referred in this list. Else, it won't show up.
        public List<Texture2D> Icons
        {
            get
            {
                return m_icons;
            }
            set
            {
                m_icons = value;
            }
        }

        #endregion
        
        #region Constructors

        public AppShortcutsUnitySettings(bool isEnabled = true, List<Texture2D> icons = null)
            : base(isEnabled: isEnabled, name: NativeFeatureType.kTaskServices)
        {
            m_icons = icons;
        }

        #endregion
    }
}