#if UNITY_IOS || UNITY_TVOS
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using VoxelBusters.CoreLibrary.NativePlugins.iOS;

namespace VoxelBusters.EssentialKit.ExtrasCore.iOS
{
    public class NativeUtilityInterface : NativeUtilityInterfaceBase
    {
        #region Constructors

        public NativeUtilityInterface()
            : base(isAvailable: true)
        { }

        #endregion

        #region Base methods
        
        public override void OpenAppStorePage(string applicationId)
        {
            string  storeURL    = string.Format("itms-apps://itunes.apple.com/app/id{0}?action=write-review", applicationId);
            Application.OpenURL(storeURL);
        }

        public override void OpenApplicationSettings()
        {
            IosNativePluginsUtility.OpenApplicationSettings();
        }

        #endregion
    }
}
#endif