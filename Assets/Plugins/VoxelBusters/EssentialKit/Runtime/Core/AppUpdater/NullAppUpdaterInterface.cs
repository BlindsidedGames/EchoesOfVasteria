using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.AppUpdaterCore
{
    internal class NullAppUpdaterInterface : NativeFeatureInterfaceBase, INativeAppUpdaterInterface
    {
        #region Constructors

        public NullAppUpdaterInterface(): base(isAvailable: false)
        { }

        #endregion

        #region Private static methods

        private static void LogNotSupported()
        {
            Diagnostics.LogNotSupported("AddressBook");
        }

        #endregion

        #region INativeAppUpdaterInterface implementation methods
        
        public void RequestUpdateInfo(EventCallback<AppUpdaterUpdateInfo> callback)
        {
            LogNotSupported();
        }

        public void PromptUpdate(PromptUpdateOptions options, EventCallback<float> callback)
        {
            LogNotSupported();
        }
        
        #endregion
    }
}