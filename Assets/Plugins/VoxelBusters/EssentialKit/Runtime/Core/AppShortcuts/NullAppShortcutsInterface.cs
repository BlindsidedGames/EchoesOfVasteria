using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using VoxelBusters.CoreLibrary.NativePlugins;

namespace VoxelBusters.EssentialKit.AppShortcutsCore
{
    internal class NullAppShortcutsInterface : NativeFeatureInterfaceBase, INativeAppShortcutsInterface
    {
        #region Constructors

        public NullAppShortcutsInterface(): base(isAvailable: false)
        { }

        #endregion

        #region Private static methods

        private static void LogNotSupported()
        {
            Diagnostics.LogNotSupported("AppShortcuts");
        }

        #endregion

        #region INativeAppShortcutsInterface implementation methods

        #pragma warning disable
        public event ShortcutClickedInternalCallback OnShortcutClicked;
        #pragma warning restore
        
        public void Add(AppShortcutItem item)
        {
            LogNotSupported();
        }
        
        public void Remove(string shortcutId)
        {
            LogNotSupported();
        }
        
        #endregion
    }
}