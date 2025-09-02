#if UNITY_ANDROID
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using VoxelBusters.CoreLibrary.NativePlugins.Android;

namespace VoxelBusters.EssentialKit.ExtrasCore.Android
{
    public class UtilityInterface : NativeUtilityInterfaceBase
    {
        #region Fields

        private NativeApplicationUtility m_applicationUtility;

        #endregion

        #region Constructors

        public UtilityInterface()
            : base(isAvailable: true)
        {
            m_applicationUtility = new NativeApplicationUtility(NativeUnityPluginUtility.GetContext());
        }

        #endregion

        #region Base methods        

        public override void OpenAppStorePage(string applicationId)
        {
            m_applicationUtility.OpenGooglePlayStoreLink(applicationId);
        }

        public override void OpenApplicationSettings()
        {
            m_applicationUtility.OpenApplicationSettings();
        }

        #endregion
    }
}
#endif