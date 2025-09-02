using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.EssentialKit
{
    [Serializable]
    public class UtilityUnitySettings : SettingsPropertyGroup
    {

        #region Fields

        #endregion


        #region Properties

        #endregion

        #region Constructors

        public UtilityUnitySettings(bool isEnabled = true)
            : base(isEnabled: isEnabled, name: NativeFeatureType.kExtras)
        {
        }

        #endregion
    }
}